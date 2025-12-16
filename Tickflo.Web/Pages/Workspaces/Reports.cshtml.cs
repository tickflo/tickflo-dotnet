using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ReportItem> Reports { get; private set; } = new();

    public ReportsModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
    }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        Reports = (await _reportRepo.ListAsync(Workspace!.Id))
            .Select(r => new ReportItem { Id = r.Id, Name = r.Name, Ready = r.Ready, LastRun = r.LastRun })
            .ToList();
    }

    public record ReportItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Ready { get; set; }
        public DateTime? LastRun { get; set; }
    }
}
