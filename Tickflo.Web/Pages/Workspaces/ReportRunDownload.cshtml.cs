using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunDownloadModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceReportRunDownloadViewService _downloadViewService;

    public ReportRunDownloadModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunDownloadViewService downloadViewService)
    {
        _workspaceRepo = workspaceRepo;
        _downloadViewService = downloadViewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _downloadViewService.BuildAsync(ws.Id, uid, reportId, runId);
        if (!data.CanViewReports) return Forbid();
        var run = data.Run;
        if (run == null || run.FileBytes == null || run.FileBytes.Length == 0) return NotFound();
        var ct = string.IsNullOrWhiteSpace(run.ContentType) ? "text/csv" : run.ContentType!;
        var name = string.IsNullOrWhiteSpace(run.FileName) ? $"report_{run.Id}.csv" : run.FileName!;
        return File(run.FileBytes, ct, name);
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
