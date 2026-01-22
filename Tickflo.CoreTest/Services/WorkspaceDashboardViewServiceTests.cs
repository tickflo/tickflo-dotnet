namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceDashboardViewServiceTests
{
    [Fact]
    public async Task BuildAsyncReturnsAggregatedView()
    {
        var workspaceId = 1;
        var userId = 10;
        var teamIds = new List<int>();
        var rangeDays = 30;

        // Repos
        var ticketRepository = new Mock<ITicketRepository>();
        var statusRepository = new Mock<ITicketStatusRepository>();
        var ticketTypeRepository = new Mock<ITicketTypeRepository>();
        var priorityRepository = new Mock<ITicketPriorityRepository>();
        var userRepository = new Mock<IUserRepository>();
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
        dashboard.Setup(d => d.GetActivitySeriesAsync(workspaceId, userId, "all", teamIds, rangeDays)).ReturnsAsync(
        [
            new ActivityDataPoint { Date = "2024-01-01", Created = 1, Closed = 0 }
        ]);
        dashboard.Setup(d => d.GetTopMembersAsync(workspaceId, userId, "all", teamIds, 5)).ReturnsAsync(
        [
            new TopMember { UserId = 10, Name = "U", ClosedCount = 2 }
        ]);
        dashboard.Setup(d => d.GetAverageResolutionTimeAsync(workspaceId, userId, "all", teamIds)).ReturnsAsync("2h");
        dashboard.Setup(d => d.FilterTicketsByAssignment(It.IsAny<List<Ticket>>(), "all", userId))
            .Returns((List<Ticket> t, string _, int _) => t);

        // Status/type/priority lists
        statusRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Name = "Open", IsClosedState = false, Color = "#00ff00" }
        ]);
        ticketTypeRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Name = "Bug", Color = "red" }
        ]);
        priorityRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Name = "High", Color = "orange" }
        ]);

        // Tickets
        ticketRepository.Setup(r => r.ListAsync(workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new() { Id = 1, WorkspaceId = workspaceId, Subject = "S", TicketTypeId = 1, StatusId = 1, CreatedAt = DateTime.UtcNow }
        ]);

        // Users/teams
        uwRepo.Setup(r => r.FindForWorkspaceAsync(workspaceId)).ReturnsAsync(
        [
            new UserWorkspace { UserId = 10, WorkspaceId = workspaceId, Accepted = true }
        ]);
        userRepository.Setup(r => r.FindByIdAsync(10)).ReturnsAsync(new User { Id = 10, Name = "User" });
        teamRepo.Setup(r => r.ListForWorkspaceAsync(workspaceId)).ReturnsAsync([]);

        // Permission repositories
        var uwrRepo = new Mock<IUserWorkspaceRoleRepository>();
        var rolePermissionRepository = new Mock<IRolePermissionRepository>();
        uwrRepo.Setup(r => r.IsAdminAsync(userId, workspaceId)).ReturnsAsync(true);
        rolePermissionRepository.Setup(r => r.GetTicketViewScopeForUserAsync(workspaceId, userId, true)).ReturnsAsync("all");

        var service = new WorkspaceDashboardViewService(
            ticketRepository.Object,
            statusRepository.Object,
            ticketTypeRepository.Object,
            priorityRepository.Object,
            userRepository.Object,
            teamRepo.Object,
            uwRepo.Object,
            dashboard.Object,
            uwrRepo.Object,
            rolePermissionRepository.Object);

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

