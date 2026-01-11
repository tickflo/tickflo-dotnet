# Business Logic Refactoring Analysis

## Executive Summary
This document outlines the identified business logic scattered across Razor Pages (.cshtml.cs) in the Tickflo.Web project and provides a comprehensive refactoring plan to move this logic into the Tickflo.Core class library.

## Key Findings

### 1. Identified Business Logic Categories

#### A. User & Permission Management
**Files Affected:**
- `Pages/Users/Profile.cshtml.cs` - Notification preference management
- `Pages/Users/Create.cshtml.cs` - User creation with validation
- `Pages/Users/Edit.cshtml.cs` - User editing
- `Pages/Workspaces/RolesAssign.cshtml.cs` - Role assignment logic
- `Pages/Workspaces/Roles.cshtml.cs` - Role listing and admin checks

**Business Logic Found:**
- Notification type definitions and preference initialization
- User creation validation (duplicate email checks, password hashing)
- User SystemAdmin authorization checks
- Role assignment operations (add/remove assignments)
- Admin access validation with detailed error handling
- Permission checking for workspace operations

#### B. Workspace & Access Control
**Files Affected:**
- `Pages/Workspace.cshtml.cs` - Dashboard data loading
- `Pages/Workspaces/RolesAssign.cshtml.cs` - Workspace admin verification
- `Pages/Workspaces/Roles.cshtml.cs` - Role management
- `Pages/Workspaces/Inventory.cshtml.cs` - Permission-based filtering
- `Pages/Workspaces/Locations.cshtml.cs` - Permission-based filtering
- `Pages/Workspaces/Contacts.cshtml.cs` - Permission-based filtering

**Business Logic Found:**
- Workspace slug-based lookup and access validation
- User workspace membership checks
- Admin privilege verification
- Dashboard metric calculations (total, open, resolved tickets)
- Active member counting
- Resolution time calculation
- Permission-based UI state determination

#### C. Data Loading & Aggregation
**Files Affected:**
- `Pages/Workspace.cshtml.cs` - Complex dashboard aggregation
- `Pages/Workspaces/Locations.cshtml.cs` - Contact preview aggregation
- `Pages/Workspaces/Inventory.cshtml.cs` - Status-based filtering & archiving

**Business Logic Found:**
- Workspace membership enumeration
- Dashboard data aggregation (metrics, top members, recent tickets)
- Activity sparkline generation
- Contact preview generation with pagination
- Inventory status archiving/restoration
- List filtering by query and status parameters

#### D. Validation Logic
**Files Affected:**
- `Pages/Signup.cshtml.cs` - Custom email validation
- `Pages/Users/Create.cshtml.cs` - Duplicate email checking

**Business Logic Found:**
- Recovery email must differ from login email validation
- Duplicate user email detection
- Password confirmation matching (handled by annotations)

### 2. Current Anti-Patterns

1. **Repeated TryGetUserId() Pattern**
   - Duplicated in every PageModel for claim extraction
   - Should be centralized in a helper service

2. **Direct Repository Access in PageModels**
   - PageModels directly call repository methods
   - Missing abstraction layer for business operations
   - Makes testing difficult

3. **Inline Permission Logic**
   - Permission checks scattered throughout OnGet/OnPost methods
   - Mixed with data loading logic
   - Hard to reuse and test

4. **Data Aggregation in PageModels**
   - Complex queries like dashboard metrics calculated in OnGetAsync
   - Difficult to unit test
   - Tight coupling to repository interfaces

5. **Mixed Concerns**
   - Authorization checks intermingled with data loading
   - No clear separation between "can I access this?" and "what data should I load?"

## Refactoring Strategy

### Phase 1: Create Core Domain Services

#### 1.1 Authentication & User Services
**Service: IUserManagementService**
```
CreateUserAsync(name, email, recoveryEmail, password)
UpdateUserAsync(userId, name, email, recoveryEmail)
ValidateDuplicateEmailAsync(email, excludeUserId?)
GetUserByIdWithAuthCheckAsync(userId, requiredRole?)
```

