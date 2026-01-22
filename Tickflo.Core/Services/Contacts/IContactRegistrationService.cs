namespace Tickflo.Core.Services.Contacts;

using Tickflo.Core.Entities;

/// <summary>
/// Handles the business workflow of registering and managing contact information.
/// </summary>
public interface IContactRegistrationService
{
    /// <summary>
    /// Registers a new contact in the workspace with validation.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="request">Contact registration details</param>
    /// <param name="createdByUserId">User creating the contact</param>
    /// <returns>The registered contact</returns>
    public Task<Contact> RegisterContactAsync(int workspaceId, ContactRegistrationRequest request, int createdByUserId);

    /// <summary>
    /// Updates contact information.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="contactId">Contact to update</param>
    /// <param name="request">Update details</param>
    /// <param name="updatedByUserId">User making the update</param>
    /// <returns>The updated contact</returns>
    public Task<Contact> UpdateContactInformationAsync(int workspaceId, int contactId, ContactUpdateRequest request, int updatedByUserId);

    /// <summary>
    /// Removes a contact from the workspace.
    /// </summary>
    /// <param name="workspaceId">The workspace context</param>
    /// <param name="contactId">Contact to remove</param>
    public Task RemoveContactAsync(int workspaceId, int contactId);
}
