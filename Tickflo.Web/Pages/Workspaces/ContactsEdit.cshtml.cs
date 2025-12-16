using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ContactsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int Id { get; private set; }

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Company { get; set; }
    [BindProperty] public string? Title { get; set; }
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? Tags { get; set; }
    [BindProperty] public string? PreferredChannel { get; set; }
    [BindProperty] public string? Priority { get; set; }

    public ContactsEditModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IContactRepository contactRepo, ITicketPriorityRepository priorityRepo)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _contactRepo = contactRepo;
        _priorityRepo = priorityRepo;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Id = id;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        var contact = await _contactRepo.FindAsync(Workspace.Id, id);
        if (contact == null) return NotFound();
        Name = contact.Name;
        Email = contact.Email;
        Phone = contact.Phone;
        Company = contact.Company;
        Title = contact.Title;
        Notes = contact.Notes;
        Tags = contact.Tags;
        PreferredChannel = contact.PreferredChannel;
        Priority = contact.Priority;
        ViewData["Priorities"] = await _priorityRepo.ListAsync(Workspace.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id)
    {
        WorkspaceSlug = slug;
        Id = id;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, Workspace.Id);
        if (!isAdmin) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewData["Priorities"] = await _priorityRepo.ListAsync(Workspace.Id);
            return Page();
        }
        var existing = await _contactRepo.FindAsync(Workspace.Id, id);
        if (existing == null) return NotFound();
        existing.Name = Name;
        existing.Email = Email;
        existing.Phone = Phone;
        existing.Company = Company;
        existing.Title = Title;
        existing.Notes = Notes;
        existing.Tags = Tags;
        existing.PreferredChannel = PreferredChannel;
        existing.Priority = Priority;
        await _contactRepo.UpdateAsync(existing);
        TempData["Success"] = $"Contact '{Name}' updated.";
        return RedirectToPage("/Workspaces/Contacts", new { slug });
    }
}