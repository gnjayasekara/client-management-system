
namespace ClientManagement.Api.Features.FollowUps.DTOs;

public record FollowUpResponse(
    int Id,
    int ClientId,
    string ClientName,
    string Title,
    DateTime DueDate,
    bool IsCompleted
);
