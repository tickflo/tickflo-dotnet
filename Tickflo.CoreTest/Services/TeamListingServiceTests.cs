namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Xunit;

public class TeamListingServiceTests
{
    [Fact]
    public async Task GetListAsyncReturnsMemberCounts()
    {
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.ListForWorkspaceAsync(1)).ReturnsAsync([new() { Id = 11, WorkspaceId = 1, Name = "Ops" }]);
        var memberRepo = new Mock<ITeamMemberRepository>();
        memberRepo.Setup(r => r.ListMembersAsync(11)).ReturnsAsync([new() { Id = 1 }, new() { Id = 2 }]);

        var svc = new TeamListingService(teamRepo.Object, memberRepo.Object);
        var (teams, memberCounts) = await svc.GetListAsync(1);

        Assert.Single(teams);
        Assert.Equal(2, memberCounts[11]);
    }
}
