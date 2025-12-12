using MediatR;

namespace CodeReviewAssistant.Core.Application.Commands
{
    public record StartAIAnalysisCommand : IRequest<StartAIAnalysisResult>
    {
        public Guid CodeReviewId { get; init; }
        public string AnalysisType { get; init; }
        public string ModelName { get; init; }
        public string ModelVersion { get; init; }
        public Dictionary<string, object> AnalysisParameters { get; init; }

        public StartAIAnalysisCommand(
            Guid codeReviewId,
            string analysisType,
            string modelName,
            string modelVersion,
            Dictionary<string, object> analysisParameters = null)
        {
            CodeReviewId = codeReviewId;
            AnalysisType = analysisType ?? throw new ArgumentNullException(nameof(analysisType));
            ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
            ModelVersion = modelVersion ?? throw new ArgumentNullException(nameof(modelVersion));
            AnalysisParameters = analysisParameters ?? new Dictionary<string, object>();
        }
    }

    public record StartAIAnalysisResult
    {
        public bool Success { get; init; }
        public string Message { get; init; }
        public Guid? AnalysisId { get; init; }
        public DateTime StartedAt { get; init; }

        public static StartAIAnalysisResult SuccessResult(Guid analysisId)
        {
            return new StartAIAnalysisResult
            {
                Success = true,
                AnalysisId = analysisId,
                StartedAt = DateTime.UtcNow,
                Message = "AI analysis started successfully"
            };
        }

        public static StartAIAnalysisResult FailureResult(string message)
        {
            return new StartAIAnalysisResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
