using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CodeReviewAssistant.Core.Application.Commands;

namespace CodeReviewAssistant.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusProcessor _serviceBusProcessor;
    private readonly ServiceBusClient _serviceBusClient;

    public Worker(
        ILogger<Worker> logger,
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        
        // Configure Service Bus processor
        var queueName = "codereview-requests";
        _serviceBusProcessor = serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            MaxConcurrentCalls = 10,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });

        // Register message handlers
        _serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
        _serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Code Review Assistant Worker starting...");

        try
        {
            // Start processing Service Bus messages
            await _serviceBusProcessor.StartProcessingAsync(stoppingToken);

            // Keep the worker running
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Worker is running and listening for messages...");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker encountered an error during execution");
            throw;
        }
        finally
        {
            _logger.LogInformation("Code Review Assistant Worker stopping...");
            await _serviceBusProcessor.StopProcessingAsync(stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message;
        var messageId = message.MessageId;
        var correlationId = message.CorrelationId;

        _logger.LogInformation("Processing Service Bus message: {MessageId} with correlation: {CorrelationId}", 
            messageId, correlationId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Determine message type and process accordingly
            var messageType = message.ContentType ?? "application/json";
            var messageBody = message.Body.ToString();

            switch (message.Subject?.ToLower())
            {
                case "startaianalysis":
                    await ProcessStartAIAnalysisCommand(messageBody, mediator);
                    break;
                
                case "processwebhook":
                    await ProcessGitHubWebhookCommand(messageBody, mediator);
                    break;
                
                case "createreview":
                    await ProcessCreateCodeReviewCommand(messageBody, mediator);
                    break;
                
                default:
                    _logger.LogWarning("Unknown message subject: {Subject}", message.Subject);
                    break;
            }

            // Complete the message if processing was successful
            await args.CompleteMessageAsync(message);
            _logger.LogInformation("Successfully processed message: {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: {MessageId}", messageId);
            
            // Don't complete the message, let it be retried or moved to dead-letter
            await args.AbandonMessageAsync(message);
        }
    }

    private async Task ProcessStartAIAnalysisCommand(string messageBody, IMediator mediator)
    {
        var command = JsonSerializer.Deserialize<StartAIAnalysisCommand>(messageBody);
        if (command != null)
        {
            await mediator.Send(command);
            _logger.LogInformation("Started AI analysis for Code Review: {CodeReviewId}", command.CodeReviewId);
        }
    }

    private async Task ProcessGitHubWebhookCommand(string messageBody, IMediator mediator)
    {
        var command = JsonSerializer.Deserialize<ProcessGitHubWebhookCommand>(messageBody);
        if (command != null)
        {
            await mediator.Send(command);
            _logger.LogInformation("Processed GitHub webhook for delivery: {DeliveryId}", command.DeliveryId);
        }
    }

    private async Task ProcessCreateCodeReviewCommand(string messageBody, IMediator mediator)
    {
        var command = JsonSerializer.Deserialize<CreateCodeReviewCommand>(messageBody);
        if (command != null)
        {
            await mediator.Send(command);
            _logger.LogInformation("Created Code Review: {Title}", command.Title);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error processing Service Bus message: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker is stopping...");
        await _serviceBusProcessor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _serviceBusProcessor?.CloseAsync();
        _serviceBusClient?.DisposeAsync();
        base.Dispose();
    }
}
