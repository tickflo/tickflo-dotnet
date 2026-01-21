namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class TeamManagementServiceTests
{
    [Fact]
    public async Task CreateTeamAsyncThrowsWhenDuplicate()
    {
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.ListForWorkspaceAsync(1)).ReturnsAsync([new() { Name = "Ops", WorkspaceId = 1 }]);
        var svc = new TeamManagementService(teamRepo.Object, Mock.Of<ITeamMemberRepository>(), Mock.Of<IUserWorkspaceRepository>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateTeamAsync(1, "Ops"));
    }

    [Fact]
    public async Task SyncTeamMembersAsyncAddsAndRemoves()
    {
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.FindByIdAsync(2)).ReturnsAsync(new Team { Id = 2, WorkspaceId = 1 });
        var memberRepo = new Mock<ITeamMemberRepository>();
        memberRepo.Setup(r => r.ListMembersAsync(2)).ReturnsAsync([new() { Id = 1 }]);
        var uw = new Mock<IUserWorkspaceRepository>();
        uw.Setup(r => r.FindForWorkspaceAsync(1)).ReturnsAsync([new() { UserId = 1, Accepted = true }, new() { UserId = 3, Accepted = true }]);

        var svc = new TeamManagementService(teamRepo.Object, memberRepo.Object, uw.Object);
        await svc.SyncTeamMembersAsync(2, 1, [3]);

        memberRepo.Verify(r => r.RemoveAsync(2, 1), Times.Once);
        memberRepo.Verify(r => r.AddAsync(2, 3), Times.Once);
    }
}
