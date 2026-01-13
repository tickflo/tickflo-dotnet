using Moq;
using System.Threading;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class WorkspaceDashboardViewServiceTests
{
    [Fact]
    public async Task BuildAsync_ReturnsAggregatedView()
    {
        var workspaceId = 1;
        var userId = 10;
        var teamIds = new List<int>();
        var rangeDays = 30;

        // Repos
        var ticketRepo = new Mock<ITicketRepository>();
        var statusRepo = new Mock<ITicketStatusRepository>();
        var typeRepo = new Mock<ITicketTypeRepository>();
        var priorityRepo = new Mock<ITicketPriorityRepository>();
        var userRepo = new Mock<IUserRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var uwRepo = new Mock<IUserWorkspaceRepository>();

        // Dashboard service
        var dashboard = new Mock<IDashboardService>();
        dashboard.Setup(d => d.GetTicketStatsAsync(workspaceId, userId, "all", teamIds)).ReturnsAsync(new DashboardTicketStats
        {
            TotalTickets = 5,
            OpenTickets = 3,
            ResolvedTickets = 2,
            ActiveMembers = 2
        });
        dashboard.Setup(d => d.GetPriorityCountsAsync(workspaceId, userId, "all", teamIds)).ReturnsAsync(new Dictionary<string, int> { { "High", 2 } });
        dashboard.Setup(d => d.GetActivitySeriesAsync(workspaceId, userId, "all", teamIds, rangeDays)).ReturnsAsync(new List<ActivityDataPoint>
        {
            new ActivityDataPoint { Date = "2024-01-01", Created = 1, Closed = 0 }
        });
        dashboard.Setup(d => d.GetTopMembersAsync(workspaceId, userId, "all", teamIds, 5)).ReturnsAsync(new List<TopMember>
        {
            new TopMember { UserId = 10, Name = "U", ClosedCount = 2 }
        });
        dashboard.Setup(d => d.GetAverageResolutionTimeAsync(workspaceId, userId, "all", teamIds)).ReturnsAsync("2h");
        dashboard.Setup(d => d.FilterTicketsByAssignment(It.IsAny<List<Ticket>>(), "all", userId))
            .Returns((List<Ticket> t, string _, int _) => t);

        // Status/type/priority lists
        statusRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketStatus>
        {
            new TicketStatus { Name = "Open", IsClosedState = false, Color = "#00ff00" }
        });
        typeRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketType>
        {
            new TicketType { Name = "Bug", Color = "red" }
        });
        priorityRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<TicketPriority>
        {
            new TicketPriority { Name = "High", Color = "orange" }
        });

        // Tickets
        ticketRepo.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Ticket>
        {
            new Ticket { Id = 1, WorkspaceId = workspaceId, Subject = "S", Type = "Bug", Status = "Open", CreatedAt = DateTime.UtcNow }
        });

        // Users/teams
        uwRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId)).ReturnsAsync(new List<UserWorkspace>
        {
            new UserWorkspace { UserId = 10, WorkspaceId = workspaceId, Accepted = true }
        });
        userRepo.Setup(r => r.FindByIdAsync(10)).ReturnsAsync(new User { Id = 10, Name = "User" });
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId)).ReturnsAsync(new List<Team>());

        // Permission repositories
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        var rolePerms = new Mock<IRolePermissionRepository>();
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId)).ReturnsAsync(true);
        rolePerms.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, true)).ReturnsAsync("all");

        var service = new WorkspaceDashboardViewService(
            ticketRepo.Object,
            statusRepo.Object,
            typeRepo.Object,
            priorityRepo.Object,
            userRepo.Object,
            teamRepo.Object,
            uwRepo.Object,
            dashboard.Object,
            uwrRepo.Object,
            rolePerms.Object);

        var view = await service.BuildAsync(workspaceId, userId, "all", teamIds, rangeDays, "all");

        Assert.Equal(5, view.TotalTickets);
        Assert.Equal(3, view.OpenTickets);
        Assert.Equal("#00ff00", view.PrimaryColor);
        Assert.Single(view.RecentTickets);
        Assert.Single(view.StatusList);
        Assert.Single(view.TypeList);
        Assert.Single(view.PriorityList);
        Assert.Equal("2h", view.AvgResolutionLabel);
        Assert.True(view.CanViewDashboard);
        Assert.True(view.CanViewTickets);
        Assert.Equal("all", view.TicketViewScope);
    }
}

