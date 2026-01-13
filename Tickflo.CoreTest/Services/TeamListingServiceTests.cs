using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Teams;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class TeamListingServiceTests
{
    [Fact]
    public async Task GetListAsync_Returns_Member_Counts()
    {
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.ListForWorkspaceAsync(1)).ReturnsAsync(new List<Team> { new() { Id = 11, WorkspaceId = 1, Name = "Ops" } });
        var memberRepo = new Mock<ITeamMemberRepository>();
        memberRepo.Setup(r => r.ListMembersAsync(11)).ReturnsAsync(new List<User> { new() { Id = 1 }, new() { Id = 2 } });

        var svc = new TeamListingService(teamRepo.Object, memberRepo.Object);
        var (teams, memberCounts) = await svc.GetListAsync(1);

        Assert.Single(teams);
        Assert.Equal(2, memberCounts[11]);
    }
}
