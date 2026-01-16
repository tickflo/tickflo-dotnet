using System.Text.RegularExpressions;
using Tickflo.Core.Data;

namespace Tickflo.Core.Services.Common;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public interface IValidationService
{
    Task<ValidationResult> ValidateEmailAsync(string email, bool checkUniqueness = false);
    ValidationResult ValidateWorkspaceSlug(string slug);
    ValidationResult ValidateTicketSubject(string subject);
    ValidationResult ValidateQuantity(int quantity);
    ValidationResult ValidateStatusTransition(string currentStatus, string newStatus);
    Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null);
    ValidationResult ValidateContactName(string name);
    ValidationResult ValidatePriceValue(decimal price);
    Task<ValidationResult> ValidateTeamNameAsync(int workspaceId, string teamName, int? excludeTeamId = null);
}

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
        if (!ValidateRequired("Email", email, out var result))
            return result;

        if (!ValidateEmailFormat(email, out result))
            return result;

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
        if (!ValidateRequired("Slug", slug, out var result))
            return result;

        if (!ValidateMaxLength("Slug", slug, 30, out result))
            return result;

        if (!Regex.IsMatch(slug, @"^[a-z0-9\-]+$"))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Slug", Message = "Slug can only contain lowercase letters, numbers, and hyphens." });
        }

        return result;
    }

    public ValidationResult ValidateTicketSubject(string subject)
        => ValidateRequiredField("Subject", subject, 255);

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
            result.Errors.Add(new ValidationError { Field = "Status", Message = $"Cannot transition from '{currentStatus}' to '{newStatus}'." });
        }

        return result;
    }

    public async Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null)
    {
        if (!ValidateRequired("Name", roleName, out var result))
            return result;

        if (!ValidateMaxLength("Name", roleName, 30, out result))
            return result;

        var existing = await _roleRepo.FindByNameAsync(workspaceId, roleName);
        if (existing != null && (excludeRoleId == null || existing.Id != excludeRoleId.Value))
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Name", Message = "A role with this name already exists in this workspace." });
        }

        return result;
    }

    public ValidationResult ValidateContactName(string name)
        => ValidateRequiredField("Name", name, 100);

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
        if (!ValidateRequired("Name", teamName, out var result))
            return result;

        return ValidateMaxLength("Name", teamName, 100, out result) ? result : result;
    }

    private bool ValidateRequired(string field, string value, out ValidationResult result)
    {
        result = new ValidationResult();
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        result.IsValid = false;
        result.Errors.Add(new ValidationError { Field = field, Message = $"{field} is required." });
        return false;
    }

    private bool ValidateMaxLength(string field, string value, int maxLength, out ValidationResult result)
    {
        result = new ValidationResult();
        if (value?.Length <= maxLength)
            return true;

        result.IsValid = false;
        result.Errors.Add(new ValidationError { Field = field, Message = $"{field} must be {maxLength} characters or less." });
        return false;
    }

    private bool ValidateEmailFormat(string email, out ValidationResult result)
    {
        result = new ValidationResult();
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError { Field = "Email", Message = "Invalid email format." });
            return false;
        }
    }

    private ValidationResult ValidateRequiredField(string fieldName, string value, int maxLength)
    {
        if (!ValidateRequired(fieldName, value, out var result))
            return result;

        return ValidateMaxLength(fieldName, value, maxLength, out result) ? result : result;
    }
}
