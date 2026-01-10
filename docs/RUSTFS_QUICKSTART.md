# RustFS Implementation Quick Start Guide

## What Was Implemented

A complete file and image storage system for Tickflo using RustFS (S3-compatible storage). The implementation includes:

### Core Services

1. **[IFileStorageService](../Tickflo.Core/Services/IFileStorageService.cs)** - Base interface for file operations
   - Upload, download, delete files
   - Generate thumbnails
   - List files with pagination
   - Get file metadata and URLs

2. **[RustFSStorageService](../Tickflo.Web/Services/RustFSStorageService.cs)** - Implementation using AWS S3 SDK
   - Automatic image compression with configurable quality
   - Image resizing with aspect ratio preservation
   - Full S3-compatible API support

3. **[IImageStorageService](../Tickflo.Core/Services/IImageStorageService.cs)** - Specialized image operations
   - User avatar management (256x256)
   - Workspace logo upload (512x512)
   - Workspace banner upload (1920x1080)
   - Document image storage (1200x900)
   - Image validation with magic byte detection

4. **[RustFSImageStorageService](../Tickflo.Web/Services/RustFSImageStorageService.cs)** - Image service implementation

### Database Layer

1. **[FileStorage Entity](../Tickflo.Core/Entities/FileStorage.cs)** - Database model for file tracking
   - Workspace and user association
   - File metadata storage (size, content type, category)
   - Soft delete support (archive flag)
   - Related entity tracking (tickets, contacts, etc.)

2. **[IFileStorageRepository](../Tickflo.Core/Data/IFileStorageRepository.cs)** - Data access interface
3. **[FileStorageRepository](../Tickflo.Core/Data/FileStorageRepository.cs)** - Repository implementation

### API & UI

1. **[FilesController](../Tickflo.Web/Controllers/FilesController.cs)** - REST API endpoints
   - `POST /api/files/upload/{workspaceId}` - Generic file upload
   - `POST /api/files/upload-image/{workspaceId}` - Image upload with compression
   - `DELETE /api/files/{fileId}` - Delete file
   - `GET /api/files/download/{fileId}` - Download file
   - `GET /api/files/list/{workspaceId}` - List workspace files
   - `GET /api/files/storage-info/{workspaceId}` - Get storage statistics

2. **[Files Razor Page](../Tickflo.Web/Pages/Workspaces/Files.cshtml)** - File manager UI
   - Modern file manager interface using DaisyUI
   - Drag-and-drop upload capability
   - File preview and download
   - Storage usage tracking with visual progress bar
   - Pagination and filtering

### Configuration

- **Docker Compose**: RustFS service configured in [compose.yml](../Tickflo.Web/compose.yml)
- **Environment Variables**: Configure in `.env` file (see [.env.example](../Tickflo.Web/.env.example))
  - `S3_ENDPOINT` - RustFS endpoint URL
  - `S3_ACCESS_KEY` - RustFS access key
  - `S3_SECRET_KEY` - RustFS secret key
  - `S3_BUCKET` - Bucket name for Tickflo files
  - `S3_REGION` - AWS region

### Documentation

- **[RUSTFS_INTEGRATION.md](./RUSTFS_INTEGRATION.md)** - Complete integration guide with examples and troubleshooting

## Getting Started

### 1. Start RustFS

```bash
cd Tickflo.Web
docker-compose up -d s3
```

Verify RustFS is running:
```bash
curl http://localhost:9000/minio/health/live
# Should return "OK"
```

RustFS UI available at: http://localhost:9001

### 2. Configure Environment

Copy and update `.env` file:
```bash
cp Tickflo.Web/.env.example Tickflo.Web/.env
```

Default values:
```
S3_ENDPOINT=http://localhost:9000
S3_ACCESS_KEY=admin
S3_SECRET_KEY=password
S3_BUCKET=tickflo
S3_REGION=us-east-1
```

### 3. Create Database Migration

Run EF Core migration to create the `file_storages` table:

```bash
# From the Tickflo.Web directory
dotnet ef migrations add AddFileStorage --project ../Tickflo.Core
dotnet ef database update
```

