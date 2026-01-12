using Tickflo.Core.Entities;

namespace Tickflo.Core.Services.Auth;

public record TokenValidationResult(bool IsValid, string? ErrorMessage, int? UserId, string? UserEmail);
public record SetPasswordResult(bool Success, string? ErrorMessage, string? LoginToken, string? WorkspaceSlug, int? UserId, string? UserEmail);

public interface IPasswordSetupService
{
    Task<TokenValidationResult> ValidateResetTokenAsync(string tokenValue);
    Task<TokenValidationResult> ValidateInitialUserAsync(int userId);
    Task<SetPasswordResult> SetPasswordWithTokenAsync(string tokenValue, string newPassword);
    Task<SetPasswordResult> SetInitialPasswordAsync(int userId, string newPassword);
}
