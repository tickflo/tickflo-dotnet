namespace Tickflo.Core.Services.Contacts;

using System.Text;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

public class ContactRegistrationService(IContactRepository contactRepository) : IContactRegistrationService
{
    #region Constants
    private const string ContactNameRequiredError = "Contact name is required";
    private static readonly CompositeFormat ContactAlreadyExistsError = CompositeFormat.Parse("Contact '{0}' already exists in this workspace");
    private const string InvalidEmailFormatError = "Invalid email format";
    private const string ContactNotFoundError = "Contact not found";
    #endregion

    private readonly IContactRepository contactRepository = contactRepository;

    public async Task<Contact> RegisterContactAsync(int workspaceId, ContactRegistrationRequest request, int createdByUserId)
    {
        var name = ValidateAndGetContactName(request.Name);
        await this.EnsureContactNameIsUniqueAsync(workspaceId, name);
        ValidateEmailIfProvided(request.Email);

        var contact = CreateContactEntity(workspaceId, request, name);
        await this.contactRepository.CreateAsync(contact);
        return contact;
    }

    public async Task<Contact> UpdateContactInformationAsync(
        int workspaceId,
        int contactId,
        ContactUpdateRequest request,
        int updatedByUserId)
    {
        var contact = await this.GetContactOrThrowAsync(workspaceId, contactId);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            await this.UpdateContactNameIfChangedAsync(workspaceId, contactId, contact, name);
        }

        UpdateContactEmail(contact, request.Email);
        UpdateContactPhone(contact, request.Phone);
        UpdateContactCompany(contact, request.Company);
        UpdateContactNotes(contact, request.Notes);

        await this.contactRepository.UpdateAsync(contact);
        return contact;
    }

    public async Task RemoveContactAsync(int workspaceId, int contactId) => await this.contactRepository.DeleteAsync(workspaceId, contactId);

    private static string ValidateAndGetContactName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(ContactNameRequiredError);
        }

        return name.Trim();
    }

    private async Task EnsureContactNameIsUniqueAsync(int workspaceId, string name, int? excludeContactId = null)
    {
        var existingContacts = await this.contactRepository.ListAsync(workspaceId);
        var isDuplicate = existingContacts.Any(c =>
            (excludeContactId == null || c.Id != excludeContactId) &&
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
        {
            throw new InvalidOperationException(string.Format(null, ContactAlreadyExistsError, name));
        }
    }

    private static void ValidateEmailIfProvided(string? email)
    {
        var trimmedEmail = email?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedEmail) && !IsValidEmail(trimmedEmail))
        {
            throw new InvalidOperationException(InvalidEmailFormatError);
        }
    }

    private static Contact CreateContactEntity(int workspaceId, ContactRegistrationRequest request, string name) => new()
    {
        WorkspaceId = workspaceId,
        Name = name,
        Email = TrimOrDefault(request.Email, string.Empty),
        Phone = TrimOrNull(request.Phone),
        Company = TrimOrNull(request.Company),
        Notes = TrimOrNull(request.Notes),
    };

    private async Task<Contact> GetContactOrThrowAsync(int workspaceId, int contactId)
    {
        var contact = await this.contactRepository.FindAsync(workspaceId, contactId) ?? throw new InvalidOperationException(ContactNotFoundError);

        return contact;
    }

    private async Task UpdateContactNameIfChangedAsync(int workspaceId, int contactId, Contact contact, string newName)
    {
        if (!string.Equals(contact.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            await this.EnsureContactNameIsUniqueAsync(workspaceId, newName, contactId);
            contact.Name = newName;
        }
        else
        {
            contact.Name = newName;
        }
    }

    private static void UpdateContactEmail(Contact contact, string? email)
    {
        if (email != null)
        {
            ValidateEmailIfProvided(email);
            contact.Email = email.Trim();
        }
    }

    private static void UpdateContactPhone(Contact contact, string? phone)
    {
        if (phone != null)
        {
            contact.Phone = TrimOrNull(phone);
        }
    }

    private static void UpdateContactCompany(Contact contact, string? company)
    {
        if (company != null)
        {
            contact.Company = TrimOrNull(company);
        }
    }

    private static void UpdateContactNotes(Contact contact, string? notes)
    {
        if (notes != null)
        {
            contact.Notes = TrimOrNull(notes);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string TrimOrDefault(string? value, string defaultValue) =>
        string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
}

public class ContactRegistrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}

public class ContactUpdateRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}
