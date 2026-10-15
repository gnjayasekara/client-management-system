
namespace ClientManagement.Api.Features.Dashboard.DTOs;

public record DashboardStatsResponse(
    int TotalClients,
    int ActiveClients,
    int InactiveClients,
    int PendingFollowUps,
    List<RecentClientResponse> RecentClients
);
