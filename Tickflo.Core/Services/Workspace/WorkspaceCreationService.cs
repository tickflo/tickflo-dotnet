namespace Tickflo.Core.Services.Workspace;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
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
    public Task<Workspace> CreateWorkspaceAsync(string workspaceName, int createdByUserId);
}

public partial class WorkspaceCreationService(
    TickfloDbContext dbContext,
    TickfloConfig config) : IWorkspaceCreationService
{
    private static readonly (string Name, bool IsAdmin)[] DefaultRoles =
    [
        ("Admin", true),
        ("Manager", false),
        ("Member", false),
        ("Viewer", false)
    ];

    private readonly TickfloDbContext dbContext = dbContext;
    private readonly TickfloConfig config = config;

    /// <summary>
    /// Creates a new workspace and initializes default roles.
    /// </summary>
    public async Task<Workspace> CreateWorkspaceAsync(
        string workspaceName,
        int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(workspaceName)
            || workspaceName.Length > this.config.Workspace.MaxNameLength
            || workspaceName.Length < this.config.Workspace.MinNameLength)
        {
            throw new BadRequestException($"Invalid workspace name: {workspaceName}");
        }

        var slug = workspaceName.Trim().ToLower().Replace(' ', '-').Trim('-');
        if (string.IsNullOrWhiteSpace(slug)
            || slug.Length < this.config.Workspace.MinNameLength
            || slug.Length > this.config.Workspace.MaxSlugLength)
        {
            throw new BadRequestException($"Invalid workspace slug: {slug}");
        }

        var existingWorkspace = await this.dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Slug.ToLower() == slug);

        if (existingWorkspace != null)
        {
            throw new BadRequestException($"Workspace with slug '{slug}' already exists");
        }

        var workspace = new Workspace
        {
            Name = workspaceName.Trim(),
            Slug = slug,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        this.dbContext.Workspaces.Add(workspace);
        await this.dbContext.SaveChangesAsync();

        var membership = new UserWorkspace
        {
            UserId = createdByUserId,
            WorkspaceId = workspace.Id,
            Accepted = true,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        this.dbContext.UserWorkspaces.Add(membership);
        await this.dbContext.SaveChangesAsync();

        int? adminRoleId = null;
        foreach (var (name, isAdmin) in DefaultRoles)
        {
            var role = new Role
            {
                WorkspaceId = workspace.Id,
                Name = name,
                Admin = isAdmin,
                CreatedBy = createdByUserId
            };

            this.dbContext.Roles.Add(role);
            await this.dbContext.SaveChangesAsync();

            if (name == "Admin")
            {
                adminRoleId = role.Id;
            }
        }

        if (adminRoleId == null)
        {
            throw new InternalServerErrorException("Failed to create admin role");
        }

        var roleAssignment = new UserWorkspaceRole
        {
            UserId = createdByUserId,
            WorkspaceId = workspace.Id,
            RoleId = adminRoleId.Value,
            CreatedBy = createdByUserId
        };

        this.dbContext.UserWorkspaceRoles.Add(roleAssignment);
        await this.dbContext.SaveChangesAsync();

        await this.CreateDefaultInboundEmailRouteAsync(workspace, slug, createdByUserId);

        return workspace;
    }

    /// <summary>
    /// Creates a default inbound email route for the workspace using its slug as the local part.
    /// If the slug collides with an existing route's local part (local parts are globally unique),
    /// a numeric suffix is appended. If no unique local part can be found, the route is skipped —
    /// workspace creation must not fail because of an email route.
    /// </summary>
    private async Task CreateDefaultInboundEmailRouteAsync(Workspace workspace, string slug, int createdByUserId)
    {
        var existingLocalParts = (await this.dbContext.InboundEmailRoutes
            .Select(r => r.LocalPart)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var localPart = slug;
        for (var suffix = 2; existingLocalParts.Contains(localPart) && suffix <= 11; suffix++)
        {
            localPart = $"{slug}-{suffix}";
        }

        if (existingLocalParts.Contains(localPart))
        {
            return;
        }

        var defaultRoute = new InboundEmailRoute
        {
            WorkspaceId = workspace.Id,
            LocalPart = localPart,
            Label = "Default",
            Active = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        this.dbContext.InboundEmailRoutes.Add(defaultRoute);
        await this.dbContext.SaveChangesAsync();
    }
}
