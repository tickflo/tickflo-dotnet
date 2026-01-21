namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Entities;

/// <summary>
/// Repository implementation for FileStorage entity operations.
/// </summary>
public class FileStorageRepository(TickfloDbContext db) : IFileStorageRepository
{
    public async Task<FileStorage> CreateAsync(FileStorage file)
    {
        file.CreatedAt = DateTime.UtcNow;
        db.FileStorages.Add(file);
        await db.SaveChangesAsync();
        return file;
    }

    public async Task<bool> UpdateAsync(FileStorage file)
    {
        file.UpdatedAt = DateTime.UtcNow;
        db.FileStorages.Update(file);
        return await db.SaveChangesAsync() > 0;
    }

    public async Task<FileStorage?> FindByIdAsync(int id) => await db.FileStorages.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

    public async Task<IReadOnlyList<FileStorage>> FindByWorkspaceAsync(int workspaceId) => await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByUserAsync(int userId) => await db.FileStorages
            .Where(f => f.UserId == userId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByCategoryAsync(int workspaceId, string category) => await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.Category == category && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> FindByTypeAsync(int workspaceId, string fileType) => await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.FileType == fileType && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<FileStorage?> FindByPathAsync(int workspaceId, string path) => await db.FileStorages
            .FirstOrDefaultAsync(f => f.WorkspaceId == workspaceId && f.Path == path && f.DeletedAt == null);

    public async Task<IReadOnlyList<FileStorage>> FindByRelatedEntityAsync(int workspaceId, string entityType, int entityId) => await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.RelatedEntityType == entityType && f.RelatedEntityId == entityId && f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<FileStorage>> ListAsync(int workspaceId, int take = 50, int skip = 0, string? category = null)
    {
        var query = db.FileStorages.Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null);

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
        var file = await db.FileStorages.FirstOrDefaultAsync(f => f.Id == id);
        if (file == null)
        {
            return false;
        }

        file.IsArchived = true;
        file.DeletedAt = DateTime.UtcNow;
        file.DeletedByUserId = archivedByUserId;
        file.UpdatedAt = DateTime.UtcNow;

        return await db.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var file = await db.FileStorages.FirstOrDefaultAsync(f => f.Id == id);
        if (file == null)
        {
            return false;
        }

        db.FileStorages.Remove(file);
        return await db.SaveChangesAsync() > 0;
    }

    public async Task<int> DeleteWorkspaceFilesAsync(int workspaceId)
    {
        var files = await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId)
            .ToListAsync();

        db.FileStorages.RemoveRange(files);
        return await db.SaveChangesAsync();
    }

    public async Task<long> GetWorkspaceStorageUsedAsync(int workspaceId) => await db.FileStorages
            .Where(f => f.WorkspaceId == workspaceId && f.DeletedAt == null)
            .SumAsync(f => f.Size);

    public async Task<int> GetWorkspaceFileCountAsync(int workspaceId) => await db.FileStorages
            .CountAsync(f => f.WorkspaceId == workspaceId && f.DeletedAt == null);
}
