# Service Methods Quick Reference

## ICurrentUserService

### Extracts user ID from HTTP claims

```csharp
// Safe extraction - returns false if not found
bool TryGetUserId(ClaimsPrincipal principal, out int userId)

// Returns nullable user ID
int? GetUserId(ClaimsPrincipal principal)

// Throws if not found
int GetUserIdOrThrow(ClaimsPrincipal principal)
```

**Usage:**
```csharp
if (!_currentUserService.TryGetUserId(User, out var userId))
    return Forbid();
```

---

## IUserManagementService

### User creation, validation, and updates

```csharp
// Create new user with duplicate checking and password hashing
Task<User> CreateUserAsync(string name, string email, string? recoveryEmail, 
                           string password, bool systemAdmin = false)

// Update existing user information
Task<User> UpdateUserAsync(int userId, string name, string email, string? recoveryEmail)

// Check if email is already in use
Task<bool> IsEmailInUseAsync(string email, int? excludeUserId = null)

// Get user by ID
Task<User?> GetUserAsync(int userId)

// Validate recovery email differs from login email
string? ValidateRecoveryEmailDifference(string email, string recoveryEmail)
```

**Usage:**
```csharp
try
{
    var user = await _userService.CreateUserAsync(name, email, recoveryEmail, password);
}
catch (InvalidOperationException ex)
{
    ModelState.AddModelError(nameof(Input.Email), ex.Message);
}
```

---

## IWorkspaceAccessService

### Permission and access verification

```csharp
// Verify user has accepted membership in workspace
Task<bool> UserHasAccessAsync(int userId, int workspaceId)

// Check if user is workspace admin
Task<bool> UserIsWorkspaceAdminAsync(int userId, int workspaceId)

// Get all permissions for user in workspace
Task<Dictionary<string, EffectiveSectionPermission>> GetUserPermissionsAsync(
    int workspaceId, int userId)

// Check if user can perform specific action
// action: "view", "create", "edit"
Task<bool> CanUserPerformActionAsync(int workspaceId, int userId, 
                                      string resourceType, string action)

// Get ticket visibility scope for user
Task<string> GetTicketViewScopeAsync(int workspaceId, int userId, bool isAdmin)

// Ensure user is admin (throws UnauthorizedAccessException)
Task EnsureAdminAccessAsync(int userId, int workspaceId)

// Ensure user has access (throws UnauthorizedAccessException)
Task EnsureWorkspaceAccessAsync(int userId, int workspaceId)
```

**Usage:**
```csharp
var canEdit = await _accessService.CanUserPerformActionAsync(
    workspaceId, userId, "contacts", "edit");
if (!canEdit) return Forbid();
```

---

## IRoleManagementService

### Role assignment and management

```csharp
// Assign role to user in workspace
// Validates role belongs to workspace
Task<UserWorkspaceRole> AssignRoleToUserAsync(int userId, int workspaceId, 
                                              int roleId, int assignedByUserId)

// Remove role from user
// Returns true if removed, false if not found
Task<bool> RemoveRoleFromUserAsync(int userId, int workspaceId, int roleId)

// Count how many users have specific role
Task<int> CountRoleAssignmentsAsync(int workspaceId, int roleId)

// Verify role belongs to workspace
Task<bool> RoleBelongsToWorkspaceAsync(int roleId, int workspaceId)

// Get all roles in workspace
Task<List<Role>> GetWorkspaceRolesAsync(int workspaceId)

// Get user's roles in workspace
Task<List<Role>> GetUserRolesAsync(int userId, int workspaceId)

// Ensure role can be deleted (no assignments)
// Throws InvalidOperationException with user-friendly message
Task EnsureRoleCanBeDeletedAsync(int workspaceId, int roleId, string roleName)
```

**Usage:**
```csharp
try
{
    await _roleService.EnsureRoleCanBeDeletedAsync(wsId, roleId, role.Name);
    await _roleRepo.DeleteAsync(roleId);
}
catch (InvalidOperationException ex)
{
    TempData["Error"] = ex.Message; // "Cannot delete role while X users assigned"
}
```

---

## IWorkspaceService

### Workspace lookup and membership

```csharp
// Get all workspace memberships for user
Task<List<UserWorkspace>> GetUserWorkspacesAsync(int userId)

// Get only accepted workspace memberships
Task<List<UserWorkspace>> GetAcceptedWorkspacesAsync(int userId)

// Get workspace by slug (e.g., "acme-corp")
Task<Workspace?> GetWorkspaceBySlugAsync(string slug)

// Get workspace by ID
Task<Workspace?> GetWorkspaceAsync(int workspaceId)

// Check if user has accepted membership
Task<bool> UserHasMembershipAsync(int userId, int workspaceId)

// Get membership details
Task<UserWorkspace?> GetMembershipAsync(int userId, int workspaceId)
```

