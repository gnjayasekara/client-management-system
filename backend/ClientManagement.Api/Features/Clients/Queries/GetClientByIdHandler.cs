
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Clients.Queries;

public static class GetClientByIdHandler
{
    public static async Task<IResult> HandleAsync(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var client = await db.Clients
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ClientResponse(
                x.Id,
                x.Name,
                x.Company,
                x.Email,
                x.Phone,
                x.Address,
                x.IsActive,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(ct);

        return client is null
            ? Results.NotFound()
            : Results.Ok(client);
    }
}
