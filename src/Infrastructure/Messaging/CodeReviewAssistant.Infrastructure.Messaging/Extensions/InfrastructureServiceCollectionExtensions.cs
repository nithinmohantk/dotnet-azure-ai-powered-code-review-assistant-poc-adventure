using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeReviewAssistant.Application.Interfaces;
using CodeReviewAssistant.Infrastructure.Messaging.Configuration;

namespace CodeReviewAssistant.Infrastructure.Messaging.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            // Register EventPublisher options
            services.Configure<EventPublisherOptions>(configuration.GetSection("EventBus"));

            // Register Service Bus client
            var connectionString = configuration["EventBus:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("EventBus:ConnectionString configuration is required");
            }

            services.AddSingleton(new ServiceBusClient(connectionString));

            // Register EventPublisher
            services.AddScoped<IEventPublisher, EventPublisher>();

            return services;
        }

        public static IServiceCollection AddInfrastructureMessaging(this IServiceCollection services, string connectionString)
        {
            // Register EventPublisher options with defaults
            services.Configure<EventPublisherOptions>(options =>
            {
                options.DefaultTopic = "code-review-events";
                options.DefaultMessageTtl = TimeSpan.FromHours(1);
                options.EventVersion = "1.0";
                options.EventSource = "CodeReviewAssistant";
                options.MaxConcurrentPublishes = 10;
            });

            // Register Service Bus client
            services.AddSingleton(new ServiceBusClient(connectionString));

            // Register EventPublisher
            services.AddScoped<IEventPublisher, EventPublisher>();

            return services;
        }
    }
}
