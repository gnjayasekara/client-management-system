
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Notes.Commands;

public static class DeleteNoteHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var note = await db.ClientNotes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (note is null)
            return Results.NotFound();

        db.ClientNotes.Remove(note);

        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
