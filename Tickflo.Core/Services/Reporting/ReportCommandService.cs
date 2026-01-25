namespace Tickflo.Core.Services.Reporting;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public interface IReportCommandService
{
    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default);
    public Task<Report> CreateAsync(Report report, CancellationToken ct = default);
    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default);
}


public class ReportCommandService(IReportRepository reporyRepository) : IReportCommandService
{
    private readonly IReportRepository reporyRepository = reporyRepository;

    public Task<Report?> FindAsync(int workspaceId, int reportId, CancellationToken ct = default) => this.reporyRepository.FindAsync(workspaceId, reportId);

    public Task<Report> CreateAsync(Report report, CancellationToken ct = default) => this.reporyRepository.CreateAsync(report);

    public Task<Report?> UpdateAsync(Report report, CancellationToken ct = default) => this.reporyRepository.UpdateAsync(report);
}


