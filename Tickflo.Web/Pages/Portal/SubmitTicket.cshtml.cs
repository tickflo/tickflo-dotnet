using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Contacts;
using Tickflo.Core.Services.Tickets;

namespace Tickflo.Web.Pages.Portal;

/// <summary>
/// Public workspace portal for ticket submission with contact creation.
/// Allows external users to submit tickets by providing contact information.
/// </summary>
[AllowAnonymous]
public class SubmitTicketModel : PageModel
{
    private const int SystemUserId = 0;
    private const string DefaultTicketPriority = "Normal";
    private const string DefaultTicketType = "Standard";
    private const string DefaultTicketStatus = "New";
    private const string PortalSubmissionNote = "Created via workspace portal";

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IContactRepository _contactRepo;
    private readonly IContactRegistrationService _contactRegistrationService;
    private readonly ITicketCreationService _ticketCreationService;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;

    public SubmitTicketModel(
        IWorkspaceRepository workspaceRepo,
        IContactRepository contactRepo,
        IContactRegistrationService contactRegistrationService,
        ITicketCreationService ticketCreationService,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo)
    {
        _workspaceRepo = workspaceRepo;
        _contactRepo = contactRepo;
        _contactRegistrationService = contactRegistrationService;
        _ticketCreationService = ticketCreationService;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
    }

    public Workspace? Workspace { get; private set; }
    public bool PortalEnabled { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }
    public IReadOnlyList<TicketPriority> Priorities { get; private set; } = Array.Empty<TicketPriority>();
    public IReadOnlyList<TicketType> Types { get; private set; } = Array.Empty<TicketType>();

    // Contact Form Fields
    [BindProperty]
    public string ContactName { get; set; } = string.Empty;

    [BindProperty]
    public string ContactEmail { get; set; } = string.Empty;

    [BindProperty]
    public string? ContactPhone { get; set; }

    [BindProperty]
    public string? ContactCompany { get; set; }

    // Ticket Form Fields
    [BindProperty]
    public string TicketSubject { get; set; } = string.Empty;

    [BindProperty]
    public string TicketDescription { get; set; } = string.Empty;

    [BindProperty]
    public string? TicketPriority { get; set; }

    [BindProperty]
    public string? TicketType { get; set; }

    public async Task<IActionResult> OnGetAsync([FromRoute] string token)
    {
        var (workspace, validationResult) = await ValidatePortalAccessAsync(token);
        if (workspace == null)
        {
            ErrorMessage = validationResult;
            return Page();
        }

        Workspace = workspace;
        PortalEnabled = true;
        await LoadFormDataAsync(workspace.Id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromRoute] string token)
    {
        var (workspace, validationResult) = await ValidatePortalAccessAsync(token);
        if (workspace == null)
        {
            ErrorMessage = validationResult;
            return Page();
        }

        Workspace = workspace;
        PortalEnabled = true;
        await LoadFormDataAsync(workspace.Id);

        // Validate required fields
        var validationError = ValidateFormInput();
        if (validationError != null)
        {
            ErrorMessage = validationError;
            return Page();
        }

        try
        {
            var contact = await EnsureContactExistsAsync(workspace.Id);
            var ticket = await CreateTicketAsync(workspace.Id, contact.Id);

            SuccessMessage = $"Ticket #{ticket.Id} has been submitted successfully! We'll be in touch soon.";
            ClearForm();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error submitting ticket: {ex.Message}";
        }

        return Page();
    }

    /// <summary>
    /// Validates workspace portal access by token.
    /// </summary>
    /// <returns>Workspace if valid, null if invalid. ValidationResult contains error message if null.</returns>
    private async Task<(Workspace?, string)> ValidatePortalAccessAsync(string token)
    {
        var workspace = await _workspaceRepo.FindByPortalTokenAsync(token);
        if (workspace == null)
            return (null, "Invalid portal link.");

        if (!workspace.PortalEnabled)
            return (null, "This workspace portal is currently disabled.");

        return (workspace, string.Empty);
    }

