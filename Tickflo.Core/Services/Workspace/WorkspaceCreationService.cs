namespace Tickflo.Core.Services.Workspace;

using System.Text;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

/// <summary>
/// Handles workspace creation and initialization workflows.
/// </summary>
public partial class WorkspaceCreationService(
    IWorkspaceRepository workspaceRepository,
    IRoleRepository roleRepo,
    IUserWorkspaceRepository userWorkspaceRepository,
    IUserWorkspaceRoleRepository userWorkspaceRoleRepository) : IWorkspaceCreationService
{
    private const int MaxSlugLength = 30;
    private const string ErrorWorkspaceNameRequired = "Workspace name is required";
    private static readonly CompositeFormat ErrorSlugInUse = CompositeFormat.Parse("Slug '{0}' is already in use");

    private static readonly (string Name, bool IsAdmin)[] DefaultRoles =
    [
        ("Admin", true),
        ("Manager", false),
        ("Member", false),
        ("Viewer", false)
    ];

    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly IRoleRepository roleRepository = roleRepo;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IUserWorkspaceRoleRepository userWorkspaceRoleRepository = userWorkspaceRoleRepository;

    /// <summary>
    /// Creates a new workspace and initializes default roles.
    /// </summary>
    public async Task<Workspace> CreateWorkspaceAsync(
        WorkspaceCreationRequest request,
        int createdByUserId)
    {
        ValidateWorkspaceRequest(request);
        var slug = await this.GenerateAndValidateSlugAsync(request.Name);

        var workspace = new Workspace
        {
            Name = request.Name.Trim(),
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        };

        await this.workspaceRepository.AddAsync(workspace);
        await this.InitializeDefaultRolesAsync(workspace.Id, createdByUserId);
        await this.AddCreatorAsAdminAsync(workspace.Id, createdByUserId);

        return workspace;
    }

    private static void ValidateWorkspaceRequest(WorkspaceCreationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException(ErrorWorkspaceNameRequired);
        }
    }

    private async Task<string> GenerateAndValidateSlugAsync(string name)
    {
        var slug = GenerateSlug(name);
        var existingSlug = await this.workspaceRepository.FindBySlugAsync(slug);

        if (existingSlug != null)
        {
            throw new InvalidOperationException(string.Format(null, ErrorSlugInUse, slug));
        }

        return slug;
    }

    private async Task AddCreatorAsAdminAsync(int workspaceId, int createdByUserId)
    {
        var adminRole = await this.roleRepository.FindByNameAsync(workspaceId, "Admin");
        if (adminRole == null)
        {
            return;
        }

        await this.userWorkspaceRepository.AddAsync(new UserWorkspace
        {
            UserId = createdByUserId,
            WorkspaceId = workspaceId,
            Accepted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdByUserId
        });

        await this.userWorkspaceRoleRepository.AddAsync(createdByUserId, workspaceId, adminRole.Id, createdByUserId);
    }

    private async Task InitializeDefaultRolesAsync(int workspaceId, int createdByUserId)
    {
        foreach (var (name, isAdmin) in DefaultRoles)
        {
            var existingRole = await this.roleRepository.FindByNameAsync(workspaceId, name);
            if (existingRole == null)
            {
                await this.roleRepository.AddAsync(workspaceId, name, isAdmin, createdByUserId);
            }
        }
    }

    private static string GenerateSlug(string name)
    {
        var normalizedName = name.ToLowerInvariant().Trim();
        var slug = MyRegex().Replace(normalizedName, string.Empty)
            .Replace(" ", "-");

        return slug[..Math.Min(MaxSlugLength, slug.Length)];
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"[^\w\-]")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
}

/// <summary>
/// Request to create a new workspace.
/// </summary>
public class WorkspaceCreationRequest
{
    public string Name { get; set; } = string.Empty;
}
