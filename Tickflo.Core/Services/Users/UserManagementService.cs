namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;

public class UserManagementService(IUserRepository userRepository, IPasswordHasher passwordHasher) : IUserManagementService
{
    private const string ErrorEmailAlreadyExists = "A user with email '{0}' already exists.";
    private const string ErrorUserNotFound = "User {0} not found.";
    private const string ErrorRecoveryEmailSame = "Recovery email must be different from your login email.";

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<User> CreateUserAsync(string name, string email, string? recoveryEmail, string password, bool systemAdmin = false)
    {
        var normalizedEmail = NormalizeEmail(email) ?? throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");

        await this.EnsureEmailNotInUseAsync(normalizedEmail, email);

        var user = this.BuildNewUser(name, normalizedEmail, recoveryEmail, password, systemAdmin);
        await this._userRepository.AddAsync(user);

        return user;
    }

    public async Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail)
    {
        var normalizedEmail = NormalizeEmail(email) ?? throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");

        await this.EnsureEmailNotInUseAsync(normalizedEmail, email, userId);

        var user = await this.GetUserOrThrowAsync(userId);
        UpdateUserFields(user, name, normalizedEmail, recoveryEmail);
        await this._userRepository.UpdateAsync(user);

        return user;
    }

    public async Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail == null)
        {
            return false;
        }

        var existing = await this._userRepository.FindByEmailAsync(normalizedEmail);

        return existing != null && existing.Id != excludeUserId;
    }

    public async Task<User?> GetUserAsync(int userId) => await this._userRepository.FindByIdAsync(userId);

    public string? ValidateRecoveryEmailDifference(string email, string recoveryEmail)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(recoveryEmail))
        {
            return null;
        }

        return email.Equals(recoveryEmail, StringComparison.OrdinalIgnoreCase)
            ? ErrorRecoveryEmailSame
            : null;
    }

    private async Task EnsureEmailNotInUseAsync(string normalizedEmail, string originalEmail, int? excludeUserId = null)
    {
        var existing = await this._userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null && existing.Id != excludeUserId)
        {
            throw new InvalidOperationException(string.Format(ErrorEmailAlreadyExists, originalEmail));
        }
    }

    private async Task<User> GetUserOrThrowAsync(int userId)
    {
        var user = await this._userRepository.FindByIdAsync(userId) ?? throw new InvalidOperationException(string.Format(ErrorUserNotFound, userId));
        return user;
    }

    private User BuildNewUser(string name, string normalizedEmail, string? recoveryEmail, string password, bool systemAdmin) => new()
    {
        Name = name.Trim(),
        Email = normalizedEmail,
        RecoveryEmail = NormalizeEmail(recoveryEmail),
        SystemAdmin = systemAdmin,
        EmailConfirmed = false,
        PasswordHash = this._passwordHasher.Hash(password),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static void UpdateUserFields(User user, string name, string normalizedEmail, string? recoveryEmail)
    {
        user.Name = name.Trim();
        user.Email = normalizedEmail;
        user.RecoveryEmail = NormalizeEmail(recoveryEmail);
        user.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}



