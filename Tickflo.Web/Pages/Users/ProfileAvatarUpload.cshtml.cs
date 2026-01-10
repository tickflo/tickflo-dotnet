using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using Tickflo.Core.Services;
using System.IO;
using System.Threading.Tasks;

namespace Tickflo.Web.Pages.Users;

public class ProfileAvatarUploadModel : PageModel
{
    private readonly IUserRepository _userRepo;
    private readonly IImageStorageService _imageStorageService;
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";

    public ProfileAvatarUploadModel(IUserRepository userRepo, IImageStorageService imageStorageService)
    {
        _userRepo = userRepo;
        _imageStorageService = imageStorageService;
    }

    public void OnGet()
    {
        var user = HttpContext.User;
        UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = HttpContext.User;
        UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var file = Request.Form.Files["AvatarImage"];
        
        if (file != null && file.Length > 0 && int.TryParse(UserId, out var uid))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
            {
                Message = "Only JPG, PNG, or GIF images are allowed.";
                return Page();
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                // Use the image storage service to upload avatar
                await _imageStorageService.UploadUserAvatarAsync(uid, stream);
                
                Message = "Avatar updated successfully.";
            }
            catch (Exception ex)
            {
                Message = $"Error uploading avatar: {ex.Message}";
            }
        }
        else
        {
            Message = "No file selected.";
        }
        return Page();
    }
}
