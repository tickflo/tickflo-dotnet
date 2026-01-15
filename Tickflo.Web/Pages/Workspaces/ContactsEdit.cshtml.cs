using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Views;

namespace Tickflo.Web.Pages.Workspaces;

[Authorize]
public class ContactsEditModel : WorkspacePageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IWorkspaceContactsEditViewService _viewService;
    private readonly IContactRepository _contactRepo;
    private readonly IContactRegistrationService _contactRegistrationService;

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

    public ContactsEditModel(
        IWorkspaceRepository workspaceRepo, 
        IUserWorkspaceRepository userWorkspaceRepo,
        IWorkspaceContactsEditViewService viewService, 
        IContactRepository contactRepo, 
        IContactRegistrationService contactRegistrationService)
    {
        _workspaceRepo = workspaceRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _viewService = viewService;
        _contactRepo = contactRepo;
        _contactRegistrationService = contactRegistrationService;
    }

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
        CanViewContacts = viewData.CanViewContacts;
        CanEditContacts = viewData.CanEditContacts;
        CanCreateContacts = viewData.CanCreateContacts;
        
        if (EnsurePermissionOrForbid(CanViewContacts) is IActionResult permCheck) return permCheck;
        
        if (viewData.ExistingContact != null)
        {
            Name = viewData.ExistingContact.Name ?? string.Empty;
            Email = viewData.ExistingContact.Email ?? string.Empty;
            Phone = viewData.ExistingContact.Phone;
            Company = viewData.ExistingContact.Company;
            Title = viewData.ExistingContact.Title;
            Notes = viewData.ExistingContact.Notes;
            Tags = viewData.ExistingContact.Tags;
            PreferredChannel = viewData.ExistingContact.PreferredChannel;
            Priority = viewData.ExistingContact.Priority;
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
        ViewData["Priorities"] = viewData.Priorities;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;
        
        var result = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (result is IActionResult actionResult) return actionResult;
        
        var (workspace, uid) = (WorkspaceUserLoadResult)result;
        Workspace = workspace;
        var workspaceId = workspace.Id;
        
        var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
        bool allowed = viewData.CanCreateContacts || viewData.CanEditContacts;
        if (!allowed) return Forbid();
        if (EnsureCreateOrEditPermission(id, viewData.CanCreateContacts, viewData.CanEditContacts) is IActionResult permCheck) return permCheck;
        
        if (!ModelState.IsValid)
        {
            ViewData["Priorities"] = viewData.Priorities;
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
        try
        {
            if (id == 0)
            {
                // Use behavior-focused service for registration
                var created = await _contactRegistrationService.RegisterContactAsync(workspaceId, new ContactRegistrationRequest
                {
                    Name = nameTrim,
                    Email = string.IsNullOrEmpty(emailTrim) ? null : emailTrim,
                    Phone = phoneTrim,
                    Company = companyTrim,
                    Notes = notesTrim
                }, uid);
                
                // Set additional fields not covered by core registration
                created.Title = titleTrim;
                created.Tags = tagsTrim;
                created.PreferredChannel = channelTrim;
                created.Priority = priorityTrim;
                await _contactRepo.UpdateAsync(created);
                
                SetSuccessMessage($"Contact '{created.Name}' created.");
            }
            else
            {
                // Use behavior-focused service for updates
                var updated = await _contactRegistrationService.UpdateContactInformationAsync(workspaceId, id, new ContactUpdateRequest
                {
                    Name = nameTrim,
                    Email = string.IsNullOrEmpty(emailTrim) ? null : emailTrim,
                    Phone = phoneTrim,
                    Company = companyTrim,
                    Notes = notesTrim
                }, uid);
                
                // Update additional fields not covered by core update
                updated.Title = titleTrim;
                updated.Tags = tagsTrim;
                updated.PreferredChannel = channelTrim;
                updated.Priority = priorityTrim;
                await _contactRepo.UpdateAsync(updated);
                
                SetSuccessMessage($"Contact '{updated.Name}' updated.");
            }
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            var errorViewData = await _viewService.BuildAsync(workspaceId, uid, id);
            ViewData["Priorities"] = errorViewData.Priorities;
            return Page();
        }
        var priorityQ = Request.Query["Priority"].ToString();
        var queryQ = Request.Query["Query"].ToString();
        var pageQ = Request.Query["PageNumber"].ToString();
        return RedirectToPage("/Workspaces/Contacts", new { slug, Priority = priorityQ, Query = queryQ, PageNumber = pageQ });
    }
}
