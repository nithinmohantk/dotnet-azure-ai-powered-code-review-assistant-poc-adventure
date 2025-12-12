using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Core.Application.DTOs
{
    public class CodeReviewDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RepositoryUrl { get; set; }
        public string BranchName { get; set; }
        public string CommitHash { get; set; }
        public ReviewStatus Status { get; set; }
        public Priority Priority { get; set; }
        public string RequestedBy { get; set; }
        public DateTime Created { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Summary { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int MajorIssues { get; set; }
        public int MinorIssues { get; set; }
        public List<ReviewIssueDto> Issues { get; set; }
        public List<ReviewCommentDto> Comments { get; set; }
        public List<ReviewFileDto> Files { get; set; }
        public bool Success { get; set; }
        public Guid CodeReviewId { get; set; }
    }

    public class ReviewIssueDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FilePath { get; set; }
        public int? LineNumber { get; set; }
        public int? EndLineNumber { get; set; }
        public Severity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string RuleId { get; set; }
        public string Suggestion { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string ResolvedBy { get; set; }
        public string ResolutionNote { get; set; }
        public DateTime Created { get; set; }
    }

    public class ReviewCommentDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public Guid? ParentCommentId { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int Upvotes { get; set; }
        public int Downvotes { get; set; }
        public DateTime Created { get; set; }
    }

    public class ReviewFileDto
    {
        public Guid Id { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long SizeInBytes { get; set; }
        public int LinesOfCode { get; set; }
        public string Language { get; set; }
        public bool IsBinary { get; set; }
        public string FileHash { get; set; }
        public DateTime Created { get; set; }
    }

    public class CreateCodeReviewRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string RepositoryUrl { get; set; }
        public string BranchName { get; set; }
        public string CommitHash { get; set; }
        public Priority Priority { get; set; }
        public List<string> FilePaths { get; set; }
    }

    public class UpdateCodeReviewRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Priority Priority { get; set; }
    }

    public class AddCommentRequest
    {
        public string Content { get; set; }
        public Guid? ParentCommentId { get; set; }
    }

    public class ResolveIssueRequest
    {
        public string ResolutionNote { get; set; }
    }

    public class CodeReviewSummaryDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string RepositoryUrl { get; set; }
        public ReviewStatus Status { get; set; }
        public Priority Priority { get; set; }
        public string RequestedBy { get; set; }
        public DateTime Created { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int MajorIssues { get; set; }
        public int MinorIssues { get; set; }
    }

    public class CodeReviewListResponse
    {
        public List<CodeReviewSummaryDto> CodeReviews { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class CodeReviewStatisticsDto
    {
        public int TotalReviews { get; set; }
        public int PendingReviews { get; set; }
        public int InProgressReviews { get; set; }
        public int CompletedReviews { get; set; }
        public int FailedReviews { get; set; }
        public double AverageCompletionTimeHours { get; set; }
        public int TotalIssuesFound { get; set; }
        public int CriticalIssuesFound { get; set; }
        public Dictionary<string, int> ReviewsByRepository { get; set; }
        public Dictionary<ReviewStatus, int> ReviewsByStatus { get; set; }
        public Dictionary<Priority, int> ReviewsByPriority { get; set; }
        public Dictionary<IssueCategory, int> IssuesByCategory { get; set; }
    }

    public class CreateCodeReviewResult
    {
        public bool Success { get; set; }
        public Guid CodeReviewId { get; set; }
        public string Message { get; set; }
        public CodeReviewDto CodeReview { get; set; }
    }
}
