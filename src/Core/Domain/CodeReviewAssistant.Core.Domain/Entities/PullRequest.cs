using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class PullRequest : AuditableEntity
    {
        public Guid Id { get; private set; }
        public int GitHubPullRequestId { get; private set; }
        public string RepositoryName { get; private set; }
        public string RepositoryOwner { get; private set; }
        public string SourceBranch { get; private set; }
        public string TargetBranch { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public string Action { get; private set; }
        public string Status { get; private set; }
        public string HeadSha { get; private set; }
        public string BaseSha { get; private set; }
        public bool IsMerged { get; private set; }
        public DateTime? MergedAt { get; private set; }
        public string MergedBy { get; private set; }
        public int Additions { get; private set; }
        public int Deletions { get; private set; }
        public int ChangedFiles { get; private set; }

        private readonly List<PullRequestFile> _files = new();
        public IReadOnlyCollection<PullRequestFile> Files => _files.AsReadOnly();

        private readonly List<PullRequestComment> _comments = new();
        public IReadOnlyCollection<PullRequestComment> Comments => _comments.AsReadOnly();

        public PullRequest(
            int gitHubPullRequestId,
            string repositoryName,
            string repositoryOwner,
            string sourceBranch,
            string targetBranch,
            string title,
            string description,
            string author,
            string action,
            string headSha,
            string baseSha,
            int additions = 0,
            int deletions = 0,
            int changedFiles = 0)
        {
            Id = Guid.NewGuid();
            GitHubPullRequestId = gitHubPullRequestId;
            RepositoryName = repositoryName ?? throw new ArgumentNullException(nameof(repositoryName));
            RepositoryOwner = repositoryOwner ?? throw new ArgumentNullException(nameof(repositoryOwner));
            SourceBranch = sourceBranch ?? throw new ArgumentNullException(nameof(sourceBranch));
            TargetBranch = targetBranch ?? throw new ArgumentNullException(nameof(targetBranch));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? string.Empty;
            Author = author ?? throw new ArgumentNullException(nameof(author));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Status = "open";
            HeadSha = headSha ?? throw new ArgumentNullException(nameof(headSha));
            BaseSha = baseSha ?? throw new ArgumentNullException(nameof(baseSha));
            IsMerged = false;
            Additions = additions;
            Deletions = deletions;
            ChangedFiles = changedFiles;
            Created = DateTime.UtcNow;
        }

        public void UpdateStatus(string status)
        {
            Status = status ?? throw new ArgumentNullException(nameof(status));
            LastModified = DateTime.UtcNow;
        }

        public void MarkAsMerged(string mergedBy)
        {
            IsMerged = true;
            MergedAt = DateTime.UtcNow;
            MergedBy = mergedBy ?? throw new ArgumentNullException(nameof(mergedBy));
            Status = "merged";
            LastModified = DateTime.UtcNow;
        }

        public void AddFile(PullRequestFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            _files.Add(file);
            LastModified = DateTime.UtcNow;
        }

        public void AddComment(PullRequestComment comment)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            _comments.Add(comment);
            LastModified = DateTime.UtcNow;
        }
    }

    public class PullRequestFile : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid PullRequestId { get; private set; }
        public string FilePath { get; private set; }
        public string Sha { get; private set; }
        public string Status { get; private set; }
        public int Additions { get; private set; }
        public int Deletions { get; private set; }
        public int Changes { get; private set; }
        public string Patch { get; private set; }
        public string BlobUrl { get; private set; }
        public string RawUrl { get; private set; }

        public PullRequestFile(
            Guid pullRequestId,
            string filePath,
            string sha,
            string status,
            int additions = 0,
            int deletions = 0,
            int changes = 0,
            string patch = null,
            string blobUrl = null,
            string rawUrl = null)
        {
            Id = Guid.NewGuid();
            PullRequestId = pullRequestId;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Sha = sha ?? throw new ArgumentNullException(nameof(sha));
            Status = status ?? throw new ArgumentNullException(nameof(status));
            Additions = additions;
            Deletions = deletions;
            Changes = changes;
            Patch = patch ?? string.Empty;
            BlobUrl = blobUrl ?? string.Empty;
            RawUrl = rawUrl ?? string.Empty;
            Created = DateTime.UtcNow;
        }
    }

    public class PullRequestComment : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid PullRequestId { get; private set; }
        public string Content { get; private set; }
        public string Author { get; private set; }
        public Guid? ParentCommentId { get; private set; }
        public bool IsEdited { get; private set; }
        public DateTime? EditedAt { get; private set; }
        public int Upvotes { get; private set; }
        public int Downvotes { get; private set; }
        public string CommentType { get; private set; }

        public PullRequestComment(
            Guid pullRequestId,
            string content,
            string author,
            Guid? parentCommentId = null,
            string commentType = "general")
        {
            Id = Guid.NewGuid();
            PullRequestId = pullRequestId;
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Author = author ?? throw new ArgumentNullException(nameof(author));
            ParentCommentId = parentCommentId;
            IsEdited = false;
            Upvotes = 0;
            Downvotes = 0;
            CommentType = commentType ?? throw new ArgumentNullException(nameof(commentType));
            Created = DateTime.UtcNow;
        }

        public void EditContent(string newContent)
        {
            Content = newContent ?? throw new ArgumentNullException(nameof(newContent));
            IsEdited = true;
            EditedAt = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
        }

        public void Upvote()
        {
            Upvotes++;
            LastModified = DateTime.UtcNow;
        }

        public void Downvote()
        {
            Downvotes++;
            LastModified = DateTime.UtcNow;
        }
    }
}
