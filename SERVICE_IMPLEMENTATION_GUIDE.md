# Quick Implementation Guide for Remaining Services

This guide provides templates and patterns for implementing the remaining 6 service classes.

---

## Implementation Checklist

For each service implementation:

- [ ] Create `ServiceName.cs` in `Tickflo.Core/Services/`
- [ ] Implement interface methods
- [ ] Follow existing patterns (see `UserManagementService`, `DashboardService`)
- [ ] Use constructor injection for repositories
- [ ] Validate inputs and trim strings
- [ ] Throw `InvalidOperationException` for business rule violations
- [ ] Add XML documentation comments
- [ ] Uncomment service registration in `Program.cs`
- [ ] Write unit tests in `Tickflo.CoreTest/Services/`
- [ ] Refactor corresponding PageModel to use service

---

## Template: Basic CRUD Service

```csharp
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepo;
    
    public ContactService(IContactRepository contactRepo)
    {
        _contactRepo = contactRepo;
    }
    
    public async Task<Contact> CreateContactAsync(int workspaceId, CreateContactRequest request)
    {
        // 1. Validate
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");
            
        var name = request.Name.Trim();
        
        // 2. Check uniqueness
        if (!await IsNameUniqueAsync(workspaceId, name))
            throw new InvalidOperationException($"Contact '{name}' already exists");
        
        // 3. Create entity
        var contact = new Contact
        {
            WorkspaceId = workspaceId,
            Name = name,
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Company = request.Company?.Trim(),
            Notes = request.Notes?.Trim()
        };
        
        // 4. Persist
        await _contactRepo.CreateAsync(contact);
        
        return contact;
    }
    
    public async Task<Contact> UpdateContactAsync(int workspaceId, int contactId, UpdateContactRequest request)
    {
        var contact = await _contactRepo.FindAsync(workspaceId, contactId);
        if (contact == null)
            throw new InvalidOperationException("Contact not found");
            
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Contact name is required");
            
        var name = request.Name.Trim();
        
        if (name != contact.Name && !await IsNameUniqueAsync(workspaceId, name, contactId))
            throw new InvalidOperationException($"Contact '{name}' already exists");
        
        contact.Name = name;
        contact.Email = request.Email?.Trim();
        contact.Phone = request.Phone?.Trim();
        contact.Company = request.Company?.Trim();
        contact.Notes = request.Notes?.Trim();
        contact.UpdatedAt = DateTime.UtcNow;
        
        await _contactRepo.UpdateAsync(contact);
        
        return contact;
    }
    
    public async Task DeleteContactAsync(int workspaceId, int contactId)
    {
        await _contactRepo.DeleteAsync(workspaceId, contactId);
    }
    
    public async Task<bool> IsNameUniqueAsync(int workspaceId, string name, int? excludeContactId = null)
    {
        var existing = await _contactRepo.FindByNameAsync(workspaceId, name);
        return existing == null || (excludeContactId.HasValue && existing.Id == excludeContactId.Value);
    }
}
```

---

## Template: Service with Complex Logic (WorkspaceSettingsService)

