
namespace ClientManagement.Api.Features.Clients.DTOs;

public record CreateClientRequest(
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive = true
);
