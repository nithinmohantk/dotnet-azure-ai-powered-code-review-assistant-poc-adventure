using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CodeReviewAssistant.WebApi.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            AddSecurityHeaders(context);

            await _next(context);
        }

        private void AddSecurityHeaders(HttpContext context)
        {
            // OWASP Top 10 Security Headers
            
            // A01: Broken Access Control - Prevent clickjacking
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            
            // A02: Cryptographic Failures - Enable HSTS
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            
            // A03: Injection - XSS Protection
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // A05: Security Misconfiguration - Content Security Policy
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self'; " +
                     "connect-src 'self'; " +
                     "frame-ancestors 'none'; " +
                     "base-uri 'self'; " +
                     "form-action 'self'";
            
            context.Response.Headers.Add("Content-Security-Policy", csp);
            
            // A07: Identification and Authentication Failures - Permissions Policy
            var permissionsPolicy = "geolocation=(), " +
                                  "microphone=(), " +
                                  "camera=(), " +
                                  "payment=(), " +
                                  "usb=(), " +
                                  "magnetometer=(), " +
                                  "gyroscope=(), " +
                                  "accelerometer=()";
            
            context.Response.Headers.Add("Permissions-Policy", permissionsPolicy);
            
            // Additional security headers
            context.Response.Headers.Add("X-Permitted-Cross-Domain-Policies", "none");
            context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
            context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
            context.Response.Headers.Add("Cross-Origin-Resource-Policy", "same-origin");
            
            _logger.LogDebug("Security headers added to response");
        }
    }
}
