# GitHub Issues to Create

This document contains ready-to-create GitHub issues based on the code review findings. Each section can be copied directly into a new GitHub issue.

---

## Issue 1: Replace `ws` abbreviation with `workspace` throughout codebase

**Labels:** `refactoring`, `code-quality`, `good-first-issue`  
**Priority:** HIGH  
**Project:** Core, Web

### Description

The codebase extensively uses `ws` as an abbreviation for `Workspace` variables, violating the naming convention guidelines in `copilot-instructions.md`.

### Rule Violated

**Naming Conventions - Variables & Fields** (copilot-instructions.md lines 174-195):
> Variable names should match the type they reference  
> Do NOT use vague or shortened names  
> No acronyms unless they are widely understood

### Current State

30+ instances of `ws` found across the codebase:

**Core Services:**
- `Tickflo.Core/Services/Authentication/PasswordSetupService.cs` - Line 117
- `Tickflo.Core/Services/Authentication/AuthenticationService.cs`

**Web PageModels:**
- `Tickflo.Web/Pages/Workspaces/ReportDelete.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/RolesEdit.cshtml.cs` - Lines 53, 86
- `Tickflo.Web/Pages/Workspaces/TeamsEdit.cshtml.cs` - Lines 46, 82, 157, 169
- `Tickflo.Web/Pages/Workspaces/UsersInvite.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/ReportRun.cshtml.cs`
- Many more...

### Examples

```csharp
// ❌ Current (Incorrect)
var ws = await this.workspaceRepository.FindBySlugAsync(slug);
private async Task CreateTeamAsync(Workspace ws, string name)

// ✅ Expected (Correct)
var workspace = await this.workspaceRepository.FindBySlugAsync(slug);
private async Task CreateTeamAsync(Workspace workspace, string name)
```

### Acceptance Criteria

- [ ] Replace all `ws` variable names with `workspace`
- [ ] Update all method parameters using `ws`
- [ ] Ensure no compilation errors
- [ ] Verify all tests still pass

### Estimated Effort

2-3 hours (straightforward find-and-replace with verification)

---

## Issue 2: Remove repository injections from PageModels (Architecture violation)

**Labels:** `architecture`, `breaking-change`, `high-priority`, `technical-debt`  
**Priority:** CRITICAL  
**Project:** Web, Core

### Description

18 PageModels in Tickflo.Web directly inject and use repository interfaces, violating the architectural principle that "Persistence concerns must not leak into UI or Razor Pages."

### Rules Violated

**Repository Structure & Responsibilities** (copilot-instructions.md line 29):
> Persistence concerns must not leak into UI or Razor Pages

**Razor & UI Guidelines** (copilot-instructions.md lines 88-93):
> PageModels should be thin  
> Prefer direct use of application services from Core

### Current State

**Critical Violations:**

1. **Settings.cshtml.cs** - Injects 7 repositories:
   - IWorkspaceRepository, ITicketStatusRepository, ITicketPriorityRepository, ITicketTypeRepository, IUserRepository, and more
   - ~760 lines with direct CRUD operations

2. **UsersEdit.cshtml.cs** - Injects 5 repositories:
   - IWorkspaceRepository, IUserRepository, IUserWorkspaceRepository, IUserWorkspaceRoleRepository, IRoleRepository

3. **Tickets.cshtml.cs** - Injects 3 repositories with direct query calls
4. **TicketsDetails.cshtml.cs** - Injects 4 repositories
5. **Users.cshtml.cs** - Injects 4 repositories with direct mutations

**Additional affected PageModels:**
- Inventory.cshtml.cs, InventoryEdit.cshtml.cs, Profile.cshtml.cs, Error.cshtml.cs, UsersInvite.cshtml.cs, RolesEdit.cshtml.cs, Workspace.cshtml.cs, and more (18 total)

### Expected Architecture

```
PageModel → Application Service (Core) → Repository (Core)
```

**NOT:**
```
PageModel → Repository (Core) ❌
```

### Acceptance Criteria

- [ ] Remove all repository injections from PageModels
- [ ] Create or use existing application services in Tickflo.Core for business operations
- [ ] PageModels should only inject:
  - Application services (from Core)
  - ViewServices (from Core)
  - ASP.NET Core framework services (ILogger, etc.)
- [ ] Update Settings.cshtml.cs to use WorkspaceSettingsService
- [ ] Update all other affected PageModels
- [ ] All tests pass
- [ ] No business logic remains in PageModels

### Estimated Effort

3-4 weeks (requires creating/refactoring application services)

### Breaking Changes

This will require creating new application services or extending existing ones.

