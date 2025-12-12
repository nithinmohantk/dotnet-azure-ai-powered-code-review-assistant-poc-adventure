using FluentValidation;
using CodeReviewAssistant.Core.Application.Commands;

namespace CodeReviewAssistant.Core.Application.Validators
{
    public class CreateCodeReviewCommandValidator : AbstractValidator<CreateCodeReviewCommand>
    {
        public CreateCodeReviewCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Title is required")
                .MaximumLength(200)
                .WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters");

            RuleFor(x => x.RepositoryUrl)
                .NotEmpty()
                .WithMessage("Repository URL is required")
                .Must(BeValidGitHubUrl)
                .WithMessage("Repository URL must be a valid GitHub URL");

            RuleFor(x => x.BranchName)
                .NotEmpty()
                .WithMessage("Branch name is required")
                .MaximumLength(100)
                .WithMessage("Branch name cannot exceed 100 characters")
                .Matches(@"^[a-zA-Z0-9/_-]+$")
                .WithMessage("Branch name can only contain letters, numbers, underscores, hyphens, and forward slashes");

            RuleFor(x => x.CommitHash)
                .NotEmpty()
                .WithMessage("Commit hash is required")
                .Length(7, 40)
                .WithMessage("Commit hash must be between 7 and 40 characters")
                .Matches(@"^[a-fA-F0-9]+$")
                .WithMessage("Commit hash must contain only hexadecimal characters");

            RuleFor(x => x.RequestedBy)
                .NotEmpty()
                .WithMessage("Requested by is required")
                .MaximumLength(100)
                .WithMessage("Requested by cannot exceed 100 characters");

            RuleFor(x => x.Priority)
                .IsInEnum()
                .WithMessage("Priority must be a valid enum value");
        }

        private bool BeValidGitHubUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var uri = new Uri(url);
                return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
                       uri.AbsolutePath.Split('/').Length >= 3;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ProcessGitHubWebhookCommandValidator : AbstractValidator<ProcessGitHubWebhookCommand>
    {
        public ProcessGitHubWebhookCommandValidator()
        {
            RuleFor(x => x.WebhookPayload)
                .NotEmpty()
                .WithMessage("Webhook payload is required")
                .Must(BeValidJson)
                .WithMessage("Webhook payload must be valid JSON");

            RuleFor(x => x.EventType)
                .NotEmpty()
                .WithMessage("Event type is required")
                .Must(BeValidGitHubEvent)
                .WithMessage("Event type must be a valid GitHub event");

            RuleFor(x => x.DeliveryId)
                .NotEmpty()
                .WithMessage("Delivery ID is required");

            RuleFor(x => x.Signature)
                .NotEmpty()
                .WithMessage("Signature is required")
                .Matches(@"^sha256=[a-fA-F0-9]{64}$")
                .WithMessage("Signature must be a valid SHA256 HMAC signature");

            RuleFor(x => x.RequestBody)
                .NotEmpty()
                .WithMessage("Request body is required");

            RuleFor(x => x.Headers)
                .NotNull()
                .WithMessage("Headers are required")
                .Must(HaveRequiredHeaders)
                .WithMessage("Required headers are missing");
        }

        private bool BeValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool BeValidGitHubEvent(string eventType)
        {
            var validEvents = new[]
            {
                "pull_request", "push", "issues", "issue_comment", 
                "pull_request_review", "pull_request_review_comment",
                "release", "deployment", "status", "check_run", "check_suite"
            };

            return validEvents.Contains(eventType);
        }

        private bool HaveRequiredHeaders(Dictionary<string, string> headers)
        {
            var requiredHeaders = new[] { "X-GitHub-Event", "X-GitHub-Delivery", "X-Hub-Signature-256" };
            return requiredHeaders.All(header => headers.ContainsKey(header));
        }
    }

    public class StartAIAnalysisCommandValidator : AbstractValidator<StartAIAnalysisCommand>
    {
        public StartAIAnalysisCommandValidator()
        {
            RuleFor(x => x.CodeReviewId)
                .NotEmpty()
                .WithMessage("Code review ID is required");

            RuleFor(x => x.AnalysisType)
                .NotEmpty()
                .WithMessage("Analysis type is required")
                .Must(BeValidAnalysisType)
                .WithMessage("Analysis type must be valid");

            RuleFor(x => x.ModelName)
                .NotEmpty()
                .WithMessage("Model name is required")
                .MaximumLength(100)
                .WithMessage("Model name cannot exceed 100 characters");

            RuleFor(x => x.ModelVersion)
                .NotEmpty()
                .WithMessage("Model version is required")
                .Matches(@"^\d+\.\d+\.\d+$")
                .WithMessage("Model version must be in semantic version format (x.y.z)");

            RuleFor(x => x.AnalysisParameters)
                .NotNull()
                .WithMessage("Analysis parameters are required")
                .Must(HaveValidParameters)
                .WithMessage("Analysis parameters contain invalid values");
        }

        private bool BeValidAnalysisType(string analysisType)
        {
            var validTypes = new[]
            {
                "security", "performance", "code_quality", "architecture", 
                "documentation", "testing", "maintainability", "comprehensive"
            };

            return validTypes.Contains(analysisType);
        }

        private bool HaveValidParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return false;

            var validParameterKeys = new[]
            {
                "max_tokens", "temperature", "top_p", "frequency_penalty", 
                "presence_penalty", "timeout", "retry_count"
            };

            return parameters.Keys.All(key => validParameterKeys.Contains(key));
        }
    }
}
