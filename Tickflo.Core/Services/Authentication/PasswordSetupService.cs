namespace Tickflo.Core.Services.Authentication;

using Tickflo.Core.Data;

public class PasswordSetupService(
    IUserRepository userRepository,
    ITokenRepository tokenRepository,
    IPasswordHasher passwordHasher,
    IUserWorkspaceRepository userWorkspaceRepository,
    IWorkspaceRepository workspaceRepository) : IPasswordSetupService
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly ITokenRepository tokenRepository = tokenRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;
    private readonly IUserWorkspaceRepository userWorkspaceRepository = userWorkspaceRepository;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;

    public async Task<TokenValidationResult> ValidateResetTokenAsync(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return new TokenValidationResult(false, "Missing token.", null, null);
        }

        var token = await this.tokenRepository.FindByValueAsync(tokenValue);
        if (token == null)
        {
            return new TokenValidationResult(false, "Invalid or expired token.", null, null);
        }

        var user = await this.userRepository.FindByIdAsync(token.UserId);
        if (user == null)
        {
            return new TokenValidationResult(false, "User not found.", null, null);
        }

        return new TokenValidationResult(true, null, user.Id, user.Email);
    }

    public async Task<TokenValidationResult> ValidateInitialUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return new TokenValidationResult(false, "Missing user id.", null, null);
        }

        var user = await this.userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return new TokenValidationResult(false, "User not found.", null, null);
        }

        if (user.PasswordHash != null)
        {
            return new TokenValidationResult(false, "Password already set.", user.Id, user.Email);
        }

        return new TokenValidationResult(true, null, user.Id, user.Email);
    }

    public async Task<SetPasswordResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword)
    {
        var validation = await this.ValidateResetTokenAsync(tokenValue);
        if (!validation.IsValid || validation.UserId == null)
        {
            return new SetPasswordResult(false, validation.ErrorMessage, null, null, null, null);
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return new SetPasswordResult(false, "Password must be at least 8 characters long.", null, null, validation.UserId, validation.UserEmail);
        }

        var user = await this.userRepository.FindByIdAsync(validation.UserId.Value);
        if (user == null)
        {
            return new SetPasswordResult(false, "User not found.", null, null, null, null);
        }

        var passwordHash = this.passwordHasher.Hash($"{user.Email}{newPassword}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        await this.userRepository.UpdateAsync(user);

        return new SetPasswordResult(true, null, null, null, user.Id, user.Email);
    }

    public async Task<SetPasswordResult> SetInitialPasswordAsync(int userId, string newPassword)
    {
        var user = await this.userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            return new SetPasswordResult(false, "User not found.", null, null, null, null);
        }

        if (user.PasswordHash != null)
        {
            return new SetPasswordResult(false, "Password already set.", null, null, user.Id, user.Email);
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            return new SetPasswordResult(false, "Password must be at least 8 characters long.", null, null, user.Id, user.Email);
        }

        var passwordHash = this.passwordHasher.Hash($"{user.Email}{newPassword}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        await this.userRepository.UpdateAsync(user);

        var loginToken = await this.tokenRepository.CreateForUserIdAsync(user.Id);

        string? workspaceSlug = null;
        var userWorkspace = await this.userWorkspaceRepository.FindAcceptedForUserAsync(user.Id);
        if (userWorkspace != null)
        {
            var workspace = await this.workspaceRepository.FindByIdAsync(userWorkspace.WorkspaceId);
            workspaceSlug = workspace?.Slug;
        }

        return new SetPasswordResult(true, null, loginToken.Value, workspaceSlug, user.Id, user.Email);
    }
}


