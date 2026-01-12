using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunsModel : PageModel
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
        if (!viewData.CanViewReports) return Forbid();
        if (viewData.Report == null) return NotFound();

        ReportId = viewData.Report.Id;
        Report = viewData.Report;
        Runs = viewData.Runs;
        return Page();
    }

    private bool TryGetUserId(out int userId)
    {
        var idValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idValue, out userId))
        {
            return true;
        }
        userId = default;
        return false;
    }
}
