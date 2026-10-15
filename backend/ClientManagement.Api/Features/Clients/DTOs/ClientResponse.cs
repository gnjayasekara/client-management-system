
namespace ClientManagement.Api.Features.Clients.DTOs;

public record ClientResponse(
    int Id,
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTime CreatedAt
);
