
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class DeleteClientHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (client is null)
            return Results.NotFound();

        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
