# Copilot Instructions Compliance Review Report

**Date:** January 22, 2026  
**Repository:** tickflo/tickflo  
**Reviewer:** GitHub Copilot (Automated Code Review)

---

## Executive Summary

This report documents violations of the coding standards defined in `.github/copilot-instructions.md`. A comprehensive review of the Tickflo .NET monorepo identified **7 major categories of violations** affecting code quality, maintainability, and architectural alignment.

**Total Violations Found:**
- Variable naming: 30+ instances
- Utility class naming: 3 files
- Repository leaking to UI: 18 PageModels
- Business logic in UI: 5+ critical PageModels
- Single-use services: 15+ ViewServices
- Test naming: ~85% of tests (60+ test files)
- Anemic domain models: All entity classes

---

## Violation Categories

### 1. Variable Naming Violations - `ws` Abbreviation

**Severity:** HIGH  
**Affected Projects:** Tickflo.Core, Tickflo.Web  
**Rule Violated:** Naming Conventions - Variables & Fields (copilot-instructions.md lines 174-195)

**Description:**
The codebase extensively uses `ws` as an abbreviation for `Workspace`, violating the rule that "Variable names should match the type they reference" and "Avoid abbreviations and shortened forms."

**Locations:**

#### Core Services (6 instances):
- `Tickflo.Core/Services/Authentication/PasswordSetupService.cs` - Line 117
- `Tickflo.Core/Services/Authentication/AuthenticationService.cs` - Multiple occurrences

#### Web PageModels (28+ instances):
- `Tickflo.Web/Pages/Workspaces/ReportDelete.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/RolesEdit.cshtml.cs` - Lines 53, 86
- `Tickflo.Web/Pages/Workspaces/ReportRunView.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/ReportRunDownload.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/UsersInvite.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/TeamsEdit.cshtml.cs` - Lines 46, 82, 157, 169 (method parameters)
- `Tickflo.Web/Pages/Workspaces/RolesAssign.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/ReportRun.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/ReportRunsBackfill.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/TeamsAssign.cshtml.cs`
- `Tickflo.Web/Pages/Workspace.cshtml.cs`
- Plus additional instances in Controllers

**Example Violations:**
```csharp
// ❌ Incorrect
var ws = await this.workspaceRepository.FindBySlugAsync(slug);
private async Task CreateTeamAsync(Workspace ws, string name)

// ✅ Correct
var workspace = await this.workspaceRepository.FindBySlugAsync(slug);
private async Task CreateTeamAsync(Workspace workspace, string name)
```

**Impact:** Reduces code readability and violates explicit naming standards.

**Recommendation:** Replace all instances of `ws` with `workspace` throughout the codebase.

---

### 2. Variable Naming Violations - Database Context Abbreviation

**Severity:** MEDIUM  
**Affected Projects:** Tickflo.Web  
**Rule Violated:** Naming Conventions - Variables & Fields (copilot-instructions.md lines 174-195)

**Description:**
The ScheduledReportsHostedService uses abbreviated variable names that don't match their types.

**Locations:**
- `Tickflo.Web/Services/ScheduledReportsHostedService.cs`
  - Line 22: `var db` (should be `dbContext` or `tickfloDbContext`)
  - Line 23: `var runSvc` (should be `reportRunService`)

**Example Violations:**
```csharp
// ❌ Incorrect
var db = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
var runSvc = scope.ServiceProvider.GetRequiredService<IReportRunService>();

// ✅ Correct
var dbContext = scope.ServiceProvider.GetRequiredService<TickfloDbContext>();
var reportRunService = scope.ServiceProvider.GetRequiredService<IReportRunService>();
```

**Impact:** Inconsistent with naming standards and reduces clarity.

**Recommendation:** Rename `db` to `dbContext` and `runSvc` to `reportRunService`.

---

### 3. Utility/Helper Class Naming Violations

**Severity:** MEDIUM  
**Affected Projects:** Tickflo.Core, Tickflo.Web  
**Rule Violated:** Service Design Guidelines - Naming (copilot-instructions.md lines 126-137) and Classes (lines 218-227)

**Description:**
Several utility classes use anti-pattern suffixes like "Helper" and "Util", and implement static helper patterns that should be avoided.

**Violations:**

#### A. ImageHelper (Tickflo.Web/Utils/ImageHelper.cs)
- **Issue:** Uses "Helper" suffix
- **Type:** Static class with utility methods
- **Rule:** "Avoid suffixes like: Helper, Util"
- **Recommendation:** Rename to `ImageCompressor` or `ImageProcessor` and consider making it an injectable service

