using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRunService _reportRunService;
    private readonly IWorkspaceReportRunExecuteViewService _viewService;

    public ReportRunModel(IWorkspaceRepository workspaceRepo, IReportRunService reportRunService, IWorkspaceReportRunExecuteViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _reportRunService = reportRunService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();

        if (!TryGetUserId(out var userId)) return Forbid();
        var viewData = await _viewService.BuildAsync(ws.Id, userId);
        if (!viewData.CanEditReports) return Forbid();

        var run = await _reportRunService.RunReportAsync(ws.Id, reportId);
        TempData["Success"] = run?.Status == "Succeeded" ? "Report run completed." : "Report run failed.";
        return RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
