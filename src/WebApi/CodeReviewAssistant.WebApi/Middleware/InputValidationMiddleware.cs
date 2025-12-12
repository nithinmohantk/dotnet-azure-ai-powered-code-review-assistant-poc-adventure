using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeReviewAssistant.WebApi.Middleware
{
    public class InputValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InputValidationMiddleware> _logger;
        private readonly InputValidationOptions _options;

        public InputValidationMiddleware(
            RequestDelegate next,
            ILogger<InputValidationMiddleware> logger,
            IOptions<InputValidationOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Validate request headers for injection attacks
            if (!ValidateHeaders(context))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid request headers detected");
                return;
            }

            // Validate request body for injection attacks
            if (context.Request.ContentLength > 0 && 
                (context.Request.Method == HttpMethods.Post || 
                 context.Request.Method == HttpMethods.Put || 
                 context.Request.Method == HttpMethods.Patch))
            {
                if (!await ValidateRequestBody(context))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Invalid request body detected");
                    return;
                }
            }

            await _next(context);
        }

        private bool ValidateHeaders(HttpContext context)
        {
            foreach (var header in context.Request.Headers)
            {
                // Check for SQL injection patterns
                if (ContainsSqlInjection(header.Value))
                {
                    _logger.LogWarning("SQL injection attempt detected in header {HeaderName}", header.Key);
                    return false;
                }

                // Check for XSS patterns
                if (ContainsXss(header.Value))
                {
                    _logger.LogWarning("XSS attempt detected in header {HeaderName}", header.Key);
                    return false;
                }

                // Check for command injection patterns
                if (ContainsCommandInjection(header.Value))
                {
                    _logger.LogWarning("Command injection attempt detected in header {HeaderName}", header.Key);
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ValidateRequestBody(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;

                // Check for SQL injection patterns
                if (ContainsSqlInjection(body))
                {
                    _logger.LogWarning("SQL injection attempt detected in request body");
                    return false;
                }

                // Check for XSS patterns
                if (ContainsXss(body))
                {
                    _logger.LogWarning("XSS attempt detected in request body");
                    return false;
                }

                // Check for command injection patterns
                if (ContainsCommandInjection(body))
                {
                    _logger.LogWarning("Command injection attempt detected in request body");
                    return false;
                }

                // Check for path traversal
                if (ContainsPathTraversal(body))
                {
                    _logger.LogWarning("Path traversal attempt detected in request body");
                    return false;
                }

                // Check for LDAP injection
                if (ContainsLdapInjection(body))
                {
                    _logger.LogWarning("LDAP injection attempt detected in request body");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating request body");
                return false;
            }
        }

        private bool ContainsSqlInjection(string input)
        {
            var sqlPatterns = _options.SqlInjectionPatterns;
            
            foreach (var pattern in sqlPatterns)
            {
                if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsXss(string input)
        {
            var xssPatterns = _options.XssPatterns;
            
            foreach (var pattern in xssPatterns)
            {
                if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsCommandInjection(string input)
        {
            var commandPatterns = _options.CommandInjectionPatterns;
            
            foreach (var pattern in commandPatterns)
            {
                if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsPathTraversal(string input)
        {
            var pathTraversalPatterns = _options.PathTraversalPatterns;
            
            foreach (var pattern in pathTraversalPatterns)
            {
                if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsLdapInjection(string input)
        {
            var ldapPatterns = _options.LdapInjectionPatterns;
            
            foreach (var pattern in ldapPatterns)
            {
                if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class InputValidationOptions
    {
        public List<string> SqlInjectionPatterns { get; set; } = new()
        {
            "OR 1=1", "DROP TABLE", "INSERT INTO", "DELETE FROM", "UPDATE SET",
            "UNION SELECT", "EXEC(", "SP_EXECUTESQL", "xp_cmdshell", "WAITFOR DELAY",
            "BULK INSERT", "OPENROWSET", "OPENDATASOURCE", "--", "/*", "*/"
        };

        public List<string> XssPatterns { get; set; } = new()
        {
            "<script>", "</script>", "javascript:", "vbscript:", "onload=", "onerror=",
            "onclick=", "onmouseover=", "onfocus=", "onblur=", "onchange=", "onsubmit=",
            "alert(", "confirm(", "prompt(", "eval(", "document.cookie", "window.location",
            "innerHTML", "outerHTML", "document.write"
        };

        public List<string> CommandInjectionPatterns { get; set; } = new()
        {
            ";", "|", "&", "&&", "||", "`", "$(", "${", ">", ">>", "<", "<<",
            "cmd.exe", "powershell", "bash", "sh", "nc", "netcat", "wget", "curl",
            "rm -rf", "dd if=", "cat /etc/passwd", "cat /etc/shadow"
        };

        public List<string> PathTraversalPatterns { get; set; } = new()
        {
            "../", "..\\", "%2e%2e%2f", "%2e%2e\\", "..%2f", "..%5c",
            "/etc/passwd", "/etc/shadow", "/etc/hosts", "c:\\windows\\system32"
        };

        public List<string> LdapInjectionPatterns { get; set; } = new()
        {
            "*", ")", "(", "|", "&", "!", "=", "<", ">", "/*", "*/", "//",
            "(&", ")(|", "(!", "uid=", "cn=", "sn=", "mail=", "objectClass="
        };
    }
}
