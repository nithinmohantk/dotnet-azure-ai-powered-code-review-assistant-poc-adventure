using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeReviewAssistant.Core.Domain.Entities;
using CodeReviewAssistant.Core.Application.Interfaces;
using CodeReviewAssistant.Infrastructure.Persistence;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.Infrastructure.Persistence.Repositories
{
    public class CodeReviewRepository : ICodeReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public CodeReviewRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CodeReview> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .FirstOrDefaultAsync(cr => cr.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .OrderByDescending(cr => cr.Created)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .Where(cr => cr.Status == status)
                .OrderByDescending(cr => cr.Created)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> GetByRequestedByAsync(string requestedBy, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .Where(cr => cr.RequestedBy == requestedBy)
                .OrderByDescending(cr => cr.Created)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> GetByRepositoryAsync(string repositoryUrl, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .Where(cr => cr.RepositoryUrl == repositoryUrl)
                .OrderByDescending(cr => cr.Created)
                .ToListAsync(cancellationToken);
        }

        public async Task<CodeReview> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .Include(cr => cr.Issues)
                .Include(cr => cr.Comments)
                .Include(cr => cr.Files)
                .FirstOrDefaultAsync(cr => cr.Id == id, cancellationToken);
        }

        public async Task AddAsync(CodeReview codeReview, CancellationToken cancellationToken = default)
        {
            await _context.CodeReviews.AddAsync(codeReview, cancellationToken);
        }

        public async Task UpdateAsync(CodeReview codeReview, CancellationToken cancellationToken = default)
        {
            _context.CodeReviews.Update(codeReview);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(CodeReview codeReview, CancellationToken cancellationToken = default)
        {
            _context.CodeReviews.Remove(codeReview);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .AnyAsync(cr => cr.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .OrderByDescending(cr => cr.Created)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews.CountAsync(cancellationToken);
        }

        public async Task<IEnumerable<CodeReview>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            return await _context.CodeReviews
                .Where(cr => cr.Title.Contains(searchTerm) || 
                             cr.Description.Contains(searchTerm) ||
                             cr.RepositoryUrl.Contains(searchTerm))
                .OrderByDescending(cr => cr.Created)
                .ToListAsync(cancellationToken);
        }
    }
}
