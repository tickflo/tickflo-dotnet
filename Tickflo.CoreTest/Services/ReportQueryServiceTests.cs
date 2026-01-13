using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ReportQueryServiceTests
{
    [Fact]
    public async Task ListReportsAsync_MapsFields()
    {
        var repo = new Mock<IReportRepository>();
        repo.Setup(r => r.ListAsync(2)).ReturnsAsync(new List<Report>
        {
            new() { Id = 10, WorkspaceId = 2, Name = "A", Ready = true, LastRun = new DateTime(2024,1,1) }
        });

        var svc = new ReportQueryService(repo.Object);

        var items = await svc.ListReportsAsync(2);

        var item = Assert.Single(items);
        Assert.Equal(10, item.Id);
        Assert.Equal("A", item.Name);
        Assert.True(item.Ready);
        Assert.Equal(new DateTime(2024,1,1), item.LastRun);
    }
}

