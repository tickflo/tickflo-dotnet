namespace Tickflo.Core.Services.Authentication;

using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
public record TokenValidationResult(bool IsValid, string? ErrorMessage, int? UserId, string? UserEmail);
public record SetPasswordResult(bool Success, string? ErrorMessage, string? LoginToken, string? WorkspaceSlug, int? UserId, string? UserEmail);

public interface IPasswordSetupService
{
    public Task<TokenValidationResult> ValidateResetTokenAsync(string tokenValue);
    public Task<TokenValidationResult> ValidateInitialUserAsync(int userId);
    public Task<SetPasswordResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword);
    public Task<SetPasswordResult> SetInitialPasswordAsync(int userId, string newPassword);
}


public class PasswordSetupService(
    TickfloDbContext db,
    TickfloConfig config,
    IPasswordHasher passwordHasher
    ) : IPasswordSetupService
{
    private readonly TickfloDbContext db = db;
    private readonly TickfloConfig config = config;
    private readonly IPasswordHasher passwordHasher = passwordHasher;

    public async Task<TokenValidationResult> ValidateResetTokenAsync(string tokenValue)
    {
        if (string.IsNullOrWhiteSpace(tokenValue))
        {
            return new TokenValidationResult(false, "Missing token.", null, null);
        }

        var token = await this.db.Tokens.FirstOrDefaultAsync(t => t.Value == tokenValue);
        if (token == null)
        {
            return new TokenValidationResult(false, "Invalid or expired token.", null, null);
        }

        var user = await this.db.Users.FindAsync(token.UserId);
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

        var user = await this.db.Users.FindAsync(userId);
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

        var user = await this.db.Users.FindAsync(validation.UserId.Value);
        if (user == null)
        {
            return new SetPasswordResult(false, "User not found.", null, null, null, null);
        }

        var passwordHash = this.passwordHasher.Hash($"{user.Email}{newPassword}");
        user.PasswordHash = passwordHash;
        user.UpdatedAt = DateTime.UtcNow;
        this.db.Users.Update(user);
        await this.db.SaveChangesAsync();

        return new SetPasswordResult(true, null, null, null, user.Id, user.Email);
    }

    public async Task<SetPasswordResult> SetInitialPasswordAsync(int userId, string newPassword)
    {
        var user = await this.db.Users.FindAsync(userId);
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
        this.db.Users.Update(user);

        var token = new Token(user.Id, this.config.SessionTimeoutMinutes * 60);
        await this.db.Tokens.AddAsync(token);

        string? workspaceSlug = null;
        var userWorkspace = await this.db.UserWorkspaces.Include(w => w.Workspace).FirstOrDefaultAsync(w => w.UserId == user.Id && w.Accepted);
        if (userWorkspace != null)
        {
            workspaceSlug = userWorkspace.Workspace.Slug;
        }
        return new SetPasswordResult(true, null, token.Value, workspaceSlug, user.Id, user.Email);
    }
}


