namespace Tickflo.Core.Services.Authentication;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using WorkspaceEntity = Entities.Workspace;

// TODO: This service should _NOT_ have optional dependencies. Refactor tests to provide mocks for all dependencies.

public partial class AuthenticationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenRepository tokenRepository,
    IWorkspaceRepository? workspaceRepository = null,
    IUserWorkspaceRepository? userWorkspaceRepository = null,
    IWorkspaceRoleBootstrapper? workspaceRoleBootstrapper = null) : IAuthenticationService
{
    #region Constants
    private const string InvalidCredentialsError = "Invalid username or password, please try again";
    private const string AccountExistsError = "An account with that email already exists";
    private const string WorkspaceRepositoriesNotConfiguredError = "Workspace repositories are not configured.";
    private const string DummyPasswordHash = "$argon2id$v=19$m=16,t=2,p=1$NlJRdlBSbDZhRVUzdTFYcQ$FbtOcbMs2IMTMHFE8WcSiQ";
    private const string SlugPattern = "[^a-z0-9\\- ]";
    private const string WhitespacePattern = "\\s+";
    private const string HyphenReplacement = "-";
    private const int MaxSlugLength = 30;
    #endregion

    private readonly IUserRepository userRepository = userRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly ITokenRepository tokenRepository = tokenRepository;
    private readonly IWorkspaceRepository? workspaceRepository = workspaceRepository;
    private readonly IUserWorkspaceRepository? userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceRoleBootstrapper? workspaceRoleBootstrapper = workspaceRoleBootstrapper;

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        var result = new AuthenticationResult();
        var user = await this.userRepository.FindByEmailAsync(email);

        if (user == null)
        {
            result.ErrorMessage = InvalidCredentialsError;
            this.PreventTimingAttack();
            return result;
        }

        if (user.PasswordHash == null)
        {
            result.ErrorMessage = InvalidCredentialsError;
            return result;
        }

        if (!this.passwordHasher.Verify($"{email}{password}", user.PasswordHash))
        {
            result.ErrorMessage = InvalidCredentialsError;
            return result;
        }

        var token = await this.tokenRepository.CreateForUserIdAsync(user.Id);
        result.Success = true;
        result.UserId = user.Id;
        result.Token = token.Value;
        result.WorkspaceSlug = await this.GetUserWorkspaceSlugAsync(user.Id);

        return result;
    }

    public async Task<AuthenticationResult> SignupAsync(string name, string email, string recoveryEmail, string workspaceName, string password)
    {
        var result = new AuthenticationResult();

        if (await this.userRepository.FindByEmailAsync(email) != null)
        {
            result.ErrorMessage = AccountExistsError;
            return result;
        }

        var user = await this.CreateUserAsync(name, email, recoveryEmail, password);
        var workspace = await this.CreateWorkspaceWithUserAsync(workspaceName, user.Id);

        if (this.workspaceRoleBootstrapper != null)
        {
            await this.workspaceRoleBootstrapper.BootstrapAdminAsync(workspace.Id, user.Id);
        }

        var token = await this.tokenRepository.CreateForUserIdAsync(user.Id);
        result.Success = true;
        result.UserId = user.Id;
        result.Token = token.Value;
        result.WorkspaceSlug = workspace.Slug;

        return result;
    }

    private void PreventTimingAttack() => this.passwordHasher.Verify("password", DummyPasswordHash);

    private async Task<string?> GetUserWorkspaceSlugAsync(int userId)
    {
        if (this.userWorkspaceRepository != null && this.workspaceRepository != null)
        {
            var userWorkspace = await this.userWorkspaceRepository.FindAcceptedForUserAsync(userId);
            if (userWorkspace != null)
            {
                var workspace = await this.workspaceRepository.FindByIdAsync(userWorkspace.WorkspaceId);
                return workspace?.Slug;
            }
        }
        return null;
    }

    private async Task<User> CreateUserAsync(string name, string email, string recoveryEmail, string password)
    {
        var passwordHash = this.passwordHasher.Hash($"{email}{password}");
        var user = new User
        {
            Name = name,
            Email = email,
            RecoveryEmail = recoveryEmail,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await this.userRepository.AddAsync(user);
        return user;
    }

    private async Task<WorkspaceEntity> CreateWorkspaceWithUserAsync(string workspaceName, int userId)
    {
        if (this.workspaceRepository == null || this.userWorkspaceRepository == null)
        {
            throw new InvalidOperationException(WorkspaceRepositoriesNotConfiguredError);
        }

        var slug = await this.GenerateUniqueSlugAsync(workspaceName);
        var workspace = new WorkspaceEntity
        {
            Name = workspaceName,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await this.workspaceRepository.AddAsync(workspace);

        var userWorkspace = new UserWorkspace
        {
            UserId = userId,
            WorkspaceId = workspace.Id,
            Accepted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await this.userWorkspaceRepository.AddAsync(userWorkspace);
        return workspace;
    }

    private async Task<string> GenerateUniqueSlugAsync(string workspaceName)
    {
        var baseSlug = Slugify(workspaceName);
        var slug = baseSlug;
        var counter = 1;

        while (!string.IsNullOrEmpty(slug) && await this.workspaceRepository!.FindBySlugAsync(slug) != null)
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static string Slugify(string input)
    {
        var lower = input.ToLowerInvariant();
        var chars = MyRegex().Replace(lower, string.Empty);
        var collapsed = MyRegex1().Replace(chars, HyphenReplacement);

        if (collapsed.Length > MaxSlugLength)
        {
            collapsed = collapsed[..MaxSlugLength];
        }

        return collapsed.Trim('-');
    }

    [System.Text.RegularExpressions.GeneratedRegex(SlugPattern)]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
    [System.Text.RegularExpressions.GeneratedRegex(WhitespacePattern)]
    private static partial System.Text.RegularExpressions.Regex MyRegex1();
}



