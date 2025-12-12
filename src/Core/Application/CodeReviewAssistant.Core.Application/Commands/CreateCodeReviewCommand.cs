using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
using CodeReviewAssistant.Core.Domain.Entities;
using CodeReviewAssistant.Core.Domain.ValueObjects;
using CodeReviewAssistant.Application.Interfaces;
using CodeReviewAssistant.Application.DTOs;

namespace CodeReviewAssistant.Application.Commands
{
    public record CreateCodeReviewCommand(
        string Title,
        string Description,
        string RepositoryUrl,
        string BranchName,
        string CommitHash,
        string RequestedBy,
        Priority Priority = Priority.Medium,
        List<string> FilePaths = null
    ) : IRequest<CodeReviewDto>;

    public class CreateCodeReviewCommandValidator : AbstractValidator<CreateCodeReviewCommand>
    {
        public CreateCodeReviewCommandValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Title is required and must not exceed 200 characters");

            RuleFor(x => x.Description)
                .NotEmpty()
                .MaximumLength(1000)
                .WithMessage("Description is required and must not exceed 1000 characters");

            RuleFor(x => x.RepositoryUrl)
                .NotEmpty()
                .Must(BeValidGitHubUrl)
                .WithMessage("Repository URL must be a valid GitHub repository URL");

            RuleFor(x => x.BranchName)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Branch name is required and must not exceed 100 characters");

            RuleFor(x => x.CommitHash)
                .NotEmpty()
                .Length(7, 40)
                .WithMessage("Commit hash must be between 7 and 40 characters");

            RuleFor(x => x.RequestedBy)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Requested by is required and must not exceed 100 characters");
        }

        private bool BeValidGitHubUrl(string url)
        {
            try
            {
                GitHubRepository.FromUrl(url);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class CreateCodeReviewCommandHandler : IRequestHandler<CreateCodeReviewCommand, CodeReviewDto>
    {
        private readonly ICodeReviewRepository _codeReviewRepository;
        private readonly IGitHubService _gitHubService;
        private readonly IEventPublisher _eventPublisher;

        public CreateCodeReviewCommandHandler(
            ICodeReviewRepository codeReviewRepository,
            IGitHubService gitHubService,
            IEventPublisher eventPublisher)
        {
            _codeReviewRepository = codeReviewRepository ?? throw new ArgumentNullException(nameof(codeReviewRepository));
            _gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task<CodeReviewDto> Handle(CreateCodeReviewCommand request, CancellationToken cancellationToken)
        {
            // Validate GitHub repository access
            var repository = GitHubRepository.FromUrl(request.RepositoryUrl);
            await _gitHubService.ValidateRepositoryAccessAsync(repository, cancellationToken);

            // Create code review
            var codeReview = new CodeReview(
                request.Title,
                request.Description,
                request.RepositoryUrl,
                request.BranchName,
                request.CommitHash,
                request.RequestedBy,
                request.Priority);

            // Add files if specified
            if (request.FilePaths != null && request.FilePaths.Any())
            {
                foreach (var filePath in request.FilePaths)
                {
                    var fileContent = await _gitHubService.GetFileContentAsync(
                        repository,
                        request.BranchName,
                        filePath,
                        cancellationToken);

                    var fileType = System.IO.Path.GetExtension(filePath);
                    codeReview.AddFile(filePath, fileContent, fileType);
                }
            }

            await _codeReviewRepository.AddAsync(codeReview, cancellationToken);
            await _codeReviewRepository.SaveChangesAsync(cancellationToken);

            // Publish domain event
            await _eventPublisher.PublishAsync(new CodeReviewCreatedEvent(codeReview.Id), cancellationToken);

            return new CodeReviewDto
            {
                Id = codeReview.Id,
                Title = codeReview.Title,
                Description = codeReview.Description,
                RepositoryUrl = codeReview.RepositoryUrl,
                BranchName = codeReview.BranchName,
                CommitHash = codeReview.CommitHash,
                Status = codeReview.Status,
                Priority = codeReview.Priority,
                RequestedBy = codeReview.RequestedBy,
                Created = codeReview.Created,
                Summary = codeReview.Summary,
                TotalIssues = codeReview.TotalIssues,
                CriticalIssues = codeReview.CriticalIssues,
                MajorIssues = codeReview.MajorIssues,
                MinorIssues = codeReview.MinorIssues
            };
        }
    }

    public record CodeReviewCreatedEvent(Guid CodeReviewId);
}
