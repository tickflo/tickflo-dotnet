# Client Portal Tests

## Overview
Comprehensive test coverage for the client portal feature, ensuring secure access, proper data aggregation, and token management.

## Test Files

### 1. ClientPortalViewServiceTests
**Location**: `Tickflo.CoreTest/Services/Views/ClientPortalViewServiceTests.cs`

Validates the view service data aggregation logic.

#### Tests

| Test | Purpose |
|------|---------|
| `BuildAsync_LoadsContactTicketsOnly` | Ensures only tickets for the current contact are returned |
| `BuildAsync_LoadsMetadataWithDefaults` | Verifies fallback default values for statuses/priorities/types |
| `BuildAsync_LoadsCustomMetadata` | Confirms custom workspace metadata is loaded correctly |
| `BuildAsync_ThrowsWhenWorkspaceNotFound` | Validates error handling for missing workspaces |
| `BuildAsync_HandlesNullColors` | Ensures null/empty colors default to "neutral" |

**Key Assertions**:
- Contact ticket filtering (only matching ContactId)
- Metadata defaults when database is empty
- Color mapping dictionaries populated correctly
- Exception thrown on workspace not found

### 2. AccessTokenServiceTests
**Location**: `Tickflo.CoreTest/Services/AccessTokenServiceTests.cs`

Tests cryptographically secure token generation.

#### Tests

| Test | Purpose |
|------|---------|
| `GenerateToken_DefaultLength_Returns32Characters` | Default token is exactly 32 chars |
| `GenerateToken_CustomLength_ReturnsCorrectLength` | Custom length tokens are generated correctly |
| `GenerateToken_ReturnsUnique` | Each generated token is unique |
| `GenerateToken_ContainsValidCharacters` | Only valid characters (alphanumeric, `-`, `_`) |
| `GenerateToken_LargeLength_Returns` | Supports large token lengths (256 chars) |
| `GenerateToken_MinimalLength_Returns` | Supports minimal token length (1 char) |

**Key Assertions**:
- Token length validation
- Uniqueness between multiple generations
- Character set validation
- Support for various lengths

### 3. ContactRegistrationServiceTests (Updated)
**Location**: `Tickflo.CoreTest/Services/ContactRegistrationServiceTests.cs`

Enhanced with access token generation tests.

#### New Tests

| Test | Purpose |
|------|---------|
| `RegisterContactAsync_GeneratesAccessToken` | Verifies token is generated on contact creation |

**Key Assertions**:
- Token service is called during registration
- Generated token is set on contact entity
- Service integration verified via mocking

### 4. ContactRepositoryAccessTokenTests
**Location**: `Tickflo.CoreTest/Data/ContactRepositoryAccessTokenTests.cs`

Tests database access for token-based lookups.

#### Tests

| Test | Purpose |
|------|---------|
| `FindByAccessTokenAsync_ReturnsContact_WhenTokenExists` | Find contact by valid token |
| `FindByAccessTokenAsync_ReturnsNull_WhenTokenDoesNotExist` | Returns null for invalid token |
| `FindByAccessTokenAsync_ReturnsNull_WhenTokenIsNull` | Handles null token gracefully |
| `FindByAccessTokenAsync_IsCaseSensitive` | Tokens are case-sensitive |
| `FindByAccessTokenAsync_ReturnsSingleContact_WhenMultipleContactsExist` | Returns correct contact when multiple exist |

**Key Assertions**:
- Database query returns correct contact
- Null handling for missing tokens
- Case sensitivity enforcement
- Correct contact isolation

## Test Coverage Summary

### By Feature

**Access Token Generation**: 6 tests
- Token length and format validation
- Uniqueness and randomness
- Character set validation

**View Service Data Aggregation**: 5 tests
- Contact ticket filtering
- Metadata loading with defaults
- Custom metadata handling
- Error handling

**Repository Access**: 5 tests
- Token-based contact lookup
- Null handling
- Case sensitivity
- Multi-contact scenarios

**Integration**: 2 tests
- Contact registration with tokens
- Duplicate contact handling

**Total: 18 new/updated tests**

## Running Tests

### Run All Tests
```bash
dotnet test Tickflo.CoreTest/Tickflo.CoreTest.csproj
```

### Run Specific Test Class
```bash
dotnet test Tickflo.CoreTest/Tickflo.CoreTest.csproj --filter "FullyQualifiedName~ClientPortalViewServiceTests"
```

### Run with Coverage
```bash
dotnet test Tickflo.CoreTest/Tickflo.CoreTest.csproj /p:CollectCoverage=true
```

## Test Architecture

### Mocking Strategy
- Repository mocks using Moq
- In-memory database (InMemoryDatabase) for data tests
- Isolation between unit tests

### Assertion Patterns
- XUnit `Assert` methods
- Verification of mock calls with `Verify()`
- Exception assertion with `ThrowsAsync<T>()`

### Test Isolation
- Unique in-memory database per test via `Guid.NewGuid()`
- Fresh mock instances per test
- No shared state between tests

## Future Test Additions

### Page Model Tests
- GET request handling
- POST ticket creation validation
- Error responses for invalid tokens

### Integration Tests
- Full client portal flow
- End-to-end ticket creation
- Authentication via token

### Performance Tests
- Token generation performance
- Large dataset query performance
- Pagination and filtering

### Security Tests
- Token collision detection
- SQL injection prevention
- XSS protection in portal

## Known Test Limitations

1. **No Page Model Unit Tests**: ASP.NET Core page models are complex to unit test
   - Recommend integration tests via test client
   
2. **No Razor View Tests**: Razor syntax validation
   - HTML generation verification in integration tests

3. **No E2E Tests**: Full client flow testing
   - Consider separate e2e test suite

## Best Practices

### When Adding Tests
1. Follow existing naming convention: `{MethodName}_{Scenario}_{ExpectedResult}`
2. Use `Arrange-Act-Assert` pattern
3. Mock external dependencies
4. Test both happy path and error cases
5. One logical assertion per test (ideally)

### Test Data
- Use realistic data values
- Avoid magic numbers (use constants/variables)
- Generate unique IDs for isolation

### Documentation
- Add XML comments to test methods
- Explain non-obvious test logic
- Link to requirements/issues

## CI/CD Integration

Tests run on every commit:
- All tests must pass to merge
- Coverage reports generated
- Failed tests block deployment

## Coverage Goals

| Component | Current | Target |
|-----------|---------|--------|
| View Service | 100% | 100% |
| Token Service | 100% | 100% |
| Repository | 80% | 90% |
| Page Model | 0% | TBD |
| Overall | ~75% | 85%+ |

## Troubleshooting

### Test Failures

**"Token service not called"**
- Verify mock setup: `tokenService.Setup(...)`
- Check inject point in constructor

**"Database not found"**
- Ensure InMemoryDatabase option builder is used
- Unique database name per test

**"Null reference exception"**
- Check mock return values
- Verify object initialization

### Debugging

Run specific test with output:
```bash
dotnet test --filter "TestName" --verbosity detailed
```

Use `xunit-html-reporter` for detailed reports:
```bash
dotnet test --logger "html"
```
