using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;

namespace Tickflo.Core.Services.Users;

public class UserManagementService : IUserManagementService
{
    private const string ErrorEmailAlreadyExists = "A user with email '{0}' already exists.";
    private const string ErrorUserNotFound = "User {0} not found.";
    private const string ErrorRecoveryEmailSame = "Recovery email must be different from your login email.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserManagementService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> CreateUserAsync(string name, string email, string? recoveryEmail, string password, bool systemAdmin = false)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");
            
        await EnsureEmailNotInUseAsync(normalizedEmail, email);

        var user = BuildNewUser(name, normalizedEmail, recoveryEmail, password, systemAdmin);
        await _userRepository.AddAsync(user);
        
        return user;
    }

    public async Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail == null)
            throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");
            
        await EnsureEmailNotInUseAsync(normalizedEmail, email, userId);

        var user = await GetUserOrThrowAsync(userId);
        UpdateUserFields(user, name, normalizedEmail, recoveryEmail);
        await _userRepository.UpdateAsync(user);
        
        return user;
    }

    public async Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail == null)
            return false;

        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);

        return existing != null && existing.Id != excludeUserId;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _userRepository.FindByIdAsync(userId);
    }

    public string? ValidateRecoveryEmailDifference(string email, string recoveryEmail)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(recoveryEmail))
            return null;

        return email.Equals(recoveryEmail, StringComparison.OrdinalIgnoreCase)
            ? ErrorRecoveryEmailSame
            : null;
    }

    private async Task EnsureEmailNotInUseAsync(string normalizedEmail, string originalEmail, int? excludeUserId = null)
    {
        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null && existing.Id != excludeUserId)
        {
            throw new InvalidOperationException(string.Format(ErrorEmailAlreadyExists, originalEmail));
        }
    }

    private async Task<User> GetUserOrThrowAsync(int userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException(string.Format(ErrorUserNotFound, userId));
        }
        return user;
    }

    private User BuildNewUser(string name, string normalizedEmail, string? recoveryEmail, string password, bool systemAdmin)
    {
        return new User
        {
            Name = name.Trim(),
            Email = normalizedEmail,
            RecoveryEmail = NormalizeEmail(recoveryEmail),
            SystemAdmin = systemAdmin,
            EmailConfirmed = false,
            PasswordHash = _passwordHasher.Hash(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private void UpdateUserFields(User user, string name, string normalizedEmail, string? recoveryEmail)
    {
        user.Name = name.Trim();
        user.Email = normalizedEmail;
        user.RecoveryEmail = NormalizeEmail(recoveryEmail);
        user.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}



