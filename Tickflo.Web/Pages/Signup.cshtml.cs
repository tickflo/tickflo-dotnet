using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class SignupModel : PageModel
{
    private readonly ILogger<SignupModel> _logger;

    public SignupModel(ILogger<SignupModel> logger)
    {
        _logger = logger;
    }

    [BindProperty]
    public SignupInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IActionResult OnPost()
    {
        // Custom validation: Recovery email must be different from login email
        if (!string.IsNullOrEmpty(Input.Email) && !string.IsNullOrEmpty(Input.RecoveryEmail) &&
            Input.Email.Equals(Input.RecoveryEmail, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.RecoveryEmail", "Recovery email must be different from your login email.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // TODO: Implement signup logic
        // This is a stub method - implement the actual signup logic here
        // 1. Validate that recovery email is different from login email
        // 2. Check if email already exists
        // 3. Hash password
        // 4. Create user account
        // 5. Create workspace
        // 6. Generate authentication token
        // 7. Set cookie and redirect

        // For now, just return an error message
        ErrorMessage = "Signup functionality not yet implemented";
        return Page();
    }
}

public class SignupInput
{
    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Recovery Email")]
    public string RecoveryEmail { get; set; } = "";

    [Required]
    [Display(Name = "Workspace Name")]
    [StringLength(50, MinimumLength = 1)]
    public string WorkspaceName { get; set; } = "";

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = "";
}
