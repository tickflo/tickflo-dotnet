# GitHub Copilot Instructions — Tickflo .NET Monorepo

These instructions guide GitHub Copilot when generating, refactoring, or completing code in the **Tickflo** .NET monorepo. The goal is to produce **clean, readable, performant, and maintainable code** aligned with **modern .NET practices** and **Domain-Driven Design (DDD)** principles.

---

## Repository Structure & Responsibilities

The repository is a **.NET monorepo** composed of multiple projects with clear boundaries:

### `Tickflo.Core`

**Purpose:** Core business logic, domain model, and persistence.

Includes:

* Domain entities and value objects
* Aggregates and domain services
* Entity Framework Core DbContext and mappings
* Application services / use cases
* Business rules and invariants

**Rules:**

* No dependency on web, API, or UI frameworks
* EF Core is allowed and considered part of Core
* Persistence concerns must not leak into UI or Razor Pages
* No static state
* No "Manager"-suffixed services

---

### `Tickflo.CoreTest`

**Purpose:** Automated tests for `Tickflo.Core`.

Includes:

* Unit tests
* Domain behavior tests
* Application service tests

**Rules:**

* Test behavior, not implementation details
* Prefer clear Arrange / Act / Assert structure
* Use expressive test names describing business behavior

---

### `Tickflo.API`

**Purpose:** (Planned) REST API layer.

**Current status:**

* Not actively used
* May be incomplete or experimental

**Guidance for Copilot:**

* Do not introduce new dependencies from Core or Web onto API
* Avoid expanding API unless explicitly requested
* When used in the future, it should act as a thin HTTP layer delegating to Core application services

Includes (when active):

* Controllers / minimal APIs
* Request / response DTOs
* Validation
* Authentication & authorization

---

### `Tickflo.Web`

**Purpose:** ASP.NET Razor-based SaaS web application.

Includes:

* Razor Pages (.cshtml + .cshtml.cs)
* View models
* UI-specific orchestration

**Rules:**

* PageModels should be thin
* No business logic in UI
* Avoid creating services used by a single PageModel
* Prefer direct use of application services from Core

---

## Architectural Principles

### Domain-Driven Design (DDD)

Copilot should generate code that:

* Centers around the **domain**, not the database or UI
* Uses **ubiquitous language** from the business domain
* Respects aggregate boundaries
* Encapsulates invariants inside entities or aggregates

**Preferred constructs:**

* Entities
* Value Objects (immutable)
* Aggregates
* Domain Services (only when logic does not belong to a single entity)
* Application Services / Use Cases

**Avoid:**

* Anemic domain models
* "God" services
* CRUD-only services without business meaning

---

## Service Design Guidelines

### Naming

* Use **intent-revealing names**
* Avoid suffixes like:

  * `Manager`
  * `Helper`
  * `Util`
* Prefer:

  * `OrderProcessor`
  * `BillingService`
  * `SubscriptionActivator`

---

### Application Services

* Represent **use cases**, not technical actions
* One public method per use case when possible
* Stateless
* Orchestrate domain entities and repositories

Example intent:

> "Activate a subscription"
> "Calculate invoice totals"

---

### Domain Services

* Used only when logic does not naturally fit in an entity
* Pure and side-effect free when possible
* Operate on domain types, not DTOs

---

## Naming Conventions

Naming is critical for readability and maintainability. Copilot must generate names that are **explicit, consistent, and type-aligned**.

### General Rules

* Names must clearly reflect their **type and responsibility**
* Avoid abbreviations and shortened forms
* Prefer longer, descriptive names over brevity
* No acronyms unless they are widely understood and meaningful in context (e.g. `Id`, `URL`, `S3`)

### Variables & Fields

* Variable names should match the type they reference
* Do **not** pluralize singular types
* Do **not** use vague or shortened names

**Correct:**

```csharp
IWorkspaceRepository workspaceRepository;
UserSubscription activeSubscription;
DateTimeOffset expirationDate;
```

