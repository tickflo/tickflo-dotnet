using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for managing workspace settings including status, priority, and type configurations.
/// </summary>
public interface IWorkspaceSettingsService
{
    /// <summary>
    /// Validates and updates workspace basic settings (name, slug).
    /// </summary>
    /// <param name="workspaceId">Workspace to update</param>
    /// <param name="name">New name</param>
    /// <param name="slug">New slug</param>
    /// <returns>Updated workspace</returns>
    /// <exception cref="InvalidOperationException">If slug is already in use</exception>
    Task<Workspace> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug);

    /// <summary>
    /// Bootstraps default status/priority/type if none exist for a workspace.
    /// </summary>
    /// <param name="workspaceId">Workspace to bootstrap</param>
    Task EnsureDefaultsExistAsync(int workspaceId);

    /// <summary>
    /// Adds a new ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Status name</param>
    /// <param name="color">Color theme</param>
    /// <param name="isClosedState">Whether this represents a closed state</param>
    /// <returns>Created status</returns>
    Task<TicketStatus> AddStatusAsync(int workspaceId, string name, string color, bool isClosedState = false);

    /// <summary>
    /// Updates an existing ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="statusId">Status to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <param name="isClosedState">New closed state flag</param>
    /// <returns>Updated status</returns>
    Task<TicketStatus> UpdateStatusAsync(
        int workspaceId, 
        int statusId, 
        string name, 
        string color, 
        int sortOrder, 
        bool isClosedState);

    /// <summary>
    /// Deletes a ticket status.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="statusId">Status to delete</param>
    Task DeleteStatusAsync(int workspaceId, int statusId);

    /// <summary>
    /// Adds a new ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Priority name</param>
    /// <param name="color">Color theme</param>
    /// <returns>Created priority</returns>
    Task<TicketPriority> AddPriorityAsync(int workspaceId, string name, string color);

    /// <summary>
    /// Updates an existing ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="priorityId">Priority to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <returns>Updated priority</returns>
    Task<TicketPriority> UpdatePriorityAsync(
        int workspaceId, 
        int priorityId, 
        string name, 
        string color, 
        int sortOrder);

    /// <summary>
    /// Deletes a ticket priority.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="priorityId">Priority to delete</param>
    Task DeletePriorityAsync(int workspaceId, int priorityId);

    /// <summary>
    /// Adds a new ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="name">Type name</param>
    /// <param name="color">Color theme</param>
    /// <returns>Created type</returns>
    Task<TicketType> AddTypeAsync(int workspaceId, string name, string color);

    /// <summary>
    /// Updates an existing ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="typeId">Type to update</param>
    /// <param name="name">New name</param>
    /// <param name="color">New color</param>
    /// <param name="sortOrder">New sort order</param>
    /// <returns>Updated type</returns>
    Task<TicketType> UpdateTypeAsync(
        int workspaceId, 
        int typeId, 
        string name, 
        string color, 
        int sortOrder);

    /// <summary>
    /// Deletes a ticket type.
    /// </summary>
    /// <param name="workspaceId">Workspace context</param>
    /// <param name="typeId">Type to delete</param>
    Task DeleteTypeAsync(int workspaceId, int typeId);
}
