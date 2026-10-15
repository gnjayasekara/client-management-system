
namespace ClientManagement.Api.Features.Clients.DTOs;

public record UpdateClientRequest(
    string Name,
    string? Company,
    string? Email,
    string? Phone,
    string? Address,
    bool IsActive
);
