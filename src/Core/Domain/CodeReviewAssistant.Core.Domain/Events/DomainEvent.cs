using System;

namespace CodeReviewAssistant.Core.Domain.Events
{
    public abstract class DomainEvent
    {
        public Guid Id { get; private set; }
        public DateTime OccurredOn { get; private set; }
        public string EventType { get; private set; }

        protected DomainEvent()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
            EventType = GetType().Name;
        }
    }

    public class CodeReviewRequestedEvent : DomainEvent
    {
        public Guid CodeReviewId { get; private set; }
        public string RepositoryUrl { get; private set; }
        public string BranchName { get; private set; }
        public string CommitHash { get; private set; }
        public string RequestedBy { get; private set; }

        public CodeReviewRequestedEvent(
            Guid codeReviewId,
            string repositoryUrl,
            string branchName,
            string commitHash,
            string requestedBy)
        {
            CodeReviewId = codeReviewId;
            RepositoryUrl = repositoryUrl;
            BranchName = branchName;
            CommitHash = commitHash;
            RequestedBy = requestedBy;
        }
    }

    public class CodeReviewCompletedEvent : DomainEvent
    {
        public Guid CodeReviewId { get; private set; }
        public int TotalIssues { get; private set; }
        public int CriticalIssues { get; private set; }
        public ReviewQuality Quality { get; private set; }
        public ReviewRisk Risk { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }

        public CodeReviewCompletedEvent(
            Guid codeReviewId,
            int totalIssues,
            int criticalIssues,
            ReviewQuality quality,
            ReviewRisk risk,
            TimeSpan processingTime)
        {
            CodeReviewId = codeReviewId;
            TotalIssues = totalIssues;
            CriticalIssues = criticalIssues;
            Quality = quality;
            Risk = risk;
            ProcessingTime = processingTime;
        }
    }

    public class GitHubPullRequestReceivedEvent : DomainEvent
    {
        public Guid PullRequestId { get; private set; }
        public int GitHubPullRequestId { get; private set; }
        public string RepositoryName { get; private set; }
        public string RepositoryOwner { get; private set; }
        public string SourceBranch { get; private set; }
        public string TargetBranch { get; private set; }
        public string Author { get; private set; }
        public string Action { get; private set; }

        public GitHubPullRequestReceivedEvent(
            Guid pullRequestId,
            int gitHubPullRequestId,
            string repositoryName,
            string repositoryOwner,
            string sourceBranch,
            string targetBranch,
            string author,
            string action)
        {
            PullRequestId = pullRequestId;
            GitHubPullRequestId = gitHubPullRequestId;
            RepositoryName = repositoryName;
            RepositoryOwner = repositoryOwner;
            SourceBranch = sourceBranch;
            TargetBranch = targetBranch;
            Author = author;
            Action = action;
        }
    }

    public class AIAnalysisStartedEvent : DomainEvent
    {
        public Guid AnalysisId { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string ModelName { get; private set; }
        public string AnalysisType { get; private set; }

        public AIAnalysisStartedEvent(
            Guid analysisId,
            Guid codeReviewId,
            string modelName,
            string analysisType)
        {
            AnalysisId = analysisId;
            CodeReviewId = codeReviewId;
            ModelName = modelName;
            AnalysisType = analysisType;
        }
    }

    public class AIAnalysisCompletedEvent : DomainEvent
    {
        public Guid AnalysisId { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string ModelName { get; private set; }
        public string AnalysisType { get; private set; }
        public int InsightsCount { get; private set; }
        public decimal Cost { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }

        public AIAnalysisCompletedEvent(
            Guid analysisId,
            Guid codeReviewId,
            string modelName,
            string analysisType,
            int insightsCount,
            decimal cost,
            TimeSpan processingTime)
        {
            AnalysisId = analysisId;
            CodeReviewId = codeReviewId;
            ModelName = modelName;
            AnalysisType = analysisType;
            InsightsCount = insightsCount;
            Cost = cost;
            ProcessingTime = processingTime;
        }
    }

    public class SecurityIssueDetectedEvent : DomainEvent
    {
        public Guid CodeReviewId { get; private set; }
        public Guid IssueId { get; private set; }
        public string Category { get; private set; }
        public Severity Severity { get; private set; }
        public string Description { get; private set; }
        public string FilePath { get; private set; }
        public int LineNumber { get; private set; }

        public SecurityIssueDetectedEvent(
            Guid codeReviewId,
            Guid issueId,
            string category,
            Severity severity,
            string description,
            string filePath,
            int lineNumber)
        {
            CodeReviewId = codeReviewId;
            IssueId = issueId;
            Category = category;
            Severity = severity;
            Description = description;
            FilePath = filePath;
            LineNumber = lineNumber;
        }
    }

    public class UserRegisteredEvent : DomainEvent
    {
        public Guid UserId { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public UserRole Role { get; private set; }

        public UserRegisteredEvent(
            Guid userId,
            string username,
            string email,
            UserRole role)
        {
            UserId = userId;
            Username = username;
            Email = email;
            Role = role;
        }
    }
}
