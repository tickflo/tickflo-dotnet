namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Data;

public class ReportQueryService(IReportRepository reporyRepository) : IReportQueryService
{
    private readonly IReportRepository reporyRepository = reporyRepository;

    public async Task<IReadOnlyList<ReportListItem>> ListReportsAsync(int workspaceId, CancellationToken ct = default)
    {
        var list = await this.reporyRepository.ListAsync(workspaceId);
        return [.. list.Select(r => new ReportListItem(r.Id, r.Name, r.Ready, r.LastRun))];
    }
}


