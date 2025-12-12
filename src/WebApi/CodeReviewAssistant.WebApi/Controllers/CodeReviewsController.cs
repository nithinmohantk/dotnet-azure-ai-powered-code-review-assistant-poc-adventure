using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using CodeReviewAssistant.Core.Application.Commands;
using CodeReviewAssistant.Core.Application.Queries;
using CodeReviewAssistant.Core.Application.DTOs;
using CodeReviewAssistant.Core.Domain.Events;

namespace CodeReviewAssistant.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CodeReviewsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CodeReviewsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpGet]
        public async Task<ActionResult<List<CodeReviewDto>>> GetCodeReviews(CancellationToken cancellationToken = default)
        {
            // For now, return empty list since GetCodeReviewsQuery doesn't exist
            return Ok(new List<CodeReviewDto>());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CodeReviewDto>> GetCodeReview(Guid id, CancellationToken cancellationToken = default)
        {
            var query = new GetCodeReviewByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<CodeReviewDto>> CreateCodeReview(
            [FromBody] CreateCodeReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new CreateCodeReviewCommand(
                request.Title,
                request.Description,
                request.RepositoryUrl,
                request.BranchName,
                request.CommitHash,
                User.Identity.Name,
                request.Priority,
                request.FilePaths);

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCodeReview), new { id = result.Id }, result);
        }

        [HttpPost("{id:guid}/start")]
        public async Task<ActionResult> StartCodeReview(Guid id, CancellationToken cancellationToken = default)
        {
            var command = new StartAIAnalysisCommand(
                id, 
                "CodeReview", 
                "GPT-4", 
                "latest", 
                new Dictionary<string, object>());
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:guid}/statistics")]
        public async Task<ActionResult> GetCodeReviewStatistics(Guid id, CancellationToken cancellationToken = default)
        {
            var query = new GetCodeReviewByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

    public class FailCodeReviewRequest
    {
        public string Reason { get; set; }
    }
}
