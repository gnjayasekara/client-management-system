
using ClientManagement.Api.Features.Clients.Commands;
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Features.Clients.Queries;
using ClientManagement.Api.Infrastructure.Data;
using ClientManagement.Api.Infrastructure.Messaging.Clients;
using ClientManagement.Api.Infrastructure.Realtime;
using FluentValidation;

namespace ClientManagement.Api.Features.Clients;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients")
            .WithTags("Clients")
            .RequireAuthorization();

        group.MapGet("/", GetClientsHandler.HandleAsync);

        group.MapGet(
            "/{id:int}",
            GetClientByIdHandler.HandleAsync
        );

        
        group.MapPost("/", async (
            CreateClientRequest request,
            IValidator<CreateClientRequest> validator,
            AppDbContext db,
            IRealtimeNotifier realtimeNotifier,
            IClientEventPublisher eventPublisher,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            return await CreateClientHandler.HandleAsync(
                request,
                db,
                realtimeNotifier,
                eventPublisher,
                loggerFactory,
                ct
            );
        });


        group.MapPut("/{id:int}", async (
            int id,
            UpdateClientRequest request,
            IValidator<UpdateClientRequest> validator,
            AppDbContext db,
            IRealtimeNotifier realtimeNotifier,
            IClientEventPublisher eventPublisher,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            return await UpdateClientHandler.HandleAsync(
                id, request, db, realtimeNotifier, eventPublisher, loggerFactory, ct);
        });

        
        group.MapDelete("/{id:int}", async (
            int id,
            AppDbContext db,
            IRealtimeNotifier realtimeNotifier,
            IClientEventPublisher eventPublisher,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            return await DeleteClientHandler.HandleAsync(
                id,
                db,
                realtimeNotifier,
                eventPublisher,
                loggerFactory,
                ct);
        })
        .RequireAuthorization(policy =>
            policy.RequireRole(
                ClientManagement.Api.Infrastructure.Identity.AppRoles.Admin));


        return group;
    }
}