**Incorrect:**

```csharp
IWorkspaceRepository workspaces;
IWorkspaceRepository repo;
IWorkspaceRepository workspaceRepo;
var ws;
```

### Interfaces

* Prefix interfaces with `I`
* Interface names should describe a capability or role

Examples:

* `IWorkspaceRepository`
* `ISubscriptionBillingService`

### Methods

* Use verbs that describe intent
* Avoid generic names like `Handle`, `Process`, `DoWork`

Prefer:

* `ActivateSubscription`
* `CalculateInvoiceTotals`
* `ValidateWorkspaceLimits`

### Classes

* Class names should be nouns
* Service names should reflect business intent, not technical function

Avoid:

* `Manager`
* `Helper`
* `Utils`

---

## Coding Style & Conventions

### General

* Target latest stable .NET and C# versions
* Prefer explicitness over cleverness
* Optimize for **readability first**, performance second
* Avoid premature optimization

---

### C# Style

* Use file-scoped namespaces
* Use `record` for immutable data structures
* Prefer `readonly` where applicable
* Prefer expression-bodied members for simple logic
* Use `async` / `await` end-to-end
* Avoid blocking calls (`.Result`, `.Wait()`)

---

### Nullability

* Enable nullable reference types
* Do not suppress warnings unless justified
* Validate inputs at boundaries (API/UI)

---

### Performance Guidelines

* Avoid unnecessary allocations
* Avoid LINQ in hot paths when clarity suffers
* Prefer streaming over materializing large collections
* Be explicit about async boundaries

---

## Schema changes

* **NEVER** change existing migration files that have been merged (assume anything on `dev` is merged/immutable).
* **Always** create new migrations for schema changes (do not "fix" old migrations).
* Migrations must follow **dbmate migration format**.
* It's okay to edit migrations **only** if they were created on the current branch and have not been merged.

---

## Hard coded string literals

* Avoid hard coded string literals for domain/system values (e.g. `DefaultTicketType = "Standard"`, `string DefaultPriority = "Normal";`).
* Prefer **enumerations** for fixed system values.
* Prefer **reference tables** for workspace-configured values (e.g. `ticket_types`, `ticket_statuses`) and store references (ids/keys), not display strings.

---

## Testing Guidelines

Copilot should generate tests that:

* Are deterministic
* Do not rely on infrastructure unless explicitly integration tests
* Clearly express business intent

**Naming convention:**

```
MethodName_WhenCondition_ShouldExpectedOutcome
```

---

## API Design Guidelines

* Follow REST conventions
* Use appropriate HTTP status codes
* Validate inputs using model validation
* Do not expose domain entities directly

---

## Razor & UI Guidelines

* PageModels should orchestrate, not decide
* Avoid creating "View" services used by a single `.cshtml.cs`
* Prefer:

  * Simple view models
  * Direct calls to application services

---

## Dependency Injection

* Constructor injection only
* Avoid service locators
* Keep dependency graphs shallow

---

## What Copilot Should Avoid Generating

* Manager-pattern services
* UI-specific services used by only one PageModel
* Business logic in controllers or Razor PageModels
* Large static helper classes
* Overly generic abstractions
* Hard-coded domain/system strings where an enum or reference table is appropriate
* Editing merged migrations instead of creating a new dbmate-format migration

---

## Build & Quality Gates

* All changes must compile without errors or warnings
* `dotnet test` must pass successfully before a task is considered complete
* run `dotnet format` on changed files to ensure consistent formatting
* Do not introduce new warnings; treat warnings as failures unless explicitly justified
* Generated code should assume CI enforcement of these rules

---

## Overall Goal

Copilot should assist in building a **clean, expressive, domain-focused SaaS platform** where:

* The domain model is the source of truth
* Code is easy to read, test, and evolve
* Each project has a clear responsibility

When in doubt, prefer **clarity, simplicity, and domain intent** over abstraction.
