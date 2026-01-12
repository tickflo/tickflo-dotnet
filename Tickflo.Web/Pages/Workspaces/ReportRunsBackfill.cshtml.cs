using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunsBackfillModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IWorkspaceReportRunsBackfillViewService _backfillViewService;

    public ReportRunsBackfillModel(IWorkspaceRepository workspaceRepo, IWorkspaceReportRunsBackfillViewService backfillViewService)
    {
        _workspaceRepo = workspaceRepo;
        _backfillViewService = backfillViewService;
    }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Core.Entities.Workspace? Workspace { get; private set; }

    public string? Message { get; private set; }
    public bool Success { get; private set; }

    public BackfillSummary? Summary { get; private set; }
    public record BackfillSummary(int TotalMissing, int Imported, int MissingOnDisk, int Errors);

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        Workspace = ws;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _backfillViewService.BuildAsync(ws.Id, uid);
        if (EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck) return permCheck;
        Message = null;
        Success = false;
        Summary = null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (EnsureWorkspaceExistsOrNotFound(ws) is IActionResult result) return result;
        Workspace = ws;
        var uid = TryGetUserId(out var idVal) ? idVal : 0;
        if (uid == 0) return Forbid();
        var data = await _backfillViewService.BuildAsync(ws.Id, uid);
        if (EnsurePermissionOrForbid(data.CanEditReports) is IActionResult permCheck) return permCheck;
        // Backfill operation not yet implemented; preserve behavior
        return NotFound();
    }
}
