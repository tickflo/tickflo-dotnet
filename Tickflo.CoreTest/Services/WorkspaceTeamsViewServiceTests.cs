namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class WorkspaceTeamsViewServiceTests
{
    [Fact]
    public async Task BuildAsyncLoadsTeamsWithPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<ITeamListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>
        {
            { "teams", new EffectiveSectionPermission { Section = "teams", CanView = true, CanCreate = true, CanEdit = true } }
        };

        var teams = new List<Team>
        {
            new() { Id = 1, WorkspaceId = 1, Name = "Engineering" },
            new() { Id = 2, WorkspaceId = 1, Name = "Sales" }
        };

        var memberCounts = new Dictionary<int, int> { { 1, 5 }, { 2, 3 } };

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1))
            .ReturnsAsync((teams, memberCounts));

        var service = new WorkspaceTeamsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.CanViewTeams);
        Assert.True(result.CanCreateTeams);
        Assert.True(result.CanEditTeams);
        Assert.Equal(2, result.Teams.Count);
        Assert.Equal("Engineering", result.Teams[0].Name);
        Assert.Equal(5, result.MemberCounts[1]);
    }

    [Fact]
    public async Task BuildAsyncAdminAlwaysCanView()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<ITeamListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        var teams = new List<Team>();
        var memberCounts = new Dictionary<int, int>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(true);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        listingService.Setup(x => x.GetListAsync(1))
            .ReturnsAsync((teams, memberCounts));

        var service = new WorkspaceTeamsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.True(result.CanViewTeams);
        Assert.True(result.CanCreateTeams);
        Assert.True(result.CanEditTeams);
    }

    [Fact]
    public async Task BuildAsyncDeniesAccessWhenNoPermissions()
    {
        // Arrange
        var accessService = new Mock<IWorkspaceAccessService>();
        var listingService = new Mock<ITeamListingService>();

        var permissions = new Dictionary<string, EffectiveSectionPermission>();

        accessService.Setup(x => x.UserIsWorkspaceAdminAsync(100, 1))
            .ReturnsAsync(false);

        accessService.Setup(x => x.GetUserPermissionsAsync(1, 100))
            .ReturnsAsync(permissions);

        var service = new WorkspaceTeamsViewService(accessService.Object, listingService.Object);

        // Act
        var result = await service.BuildAsync(1, 100);

        // Assert
        Assert.False(result.CanViewTeams);
        Assert.False(result.CanCreateTeams);
        Assert.False(result.CanEditTeams);
        Assert.Empty(result.Teams);
        Assert.Empty(result.MemberCounts);
        listingService.Verify(x => x.GetListAsync(It.IsAny<int>()), Times.Never);
    }
}

