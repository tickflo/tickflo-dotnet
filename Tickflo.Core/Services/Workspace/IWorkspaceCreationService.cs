namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
public interface IWorkspaceCreationService
{
    /// <summary>
    /// Creates a new workspace with default roles and structure.
    /// </summary>
    /// <param name="request">Workspace creation details</param>
    /// <param name="createdByUserId">User creating the workspace</param>
    /// <returns>The created workspace</returns>
    public Task<Entities.Workspace> CreateWorkspaceAsync(string workspaceName, int createdByUserId);
}
