using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Workspace;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
public class WorkspaceCreationService : IWorkspaceCreationService
{
    private const int MaxSlugLength = 30;
    private const string ErrorWorkspaceNameRequired = "Workspace name is required";
    private const string ErrorSlugInUse = "Slug '{0}' is already in use";
    
    private static readonly (string Name, bool IsAdmin)[] DefaultRoles = new[]
    {
        ("Admin", true),
        ("Manager", false),
        ("Member", false),
        ("Viewer", false)
    };

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
        ValidateWorkspaceRequest(request);
        var slug = await GenerateAndValidateSlugAsync(request.Name);

        var workspace = new Entities.Workspace
        {
            Name = request.Name.Trim(),
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await _workspaceRepo.AddAsync(workspace);
        await InitializeDefaultRolesAsync(workspace.Id, createdByUserId);
        await AddCreatorAsAdminAsync(workspace.Id, createdByUserId);

        return workspace;
    }

    private static void ValidateWorkspaceRequest(WorkspaceCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException(ErrorWorkspaceNameRequired);
    }

    private async Task<string> GenerateAndValidateSlugAsync(string name)
    {
        var slug = GenerateSlug(name);
        var existingSlug = await _workspaceRepo.FindBySlugAsync(slug);
        
        if (existingSlug != null)
            throw new InvalidOperationException(string.Format(ErrorSlugInUse, slug));

        return slug;
    }

    private async Task AddCreatorAsAdminAsync(int workspaceId, int createdByUserId)
    {
        var adminRole = await _roleRepo.FindByNameAsync(workspaceId, "Admin");
        if (adminRole == null)
            return;

        await _userWorkspaceRepo.AddAsync(new UserWorkspace
        {
            UserId = createdByUserId,
            WorkspaceId = workspaceId,
            Accepted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        });

        await _userRoleRepo.AddAsync(createdByUserId, workspaceId, adminRole.Id, createdByUserId);
    }

    private async Task InitializeDefaultRolesAsync(int workspaceId, int createdByUserId)
    {
        foreach (var (name, isAdmin) in DefaultRoles)
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
        var normalizedName = name.ToLowerInvariant().Trim();
        var slug = System.Text.RegularExpressions.Regex.Replace(
            normalizedName,
            @"[^\w\-]",
            string.Empty)
            .Replace(" ", "-");

        return slug.Substring(0, Math.Min(MaxSlugLength, slug.Length));
    }
}

/// <summary>
/// Request to create a new workspace.
/// </summary>
public class WorkspaceCreationRequest
{
    public string Name { get; set; } = string.Empty;
}
