namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Entities;

public interface IReportCommandService
{
    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<Report> CreateAsync(Report report, CancellationToken ct = default);
    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default);
}


