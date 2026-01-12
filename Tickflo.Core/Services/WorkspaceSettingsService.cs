using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

/// <summary>
/// Service for managing workspace settings including status, priority, and type configurations.
/// </summary>
public class WorkspaceSettingsService : IWorkspaceSettingsService
{
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

    public async Task<Workspace> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug)
    {
        var workspace = await _workspaceRepo.FindByIdAsync(workspaceId);
        if (workspace == null)
            throw new InvalidOperationException("Workspace not found");

        workspace.Name = name.Trim();

        var newSlug = slug.Trim();
        if (newSlug != workspace.Slug)
        {
            var existing = await _workspaceRepo.FindBySlugAsync(newSlug);
            if (existing != null && existing.Id != workspaceId)
                throw new InvalidOperationException("Slug is already in use");
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
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Status name is required");

        name = name.Trim();
        color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();

        var existing = await _statusRepo.FindByNameAsync(workspaceId, name);
        if (existing != null)
            throw new InvalidOperationException($"Status '{name}' already exists");

        var statuses = await _statusRepo.ListAsync(workspaceId);
        var maxOrder = statuses.Any() ? statuses.Max(s => s.SortOrder) : 0;

        var status = new TicketStatus
        {
            WorkspaceId = workspaceId,
            Name = name,
            Color = color,
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
            throw new InvalidOperationException("Status not found");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Status name is required");

        status.Name = name.Trim();
        status.Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();
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
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Priority name is required");

        name = name.Trim();
        color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();

        var existing = await _priorityRepo.FindAsync(workspaceId, name);
        if (existing != null)
            throw new InvalidOperationException($"Priority '{name}' already exists");

        var priorities = await _priorityRepo.ListAsync(workspaceId);
        var maxOrder = priorities.Any() ? priorities.Max(p => p.SortOrder) : 0;

        var priority = new TicketPriority
        {
            WorkspaceId = workspaceId,
            Name = name,
            Color = color,
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
            throw new InvalidOperationException("Priority not found");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Priority name is required");

        priority.Name = name.Trim();
        priority.Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();
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
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Type name is required");

        name = name.Trim();
        color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();

        var existing = await _typeRepo.FindByNameAsync(workspaceId, name);
        if (existing != null)
            throw new InvalidOperationException($"Type '{name}' already exists");

        var types = await _typeRepo.ListAsync(workspaceId);
        var maxOrder = types.Any() ? types.Max(t => t.SortOrder) : 0;

        var type = new TicketType
        {
            WorkspaceId = workspaceId,
            Name = name,
            Color = color,
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
            throw new InvalidOperationException("Type not found");

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Type name is required");

        type.Name = name.Trim();
        type.Color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();
        type.SortOrder = sortOrder;

        await _typeRepo.UpdateAsync(type);
        return type;
    }

    public async Task DeleteTypeAsync(int workspaceId, int typeId)
    {
        await _typeRepo.DeleteAsync(workspaceId, typeId);
    }
}
