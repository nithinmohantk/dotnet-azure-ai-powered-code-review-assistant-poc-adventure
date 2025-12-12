using System;
using System.Collections.Generic;

namespace CodeReviewAssistant.Core.Domain.ValueObjects
{
    public abstract class ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(1, (current, obj) => current * 23 + (obj?.GetHashCode() ?? 0));
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (left is null && right is null)
                return true;

            if (left is null || right is null)
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }
    }
    public class ReviewMetrics : ValueObject
    {
        public int TotalFiles { get; private set; }
        public int AddedLines { get; private set; }
        public int DeletedLines { get; private set; }
        public int ModifiedLines { get; private set; }
        public int TotalLines { get; private set; }
        public int TotalIssues { get; private set; }
        public int CriticalIssues { get; private set; }
        public int MajorIssues { get; private set; }
        public int MinorIssues { get; private set; }
        public int InfoIssues { get; private set; }
        public double ComplexityScore { get; private set; }
        public double MaintainabilityIndex { get; private set; }
        public int TestCoverage { get; private set; }
        public TimeSpan AnalysisDuration { get; private set; }
        public decimal AnalysisCost { get; private set; }
        public DateTime CalculatedAt { get; private set; }

        protected ReviewMetrics()
        {
        }

        public ReviewMetrics(
            int totalFiles,
            int addedLines,
            int deletedLines,
            int modifiedLines,
            int totalIssues,
            int criticalIssues,
            int majorIssues,
            int minorIssues,
            int infoIssues,
            double complexityScore,
            double maintainabilityIndex,
            int testCoverage,
            TimeSpan analysisDuration,
            decimal analysisCost)
        {
            TotalFiles = totalFiles;
            AddedLines = addedLines;
            DeletedLines = deletedLines;
            ModifiedLines = modifiedLines;
            TotalLines = addedLines + deletedLines + modifiedLines;
            TotalIssues = totalIssues;
            CriticalIssues = criticalIssues;
            MajorIssues = majorIssues;
            MinorIssues = minorIssues;
            InfoIssues = infoIssues;
            ComplexityScore = complexityScore;
            MaintainabilityIndex = maintainabilityIndex;
            TestCoverage = testCoverage;
            AnalysisDuration = analysisDuration;
            AnalysisCost = analysisCost;
            CalculatedAt = DateTime.UtcNow;
        }

        public double GetIssueDensity()
        {
            return TotalLines > 0 ? (double)TotalIssues / TotalLines * 1000 : 0;
        }

        public double GetCriticalIssueRatio()
        {
            return TotalIssues > 0 ? (double)CriticalIssues / TotalIssues * 100 : 0;
        }

        public ReviewQuality GetQualityScore()
        {
            var score = 100.0;

            // Deduct points for issues
            score -= CriticalIssues * 10;
            score -= MajorIssues * 5;
            score -= MinorIssues * 2;
            score -= InfoIssues * 0.5;

            // Add points for good metrics
            score += MaintainabilityIndex * 0.1;
            score += TestCoverage * 0.05;

            // Deduct for high complexity
            if (ComplexityScore > 10)
                score -= (ComplexityScore - 10) * 2;

            score = Math.Max(0, Math.Min(100, score));

            return score switch
            {
                >= 90 => ReviewQuality.Excellent,
                >= 80 => ReviewQuality.Good,
                >= 70 => ReviewQuality.Fair,
                >= 60 => ReviewQuality.Poor,
                _ => ReviewQuality.VeryPoor
            };
        }

        public ReviewRisk GetRiskLevel()
        {
            if (CriticalIssues > 5 || ComplexityScore > 20)
                return ReviewRisk.High;

            if (CriticalIssues > 2 || MajorIssues > 10 || ComplexityScore > 15)
                return ReviewRisk.Medium;

            if (CriticalIssues > 0 || MajorIssues > 5 || ComplexityScore > 10)
                return ReviewRisk.Low;

            return ReviewRisk.Minimal;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return TotalFiles;
            yield return AddedLines;
            yield return DeletedLines;
            yield return ModifiedLines;
            yield return TotalLines;
            yield return TotalIssues;
            yield return CriticalIssues;
            yield return MajorIssues;
            yield return MinorIssues;
            yield return InfoIssues;
            yield return ComplexityScore;
            yield return MaintainabilityIndex;
            yield return TestCoverage;
            yield return AnalysisDuration;
            yield return AnalysisCost;
        }
    }

    public enum ReviewQuality
    {
        VeryPoor,
        Poor,
        Fair,
        Good,
        Excellent
    }

    public enum ReviewRisk
    {
        Minimal,
        Low,
        Medium,
        High,
        Critical
    }
}
