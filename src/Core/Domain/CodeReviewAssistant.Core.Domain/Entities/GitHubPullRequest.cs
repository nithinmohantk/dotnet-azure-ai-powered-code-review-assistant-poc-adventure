using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class GitHubPullRequest : AuditableEntity
    {
        public Guid Id { get; private set; }
        public int PullRequestId { get; private set; }
        public string RepositoryName { get; private set; }
        public string RepositoryOwner { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string SourceBranch { get; private set; }
        public string TargetBranch { get; private set; }
        public string HeadCommitSha { get; private set; }
        public string BaseCommitSha { get; private set; }
        public string Author { get; private set; }
        public string AuthorEmail { get; private set; }
        public PullRequestStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }
        public DateTime? MergedAt { get; private set; }
        public bool IsDraft { get; private set; }
        public bool IsMerged { get; private set; }
        public bool Mergeable { get; private set; }
        public string MergeCommitSha { get; private set; }
        public int AddedLines { get; private set; }
        public int DeletedLines { get; private set; }
        public int ChangedFiles { get; private set; }
        public string WebhookPayload { get; private set; }

        private readonly List<ReviewFile> _files = new();
        public IReadOnlyCollection<ReviewFile> Files => _files.AsReadOnly();

        private readonly List<ReviewComment> _comments = new();
        public IReadOnlyCollection<ReviewComment> Comments => _comments.AsReadOnly();

        protected GitHubPullRequest()
        {
        }

        public GitHubPullRequest(
            int pullRequestId,
            string repositoryName,
            string repositoryOwner,
            string title,
            string description,
            string sourceBranch,
            string targetBranch,
            string headCommitSha,
            string baseCommitSha,
            string author,
            string authorEmail,
            string webhookPayload)
        {
            Id = Guid.NewGuid();
            PullRequestId = pullRequestId;
            RepositoryName = repositoryName ?? throw new ArgumentNullException(nameof(repositoryName));
            RepositoryOwner = repositoryOwner ?? throw new ArgumentNullException(nameof(repositoryOwner));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? string.Empty;
            SourceBranch = sourceBranch ?? throw new ArgumentNullException(nameof(sourceBranch));
            TargetBranch = targetBranch ?? throw new ArgumentNullException(nameof(targetBranch));
            HeadCommitSha = headCommitSha ?? throw new ArgumentNullException(nameof(headCommitSha));
            BaseCommitSha = baseCommitSha ?? throw new ArgumentNullException(nameof(baseCommitSha));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            AuthorEmail = authorEmail ?? throw new ArgumentNullException(nameof(authorEmail));
            Status = PullRequestStatus.Open;
            CreatedAt = DateTime.UtcNow;
            IsDraft = false;
            IsMerged = false;
            Mergeable = true;
            WebhookPayload = webhookPayload ?? throw new ArgumentNullException(nameof(webhookPayload));
            AddedLines = 0;
            DeletedLines = 0;
            ChangedFiles = 0;
        }

        public void UpdateStatus(PullRequestStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsDraft()
        {
            IsDraft = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsReadyForReview()
        {
            IsDraft = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void MarkAsMerged(string mergeCommitSha)
        {
            IsMerged = true;
            Status = PullRequestStatus.Merged;
            MergedAt = DateTime.UtcNow;
            MergeCommitSha = mergeCommitSha;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Close()
        {
            Status = PullRequestStatus.Closed;
            ClosedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateMergeability(bool mergeable)
        {
            Mergeable = mergeable;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatistics(int addedLines, int deletedLines, int changedFiles)
        {
            AddedLines = addedLines;
            DeletedLines = deletedLines;
            ChangedFiles = changedFiles;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddFile(ReviewFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            _files.Add(file);
        }

        public void AddComment(ReviewComment comment)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            _comments.Add(comment);
        }
    }

    public enum PullRequestStatus
    {
        Open,
        Closed,
        Merged,
        Draft,
        InReview
    }
}
