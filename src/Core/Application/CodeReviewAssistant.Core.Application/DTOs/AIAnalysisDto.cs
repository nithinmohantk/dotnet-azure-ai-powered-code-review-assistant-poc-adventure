using System;

namespace CodeReviewAssistant.Core.Application.DTOs
{
    public class AIAnalysisDto
    {
        public Guid Id { get; set; }
        public Guid CodeReviewId { get; set; }
        public string ModelName { get; set; }
        public string ModelVersion { get; set; }
        public string AnalysisType { get; set; }
        public string Prompt { get; set; }
        public string Response { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
        public decimal Cost { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public AnalysisStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Metadata { get; set; }
        public List<AIInsightDto> Insights { get; set; } = new();
    }

    public class AIInsightDto
    {
        public Guid Id { get; set; }
        public Guid AIAnalysisId { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public Severity Severity { get; set; }
        public int LineNumber { get; set; }
        public string FilePath { get; set; }
        public string CodeSnippet { get; set; }
        public ConfidenceLevel Confidence { get; set; }
        public string Metadata { get; set; }
        public bool IsActionable { get; set; }
        public bool IsAutomaticallyFixable;
