namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Views;

[Authorize]
public class ContactsEditModel(
    IWorkspaceRepository workspaceRepo,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceContactsEditViewService viewService,
    IContactRepository contactRepository,
    IContactRegistrationService contactRegistrationService) : WorkspacePageModel
{
    #region Constants
    private const int NewContactId = 0;
    private const string ContactCreatedMessage = "Contact '{0}' created.";
    private const string ContactUpdatedMessage = "Contact '{0}' updated.";
    #endregion

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceContactsEditViewService _viewService = viewService;
    private readonly IContactRepository contactRepository = contactRepository;
    private readonly IContactRegistrationService _contactRegistrationService = contactRegistrationService;

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

    public async Task<IActionResult> OnGetAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        this.Id = id;

        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await this._viewService.BuildAsync(workspaceId, uid, id);
        this.CanViewContacts = viewData.CanViewContacts;
        this.CanEditContacts = viewData.CanEditContacts;
        this.CanCreateContacts = viewData.CanCreateContacts;

        if (this.EnsurePermissionOrForbid(this.CanViewContacts) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (viewData.ExistingContact != null)
        {
            this.LoadContactFieldsFromEntity(viewData.ExistingContact);
        }

        this.ViewData["Priorities"] = viewData.Priorities;
        return this.Page();
    }

    public async Task<IActionResult> OnPostAsync(string slug, int id = 0)
    {
        this.WorkspaceSlug = slug;
        this.Id = id;

        var loadResult = await this.LoadWorkspaceAndValidateUserMembershipAsync(this.workspaceRepository, this.userWorkspaceRepository, slug);
        if (loadResult is IActionResult actionResult)
        {
            return actionResult;
        }

        var (workspace, uid) = (WorkspaceUserLoadResult)loadResult;
        this.Workspace = workspace;
        var workspaceId = workspace!.Id;

        var viewData = await this._viewService.BuildAsync(workspaceId, uid, id);
        var allowed = viewData.CanCreateContacts || viewData.CanEditContacts;
        if (!allowed)
        {
            return this.Forbid();
        }

        if (this.EnsureCreateOrEditPermission(id, viewData.CanCreateContacts, viewData.CanEditContacts) is IActionResult permCheck)
        {
            return permCheck;
        }

        if (!this.ModelState.IsValid)
        {
            this.ViewData["Priorities"] = viewData.Priorities;
            return this.Page();
        }

        try
        {
            var contact = id == NewContactId
                ? await this.CreateContactAsync(workspaceId, uid)
                : await this.UpdateContactAsync(workspaceId, id, uid);

            return this.RedirectToContactsWithPreservedFilters(slug);
        }
        catch (InvalidOperationException ex)
        {
            this.SetErrorMessage(ex.Message);
            var errorViewData = await this._viewService.BuildAsync(workspaceId, uid, id);
            this.ViewData["Priorities"] = errorViewData.Priorities;
            return this.Page();
        }
    }

    private void LoadContactFieldsFromEntity(Contact contact)
    {
        this.Name = contact.Name ?? string.Empty;
        this.Email = contact.Email ?? string.Empty;
        this.Phone = contact.Phone;
        this.Company = contact.Company;
        this.Title = contact.Title;
        this.Notes = contact.Notes;
        this.Tags = contact.Tags;
        this.PreferredChannel = contact.PreferredChannel;
        this.Priority = contact.Priority;
    }

    private async Task<Contact> CreateContactAsync(int workspaceId, int userId)
    {
        var trimmedFields = this.TrimContactFields();

        var created = await this._contactRegistrationService.RegisterContactAsync(workspaceId, new ContactRegistrationRequest
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
        await this.contactRepository.UpdateAsync(created);

        this.SetSuccessMessage(string.Format(ContactCreatedMessage, created.Name));
        return created;
    }

    private async Task<Contact> UpdateContactAsync(int workspaceId, int id, int userId)
    {
        var trimmedFields = this.TrimContactFields();

        var updated = await this._contactRegistrationService.UpdateContactInformationAsync(workspaceId, id, new ContactUpdateRequest
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
        await this.contactRepository.UpdateAsync(updated);

        this.SetSuccessMessage(string.Format(ContactUpdatedMessage, updated.Name));
        return updated;
    }

    private (string Name, string Email, string? Phone, string? Company, string? Notes, string? Title, string? Tags, string? PreferredChannel, string? Priority) TrimContactFields() => (
            Name: this.Name?.Trim() ?? string.Empty,
            Email: this.Email?.Trim() ?? string.Empty,
            Phone: string.IsNullOrWhiteSpace(this.Phone) ? null : this.Phone!.Trim(),
            Company: string.IsNullOrWhiteSpace(this.Company) ? null : this.Company!.Trim(),
            Notes: string.IsNullOrWhiteSpace(this.Notes) ? null : this.Notes!.Trim(),
            Title: string.IsNullOrWhiteSpace(this.Title) ? null : this.Title!.Trim(),
            Tags: string.IsNullOrWhiteSpace(this.Tags) ? null : this.Tags!.Trim(),
            PreferredChannel: string.IsNullOrWhiteSpace(this.PreferredChannel) ? null : this.PreferredChannel!.Trim(),
            Priority: string.IsNullOrWhiteSpace(this.Priority) ? null : this.Priority!.Trim()
        );

    private RedirectToPageResult RedirectToContactsWithPreservedFilters(string slug) => this.RedirectToPage("/Workspaces/Contacts", new
    {
        slug,
        Priority = this.Request.Query["Priority"].ToString(),
        Query = this.Request.Query["Query"].ToString(),
        PageNumber = this.Request.Query["PageNumber"].ToString()
    });
}
