using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class ReportRunRepository(TickfloDbContext db) : IReportRunRepository
{
    public async Task<ReportRun> CreateAsync(ReportRun run)
    {
        db.ReportRuns.Add(run);
        await db.SaveChangesAsync();
        return run;
    }

    public async Task<bool> MarkRunningAsync(int id)
    {
        var rr = await db.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (rr == null) return false;
        rr.Status = "Running";
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteAsync(int id, string status, int rowCount, string? filePath, byte[]? fileBytes = null, string? contentType = null, string? fileName = null)
    {
        var rr = await db.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (rr == null) return false;
        rr.Status = status;
        rr.RowCount = rowCount;
        rr.FilePath = filePath;
        rr.FileBytes = fileBytes;
        rr.ContentType = contentType;
        rr.FileName = fileName;
        rr.FinishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ReportRun>> ListForReportAsync(int workspaceId, int reportId, int take = 50)
    {
        return await db.ReportRuns
            .Where(r => r.WorkspaceId == workspaceId && r.ReportId == reportId)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ReportRun?> FindAsync(int workspaceId, int id)
    {
        return await db.ReportRuns.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);
    }

    public async Task<int> DeleteForReportAsync(int workspaceId, int reportId)
    {
        var runs = await db.ReportRuns.Where(r => r.WorkspaceId == workspaceId && r.ReportId == reportId).ToListAsync();
        if (runs.Count == 0) return 0;
        db.ReportRuns.RemoveRange(runs);
        return await db.SaveChangesAsync();
    }

    public async Task<bool> UpdateContentAsync(int id, byte[] fileBytes, string contentType, string fileName)
    {
        var rr = await db.ReportRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (rr == null) return false;
        rr.FileBytes = fileBytes;
        rr.ContentType = contentType;
        rr.FileName = fileName;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<ReportRun>> ListMissingContentAsync(int workspaceId, int? reportId = null, int take = 10000)
    {
        var q = db.ReportRuns.AsNoTracking().Where(r => r.WorkspaceId == workspaceId && r.FileBytes == null && r.FilePath != null && r.Status == "Succeeded");
        if (reportId.HasValue) q = q.Where(r => r.ReportId == reportId.Value);
        return await q.OrderByDescending(r => r.StartedAt).Take(take).ToListAsync();
    }
}
