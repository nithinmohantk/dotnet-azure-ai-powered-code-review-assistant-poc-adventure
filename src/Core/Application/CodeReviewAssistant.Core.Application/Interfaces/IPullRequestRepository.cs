using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeReviewAssistant.Core.Domain.Entities;

namespace CodeReviewAssistant.Core.Application.Interfaces
{
    public interface IPullRequestRepository
    {
        Task<PullRequest> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<PullRequest> GetByGitHubIdAsync(int gitHubPullRequestId, string repositoryName, string repositoryOwner, CancellationToken cancellationToken = default);
        Task<IEnumerable<PullRequest>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<PullRequest>> GetByRepositoryAsync(string repositoryName, string repositoryOwner, CancellationToken cancellationToken = default);
        Task<PullRequest> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);
        Task UpdateAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);
        Task DeleteAsync(PullRequest pullRequest, CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByGitHubIdAsync(int gitHubPullRequestId, string repositoryName, string repositoryOwner, CancellationToken cancellationToken = default);
    }
}
