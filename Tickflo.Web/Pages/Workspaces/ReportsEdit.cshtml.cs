using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int ReportId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public bool Ready { get; set; }

    public ReportsEditModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();

        if (reportId > 0)
        {
            var rep = await _reportRepo.FindAsync(workspaceId, reportId);
            if (rep == null) return NotFound();
            ReportId = rep.Id;
            Name = rep.Name;
            Ready = rep.Ready;
        }
        else
        {
            ReportId = 0;
            Name = string.Empty;
            Ready = false;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        if (ReportId == 0)
        {
            await _reportRepo.CreateAsync(new Report { WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready });
            TempData["Success"] = $"Report '{Name}' created successfully.";
        }
        else
        {
            var updated = await _reportRepo.UpdateAsync(new Report { Id = ReportId, WorkspaceId = workspaceId, Name = nameTrim, Ready = Ready });
            if (updated == null) return NotFound();
            TempData["Success"] = $"Report '{Name}' updated successfully.";
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Reports", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}
