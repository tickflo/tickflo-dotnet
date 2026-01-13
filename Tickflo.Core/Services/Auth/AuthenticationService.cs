using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Auth;
using WorkspaceEntity = Tickflo.Core.Entities.Workspace;

namespace Tickflo.Core.Services.Auth;

public class AuthenticationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenRepository tokenRepository,
    IWorkspaceRepository? workspaceRepository = null,
    IUserWorkspaceRepository? userWorkspaceRepository = null,
    IWorkspaceRoleBootstrapper? workspaceRoleBootstrapper = null) : IAuthenticationService
{
    private readonly IUserRepository _userRepository = userRepository;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ITokenRepository _tokenRepository = tokenRepository;
        private readonly IWorkspaceRepository? _workspaceRepository = workspaceRepository;
        private readonly IUserWorkspaceRepository? _userWorkspaceRepository = userWorkspaceRepository;
        private readonly IWorkspaceRoleBootstrapper? _workspaceRoleBootstrapper = workspaceRoleBootstrapper;

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        var result = new AuthenticationResult();
        var user = await _userRepository.FindByEmailAsync(email);
        if (user == null)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            _passwordHasher.Verify("password", "$argon2id$v=19$m=16,t=2,p=1$NlJRdlBSbDZhRVUzdTFYcQ$FbtOcbMs2IMTMHFE8WcSiQ");
            return result;
        }

        // Reject logins where no password hash is set
        if (user.PasswordHash == null)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        var isValid = _passwordHasher.Verify($"{email}{password}", user.PasswordHash);
        if (!isValid)
        {
            result.ErrorMessage = "Invalid username or password, please try again";
            return result;
        }

        var token = await _tokenRepository.CreateForUserIdAsync(user.Id);

        result.Success = true;
        result.UserId = user.Id;
        result.Token = token.Value;

        if (_userWorkspaceRepository != null && _workspaceRepository != null)
        {
            var uw = await _userWorkspaceRepository.FindAcceptedForUserAsync(user.Id);
            if (uw != null)
            {
                var ws = await _workspaceRepository.FindByIdAsync(uw.WorkspaceId);
                if (ws != null)
                {
                    result.WorkspaceSlug = ws.Slug;
                }
            }
        }

        return result;
    }

    public async Task<AuthenticationResult> SignupAsync(string name, string email, string recoveryEmail, string workspaceName, string password)
    {
        var result = new AuthenticationResult();

        // check existing user
        var existing = await _userRepository.FindByEmailAsync(email);
        if (existing != null)
        {
            result.ErrorMessage = "An account with that email already exists";
            return result;
        }

        // create user
        var passwordHash = _passwordHasher.Hash($"{email}{password}");
        var user = new User
        {
            Name = name,
            Email = email,
            RecoveryEmail = recoveryEmail,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        // create workspace (unique slug)
        string Slugify(string s)
        {
            var lower = s.ToLowerInvariant();
            var chars = System.Text.RegularExpressions.Regex.Replace(lower, "[^a-z0-9\\- ]", "");
            var collapsed = System.Text.RegularExpressions.Regex.Replace(chars, "\\s+", "-");
            if (collapsed.Length > 30) collapsed = collapsed.Substring(0, 30);
            return collapsed.Trim('-');
        }

        var baseSlug = Slugify(workspaceName);
        var slug = baseSlug;
        var i = 1;
            if (_workspaceRepository == null || _userWorkspaceRepository == null)
            {
                throw new InvalidOperationException("Workspace repositories are not configured.");
            }

        while (!string.IsNullOrEmpty(slug) && await _workspaceRepository.FindBySlugAsync(slug) != null)
        {
            slug = $"{baseSlug}-{i}";
            i++;
        }

        var workspace = new WorkspaceEntity
        {
            Name = workspaceName,
            Slug = slug,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

            await _workspaceRepository.AddAsync(workspace);

        var userWorkspace = new UserWorkspace
        {
            UserId = user.Id,
            WorkspaceId = workspace.Id,
            Accepted = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

            await _userWorkspaceRepository.AddAsync(userWorkspace);

        // create Admin role for the workspace and assign the creator as Admin
        if (_workspaceRoleBootstrapper != null)
        {
            await _workspaceRoleBootstrapper.BootstrapAdminAsync(workspace.Id, user.Id);
        }

        var token = await _tokenRepository.CreateForUserIdAsync(user.Id);
        result.Success = true;
        result.UserId = user.Id;
        result.Token = token.Value;
        
        if (_userWorkspaceRepository != null && _workspaceRepository != null)
        {
            var uw = await _userWorkspaceRepository.FindAcceptedForUserAsync(user.Id);
            if (uw != null)
            {
                var ws = await _workspaceRepository.FindByIdAsync(uw.WorkspaceId);
                if (ws != null)
                {
                    result.WorkspaceSlug = ws.Slug;
                }
            }
        }
        result.WorkspaceSlug = workspace.Slug;
        return result;
    }
}
