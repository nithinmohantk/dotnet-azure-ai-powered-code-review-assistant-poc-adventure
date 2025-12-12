using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Core.Application.DTOs
{
    public class CodeReviewMetricsDto
    {
        public Guid Id { get; set; }
        public Guid CodeReviewId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int TotalFiles { get; set; }
        public int TotalLinesOfCode { get; set; }
        public int TotalIssues { get; set; }
        public int CriticalIssues { get; set; }
        public int MajorIssues { get; set; }
        public int MinorIssues { get; set; }
        public ReviewQuality Quality { get; set; }
        public ReviewRisk Risk { get; set; }
        public Dictionary<IssueCategory, int> IssuesByCategory { get; set; }
        public Dictionary<Severity, int> IssuesBySeverity { get; set; }
        public double ComplexityScore { get; set; }
        public double MaintainabilityScore { get; set; }
        public List<FileMetricsDto> FileMetrics { get; set; }
    }

    public class FileMetricsDto
    {
        public string FilePath { get; set; }
        public int LinesOfCode { get; set; }
        public int Complexity { get; set; }
        public int Issues { get; set; }
        public double MaintainabilityIndex { get; set; }
        public List<IssueDto> FileIssues { get; set; }
    }

    public class IssueDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Severity Severity { get; set; }
        public IssueCategory Category { get; set; }
        public string FilePath { get; set; }
        public int? LineNumber { get; set; }
        public string RuleId { get; set; }
        public string Suggestion { get; set; }
        public bool IsResolved { get; set; }
    }
}
