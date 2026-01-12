using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceReportsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ReportSummary> Reports { get; private set; } = new();
    public bool CanCreateReports { get; private set; }
    public bool CanEditReports { get; private set; }

    public ReportsModel(
        IWorkspaceRepository workspaceRepo,
        ICurrentUserService currentUserService,
        IWorkspaceReportsViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _currentUserService = currentUserService;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        
        var result = await LoadWorkspaceAndUserOrExitAsync(_workspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;

        var viewData = await _viewService.BuildAsync(Workspace.Id, uid);
        Reports = viewData.Reports;
        CanCreateReports = viewData.CanCreateReports;
        CanEditReports = viewData.CanEditReports;

        return Page();
    }
}
