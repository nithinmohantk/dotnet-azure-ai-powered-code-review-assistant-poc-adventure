using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CodeReviewAssistant.Infrastructure.Messaging.Configuration
{
    public class EventPublisherOptions
    {
        public string DefaultTopic { get; set; } = "code-review-events";
        public Dictionary<string, string> TopicMappings { get; set; } = new();
        public TimeSpan DefaultMessageTtl { get; set; } = TimeSpan.FromHours(1);
        public DateTimeOffset? DefaultScheduledEnqueueTime { get; set; }
        public string EventVersion { get; set; } = "1.0";
        public string EventSource { get; set; } = "CodeReviewAssistant";
        public int MaxConcurrentPublishes { get; set; } = 10;
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        public Dictionary<string, Dictionary<string, object>> EventProperties { get; set; } = new();
    }
}
