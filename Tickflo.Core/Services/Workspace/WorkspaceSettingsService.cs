using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using WorkspaceEntity = Tickflo.Core.Entities.Workspace;

using Tickflo.Core.Services.Workspace;

namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Service for managing workspace settings including status, priority, and type configurations.
/// </summary>
public class WorkspaceSettingsService : IWorkspaceSettingsService
{
    #region Constants
    private const string WorkspaceNotFoundError = "Workspace not found";
    private const string SlugInUseError = "Slug is already in use";
    private const string NameRequiredError = "{0} name is required";
    private const string AlreadyExistsError = "{0} '{1}' already exists";
    private const string NotFoundError = "{0} not found";
    private const string DefaultColor = "neutral";
    #endregion

    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;

    public WorkspaceSettingsService(
        IWorkspaceRepository workspaceRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo)
    {
        _workspaceRepo = workspaceRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
    }

    public async Task<WorkspaceEntity> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug)
    {
        var workspace = await GetWorkspaceOrThrowAsync(workspaceId);
        workspace.Name = name.Trim();

        var newSlug = slug.Trim();
        if (newSlug != workspace.Slug)
        {
            await ValidateSlugIsAvailableAsync(newSlug, workspaceId);
            workspace.Slug = newSlug;
        }

        await _workspaceRepo.UpdateAsync(workspace);
        return workspace;
    }

    public async Task EnsureDefaultsExistAsync(int workspaceId)
    {
        // Bootstrap statuses
        var statuses = await _statusRepo.ListAsync(workspaceId);
        if (statuses.Count == 0)
        {
            var defaults = new[]
            {
                new TicketStatus { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true }
            };

            foreach (var status in defaults)
                await _statusRepo.CreateAsync(status);
        }

        // Bootstrap priorities
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        if (priorities.Count == 0)
        {
            var defaults = new[]
            {
                new TicketPriority { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 }
            };

            foreach (var priority in defaults)
                await _priorityRepo.CreateAsync(priority);
        }

        // Bootstrap types
        var types = await _typeRepo.ListAsync(workspaceId);
        if (types.Count == 0)
        {
            var defaults = new[]
            {
                new TicketType { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new TicketType { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new TicketType { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 }
            };

            foreach (var type in defaults)
                await _typeRepo.CreateAsync(type);
        }
    }

    public async Task<TicketStatus> AddStatusAsync(int workspaceId, string name, string color, bool isClosedState = false)
    {
        var trimmedName = ValidateAndTrimName(name, "Status");
        var trimmedColor = TrimColorOrDefault(color);

        await EnsureNameIsUniqueAsync(workspaceId, trimmedName, "Status", 
            () => _statusRepo.FindByNameAsync(workspaceId, trimmedName));

        var maxOrder = await GetMaxSortOrderAsync(() => _statusRepo.ListAsync(workspaceId), s => s.SortOrder);

        var status = new TicketStatus
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1,
            IsClosedState = isClosedState
        };

        await _statusRepo.CreateAsync(status);
        return status;
    }

    public async Task<TicketStatus> UpdateStatusAsync(
        int workspaceId,
        int statusId,
        string name,
        string color,
        int sortOrder,
        bool isClosedState)
    {
        var status = await _statusRepo.FindByIdAsync(workspaceId, statusId);
        if (status == null)
            throw new InvalidOperationException(string.Format(NotFoundError, "Status"));

        status.Name = ValidateAndTrimName(name, "Status");
        status.Color = TrimColorOrDefault(color);
        status.SortOrder = sortOrder;
        status.IsClosedState = isClosedState;

        await _statusRepo.UpdateAsync(status);
        return status;
    }

    public async Task DeleteStatusAsync(int workspaceId, int statusId)
    {
        await _statusRepo.DeleteAsync(workspaceId, statusId);
    }

    public async Task<TicketPriority> AddPriorityAsync(int workspaceId, string name, string color)
    {
        var trimmedName = ValidateAndTrimName(name, "Priority");
        var trimmedColor = TrimColorOrDefault(color);

        await EnsureNameIsUniqueAsync(workspaceId, trimmedName, "Priority",
            () => _priorityRepo.FindAsync(workspaceId, trimmedName));

        var maxOrder = await GetMaxSortOrderAsync(() => _priorityRepo.ListAsync(workspaceId), p => p.SortOrder);

        var priority = new TicketPriority
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1
        };

        await _priorityRepo.CreateAsync(priority);
        return priority;
    }

    public async Task<TicketPriority> UpdatePriorityAsync(
        int workspaceId,
        int priorityId,
        string name,
        string color,
        int sortOrder)
    {
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        var priority = priorities.FirstOrDefault(p => p.Id == priorityId);
        
        if (priority == null)
            throw new InvalidOperationException(string.Format(NotFoundError, "Priority"));

        priority.Name = ValidateAndTrimName(name, "Priority");
        priority.Color = TrimColorOrDefault(color);
        priority.SortOrder = sortOrder;

        await _priorityRepo.UpdateAsync(priority);
        return priority;
    }

    public async Task DeletePriorityAsync(int workspaceId, int priorityId)
    {
        await _priorityRepo.DeleteAsync(workspaceId, priorityId);
    }

    public async Task<TicketType> AddTypeAsync(int workspaceId, string name, string color)
    {
        var trimmedName = ValidateAndTrimName(name, "Type");
        var trimmedColor = TrimColorOrDefault(color);

        await EnsureNameIsUniqueAsync(workspaceId, trimmedName, "Type",
            () => _typeRepo.FindByNameAsync(workspaceId, trimmedName));

        var maxOrder = await GetMaxSortOrderAsync(() => _typeRepo.ListAsync(workspaceId), t => t.SortOrder);

        var type = new TicketType
        {
            WorkspaceId = workspaceId,
            Name = trimmedName,
            Color = trimmedColor,
            SortOrder = maxOrder + 1
        };

        await _typeRepo.CreateAsync(type);
        return type;
    }

    public async Task<TicketType> UpdateTypeAsync(
        int workspaceId,
        int typeId,
        string name,
        string color,
        int sortOrder)
    {
        var types = await _typeRepo.ListAsync(workspaceId);
        var type = types.FirstOrDefault(t => t.Id == typeId);
        
        if (type == null)
            throw new InvalidOperationException(string.Format(NotFoundError, "Type"));

        type.Name = ValidateAndTrimName(name, "Type");
        type.Color = TrimColorOrDefault(color);
        type.SortOrder = sortOrder;

        await _typeRepo.UpdateAsync(type);
        return type;
    }

    public async Task DeleteTypeAsync(int workspaceId, int typeId)
    {
        await _typeRepo.DeleteAsync(workspaceId, typeId);
    }

    private async Task<WorkspaceEntity> GetWorkspaceOrThrowAsync(int workspaceId)
    {
        var workspace = await _workspaceRepo.FindByIdAsync(workspaceId);
        if (workspace == null)
            throw new InvalidOperationException(WorkspaceNotFoundError);
        return workspace;
    }

    private async Task ValidateSlugIsAvailableAsync(string slug, int workspaceId)
    {
        var existing = await _workspaceRepo.FindBySlugAsync(slug);
        if (existing != null && existing.Id != workspaceId)
            throw new InvalidOperationException(SlugInUseError);
    }

    private string ValidateAndTrimName(string name, string entityType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException(string.Format(NameRequiredError, entityType));
        return name.Trim();
    }

    private string TrimColorOrDefault(string color)
    {
        return string.IsNullOrWhiteSpace(color) ? DefaultColor : color.Trim();
    }

    private async Task EnsureNameIsUniqueAsync<T>(int workspaceId, string name, string entityType, Func<Task<T?>> findExisting) where T : class
    {
        var existing = await findExisting();
        if (existing != null)
            throw new InvalidOperationException(string.Format(AlreadyExistsError, entityType, name));
    }

    private async Task<int> GetMaxSortOrderAsync<T>(Func<Task<IReadOnlyList<T>>> getList, Func<T, int> getSortOrder)
    {
        var list = await getList();
        return list.Any() ? list.Max(getSortOrder) : 0;
    }
}




