namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Represents a bulk update request for workspace settings.
/// </summary>
public sealed record BulkSettingsUpdateRequest
{
    /// <summary>
    /// Gets the workspace name update (if provided).
    /// </summary>
    public string? WorkspaceName { get; init; }

    /// <summary>
    /// Gets the workspace slug update (if provided).
    /// </summary>
    public string? WorkspaceSlug { get; init; }

    /// <summary>
    /// Gets the list of status updates.
    /// </summary>
    public IReadOnlyList<StatusUpdate> StatusUpdates { get; init; } = [];

    /// <summary>
    /// Gets the new status to create (if provided).
    /// </summary>
    public StatusCreate? NewStatus { get; init; }

    /// <summary>
    /// Gets the list of priority updates.
    /// </summary>
    public IReadOnlyList<PriorityUpdate> PriorityUpdates { get; init; } = [];

    /// <summary>
    /// Gets the new priority to create (if provided).
    /// </summary>
    public PriorityCreate? NewPriority { get; init; }

    /// <summary>
    /// Gets the list of type updates.
    /// </summary>
    public IReadOnlyList<TypeUpdate> TypeUpdates { get; init; } = [];

    /// <summary>
    /// Gets the new type to create (if provided).
    /// </summary>
    public TypeCreate? NewType { get; init; }
}

/// <summary>
/// Represents a status update operation.
/// </summary>
public sealed record StatusUpdate
{
    /// <summary>
    /// Gets the status ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets whether this status should be deleted.
    /// </summary>
    public bool Delete { get; init; }

    /// <summary>
    /// Gets the updated name (if provided).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the updated color (if provided).
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets the updated sort order (if provided).
    /// </summary>
    public int? SortOrder { get; init; }

    /// <summary>
    /// Gets whether this is a closed state.
    /// </summary>
    public bool? IsClosedState { get; init; }
}

/// <summary>
/// Represents a new status to create.
/// </summary>
public sealed record StatusCreate
{
    /// <summary>
    /// Gets the status name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the status color.
    /// </summary>
    public string Color { get; init; } = "neutral";

    /// <summary>
    /// Gets whether this is a closed state.
    /// </summary>
    public bool IsClosedState { get; init; }
}

/// <summary>
/// Represents a priority update operation.
/// </summary>
public sealed record PriorityUpdate
{
    /// <summary>
    /// Gets the priority ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets whether this priority should be deleted.
    /// </summary>
    public bool Delete { get; init; }

    /// <summary>
    /// Gets the updated name (if provided).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the updated color (if provided).
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets the updated sort order (if provided).
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// Represents a new priority to create.
/// </summary>
public sealed record PriorityCreate
{
    /// <summary>
    /// Gets the priority name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the priority color.
    /// </summary>
    public string Color { get; init; } = "neutral";
}

/// <summary>
/// Represents a type update operation.
/// </summary>
public sealed record TypeUpdate
{
    /// <summary>
    /// Gets the type ID.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets whether this type should be deleted.
    /// </summary>
    public bool Delete { get; init; }

    /// <summary>
    /// Gets the updated name (if provided).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the updated color (if provided).
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Gets the updated sort order (if provided).
    /// </summary>
    public int? SortOrder { get; init; }
}

/// <summary>
/// Represents a new type to create.
/// </summary>
public sealed record TypeCreate
{
    /// <summary>
    /// Gets the type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type color.
    /// </summary>
    public string Color { get; init; } = "neutral";
}

/// <summary>
/// Result of a bulk settings update operation.
/// </summary>
public sealed record BulkSettingsUpdateResult
{
    /// <summary>
    /// Gets the updated workspace (if workspace settings were updated).
    /// </summary>
    public Entities.Workspace? UpdatedWorkspace { get; init; }

    /// <summary>
    /// Gets the number of changes applied.
    /// </summary>
    public int ChangesApplied { get; init; }

    /// <summary>
    /// Gets any errors that occurred during the update.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}
