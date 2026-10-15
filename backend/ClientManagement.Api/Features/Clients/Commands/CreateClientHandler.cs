
using ClientManagement.Api.Features.Clients.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.Clients.Commands;

public static class CreateClientHandler
{
    public static async Task<IResult> HandleAsync(
        CreateClientRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var client = new Client
        {
            Name = request.Name.Trim(),
            Company = request.Company,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = request.IsActive
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);

        var response = new ClientResponse(
            client.Id,
            client.Name,
            client.Company,
            client.Email,
            client.Phone,
            client.Address,
            client.IsActive,
            client.CreatedAt
        );

        return Results.Created(
            $"/api/clients/{client.Id}",
            response
        );
    }
}
