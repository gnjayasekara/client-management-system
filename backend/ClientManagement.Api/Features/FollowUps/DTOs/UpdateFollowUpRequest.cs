
namespace ClientManagement.Api.Features.FollowUps.DTOs;

public record UpdateFollowUpRequest(
    int ClientId,
    string Title,
    DateTime DueDate,
    bool IsCompleted
);
