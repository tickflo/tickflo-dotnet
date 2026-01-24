namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;
using Tickflo.Core.Services.Workspace;

[Authorize]
public class ReportsModel(
    IWorkspaceService workspaceService,
    IWorkspaceReportsViewService workspaceReportsViewService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;
    private readonly IWorkspaceReportsViewService workspaceReportsViewService = workspaceReportsViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ReportSummary> Reports { get; private set; } = [];
    public bool CanCreateReports { get; private set; }
    public bool CanEditReports { get; private set; }

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

        var viewData = await this.workspaceReportsViewService.BuildAsync(this.Workspace.Id, uid);
        this.Reports = viewData.Reports;
        this.CanCreateReports = viewData.CanCreateReports;
        this.CanEditReports = viewData.CanEditReports;

        return this.Page();
    }
}

