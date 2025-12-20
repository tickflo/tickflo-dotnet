using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Data;

public class ReportRepository(TickfloDbContext db) : IReportRepository
{
    public async Task<IReadOnlyList<Report>> ListAsync(int workspaceId)
        => await db.Reports.Where(r => r.WorkspaceId == workspaceId).OrderBy(r => r.Name).ToListAsync();

    public async Task<Report?> FindAsync(int workspaceId, int id)
        => await db.Reports.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);

    public async Task<Report> CreateAsync(Report report)
    {
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    public async Task<Report?> UpdateAsync(Report report)
    {
        var existing = await FindAsync(report.WorkspaceId, report.Id);
        if (existing == null) return null;
        existing.Name = report.Name;
        existing.Ready = report.Ready;
        existing.LastRun = report.LastRun;
        existing.DefinitionJson = report.DefinitionJson;
        existing.ScheduleEnabled = report.ScheduleEnabled;
        existing.ScheduleType = report.ScheduleType;
        existing.ScheduleTime = report.ScheduleTime;
        existing.ScheduleDayOfWeek = report.ScheduleDayOfWeek;
        existing.ScheduleDayOfMonth = report.ScheduleDayOfMonth;
        await db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id)
    {
        var rep = await FindAsync(workspaceId, id);
        if (rep == null) return false;
        db.Reports.Remove(rep);
        await db.SaveChangesAsync();
        return true;
    }
}
