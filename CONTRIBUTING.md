# Contributing to Tickflo

Thank you for your interest in contributing to Tickflo! This document provides guidelines and instructions for contributing to the project.

## 🚀 Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
   ```bash
   git clone https://github.com/YOUR-USERNAME/tickflo-dotnet.git
   cd tickflo-dotnet
   ```
3. **Set up the development environment** following the [README](README.md)
4. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

## 📋 Development Workflow

### 1. Code Changes

- Write clean, readable code following C# conventions
- Follow the existing project structure and patterns
- Keep changes focused and atomic
- Update or add tests as needed

### 2. Testing

Before submitting your changes:

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Verify no errors or warnings
```

### 3. Code Style

- Use meaningful variable and method names
- Follow the repository pattern for data access
- Place business logic in service classes
- Use dependency injection for dependencies
- Add XML documentation comments for public APIs

**Example:**
```csharp
/// <summary>
/// Retrieves a ticket by its unique identifier.
/// </summary>
/// <param name="ticketId">The ticket ID to search for.</param>
/// <returns>The ticket if found, null otherwise.</returns>
public async Task<Ticket?> GetTicketByIdAsync(int ticketId)
{
    return await _ticketRepository.GetByIdAsync(ticketId);
}
```

### 4. UI Components

When working on UI:

- Use DaisyUI components for consistency
- Follow the [DaisyUI Style Guide](docs/guides/DAISYUI_QUICK_REFERENCE.md)
- Ensure responsive design (mobile-first)
- Test across different screen sizes
- Maintain accessibility standards (ARIA labels, keyboard navigation)

### 5. Database Changes

If your change requires database modifications:

1. Create a new migration:
   ```bash
   dbmate new descriptive_migration_name
   ```

2. Write both `up` and `down` migrations
3. Test migration forward and rollback:
   ```bash
   dbmate up
   dbmate down
   dbmate up
   ```

4. Update seed data if needed

### 6. Documentation

Update documentation for:
- New features or APIs
- Configuration changes
- Breaking changes
- Setup instructions

Documentation locations:
- `README.md` - Main project documentation
- `docs/` - Detailed guides and references
- Code comments - Public APIs and complex logic

## 🔍 Pull Request Process

### Before Submitting

- [ ] Code builds without errors or warnings
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated if needed
- [ ] Commit messages are clear and descriptive
- [ ] Branch is up to date with main

### Commit Messages

Use clear, descriptive commit messages:

```
Add user avatar upload functionality

- Implement file upload service
- Add avatar cropping UI
- Update user profile page
- Add unit tests for upload service
```

Format: `<type>: <subject>`

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks

### Submitting

1. **Push your branch** to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub
   - Use a clear title describing the change
   - Reference any related issues
   - Provide context and motivation
   - Include screenshots for UI changes
   - List any breaking changes

3. **Address feedback** from code review
   - Be responsive to comments
   - Make requested changes
   - Push updates to the same branch

## 🐛 Reporting Bugs

### Before Reporting

- Check if the issue already exists
- Verify it's reproducible
- Test with the latest code

### Bug Report Template

```markdown
**Description**
Clear description of the bug

**Steps to Reproduce**
1. Go to '...'
2. Click on '...'
3. See error

**Expected Behavior**
What should happen

**Actual Behavior**
What actually happens

**Environment**
- OS: [e.g., Windows 11]
- .NET Version: [e.g., 10.0.2]
- Browser: [if applicable]

**Screenshots**
If applicable, add screenshots

**Additional Context**
Any other relevant information
```

## 💡 Feature Requests

We welcome feature suggestions! Please:

1. Check if the feature is already requested
2. Clearly describe the feature and its use case
3. Explain why it would be valuable
4. Provide examples if possible

## 🧪 Testing Guidelines

### Unit Tests

- Test business logic in services
- Use descriptive test names
- Follow Arrange-Act-Assert pattern
- Mock dependencies appropriately

Example:
```csharp
[Fact]
public async Task CreateTicket_ValidData_ReturnsNewTicket()
{
    // Arrange
    var service = new TicketService(_mockRepository.Object);
    var newTicket = new Ticket { Title = "Test", Description = "Test" };

    // Act
    var result = await service.CreateTicketAsync(newTicket);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Title);
}
```

### Integration Tests

- Test repository operations against test database
- Verify database constraints
- Test transaction handling

## 🏗️ Architecture Guidelines

### Service Layer

- Keep services focused (Single Responsibility)
- Services should not depend on web/UI concerns
- Use dependency injection
- Return appropriate types (not raw entities for public APIs)

### Repository Pattern

- Repositories handle data access only
- No business logic in repositories
- Use async methods consistently
- Return `IQueryable` for complex queries when appropriate

### Page Models

- Keep page models thin
- Delegate to services for business logic
- Handle only web concerns (validation, redirects, etc.)

## 📝 License

By contributing to Tickflo, you agree that your contributions will be licensed under the same license as the project (see [LICENSE.txt](LICENSE.txt)).

## 🤝 Code of Conduct

### Our Standards

- Be respectful and inclusive
- Welcome newcomers
- Provide constructive feedback
- Focus on what's best for the community
- Show empathy towards others

### Unacceptable Behavior

- Harassment or discriminatory language
- Personal attacks
- Spam or trolling
- Publishing private information
- Other unprofessional conduct

## 💬 Communication

- **GitHub Issues**: Bug reports and feature requests
- **Pull Requests**: Code contributions and discussions
- **Code Reviews**: Constructive feedback on PRs

## 🙏 Recognition

Contributors will be recognized in:
- Git commit history
- Release notes for significant contributions
- Project documentation (where appropriate)

Thank you for contributing to Tickflo! 🎉

---

**Questions?** Open an issue or reach out to the maintainers.
