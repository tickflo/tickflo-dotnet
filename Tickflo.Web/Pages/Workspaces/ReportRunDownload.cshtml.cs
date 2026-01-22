namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ReportRunDownloadModel(IWorkspaceService workspaceService, IWorkspaceReportRunDownloadViewService workspaceReportRunDownloadViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceReportRunDownloadViewService workspaceReportRunDownloadViewService = workspaceReportRunDownloadViewService;

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId)
    {
        var workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null)
        {
            return this.NotFound();
        }

        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var data = await this.workspaceReportRunDownloadViewService.BuildAsync(workspace.Id, uid, reportId, runId);
        if (this.EnsurePermissionOrForbid(data.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        var run = data.Run;
        if (run == null || run.FileBytes == null || run.FileBytes.Length == 0)
        {
            return this.NotFound();
        }

        var ct = string.IsNullOrWhiteSpace(run.ContentType) ? "text/csv" : run.ContentType;
        var name = string.IsNullOrWhiteSpace(run.FileName) ? $"report_{run.Id}.csv" : run.FileName;
        return this.File(run.FileBytes, ct, name);
    }

}