---

## Issue 3: Extract business logic from Settings.cshtml.cs to application service

**Labels:** `refactoring`, `technical-debt`, `high-priority`, `architecture`  
**Priority:** CRITICAL  
**Project:** Web, Core

### Description

`Settings.cshtml.cs` contains approximately 760 lines of code with extensive business logic, CRUD operations, and validation that should be in application services.

### Rules Violated

**Razor & UI Guidelines** (copilot-instructions.md lines 88-93):
> PageModels should be thin  
> No business logic in UI  
> Prefer direct use of application services from Core

### Current State

**Business Logic in Settings.cshtml.cs:**
- **Complex form parsing** (lines 506-757): Manual parsing with regex
- **Direct repository mutations** (lines 537, 548, 557, 582, 628, 651, 718)
- **Slug validation** (lines 521-535): Business rules in UI
- **Status/Priority/Type CRUD** (lines 190-279, 282-381, 435-459)
- **Permission checks mixed with logic** (line 110-115)

**Example of problematic code:**
```csharp
public async Task<IActionResult> OnPostAddStatusAsync(/* ... */)
{
    // Full CRUD logic in PageModel
    var status = new TicketStatus { /* ... */ };
    await this.statusRepository.AddAsync(status);
}
```

### Expected Behavior

Settings.cshtml.cs should:
1. Validate basic input
2. Call `WorkspaceSettingsService` methods
3. Handle responses
4. Return appropriate ActionResults

All business logic should be in `WorkspaceSettingsService` in Tickflo.Core.

### Acceptance Criteria

- [ ] Create `IWorkspaceSettingsManagementService` in Tickflo.Core
- [ ] Move all CRUD operations to the service:
  - Status management (add, update, delete)
  - Priority management (add, update, delete)
  - Type management (add, update, delete)
  - Slug validation and update
  - Workspace settings updates
- [ ] Refactor Settings.cshtml.cs to be thin (<200 lines)
- [ ] Remove all repository injections from Settings.cshtml.cs
- [ ] Add comprehensive tests for the new service
- [ ] All existing functionality works

### Estimated Effort

1-2 weeks

---

## Issue 4: Extract business logic from Tickets.cshtml.cs to application services

**Labels:** `refactoring`, `technical-debt`, `architecture`  
**Priority:** HIGH  
**Project:** Web, Core

### Description

`Tickets.cshtml.cs` contains filtering logic, pagination, ID resolution, and assignment logic that should be in application services.

### Rules Violated

**Razor & UI Guidelines** (copilot-instructions.md lines 88-93):
> PageModels should be thin  
> No business logic in UI

### Current State

**Business Logic in Tickets.cshtml.cs:**
- **Filter application** (lines 124-194): `ApplyStatusOpenFilter()`, `ApplyContactFilter()`, `ApplyTeamFilter()`
- **Pagination logic** (lines 232-246): Custom normalization
- **ID resolution** (lines 150-178): `ResolveStatusId()`, `ResolvePriorityId()`, `ResolveTypeId()`
- **Assignment logic** (lines 286-301): Ticket assignment with entity mutations
- **Direct repository calls** (line 91): `ticketRepository.ListAsync()`

### Expected Behavior

Tickets.cshtml.cs should:
1. Collect filter parameters
2. Call `TicketFilterService` (already exists but underutilized)
3. Call `TicketAssignmentService` for assignments
4. Map results to view models

### Acceptance Criteria

- [ ] Enhance `TicketFilterService` to handle all filtering logic
- [ ] Move ID resolution to appropriate services
- [ ] Move assignment logic to `TicketAssignmentService`
- [ ] Remove direct repository usage
- [ ] Refactor Tickets.cshtml.cs to be thin (<150 lines)
- [ ] All tests pass

### Estimated Effort

1 week

---

## Issue 5: Rename utility classes to remove Helper/Util suffixes

**Labels:** `refactoring`, `code-quality`, `naming`  
**Priority:** MEDIUM  
**Project:** Core, Web

### Description

Several utility classes use anti-pattern suffixes like "Helper" and exist in "Utils" folders, violating naming guidelines.

### Rules Violated

**Service Design Guidelines - Naming** (copilot-instructions.md lines 126-137):
> Avoid suffixes like: Manager, Helper, Util

**What Copilot Should Avoid Generating** (lines 314-320):
> Large static helper classes

### Affected Files

1. **ImageHelper.cs** (`Tickflo.Web/Utils/`)
   - Uses "Helper" suffix
   - Static utility class
   - Recommendation: Rename to `ImageCompressor` or `ImageProcessor`, consider making injectable

