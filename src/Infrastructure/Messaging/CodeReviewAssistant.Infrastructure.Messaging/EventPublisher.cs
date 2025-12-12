using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Messaging.ServiceBus;
using CodeReviewAssistant.Application.Interfaces;
using CodeReviewAssistant.Infrastructure.Messaging.Configuration;

namespace CodeReviewAssistant.Infrastructure.Messaging
{
    public class EventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly EventPublisherOptions _options;
        private readonly ILogger<EventPublisher> _logger;
        private readonly SemaphoreSlim _publishSemaphore;
        private readonly Dictionary<Type, ServiceBusSender> _senders;
        private readonly object _sendersLock = new object();

        public EventPublisher(ServiceBusClient serviceBusClient, IOptions<EventPublisherOptions> options, ILogger<EventPublisher> logger)
        {
            _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publishSemaphore = new SemaphoreSlim(_options.MaxConcurrentPublishes, _options.MaxConcurrentPublishes);
            _senders = new Dictionary<Type, ServiceBusSender>();
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var topicName = GetTopicName(typeof(T));
            await PublishAsync(@event, topicName, cancellationToken);
        }

        public async Task PublishBatchAsync<T>(IEnumerable<T> events, CancellationToken cancellationToken = default) where T : class
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            var eventList = events.ToList();
            if (!eventList.Any())
                return;

            var topicName = GetTopicName(typeof(T));
            await PublishBatchAsync(eventList, topicName, cancellationToken);
        }

        public async Task PublishWithDelayAsync<T>(T @event, TimeSpan delay, CancellationToken cancellationToken = default) where T : class
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var topicName = GetTopicName(typeof(T));
            await PublishWithDelayAsync(@event, topicName, delay, cancellationToken);
        }

        public async Task PublishWithRetryAsync<T>(T @event, int maxRetries = 3, CancellationToken cancellationToken = default) where T : class
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            var topicName = GetTopicName(typeof(T));
            await PublishWithRetryAsync(@event, topicName, maxRetries, cancellationToken);
        }

        public async Task PublishAsync<T>(T @event, string topic, CancellationToken cancellationToken = default) where T : class
        {
            await _publishSemaphore.WaitAsync(cancellationToken);
            try
            {
                var sender = GetOrCreateSender(typeof(T), topic);
                var messageBody = JsonSerializer.Serialize(@event, _options.JsonSerializerOptions);
                var serviceBusMessage = CreateServiceBusMessage(@event);

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                _logger.LogInformation("Successfully published event {EventType} to topic {Topic} with MessageId {MessageId}", 
                    typeof(T).Name, topic, serviceBusMessage.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventType} to topic {Topic}", typeof(T).Name, topic);
                throw;
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        private async Task PublishBatchAsync<T>(IEnumerable<T> events, string topic, CancellationToken cancellationToken) where T : class
        {
            await _publishSemaphore.WaitAsync(cancellationToken);
            try
            {
                var sender = GetOrCreateSender(typeof(T), topic);
                var messages = events.Select(@event => CreateServiceBusMessage(@event)).ToList();
                
                if (messages.Count == 1)
                {
                    await sender.SendMessageAsync(messages.First(), cancellationToken);
                }
                else
                {
                    await sender.SendMessagesAsync(messages, cancellationToken);
                }

                _logger.LogInformation("Successfully published {EventCount} events of type {EventType} to topic {Topic}", 
                    messages.Count, typeof(T).Name, topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish batch events of type {EventType} to topic {Topic}", typeof(T).Name, topic);
                throw;
            }
            finally
            {
                _publishSemaphore.Release();
            }
        }

        private async Task PublishWithDelayAsync<T>(T @event, string topic, TimeSpan delay, CancellationToken cancellationToken) where T : class
        {
            await Task.Delay(delay, cancellationToken);
            await PublishAsync(@event, topic, cancellationToken);
        }

        private async Task PublishWithRetryAsync<T>(T @event, string topic, int maxRetries, CancellationToken cancellationToken) where T : class
        {
            var attempt = 0;
            Exception lastException = null;

            while (attempt < maxRetries)
            {
                try
                {
                    await PublishAsync(@event, topic, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    attempt++;
                    
                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                        _logger.LogWarning(ex, "Attempt {Attempt} failed to publish event {EventType} to topic {Topic}. Retrying in {Delay}s", 
                            attempt, typeof(T).Name, topic, delay.TotalSeconds);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            _logger.LogError(lastException, "Failed to publish event {EventType} to topic {Topic} after {MaxRetries} attempts", 
                typeof(T).Name, topic, maxRetries);
            throw lastException;
        }

        private ServiceBusMessage CreateServiceBusMessage<T>(T @event) where T : class
        {
            var messageBody = JsonSerializer.Serialize(@event, _options.JsonSerializerOptions);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = Guid.NewGuid().ToString(),
                Subject = typeof(T).Name,
                ContentType = "application/json",
                TimeToLive = _options.DefaultMessageTtl,
                ScheduledEnqueueTime = _options.DefaultScheduledEnqueueTime
            };

            // Add custom properties
            serviceBusMessage.ApplicationProperties.Add("EventType", typeof(T).Name);
            serviceBusMessage.ApplicationProperties.Add("EventVersion", _options.EventVersion);
            serviceBusMessage.ApplicationProperties.Add("Timestamp", DateTime.UtcNow);
            serviceBusMessage.ApplicationProperties.Add("Source", _options.EventSource);
            serviceBusMessage.ApplicationProperties.Add("CorrelationId", Guid.NewGuid().ToString());

            // Add event-specific properties if configured
            if (_options.EventProperties.TryGetValue(typeof(T).Name, out var properties))
            {
                foreach (var property in properties)
                {
                    serviceBusMessage.ApplicationProperties.Add(property.Key, property.Value);
                }
            }

            return serviceBusMessage;
        }

        private ServiceBusSender GetOrCreateSender(Type eventType, string topic)
        {
            lock (_sendersLock)
            {
                if (_senders.TryGetValue(eventType, out var sender))
                {
                    return sender;
                }

                sender = _serviceBusClient.CreateSender(topic);
                _senders[eventType] = sender;
                return sender;
            }
        }

        private string GetTopicName(Type eventType)
        {
            var eventTypeName = eventType.Name;

            if (_options.TopicMappings.TryGetValue(eventTypeName, out var topicName))
            {
                return topicName;
            }

            return _options.DefaultTopic;
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }
            _senders.Clear();
            
            await _serviceBusClient.DisposeAsync();
            _publishSemaphore.Dispose();
        }
    }
}
