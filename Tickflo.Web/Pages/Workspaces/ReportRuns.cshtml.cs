namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Views;

[Authorize]
public class ReportRunsModel(IWorkspaceRepository workspaceRepository, IWorkspaceReportRunsViewService workspaceReportRunsViewService) : WorkspacePageModel
{
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IWorkspaceReportRunsViewService workspaceReportRunsViewService = workspaceReportRunsViewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int ReportId { get; private set; }
    public Report? Report { get; private set; }
    public List<ReportRun> Runs { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, int reportId)
    {
        this.WorkspaceSlug = slug;
        this.Workspace = await this.workspaceRepository.FindBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var userId))
        {
            return this.Forbid();
        }

        var viewData = await this.workspaceReportRunsViewService.BuildAsync(this.Workspace.Id, userId, reportId);
        if (this.EnsurePermissionOrForbid(viewData.CanViewReports) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (viewData.Report == null)
        {
            return this.NotFound();
        }

        this.ReportId = viewData.Report.Id;
        this.Report = viewData.Report;
        this.Runs = viewData.Runs;
        return this.Page();
    }
}

