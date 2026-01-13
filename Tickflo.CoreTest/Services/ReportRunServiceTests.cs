using Microsoft.Extensions.Logging;
using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ReportRunServiceTests
{
    [Fact]
    public async Task RunReportAsync_Succeeds_CompletesRun()
    {
        var report = new Report { Id = 10, WorkspaceId = 7, Name = "Test" };
        var createdRunId = 42;
        var reportRepo = new Mock<IReportRepository>();
        var runRepo = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>();
        var logger = new Mock<ILogger<ReportRunService>>();

        reportRepo.Setup(r => r.FindAsync(report.WorkspaceId, report.Id)).ReturnsAsync(report);
        runRepo.Setup(r => r.CreateAsync(It.IsAny<ReportRun>())).ReturnsAsync((ReportRun rr) => { rr.Id = createdRunId; return rr; });
        runRepo.Setup(r => r.MarkRunningAsync(createdRunId)).ReturnsAsync(true);
        reporting.Setup(r => r.ExecuteAsync(report.WorkspaceId, report, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportExecutionResult(5, string.Empty, new byte[] { 1, 2 }, "file.csv", "text/csv"));
        runRepo.Setup(r => r.CompleteAsync(createdRunId, "Succeeded", 5, null, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        reportRepo.Setup(r => r.UpdateAsync(It.IsAny<Report>())).ReturnsAsync(report);

        var svc = new ReportRunService(reportRepo.Object, runRepo.Object, reporting.Object, logger.Object);

        var run = await svc.RunReportAsync(report.WorkspaceId, report.Id);

        Assert.NotNull(run);
        Assert.Equal("Succeeded", run!.Status);
        Assert.Equal(5, run.RowCount);
        Assert.NotNull(run.FileBytes);
        runRepo.Verify(r => r.CompleteAsync(createdRunId, "Succeeded", 5, null, It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        reportRepo.Verify(r => r.UpdateAsync(It.Is<Report>(rep => rep.LastRun != null)), Times.Once);
    }

    [Fact]
    public async Task RunReportAsync_ReportMissing_ReturnsNull()
    {
        var reportRepo = new Mock<IReportRepository>();
        var runRepo = new Mock<IReportRunRepository>(MockBehavior.Strict);
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        reportRepo.Setup(r => r.FindAsync(7, 99)).ReturnsAsync((Report?)null);

        var svc = new ReportRunService(reportRepo.Object, runRepo.Object, reporting.Object, logger.Object);

        var run = await svc.RunReportAsync(7, 99);

        Assert.Null(run);
        runRepo.VerifyNoOtherCalls();
        reporting.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetReportRunsAsync_ReturnsReportAndRuns()
    {
        var reportRepo = new Mock<IReportRepository>();
        var runRepo = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        var report = new Report { Id = 5, WorkspaceId = 3, Name = "r" };
        var runs = new List<ReportRun> { new() { Id = 11, ReportId = 5, WorkspaceId = 3 } };

        reportRepo.Setup(r => r.FindAsync(3, 5)).ReturnsAsync(report);
        runRepo.Setup(r => r.ListForReportAsync(3, 5, 100)).ReturnsAsync(runs);

        var svc = new ReportRunService(reportRepo.Object, runRepo.Object, reporting.Object, logger.Object);

        var result = await svc.GetReportRunsAsync(3, 5, 100);

        Assert.NotNull(result.Report);
        Assert.Single(result.Runs);
        Assert.Equal(11, result.Runs[0].Id);
    }

    [Fact]
    public async Task GetRunAsync_ReturnsRun()
    {
        var reportRepo = new Mock<IReportRepository>(MockBehavior.Strict);
        var runRepo = new Mock<IReportRunRepository>();
        var reporting = new Mock<IReportingService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<ReportRunService>>();

        runRepo.Setup(r => r.FindAsync(3, 12)).ReturnsAsync(new ReportRun { Id = 12, WorkspaceId = 3, ReportId = 7 });

        var svc = new ReportRunService(reportRepo.Object, runRepo.Object, reporting.Object, logger.Object);

        var run = await svc.GetRunAsync(3, 12);

        Assert.NotNull(run);
        Assert.Equal(12, run!.Id);
        Assert.Equal(3, run.WorkspaceId);
    }
}
