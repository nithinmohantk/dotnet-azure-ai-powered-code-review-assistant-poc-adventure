using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using CodeReviewAssistant.Core.Application.Commands;
using CodeReviewAssistant.Core.Application.Queries;
using CodeReviewAssistant.Core.Application.DTOs;

namespace CodeReviewAssistant.WebApi.Endpoints
{
    public static class CodeReviewEndpoints
    {
        public static void MapCodeReviewEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/codereviews")
                .RequireAuthorization()
                .WithTags("Code Reviews");

            // GET /api/codereviews/{id}
            group.MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCodeReviewByIdQuery(id);
                var result = await mediator.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetCodeReviewById")
            .WithSummary("Get a code review by ID")
            .WithDescription("Retrieves a specific code review by its unique identifier")
            .Produces<CodeReviewDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // POST /api/codereviews
            group.MapPost("/", async (
                [FromBody] CreateCodeReviewCommand command,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.Success 
                    ? Results.Created($"/api/codereviews/{result.CodeReviewId}", result)
                    : Results.BadRequest(result);
            })
            .WithName("CreateCodeReview")
            .WithSummary("Create a new code review")
            .WithDescription("Creates a new code review request")
            .Produces<CreateCodeReviewResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

            // GET /api/codereviews/{id}/analyses
            group.MapGet("/{id:guid}/analyses", async (
                Guid id,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetAIAnalysesByReviewIdQuery(id);
                var result = await mediator.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetAIAnalysesByReviewId")
            .WithSummary("Get AI analyses for a code review")
            .WithDescription("Retrieves all AI analyses performed for a specific code review")
            .Produces<List<AIAnalysisDto>>(StatusCodes.Status200OK);

            // GET /api/codereviews/{id}/metrics
            group.MapGet("/{id:guid}/metrics", async (
                Guid id,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCodeReviewMetricsQuery(id);
                var result = await mediator.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetCodeReviewMetrics")
            .WithSummary("Get code review metrics")
            .WithDescription("Retrieves detailed metrics for a specific code review")
            .Produces<CodeReviewMetricsDto>(StatusCodes.Status200OK);
        }
    }
}
