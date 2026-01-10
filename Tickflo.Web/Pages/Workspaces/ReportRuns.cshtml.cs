using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int ReportId { get; private set; }
    public Report? Report { get; private set; }
    public List<ReportRun> Runs { get; private set; } = new();

    public ReportRunsModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IReportRunRepository reportRunRepo)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var rep = await _reportRepo.FindAsync(Workspace.Id, reportId);
        if (rep == null) return NotFound();
        ReportId = rep.Id;
        Report = rep;
        Runs = (await _reportRunRepo.ListForReportAsync(Workspace.Id, rep.Id, 100)).ToList();
        return Page();
    }
}