#### B. TicketHistoryFormatter (Tickflo.Web/Utils/TicketHistoryFormatter.cs)
- **Issue:** Static utility class in Utils folder
- **Type:** Static class with formatting logic
- **Rule:** "What Copilot Should Avoid Generating: Large static helper classes"
- **Recommendation:** Convert to injectable `ITicketHistoryFormatter` service

#### C. TokenGenerator (Tickflo.Core/Utils/TokenGenerator.cs)
- **Issue:** Static utility class, generic "Generator" name
- **Type:** Static class in Utils folder
- **Rule:** "Avoid: Helper, Utils" folders/patterns
- **Recommendation:** Rename to `SecureTokenGenerator` or similar intent-revealing name, consider injectable service

**Impact:** These utilities represent anti-patterns that reduce testability and violate DI principles.

**Recommendation:** 
1. Rename classes to use intent-revealing names
2. Consider converting static classes to injectable services
3. Remove or minimize Utils folders

---

### 4. Repository Leaking to UI Layer (CRITICAL)

**Severity:** CRITICAL  
**Affected Projects:** Tickflo.Web  
**Rule Violated:** 
- Repository Structure & Responsibilities - Tickflo.Core rules (line 29)
- Razor & UI Guidelines (lines 88-93)

**Description:**
Multiple PageModels directly inject and use repository interfaces, violating the principle that "Persistence concerns must not leak into UI or Razor Pages" and "Prefer direct use of application services from Core."

**Critical Violations:**

#### A. Settings.cshtml.cs - WORST OFFENDER
- **Repositories Injected:** 7 total
  - IWorkspaceRepository
  - ITicketStatusRepository  
  - ITicketPriorityRepository
  - ITicketTypeRepository
  - IUserRepository
  - Plus more
- **Lines of Code:** ~760 lines
- **Business Logic:** Direct CRUD operations on repositories (lines 537, 548, 557, 582, 628, 651, 718)
- **Severity:** CRITICAL

#### B. UsersEdit.cshtml.cs
- **Repositories Injected:** 5 total
  - IWorkspaceRepository
  - IUserRepository
  - IUserWorkspaceRepository
  - IUserWorkspaceRoleRepository
  - IRoleRepository
- **Severity:** HIGH

#### C. Tickets.cshtml.cs
- **Repositories Injected:** 3 total
  - IWorkspaceRepository
  - IUserWorkspaceRepository
  - ITicketRepository
- **Business Logic:** Direct `ticketRepository.ListAsync()` calls (line 91)
- **Severity:** HIGH

#### D. TicketsDetails.cshtml.cs
- **Repositories Injected:** 4 total
  - IWorkspaceRepository
  - IUserWorkspaceRepository
  - ITicketRepository
  - ITeamRepository
- **Severity:** HIGH

#### E. Users.cshtml.cs
- **Repositories Injected:** 4 total
  - IWorkspaceRepository
  - IUserRepository
  - IUserWorkspaceRepository
  - INotificationRepository
- **Business Logic:** Direct repository mutations (lines 79-86)
- **Severity:** HIGH

**Additional PageModels with Repository Violations:**
- Inventory.cshtml.cs (3 repositories)
- InventoryEdit.cshtml.cs (3 repositories)
- Profile.cshtml.cs (IUserRepository)
- Error.cshtml.cs (IUserRepository)
- UsersInvite.cshtml.cs (4 repositories)
- RolesEdit.cshtml.cs (3 repositories)
- Workspace.cshtml.cs (IUserRepository)
- Plus additional PageModels

**Total Count:** 18+ PageModels

**Impact:** 
- Violates architectural boundaries
- Makes UI layer dependent on persistence implementation
- Reduces testability
- Breaks DDD principles

**Recommendation:**
1. Remove all repository injections from PageModels
2. Create or use application services in Core to encapsulate persistence operations
3. PageModels should only inject application services and ViewServices

---

### 5. Business Logic in PageModels (CRITICAL)

**Severity:** CRITICAL  
**Affected Projects:** Tickflo.Web  
**Rule Violated:** Razor & UI Guidelines - "PageModels should be thin" and "No business logic in UI" (lines 88-93)

**Description:**
Several PageModels contain significant business logic that should be moved to application services in Tickflo.Core.

**Critical Violations:**

#### A. Settings.cshtml.cs (~760 lines) - WORST OFFENDER
**Business Logic Found:**
- **Complex form parsing** (lines 506-757): Manual form data parsing with regex patterns
- **Direct repository mutations** (lines 537, 548, 557, 582, 628, 651, 718)
- **Slug validation logic** (lines 521-535): Uniqueness checks and business rules
- **Permission checks mixed with logic** (line 110-115)
- **Status/Priority/Type CRUD** (lines 190-279, 282-381, 435-459): Full entity lifecycle management

