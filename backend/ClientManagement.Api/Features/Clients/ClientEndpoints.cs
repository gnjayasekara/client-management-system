
using ClientManagement.Api.Features.Clients.Commands;
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Features.Clients.Queries;
using ClientManagement.Api.Infrastructure.Data;
using FluentValidation;

namespace ClientManagement.Api.Features.Clients;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients")
            .WithTags("Clients");

        group.MapGet("/", GetClientsHandler.HandleAsync);

        group.MapGet(
            "/{id:int}",
            GetClientByIdHandler.HandleAsync
        );

        group.MapPost("/", async (
            CreateClientRequest request,
            IValidator<CreateClientRequest> validator,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            return await CreateClientHandler.HandleAsync(
                request, db, ct);
        });

        group.MapPut("/{id:int}", async (
            int id,
            UpdateClientRequest request,
            IValidator<UpdateClientRequest> validator,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(request, ct);

            if (!result.IsValid)
                return Results.ValidationProblem(result.ToDictionary());

            return await UpdateClientHandler.HandleAsync(
                id, request, db, ct);
        });

        group.MapDelete(
            "/{id:int}",
            DeleteClientHandler.HandleAsync
        );

        return group;
    }
}
