using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using CodeReviewAssistant.Application.Commands;
using CodeReviewAssistant.Application.Queries;
using CodeReviewAssistant.Application.DTOs;

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
        public async Task<ActionResult<CodeReviewListResponse>> GetCodeReviews(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] ReviewStatus? status = null,
            [FromQuery] string search = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetCodeReviewsQuery(pageNumber, pageSize, status, search);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
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

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CodeReviewDto>> UpdateCodeReview(
            Guid id,
            [FromBody] UpdateCodeReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new UpdateCodeReviewCommand(id, request.Title, request.Description, request.Priority);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:guid}/start")]
        public async Task<ActionResult> StartCodeReview(Guid id, CancellationToken cancellationToken = default)
        {
            var command = new StartCodeReviewCommand(id, User.Identity.Name);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/complete")]
        public async Task<ActionResult> CompleteCodeReview(Guid id, CancellationToken cancellationToken = default)
        {
            var command = new CompleteCodeReviewCommand(id, User.Identity.Name);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/fail")]
        public async Task<ActionResult> FailCodeReview(
            Guid id,
            [FromBody] FailCodeReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new FailCodeReviewCommand(id, request.Reason, User.Identity.Name);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:guid}/comments")]
        public async Task<ActionResult<ReviewCommentDto>> AddComment(
            Guid id,
            [FromBody] AddCommentRequest request,
            CancellationToken cancellationToken = default)
        {
            var command = new AddCommentCommand(id, request.Content, User.Identity.Name, request.ParentCommentId);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/statistics")]
        public async Task<ActionResult<CodeReviewStatisticsDto>> GetStatistics(CancellationToken cancellationToken = default)
        {
            var query = new GetCodeReviewStatisticsQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> DeleteCodeReview(Guid id, CancellationToken cancellationToken = default)
        {
            var command = new DeleteCodeReviewCommand(id, User.Identity.Name);
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }

    public class FailCodeReviewRequest
    {
        public string Reason { get; set; }
    }
}
