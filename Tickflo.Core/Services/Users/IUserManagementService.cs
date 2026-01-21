namespace Tickflo.Core.Services.Users;

using Tickflo.Core.Entities;

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


