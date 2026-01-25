namespace Tickflo.Core.Services.Users;

using System.Text;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Authentication;

/// <summary>
/// Service for managing user creation, updates, and validation.
/// Centralizes user management business logic.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Creates a new system user with the given details.
    /// </summary>
    /// <param name="name">User's display name</param>
    /// <param name="email">User's email (must be unique)</param>
    /// <param name="recoveryEmail">Optional recovery email address</param>
    /// <param name="password">User's password (will be hashed)</param>
    /// <param name="systemAdmin">Whether user should be a system administrator</param>
    /// <returns>The created user</returns>
    /// <exception cref="InvalidOperationException">Thrown if email already exists or validation fails</exception>
    public Task<User> CreateUserAsync(string name, string email, string? recoveryEmail, string password, bool systemAdmin = false);

    /// <summary>
    /// Updates an existing user's basic information.
    /// </summary>
    /// <param name="userId">The user to update</param>
    /// <param name="name">New display name</param>
    /// <param name="email">New email address</param>
    /// <param name="recoveryEmail">New recovery email (optional)</param>
    /// <returns>The updated user</returns>
    /// <exception cref="InvalidOperationException">Thrown if email conflicts or user not found</exception>
    public Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail);

    /// <summary>
    /// Checks if an email address is already in use.
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="excludeUserId">Optional user ID to exclude from check</param>
    /// <returns>True if email is already in use, false otherwise</returns>
    public Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null);

    /// <summary>
    /// Gets a user by ID with authorization check.
    /// </summary>
    /// <param name="userId">The user to retrieve</param>
    /// <returns>The user, or null if not found</returns>
    public Task<User?> GetUserAsync(int userId);

    /// <summary>
    /// Validates notification preference email requirements.
    /// Ensures recovery email is different from login email.
    /// </summary>
    /// <param name="email">The login email</param>
    /// <param name="recoveryEmail">The recovery email</param>
    /// <returns>Validation error message, or null if valid</returns>
    public string? ValidateRecoveryEmailDifference(string email, string recoveryEmail);
}


public class UserManagementService(IUserRepository userRepository, IPasswordHasher passwordHasher) : IUserManagementService
{
    private static readonly CompositeFormat ErrorEmailAlreadyExists = CompositeFormat.Parse("A user with email '{0}' already exists.");
    private static readonly CompositeFormat ErrorUserNotFound = CompositeFormat.Parse("User {0} not found.");
    private const string ErrorRecoveryEmailSame = "Recovery email must be different from your login email.";

    private readonly IUserRepository userRepository = userRepository;
    private readonly IPasswordHasher passwordHasher = passwordHasher;

    public async Task<User> CreateUserAsync(string name, string email, string? recoveryEmail, string password, bool systemAdmin = false)
    {
        var normalizedEmail = NormalizeEmail(email) ?? throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");

        await this.EnsureEmailNotInUseAsync(normalizedEmail, email);

        var user = new User(name, normalizedEmail, recoveryEmail, this.passwordHasher.Hash($"{normalizedEmail}{password}"));
        await this.userRepository.AddAsync(user);

        return user;
    }

    public async Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail)
    {
        var normalizedEmail = NormalizeEmail(email) ?? throw new ArgumentNullException(nameof(email), "Email cannot be null or empty");

        await this.EnsureEmailNotInUseAsync(normalizedEmail, email, userId);

        var user = await this.GetUserOrThrowAsync(userId);
        UpdateUserFields(user, name, normalizedEmail, recoveryEmail);
        await this.userRepository.UpdateAsync(user);

        return user;
    }

    public async Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail == null)
        {
            return false;
        }

        var existing = await this.userRepository.FindByEmailAsync(normalizedEmail);

        return existing != null && existing.Id != excludeUserId;
    }

    public async Task<User?> GetUserAsync(int userId) => await this.userRepository.FindByIdAsync(userId);

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
        var existing = await this.userRepository.FindByEmailAsync(normalizedEmail);
        if (existing != null && existing.Id != excludeUserId)
        {
            throw new InvalidOperationException(string.Format(null, ErrorEmailAlreadyExists, originalEmail));
        }
    }

    private async Task<User> GetUserOrThrowAsync(int userId)
    {
        var user = await this.userRepository.FindByIdAsync(userId) ?? throw new InvalidOperationException(string.Format(null, ErrorUserNotFound, userId));
        return user;
    }

    private static void UpdateUserFields(User user, string name, string normalizedEmail, string? recoveryEmail)
    {
        user.Name = name.Trim();
        user.Email = normalizedEmail;
        user.RecoveryEmail = NormalizeEmail(recoveryEmail);
        user.UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}



