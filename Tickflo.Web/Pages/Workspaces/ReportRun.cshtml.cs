using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Web.Services;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportRunModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IReportingService _reportingService;

    public ReportRunModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IReportRunRepository reportRunRepo, IReportingService reportingService)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _reportingService = reportingService;
    }

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var rep = await _reportRepo.FindAsync(ws.Id, reportId);
        if (rep == null) return NotFound();

        var run = await _reportRunRepo.CreateAsync(new ReportRun
        {
            WorkspaceId = ws.Id,
            ReportId = rep.Id,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        });
        await _reportRunRepo.MarkRunningAsync(run.Id);
        try
        {
            var res = await _reportingService.ExecuteAsync(ws.Id, rep);
            await _reportRunRepo.CompleteAsync(run.Id, "Succeeded", res.RowCount, null, res.Bytes, res.ContentType, res.FileName);
            rep.LastRun = DateTime.UtcNow;
            await _reportRepo.UpdateAsync(rep);
            TempData["Success"] = "Report run completed.";
        }
        catch
        {
            await _reportRunRepo.CompleteAsync(run.Id, "Failed", 0, null);
            TempData["Success"] = "Report run failed.";
        }
        return RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }
}
