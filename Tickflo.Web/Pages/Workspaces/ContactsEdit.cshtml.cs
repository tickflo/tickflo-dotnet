using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ContactsEditModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userWorkspaceRoleRepo;
    private readonly IContactRepository _contactRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly IRolePermissionRepository _rolePerms;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }
    public int Id { get; private set; }
    public bool CanViewContacts { get; private set; }
    public bool CanEditContacts { get; private set; }
    public bool CanCreateContacts { get; private set; }

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string? Phone { get; set; }
    [BindProperty] public string? Company { get; set; }
    [BindProperty] public string? Title { get; set; }
    [BindProperty] public string? Notes { get; set; }
    [BindProperty] public string? Tags { get; set; }
    [BindProperty] public string? PreferredChannel { get; set; }
    [BindProperty] public string? Priority { get; set; }

    public ContactsEditModel(IWorkspaceRepository workspaceRepo, IUserWorkspaceRoleRepository userWorkspaceRoleRepo, IContactRepository contactRepo, ITicketPriorityRepository priorityRepo, IRolePermissionRepository rolePerms)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRoleRepo = userWorkspaceRoleRepo;
        _contactRepo = contactRepo;
        _priorityRepo = priorityRepo;
        _rolePerms = rolePerms;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        // Compute effective permissions for contacts
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        if (isAdmin)
        {
            CanViewContacts = CanEditContacts = CanCreateContacts = true;
        }
        else if (eff.TryGetValue("contacts", out var cp))
        {
            CanViewContacts = cp.CanView;
            CanEditContacts = cp.CanEdit;
            CanCreateContacts = cp.CanCreate;
        }
        // Restrict page view if lacking view permission
        if (!CanViewContacts) return Forbid();
        if (id > 0)
        {
            var contact = await _contactRepo.FindAsync(workspaceId, id);
            if (contact == null) return NotFound();
            Name = contact.Name ?? string.Empty;
            Email = contact.Email ?? string.Empty;
            Phone = contact.Phone;
            Company = contact.Company;
            Title = contact.Title;
            Notes = contact.Notes;
            Tags = contact.Tags;
            PreferredChannel = contact.PreferredChannel;
            Priority = contact.Priority;
        }
        else
        {
            // defaults for new contact
            Name = string.Empty;
            Email = string.Empty;
            Phone = null;
            Company = null;
            Title = null;
            Notes = null;
            Tags = null;
            PreferredChannel = null;
            Priority = null;
        }
        ViewData["Priorities"] = await _priorityRepo.ListAsync(workspaceId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        if (!TryGetUserId(out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        var eff = await _rolePerms.GetEffectivePermissionsForUserAsync(workspaceId, uid);
        bool allowed = isAdmin;
        if (!allowed && eff.TryGetValue("contacts", out var cp))
        {
            allowed = (id == 0) ? cp.CanCreate : cp.CanEdit;
        }
        if (!allowed) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewData["Priorities"] = await _priorityRepo.ListAsync(workspaceId);
            return Page();
        }
        var nameTrim = Name?.Trim() ?? string.Empty;
        var emailTrim = Email?.Trim() ?? string.Empty;
        var phoneTrim = string.IsNullOrWhiteSpace(Phone) ? null : Phone!.Trim();
        var companyTrim = string.IsNullOrWhiteSpace(Company) ? null : Company!.Trim();
        var titleTrim = string.IsNullOrWhiteSpace(Title) ? null : Title!.Trim();
        var notesTrim = string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim();
        var tagsTrim = string.IsNullOrWhiteSpace(Tags) ? null : Tags!.Trim();
        var channelTrim = string.IsNullOrWhiteSpace(PreferredChannel) ? null : PreferredChannel!.Trim();
        var priorityTrim = string.IsNullOrWhiteSpace(Priority) ? null : Priority!.Trim();
        if (id == 0)
        {
            var created = await _contactRepo.CreateAsync(new Contact
            {
                WorkspaceId = workspaceId,
                Name = nameTrim,
                Email = emailTrim,
                Phone = phoneTrim,
                Company = companyTrim,
                Title = titleTrim,
                Notes = notesTrim,
                Tags = tagsTrim,
                PreferredChannel = channelTrim,
                Priority = priorityTrim
            });
            TempData["Success"] = $"Contact '{Name}' created.";
        }
        else
        {
            var existing = await _contactRepo.FindAsync(workspaceId, id);
            if (existing == null) return NotFound();
            existing.Name = nameTrim;
            existing.Email = emailTrim;
            existing.Phone = phoneTrim;
            existing.Company = companyTrim;
            existing.Title = titleTrim;
            existing.Notes = notesTrim;
            existing.Tags = tagsTrim;
            existing.PreferredChannel = channelTrim;
            existing.Priority = priorityTrim;
            await _contactRepo.UpdateAsync(existing);
            TempData["Success"] = $"Contact '{Name}' updated.";
        }
        var priorityQ = Request.Query["Priority"].ToString();
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Contacts", new { slug, Priority = priorityQ, Query = queryQ, PageNumber = pageQ });
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
}