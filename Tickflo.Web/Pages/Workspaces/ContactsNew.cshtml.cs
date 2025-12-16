using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace Tickflo.Web.Pages.Workspaces;

public class ContactsNewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string Email { get; set; } = string.Empty;
    [BindProperty]
    public string? Phone { get; set; }
    [BindProperty]
    public string? Company { get; set; }
    [BindProperty]
    public string? Title { get; set; }
    [BindProperty]
    public string? Notes { get; set; }
    [BindProperty]
    public string? Tags { get; set; }
    [BindProperty]
    public string? PreferredChannel { get; set; }
    [BindProperty]
    public string? Priority { get; set; }

    public ContactsNewModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRepository userWorkspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IHttpContextAccessor httpContextAccessor)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, [FromServices] IContactRepository contactRepo)
    {
        WorkspaceSlug = slug;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        if (!ModelState.IsValid)
        {
            return Page();
        }
        var contact = new Contact
        {
            WorkspaceId = Workspace.Id,
            Name = Name,
            Email = Email,
            Phone = Phone,
            Company = Company,
            Title = Title,
            Notes = Notes,
            Tags = Tags,
            PreferredChannel = PreferredChannel,
            Priority = Priority,
            Status = "Active"
        };
        await contactRepo.CreateAsync(contact);
        TempData["Success"] = $"Contact '{Name}' created successfully.";
        return RedirectToPage("/Workspaces/Contacts", new { slug });
    }
}
