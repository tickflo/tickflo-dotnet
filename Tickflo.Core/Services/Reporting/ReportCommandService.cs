namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ReportCommandService(IReportRepository reportRepo) : IReportCommandService
{
    private readonly IReportRepository _reportRepo = reportRepo;

    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default) => this._reportRepo.FindAsync(workspaceId, reportId);

    public Task<Report> CreateAsync(Report report, CancellationToken ct = default) => this._reportRepo.CreateAsync(report);

    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default) => this._reportRepo.UpdateAsync(report);
}


