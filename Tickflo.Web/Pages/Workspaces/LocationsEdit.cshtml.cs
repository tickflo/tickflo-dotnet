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
    [BindProperty]
    public int? DefaultAssigneeUserId { get; set; }
    public List<User> MemberOptions { get; private set; } = new();
    [BindProperty]
    public List<int> SelectedContactIds { get; set; } = new();
    public List<Contact> ContactOptions { get; private set; } = new();

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

        // Load members for default assignee selection
        MemberOptions = new();
        var memberships = await HttpContext.RequestServices.GetRequiredService<IUserWorkspaceRepository>().FindForWorkspaceAsync(Workspace.Id);
        var usersSvc = HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        foreach (var m in memberships.Select(m => m.UserId).Distinct())
        {
            var u = await usersSvc.FindByIdAsync(m);
            if (u != null) MemberOptions.Add(u);
        }

        // Load contacts and preselect those linked to this location
        var contactsSvc = HttpContext.RequestServices.GetRequiredService<IContactRepository>();
        ContactOptions = (await contactsSvc.ListAsync(Workspace.Id)).ToList();
        SelectedContactIds = new();

        if (locationId > 0)
        {
            var loc = await _locationRepo.FindAsync(workspaceId, locationId);
            if (loc == null) return NotFound();
            LocationId = loc.Id;
            Name = loc.Name ?? string.Empty;
            Address = loc.Address ?? string.Empty;
            Active = loc.Active;
            DefaultAssigneeUserId = loc.DefaultAssigneeUserId;
            var selected = await _locationRepo.ListContactIdsAsync(workspaceId, locationId);
            SelectedContactIds = selected.ToList();
        }
        else
        {
            LocationId = 0;
            Name = string.Empty;
            Address = string.Empty;
            Active = true;
            DefaultAssigneeUserId = null;
            SelectedContactIds = new();
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
        int effectiveLocationId = LocationId;
        if (LocationId == 0)
        {
            var created = await _locationRepo.CreateAsync(new Location { WorkspaceId = workspaceId, Name = nameTrim, Address = addressTrim, Active = Active, DefaultAssigneeUserId = DefaultAssigneeUserId });
            effectiveLocationId = created.Id;
            TempData["Success"] = $"Location '{Name}' created successfully.";
        }
        else
        {
            var updated = await _locationRepo.UpdateAsync(new Location { Id = LocationId, WorkspaceId = workspaceId, Name = nameTrim, Address = addressTrim, Active = Active, DefaultAssigneeUserId = DefaultAssigneeUserId });
            if (updated == null) return NotFound();
            TempData["Success"] = $"Location '{Name}' updated successfully.";
        }
        // Persist contact assignments
        await _locationRepo.SetContactsAsync(workspaceId, effectiveLocationId, SelectedContactIds ?? new List<int>());
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Locations", new { slug, Query = queryQ, PageNumber = pageQ });
    }
}