**Example:**
```csharp
// Lines 190-279 - Full CRUD logic in PageModel
public async Task<IActionResult> OnPostAddStatusAsync(/* ... */)
{
    // Validation, creation, persistence all in PageModel
    var status = new TicketStatus { /* ... */ };
    await this.statusRepository.AddAsync(status);
}
```

**Recommendation:** Extract all CRUD and validation logic into `WorkspaceSettingsService` in Core.

#### B. Tickets.cshtml.cs
**Business Logic Found:**
- **Filter application** (lines 124-194): Complex filtering logic with business rules
  - `ApplyStatusOpenFilter()`
  - `ApplyContactFilter()`
  - `ApplyTeamFilter()`
- **Pagination logic** (lines 232-246): Custom normalization
- **ID resolution** (lines 150-178): Business lookups in UI layer
- **Assignment logic** (lines 286-301): Ticket assignment with entity mutations

**Recommendation:** Move filtering to `TicketFilterService` (already exists but underutilized), extract assignment to `TicketAssignmentService`.

#### C. Users.cshtml.cs
**Business Logic Found:**
- **Direct repository operations** (lines 79-86): UserWorkspace entity mutation
- **Business logic in OnPostResend** (lines 92-100+)
- **String constants** for notification types and statuses (lines 14-20)

**Recommendation:** Move user management logic to `UserManagementService`.

#### D. InventoryEdit.cshtml.cs
**Business Logic Found:**
- Direct use of `IInventoryAllocationService` via repository
- Item creation/mutation logic in handler methods

**Recommendation:** Simplify to pure orchestration using application services.

**Impact:**
- Violates separation of concerns
- Makes business logic untestable outside of UI context
- Duplicates logic across PageModels
- Reduces reusability

**Recommendation:**
1. Extract all business logic to application services in Tickflo.Core
2. PageModels should only: validate input, call services, map to view models, return results
3. Aim for PageModels under 200 lines

---

### 6. Single-Use ViewService Pattern Violations

**Severity:** MEDIUM  
**Affected Projects:** Tickflo.Core (Services/Views)  
**Rule Violated:** Razor & UI Guidelines - "Avoid creating services used by a single PageModel" (line 92)

**Description:**
Many ViewServices follow a 1:1 pattern with PageModels, violating the guideline to avoid single-use services.

**Violations:**

| ViewService | PageModel | Single-Use? |
|-------------|-----------|-------------|
| IWorkspaceInventoryViewService | Inventory.cshtml.cs | ✓ Yes |
| IWorkspaceInventoryEditViewService | InventoryEdit.cshtml.cs | ✓ Yes |
| IWorkspaceReportsViewService | Reports.cshtml.cs | ✓ Yes |
| IWorkspaceReportsEditViewService | ReportsEdit.cshtml.cs | ✓ Yes |
| IWorkspaceTicketsViewService | Tickets.cshtml.cs | ✓ Yes |
| IWorkspaceTicketDetailsViewService | TicketsDetails.cshtml.cs | ✓ Yes |
| IWorkspaceTicketsSaveViewService | TicketsDetails.cshtml.cs | ✓ Yes |
| IWorkspaceUsersViewService | Users.cshtml.cs | ✓ Yes |
| IWorkspaceUsersManageViewService | Users.cshtml.cs / UsersEdit.cshtml.cs | Partial |
| IWorkspaceContactsViewService | Contacts.cshtml.cs | ✓ Yes |
| IWorkspaceContactsEditViewService | ContactsEdit.cshtml.cs | ✓ Yes |
| IWorkspaceLocationsViewService | Locations.cshtml.cs | ✓ Yes |
| IWorkspaceLocationsEditViewService | LocationsEdit.cshtml.cs | ✓ Yes |
| IWorkspaceTeamsViewService | Teams.cshtml.cs | ✓ Yes |
| IWorkspaceTeamsEditViewService | TeamsEdit.cshtml.cs | ✓ Yes |
| IWorkspaceRolesViewService | Roles.cshtml.cs | ✓ Yes |
| IWorkspaceRolesEditViewService | RolesEdit.cshtml.cs | ✓ Yes |
| IWorkspaceRolesAssignViewService | RolesAssign.cshtml.cs | ✓ Yes |

**Total:** 15+ single-use ViewServices

**Pattern Analysis:**
- Each PageModel gets its own dedicated ViewService
- Creates tight coupling between UI and service layer
- Increases service proliferation without clear benefit

