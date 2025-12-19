using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class LocationsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ILocationRepository _locationRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRolePermissionRepository _rolePerms;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty]
    public int LocationId { get; set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string Address { get; set; } = string.Empty;
    [BindProperty]
    public bool Active { get; set; } = true;

    public LocationsEditModel(IWorkspaceRepository workspaceRepo, ILocationRepository locationRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor, IRolePermissionRepository rolePerms)
    {
        _workspaceRepo = workspaceRepo;
        _locationRepo = locationRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
        _rolePerms = rolePerms;
    }
    public bool CanViewLocations { get; private set; }
    public bool CanEditLocations { get; private set; }
    public bool CanCreateLocations { get; private set; }

    public async Task<IActionResult> OnGetAsync(string slug, int locationId = 0)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        if (isAdmin)
        {
            CanViewLocations = CanEditLocations = CanCreateLocations = true;
        }
        else if (eff.TryGetValue("locations", out var lp))
        {
            CanViewLocations = lp.CanView;
            CanEditLocations = lp.CanEdit;
            CanCreateLocations = lp.CanCreate;
        }
        if (!CanViewLocations) return Forbid();

        if (locationId > 0)
        {
            var loc = await _locationRepo.FindAsync(workspaceId, locationId);
            if (loc == null) return NotFound();
            LocationId = loc.Id;
            Name = loc.Name ?? string.Empty;
            Address = loc.Address ?? string.Empty;
            Active = loc.Active;
        }
        else
        {
            LocationId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Active = true;
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
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        bool allowed = isAdmin;
        if (!allowed && eff.TryGetValue("locations", out var lp))
        {
            allowed = (LocationId == 0) ? lp.CanCreate : lp.CanEdit;
        }
        if (!allowed) return Forbid();
        if (!ModelState.IsValid) return Page();

        var nameTrim = Name?.Trim() ?? string.Empty;
        var addressTrim = Address?.Trim() ?? string.Empty;
        if (LocationId == 0)
        {
            await _locationRepo.CreateAsync(new Location { WorkspaceId = workspaceId, Name = nameTrim, Address = addressTrim, Active = Active });
            TempData["Success"] = $"Location '{Name}' created successfully.";
        }
        else
        {
            var updated = await _locationRepo.UpdateAsync(new Location { Id = LocationId, WorkspaceId = workspaceId, Name = nameTrim, Address = addressTrim, Active = Active });
            if (updated == null) return NotFound();
            TempData["Success"] = $"Location '{Name}' updated successfully.";
        }
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}
