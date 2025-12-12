using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeReviewAssistant.Core.Domain.Entities;

namespace CodeReviewAssistant.Application.Interfaces
{
    public interface ICodeReviewRepository
    {
        Task<CodeReview> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<CodeReview>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<CodeReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default);
        Task<IEnumerable<CodeReview>> GetByRequestedByAsync(string requestedBy, CancellationToken cancellationToken = default);
        Task<IEnumerable<CodeReview>> GetByRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default);
        Task<CodeReview> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(CodeReview codeReview, CancellationToken cancellationToken = default);
        Task UpdateAsync(CodeReview codeReview, CancellationToken cancellationToken = default);
        Task DeleteAsync(CodeReview codeReview, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
