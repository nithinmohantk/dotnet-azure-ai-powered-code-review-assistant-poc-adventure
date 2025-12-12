using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeReviewAssistant.WebApi.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger,
            IOptions<RateLimitOptions> options)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);
            var endpoint = context.Request.Path.Value;
            var key = $"rate_limit_{clientId}_{endpoint}";

            var rateLimitInfo = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return new RateLimitInfo
                {
                    Requests = 0,
                    WindowStart = DateTime.UtcNow
                };
            });

            rateLimitInfo.Requests++;

            if (rateLimitInfo.Requests > _options.MaxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", clientId, endpoint);
                
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers.Add("Retry-After", "60");
                context.Response.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerMinute.ToString());
                context.Response.Headers.Add("X-RateLimit-Remaining", "0");
                context.Response.Headers.Add("X-RateLimit-Reset", 
                    (DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1)).TotalSeconds.ToString());
                
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            context.Response.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerMinute.ToString());
            context.Response.Headers.Add("X-RateLimit-Remaining", 
                (_options.MaxRequestsPerMinute - rateLimitInfo.Requests).ToString());
            context.Response.Headers.Add("X-RateLimit-Reset", 
                (DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1)).TotalSeconds.ToString());

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Use IP address as client identifier
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            // If behind a proxy, use the forwarded IP
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();
            }
            
            return ipAddress ?? "unknown";
        }
    }

    public class RateLimitOptions
    {
        public int MaxRequestsPerMinute { get; set; } = 100;
        public int MaxRequestsPerHour { get; set; } = 1000;
        public int MaxRequestsPerDay { get; set; } = 10000;
        public List<string> WhitelistedIPs { get; set; } = new();
        public List<string> BlacklistedIPs { get; set; } = new();
    }

    public class RateLimitInfo
    {
        public int Requests { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
