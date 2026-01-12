using System.Text;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ReportingServiceTests
{
    [Fact]
    public async Task GetRunPageAsync_PaginatesCsv()
    {
        var ctx = CreateContext();
        var svc = new ReportingService(ctx);
        var csv = "Id,Name\n1,A\n2,B\n3,C\n";
        var run = new ReportRun { RowCount = 3, FileBytes = Encoding.UTF8.GetBytes(csv) };

        var page = await svc.GetRunPageAsync(run, page: 2, take: 2);

        Assert.Equal(2, page.Page);
        Assert.Equal(2, page.Take);
        Assert.Equal(3, page.TotalRows);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(3, page.FromRow);
        Assert.Equal(3, page.ToRow);
        Assert.True(page.HasContent);
        Assert.Collection(page.Headers,
            h => Assert.Equal("Id", h),
            h => Assert.Equal("Name", h));
        Assert.Single(page.Rows);
        Assert.Equal(new[] { "3", "C" }, page.Rows[0]);
    }

    [Fact]
    public async Task GetRunPageAsync_NoContent_ReturnsEmpty()
    {
        var ctx = CreateContext();
        var svc = new ReportingService(ctx);
        var run = new ReportRun { RowCount = 0, FileBytes = null };

        var page = await svc.GetRunPageAsync(run, page: 1, take: 100);

        Assert.False(page.HasContent);
        Assert.Empty(page.Headers);
        Assert.Empty(page.Rows);
        Assert.Equal(0, page.FromRow);
        Assert.Equal(0, page.ToRow);
    }

    private static Tickflo.Core.Data.TickfloDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<Tickflo.Core.Data.TickfloDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new Tickflo.Core.Data.TickfloDbContext(options);
    }
}
