namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;

using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunDownloadModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunDownloadViewService downloadViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IWorkspaceReportRunDownloadViewService _downloadViewService = downloadViewService;

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId)
    {
        var ws = await this._workspaceRepo.FindBySlugAsync(slug);
        if (ws == null)
        {
            return this.NotFound();
        }

        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this._downloadViewService.BuildAsync(ws.Id, uid, reportId, runId);
        if (this.EnsurePermissionOrForbid(data.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        var run = data.Run;
        if (run == null || run.FileBytes == null || run.FileBytes.Length == 0)
        {
            return this.NotFound();
        }

        var ct = string.IsNullOrWhiteSpace(run.ContentType) ? "text/csv" : run.ContentType!;
        var name = string.IsNullOrWhiteSpace(run.FileName) ? $"report_{run.Id}.csv" : run.FileName!;
        return this.File(run.FileBytes, ct, name);
    }

}

