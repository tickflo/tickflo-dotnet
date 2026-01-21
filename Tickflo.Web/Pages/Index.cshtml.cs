namespace Tickflo.Web.Pages;

using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
    private readonly ILogger<IndexModel> logger = logger;

    public void OnGet()
    {
        if (this.Request.Path == "/")
        {
            this.Response.Redirect("/login");
        }
    }
}
