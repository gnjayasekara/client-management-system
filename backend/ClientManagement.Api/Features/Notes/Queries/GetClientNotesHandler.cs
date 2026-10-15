
using ClientManagement.Api.Features.Notes.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Notes.Queries;

public static class GetClientNotesHandler
{
    public static async Task<IResult> HandleAsync(
        int clientId,
        AppDbContext db,
        CancellationToken ct)
    {
        // Check whether the client exists
        var clientExists = await db.Clients
            .AnyAsync(x => x.Id == clientId, ct);

        if (!clientExists)
            return Results.NotFound(new
            {
                message = "Client not found."
            });

        // Retrieve only notes for this client
        var notes = await db.ClientNotes
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new NoteResponse(
                x.Id,
                x.ClientId,
                x.Content,
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return Results.Ok(notes);
    }
}
