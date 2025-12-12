using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class CodeReview : AuditableEntity
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string RepositoryUrl { get; private set; }
        public string BranchName { get; private set; }
        public string CommitHash { get; private set; }
        public ReviewStatus Status { get; private set; }
        public Priority Priority { get; private set; }
        public string RequestedBy { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public string Summary { get; private set; }
        public int TotalIssues { get; private set; }
        public int CriticalIssues { get; private set; }
        public int MajorIssues { get; private set; }
        public int MinorIssues { get; private set; }

        private readonly List<ReviewIssue> _issues = new();
        public IReadOnlyCollection<ReviewIssue> Issues => _issues.AsReadOnly();

        private readonly List<ReviewComment> _comments = new();
        public IReadOnlyCollection<ReviewComment> Comments => _comments.AsReadOnly();

        private readonly List<ReviewFile> _files = new();
        public IReadOnlyCollection<ReviewFile> Files => _files.AsReadOnly();

        protected CodeReview()
        {
        }

        public CodeReview(
            string title,
            string description,
            string repositoryUrl,
            string branchName,
            string commitHash,
            string requestedBy,
            Priority priority = Priority.Medium)
        {
            Id = Guid.NewGuid();
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            RepositoryUrl = repositoryUrl ?? throw new ArgumentNullException(nameof(repositoryUrl));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));
            CommitHash = commitHash ?? throw new ArgumentNullException(nameof(commitHash));
            RequestedBy = requestedBy ?? throw new ArgumentNullException(nameof(requestedBy));
            Priority = priority;
            Status = ReviewStatus.Pending;
            Created = DateTime.UtcNow;
            CreatedBy = requestedBy;
        }

        public void StartReview()
        {
            if (Status != ReviewStatus.Pending)
                throw new InvalidOperationException("Review can only be started from pending status");

            Status = ReviewStatus.InProgress;
            LastModified = DateTime.UtcNow;
        }

        public void CompleteReview(string summary, List<ReviewIssue> issues)
        {
            if (Status != ReviewStatus.InProgress)
                throw new InvalidOperationException("Review can only be completed from in-progress status");

            Status = ReviewStatus.Completed;
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            CompletedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;

            _issues.Clear();
            _issues.AddRange(issues ?? new List<ReviewIssue>());

            CalculateIssueCounts();
        }

        public void FailReview(string reason)
        {
            if (Status != ReviewStatus.InProgress)
                throw new InvalidOperationException("Review can only be failed from in-progress status");

            Status = ReviewStatus.Failed;
            Summary = reason ?? throw new ArgumentNullException(nameof(reason));
            CompletedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        public void AddComment(string content, string author)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Comment content cannot be empty", nameof(content));

            var comment = new ReviewComment(Id, content, author);
            _comments.Add(comment);
            LastModified = DateTime.UtcNow;
        }

        public void AddFile(string filePath, string content, string fileType)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            var file = new ReviewFile(Id, filePath, content, fileType);
            _files.Add(file);
            LastModified = DateTime.UtcNow;
        }

        private void CalculateIssueCounts()
        {
            TotalIssues = _issues.Count;
            CriticalIssues = _issues.Count(i => i.Severity == Severity.Critical);
            MajorIssues = _issues.Count(i => i.Severity == Severity.Major);
            MinorIssues = _issues.Count(i => i.Severity == Severity.Minor);
        }
    }
}
