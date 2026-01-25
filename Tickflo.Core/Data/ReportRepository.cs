namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

public interface IReportRepository
{
    public Task<IReadOnlyList<Report>> ListAsync(int workspaceId);
    public Task<Report?> FindAsync(int workspaceId, int id);
    public Task<Report> CreateAsync(Report report);
    public Task<Report?> UpdateAsync(Report report);
    public Task<bool> DeleteAsync(int workspaceId, int id);
}


public class ReportRepository(TickfloDbContext dbContext) : IReportRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<IReadOnlyList<Report>> ListAsync(int workspaceId)
        => await this.dbContext.Reports.Where(r => r.WorkspaceId == workspaceId).OrderBy(r => r.Name).ToListAsync();

    public async Task<Report?> FindAsync(int workspaceId, int id)
        => await this.dbContext.Reports.FirstOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == id);

    public async Task<Report> CreateAsync(Report report)
    {
        this.dbContext.Reports.Add(report);
        await this.dbContext.SaveChangesAsync();
        return report;
    }

    public async Task<Report?> UpdateAsync(Report report)
    {
        var existing = await this.FindAsync(report.WorkspaceId, report.Id);
        if (existing == null)
        {
            return null;
        }

        existing.Name = report.Name;
        existing.Ready = report.Ready;
        existing.LastRun = report.LastRun;
        existing.DefinitionJson = report.DefinitionJson;
        existing.ScheduleEnabled = report.ScheduleEnabled;
        existing.ScheduleType = report.ScheduleType;
        existing.ScheduleTime = report.ScheduleTime;
        existing.ScheduleDayOfWeek = report.ScheduleDayOfWeek;
        existing.ScheduleDayOfMonth = report.ScheduleDayOfMonth;
        await this.dbContext.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int workspaceId, int id)
    {
        var rep = await this.FindAsync(workspaceId, id);
        if (rep == null)
        {
            return false;
        }

        this.dbContext.Reports.Remove(rep);
        await this.dbContext.SaveChangesAsync();
        return true;
    }
}
