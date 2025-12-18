using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Tickflo.Core.Data;
using System.IO;
using System.Threading.Tasks;
using Tickflo.Core.Config;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace Tickflo.Web.Pages.Users;

public class ProfileAvatarUploadModel : PageModel
{
    private readonly IUserRepository _userRepo;
    private readonly IWebHostEnvironment _env;
    private readonly TickfloConfig _config;
    private readonly IAmazonS3 _s3Client;
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";

    public ProfileAvatarUploadModel(IUserRepository userRepo, IWebHostEnvironment env, TickfloConfig config, IAmazonS3 s3Client)
    {
        _userRepo = userRepo;
        _env = env;
        _config = config;
        _s3Client = s3Client;
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
            // Compress and save to a memory stream
            using (var inputMs = new MemoryStream())
            using (var outputMs = new MemoryStream())
            {
                await file.CopyToAsync(inputMs);
                inputMs.Position = 0;
                // Use ImageHelper to compress and save to outputMs
                Utils.ImageHelper.CompressAndSave(inputMs, outputMs, 256, 256, 75L);
                outputMs.Position = 0;
                // Upload to S3
                var bucket = _config.S3_BUCKET;
                var key = $"user-data/{UserId}/avatar.jpg";
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = outputMs,
                    Key = key,
                    BucketName = bucket,
                    ContentType = "image/jpeg",
                    CannedACL = S3CannedACL.PublicRead // or adjust as needed
                };
                var transferUtility = new TransferUtility(_s3Client);
                await transferUtility.UploadAsync(uploadRequest);
            }
            Message = "Avatar updated and uploaded to S3 successfully.";
        }
        else
        {
            Message = "No file selected.";
        }
        return Page();
    }
}
