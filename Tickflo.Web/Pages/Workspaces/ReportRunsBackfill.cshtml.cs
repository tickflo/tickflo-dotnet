namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ReportRunsBackfillModel(IWorkspaceService workspaceService, IWorkspaceReportRunsBackfillViewService workspaceReportRunsBackfillViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceReportRunsBackfillViewService workspaceReportRunsBackfillViewService = workspaceReportRunsBackfillViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Core.Entities.Workspace? Workspace { get; private set; }

    public string? Message { get; private set; }
    public bool Success { get; private set; }

    public BackfillSummary? Summary { get; private set; }
    public record BackfillSummary(int TotalMissing, int Imported, int MissingOnDisk, int Errors);

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var data = await this.workspaceReportRunsBackfillViewService.BuildAsync(this.Workspace.Id, uid);
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
        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var uid))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(uid, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var data = await this.workspaceReportRunsBackfillViewService.BuildAsync(this.Workspace.Id, uid);
        if (this.EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        return this.NotFound();
    }
}

