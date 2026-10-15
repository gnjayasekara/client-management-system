
using ClientManagement.Api.Features.FollowUps.DTOs;
using ClientManagement.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientManagement.Api.Features.FollowUps.Commands;

public static class CreateFollowUpHandler
{
    public static async Task<IResult> HandleAsync(
        CreateFollowUpRequest request,
        AppDbContext db,
        CancellationToken ct)
    {
        var clientExists = await db.Clients
            .AnyAsync(x => x.Id == request.ClientId, ct);

        if (!clientExists)
        {
            return Results.NotFound(
                new { message = "Client not found." });
        }

        var followUp = new FollowUp
        {
            ClientId = request.ClientId,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            IsCompleted = false
        };

        db.FollowUps.Add(followUp);
        await db.SaveChangesAsync(ct);

        var clientName = await db.Clients
            .Where(x => x.Id == followUp.ClientId)
            .Select(x => x.Name)
            .SingleAsync(ct);

        var response = new FollowUpResponse(
            followUp.Id,
            followUp.ClientId,
            clientName,
            followUp.Title,
            followUp.DueDate,
            followUp.IsCompleted
        );

        return Results.Created(
            $"/api/followups/{followUp.Id}",
            response);
    }
}
