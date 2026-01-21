namespace Tickflo.Web.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

using Tickflo.Core.Services.Common;
using Tickflo.Core.Services.Storage;

/// <summary>
/// REST API controller for file and image management using RustFS.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController(
    IFileStorageService fileStorageService,
    IImageStorageService imageStorageService,
    IFileStorageRepository fileRepository,
    IWorkspaceRepository workspaceRepository,
    ILogger<FilesController> logger,
    ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IFileStorageService fileStorageService = fileStorageService;
    private readonly IImageStorageService imageStorageService = imageStorageService;
    private readonly IFileStorageRepository fileRepository = fileRepository;
    private readonly IWorkspaceRepository workspaceRepository = workspaceRepository;
    private readonly ILogger<FilesController> logger = logger;
    private readonly ICurrentUserService currentUserService = currentUserService;

    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    private const long MaxImageSize = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Uploads a file to a workspace.
    /// </summary>
    [HttpPost("upload/{workspaceId}")]
    public async Task<IActionResult> UploadFile(int workspaceId, [FromForm] IFormFile file)
    {
        try
        {
            if (!this.currentUserService.TryGetUserId(this.User, out var userId))
            {
                return this.Unauthorized();
            }

            // Verify workspace access
            var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
            if (workspace == null)
            {
                return this.NotFound();
            }

            if (file == null || file.Length == 0)
            {
                return this.BadRequest("No file provided");
            }

            if (file.Length > MaxFileSize)
            {
                return this.BadRequest($"File too large. Maximum size: 50MB");
            }

            var originalFileName = file.FileName;
            var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = $"workspace-uploads/{workspaceId}/{fileName}";

            using var stream = file.OpenReadStream();
            var fileUrl = await this.fileStorageService.UploadFileAsync(filePath, stream, file.ContentType, false);

            // Create file storage record
            var fileRecord = new FileStorage
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Path = filePath,
                FileName = originalFileName,
                ContentType = file.ContentType,
                Size = file.Length,
                FileType = "document",
                Category = "workspace-upload",
                PublicUrl = fileUrl,
                IsPublic = false,
                CreatedByUserId = userId
            };

            await this.fileRepository.CreateAsync(fileRecord);

            this.logger.LogInformation($"File uploaded by user {userId} to workspace {workspaceId}: {originalFileName}");

            return this.Ok(new { id = fileRecord.Id, url = fileUrl, fileName = originalFileName });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error uploading file to workspace {workspaceId}");
            return this.StatusCode(500, "Error uploading file");
        }
    }

    /// <summary>
    /// Uploads an image to a workspace.
    /// </summary>
    [HttpPost("upload-image/{workspaceId}")]
    public async Task<IActionResult> UploadImage(int workspaceId, [FromForm] IFormFile image, [FromForm] string? category = "document")
    {
        try
        {
            if (!this.currentUserService.TryGetUserId(this.User, out var userId))
            {
                return this.Unauthorized();
            }

            // Verify workspace access
            var workspace = await this.workspaceRepository.FindByIdAsync(workspaceId);
            if (workspace == null)
            {
                return this.NotFound();
            }

            if (image == null || image.Length == 0)
            {
                return this.BadRequest("No image provided");
            }

            if (image.Length > MaxImageSize)
            {
                return this.BadRequest($"Image too large. Maximum size: 10MB");
            }

            // Validate image type
            using var checkStream = image.OpenReadStream();
            if (!this.imageStorageService.IsValidImage(checkStream))
            {
                return this.BadRequest("Invalid image file");
            }

            var allowedExtensions = this.imageStorageService.GetAllowedImageExtensions();
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return this.BadRequest($"Image type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
            }

            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = $"workspace-images/{workspaceId}/{category ?? "document"}/{fileName}";

            using var imageStream = image.OpenReadStream();
            var imageUrl = await this.fileStorageService.UploadImageAsync(filePath, imageStream, 1200, 900, 80);

            // Create file storage record
            var imageRecord = new FileStorage
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Path = filePath,
                FileName = image.FileName,
                ContentType = "image/jpeg",
                Size = image.Length,
                FileType = "image",
                Category = category ?? "document-image",
                PublicUrl = imageUrl,
                IsPublic = false,
                CreatedByUserId = userId
            };

            await this.fileRepository.CreateAsync(imageRecord);

            this.logger.LogInformation($"Image uploaded by user {userId} to workspace {workspaceId}");

            return this.Ok(new { id = imageRecord.Id, url = imageUrl, fileName = image.FileName });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error uploading image to workspace {workspaceId}");
            return this.StatusCode(500, "Error uploading image");
        }
    }

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        try
        {
            if (!this.currentUserService.TryGetUserId(this.User, out var userId))
            {
                return this.Unauthorized();
            }

            var file = await this.fileRepository.FindByIdAsync(fileId);
            if (file == null)
            {
                return this.NotFound();
            }

            // Delete from storage
            await this.fileStorageService.DeleteFileAsync(file.Path);

            // Archive in database
            await this.fileRepository.ArchiveAsync(fileId, userId);

            this.logger.LogInformation($"File {fileId} deleted by user {userId}");

            return this.Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error deleting file {fileId}");
            return this.StatusCode(500, "Error deleting file");
        }
    }

    /// <summary>
    /// Downloads a file from storage.
    /// </summary>
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        try
        {
            var file = await this.fileRepository.FindByIdAsync(fileId);
            if (file == null)
            {
                return this.NotFound();
            }

            var stream = await this.fileStorageService.DownloadFileAsync(file.Path);
            return this.File(stream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error downloading file {fileId}");
            return this.StatusCode(500, "Error downloading file");
        }
    }

    /// <summary>
    /// Lists files in a workspace.
    /// </summary>
    [HttpGet("list/{workspaceId}")]
    public async Task<IActionResult> ListFiles(int workspaceId, [FromQuery] int take = 50, [FromQuery] int skip = 0, [FromQuery] string? category = null)
    {
        try
        {
            if (!this.currentUserService.TryGetUserId(this.User, out var userId))
            {
                return this.Unauthorized();
            }

            var files = await this.fileRepository.ListAsync(workspaceId, take, skip, category);
            var total = await this.fileRepository.GetWorkspaceFileCountAsync(workspaceId);

            return this.Ok(new
            {
                files = files.Select(f => new
                {
                    f.Id,
                    f.FileName,
                    f.PublicUrl,
                    f.Size,
                    f.ContentType,
                    f.Category,
                    f.FileType,
                    f.CreatedAt
                }),
                total,
                take,
                skip
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error listing files for workspace {workspaceId}");
            return this.StatusCode(500, "Error listing files");
        }
    }

    /// <summary>
    /// Gets storage usage for a workspace.
    /// </summary>
    [HttpGet("storage-info/{workspaceId}")]
    public async Task<IActionResult> GetStorageInfo(int workspaceId)
    {
        try
        {
            if (!this.currentUserService.TryGetUserId(this.User, out var userId))
            {
                return this.Unauthorized();
            }

            var usedBytes = await this.fileRepository.GetWorkspaceStorageUsedAsync(workspaceId);
            var fileCount = await this.fileRepository.GetWorkspaceFileCountAsync(workspaceId);

            return this.Ok(new
            {
                usedBytes,
                usedMB = Math.Round((decimal)usedBytes / (1024 * 1024), 2),
                fileCount,
                maxBytes = MaxFileSize,
                maxMB = MaxFileSize / (1024 * 1024)
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error getting storage info for workspace {workspaceId}");
            return this.StatusCode(500, "Error getting storage info");
        }
    }
}


