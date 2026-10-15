
using ClientManagement.Api.Features.Notes.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Notes.Commands;

public static class CreateNoteHandler
{
    public static async Task<IResult> HandleAsync(
        int clientId,
        CreateNoteRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        // Check whether the client exists
        var clientExists = await db.Clients
            .AnyAsync(x => x.Id == clientId, ct);

        if (!clientExists)
        {
            return Results.NotFound(new
            {
                message = "Client not found."
            });
        }

        // Create a new note
        var note = new ClientNote
        {
            ClientId = clientId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        // Save to SQL Server
        db.ClientNotes.Add(note);
        await db.SaveChangesAsync(ct);

        // Create response
        var response = new NoteResponse(
            note.Id,
            note.ClientId,
            note.Content,
            note.CreatedAt
        );

        return Results.Created(
            $"/api/clients/{clientId}/notes",
            response
        );
    }
}
