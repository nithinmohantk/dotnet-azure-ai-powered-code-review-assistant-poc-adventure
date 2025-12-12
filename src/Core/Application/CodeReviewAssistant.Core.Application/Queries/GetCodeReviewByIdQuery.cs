using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CodeReviewAssistant.Core.Domain.Entities;
using CodeReviewAssistant.Core.Domain.Events;
using CodeReviewAssistant.Core.Application.DTOs;
using CodeReviewAssistant.Core.Application.Interfaces;

namespace CodeReviewAssistant.Core.Application.Queries
{
    public record GetCodeReviewByIdQuery : IRequest<CodeReviewDto>
    {
        public Guid Id { get; init; }

        public GetCodeReviewByIdQuery(Guid id)
        {
            Id = id;
        }
    }

    public record GetCodeReviewsByUserQuery : IRequest<List<CodeReviewDto>>
    {
        public string Username { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public ReviewStatus? Status { get; init; }

        public GetCodeReviewsByUserQuery(string username, int page = 1, int pageSize = 20, ReviewStatus? status = null)
        {
            Username = username ?? throw new ArgumentNullException(nameof(username));
            Page = page;
            PageSize = pageSize;
            Status = status;
        }
    }

    public record GetCodeReviewsByRepositoryQuery : IRequest<List<CodeReviewDto>>
    {
        public string RepositoryUrl { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public ReviewStatus? Status { get; init; }

        public GetCodeReviewsByRepositoryQuery(string repositoryUrl, int page = 1, int pageSize = 20, ReviewStatus? status = null)
        {
            RepositoryUrl = repositoryUrl ?? throw new ArgumentNullException(nameof(repositoryUrl));
            Page = page;
            PageSize = pageSize;
            Status = status;
        }
    }

    public record GetAIAnalysesByReviewIdQuery : IRequest<List<AIAnalysisDto>>
    {
        public Guid CodeReviewId { get; init; }

        public GetAIAnalysesByReviewIdQuery(Guid codeReviewId)
        {
            CodeReviewId = codeReviewId;
        }
    }

    public record GetCodeReviewMetricsQuery : IRequest<CodeReviewMetricsDto>
    {
        public Guid CodeReviewId { get; init; }

        public GetCodeReviewMetricsQuery(Guid codeReviewId)
        {
            CodeReviewId = codeReviewId;
        }
    }

    public record SearchCodeReviewsQuery : IRequest<PagedResult<CodeReviewDto>>
    {
        public string SearchTerm { get; init; }
        public ReviewStatus? Status { get; init; }
        public Priority? Priority { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public string SortBy { get; init; } = "CreatedAt";
        public bool SortDescending { get; init; } = true;

        public SearchCodeReviewsQuery(
            string searchTerm = null,
            ReviewStatus? status = null,
            Priority? priority = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20,
            string sortBy = "CreatedAt",
            bool sortDescending = true)
        {
            SearchTerm = searchTerm;
            Status = status;
            Priority = priority;
            StartDate = startDate;
            EndDate = endDate;
            Page = page;
            PageSize = pageSize;
            SortBy = sortBy;
            SortDescending = sortDescending;
        }
    }
}
