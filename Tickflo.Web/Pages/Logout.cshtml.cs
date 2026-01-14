using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Tickflo.Web.Pages;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        return SignOutAndRedirect();
    }

    public IActionResult OnPost()
    {
        return SignOutAndRedirect();
    }

    private IActionResult SignOutAndRedirect()
    {
        // Delete the auth cookie and clear session to fully sign out.
        Response.Cookies.Delete("user_token", new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });

        HttpContext.Session.Clear();

        return Redirect("/login");
    }
}