Or manually create the table using the SQL in [RUSTFS_INTEGRATION.md](./RUSTFS_INTEGRATION.md#database-migration).

### 4. Build and Run

```bash
dotnet build
dotnet run --project Tickflo.Web
```

### 5. Access File Manager

Navigate to: `http://localhost:3000/workspace/{slug}/files`

## Usage Examples

### Inject Services

```csharp
public class MyController : Controller
{
    private readonly IFileStorageService _fileStorage;
    private readonly IImageStorageService _imageStorage;
    private readonly IFileStorageRepository _fileRepo;

    public MyController(
        IFileStorageService fileStorage,
        IImageStorageService imageStorage,
        IFileStorageRepository fileRepo)
    {
        _fileStorage = fileStorage;
        _imageStorage = imageStorage;
        _fileRepo = fileRepo;
    }

    // Use in your methods...
}
```

### Upload User Avatar

```csharp
using var stream = file.OpenReadStream();
var url = await _imageStorage.UploadUserAvatarAsync(userId, stream);
// Returns: http://localhost:9000/tickflo/user-data/123/avatar.jpg
```

### Upload Document Image

```csharp
using var stream = file.OpenReadStream();
var url = await _imageStorage.UploadDocumentImageAsync(
    workspaceId,
    $"ticket-{ticketId}/screenshot",
    stream
);
// Returns: http://localhost:9000/tickflo/workspace-documents/1/ticket-789/screenshot.jpg
```

### Get Storage Usage

```csharp
var used = await _fileRepo.GetWorkspaceStorageUsedAsync(workspaceId);
var count = await _fileRepo.GetWorkspaceFileCountAsync(workspaceId);

Console.WriteLine($"Used: {used} bytes in {count} files");
```

### Track File in Database

```csharp
var fileRecord = new FileStorage
{
    WorkspaceId = workspaceId,
    UserId = userId,
    Path = "workspace-uploads/1/document.pdf",
    FileName = "document.pdf",
    ContentType = "application/pdf",
    Size = fileStream.Length,
    FileType = "document",
    Category = "workspace-upload",
    PublicUrl = "http://localhost:9000/tickflo/workspace-uploads/1/document.pdf",
    CreatedByUserId = userId,
    RelatedEntityType = "Ticket",
    RelatedEntityId = ticketId
};

await _fileRepo.CreateAsync(fileRecord);
```

## Directory Structure in RustFS

```
tickflo/
├── user-data/
│   └── {userId}/avatar.jpg
├── workspace-data/
│   ├── {workspaceId}/logo.jpg
│   └── {workspaceId}/banner.jpg
├── workspace-images/
│   └── {workspaceId}/{category}/{filename}.jpg
├── workspace-documents/
│   └── {workspaceId}/{path}/{filename}.jpg
└── workspace-uploads/
    └── {workspaceId}/{filename}
```

## Image Optimization

All images are automatically optimized:

| Type | Dimensions | Quality | Format |
|------|-----------|---------|--------|
| Avatar | 256x256 | 85% | JPEG |
| Logo | 512x512 | 85% | JPEG |
| Banner | 1920x1080 | 80% | JPEG |
| Document | 1200x900 | 80% | JPEG |

Compression saves ~70-80% of storage space.

## API Integration

### Upload via REST API

```bash
curl -X POST http://localhost:3000/api/files/upload/1 \
  -H "Authorization: Bearer {token}" \
  -F "file=@document.pdf"
```

### Get Storage Info

```bash
curl http://localhost:3000/api/files/storage-info/1 \
  -H "Authorization: Bearer {token}"
```

## Security Features

✅ **Authentication Required** - All endpoints protected with Bearer token  
✅ **File Validation** - Image magic byte validation  
✅ **Size Limits** - Configurable upload size limits  
✅ **Workspace Isolation** - Files scoped to workspace  
✅ **Soft Delete** - Files archived before hard delete  
✅ **Access Control** - User and workspace-based permissions  

## Monitoring

View RustFS logs:
```bash
docker-compose logs -f s3
```

All file operations are logged with:
- Operation type (upload, download, delete)
- User ID and Workspace ID
- File details (path, size, type)
- Timestamps
- Error details

## Next Steps

1. **Integrate with Tickets** - Add file uploads to ticket creation
2. **User Profile Enhancement** - Use avatar service for profile pictures
3. **Report Generation** - Store generated reports using file service
4. **Document Search** - Implement full-text search for document content
5. **Backup Strategy** - Configure RustFS data persistence and backups

## Files Modified/Created

### Core Project
- ✅ `Tickflo.Core/Services/IFileStorageService.cs` - New interface
- ✅ `Tickflo.Core/Services/IImageStorageService.cs` - New interface  
- ✅ `Tickflo.Core/Entities/FileStorage.cs` - New entity
- ✅ `Tickflo.Core/Data/IFileStorageRepository.cs` - New interface
- ✅ `Tickflo.Core/Data/FileStorageRepository.cs` - New implementation
- ✅ `Tickflo.Core/Data/TickfloDbContext.cs` - Updated with DbSet

### Web Project
- ✅ `Tickflo.Web/Services/RustFSStorageService.cs` - New implementation
- ✅ `Tickflo.Web/Services/RustFSImageStorageService.cs` - New implementation
- ✅ `Tickflo.Web/Controllers/FilesController.cs` - New API endpoints
- ✅ `Tickflo.Web/Pages/Workspaces/Files.cshtml` - New file manager UI
- ✅ `Tickflo.Web/Pages/Workspaces/Files.cshtml.cs` - New page model
- ✅ `Tickflo.Web/Program.cs` - Updated service registration
- ✅ `Tickflo.Web/.env.example` - Updated with S3 config

### Documentation
- ✅ `docs/RUSTFS_INTEGRATION.md` - Complete integration guide

## Troubleshooting

### RustFS Connection Failed
```
error: Connection refused
```
**Solution**: Ensure RustFS is running: `docker-compose ps`

### Image Upload Fails
```
error: Unable to load image
```
**Solution**: Verify image file is valid and not corrupted

### Storage Quota Exceeded
**Solution**: Clean up old files or increase storage limit in .env

### Database Migration Failed
**Solution**: Manually create the `file_storages` table using SQL from documentation

## Support

For detailed documentation, see [RUSTFS_INTEGRATION.md](./RUSTFS_INTEGRATION.md)

For issues:
1. Check RustFS logs: `docker-compose logs s3`
2. Check application logs for detailed error messages
3. Verify S3 credentials in .env file
4. Confirm RustFS is accessible: `curl http://localhost:9000/minio/health/live`
