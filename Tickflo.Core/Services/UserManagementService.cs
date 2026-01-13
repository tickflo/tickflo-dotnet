using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Auth;

namespace Tickflo.Core.Services;

/// <summary>
/// Implementation of IUserManagementService.
/// Provides user creation, validation, and update operations.
/// </summary>
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
        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        // Check for duplicate
        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null)
        {
            throw new InvalidOperationException($"A user with email '{email}' already exists.");
        }

        // Create user with hashed password
        var user = new User
        {
            Name = name.Trim(),
            Email = normalizedEmail,
            RecoveryEmail = string.IsNullOrWhiteSpace(recoveryEmail) ? null : recoveryEmail.Trim().ToLowerInvariant(),
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
        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        // Check for email conflicts (excluding current user)
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
        user.RecoveryEmail = string.IsNullOrWhiteSpace(recoveryEmail) ? null : recoveryEmail.Trim().ToLowerInvariant();
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return user;
    }

    public async Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await _userRepository.FindByEmailAsync(normalizedEmail);
        
        if (existing == null)
            return false;

        if (excludeUserId.HasValue && existing.Id == excludeUserId.Value)
            return false;

        return true;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        return await _userRepository.FindByIdAsync(userId);
    }

    public string? ValidateRecoveryEmailDifference(string email, string recoveryEmail)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(recoveryEmail))
            return null;

        if (email.Equals(recoveryEmail, StringComparison.OrdinalIgnoreCase))
        {
            return "Recovery email must be different from your login email.";
        }

        return null;
    }
}
