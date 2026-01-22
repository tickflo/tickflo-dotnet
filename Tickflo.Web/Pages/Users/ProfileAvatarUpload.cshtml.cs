namespace Tickflo.Web.Pages.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Storage;

[Authorize]
public class ProfileAvatarUploadModel(IUserRepository userRepository, IImageStorageService imageStorageService, ICurrentUserService currentUserService) : PageModel
{
    private readonly IUserRepository userRepository = userRepository;
    private readonly IImageStorageService _imageStorageService = imageStorageService;
    private readonly ICurrentUserService currentUserService = currentUserService;
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";

    public void OnGet() => this.UserId = this.currentUserService.TryGetUserId(this.User, out var uid) ? uid.ToString() : "";

    public async Task<IActionResult> OnPostAsync()
    {
        if (!this.currentUserService.TryGetUserId(this.User, out var uid))
        {
            return this.Challenge();
        }

        this.UserId = uid.ToString();
        var file = this.Request.Form.Files["AvatarImage"];

        if (file != null && file.Length > 0)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".gif")
            {
                this.Message = "Only JPG, PNG, or GIF images are allowed.";
                return this.Page();
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                // Use the image storage service to upload avatar
                await this._imageStorageService.UploadUserAvatarAsync(uid, stream);

                this.Message = "Avatar updated successfully.";
            }
            catch (Exception ex)
            {
                this.Message = $"Error uploading avatar: {ex.Message}";
            }
        }
        else
        {
            this.Message = "No file selected.";
        }
        return this.Page();
    }
}


