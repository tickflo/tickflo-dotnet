namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ReportCommandServiceTests
{
    [Fact]
    public async Task CreateAsyncCallsRepo()
    {
        var repo = new Mock<IReportRepository>();
        var input = new Report { WorkspaceId = 1, Name = "r" };
        repo.Setup(r => r.CreateAsync(input)).ReturnsAsync(input);

        var svc = new ReportCommandService(repo.Object);
        var result = await svc.CreateAsync(input);

        Assert.Equal("r", result.Name);
        repo.Verify(r => r.CreateAsync(input), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncCallsRepo()
    {
        var repo = new Mock<IReportRepository>();
        var input = new Report { Id = 2, WorkspaceId = 1, Name = "r" };
        repo.Setup(r => r.UpdateAsync(input)).ReturnsAsync(input);

        var svc = new ReportCommandService(repo.Object);
        var result = await svc.UpdateAsync(input);

        Assert.Equal(2, result!.Id);
        repo.Verify(r => r.UpdateAsync(input), Times.Once);
    }
}