**Service: ICurrentUserService**
```
GetCurrentUserIdAsync(claimsPrincipal)
GetCurrentUserAsync(claimsPrincipal)
IsCurrentUserSystemAdminAsync(claimsPrincipal)
```

#### 1.2 Permission & Authorization Services
**Service: IWorkspaceAccessService**
```
VerifyUserWorkspaceAccessAsync(userId, workspaceId)
VerifyWorkspaceAdminAsync(userId, workspaceId)
GetUserWorkspacePermissionsAsync(userId, workspaceId)
CanUserViewDashboardAsync(userId, workspaceId)
CanUserManageRolesAsync(userId, workspaceId)
```

**Service: IRoleManagementService**
```
AssignRoleToUserAsync(userId, workspaceId, roleId, assignedBy)
RemoveRoleFromUserAsync(userId, workspaceId, roleId)
CountRoleAssignmentsAsync(workspaceId, roleId)
VerifyRoleBelongsToWorkspaceAsync(roleId, workspaceId)
```

#### 1.3 Dashboard & Aggregation Services
**Service: IDashboardService**
```
LoadDashboardMetricsAsync(workspaceId, rangeDays, userId)
GetTicketStatsAsync(workspaceId, rangeDays)
GetTopMembersAsync(workspaceId, rangeDays, limit)
GetRecentTicketsAsync(workspaceId, limit, userId)
CalculateAverageResolutionTimeAsync(workspaceId, rangeDays)
GetActivitySparklineAsync(workspaceId, days)
```

#### 1.4 Workspace Services
**Service: IWorkspaceService**
```
GetUserWorkspacesAsync(userId)
GetWorkspaceBySlugAsync(slug)
VerifyUserMembershipAsync(userId, workspaceId)
```

#### 1.5 Notification Preference Services
**Service: INotificationPreferenceService**
```
InitializeDefaultPreferencesAsync(userId)
GetNotificationPreferencesForUserAsync(userId)
UpdateUserPreferencesAsync(userId, preferences)
GetNotificationTypeDefinitionsAsync()
```

#### 1.6 Inventory & Status Services
**Service: IInventoryManagementService**
```
ArchiveInventoryAsync(inventoryId, workspaceId)
RestoreInventoryAsync(inventoryId, workspaceId)
ListInventoryAsync(workspaceId, query, status)
```

### Phase 2: Implement Services in Tickflo.Core

Create service interfaces and implementations following the existing pattern:
- Create `IXxxService.cs` interfaces
- Create `XxxService.cs` implementations
- Follow dependency injection through constructors
- Use existing repositories for data access

### Phase 3: Update PageModels

1. Remove direct repository dependencies where services exist
2. Inject new services via constructor
3. Replace logic-heavy methods with service calls
4. Keep PageModels focused on:
   - Page state management (ViewData, properties)
   - HTTP request/response handling
   - Calling appropriate services

### Phase 4: Update Program.cs

Register all new services in the DI container with proper scopes.

## Implementation Order

1. **Priority 1: Foundation Services**
   - ICurrentUserService (enables other services)
   - IUserManagementService
   - IWorkspaceAccessService

2. **Priority 2: Authorization Services**
   - IRoleManagementService
   - IWorkspaceService

3. **Priority 3: Data Aggregation**
   - IDashboardService
   - IInventoryManagementService

4. **Priority 4: Secondary Services**
   - INotificationPreferenceService

## Benefits

✅ **Single Responsibility** - PageModels focus on HTTP concerns, services handle business logic
✅ **Testability** - Services can be unit tested independently with mocked repositories
✅ **Reusability** - Services can be used by Controllers, SignalR hubs, background jobs
✅ **Maintainability** - Business logic centralized and versioned with Core
✅ **Clean Code** - Clear separation of concerns, reduced duplication
✅ **Dependency Injection** - Loose coupling through interfaces
✅ **Consistency** - Follows existing Entity Framework repository pattern

## Risk Mitigation

- Incremental refactoring reduces risk of breaking changes
- Each service isolated and independently testable
- Existing PageModel behavior maintained (behavior-preserving refactoring)
- Controllers already provide a template for service consumption
