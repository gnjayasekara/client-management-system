
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class UpdateClientHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        UpdateClientRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var client = await db.Clients
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (client is null)
            return Results.NotFound();

        client.Name = request.Name.Trim();
        client.Company = request.Company;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.Address = request.Address;
        client.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
