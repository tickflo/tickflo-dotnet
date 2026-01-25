namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

/// <summary>
/// Repository implementation for FileStorage entity operations.
/// </summary>

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

public class FileStorageRepository(TickfloDbContext dbContext) : IFileStorageRepository
{
    private readonly TickfloDbContext dbContext = dbContext;
    public async Task<FileStorage> CreateAsync(FileStorage file)
    {
        this.dbContext.FileStorages.Add(file);
        await this.dbContext.SaveChangesAsync();
        return file;
    }

    public async Task<bool> UpdateAsync(FileStorage file)
    {
        file.UpdatedAt = DateTime.UtcNow;
        this.dbContext.FileStorages.Update(file);
        return await this.dbContext.SaveChangesAsync() > 0;
    }

    public async Task<FileStorage?> FindByIdAsync(int id) => await this.dbContext.FileStorages.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

    public async Task<IReadOnlyList<FileStorage>> FindByWorkspaceAsync(int workspaceId) => await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByUserAsync(int userId) => await this.dbContext.FileStorages
            .Where(f => f.UserId == userId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByCategoryAsync(int workspaceId, string category) => await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.Category == category && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByTypeAsync(int workspaceId, string fileType) => await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.FileType == fileType && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<FileStorage?> FindByPathAsync(int workspaceId, string path) => await this.dbContext.FileStorages
            .FirstOrDefaultAsync(f => f.WorkspaceId == workspaceId && f.Path == path && f.DeletedAt == null);

    public async Task<IReadOnlyList<FileStorage>> FindByRelatedEntityAsync(int workspaceId, string entityType, int entityId) => await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.RelatedEntityType == entityType && f.RelatedEntityId == entityId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> ListAsync(int workspaceId, int take = 50, int skip = 0, string? category = null)
    {
        var query = this.dbContext.FileStorages.Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(f => f.Category == category);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> ArchiveAsync(int id, int archivedByUserId)
    {
        var file = await this.dbContext.FileStorages.FirstOrDefaultAsync(f => f.Id == id);
        if (file == null)
        {
            return false;
        }

        file.IsArchived = true;
        file.DeletedAt = DateTime.UtcNow;
        file.DeletedByUserId = archivedByUserId;
        file.UpdatedAt = DateTime.UtcNow;

        return await this.dbContext.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var file = await this.dbContext.FileStorages.FirstOrDefaultAsync(f => f.Id == id);
        if (file == null)
        {
            return false;
        }

        this.dbContext.FileStorages.Remove(file);
        return await this.dbContext.SaveChangesAsync() > 0;
    }

    public async Task<int> DeleteWorkspaceFilesAsync(int workspaceId)
    {
        var files = await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId)
            .ToListAsync();

        this.dbContext.FileStorages.RemoveRange(files);
        return await this.dbContext.SaveChangesAsync();
    }

    public async Task<long> GetWorkspaceStorageUsedAsync(int workspaceId) => await this.dbContext.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null)
            .SumAsync(f => f.Size);

    public async Task<int> GetWorkspaceFileCountAsync(int workspaceId) => await this.dbContext.FileStorages
            .CountAsync(f => f.WorkspaceId == workspaceId && f.DeletedAt == null);
}
