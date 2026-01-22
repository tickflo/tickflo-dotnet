# Email Template System - Version 1.0 Implementation

## Overview
This document describes the implementation of versioned, immutable email templates for the Tickflo application. The email template system has been redesigned to support versioning, making templates global and immutable.

## Key Changes

### 1. Database Schema Changes

#### Migration 20260116120000_email_template_versioning.sql
- **Added** `version` column (integer, NOT NULL, default 1)
- **Removed** `workspace_id` column (templates are now global)
- **Dropped** old unique constraint on `(workspace_id, template_type_id)`
- **Added** new unique constraint on `(template_type_id, version)`

Templates are now uniquely identified by their template type and version number.

### 2. Default Email Templates

#### Migration 20260116120100_insert_default_email_templates.sql
Added 9 default email templates (all at version 1):

| Template Type ID | Name | Purpose |
|-----------------|------|---------|
| 1 | Email Confirmation Thank You | Page content shown after email confirmation |
| 2 | Workspace Invite - New User | Email sent when inviting new users |
| 3 | Email Confirmation Request | Email sent to request email confirmation |
| 4 | Workspace Invite Resend | Email sent when resending workspace invitation |
| 5 | Signup Welcome | Email sent after user signup |
| 6 | Forgot Password | Email sent for password reset requests |
| 7 | Confirm New Email | Email sent to new address during email change |
| 8 | Revert Email Change | Email sent to old address with revert option |
| 9 | Workspace Member Removal | Email sent when user is removed from workspace |

### 3. Entity Changes

#### EmailTemplate.cs
- **Added** `Version` property (int)
- **Removed** `WorkspaceId` property (int?)

Templates are now global (not workspace-specific) and versioned.

### 4. Repository Changes

#### EmailTemplateRepository.cs
Key behavioral changes:

- **FindByTypeAsync**: Now returns the template with the **highest version** for a given template type
  - The `workspaceId` parameter is maintained for interface compatibility but is no longer used
  
- **CreateAsync**: Automatically calculates and assigns the next version number
  - When creating a template, the system finds the latest version and increments it
  
- **UpdateAsync**: Now creates a new version instead of modifying existing templates
  - Templates are immutable - updates create new versions with incremented version numbers
  - The old version remains in the database for audit/history purposes
  
- **ListAsync**: Returns only the latest version of each template type
  - Groups by template type and returns the highest version

### 5. Service Layer

#### EmailTemplateService.cs
- No changes needed - already calls `FindByTypeAsync` which now automatically returns the newest version

### 6. Constants

#### EmailTemplateType.cs (New File)
Created a static class with constants for all template type IDs:
- `EmailConfirmationThankYou = 1`
- `WorkspaceInviteNewUser = 2`
- `EmailConfirmationRequest = 3`
- `WorkspaceInviteResend = 4`
- `SignupWelcome = 5`
- `ForgotPassword = 6`
- `ConfirmNewEmail = 7`
- `RevertEmailChange = 8`
- `WorkspaceMemberRemoval = 9`

### 7. Updated Code References

Updated files to use `EmailTemplateType` constants instead of magic numbers:
- `Tickflo.Web/Pages/EmailConfirmationThankYou.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/Users.cshtml.cs`
- `Tickflo.Web/Pages/Workspaces/UsersInvite.cshtml.cs`
- `Tickflo.Web/Controllers/EmailConfirmationController.cs`
- `Tickflo.CoreTest/Services/EmailTemplateServiceTests.cs`

### 8. Seed Data Changes

#### seed_data.sql
- Removed email template insert statements
- Added comment referencing the migration file for default templates

## How It Works

### Template Versioning
1. Each template type can have multiple versions
2. When code requests a template by type, it automatically gets the newest version
3. Templates are immutable - modifications create new versions
4. Old versions remain in the database for audit purposes

### Creating New Template Versions
```csharp
// Get the current template
var template = await _emailTemplateRepo.FindByTypeAsync(EmailTemplateType.SignupWelcome);

// Modify it
template.Subject = "New Subject";
template.Body = "New Body";

// Update creates a new version (original remains unchanged)
var newVersion = await _emailTemplateRepo.UpdateAsync(template);
// This creates version 2 while version 1 remains in the database
```

### Using Templates in Code
```csharp
// Always gets the newest version automatically
var variables = new Dictionary<string, string>
{
    { "USER_NAME", "John Doe" },
    { "CONFIRMATION_LINK", "https://..." }
};

var (subject, body) = await emailTemplateService.RenderTemplateAsync(
    EmailTemplateType.EmailConfirmationRequest, 
    variables
);
```

## Migration Path

### To Apply These Changes:
1. Run migration `20260116120000_email_template_versioning.sql` (schema changes)
2. Run migration `20260116120100_insert_default_email_templates.sql` (default templates)
3. Deploy updated application code

### Data Migration Notes:
- Existing templates will be assigned version 1 automatically
- If workspace-specific templates existed, they will need manual review (the workspace_id column is dropped)

## Benefits

1. **Immutability**: Templates cannot be accidentally modified, preserving history
2. **Versioning**: Track changes over time, ability to rollback if needed
3. **Simplified Logic**: No workspace-specific template logic needed
4. **Type Safety**: Use constants instead of magic numbers
5. **Audit Trail**: All template versions are preserved

## Future Enhancements

Potential improvements for future versions:
- Admin UI to view all template versions
- Ability to rollback to a previous version
- Template preview functionality
- Template variable documentation
- Multi-language support with versioning per language
