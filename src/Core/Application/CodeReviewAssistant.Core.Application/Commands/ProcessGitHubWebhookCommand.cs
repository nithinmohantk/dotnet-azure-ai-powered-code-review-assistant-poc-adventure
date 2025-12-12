using MediatR;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Application.Commands
{
    public record ProcessGitHubWebhookCommand : IRequest<ProcessGitHubWebhookResult>
    {
        public string WebhookPayload { get; init; }
        public string EventType { get; init; }
        public string DeliveryId { get; init; }
        public string Signature { get; init; }
        public string RequestBody { get; init; }
        public Dictionary<string, string> Headers { get; init; }

        public ProcessGitHubWebhookCommand(
            string webhookPayload,
            string eventType,
            string deliveryId,
            string signature,
            string requestBody,
            Dictionary<string, string> headers)
        {
            WebhookPayload = webhookPayload ?? throw new ArgumentNullException(nameof(webhookPayload));
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            DeliveryId = deliveryId ?? throw new ArgumentNullException(nameof(deliveryId));
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            RequestBody = requestBody ?? throw new ArgumentNullException(nameof(requestBody));
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
        }
    }

    public record ProcessGitHubWebhookResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public Guid? PullRequestId { get; init; }
        public Guid? CodeReviewId { get; init; }
        public List<string> ActionsTaken { get; init; } = new();

        public static ProcessGitHubWebhookResult SuccessResult(
            Guid? pullRequestId = null,
            Guid? codeReviewId = null,
            List<string> actions = null)
        {
            return new ProcessGitHubWebhookResult
            {
                Success = true,
                PullRequestId = pullRequestId,
                CodeReviewId = codeReviewId,
                ActionsTaken = actions ?? new List<string>()
            };
        }

        public static ProcessGitHubWebhookResult FailureResult(string message)
        {
            return new ProcessGitHubWebhookResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
