namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services.Reporting;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ReportRunModel(IWorkspaceService workspaceService, IReportRunService reportRunService, IWorkspaceReportRunExecuteViewService workspaceReportRunExecuteViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IReportRunService reportRunService = reportRunService;
    private readonly IWorkspaceReportRunExecuteViewService workspaceReportRunExecuteViewService = workspaceReportRunExecuteViewService;

    public async Task<IActionResult> OnPostAsync(string slug, int reportId)
    {
        var workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(userId, workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceReportRunExecuteViewService.BuildAsync(workspace.Id, userId);
        if (this.EnsurePermissionOrForbid(viewData.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        var run = await this.reportRunService.RunReportAsync(workspace.Id, reportId);
        this.SetSuccessMessage(run?.Status == "Succeeded" ? "Report run completed." : "Report run failed.");
        return this.RedirectToPage("/Workspaces/ReportRuns", new { slug, reportId });
    }


}


