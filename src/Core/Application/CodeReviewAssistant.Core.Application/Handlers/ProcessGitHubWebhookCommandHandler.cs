using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CodeReviewAssistant.Core.Application.Commands;
using CodeReviewAssistant.Core.Application.Interfaces;
using CodeReviewAssistant.Core.Domain.Entities;
using CodeReviewAssistant.Core.Domain.ValueObjects;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Core.Application.Handlers
{
    public class ProcessGitHubWebhookCommandHandler : IRequestHandler<ProcessGitHubWebhookCommand, ProcessGitHubWebhookResult>
    {
        private readonly ILogger<ProcessGitHubWebhookCommandHandler> _logger;
        private readonly IGitHubService _gitHubService;
        private readonly IPullRequestRepository _pullRequestRepository;
        private readonly ICodeReviewRepository _codeReviewRepository;
        private readonly IMediator _mediator;
        private readonly IEventPublisher _eventPublisher;

        public ProcessGitHubWebhookCommandHandler(
            ILogger<ProcessGitHubWebhookCommandHandler> logger,
            IGitHubService gitHubService,
            IPullRequestRepository pullRequestRepository,
            ICodeReviewRepository codeReviewRepository,
            IMediator mediator,
            IEventPublisher eventPublisher)
        {
            _logger = logger;
            _gitHubService = gitHubService;
            _pullRequestRepository = pullRequestRepository;
            _codeReviewRepository = codeReviewRepository;
            _mediator = mediator;
            _eventPublisher = eventPublisher;
        }

        public async Task<ProcessGitHubWebhookResult> Handle(
            ProcessGitHubWebhookCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing GitHub webhook event: {EventType} for delivery: {DeliveryId}", 
                    request.EventType, request.DeliveryId);

                // Validate webhook signature
                if (!_gitHubService.ValidateWebhookSignature(request.RequestBody, request.Signature))
                {
                    return ProcessGitHubWebhookResult.FailureResult("Invalid webhook signature");
                }

                // Parse webhook payload
                var webhookData = JsonSerializer.Deserialize<JsonElement>(request.WebhookPayload);
                
                switch (request.EventType.ToLower())
                {
                    case "pull_request":
                        return await HandlePullRequestEvent(webhookData, cancellationToken);
                    
                    case "push":
                        return await HandlePushEvent(webhookData, cancellationToken);
                    
                    default:
                        _logger.LogWarning("Unsupported GitHub event type: {EventType}", request.EventType);
                        return ProcessGitHubWebhookResult.FailureResult($"Unsupported event type: {request.EventType}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitHub webhook: {Error}", ex.Message);
                return Process webhook: {Error}", ex.Message);
                return ProcessGitHubWebhookResult.FailureResult($"Internal error: {ex.Message}");
            }
        }

        private async Task<ProcessGitHubWebhookResult> HandlePullRequestEvent(
            JsonElement webhookData, 
            CancellationToken cancellationToken)
        {
            var action = webhookData.GetProperty("action").GetString();
            var prData = webhookData.GetProperty("pull_request");
            var repoData = webhookData.GetProperty("repository");

            var pullRequestId = prData.GetProperty("id").GetInt32();
            var repositoryName = repoData.GetProperty("name").GetString();
            var repositoryOwner = repoData.GetProperty("owner").GetProperty("login").GetString();
            var title = prData.GetProperty("title").GetString();
            var description = prData.GetProperty("body").GetString() ?? string.Empty;
            var sourceBranch = prData.GetProperty("head").GetProperty("ref").GetString();
            var targetBranch = prData.GetProperty("base").GetProperty("ref").GetString();
            var headCommitSha = prData.GetProperty("head").GetProperty("sha").GetString();
            var baseCommitSha = prData.GetProperty("base").GetProperty("sha").GetString();
            var author = prData.GetProperty("user").GetProperty("login").GetString();
            var authorEmail = prData.GetProperty("user").GetProperty("email").GetString() ?? string.Empty;

            // Check if pull request already exists
            var existingPR = await _pullRequestRepository.GetByGitHubIdAsync(pullRequestId, cancellationToken);
            
            if (existingPR != null)
            {
                // Update existing pull request
                switch (action)
                {
                    case "opened":
                    case "reopened":
                        await UpdatePullRequestStatus(existingPR, PullRequestStatus.Open, cancellationToken);
                        break;
                    case "closed":
                        if (prData.TryGetProperty("merged", out var mergedElement) && mergedElement.GetBoolean())
                        {
                            var mergeCommitSha = prData.GetProperty("merge_commit_sha").GetString();
                            await UpdatePullRequestStatus(existingPR, PullRequestStatus.Merged, cancellationToken);
                            existingPR.MarkAsMerged(mergeCommitSha);
                        }
                        else
                        {
                            await UpdatePullRequestStatus(existingPR, PullRequestStatus.Closed, cancellationToken);
                        }
                        break;
                    case "synchronize":
                        // Pull request was updated with new commits
                        existingPR.HeadCommitSha = headCommitSha;
                        await _pullRequestRepository.UpdateAsync(existingPR, cancellationToken);
                        break;
                }

                await _pullRequestRepository.SaveChangesAsync(cancellationToken);
                return ProcessGitHubWebhookResult.SuccessResult(existingPR.Id);
            }

            // Create new pull request
            var pullRequest = new GitHubPullRequest(
                pullRequestId,
                repositoryName,
                repositoryOwner,
                title,
                description,
                sourceBranch,
                targetBranch,
                headCommitSha,
                baseCommitSha,
                author,
                authorEmail,
                request.WebhookPayload);

            if (action == "draft")
            {
                pullRequest.MarkAsDraft();
            }

            await _pullRequestRepository.AddAsync(pullRequest, cancellationToken);
            await _pullRequestRepository.SaveChangesAsync(cancellationToken);

            // Publish domain event
            await _eventPublisher.PublishAsync(new GitHubPullRequestReceivedEvent(
                pullRequest.Id,
                pullRequestId,
                repositoryName,
                repositoryOwner,
                sourceBranch,
                targetBranch,
                author,
                action));

            // Trigger code review if PR is opened and not a draft
            if ((action == "opened" || action == "reopened") && !pullRequest.IsDraft)
            {
                var codeReviewCommand = new CreateCodeReviewCommand(
                    title,
                    description,
                    $"https://github.com/{repositoryOwner}/{repositoryName}",
                    sourceBranch,
                    headCommitSha,
                    author);

                var codeReviewResult = await _mediator.Send(codeReviewCommand, cancellationToken);
                
                if (codeReviewResult.Success)
                {
                    return ProcessGitHubWebhookResult.SuccessResult(
                        pullRequest.Id,
                        codeReviewResult.CodeReviewId,
                        new List<string> { "Pull request created", "Code review started" });
                }
            }

            return ProcessGitHubWebhookResult.SuccessResult(pullRequest.Id);
        }

        private async Task<ProcessGitHubWebhookResult> HandlePushEvent(
            JsonElement webhookData, 
            CancellationToken cancellationToken)
        {
            var repoData = webhookData.GetProperty("repository");
            var commits = webhookData.GetProperty("commits");
            var refName = webhookData.GetProperty("ref").GetString();

            // Only process pushes to branches (not tags)
            if (!refName.StartsWith("refs/heads/"))
            {
                return ProcessGitHubWebhookResult.SuccessResult();
            }

            var branchName = refName.Replace("refs/heads/", "");
            var repositoryName = repoData.GetProperty("name").GetString();
            var repositoryOwner = repoData.GetProperty("owner").GetProperty("login").GetString();

            _logger.LogInformation("Processing push event for {Repository}/{Branch}", 
                $"{repositoryOwner}/{repositoryName}", branchName);

            // Process each commit if needed
            foreach (var commitElement in commits.EnumerateArray())
            {
                var commitSha = commitElement.GetProperty("id").GetString();
                var message = commitElement.GetProperty("message").GetString();
                var author = commitElement.GetProperty("author").GetProperty("name").GetString();

                // You might want to trigger analysis for certain types of commits
                if (ShouldAnalyzeCommit(message))
                {
                    _logger.LogInformation("Triggering analysis for commit {CommitSha}", commitSha);
                    // Implement commit analysis logic here
                }
            }

            return ProcessGitHubWebhookResult.SuccessResult();
        }

        private async Task UpdatePullRequestStatus(
            GitHubPullRequest pullRequest, 
            PullRequestStatus status, 
            CancellationToken cancellationToken)
        {
            pullRequest.UpdateStatus(status);
            await _pullRequestRepository.UpdateAsync(pullRequest, cancellationToken);
        }

        private bool ShouldAnalyzeCommit(string commitMessage)
        {
            // Implement logic to determine if a commit should be analyzed
            var triggerKeywords = new[] { "feat:", "fix:", "refactor:", "perf:", "security:" };
            return triggerKeywords.Any(keyword => commitMessage.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
