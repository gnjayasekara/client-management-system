
namespace ClientManagement.Api.Features.FollowUps.DTOs;

public record CreateFollowUpRequest(
    int ClientId,
    string Title,
    DateTime DueDate
);