2. **TicketHistoryFormatter.cs** (`Tickflo.Web/Utils/`)
   - Static utility class
   - Recommendation: Convert to injectable `ITicketHistoryFormatter` service

3. **TokenGenerator.cs** (`Tickflo.Core/Utils/`)
   - Static utility class with generic name
   - Recommendation: Rename to `SecureTokenGenerator`, consider making injectable

### Acceptance Criteria

- [ ] Rename `ImageHelper` → `ImageCompressor` (or similar intent-revealing name)
- [ ] Rename `TokenGenerator` → `SecureTokenGenerator`
- [ ] Consider converting `TicketHistoryFormatter` to injectable service
- [ ] Update all references
- [ ] Consider eliminating Utils folders if possible
- [ ] All tests pass

### Estimated Effort

4-6 hours

---

## Issue 6: Update test naming convention to follow MethodName_WhenCondition_ShouldExpectedOutcome

**Labels:** `testing`, `code-quality`, `refactoring`  
**Priority:** MEDIUM  
**Project:** CoreTest

### Description

Approximately 85% of test methods don't follow the documented naming convention, reducing readability and consistency.

### Rule Violated

**Testing Guidelines** (copilot-instructions.md lines 270-282):
> Naming convention: `MethodName_WhenCondition_ShouldExpectedOutcome`

### Current State

**Examples from UserManagementServiceTests.cs:**
```csharp
// ❌ Current (Incorrect)
CreateUserAsyncThrowsOnDuplicateEmail
CreateUserAsyncNormalizesEmail
IsEmailInUseAsyncReturnsFalseWhenNotFound
```

**Examples from AuthenticationServiceTests.cs:**
```csharp
// ❌ Current (Incorrect)
AuthenticateAsyncValidCredentialsReturnsSuccess
AuthenticateAsyncUserNotFoundReturnsError
```

### Expected Convention

```csharp
// ✅ Expected (Correct)
CreateUserAsync_WhenEmailIsDuplicate_ShouldThrowInvalidOperationException
CreateUserAsync_WhenEmailHasMixedCase_ShouldNormalizeToLowercase
IsEmailInUseAsync_WhenEmailNotFound_ShouldReturnFalse

AuthenticateAsync_WhenCredentialsAreValid_ShouldReturnSuccess
AuthenticateAsync_WhenUserNotFound_ShouldReturnError
```

### Affected Files

All 60+ test files in Tickflo.CoreTest

**Common Anti-Patterns:**
1. Missing "When" separator between method and condition
2. Using "Returns" or "Throws" instead of "Should"
3. Combining clauses without clear separation

### Acceptance Criteria

- [ ] Refactor all test names to follow `MethodName_WhenCondition_ShouldExpectedOutcome`
- [ ] Use clear "When" separators with underscores
- [ ] Use "Should" prefix for expected outcomes
- [ ] All tests still pass after renaming
- [ ] Update code review checklist to enforce convention

### Estimated Effort

2-3 weeks (mechanical but extensive - can be done incrementally)

### Notes

This can be done incrementally, file by file, without breaking functionality.

---

## Issue 7: Consolidate or eliminate single-use ViewServices

**Labels:** `architecture`, `refactoring`, `service-layer`  
**Priority:** MEDIUM  
**Project:** Core

### Description

Multiple ViewServices follow a 1:1 pattern with PageModels, violating the guideline to avoid creating services used by a single PageModel.

### Rule Violated

**Razor & UI Guidelines** (copilot-instructions.md line 92):
> Avoid creating services used by a single PageModel

### Affected Services

15+ ViewServices that appear to be single-use:

| ViewService | PageModel | Recommendation |
|-------------|-----------|----------------|
| IWorkspaceInventoryViewService | Inventory.cshtml.cs | Merge with edit service |
| IWorkspaceInventoryEditViewService | InventoryEdit.cshtml.cs | Merge with view service |
| IWorkspaceReportsViewService | Reports.cshtml.cs | Consider if needed |
| IWorkspaceReportsEditViewService | ReportsEdit.cshtml.cs | Merge with view service |
| IWorkspaceContactsViewService | Contacts.cshtml.cs | Merge with edit service |
| IWorkspaceContactsEditViewService | ContactsEdit.cshtml.cs | Merge with view service |
| IWorkspaceLocationsViewService | Locations.cshtml.cs | Merge with edit service |
| IWorkspaceLocationsEditViewService | LocationsEdit.cshtml.cs | Merge with view service |
| IWorkspaceTeamsViewService | Teams.cshtml.cs | Merge with edit service |
| IWorkspaceTeamsEditViewService | TeamsEdit.cshtml.cs | Merge with view service |
| IWorkspaceRolesViewService | Roles.cshtml.cs | Consider consolidation |
| IWorkspaceRolesEditViewService | RolesEdit.cshtml.cs | Merge with view service |
| IWorkspaceRolesAssignViewService | RolesAssign.cshtml.cs | Merge with roles service |
| IWorkspaceTicketDetailsViewService | TicketsDetails.cshtml.cs | May be justified |
| IWorkspaceTicketsSaveViewService | TicketsDetails.cshtml.cs | Merge with details |

