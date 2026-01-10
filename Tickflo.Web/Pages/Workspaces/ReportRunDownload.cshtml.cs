using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunDownloadModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IWebHostEnvironment _env;

    public ReportRunDownloadModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IReportRunRepository reportRunRepo, IWebHostEnvironment env)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _env = env;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var rep = await _reportRepo.FindAsync(ws.Id, reportId);
        if (rep == null) return NotFound();
        var run = await _reportRunRepo.FindAsync(ws.Id, runId);
        if (run == null) return NotFound();
        if (run.FileBytes == null || run.FileBytes.Length == 0) return NotFound();
        var ct = string.IsNullOrWhiteSpace(run.ContentType) ? "text/csv" : run.ContentType!;
        var name = string.IsNullOrWhiteSpace(run.FileName) ? $"report_{run.Id}.csv" : run.FileName!;
        return File(run.FileBytes, ct, name);
    }
}
