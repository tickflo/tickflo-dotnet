namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunModel(IWorkspaceRepository workspaceRepo, IReportRunService reportRunService, IWorkspaceReportRunExecuteViewService viewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IReportRunService _reportRunService = reportRunService;
    private readonly IWorkspaceReportRunExecuteViewService _viewService = viewService;

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var ws = await this.workspaceRepository.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var viewData = await this._viewService.BuildAsync(ws.Id, userId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        var run = await this._reportRunService.RunReportAsync(ws.Id, reportId);
        this.SetSuccessMessage(run?.Status == "Succeeded" ? "Report run completed." : "Report run failed.");
        return this.RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }


}


