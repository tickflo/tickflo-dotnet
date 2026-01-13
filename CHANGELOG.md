# Changelog

All notable changes to the Tickflo project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Documentation reorganization and comprehensive README
- Contributing guidelines
- Project structure reference guide

### Changed
- Organized documentation into logical folders (docs/, docs/guides/, docs/archive/)
- Enhanced README with detailed setup instructions and feature list

### Fixed
- N/A

---

## [1.0.0] - 2026-01-12

### Added
- Initial release
- Multi-tenant workspace system
- Ticket management with priorities and statuses
- Role-based access control (RBAC)
- Team management functionality
- Contact and location tracking
- Real-time notifications via SignalR
- RustFS S3-compatible file storage
- Email notification system
- User preference management
- Modern UI with DaisyUI components
- PostgreSQL database with migrations
- REST API for external integrations
- Comprehensive test suite

### Core Features
- **Multi-tenancy**: Isolated workspaces with separate data
- **Permissions**: Flexible role and permission system
- **File Storage**: S3-compatible object storage with image optimization
- **Notifications**: Email and in-app notification system
- **Real-time**: SignalR-powered live updates
- **Modern UI**: Responsive design with Tailwind CSS and DaisyUI

### Technical Stack
- ASP.NET Core 8.0
- Entity Framework Core 9.0
- PostgreSQL 17.2
- RustFS for object storage
- SignalR for real-time communication
- Tailwind CSS + DaisyUI for UI

---

## Version Format

Versions follow Semantic Versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Incompatible API changes
- **MINOR**: New functionality (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Change Categories

- **Added**: New features
- **Changed**: Changes to existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security vulnerability fixes

## Links

- [Documentation](docs/INDEX.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Project Structure](docs/PROJECT_STRUCTURE.md)

---

[Unreleased]: https://github.com/your-org/tickflo-dotnet/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/your-org/tickflo-dotnet/releases/tag/v1.0.0
