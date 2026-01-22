namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel() : PageModel
{
    public void OnGet()
    {
        if (this.Request.Path == "/")
        {
            this.Response.Redirect("/login");
        }
    }
}
