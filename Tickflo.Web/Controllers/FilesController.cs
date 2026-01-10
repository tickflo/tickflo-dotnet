using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Config;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services;

namespace Tickflo.Web.Controllers;

/// <summary>
/// REST API controller for file and image management using RustFS.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IImageStorageService _imageStorageService;
    private readonly IFileStorageRepository _fileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly TickfloConfig _config;
    private readonly ILogger<FilesController> _logger;

    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    private const long MaxImageSize = 10 * 1024 * 1024; // 10 MB

    public FilesController(
        IFileStorageService fileStorageService,
        IImageStorageService imageStorageService,
        IFileStorageRepository fileRepository,
        IWorkspaceRepository workspaceRepository,
        TickfloConfig config,
        ILogger<FilesController> logger)
    {
        _fileStorageService = fileStorageService;
        _imageStorageService = imageStorageService;
        _fileRepository = fileRepository;
        _workspaceRepository = workspaceRepository;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Uploads a file to a workspace.
    /// </summary>
    [HttpPost("upload/{workspaceId}")]
    public async Task<IActionResult> UploadFile(int workspaceId, [FromForm] IFormFile file)
    {
        try
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            // Verify workspace access
            var workspace = await _workspaceRepository.FindByIdAsync(workspaceId);
            if (workspace == null) return NotFound();

            if (file == null || file.Length == 0) return BadRequest("No file provided");
            if (file.Length > MaxFileSize) return BadRequest($"File too large. Maximum size: 50MB");

            var originalFileName = file.FileName;
            var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = $"workspace-uploads/{workspaceId}/{fileName}";

            using var stream = file.OpenReadStream();
            var fileUrl = await _fileStorageService.UploadFileAsync(filePath, stream, file.ContentType, false);

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

            await _fileRepository.CreateAsync(fileRecord);

            _logger.LogInformation($"File uploaded by user {userId} to workspace {workspaceId}: {originalFileName}");

            return Ok(new { id = fileRecord.Id, url = fileUrl, fileName = originalFileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file to workspace {workspaceId}");
            return StatusCode(500, "Error uploading file");
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
            if (!TryGetUserId(out var userId)) return Unauthorized();

            // Verify workspace access
            var workspace = await _workspaceRepository.FindByIdAsync(workspaceId);
            if (workspace == null) return NotFound();

            if (image == null || image.Length == 0) return BadRequest("No image provided");
            if (image.Length > MaxImageSize) return BadRequest($"Image too large. Maximum size: 10MB");

            // Validate image type
            using var checkStream = image.OpenReadStream();
            if (!_imageStorageService.IsValidImage(checkStream))
                return BadRequest("Invalid image file");

            var allowedExtensions = _imageStorageService.GetAllowedImageExtensions();
            var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest($"Image type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

            var fileName = $"{Guid.NewGuid()}.jpg";
            var filePath = $"workspace-images/{workspaceId}/{category ?? "document"}/{fileName}";

            using var imageStream = image.OpenReadStream();
            var imageUrl = await _fileStorageService.UploadImageAsync(filePath, imageStream, 1200, 900, 80);

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

            await _fileRepository.CreateAsync(imageRecord);

            _logger.LogInformation($"Image uploaded by user {userId} to workspace {workspaceId}");

            return Ok(new { id = imageRecord.Id, url = imageUrl, fileName = image.FileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading image to workspace {workspaceId}");
            return StatusCode(500, "Error uploading image");
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
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var file = await _fileRepository.FindByIdAsync(fileId);
            if (file == null) return NotFound();

            // Delete from storage
            await _fileStorageService.DeleteFileAsync(file.Path);

            // Archive in database
            await _fileRepository.ArchiveAsync(fileId, userId);

            _logger.LogInformation($"File {fileId} deleted by user {userId}");

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {fileId}");
            return StatusCode(500, "Error deleting file");
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
            var file = await _fileRepository.FindByIdAsync(fileId);
            if (file == null) return NotFound();

            var stream = await _fileStorageService.DownloadFileAsync(file.Path);
            return File(stream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading file {fileId}");
            return StatusCode(500, "Error downloading file");
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
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var files = await _fileRepository.ListAsync(workspaceId, take, skip, category);
            var total = await _fileRepository.GetWorkspaceFileCountAsync(workspaceId);

            return Ok(new
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
            _logger.LogError(ex, $"Error listing files for workspace {workspaceId}");
            return StatusCode(500, "Error listing files");
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
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var usedBytes = await _fileRepository.GetWorkspaceStorageUsedAsync(workspaceId);
            var fileCount = await _fileRepository.GetWorkspaceFileCountAsync(workspaceId);

            return Ok(new
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
            _logger.LogError(ex, $"Error getting storage info for workspace {workspaceId}");
            return StatusCode(500, "Error getting storage info");
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(id, out userId))
        {
            return true;
        }

        userId = default;
        return false;
    }
}
