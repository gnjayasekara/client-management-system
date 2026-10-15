
using ClientManagement.Api.Features.Notes.Commands;
using ClientManagement.Api.Features.Notes.DTOs;
using ClientManagement.Api.Features.Notes.Queries;
using ClientManagement.Api.Infrastructure.Data;
using FluentValidation;

namespace ClientManagement.Api.Features.Notes;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(
        this IEndpointRouteBuilder app)
    {
        // Routes nested under a client
        var clientNotes = app
            .MapGroup("/api/clients/{clientId:int}/notes")
            .WithTags("Client Notes");

        // GET: Notes belonging to a client
        clientNotes.MapGet(
            "/",
            GetClientNotesHandler.HandleAsync
        );

        // POST: Add a note to a client
        clientNotes.MapPost("/", async (
            int clientId,
            CreateNoteRequest request,
            IValidator<CreateNoteRequest> validator,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var result = await validator.ValidateAsync(
                request, ct);

            if (!result.IsValid)
            {
                return Results.ValidationProblem(
                    result.ToDictionary());
            }

            return await CreateNoteHandler.HandleAsync(
                clientId, request, db, ct);
        });

        // DELETE: Delete a note by its ID
        app.MapDelete(
            "/api/notes/{id:int}",
            DeleteNoteHandler.HandleAsync
        )
        .WithTags("Client Notes");

        return app;
    }
}
