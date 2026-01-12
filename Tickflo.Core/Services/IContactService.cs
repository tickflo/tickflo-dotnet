using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for managing contacts.
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Creates a new contact.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="request">Contact creation details</param>
    /// <returns>Created contact</returns>
    Task<Contact> CreateContactAsync(int workspaceId, CreateContactRequest request);

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="contactId">Contact to update</param>
    /// <param name="request">Update details</param>
    /// <returns>Updated contact</returns>
    Task<Contact> UpdateContactAsync(int workspaceId, int contactId, UpdateContactRequest request);

    /// <summary>
    /// Deletes a contact.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="contactId">Contact to delete</param>
    Task DeleteContactAsync(int workspaceId, int contactId);

    /// <summary>
    /// Validates that a contact name is unique within a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Contact name to check</param>
    /// <param name="excludeContactId">Optional contact ID to exclude from check</param>
    /// <returns>True if name is unique</returns>
    Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeContactId = null);
}

public class CreateContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}

public class UpdateContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? Notes { get; set; }
}
