namespace Tickflo.CoreTest.Services;

using Microsoft.Extensions.Logging;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class ReportRunServiceTests
{
    [Fact]
    public async Task RunReportAsyncSucceedsCompletesRun()
    {
        var report = new Report { Id = 10, WorkspaceId = 7, Name = "Test" };
        var createdRunId = 42;
        var reporyRepository = new Mock<IReportRepository>();
        var reportRunRepository = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>();
        var logger = new Mock<ILogger<ReportRunService>>();

        reporyRepository.Setup(r => r.FindAsync(report.WorkspaceId, report.Id)).ReturnsAsync(report);
        reportRunRepository.Setup(r => r.CreateAsync(It.IsAny<ReportRun>())).ReturnsAsync((ReportRun rr) => { rr.Id = createdRunId; return rr; });
        reportRunRepository.Setup(r => r.MarkRunningAsync(createdRunId)).ReturnsAsync(true);
        reporting.Setup(r => r.ExecuteAsync(report.WorkspaceId, report, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportExecutionResult(5, string.Empty, [1, 2], "file.csv", "text/csv"));
        reportRunRepository.Setup(r => r.CompleteAsync(createdRunId, "Succeeded", 5, null, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        reporyRepository.Setup(r => r.UpdateAsync(It.IsAny<Report>())).ReturnsAsync(report);

        var svc = new ReportRunService(reporyRepository.Object, reportRunRepository.Object, reporting.Object, logger.Object);

        var run = await svc.RunReportAsync(report.WorkspaceId, report.Id);

        Assert.NotNull(run);
        Assert.Equal("Succeeded", run!.Status);
        Assert.Equal(5, run.RowCount);
        Assert.NotNull(run.FileBytes);
        reportRunRepository.Verify(r => r.CompleteAsync(createdRunId, "Succeeded", 5, null, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        reporyRepository.Verify(r => r.UpdateAsync(It.Is<Report>(rep => rep.LastRun != null)), Times.Once);
    }

    [Fact]
    public async Task RunReportAsyncReportMissingReturnsNull()
    {
        var reporyRepository = new Mock<IReportRepository>();
        var reportRunRepository = new Mock<IReportRunRepository>(MockBehavior.Strict);
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        reporyRepository.Setup(r => r.FindAsync(7, 99)).ReturnsAsync((Report?)null);

        var svc = new ReportRunService(reporyRepository.Object, reportRunRepository.Object, reporting.Object, logger.Object);

        var run = await svc.RunReportAsync(7, 99);

        Assert.Null(run);
        reportRunRepository.VerifyNoOtherCalls();
        reporting.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReportRunsAsyncReturnsReportAndRuns()
    {
        var reporyRepository = new Mock<IReportRepository>();
        var reportRunRepository = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        var report = new Report { Id = 5, WorkspaceId = 3, Name = "r" };
        var runs = new List<ReportRun> { new() { Id = 11, ReportId = 5, WorkspaceId = 3 } };

        reporyRepository.Setup(r => r.FindAsync(3, 5)).ReturnsAsync(report);
        reportRunRepository.Setup(r => r.ListForReportAsync(3, 5, 100)).ReturnsAsync(runs);

        var svc = new ReportRunService(reporyRepository.Object, reportRunRepository.Object, reporting.Object, logger.Object);

        var result = await svc.GetReportRunsAsync(3, 5, 100);

        Assert.NotNull(result.Report);
        Assert.Single(result.Runs);
        Assert.Equal(11, result.Runs[0].Id);
    }

    [Fact]
    public async Task GetRunAsyncReturnsRun()
    {
        var reporyRepository = new Mock<IReportRepository>(MockBehavior.Strict);
        var reportRunRepository = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        reportRunRepository.Setup(r => r.FindAsync(3, 12)).ReturnsAsync(new ReportRun { Id = 12, WorkspaceId = 3, ReportId = 7 });

        var svc = new ReportRunService(reporyRepository.Object, reportRunRepository.Object, reporting.Object, logger.Object);

        var run = await svc.GetRunAsync(3, 12);

        Assert.NotNull(run);
        Assert.Equal(12, run!.Id);
        Assert.Equal(3, run.WorkspaceId);
    }
}

