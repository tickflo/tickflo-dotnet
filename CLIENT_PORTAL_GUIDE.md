# Client Portal Feature Guide

## Overview

The Client Portal is a public-facing feature that allows clients to:
- View only their own tickets
- Create new tickets
- Maintain a secure connection with locked contact association

## How It Works

### 1. **Access Token Security**
- Each contact is automatically assigned a unique, cryptographically secure access token when created
- The token is 32 characters long using alphanumeric characters and special characters (`-`, `_`)
- Tokens are stored in the database and used as the sole authentication method

### 2. **Portal URL Structure**
```
https://tickflo.local/portal/{access_token}
```

Replace `{access_token}` with the actual token assigned to the contact.

### 3. **Ticket Association Lock**
- Tickets created through the client portal are **automatically** locked to the contact who created them
- Clients cannot modify the contact association of their tickets
- Clients can only view and create tickets (no editing/deletion permissions)

## Database Schema

### New Column
- **Contacts.AccessToken** - Nullable string, unique index for fast lookups

### Migration
```sql
ALTER TABLE "Contacts" ADD COLUMN "AccessToken" varchar NULL;
CREATE INDEX "idx_contacts_accesstoken" ON "Contacts"("AccessToken") WHERE "AccessToken" IS NOT NULL;
```

## Implementation Details

### Services

#### IAccessTokenService
Generates cryptographically secure random tokens.

```csharp
var token = _tokenService.GenerateToken(); // 32 chars
var token = _tokenService.GenerateToken(64); // Custom length
```

### Repository Methods

#### IContactRepository.FindByAccessTokenAsync
Finds a contact by their access token.

```csharp
var contact = await _contactRepo.FindByAccessTokenAsync(token);
```

### Page Model

#### ClientPortalModel (`/Pages/ClientPortal.cshtml.cs`)
- Handles GET requests to display tickets
- Handles POST requests to create new tickets
- Validates token and enforces contact association

### Key Features

1. **GET /portal/{token}**
   - Validates access token
   - Loads contact and workspace information
   - Lists all tickets associated with the contact
   - Displays ticket metadata (status, priority, created date)

2. **POST /portal/{token}**
   - Validates access token
   - Creates new ticket with:
     - Subject (required)
     - Description (required)
     - Priority (optional)
     - Type (optional)
     - ContactId locked to the authenticated contact
     - Status defaulted to "New"

## Using the Client Portal

### For Workspace Admins

1. **Create a Contact**
   - Navigate to `/workspaces/{slug}/contacts`
   - Click "New Contact"
   - Fill in contact information
   - **Important**: An access token is automatically generated

2. **Share Portal Link**
   - The portal URL is: `https://tickflo.local/portal/{token}`
   - Send this link to the client via email
   - The token is found in the contact's `AccessToken` field

3. **View Contact Token** (optional)
   - Access the database or add a contacts list view that displays tokens
   - Securely share the portal link

### For Clients

1. **Access Portal**
   - Click the portal link received from support
   - View all your tickets
   - See ticket status, priority, and creation date

2. **Create New Ticket**
   - Fill in the "Create Ticket" form
   - Required: Subject and Description
   - Optional: Priority and Type
   - Click "Submit Ticket"
   - Your ticket is automatically associated with your contact

3. **Track Ticket Status**
   - Tickets appear in chronological order
   - View current status and priority
   - Monitor ticket updates

## Security Considerations

### Authentication
- Token-based access (no login required for contacts)
- Tokens are 32 characters of high entropy
- Tokens are unique per contact
- No password mechanism (token acts as authentication)

### Authorization
- Contacts can only view their own tickets
- Contacts cannot edit their ticket's contact association
- Contact data is isolated per workspace

### Best Practices

1. **Secure Token Distribution**
   - Use HTTPS only
   - Send via email to contact's verified email address
   - Avoid sharing in plain text on insecure channels

2. **Token Rotation** (future enhancement)
   - Consider implementing token rotation functionality
   - Allow admins to revoke/regenerate tokens if compromised

3. **Rate Limiting** (recommended)
   - Implement rate limiting on portal endpoints
   - Prevent abuse/brute force attempts

## Testing the Feature

### Local Testing

1. **Apply Migration**
   ```sql
   -- Run the migration in your database
   ALTER TABLE "Contacts" ADD COLUMN "AccessToken" varchar NULL;
   ```

2. **Create Test Contact**
   - Use the web UI to create a contact
   - Note the generated AccessToken

3. **Access Portal**
   - Navigate to `/portal/{token}`
   - Verify you see the contact's details

4. **Create Test Ticket**
   - Fill in the form
   - Submit
   - Verify ticket appears in the list
   - Verify it's associated with the contact

## API Endpoints

### Client Portal
- **GET** `/portal/{token}` - Display portal and ticket list
- **POST** `/portal/{token}` - Create new ticket

## Future Enhancements

1. **Email Verification**
   - Send portal link via email to contact
   - Require email verification before first access

2. **Token Management**
   - Admin ability to regenerate tokens
   - View token generation/last used dates
   - Token expiration (optional)

3. **Ticket Details View**
   - Allow clients to view full ticket details
   - Show ticket history/comments

4. **Notifications**
   - Email clients when ticket status changes
   - Notification preferences for clients

5. **File Attachments**
   - Allow clients to upload files with tickets
   - View ticket-related files

6. **Search & Filtering**
   - Search tickets by subject/description
   - Filter by status, priority, creation date
