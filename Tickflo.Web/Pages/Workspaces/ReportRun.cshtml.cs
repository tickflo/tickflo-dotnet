namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunModel(IWorkspaceRepository workspaceRepository, IReportRunService reportRunService, IWorkspaceReportRunExecuteViewService workspaceReportRunExecuteViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IReportRunService reportRunService = reportRunService;
    private readonly IWorkspaceReportRunExecuteViewService workspaceReportRunExecuteViewService = workspaceReportRunExecuteViewService;

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

        var viewData = await this.workspaceReportRunExecuteViewService.BuildAsync(ws.Id, userId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        var run = await this.reportRunService.RunReportAsync(ws.Id, reportId);
        this.SetSuccessMessage(run?.Status == "Succeeded" ? "Report run completed." : "Report run failed.");
        return this.RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }


}


