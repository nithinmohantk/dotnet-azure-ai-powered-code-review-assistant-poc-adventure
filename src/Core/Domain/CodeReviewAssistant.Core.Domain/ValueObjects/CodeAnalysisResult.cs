using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeReviewAssistant.Core.Domain.ValueObjects
{
    public class CodeAnalysisResult
    {
        public string Summary { get; private set; }
        public int TotalIssues { get; private set; }
        public int CriticalIssues { get; private set; }
        public int MajorIssues { get; private set; }
        public int MinorIssues { get; private set; }
        public int InfoIssues { get; private set; }
        public double Score { get; private set; }
        public List<AnalysisIssue> Issues { get; private set; }
        public List<string> Recommendations { get; private set; }
        public Dictionary<string, int> IssueDistribution { get; private set; }
        public DateTime AnalyzedAt { get; private set; }

        public CodeAnalysisResult(
            string summary,
            List<AnalysisIssue> issues,
            List<string> recommendations = null)
        {
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            Issues = issues ?? throw new ArgumentNullException(nameof(issues));
            Recommendations = recommendations ?? new List<string>();
            AnalyzedAt = DateTime.UtcNow;

            CalculateMetrics();
            CalculateScore();
            CalculateDistribution();
        }

        private void CalculateMetrics()
        {
            TotalIssues = Issues.Count;
            CriticalIssues = Issues.Count(i => i.Severity == Severity.Critical);
            MajorIssues = Issues.Count(i => i.Severity == Severity.Major);
            MinorIssues = Issues.Count(i => i.Severity == Severity.Minor);
            InfoIssues = Issues.Count(i => i.Severity == Severity.Info);
        }

        private void CalculateScore()
        {
            // Score calculation: 100 - (weighted penalty based on severity)
            double penalty = 0;
            penalty += CriticalIssues * 20;  // 20 points per critical issue
            penalty += MajorIssues * 10;     // 10 points per major issue
            penalty += MinorIssues * 5;      // 5 points per minor issue
            penalty += InfoIssues * 1;       // 1 point per info issue

            Score = Math.Max(0, 100 - penalty);
        }

        private void CalculateDistribution()
        {
            IssueDistribution = Issues
                .GroupBy(i => i.Category)
                .ToDictionary(g => g.Key.ToString(), g => g.Count());
        }

        public static CodeAnalysisResult CreateEmpty(string repositoryName)
        {
            return new CodeAnalysisResult(
                $"No issues found in {repositoryName}",
                new List<AnalysisIssue>(),
                new List<string> { "Repository appears to be in good condition!" }
            );
        }
    }

    public class AnalysisIssue
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string FilePath { get; private set; }
        public int? LineNumber { get; private set; }
        public int? EndLineNumber { get; private set; }
        public Severity Severity { get; private set; }
        public IssueCategory Category { get; private set; }
        public string RuleId { get; private set; }
        public string Suggestion { get; private set; }
        public string CodeSnippet { get; private set; }

        public AnalysisIssue(
            string title,
            string description,
            string filePath,
            Severity severity,
            IssueCategory category,
            string ruleId = null,
            int? lineNumber = null,
            int? endLineNumber = null,
            string suggestion = null,
            string codeSnippet = null)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Severity = severity;
            Category = category;
            RuleId = ruleId;
            LineNumber = lineNumber;
            EndLineNumber = endLineNumber;
            Suggestion = suggestion;
            CodeSnippet = codeSnippet;
        }
    }
}
