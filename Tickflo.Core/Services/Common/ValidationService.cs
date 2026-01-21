namespace Tickflo.Core.Services.Common;

using System.Text.RegularExpressions;
using Tickflo.Core.Data;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<ValidationError> Errors { get; set; } = [];

    public void AddError(string field, string message)
    {
        this.IsValid = false;
        this.Errors.Add(new ValidationError { Field = field, Message = message });
    }
}

public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public interface IValidationService
{
    public Task<ValidationResult> ValidateEmailAsync(string email, bool checkUniqueness = false);
    public ValidationResult ValidateWorkspaceSlug(string slug);
    public ValidationResult ValidateTicketSubject(string subject);
    public ValidationResult ValidateQuantity(int quantity);
    public ValidationResult ValidateStatusTransition(string currentStatus, string newStatus);
    public Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null);
    public ValidationResult ValidateContactName(string name);
    public ValidationResult ValidatePriceValue(decimal price);
    public Task<ValidationResult> ValidateTeamNameAsync(int workspaceId, string teamName, int? excludeTeamId = null);
}

public partial class ValidationService(
    IUserRepository userRepo,
    IRoleRepository roleRepo,
    ITeamRepository teamRepo) : IValidationService
{
    private const int MaxSlugLength = 30;
    private const int MaxSubjectLength = 255;
    private const int MaxRoleNameLength = 30;
    private const int MaxContactNameLength = 100;
    private const int MaxTeamNameLength = 100;
    private const string SlugPattern = @"^[a-z0-9\-]+$";
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedStatusTransitions = new Dictionary<string, IReadOnlyList<string>>
    {
        { "New", new[] { "Open", "Cancelled" } },
        { "Open", new[] { "InProgress", "Resolved", "Cancelled" } },
        { "InProgress", new[] { "Resolved", "OnHold", "Cancelled" } },
        { "OnHold", new[] { "Open", "Resolved", "Cancelled" } },
        { "Resolved", new[] { "Closed" } },
        { "Closed", new[] { "Open" } },
        { "Cancelled", new[] { "Open" } }
    };

    private readonly IUserRepository _userRepo = userRepo;
    private readonly IRoleRepository _roleRepo = roleRepo;
    private readonly ITeamRepository _teamRepo = teamRepo;

    public async Task<ValidationResult> ValidateEmailAsync(string email, bool checkUniqueness = false)
    {
        var result = new ValidationResult();

        if (!ValidateRequired("Email", email, result))
        {
            return result;
        }

        if (!this.ValidateEmailFormat(email, result))
        {
            return result;
        }

        if (checkUniqueness && await this.IsEmailAlreadyInUseAsync(email))
        {
            result.AddError("Email", "Email address is already in use.");
        }

        return result;
    }

    private async Task<bool> IsEmailAlreadyInUseAsync(string email)
    {
        var existing = await this._userRepo.FindByEmailAsync(email);
        return existing != null;
    }

    public ValidationResult ValidateWorkspaceSlug(string slug)
    {
        var result = new ValidationResult();

        if (!ValidateRequired("Slug", slug, result))
        {
            return result;
        }

        if (!ValidateMaxLength("Slug", slug, MaxSlugLength, result))
        {
            return result;
        }

        if (!MyRegex().IsMatch(slug))
        {
            result.AddError("Slug", "Slug can only contain lowercase letters, numbers, and hyphens.");
        }

        return result;
    }

    public ValidationResult ValidateTicketSubject(string subject)
        => this.ValidateRequiredField("Subject", subject, MaxSubjectLength);

    public ValidationResult ValidateQuantity(int quantity)
    {
        var result = new ValidationResult();

        if (quantity < 0)
        {
            result.AddError("Quantity", "Quantity cannot be negative.");
        }

        return result;
    }

    public ValidationResult ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var result = new ValidationResult();

        if (!IsValidStatus(currentStatus))
        {
            result.AddError("Status", $"Unknown status '{currentStatus}'.");
            return result;
        }

        if (!IsTransitionAllowed(currentStatus, newStatus))
        {
            result.AddError("Status", $"Cannot transition from '{currentStatus}' to '{newStatus}'.");
        }

        return result;
    }

    private static bool IsValidStatus(string status) => AllowedStatusTransitions.ContainsKey(status);

    private static bool IsTransitionAllowed(string currentStatus, string newStatus) => AllowedStatusTransitions.TryGetValue(currentStatus, out var allowedTargets)
            && allowedTargets.Contains(newStatus);

    public async Task<ValidationResult> ValidateRoleNameAsync(int workspaceId, string roleName, int? excludeRoleId = null)
    {
        var result = new ValidationResult();

        if (!ValidateRequired("Name", roleName, result))
        {
            return result;
        }

        if (!ValidateMaxLength("Name", roleName, MaxRoleNameLength, result))
        {
            return result;
        }

        if (await this.IsRoleNameDuplicateAsync(workspaceId, roleName, excludeRoleId))
        {
            result.AddError("Name", "A role with this name already exists in this workspace.");
        }

        return result;
    }

    private async Task<bool> IsRoleNameDuplicateAsync(int workspaceId, string roleName, int? excludeRoleId)
    {
        var existing = await this._roleRepo.FindByNameAsync(workspaceId, roleName);
        return existing != null && (excludeRoleId == null || existing.Id != excludeRoleId.Value);
    }

    public ValidationResult ValidateContactName(string name)
        => this.ValidateRequiredField("Name", name, MaxContactNameLength);

    public ValidationResult ValidatePriceValue(decimal price)
    {
        var result = new ValidationResult();

        if (price < 0)
        {
            result.AddError("Price", "Price cannot be negative.");
        }

        return result;
    }

    public async Task<ValidationResult> ValidateTeamNameAsync(int workspaceId, string teamName, int? excludeTeamId = null)
    {
        var result = new ValidationResult();

        if (!ValidateRequired("Name", teamName, result))
        {
            return result;
        }

        ValidateMaxLength("Name", teamName, MaxTeamNameLength, result);
        return result;
    }

    private static bool ValidateRequired(string field, string value, ValidationResult result)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        result.AddError(field, $"{field} is required.");
        return false;
    }

    private static bool ValidateMaxLength(string field, string value, int maxLength, ValidationResult result)
    {
        if (value?.Length <= maxLength)
        {
            return true;
        }

        result.AddError(field, $"{field} must be {maxLength} characters or less.");
        return false;
    }

    private bool ValidateEmailFormat(string email, ValidationResult result)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            result.AddError("Email", "Invalid email format.");
            return false;
        }
    }

    private ValidationResult ValidateRequiredField(string fieldName, string value, int maxLength)
    {
        var result = new ValidationResult();

        if (!ValidateRequired(fieldName, value, result))
        {
            return result;
        }

        ValidateMaxLength(fieldName, value, maxLength, result);
        return result;
    }

    [GeneratedRegex(SlugPattern)]
    private static partial Regex MyRegex();
}
