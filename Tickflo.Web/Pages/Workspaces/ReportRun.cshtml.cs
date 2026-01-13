using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunModel : WorkspacePageModel
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
        if (EnsurePermissionOrForbid(viewData.CanEditReports) is IActionResult permCheck) return permCheck;

        var run = await _reportRunService.RunReportAsync(ws.Id, reportId);
        SetSuccessMessage(run?.Status == "Succeeded" ? "Report run completed." : "Report run failed.");
        return RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }


}


