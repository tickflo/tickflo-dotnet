namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;

using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunsBackfillModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunsBackfillViewService backfillViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo = workspaceRepo;
    private readonly IWorkspaceReportRunsBackfillViewService _backfillViewService = backfillViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Core.Entities.Workspace? Workspace { get; private set; }

    public string? Message { get; private set; }
    public bool Success { get; private set; }

    public BackfillSummary? Summary { get; private set; }
    public record BackfillSummary(int TotalMissing, int Imported, int MissingOnDisk, int Errors);

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        var ws = await this._workspaceRepo.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        this.Workspace = ws;
        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this._backfillViewService.BuildAsync(ws!.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        this.Message = null;
        this.Success = false;
        this.Summary = null;
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        var ws = await this._workspaceRepo.FindBySlugAsync(slug);
        if (this.EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result)
        {
            return result;
        }

        this.Workspace = ws;
        var uid = this.TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0)
        {
            return this.Forbid();
        }

        var data = await this._backfillViewService.BuildAsync(ws!.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        return this.NotFound();
    }
}