```csharp
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Core.Services;

public class WorkspaceSettingsService : IWorkspaceSettingsService
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly ITicketStatusRepository _statusRepo;
    private readonly ITicketPriorityRepository _priorityRepo;
    private readonly ITicketTypeRepository _typeRepo;
    
    public WorkspaceSettingsService(
        IWorkspaceRepository workspaceRepo,
        ITicketStatusRepository statusRepo,
        ITicketPriorityRepository priorityRepo,
        ITicketTypeRepository typeRepo)
    {
        _workspaceRepo = workspaceRepo;
        _statusRepo = statusRepo;
        _priorityRepo = priorityRepo;
        _typeRepo = typeRepo;
    }
    
    public async Task<Workspace> UpdateWorkspaceBasicSettingsAsync(int workspaceId, string name, string slug)
    {
        var workspace = await _workspaceRepo.FindByIdAsync(workspaceId);
        if (workspace == null)
            throw new InvalidOperationException("Workspace not found");
            
        workspace.Name = name.Trim();
        
        var newSlug = slug.Trim();
        if (newSlug != workspace.Slug)
        {
            var existing = await _workspaceRepo.FindBySlugAsync(newSlug);
            if (existing != null)
                throw new InvalidOperationException("Slug is already in use");
            workspace.Slug = newSlug;
        }
        
        await _workspaceRepo.UpdateAsync(workspace);
        return workspace;
    }
    
    public async Task EnsureDefaultsExistAsync(int workspaceId)
    {
        // Bootstrap statuses
        var statuses = await _statusRepo.ListAsync(workspaceId);
        if (statuses.Count == 0)
        {
            var defaults = new[]
            {
                new TicketStatus { WorkspaceId = workspaceId, Name = "New", Color = "info", SortOrder = 1, IsClosedState = false },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Completed", Color = "success", SortOrder = 2, IsClosedState = true },
                new TicketStatus { WorkspaceId = workspaceId, Name = "Closed", Color = "error", SortOrder = 3, IsClosedState = true }
            };
            
            foreach (var status in defaults)
                await _statusRepo.CreateAsync(status);
        }
        
        // Bootstrap priorities
        var priorities = await _priorityRepo.ListAsync(workspaceId);
        if (priorities.Count == 0)
        {
            var defaults = new[]
            {
                new TicketPriority { WorkspaceId = workspaceId, Name = "Low", Color = "warning", SortOrder = 1 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "Normal", Color = "neutral", SortOrder = 2 },
                new TicketPriority { WorkspaceId = workspaceId, Name = "High", Color = "error", SortOrder = 3 }
            };
            
            foreach (var priority in defaults)
                await _priorityRepo.CreateAsync(priority);
        }
        
        // Bootstrap types
        var types = await _typeRepo.ListAsync(workspaceId);
        if (types.Count == 0)
        {
            var defaults = new[]
            {
                new TicketType { WorkspaceId = workspaceId, Name = "Standard", Color = "neutral", SortOrder = 1 },
                new TicketType { WorkspaceId = workspaceId, Name = "Bug", Color = "error", SortOrder = 2 },
                new TicketType { WorkspaceId = workspaceId, Name = "Feature", Color = "primary", SortOrder = 3 }
            };
            
            foreach (var type in defaults)
                await _typeRepo.CreateAsync(type);
        }
    }
    
    public async Task<TicketStatus> AddStatusAsync(int workspaceId, string name, string color, bool isClosedState = false)
    {
        name = name.Trim();
        color = string.IsNullOrWhiteSpace(color) ? "neutral" : color.Trim();
        
        var existing = await _statusRepo.FindByNameAsync(workspaceId, name);
        if (existing != null)
            throw new InvalidOperationException($"Status '{name}' already exists");
        
        var statuses = await _statusRepo.ListAsync(workspaceId);
        var maxOrder = statuses.Any() ? statuses.Max(s => s.SortOrder) : 0;
        
        var status = new TicketStatus
        {
            WorkspaceId = workspaceId,
            Name = name,
            Color = color,
            SortOrder = maxOrder + 1,
            IsClosedState = isClosedState
        };
        
        await _statusRepo.CreateAsync(status);
        return status;
    }
    
    // Implement remaining methods following same patterns...
}
```

---

## Template: Service with Member Sync (TeamManagementService)

```csharp
public async Task SyncTeamMembersAsync(int teamId, int workspaceId, List<int> memberUserIds)
{
    var team = await _teamRepo.FindByIdAsync(teamId);
    if (team == null)
        throw new InvalidOperationException("Team not found");
        
    // Validate all users are workspace members
    if (!await ValidateMembersAsync(workspaceId, memberUserIds))
        throw new InvalidOperationException("One or more users are not workspace members");
    
    // Get current members
    var currentMembers = await _teamMemberRepo.ListMembersAsync(teamId);
    var currentUserIds = currentMembers.Select(m => m.UserId).ToHashSet();
    
    var newUserIds = memberUserIds.ToHashSet();
    
    // Remove members not in new list
    var toRemove = currentUserIds.Except(newUserIds);
    foreach (var userId in toRemove)
    {
        var member = currentMembers.First(m => m.UserId == userId);
        await _teamMemberRepo.DeleteAsync(member.Id);
    }
    
    // Add new members
    var toAdd = newUserIds.Except(currentUserIds);
    foreach (var userId in toAdd)
    {
        await _teamMemberRepo.CreateAsync(new TeamMember
        {
            TeamId = teamId,
            UserId = userId
        });
    }
}
```

---

## PageModel Refactoring Pattern

