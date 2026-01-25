namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Config;

[AllowAnonymous]
public class LogoutModel(TickfloConfig config) : PageModel
{
    private readonly TickfloConfig config = config;
    public IActionResult OnGet() => this.SignOutAndRedirect();

    public IActionResult OnPost() => this.SignOutAndRedirect();

    private RedirectResult SignOutAndRedirect()
    {
        // Delete the auth cookie and clear session to fully sign out.
        this.Response.Cookies.Delete(this.config.SessionCookieName, new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = this.Request.IsHttps,
            SameSite = SameSiteMode.Lax
        });

        this.HttpContext.Session.Clear();

        return this.Redirect("/login");
    }
}
