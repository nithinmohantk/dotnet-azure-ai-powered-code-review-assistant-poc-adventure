using System;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class ReviewIssue : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string FilePath { get; private set; }
        public int? LineNumber { get; private set; }
        public int? EndLineNumber { get; private set; }
        public Severity Severity { get; private set; }
        public IssueCategory Category { get; private set; }
        public string RuleId { get; private set; }
        public string Suggestion { get; private set; }
        public bool IsResolved { get; private set; }
        public DateTime? ResolvedAt { get; private set; }
        public string ResolvedBy { get; private set; }
        public string ResolutionNote { get; private set; }

        protected ReviewIssue()
        {
        }

        public ReviewIssue(
            Guid codeReviewId,
            string title,
            string description,
            string filePath,
            Severity severity,
            IssueCategory category,
            string ruleId = null,
            int? lineNumber = null,
            int? endLineNumber = null,
            string suggestion = null)
        {
            Id = Guid.NewGuid();
            CodeReviewId = codeReviewId;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Severity = severity;
            Category = category;
            RuleId = ruleId;
            LineNumber = lineNumber;
            EndLineNumber = endLineNumber;
            Suggestion = suggestion;
            IsResolved = false;
            Created = DateTime.UtcNow;
            CreatedBy = "system";
        }

        public void Resolve(string resolvedBy, string resolutionNote = null)
        {
            if (IsResolved)
                throw new InvalidOperationException("Issue is already resolved");

            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
            ResolvedBy = resolvedBy ?? throw new ArgumentNullException(nameof(resolvedBy));
            ResolutionNote = resolutionNote;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = resolvedBy;
        }

        public void Reopen()
        {
            if (!IsResolved)
                throw new InvalidOperationException("Issue is not resolved");

            IsResolved = false;
            ResolvedAt = null;
            ResolvedBy = null;
            ResolutionNote = null;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = "system";
        }

        public void UpdateSeverity(Severity newSeverity)
        {
            if (Severity != newSeverity)
            {
                Severity = newSeverity;
                LastModified = DateTime.UtcNow;
                LastModifiedBy = "system";
            }
        }

        public void UpdateSuggestion(string suggestion)
        {
            Suggestion = suggestion;
            LastModified = DateTime.UtcNow;
            LastModifiedBy = "system";
        }
    }

    public enum Severity
    {
        Info,
        Minor,
        Major,
        Critical,
        Blocker
    }

    public enum IssueCategory
    {
        Security,
        Performance,
        CodeQuality,
        Maintainability,
        Reliability,
        Documentation,
        BestPractices,
        Style,
        ErrorHandling,
        Testing,
        Architecture,
        Other
    }
}
