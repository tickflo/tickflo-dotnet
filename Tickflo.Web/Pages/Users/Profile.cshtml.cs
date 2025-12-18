using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Users;

public class ProfileModel : PageModel
{
    private readonly IUserRepository _userRepo;
    [BindProperty]
    public string UserId { get; set; } = "";
    [BindProperty]
    public string UserName { get; set; } = "";
    [BindProperty]
    public string Email { get; set; } = "";

    public ProfileModel(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task OnGetAsync()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && int.TryParse(userId, out var uid))
        {
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null)
            {
                UserId = u.Id.ToString();
                UserName = u.Name;
                Email = u.Email;
            }
        }
        } // Added missing closing brace for OnGetAsync method

    public async Task<IActionResult> OnPostAsync()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null && int.TryParse(userId, out var uid))
        {
            var u = await _userRepo.FindByIdAsync(uid);
            if (u != null)
            {
                u.Name = UserName;
                u.Email = Email;
                await _userRepo.UpdateAsync(u);
            }
        }
        return RedirectToPage();
    }
}
