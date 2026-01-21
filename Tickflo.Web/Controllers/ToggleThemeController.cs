namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[AllowAnonymous]
[Route("toggle-theme")]
public class ToggleThemeController : Controller
{
    [HttpPost]
    public IActionResult ToggleTheme()
    {
        var theme = this.Request.Cookies["theme"];
        var newTheme = theme == "dark" ? "light" : "dark";
        this.Response.Cookies.Append("theme", newTheme, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });

        var referer = this.Request.Headers.Referer.ToString();
        var redirectUrl = string.IsNullOrWhiteSpace(referer) ? "/" : referer;
        return this.Redirect(redirectUrl);
    }
}
