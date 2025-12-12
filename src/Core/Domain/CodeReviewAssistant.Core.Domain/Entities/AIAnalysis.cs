using System;
using System.Collections.Generic;
using CodeReviewAssistant.Core.Domain.Common;
using CodeReviewAssistant.Core.Domain.ValueObjects;

namespace CodeReviewAssistant.Core.Domain.Entities
{
    public class AIAnalysis : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid CodeReviewId { get; private set; }
        public string ModelName { get; private set; }
        public string ModelVersion { get; private set; }
        public string AnalysisType { get; private set; }
        public string Prompt { get; private set; }
        public string Response { get; private set; }
        public int TokenCount { get; private set; }
        public int InputTokenCount { get; private set; }
        public int OutputTokenCount { get; private set; }
        public decimal Cost { get; private set; }
        public TimeSpan ProcessingTime { get; private set; }
        public AnalysisStatus Status { get; private set; }
        public string ErrorMessage { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public string Metadata { get; private set; }

        private readonly List<AIInsight> _insights = new();
        public IReadOnlyCollection<AIInsight> Insights => _insights.AsReadOnly();

        protected AIAnalysis()
        {
        }

        public AIAnalysis(
            Guid codeReviewId,
            string modelName,
            string modelVersion,
            string analysisType,
            string prompt)
        {
            Id = Guid.NewGuid();
            CodeReviewId = codeReviewId;
            ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
            ModelVersion = modelVersion ?? throw new ArgumentNullException(nameof(modelVersion));
            AnalysisType = analysisType ?? throw new ArgumentNullException(nameof(analysisType));
            Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
            Status = AnalysisStatus.Pending;
            StartedAt = DateTime.UtcNow;
            TokenCount = 0;
            InputTokenCount = 0;
            OutputTokenCount = 0;
            Cost = 0;
            ProcessingTime = TimeSpan.Zero;
        }

        public void StartAnalysis()
        {
            Status = AnalysisStatus.Processing;
            StartedAt = DateTime.UtcNow;
        }

        public void CompleteAnalysis(
            string response,
            int inputTokenCount,
            int outputTokenCount,
            decimal cost,
            TimeSpan processingTime,
            string metadata = null)
        {
            Response = response ?? throw new ArgumentNullException(nameof(response));
            InputTokenCount = inputTokenCount;
            OutputTokenCount = outputTokenCount;
            TokenCount = inputTokenCount + outputTokenCount;
            Cost = cost;
            ProcessingTime = processingTime;
            Metadata = metadata ?? string.Empty;
            Status = AnalysisStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void FailAnalysis(string errorMessage)
        {
            Status = AnalysisStatus.Failed;
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            CompletedAt = DateTime.UtcNow;
        }

        public void AddInsight(AIInsight insight)
        {
            if (insight == null) throw new ArgumentNullException(nameof(insight));
            _insights.Add(insight);
        }

        public void AddInsights(IEnumerable<AIInsight> insights)
        {
            if (insights == null) throw new ArgumentNullException(nameof(insights));
            foreach (var insight in insights)
            {
                _insights.Add(insight);
            }
        }
    }

    public class AIInsight : AuditableEntity
    {
        public Guid Id { get; private set; }
        public Guid AIAnalysisId { get; private set; }
        public string Category { get; private set; }
        public string Type { get; private set; }
        public string Description { get; private set; }
        public string Recommendation { get; private set; }
        public Severity Severity { get; private set; }
        public int LineNumber { get; private set; }
        public string FilePath { get; private set; }
        public string CodeSnippet { get; private set; }
        public ConfidenceLevel Confidence { get; private set; }
        public string Metadata { get; private set; }
        public bool IsActionable { get; private set; }
        public bool IsAutomaticallyFixable { get; private set; }

        protected AIInsight()
        {
        }

        public AIInsight(
            Guid aiAnalysisId,
            string category,
            string type,
            string description,
            Severity severity,
            ConfidenceLevel confidence)
        {
            Id = Guid.NewGuid();
            AIAnalysisId = aiAnalysisId;
            Category = category ?? throw new ArgumentNullException(nameof(category));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Severity = severity;
            Confidence = confidence;
            IsActionable = true;
            IsAutomaticallyFixable = false;
        }

        public void SetLocation(int lineNumber, string filePath, string codeSnippet = null)
        {
            LineNumber = lineNumber;
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            CodeSnippet = codeSnippet ?? string.Empty;
        }

        public void SetRecommendation(string recommendation)
        {
            Recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        }

        public void SetMetadata(string metadata)
        {
            Metadata = metadata ?? string.Empty;
        }

        public void SetActionability(bool isActionable, bool isAutomaticallyFixable = false)
        {
            IsActionable = isActionable;
            IsAutomaticallyFixable = isAutomaticallyFixable;
        }
    }

    public enum AnalysisStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled
    }

    public enum ConfidenceLevel
    {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
}
