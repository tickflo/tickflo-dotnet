using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
public class WorkspaceCreationService : IWorkspaceCreationService
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserWorkspaceRepository _userWorkspaceRepo;
    private readonly IUserWorkspaceRoleRepository _userRoleRepo;

    public WorkspaceCreationService(
        IWorkspaceRepository workspaceRepo,
        IRoleRepository roleRepo,
        IUserWorkspaceRepository userWorkspaceRepo,
        IUserWorkspaceRoleRepository userRoleRepo)
    {
        _workspaceRepo = workspaceRepo;
        _roleRepo = roleRepo;
        _userWorkspaceRepo = userWorkspaceRepo;
        _userRoleRepo = userRoleRepo;
    }

    /// <summary>
    /// Creates a new workspace and initializes default roles.
    /// </summary>
    public async Task<Entities.Workspace> CreateWorkspaceAsync(
        WorkspaceCreationRequest request, 
        int createdByUserId)
    {
        // Business rule: Workspace name is required
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Workspace name is required");

        // Business rule: Slug must be unique and valid
        var slug = GenerateSlug(request.Name);
        var existingSlug = await _workspaceRepo.FindBySlugAsync(slug);
        if (existingSlug != null)
            throw new InvalidOperationException($"Slug '{slug}' is already in use");

        var workspace = new Entities.Workspace
        {
            Name = request.Name.Trim(),
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await _workspaceRepo.AddAsync(workspace);

        // Business rule: Create default roles
        await InitializeDefaultRolesAsync(workspace.Id, createdByUserId);

        // Business rule: Add creator as workspace member with admin role
        var adminRole = await _roleRepo.FindByNameAsync(workspace.Id, "Admin");
        if (adminRole != null)
        {
            await _userWorkspaceRepo.AddAsync(new UserWorkspace
            {
                UserId = createdByUserId,
                WorkspaceId = workspace.Id,
                Accepted = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId
            });

            await _userRoleRepo.AddAsync(createdByUserId, workspace.Id, adminRole.Id, createdByUserId);
        }

        return workspace;
    }

    private async Task InitializeDefaultRolesAsync(int workspaceId, int createdByUserId)
    {
        var defaultRoles = new[]
        {
            ("Admin", true),
            ("Manager", false),
            ("Member", false),
            ("Viewer", false)
        };

        foreach (var (name, isAdmin) in defaultRoles)
        {
            var existingRole = await _roleRepo.FindByNameAsync(workspaceId, name);
            if (existingRole == null)
            {
                await _roleRepo.AddAsync(workspaceId, name, isAdmin, createdByUserId);
            }
        }
    }

    private static string GenerateSlug(string name)
    {
        // Convert to lowercase and replace spaces with hyphens
        return System.Text.RegularExpressions.Regex.Replace(
            name.ToLowerInvariant().Trim(),
            @"[^\w\-]",
            string.Empty)
            .Replace(" ", "-")
            .Substring(0, Math.Min(30, name.Length));
    }
}

/// <summary>
/// Request to create a new workspace.
/// </summary>
public class WorkspaceCreationRequest
{
    public string Name { get; set; } = string.Empty;
}
