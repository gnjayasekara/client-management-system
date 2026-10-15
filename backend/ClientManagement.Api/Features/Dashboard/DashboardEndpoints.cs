
using ClientManagement.Api.Features.Dashboard.Queries;

namespace ClientManagement.Api.Features.Dashboard;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard");

        group.MapGet(
            "/stats",
            GetDashboardStatsHandler.HandleAsync
        );

        return group;
    }
}
