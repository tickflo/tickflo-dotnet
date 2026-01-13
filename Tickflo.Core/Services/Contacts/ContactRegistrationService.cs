using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Contacts;

/// <summary>
/// Handles the business workflow of registering new contacts and updating contact information.
/// </summary>
public class ContactRegistrationService : IContactRegistrationService
{
    private readonly IContactRepository _contactRepo;

    public ContactRegistrationService(IContactRepository contactRepo)
    {
        _contactRepo = contactRepo;
    }

    /// <summary>
    /// Registers a new contact with validation and uniqueness checks.
    /// </summary>
    public async Task<Contact> RegisterContactAsync(int workspaceId, ContactRegistrationRequest request, int createdByUserId)
    {
        // Business rule: Contact name is required and must be unique
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");

        var name = request.Name.Trim();

        // Check uniqueness
        var existingContacts = await _contactRepo.ListAsync(workspaceId);
        if (existingContacts.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Contact '{name}' already exists in this workspace");

        // Business rule: Email must be valid format if provided
        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            throw new InvalidOperationException("Invalid email format");

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

    /// <summary>
    /// Updates contact information with validation.
    /// </summary>
    public async Task<Contact> UpdateContactInformationAsync(
        int workspaceId, 
        int contactId, 
        ContactUpdateRequest request, 
        int updatedByUserId)
    {
        var contact = await _contactRepo.FindAsync(workspaceId, contactId);
        if (contact == null)
            throw new InvalidOperationException("Contact not found");

        // Validate name change
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            
            // Check uniqueness if name is changing
            if (!string.Equals(contact.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var existingContacts = await _contactRepo.ListAsync(workspaceId);
                if (existingContacts.Any(c => c.Id != contactId && 
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Contact '{name}' already exists in this workspace");
            }

            contact.Name = name;
        }

        // Validate email format
        if (request.Email != null)
        {
            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
                throw new InvalidOperationException("Invalid email format");
            
            contact.Email = request.Email.Trim();
        }

        // Update other fields if provided
        if (request.Phone != null)
            contact.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (request.Company != null)
            contact.Company = string.IsNullOrWhiteSpace(request.Company) ? null : request.Company.Trim();

        if (request.Notes != null)
            contact.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _contactRepo.UpdateAsync(contact);

        return contact;
    }

    /// <summary>
    /// Removes a contact from the system.
    /// </summary>
    public async Task RemoveContactAsync(int workspaceId, int contactId)
    {
        // Could add business rules here like:
        // - Check if contact is referenced by active tickets
        // - Send notifications
        // - Archive instead of delete
        
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
}

/// <summary>
/// Request to register a new contact.
/// </summary>
public class ContactRegistrationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to update contact information.
/// </summary>
public class ContactUpdateRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}