### Proposal

1. **Merge Edit + View services** where they serve the same domain (e.g., Inventory, Contacts, Locations, Teams)
2. **Evaluate necessity** of ViewServices - can application services be used directly?
3. **Consolidate related services** under single interface with multiple methods

### Example Consolidation

```csharp
// Before: Two services
IWorkspaceInventoryViewService
IWorkspaceInventoryEditViewService

// After: One service
public interface IWorkspaceInventoryViewService
{
    Task<InventoryListView> BuildListViewAsync(int workspaceId, int userId);
    Task<InventoryEditView> BuildEditViewAsync(int workspaceId, int userId, int? inventoryId);
}
```

### Acceptance Criteria

- [ ] Identify ViewServices that can be merged
- [ ] Consolidate related ViewServices
- [ ] Update PageModels to use consolidated services
- [ ] Remove obsolete interfaces and implementations
- [ ] All tests pass
- [ ] No functionality is lost

### Estimated Effort

2-3 weeks

---

## Issue 8: Fix variable naming in ScheduledReportsHostedService

**Labels:** `refactoring`, `code-quality`, `good-first-issue`, `naming`  
**Priority:** MEDIUM  
**Project:** Web

### Description

`ScheduledReportsHostedService.cs` uses abbreviated variable names that don't match their types.

### Rule Violated

**Naming Conventions - Variables & Fields** (copilot-instructions.md lines 174-195):
> Variable names should match the type they reference

### Current State

**File:** `Tickflo.Web/Services/ScheduledReportsHostedService.cs`

```csharp
// Line 22-23 - Incorrect
var db = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
var runSvc = scope.ServiceProvider.GetRequiredService<IReportRunService>();
```

### Expected Behavior

```csharp
// Correct
var dbContext = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
var reportRunService = scope.ServiceProvider.GetRequiredService<IReportRunService>();
```

Or even more explicit:
```csharp
var tickfloDbContext = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
var reportRunService = scope.ServiceProvider.GetRequiredService<IReportRunService>();
```

### Acceptance Criteria

- [ ] Rename `db` to `dbContext` (or `tickfloDbContext`)
- [ ] Rename `runSvc` to `reportRunService`
- [ ] Update all references in the method
- [ ] Verify service still works
- [ ] All tests pass

### Estimated Effort

15 minutes

---

## Issue 9: Add behavior to domain entities (address anemic domain model)

**Labels:** `architecture`, `ddd`, `enhancement`, `long-term`, `domain-model`  
**Priority:** MEDIUM (Long-term improvement)  
**Project:** Core

### Description

All entity classes in `Tickflo.Core/Entities` are anemic domain models - pure data containers with no behavior, violating Domain-Driven Design principles.

### Rules Violated

**Architectural Principles - Domain-Driven Design** (copilot-instructions.md lines 97-120):
> Centers around the domain, not the database or UI  
> Encapsulates invariants inside entities or aggregates  
> Avoid: Anemic domain models

### Current State

Entities only contain properties with public setters:

```csharp
// Current: Ticket.cs
public class Ticket : IWorkspaceEntity
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? AssignedUserId { get; set; }
    // ... only properties
}
```

### Expected Rich Domain Model

```csharp
// Expected: Ticket.cs with behavior
public class Ticket : IWorkspaceEntity
{
    public int Id { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int? AssignedUserId { get; private set; }
    
    // Constructor enforces invariants
    public Ticket(int workspaceId, string subject, string description)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));
            
        WorkspaceId = workspaceId;
        Subject = subject;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
    
    // Business methods
    public void AssignTo(int userId)
    {
        AssignedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateSubject(string newSubject)
    {
        if (string.IsNullOrWhiteSpace(newSubject))
            throw new ArgumentException("Subject cannot be empty", nameof(newSubject));
            
        Subject = newSubject;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Close()
    {
        // Business logic for closing
    }
}
```

### Affected Entities

