namespace Tickflo.Core.Entities;

/// <summary>
/// Marker interface for entities that belong to a workspace.
/// Used for validation in page models to ensure entities belong to the correct workspace.
/// </summary>
public interface IWorkspaceEntity
{
    /// <summary>
    /// Gets the ID of the workspace this entity belongs to.
    /// </summary>
    int WorkspaceId { get; }
}
