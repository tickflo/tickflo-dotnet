using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ReportRunsBackfillModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IRolePermissionRepository _rolePerms;
    private readonly IWebHostEnvironment _env;

    public ReportRunsBackfillModel(IWorkspaceRepository workspaceRepo, IReportRunRepository reportRunRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IRolePermissionRepository rolePerms, IWebHostEnvironment env)
    {
        _workspaceRepo = workspaceRepo;
        _reportRunRepo = reportRunRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _rolePerms = rolePerms;
        _env = env;
    }

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Core.Entities.Workspace? Workspace { get; private set; }

    public string? Message { get; private set; }
    public bool Success { get; private set; }

    public BackfillSummary? Summary { get; private set; }
    public record BackfillSummary(int TotalMissing, int Imported, int MissingOnDisk, int Errors);

    public Task<IActionResult> OnGetAsync(string slug)
        => Task.FromResult<IActionResult>(NotFound());

    public Task<IActionResult> OnPostAsync(string slug)
        => Task.FromResult<IActionResult>(NotFound());
}