All entities in `Tickflo.Core/Entities`:
- Ticket
- User
- Workspace
- Contact
- Team
- Role
- Location
- Inventory
- Report
- And more...

### Acceptance Criteria

- [ ] Identify key business rules for each entity
- [ ] Add constructors that enforce invariants
- [ ] Convert public setters to private where appropriate
- [ ] Add behavior methods that encapsulate business logic
- [ ] Move entity-specific logic from services to entities
- [ ] Update services to use entity methods
- [ ] Add unit tests for entity behavior
- [ ] Ensure EF Core still works (may need special configuration)
- [ ] All existing tests pass

### Migration Strategy

1. Start with one entity (e.g., Ticket)
2. Add behavior incrementally
3. Refactor services that use the entity
4. Test thoroughly
5. Repeat for other entities

### Estimated Effort

Ongoing (3-6 months for full codebase)

### Notes

This is a significant architectural improvement that should be done incrementally. Start with core aggregates like Ticket and User.

---

## Issue 10: Extract business logic from Users.cshtml.cs

**Labels:** `refactoring`, `technical-debt`, `architecture`  
**Priority:** MEDIUM  
**Project:** Web, Core

### Description

`Users.cshtml.cs` contains business logic including direct repository operations, user workspace mutations, and notification logic.

### Rules Violated

**Razor & UI Guidelines** (copilot-instructions.md lines 88-93):
> PageModels should be thin  
> No business logic in UI  
> Prefer direct use of application services from Core

### Current State

**Business Logic in Users.cshtml.cs:**
- **Direct repository operations** (lines 79-86): UserWorkspace entity mutation
- **Business logic in OnPostResend** (lines 92-100+)
- **String constants for notification types** (lines 14-20)
- **Injected repositories**: IWorkspaceRepository, IUserRepository, IUserWorkspaceRepository, INotificationRepository

### Expected Behavior

Users.cshtml.cs should:
1. Call `UserManagementService` or similar for user operations
2. Call `NotificationService` for resending notifications
3. Be thin orchestration layer only

### Acceptance Criteria

- [ ] Move user workspace management to `UserManagementService`
- [ ] Move notification resend logic to `NotificationService`
- [ ] Remove direct repository injections
- [ ] Refactor Users.cshtml.cs to be thin (<150 lines)
- [ ] All tests pass

### Estimated Effort

3-5 days

---

## Issue 11: Extract business logic from InventoryEdit.cshtml.cs

**Labels:** `refactoring`, `technical-debt`, `architecture`  
**Priority:** MEDIUM  
**Project:** Web, Core

### Description

`InventoryEdit.cshtml.cs` contains inventory item creation and mutation logic that should be in application services.

### Rules Violated

**Razor & UI Guidelines** (copilot-instructions.md lines 88-93):
> PageModels should be thin  
> No business logic in UI

### Current State

**Issues:**
- Direct repository usage for inventory operations
- Item creation/mutation logic in handlers
- Business rules in PageModel

### Expected Behavior

InventoryEdit.cshtml.cs should:
1. Validate basic input
2. Call `InventoryManagementService` (or similar)
3. Handle responses
4. Return results

### Acceptance Criteria

- [ ] Create or use `InventoryManagementService` for CRUD operations
- [ ] Remove repository injections
- [ ] Move all business logic to service
- [ ] Refactor InventoryEdit.cshtml.cs to be thin
- [ ] All tests pass

### Estimated Effort

2-3 days

---

## Summary

**Total Issues:** 11  
**High Priority:** 4 issues  
**Medium Priority:** 6 issues  
**Long-term:** 1 issue

**Estimated Total Effort:** 3-6 months (depending on priority and resource allocation)

---

## Priority Order Recommendation

1. Issue #2 - Remove repository injections (CRITICAL - architectural foundation)
2. Issue #3 - Extract logic from Settings.cshtml.cs (CRITICAL - worst offender)
3. Issue #1 - Replace `ws` abbreviation (HIGH - easy win, widespread)
4. Issue #4 - Extract logic from Tickets.cshtml.cs (HIGH)
5. Issue #8 - Fix ScheduledReportsHostedService naming (MEDIUM - quick win)
6. Issue #5 - Rename utility classes (MEDIUM)
7. Issue #10 - Extract logic from Users.cshtml.cs (MEDIUM)
8. Issue #11 - Extract logic from InventoryEdit.cshtml.cs (MEDIUM)
9. Issue #7 - Consolidate ViewServices (MEDIUM)
10. Issue #6 - Update test naming (MEDIUM - can be done incrementally)
11. Issue #9 - Add entity behavior (LONG-TERM - architectural evolution)
