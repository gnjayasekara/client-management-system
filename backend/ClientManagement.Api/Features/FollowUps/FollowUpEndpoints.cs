
using ClientManagement.Api.Features.FollowUps.Commands;
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Features.FollowUps.Queries;
using ClientManagement.Api.Infrastructure.Data;
using FluentValidation;

namespace ClientManagement.Api.Features.FollowUps;

public static class FollowUpEndpoints
{
    public static RouteGroupBuilder MapFollowUpEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/followups")
            .WithTags("FollowUps")
            .RequireAuthorization();

        // Read operations
        group.MapGet("/", GetFollowUpsHandler.HandleAsync);

        group.MapGet(
            "/{id:int}",
            GetFollowUpByIdHandler.HandleAsync
        );

        // Create
        group.MapPost("/", async (
            CreateFollowUpRequest request,
            IValidator<CreateFollowUpRequest> validator,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(
                request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(
                    result.ToDictionary());

            return await CreateFollowUpHandler.HandleAsync(
                request, db, ct);
        });

        // Update
        group.MapPut("/{id:int}", async (
            int id,
            UpdateFollowUpRequest request,
            IValidator<UpdateFollowUpRequest> validator,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(
                request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(
                    result.ToDictionary());

            return await UpdateFollowUpHandler.HandleAsync(
                id, request, db, ct);
        });

        // Mark completed
        group.MapPatch(
            "/{id:int}/complete",
            CompleteFollowUpHandler.HandleAsync
        );

        // Delete
        group.MapDelete(
            "/{id:int}",
            DeleteFollowUpHandler.HandleAsync
        );

        return group;
    }
}
