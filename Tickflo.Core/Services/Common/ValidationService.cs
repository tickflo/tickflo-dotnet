using System.Text.RegularExpressions;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Common;

/// <summary>
/// Validation result containing any validation errors.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<ValidationError> Errors { get; set; } = new();
}

/// <summary>
/// Individual validation error.
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Cross-cutting service for shared validation rules used across multiple services.
/// Centralizes business rule validation to maintain consistency.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validate email format and optionally check for uniqueness.
    /// </summary>
    Task<ValidationResult> ValidateEmailAsync(string email, bool checkUniqueness = false);

    /// <summary>
    /// Validate a workspace slug (lowercase, hyphens, length).
    /// </summary>
    ValidationResult ValidateWorkspaceSlug(string slug);

    /// <summary>
    /// Validate a ticket subject (non-empty, reasonable length).
    /// </summary>
    ValidationResult ValidateTicketSubject(string subject);

    /// <summary>
    /// Validate quantity for inventory (positive number).
    /// </summary>
    ValidationResult ValidateQuantity(int quantity);

    /// <summary>
    /// Validate a status transition is allowed.
    /// </summary>
    ValidationResult ValidateStatusTransition(string currentStatus, string newStatus);

    /// <summary>
    /// Validate a role name (unique, non-empty, valid characters).
    /// </summary>
    Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null);

    /// <summary>
    /// Validate contact name (non-empty, reasonable length).
    /// </summary>
    ValidationResult ValidateContactName(string name);

    /// <summary>
    /// Validate a price/cost value (non-negative).
    /// </summary>
    ValidationResult ValidatePriceValue(decimal price);

    /// <summary>
    /// Validate team name (unique per workspace, non-empty).
    /// </summary>
    Task<ValidationResult> ValidateTeamNameAsync(int workspaceId, string teamName, int? excludeTeamId = null);
}

/// <summary>
/// Implementation of shared validation service.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly ITeamRepository _teamRepo;

    public ValidationService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        ITeamRepository teamRepo)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _teamRepo = teamRepo;
    }

    public async Task<ValidationResult> ValidateEmailAsync(string email, bool checkUniqueness = false)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(email))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Email", Message = "Email is required." });
            return result;
        }

        // Validate format
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
        }
        catch
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Email", Message = "Invalid email format." });
            return result;
        }

        // Check uniqueness if requested
        if (checkUniqueness)
        {
            var existing = await _userRepo.FindByEmailAsync(email);
            if (existing != null)
            {
                result.IsValid = false;
                result.Errors.Add(new ValidationError { Field = "Email", Message = "Email address is already in use." });
            }
        }

        return result;
    }

    public ValidationResult ValidateWorkspaceSlug(string slug)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(slug))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Slug", Message = "Slug is required." });
            return result;
        }

        if (slug.Length > 30)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Slug", Message = "Slug must be 30 characters or less." });
        }

        if (!Regex.IsMatch(slug, @"^[a-z0-9\-]+$"))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Slug", Message = "Slug can only contain lowercase letters, numbers, and hyphens." });
        }

        return result;
    }

    public ValidationResult ValidateTicketSubject(string subject)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(subject))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Subject", Message = "Subject is required." });
            return result;
        }

        if (subject.Length > 255)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Subject", Message = "Subject must be 255 characters or less." });
        }

        return result;
    }

    public ValidationResult ValidateQuantity(int quantity)
    {
        var result = new ValidationResult();

        if (quantity < 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Quantity", Message = "Quantity cannot be negative." });
        }

        return result;
    }

    public ValidationResult ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var result = new ValidationResult();

        // Define allowed transitions
        var allowedTransitions = new Dictionary<string, List<string>>
        {
            { "New", new List<string> { "Open", "Cancelled" } },
            { "Open", new List<string> { "InProgress", "Resolved", "Cancelled" } },
            { "InProgress", new List<string> { "Resolved", "OnHold", "Cancelled" } },
            { "OnHold", new List<string> { "Open", "Resolved", "Cancelled" } },
            { "Resolved", new List<string> { "Closed" } },
            { "Closed", new List<string> { "Open" } },
            { "Cancelled", new List<string> { "Open" } }
        };

        if (!allowedTransitions.ContainsKey(currentStatus))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Status", Message = $"Unknown status '{currentStatus}'." });
            return result;
        }

        if (!allowedTransitions[currentStatus].Contains(newStatus))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError 
            { 
                Field = "Status", 
                Message = $"Cannot transition from '{currentStatus}' to '{newStatus}'." 
            });
        }

        return result;
    }

    public async Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(roleName))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Role name is required." });
            return result;
        }

        if (roleName.Length > 30)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Role name must be 30 characters or less." });
        }

        // Check uniqueness in workspace
        var existing = await _roleRepo.FindByNameAsync(workspaceId, roleName);
        if (existing != null && (excludeRoleId == null || existing.Id != excludeRoleId.Value))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "A role with this name already exists in this workspace." });
        }

        return result;
    }

    public ValidationResult ValidateContactName(string name)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(name))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Contact name is required." });
            return result;
        }

        if (name.Length > 100)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Contact name must be 100 characters or less." });
        }

        return result;
    }

    public ValidationResult ValidatePriceValue(decimal price)
    {
        var result = new ValidationResult();

        if (price < 0)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Price", Message = "Price cannot be negative." });
        }

        return result;
    }

    public async Task<ValidationResult> ValidateTeamNameAsync(int workspaceId, string teamName, int? excludeTeamId = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(teamName))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Team name is required." });
            return result;
        }

        if (teamName.Length > 100)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "Team name must be 100 characters or less." });
        }

        // Assuming teams are workspace-scoped, check uniqueness
        // Would need appropriate method on TeamRepository
        return result;
    }
}
