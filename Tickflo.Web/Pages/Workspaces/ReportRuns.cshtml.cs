using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

using Tickflo.Core.Services.Views;
namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunsModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceReportRunsViewService _viewService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int ReportId { get; private set; }
    public Report? Report { get; private set; }
    public List<ReportRun> Runs { get; private set; } = new();

    public ReportRunsModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunsViewService viewService)
    {
        _workspaceRepo = workspaceRepo;
        _viewService = viewService;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();

        if (!TryGetUserId(out var userId)) return Forbid();
        var viewData = await _viewService.BuildAsync(Workspace.Id, userId, reportId);
        if (EnsurePermissionOrForbid(viewData.CanViewReports) is IActionResult permCheck) return permCheck;
        if (viewData.Report == null) return NotFound();

        ReportId = viewData.Report.Id;
        Report = viewData.Report;
        Runs = viewData.Runs;
        return Page();
    }
}

