namespace Tickflo.Web.TagHelpers;

using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

[HtmlTargetElement("a", Attributes = "asp-preserve-query")]
public class PreserveQueryAnchorTagHelper : TagHelper
{
    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext? ViewContext { get; set; }

    // Ensure this runs after the default AnchorTagHelper so href is available
    public override int Order => 10000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Remove the marker attribute from the rendered tag
        output.Attributes.RemoveAll("asp-preserve-query");

        var queryString = this.ViewContext?.HttpContext?.Request?.QueryString.ToString();
        if (string.IsNullOrEmpty(queryString) || queryString == "?")
        {
            return;
        }

        var hrefAttr = output.Attributes["href"];
        var href = hrefAttr?.Value?.ToString();
        if (string.IsNullOrEmpty(href))
        {
            return;
        }

        if (href.Contains('?'))
        {
            var append = queryString.StartsWith('?') ? "&" + queryString[1..] : (queryString.StartsWith('&') ? queryString : "&" + queryString);
            output.Attributes.SetAttribute("href", href + append);
        }
        else
        {
            output.Attributes.SetAttribute("href", href + queryString);
        }
    }
}
