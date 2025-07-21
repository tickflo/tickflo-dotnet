using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services;

namespace Tickflo.Web.Pages;

public class LoginModel(ILogger<LoginModel> logger, IAuthenticationService authService) : PageModel
{
    private readonly ILogger<LoginModel> _logger = logger;
    private readonly IAuthenticationService _authService = authService;

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _authService.AuthenticateAsync(Input.Email, Input.Password);
        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            return Page();
        }

        return Page();
    }
}

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

