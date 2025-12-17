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

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;
        Workspace = await _workspaceRepo.FindBySlugAsync(slug);
        if (Workspace == null) return NotFound();
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
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
        var uidStr = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid)) return Forbid();
        var workspaceId = Workspace.Id;
        var isAdmin = await _userWorkspaceRoleRepo.IsAdminAsync(uid, workspaceId);
        if (!isAdmin) return Forbid();
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
}