**Impact:**
- Service explosion without architectural benefit
- Increased maintenance burden
- Violates guideline explicitly

**Recommendation:**
1. Consolidate related ViewServices (e.g., InventoryViewService + InventoryEditViewService → InventoryViewService)
2. Consider if ViewServices are necessary or if application services could be used directly
3. Evaluate each ViewService for potential elimination or merging

---

### 7. Test Naming Convention Violations

**Severity:** MEDIUM  
**Affected Projects:** Tickflo.CoreTest  
**Rule Violated:** Testing Guidelines - Naming convention (lines 270-282)

**Description:**
Approximately 85% of test methods do not follow the documented naming convention: `MethodName_WhenCondition_ShouldExpectedOutcome`

**Expected Convention:**
```
MethodName_WhenCondition_ShouldExpectedOutcome
```

**Current Patterns Found:**

#### A. UserManagementServiceTests.cs
```csharp
// ❌ Incorrect naming
CreateUserAsyncThrowsOnDuplicateEmail
CreateUserAsyncNormalizesEmail
UpdateUserAsyncThrowsWhenUserNotFound
IsEmailInUseAsyncReturnsFalseWhenNotFound

// ✅ Correct naming should be:
CreateUserAsync_WhenEmailIsDuplicate_ShouldThrowInvalidOperationException
CreateUserAsync_WhenEmailHasMixedCase_ShouldNormalizeToLowercase
UpdateUserAsync_WhenUserNotFound_ShouldThrowInvalidOperationException
IsEmailInUseAsync_WhenEmailNotFound_ShouldReturnFalse
```

#### B. TicketManagementServiceTests.cs
```csharp
// ❌ Incorrect naming
ValidateUserAssignmentAsyncReturnsTrueWhenMember
CanUserAccessTicketAsyncAllowsAdmin

// ✅ Correct naming should be:
ValidateUserAssignmentAsync_WhenUserIsMember_ShouldReturnTrue
CanUserAccessTicketAsync_WhenUserIsAdmin_ShouldReturnTrue
```

#### C. AuthenticationServiceTests.cs
```csharp
// ❌ Incorrect naming
AuthenticateAsyncValidCredentialsReturnsSuccess
AuthenticateAsyncUserNotFoundReturnsError
AuthenticateAsyncInvalidPasswordReturnsError

// ✅ Correct naming should be:
AuthenticateAsync_WhenCredentialsAreValid_ShouldReturnSuccess
AuthenticateAsync_WhenUserNotFound_ShouldReturnError
AuthenticateAsync_WhenPasswordIsInvalid_ShouldReturnError
```

**Common Anti-Patterns:**
1. Missing "When" separator between method and condition
2. Using "Returns" or "Throws" instead of "Should"
3. Combining multiple clauses without clear separation

**Affected Files:** All 60+ test files in Tickflo.CoreTest

**Impact:**
- Tests are less readable
- Intent is not immediately clear
- Inconsistent with documented standards

**Recommendation:**
1. Refactor all test names to follow `MethodName_WhenCondition_ShouldExpectedOutcome` pattern
2. Use clear "When" separators
3. Use "Should" prefix for expected outcomes
4. Update test review process to enforce convention

---

### 8. Anemic Domain Models

**Severity:** MEDIUM-HIGH  
**Affected Projects:** Tickflo.Core  
**Rule Violated:** Architectural Principles - Domain-Driven Design (lines 97-120)

**Description:**
All entity classes in Tickflo.Core/Entities are anemic domain models - pure data containers with no behavior. This violates DDD principles that state: "Encapsulates invariants inside entities or aggregates."

**Rules Violated:**
- "Centers around the domain, not the database or UI"
- "Encapsulates invariants inside entities or aggregates"
- "Avoid: Anemic domain models"

**Examples:**

#### Ticket Entity (Tickflo.Core/Entities/Ticket.cs)
```csharp
public class Ticket : IWorkspaceEntity
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // ... only properties, no behavior
}
```

**Missing Behavior Examples:**
- No `AssignTo(User user)` method
- No `Close()` method
- No `UpdateStatus(TicketStatus status)` method
- No validation of invariants (e.g., subject required)

