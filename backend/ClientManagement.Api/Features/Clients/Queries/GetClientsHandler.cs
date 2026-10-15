
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Clients.Queries;

public static class GetClientsHandler
{
    public static async Task<IResult> HandleAsync(
        string? search,
        bool? isActive,
        AppDbContext db,
        CancellationToken ct)
    {
        var query = db.Clients.AsNoTracking();

        // Search by name, company or email
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Company != null &&
                 x.Company.Contains(term)) ||
                (x.Email != null &&
                 x.Email.Contains(term)));
        }

        // Filter by status
        if (isActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == isActive.Value);
        }

        var clients = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
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
            .ToListAsync(ct);

        return Results.Ok(clients);
    }
}
