using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Contacts;

public class ContactRegistrationService : IContactRegistrationService
{
    private readonly IContactRepository _contactRepo;

    public ContactRegistrationService(IContactRepository contactRepo)
    {
        _contactRepo = contactRepo;
    }

    public async Task<Contact> RegisterContactAsync(int workspaceId, ContactRegistrationRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");

        var name = request.Name.Trim();

        var existingContacts = await _contactRepo.ListAsync(workspaceId);
        if (existingContacts.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Contact '{name}' already exists in this workspace");

        var email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
            throw new InvalidOperationException("Invalid email format");

        var contact = new Contact
        {
            WorkspaceId = workspaceId,
            Name = name,
            Email = TrimOrDefault(request.Email, string.Empty),
            Phone = TrimOrNull(request.Phone),
            Company = TrimOrNull(request.Company),
            Notes = TrimOrNull(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        await _contactRepo.CreateAsync(contact);
        return contact;
    }

    public async Task<Contact> UpdateContactInformationAsync(
        int workspaceId,
        int contactId,
        ContactUpdateRequest request,
        int updatedByUserId)
    {
        var contact = await _contactRepo.FindAsync(workspaceId, contactId);
        if (contact == null)
            throw new InvalidOperationException("Contact not found");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();

            if (!string.Equals(contact.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var existingContacts = await _contactRepo.ListAsync(workspaceId);
                if (existingContacts.Any(c => c.Id != contactId &&
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Contact '{name}' already exists in this workspace");
            }

            contact.Name = name;
        }

        if (request.Email != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
                throw new InvalidOperationException("Invalid email format");

            contact.Email = request.Email.Trim();
        }

        if (request.Phone != null)
            contact.Phone = TrimOrNull(request.Phone);

        if (request.Company != null)
            contact.Company = TrimOrNull(request.Company);

        if (request.Notes != null)
            contact.Notes = TrimOrNull(request.Notes);

        await _contactRepo.UpdateAsync(contact);
        return contact;
    }

    public async Task RemoveContactAsync(int workspaceId, int contactId)
    {
        await _contactRepo.DeleteAsync(workspaceId, contactId);
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
