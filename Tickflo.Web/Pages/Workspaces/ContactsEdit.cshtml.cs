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
    #region Constants
    private const int NewContactId = 0;
    private const string ContactCreatedMessage = "Contact '{0}' created.";
    private const string ContactUpdatedMessage = "Contact '{0}' updated.";
    private const string ContactErrorHandled = "Failed to save contact. Please check the form and try again.";
    #endregion

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

        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
        CanViewContacts = viewData.CanViewContacts;
        CanEditContacts = viewData.CanEditContacts;
        CanCreateContacts = viewData.CanCreateContacts;

        if (EnsurePermissionOrForbid(CanViewContacts) is IActionResult permCheck) return permCheck;

        if (viewData.ExistingContact != null)
            LoadContactFieldsFromEntity(viewData.ExistingContact);

        ViewData["Priorities"] = viewData.Priorities;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        WorkspaceSlug = slug;
        Id = id;

        var loadResult = await LoadWorkspaceAndValidateUserMembershipAsync(_workspaceRepo, _userWorkspaceRepo, slug);
        if (loadResult is IActionResult actionResult) return actionResult;

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await _viewService.BuildAsync(workspaceId, uid, id);
        bool allowed = viewData.CanCreateContacts || viewData.CanEditContacts;
        if (!allowed) return Forbid();
        if (EnsureCreateOrEditPermission(id, viewData.CanCreateContacts, viewData.CanEditContacts) is IActionResult permCheck) return permCheck;

        if (!ModelState.IsValid)
        {
            ViewData["Priorities"] = viewData.Priorities;
            return Page();
        }

        try
        {
            var contact = id == NewContactId
                ? await CreateContactAsync(workspaceId, uid)
                : await UpdateContactAsync(workspaceId, id, uid);

            return RedirectToContactsWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            SetErrorMessage(ex.Message);
            var errorViewData = await _viewService.BuildAsync(workspaceId, uid, id);
            ViewData["Priorities"] = errorViewData.Priorities;
            return Page();
        }
    }

    private void LoadContactFieldsFromEntity(Contact contact)
    {
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

    private async Task<Contact> CreateContactAsync(int workspaceId, int userId)
    {
        var trimmedFields = TrimContactFields();

        var created = await _contactRegistrationService.RegisterContactAsync(workspaceId, new ContactRegistrationRequest
        {
            Name = trimmedFields.Name,
            Email = string.IsNullOrEmpty(trimmedFields.Email) ? null : trimmedFields.Email,
            Phone = trimmedFields.Phone,
            Company = trimmedFields.Company,
            Notes = trimmedFields.Notes
        }, userId);

        created.Title = trimmedFields.Title;
        created.Tags = trimmedFields.Tags;
        created.PreferredChannel = trimmedFields.PreferredChannel;
        created.Priority = trimmedFields.Priority;
        await _contactRepo.UpdateAsync(created);

        SetSuccessMessage(string.Format(ContactCreatedMessage, created.Name));
        return created;
    }

    private async Task<Contact> UpdateContactAsync(int workspaceId, int id, int userId)
    {
        var trimmedFields = TrimContactFields();

        var updated = await _contactRegistrationService.UpdateContactInformationAsync(workspaceId, id, new ContactUpdateRequest
        {
            Name = trimmedFields.Name,
            Email = string.IsNullOrEmpty(trimmedFields.Email) ? null : trimmedFields.Email,
            Phone = trimmedFields.Phone,
            Company = trimmedFields.Company,
            Notes = trimmedFields.Notes
        }, userId);

        updated.Title = trimmedFields.Title;
        updated.Tags = trimmedFields.Tags;
        updated.PreferredChannel = trimmedFields.PreferredChannel;
        updated.Priority = trimmedFields.Priority;
        await _contactRepo.UpdateAsync(updated);

        SetSuccessMessage(string.Format(ContactUpdatedMessage, updated.Name));
        return updated;
    }

    private (string Name, string Email, string? Phone, string? Company, string? Notes, string? Title, string? Tags, string? PreferredChannel, string? Priority) TrimContactFields()
    {
        return (
            Name: Name?.Trim() ?? string.Empty,
            Email: Email?.Trim() ?? string.Empty,
            Phone: string.IsNullOrWhiteSpace(Phone) ? null : Phone!.Trim(),
            Company: string.IsNullOrWhiteSpace(Company) ? null : Company!.Trim(),
            Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes!.Trim(),
            Title: string.IsNullOrWhiteSpace(Title) ? null : Title!.Trim(),
            Tags: string.IsNullOrWhiteSpace(Tags) ? null : Tags!.Trim(),
            PreferredChannel: string.IsNullOrWhiteSpace(PreferredChannel) ? null : PreferredChannel!.Trim(),
            Priority: string.IsNullOrWhiteSpace(Priority) ? null : Priority!.Trim()
        );
    }

    private RedirectToPageResult RedirectToContactsWithPreservedFilters(string slug)
    {
        return RedirectToPage("/Workspaces/Contacts", new
        {
            slug,
            Priority = Request.Query["Priority"].ToString(),
            Query = Request.Query["Query"].ToString(),
            PageNumber = Request.Query["PageNumber"].ToString()
        });
    }
}
