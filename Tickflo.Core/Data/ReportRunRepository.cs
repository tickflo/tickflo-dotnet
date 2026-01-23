namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public class ReportRunRepository(TickfloDbContext dbContext) : IReportRunRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<ReportRun> CreateAsync(ReportRun run)
    {
        this.dbContext.ReportRuns.Add(run);
        await this.dbContext.SaveChangesAsync();
        return run;
    }

    public async Task<bool> MarkRunningAsync(int id)
    {
        var reportRun = await this.dbContext.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (reportRun == null)
        {
            return false;
        }

        reportRun.Status = "Running";
        await this.dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAsync(int id, string status, int rowCount, string? filePath, byte[]? fileBytes = null, string? contentType = null, string? fileName = null)
    {
        var reportRun = await this.dbContext.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (reportRun == null)
        {
            return false;
        }

        reportRun.Status = status;
        reportRun.RowCount = rowCount;
        reportRun.FilePath = filePath;
        reportRun.FileBytes = fileBytes;
        reportRun.ContentType = contentType;
        reportRun.FileName = fileName;
        reportRun.FinishedAt = DateTime.UtcNow;
        await this.dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ReportRun>> ListForReportAsync(int workspaceId, int reportId, int take = 50) => await this.dbContext.ReportRuns
            .Where(r => r.WorkspaceId == workspaceId && r.ReportId == reportId)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync();

    public async Task<ReportRun?> FindAsync(int workspaceId, int id) => await this.dbContext.ReportRuns.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);

    public async Task<int> DeleteForReportAsync(int workspaceId, int reportId)
    {
        var runs = await this.dbContext.ReportRuns.Where(r => r.WorkspaceId == workspaceId && r.ReportId == reportId).ToListAsync();
        if (runs.Count == 0)
        {
            return 0;
        }

        this.dbContext.ReportRuns.RemoveRange(runs);
        return await this.dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateContentAsync(int id, byte[] fileBytes, string contentType, string fileName)
    {
        var reportRun = await this.dbContext.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (reportRun == null)
        {
            return false;
        }

        reportRun.FileBytes = fileBytes;
        reportRun.ContentType = contentType;
        reportRun.FileName = fileName;
        await this.dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ReportRun>> ListMissingContentAsync(int workspaceId, int? reportId = null, int take = 10000)
    {
        var query = this.dbContext.ReportRuns.AsNoTracking().Where(r => r.WorkspaceId == workspaceId && r.FileBytes == null && r.FilePath != null && r.Status == "Succeeded");
        if (reportId.HasValue)
        {
            query = query.Where(r => r.ReportId == reportId.Value);
        }

        return await query.OrderByDescending(r => r.StartedAt).Take(take).ToListAsync();
    }
}