### Before:
```csharp
public async Task<IActionResult> OnPostAsync(string slug, int id)
{
    var workspace = await _workspaceRepo.FindBySlugAsync(slug);
    if (workspace == null) return NotFound();
    
    var contact = await _contactRepo.FindAsync(workspace.Id, id);
    if (contact == null) return NotFound();
    
    var name = Input.Name?.Trim();
    if (string.IsNullOrEmpty(name))
    {
        ModelState.AddModelError(nameof(Input.Name), "Name is required");
        return Page();
    }
    
    var existing = await _contactRepo.FindByNameAsync(workspace.Id, name);
    if (existing != null && existing.Id != id)
    {
        ModelState.AddModelError(nameof(Input.Name), "Name already exists");
        return Page();
    }
    
    contact.Name = name;
    contact.Email = Input.Email?.Trim();
    contact.Phone = Input.Phone?.Trim();
    contact.UpdatedAt = DateTime.UtcNow;
    
    await _contactRepo.UpdateAsync(contact);
    
    return RedirectToPage("/Workspaces/Contacts", new { slug });
}
```

### After:
```csharp
public async Task<IActionResult> OnPostAsync(string slug, int id)
{
    var workspace = await _workspaceService.GetWorkspaceBySlugAsync(slug);
    if (workspace == null) return NotFound();
    
    if (!ModelState.IsValid)
        return Page();
    
    try
    {
        var request = new UpdateContactRequest
        {
            Name = Input.Name,
            Email = Input.Email,
            Phone = Input.Phone,
            Company = Input.Company,
            Notes = Input.Notes
        };
        
        await _contactService.UpdateContactAsync(workspace.Id, id, request);
        
        return RedirectToPage("/Workspaces/Contacts", new { slug });
    }
    catch (InvalidOperationException ex)
    {
        ModelState.AddModelError(string.Empty, ex.Message);
        return Page();
    }
}
```

---

## Service Registration Template

In `Program.cs`, after implementing each service:

```csharp
// Uncomment the corresponding line:
builder.Services.AddScoped<IWorkspaceSettingsService, WorkspaceSettingsService>();
builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ITeamManagementService, TeamManagementService>();
```

---

## Testing Template

```csharp
using Xunit;
using Moq;
using Tickflo.Core.Services;
using Tickflo.Core.Data;

namespace Tickflo.CoreTest.Services;

public class ContactServiceTests
{
    [Fact]
    public async Task CreateContactAsync_ValidRequest_CreatesContact()
    {
        // Arrange
        var mockRepo = new Mock<IContactRepository>();
        mockRepo.Setup(r => r.FindByNameAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((Contact?)null);
        mockRepo.Setup(r => r.CreateAsync(It.IsAny<Contact>()))
                .Returns(Task.CompletedTask);
        
        var service = new ContactService(mockRepo.Object);
        
        var request = new CreateContactRequest
        {
            Name = "Test Contact",
            Email = "test@example.com"
        };
        
        // Act
        var result = await service.CreateContactAsync(1, request);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Contact", result.Name);
        Assert.Equal("test@example.com", result.Email);
        mockRepo.Verify(r => r.CreateAsync(It.IsAny<Contact>()), Times.Once);
    }
    
    [Fact]
    public async Task CreateContactAsync_DuplicateName_ThrowsException()
    {
        // Arrange
        var mockRepo = new Mock<IContactRepository>();
        mockRepo.Setup(r => r.FindByNameAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new Contact { Id = 1, Name = "Test Contact" });
        
        var service = new ContactService(mockRepo.Object);
        
        var request = new CreateContactRequest { Name = "Test Contact" };
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateContactAsync(1, request));
    }
}
```

---

## Priority Order for Implementation

1. **ContactService** (simplest, good warm-up)
2. **LocationService** (similar to Contact)
3. **InventoryService** (similar to Contact)
4. **TeamManagementService** (member sync logic)
5. **WorkspaceSettingsService** (multiple CRUD operations)
6. **UserInvitationService** (most complex, involves email)

---

## Common Patterns to Follow

### Input Validation
```csharp
if (string.IsNullOrWhiteSpace(request.Name))
    throw new InvalidOperationException("Name is required");
    
var name = request.Name.Trim();
```

### Uniqueness Check
```csharp
var existing = await _repo.FindByNameAsync(workspaceId, name);
if (existing != null && (!excludeId.HasValue || existing.Id != excludeId.Value))
    throw new InvalidOperationException($"'{name}' already exists");
```

### Update Pattern
```csharp
var entity = await _repo.FindAsync(workspaceId, entityId);
if (entity == null)
    throw new InvalidOperationException("Entity not found");
    
// Update fields...
entity.UpdatedAt = DateTime.UtcNow;
await _repo.UpdateAsync(entity);
```

---

**Remember**: The goal is thin PageModels that delegate all business logic to services!
