using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;

namespace Tickflo.Core.Services.Users;

public class UserManagementService : IUserManagementService
{
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

        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null)
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        var user = new User
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

        await _userRepository.AddAsync(user);
        return user;
    }

    public async Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail)
    {
        var normalizedEmail = NormalizeEmail(email);

        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null && existing.Id != userId)
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        var user = await _userRepository.FindByIdAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found.");
        }

        user.Name = name.Trim();
        user.Email = normalizedEmail;
        user.RecoveryEmail = NormalizeEmail(recoveryEmail);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return user;
    }

    public async Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);

        if (existing == null)
            return false;

        return !excludeUserId.HasValue || existing.Id != excludeUserId.Value;
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
            ? "Recovery email must be different from your login email."
            : null;
    }

    private static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}



