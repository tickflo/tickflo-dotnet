namespace Tickflo.Core.Data;

using Tickflo.Core.Entities;

/// <summary>
/// Repository interface for FileStorage entity operations.
/// </summary>
public interface IFileStorageRepository
{
    /// <summary>
    /// Creates a new file storage record.
    /// </summary>
    public Task<FileStorage> CreateAsync(FileStorage file);

    /// <summary>
    /// Updates an existing file storage record.
    /// </summary>
    public Task<bool> UpdateAsync(FileStorage file);

    /// <summary>
    /// Finds a file storage record by ID.
    /// </summary>
    public Task<FileStorage?> FindByIdAsync(int id);

    /// <summary>
    /// Finds file storage records by workspace ID.
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> FindByWorkspaceAsync(int workspaceId);

    /// <summary>
    /// Finds file storage records by user ID.
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> FindByUserAsync(int userId);

    /// <summary>
    /// Finds file storage records by category (e.g., "user-avatar", "workspace-logo").
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> FindByCategoryAsync(int workspaceId, string category);

    /// <summary>
    /// Finds file storage records by file type.
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> FindByTypeAsync(int workspaceId, string fileType);

    /// <summary>
    /// Finds a file by path.
    /// </summary>
    public Task<FileStorage?> FindByPathAsync(int workspaceId, string path);

    /// <summary>
    /// Finds files related to a specific entity (e.g., ticket, contact).
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> FindByRelatedEntityAsync(int workspaceId, string entityType, int entityId);

    /// <summary>
    /// Lists all files in a workspace with optional filtering.
    /// </summary>
    public Task<IReadOnlyList<FileStorage>> ListAsync(int workspaceId, int take = 50, int skip = 0, string? category = null);

    /// <summary>
    /// Soft deletes a file storage record (archives it).
    /// </summary>
    public Task<bool> ArchiveAsync(int id, int archivedByUserId);

    /// <summary>
    /// Hard deletes a file storage record.
    /// </summary>
    public Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Permanently deletes all files for a workspace.
    /// </summary>
    public Task<int> DeleteWorkspaceFilesAsync(int workspaceId);

    /// <summary>
    /// Gets total storage used by a workspace in bytes.
    /// </summary>
    public Task<long> GetWorkspaceStorageUsedAsync(int workspaceId);

    /// <summary>
    /// Gets count of files in a workspace.
    /// </summary>
    public Task<int> GetWorkspaceFileCountAsync(int workspaceId);
}
