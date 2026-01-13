using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Reporting;

public class ReportQueryService : IReportQueryService
{
    private readonly IReportRepository _reportRepo;

    public ReportQueryService(IReportRepository reportRepo)
    {
        _reportRepo = reportRepo;
    }

    public async Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default)
    {
        var list = await _reportRepo.ListAsync(workspaceId);
        return list.Select(r => new ReportListItem(r.Id, r.Name, r.Ready, r.LastRun)).ToList();
    }
}


