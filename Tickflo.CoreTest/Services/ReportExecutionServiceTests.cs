namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ReportExecutionServiceTests
{
    [Fact]
    public async Task ExecuteReportAsyncThrowsWhenNoWorkspaceAccess()
    {
        var reporyRepository = Mock.Of<IReportRepository>();
        var reportRunRepository = Mock.Of<IReportRunRepository>();
        var wsRepo = new Mock<IUserWorkspaceRepository>();
        wsRepo.Setup(r => r.FindAsync(8, 1)).ReturnsAsync((UserWorkspace?)null);
        var reporting = Mock.Of<IReportingService>();
        var svc = new ReportExecutionService(reporyRepository, reportRunRepository, wsRepo.Object, reporting);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.ExecuteReportAsync(8, 1, 2));
    }

    [Fact]
    public async Task ExecuteReportAsyncDelegatesToReportingService()
    {
        var reporyRepository = new Mock<IReportRepository>();
        reporyRepository.Setup(r => r.FindAsync(1, 2)).ReturnsAsync(new Report { Id = 2, WorkspaceId = 1 });
        var reportRunRepository = Mock.Of<IReportRunRepository>();
        var wsRepo = new Mock<IUserWorkspaceRepository>();
        wsRepo.Setup(r => r.FindAsync(9, 1)).ReturnsAsync(new UserWorkspace { UserId = 9, WorkspaceId = 1, Accepted = true });
        var reporting = new Mock<IReportingService>();
        reporting.Setup(r => r.ExecuteAsync(1, It.IsAny<Report>(), CancellationToken.None))
            .ReturnsAsync(new ReportExecutionResult(1, "path", [], "report.csv", "text/csv"));
        var svc = new ReportExecutionService(reporyRepository.Object, reportRunRepository, wsRepo.Object, reporting.Object);

        var result = await svc.ExecuteReportAsync(9, 1, 2);

        Assert.NotNull(result);
        reporting.Verify(r => r.ExecuteAsync(1, It.Is<Report>(rep => rep.Id == 2), CancellationToken.None), Times.Once);
    }
}
