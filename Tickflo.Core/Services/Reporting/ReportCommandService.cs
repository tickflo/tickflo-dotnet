using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Reporting;

public class ReportCommandService : IReportCommandService
{
    private readonly IReportRepository _reportRepo;

    public ReportCommandService(IReportRepository reportRepo)
    {
        _reportRepo = reportRepo;
    }

    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default)
    {
        return _reportRepo.FindAsync(workspaceId, reportId);
    }

    public Task<Report> CreateAsync(Report report, CancellationToken ct = default)
    {
        return _reportRepo.CreateAsync(report);
    }

    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default)
    {
        return _reportRepo.UpdateAsync(report);
    }
}


