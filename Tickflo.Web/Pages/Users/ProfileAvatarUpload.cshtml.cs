namespace Tickflo.Web.Pages.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Storage;

[Authorize]
public class ProfileAvatarUploadModel(IImageStorageService imageStorageService, ICurrentUserService currentUserService) : PageModel
{
    private readonly IImageStorageService imageStorageService = imageStorageService;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private const long MaxAvatarSize = 5 * 1024 * 1024; // 5 MB
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

        if (file == null || file.Length == 0)
        {
            this.Message = "No file selected.";
            return this.Page();
        }

        // Validate file size
        if (file.Length > MaxAvatarSize)
        {
            this.Message = "Image too large. Maximum size: 5 MB.";
            return this.Page();
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".gif")
        {
            this.Message = "Only JPG, PNG, or GIF images are allowed.";
            return this.Page();
        }

        try
        {
            // Validate magic bytes before processing (prevents renamed file attacks)
            using var validationStream = file.OpenReadStream();
            if (!this.imageStorageService.IsValidImage(validationStream))
            {
                this.Message = "Invalid image file. The file content does not match its extension.";
                return this.Page();
            }

            // Stream directly to the upload service — no MemoryStream buffering
            using var uploadStream = file.OpenReadStream();
            await this.imageStorageService.UploadUserAvatarAsync(uid, uploadStream);

            this.Message = "Avatar updated successfully.";
        }
        catch (Exception ex)
        {
            this.Message = $"Error uploading avatar: {ex.Message}";
        }

        return this.Page();
    }
}
