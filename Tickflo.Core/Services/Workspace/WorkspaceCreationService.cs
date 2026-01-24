namespace Tickflo.Core.Services.Workspace;

using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Exceptions;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
public partial class WorkspaceCreationService(
    IWorkspaceRepository workspaceRepository,
    IRoleRepository roleRepository,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository,
    TickfloConfig config) : IWorkspaceCreationService
{
    private static readonly (string Name, bool IsAdmin)[] DefaultRoles =
    [
        ("Admin", true),
        ("Manager", false),
        ("Member", false),
        ("Viewer", false)
    ];

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;
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

        var slug = workspaceName.Trim().ToLowerInvariant().Replace(' ', '-').Trim('-');
        if (string.IsNullOrWhiteSpace(slug)
            || slug.Length < this.config.Workspace.MinNameLength
            || slug.Length > this.config.Workspace.MaxSlugLength)
        {
            throw new BadRequestException($"Invalid workspace slug: {slug}");
        }

        if (await this.workspaceRepository.FindBySlugAsync(slug) != null)
        {
            throw new BadRequestException($"Workspace with slug '{slug}' already exists");
        }

        var workspace = await this.workspaceRepository.AddAsync(new Workspace
        {
            Name = workspaceName.Trim(),
            Slug = slug,
            CreatedBy = createdByUserId
        });

        await this.userWorkspaceRepository.AddAsync(new UserWorkspace
        {
            UserId = createdByUserId,
            WorkspaceId = workspace.Id,
            Accepted = true,
            CreatedBy = createdByUserId
        });

        int? adminRoleId = null;
        foreach (var (name, isAdmin) in DefaultRoles)
        {
            var role = await this.roleRepository.AddAsync(new Role
            {
                WorkspaceId = workspace.Id,
                Name = name,
                Admin = isAdmin,
                CreatedBy = createdByUserId
            });

            if (name == "Admin")
            {
                adminRoleId = role.Id;
            }
        }

        if (adminRoleId == null)
        {
            throw new InternalServerErrorException("Failed to create admin role");
        }

        await this.userWorkspaceRoleRepository.AddAsync(new UserWorkspaceRole
        {
            UserId = createdByUserId,
            WorkspaceId = workspace.Id,
            RoleId = adminRoleId.Value,
            CreatedBy = createdByUserId
        });

        return workspace;
    }
}