    /// <summary>
    /// Loads form data (priorities and types) for the workspace.
    /// </summary>
    private async Task LoadFormDataAsync(int workspaceId)
    {
        Priorities = await _priorityRepo.ListAsync(workspaceId);
        Types = await _typeRepo.ListAsync(workspaceId);
    }

    /// <summary>
    /// Validates user input from the form.
    /// </summary>
    /// <returns>Error message if validation fails, null if valid.</returns>
    private string? ValidateFormInput()
    {
        if (string.IsNullOrWhiteSpace(ContactName))
            return "Contact name is required.";

        if (string.IsNullOrWhiteSpace(ContactEmail))
            return "Contact email is required.";

        if (string.IsNullOrWhiteSpace(TicketSubject))
            return "Ticket subject is required.";

        return null;
    }

    /// <summary>
    /// Ensures a contact exists for the given email, creating or updating as needed.
    /// </summary>
    private async Task<Contact> EnsureContactExistsAsync(int workspaceId)
    {
        var contacts = await _contactRepo.ListAsync(workspaceId);
        var existingContact = FindContactByEmail(contacts, ContactEmail);

        if (existingContact != null)
            return await UpdateExistingContactAsync(workspaceId, existingContact);

        return await CreateNewContactAsync(workspaceId);
    }

    /// <summary>
    /// Finds a contact by email address (case-insensitive).
    /// </summary>
    private Contact? FindContactByEmail(IReadOnlyList<Contact> contacts, string email)
    {
        return contacts.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.Email) &&
            c.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Updates an existing contact with new information.
    /// </summary>
    private async Task<Contact> UpdateExistingContactAsync(int workspaceId, Contact contact)
    {
        await _contactRegistrationService.UpdateContactInformationAsync(
            workspaceId,
            contact.Id,
            new ContactUpdateRequest
            {
                Name = ContactName.Trim(),
                Phone = NormalizeOptionalString(ContactPhone),
                Company = NormalizeOptionalString(ContactCompany)
            },
            SystemUserId);

        return contact;
    }

    /// <summary>
    /// Creates a new contact with the provided information.
    /// </summary>
    private async Task<Contact> CreateNewContactAsync(int workspaceId)
    {
        return await _contactRegistrationService.RegisterContactAsync(
            workspaceId,
            new ContactRegistrationRequest
            {
                Name = ContactName.Trim(),
                Email = ContactEmail.Trim(),
                Phone = NormalizeOptionalString(ContactPhone),
                Company = NormalizeOptionalString(ContactCompany),
                Notes = PortalSubmissionNote
            },
            SystemUserId);
    }

    /// <summary>
    /// Creates a ticket for the contact.
    /// </summary>
    private async Task<Ticket> CreateTicketAsync(int workspaceId, int contactId)
    {
        return await _ticketCreationService.CreateTicketAsync(
            workspaceId,
            new TicketCreationRequest
            {
                Subject = TicketSubject.Trim(),
                Description = NormalizeOptionalString(TicketDescription) ?? string.Empty,
                Priority = TicketPriority?.Trim() ?? DefaultTicketPriority,
                Type = TicketType?.Trim() ?? DefaultTicketType,
                Status = DefaultTicketStatus,
                ContactId = contactId
            },
            SystemUserId);
    }

    /// <summary>
    /// Normalizes optional string input by trimming whitespace.
    /// </summary>
    /// <returns>Trimmed string or null if input is empty.</returns>
    private static string? NormalizeOptionalString(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    /// <summary>
    /// Clears all form fields after successful submission.
    /// </summary>
    private void ClearForm()
    {
        ContactName = string.Empty;
        ContactEmail = string.Empty;
        ContactPhone = null;
        ContactCompany = null;
        TicketSubject = string.Empty;
        TicketDescription = string.Empty;
        TicketPriority = null;
        TicketType = null;
    }
}

