using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Contacts;

/// <summary>
/// Service for managing contacts.
/// </summary>
public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepo;

    public ContactService(IContactRepository contactRepo)
    {
        _contactRepo = contactRepo;
    }

    public async Task<Contact> CreateContactAsync(int workspaceId, CreateContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");

        var name = request.Name.Trim();

        if (!await IsNameUniqueAsync(workspaceId, name))
            throw new InvalidOperationException($"Contact '{name}' already exists");

        var contact = new Contact
        {
            WorkspaceId = workspaceId,
            Name = name,
            Email = string.IsNullOrWhiteSpace(request.Email) ? string.Empty : request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _contactRepo.CreateAsync(contact);

        return contact;
    }

    public async Task<Contact> UpdateContactAsync(int workspaceId, int contactId, UpdateContactRequest request)
    {
        var contact = await _contactRepo.FindAsync(workspaceId, contactId);
        if (contact == null)
            throw new InvalidOperationException("Contact not found");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");

        var name = request.Name.Trim();

        if (name != contact.Name && !await IsNameUniqueAsync(workspaceId, name, contactId))
            throw new InvalidOperationException($"Contact '{name}' already exists");

        contact.Name = name;
        contact.Email = string.IsNullOrWhiteSpace(request.Email) ? string.Empty : request.Email.Trim();
        contact.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        contact.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();
        contact.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _contactRepo.UpdateAsync(contact);

        return contact;
    }

    public async Task DeleteContactAsync(int workspaceId, int contactId)
    {
        await _contactRepo.DeleteAsync(workspaceId, contactId);
    }

    public async Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeContactId = null)
    {
        var contacts = await _contactRepo.ListAsync(workspaceId);
        var existing = contacts.FirstOrDefault(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        
        return existing == null || (excludeContactId.HasValue && existing.Id == excludeContactId.Value);
    }
}