#### User Entity (Tickflo.Core/Entities/User.cs)
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    // ... only properties
}
```

**Missing Behavior Examples:**
- No `ConfirmEmail(string code)` method
- No `UpdatePassword(string newPassword)` method
- Email normalization in services instead of entity

#### Workspace Entity (Tickflo.Core/Entities/Workspace.cs)
```csharp
public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    // ... only properties
}
```

**Missing Behavior Examples:**
- No `RenameWorkspace(string newName)` method
- No slug generation logic
- No audit tracking behavior

**Impact:**
- Business logic scattered across services instead of encapsulated in domain
- Entities can be put into invalid states
- Reduces expressiveness of domain model
- Violates DDD best practices

**Recommendation:**
1. Add behavior methods to entities that encapsulate business rules
2. Make setters private where appropriate, expose through methods
3. Validate invariants in entity constructors and methods
4. Consider creating factory methods for complex entity creation
5. Move entity-specific logic from services back to entities

**Note:** This is a significant refactoring that should be done incrementally and carefully tested.

---

## Recommended Issues to Create

Based on the violations found, here are the recommended GitHub issues to create:

### High Priority

1. **[Refactoring] Replace `ws` abbreviation with `workspace` throughout codebase**
   - Labels: `refactoring`, `code-quality`, `good-first-issue`
   - Affected: 30+ files across Core and Web

2. **[Architecture] Remove repository injections from PageModels**
   - Labels: `architecture`, `breaking-change`, `high-priority`
   - Affected: 18 PageModels

3. **[Refactoring] Extract business logic from Settings.cshtml.cs to application service**
   - Labels: `refactoring`, `technical-debt`, `high-priority`
   - Critical: 760 lines of business logic in UI

4. **[Refactoring] Extract business logic from Tickets.cshtml.cs to application services**
   - Labels: `refactoring`, `technical-debt`
   - Affected: Filtering, pagination, assignment logic

### Medium Priority

5. **[Refactoring] Rename utility classes to remove Helper/Util suffixes**
   - Labels: `refactoring`, `code-quality`
   - Affected: ImageHelper, TicketHistoryFormatter, TokenGenerator

6. **[Testing] Update test naming convention to follow MethodName_WhenCondition_ShouldExpectedOutcome**
   - Labels: `testing`, `code-quality`
   - Affected: 60+ test files (~85% of tests)

7. **[Architecture] Consolidate or eliminate single-use ViewServices**
   - Labels: `architecture`, `refactoring`
   - Affected: 15+ ViewServices

8. **[Refactoring] Fix variable naming: db → dbContext, runSvc → reportRunService**
   - Labels: `refactoring`, `code-quality`, `good-first-issue`
   - Affected: ScheduledReportsHostedService.cs

### Lower Priority (Long-term)

9. **[Architecture] Add behavior to domain entities (address anemic domain model)**
   - Labels: `architecture`, `ddd`, `enhancement`, `long-term`
   - Affected: All entity classes

10. **[Refactoring] Extract business logic from Users.cshtml.cs**
    - Labels: `refactoring`, `technical-debt`

11. **[Refactoring] Extract business logic from InventoryEdit.cshtml.cs**
    - Labels: `refactoring`, `technical-debt`

---

## Summary Statistics

| Category | Violations | Severity |
|----------|-----------|----------|
| Variable naming (`ws`) | 30+ files | HIGH |
| Variable naming (other) | 2 instances | MEDIUM |
| Utility class naming | 3 files | MEDIUM |
| Repository leaking | 18 PageModels | CRITICAL |
| Business logic in UI | 5+ PageModels | CRITICAL |
| Single-use services | 15+ services | MEDIUM |
| Test naming | 60+ files (~85%) | MEDIUM |
| Anemic domain models | All entities | MEDIUM-HIGH |

**Total Estimated Effort:** 
- High priority fixes: 3-4 weeks
- Medium priority fixes: 2-3 weeks  
- Long-term improvements: Ongoing

---

## Compliance Status

### Projects Compliance Score

| Project | Compliant Areas | Non-Compliant Areas | Score |
|---------|----------------|---------------------|-------|
| Tickflo.Core | Service naming, DI, async/await | Variable naming, Utils folder, anemic entities | 60% |
| Tickflo.Web | Service injection, Razor structure | Repository leaking, business logic in UI, naming | 40% |
| Tickflo.CoreTest | Test structure, mocking | Test naming conventions | 50% |

**Overall Compliance:** ~50%

---

## Next Steps

1. **Review this report** with the development team
2. **Prioritize issues** based on business impact and effort
3. **Create GitHub issues** using the recommended list above
4. **Create a refactoring roadmap** with milestones
5. **Establish code review checklist** to prevent future violations
6. **Update CI/CD** to enforce naming conventions where possible (linters, analyzers)

---

**Report Generated:** January 22, 2026  
**Tool:** GitHub Copilot Code Review Agent  
**Reference:** `.github/copilot-instructions.md`
