using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeReviewAssistant.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
        Task PublishAsync<T>(T @event, string topic, CancellationToken cancellationToken = default) where T : class;
        Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : class;
        Task PublishWithDelayAsync<T>(T @event, TimeSpan delay, CancellationToken cancellationToken = default) where T : class;
        Task PublishWithRetryAsync<T>(T @event, int maxRetries = 3, CancellationToken cancellationToken = default) where T : class;
    }

    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
        Guid EventId { get; }
    }

    public abstract class DomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();
    }

    public class IntegrationEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
        public Guid EventId { get; } = Guid.NewGuid();
        public string EventType { get; }
        public string EventVersion { get; }

        protected IntegrationEvent(string eventType, string eventVersion = "1.0")
        {
            EventType = eventType;
            EventVersion = eventVersion;
        }
    }
}
