namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Data;

public class ReportQueryService(IReportRepository reportRepo) : IReportQueryService
{
    private readonly IReportRepository _reportRepo = reportRepo;

    public async Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default)
    {
        var list = await this._reportRepo.ListAsync(workspaceId);
        return list.Select(r => new ReportListItem(r.Id, r.Name, r.Ready, r.LastRun)).ToList();
    }
}