**Usage:**
```csharp
var workspace = await _workspaceService.GetWorkspaceBySlugAsync(slug);
if (workspace == null) return NotFound();
```

---

## INotificationPreferenceService

### Notification preferences and types

```csharp
// Get all available notification type definitions
List<NotificationTypeDefinition> GetNotificationTypeDefinitions()
// Returns: List of {Type, Label} pairs

// Get user's notification preferences
// Initializes defaults if none exist
Task<List<UserNotificationPreference>> GetUserPreferencesAsync(int userId)

// Save notification preferences
// Handles timestamps automatically
Task<List<UserNotificationPreference>> SavePreferencesAsync(
    int userId, List<UserNotificationPreference> preferences)

// Initialize default preferences for new user
// Email/in-app enabled, SMS/push disabled
Task<List<UserNotificationPreference>> InitializeDefaultPreferencesAsync(int userId)
```

**Available Notification Types:**
```
- workspace_invite (Workspace Invitation)
- ticket_assigned (Ticket Assigned to You)
- ticket_comment (Comments on Your Tickets)
- ticket_status_change (Ticket Status Changes)
- report_completed (Report Completed)
- mention (Mentions in Comments)
- ticket_summary (Daily Ticket Summary)
- password_reset (Password Reset Confirmation)
```

**Usage:**
```csharp
var prefs = await _prefService.GetUserPreferencesAsync(userId);
var types = _prefService.GetNotificationTypeDefinitions();

foreach (var type in types)
{
    var userPref = prefs.FirstOrDefault(p => p.NotificationType == type.Type);
    // Display to user...
}
```

---

## Exception Handling

All services throw meaningful exceptions for business logic errors:

```csharp
// UserManagementService
throw new InvalidOperationException(
    $"A user with email '{email}' already exists.");

// WorkspaceAccessService
throw new UnauthorizedAccessException(
    $"User {userId} is not an admin of workspace {workspaceId}.");

// RoleManagementService
throw new InvalidOperationException(
    $"Cannot delete role '{roleName}' while {assignCount} user(s) are assigned. " +
    "Unassign them first.");
```

---

## Common Patterns

### Permission Checking
```csharp
// Single permission check
bool canEdit = await _accessService.CanUserPerformActionAsync(
    workspaceId, userId, "contacts", "edit");

// Get all permissions
var permissions = await _accessService.GetUserPermissionsAsync(workspaceId, userId);
bool canCreate = permissions.TryGetValue("inventory", out var ip) && ip.CanCreate;
```

### Error Handling
```csharp
// For business logic errors
try
{
    await _roleService.EnsureRoleCanBeDeletedAsync(wsId, roleId, name);
}
catch (InvalidOperationException ex)
{
    TempData["Error"] = ex.Message;
    return Redirect(returnUrl);
}
```

### User ID Extraction
```csharp
// Safe extraction
if (!_currentUserService.TryGetUserId(User, out var userId))
    return Forbid();

// Or with null check
var userId = _currentUserService.GetUserId(User);
if (!userId.HasValue)
    return Forbid();

// Or with exception (only when certain user exists)
var userId = _currentUserService.GetUserIdOrThrow(User);
```

---

## Service Dependencies

Each service depends on:

```
CurrentUserService
  - No dependencies

UserManagementService
  - IUserRepository
  - IPasswordHasher

WorkspaceAccessService
  - IUserWorkspaceRepository
  - IUserWorkspaceRoleRepository
  - IRolePermissionRepository

RoleManagementService
  - IUserWorkspaceRoleRepository
  - IRoleRepository

WorkspaceService
  - IWorkspaceRepository
  - IUserWorkspaceRepository

NotificationPreferenceService
  - IUserNotificationPreferenceRepository
```

---

## Injection Example

```csharp
public class MyPageModel : PageModel
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IRoleManagementService _roleService;
    
    public MyPageModel(
        ICurrentUserService currentUserService,
        IWorkspaceAccessService accessService,
        IRoleManagementService roleService)
    {
        _currentUserService = currentUserService;
        _accessService = accessService;
        _roleService = roleService;
    }
    
    public async Task<IActionResult> OnGetAsync(string slug, int roleId)
    {
        // Extract user ID
        if (!_currentUserService.TryGetUserId(User, out var userId))
            return Forbid();
        
        // Get workspace and verify access
        var workspace = await _workspaceService.GetWorkspaceBySlugAsync(slug);
        if (workspace == null) return NotFound();
        
        // Check admin status
        var isAdmin = await _accessService.UserIsWorkspaceAdminAsync(userId, workspace.Id);
        if (!isAdmin) return Forbid();
        
        // Count role assignments
        var count = await _roleService.CountRoleAssignmentsAsync(workspace.Id, roleId);
        
        return Page();
    }
}
```

---

**Last Updated:** January 11, 2026  
**Version:** 1.0 - Final  
**Status:** ✅ Complete and verified
