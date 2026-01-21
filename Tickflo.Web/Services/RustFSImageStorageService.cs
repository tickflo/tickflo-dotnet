namespace Tickflo.Web.Services;

using Tickflo.Core.Services.Storage;

/// <summary>
/// Implementation of IImageStorageService using RustFS storage.
/// </summary>
public class RustFSImageStorageService(IFileStorageService storageService, ILogger<RustFSImageStorageService> logger) : IImageStorageService
{
    private readonly IFileStorageService _storageService = storageService;
    private readonly ILogger<RustFSImageStorageService> _logger = logger;

    private const string AvatarDirectory = "user-data";
    private const string LogoDirectory = "workspace-data";
    private const string BannerDirectory = "workspace-data";
    private const string DocumentDirectory = "workspace-documents";

    public async Task<string> UploadUserAvatarAsync(int userId, Stream imageStream)
    {
        try
        {
            var path = $"{AvatarDirectory}/{userId}/avatar.jpg";
            return await this._storageService.UploadImageAsync(path, imageStream, 256, 256, 85);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error uploading avatar for user {userId}");
            throw;
        }
    }

    public async Task<bool> DeleteUserAvatarAsync(int userId)
    {
        try
        {
            var path = $"{AvatarDirectory}/{userId}/avatar.jpg";
            return await this._storageService.DeleteFileAsync(path);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error deleting avatar for user {userId}");
            return false;
        }
    }

    public string GetUserAvatarUrl(int userId)
    {
        var path = $"{AvatarDirectory}/{userId}/avatar.jpg";
        return this._storageService.GetFileUrl(path);
    }

    public async Task<string> UploadWorkspaceLogoAsync(int workspaceId, Stream imageStream)
    {
        try
        {
            var path = $"{LogoDirectory}/{workspaceId}/logo.jpg";
            return await this._storageService.UploadImageAsync(path, imageStream, 512, 512, 85);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error uploading logo for workspace {workspaceId}");
            throw;
        }
    }

    public async Task<bool> DeleteWorkspaceLogoAsync(int workspaceId)
    {
        try
        {
            var path = $"{LogoDirectory}/{workspaceId}/logo.jpg";
            return await this._storageService.DeleteFileAsync(path);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error deleting logo for workspace {workspaceId}");
            return false;
        }
    }

    public string GetWorkspaceLogoUrl(int workspaceId)
    {
        var path = $"{LogoDirectory}/{workspaceId}/logo.jpg";
        return this._storageService.GetFileUrl(path);
    }

    public async Task<string> UploadWorkspaceBannerAsync(int workspaceId, Stream imageStream)
    {
        try
        {
            var path = $"{BannerDirectory}/{workspaceId}/banner.jpg";
            return await this._storageService.UploadImageAsync(path, imageStream, 1920, 1080, 80);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error uploading banner for workspace {workspaceId}");
            throw;
        }
    }

    public async Task<bool> DeleteWorkspaceBannerAsync(int workspaceId)
    {
        try
        {
            var path = $"{BannerDirectory}/{workspaceId}/banner.jpg";
            return await this._storageService.DeleteFileAsync(path);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error deleting banner for workspace {workspaceId}");
            return false;
        }
    }

    public string GetWorkspaceBannerUrl(int workspaceId)
    {
        var path = $"{BannerDirectory}/{workspaceId}/banner.jpg";
        return this._storageService.GetFileUrl(path);
    }

    public async Task<string> UploadDocumentImageAsync(int workspaceId, string documentPath, Stream imageStream)
    {
        try
        {
            var path = $"{DocumentDirectory}/{workspaceId}/{documentPath}.jpg";
            return await this._storageService.UploadImageAsync(path, imageStream, 1200, 900, 80);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error uploading document image for workspace {workspaceId} at {documentPath}");
            throw;
        }
    }

    public async Task<bool> DeleteDocumentImageAsync(int workspaceId, string documentPath)
    {
        try
        {
            var path = $"{DocumentDirectory}/{workspaceId}/{documentPath}.jpg";
            return await this._storageService.DeleteFileAsync(path);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, $"Error deleting document image for workspace {workspaceId} at {documentPath}");
            return false;
        }
    }

    public string GetDocumentImageUrl(int workspaceId, string documentPath)
    {
        var path = $"{DocumentDirectory}/{workspaceId}/{documentPath}.jpg";
        return this._storageService.GetFileUrl(path);
    }

    public bool IsValidImage(Stream imageStream)
    {
        try
        {
            imageStream.Position = 0;
            // Read the first few bytes to check for image signatures
            var buffer = new byte[8];
            imageStream.ReadExactly(buffer, 0, 8);
            imageStream.Position = 0;

            // Check for common image signatures
            // JPEG: FF D8 FF
            if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
            {
                return true;
            }
            // PNG: 89 50 4E 47
            if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
            {
                return true;
            }
            // GIF: 47 49 46
            if (buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
            {
                return true;
            }
            // WebP: RIFF ... WEBP
            if (buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
            {
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public string[] GetAllowedImageExtensions() => [".jpg", ".jpeg", ".png", ".gif", ".webp"];
}

