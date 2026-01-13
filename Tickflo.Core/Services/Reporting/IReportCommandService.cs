using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

public interface IReportCommandService
{
    Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default);
    Task<Report> CreateAsync(Report report, CancellationToken ct = default);
    Task<Report?> UpdateAsync(Report report, CancellationToken ct = default);
}


