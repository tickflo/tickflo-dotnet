namespace Tickflo.Core.Services.Authentication;

public record TokenValidationResult(bool IsValid, string? ErrorMessage, int? UserId, string? UserEmail);
public record SetPasswordResult(bool Success, string? ErrorMessage, string? LoginToken, string? WorkspaceSlug, int? UserId, string? UserEmail);

public interface IPasswordSetupService
{
    public Task<TokenValidationResult> ValidateResetTokenAsync(string tokenValue);
    public Task<TokenValidationResult> ValidateInitialUserAsync(int userId);
    public Task<SetPasswordResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword);
    public Task<SetPasswordResult> SetInitialPasswordAsync(int userId, string newPassword);
}


