using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportsModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IRolePermissionRepository _rolePerms;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public List<ReportItem> Reports { get; private set; } = new();

    public ReportsModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IRolePermissionRepository rolePerms)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _rolePerms = rolePerms;
    }

    public bool CanCreateReports { get; private set; }
    public bool CanEditReports { get; private set; }

    public async Task OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace != null && TryGetUserId(out var uid))
        {
            var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(Workspace.Id, uid);
            if (eff.TryGetValue("reports", out var rp))
            {
                CanCreateReports = rp.CanCreate;
                CanEditReports = rp.CanEdit;
            }
        }
        Reports = Workspace != null
            ? (await _reportRepo.ListAsync(Workspace.Id)).Select(r => new ReportItem { Id = r.Id, Name = r.Name, Ready = r.Ready, LastRun = r.LastRun }).ToList()
            : new List<ReportItem>();
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

    public record ReportItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Ready { get; set; }
        public DateTime? LastRun { get; set; }
    }
}
