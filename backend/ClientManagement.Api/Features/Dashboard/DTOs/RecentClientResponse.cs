
namespace ClientManagement.Api.Features.Dashboard.DTOs;

public record RecentClientResponse(
    int Id,
    string Name,
    string? Company,
    string? Email,
    bool IsActive,
    DateTime CreatedAt
);
