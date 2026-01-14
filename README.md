# Tickflo

> A modern, multi-tenant ticketing and workspace management system

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18.1-336791?logo=postgresql)](https://www.postgresql.org/)
[![License](https://img.shields.io/github/license/tickflo/tickflo)](LICENSE.txt)

Tickflo is a full-featured help desk and ticketing system designed for teams that need isolated workspaces, flexible permissions, and real-time collaboration.

## ✨ Features

- 🏢 **Multi-tenant workspaces** with complete data isolation
- 🎫 **Ticket management** with priorities, statuses, and assignments
- 👥 **Team collaboration** with real-time updates via SignalR
- 🔐 **Role-based access control** with customizable permissions
- 📧 **Smart notifications** (email + in-app)
- 📎 **File attachments** with S3-compatible storage (RustFS)
- 🎨 **Modern UI** built with Tailwind CSS and DaisyUI
- 📊 **Contact & location tracking** for service management

## 🚀 Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [dbmate](https://github.com/amacneil/dbmate#installation)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd tickflo-dotnet

# Set up environment
cp .env.example .env
cp Tickflo.Web/.env.example Tickflo.Web/.env

# Start services
# The provided compose.yml contains the following services:
# - db (postgresql database)
# - s3 (rustfs service)
# - web (tickflo web app)
#
# You can choose to run all of these:
docker compose up -d

# Or if you prefer to run the app locally:
docker compose up -d db s3

# Run migrations
dbmate up

# (Optional) Load demo data
docker exec -i $(docker ps -qf name=db) psql -U $POSTGRES_USER -d $POSTGRES_DB -f /work/db/seed_data.sql

# Run the app
cd Tickflo.Web
dotnet run
```

Open [https://localhost:5001](https://localhost:5001) in your browser.

## 🏗️ Architecture

```
Tickflo.Web/         # Razor Pages web application
Tickflo.Core/        # Business logic & data access
Tickflo.API/         # REST API
Tickflo.CoreTest/    # Tests
db/                  # Database schema & migrations
```

**Tech Stack:**
- ASP.NET Core 10.0 + Entity Framework Core 9.0
- PostgreSQL 18.1
- RustFS (S3-compatible storage)
- Tailwind CSS + DaisyUI
- SignalR for real-time updates

## 🛠️ Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Database migrations
dbmate new migration_name  # Create
dbmate up                  # Apply
dbmate down                # Rollback
```

## 📖 Documentation

- [Project Structure](docs/PROJECT_STRUCTURE.md) - Navigate the codebase
- [Contributing Guide](CONTRIBUTING.md) - Development workflow
- [RustFS Setup](docs/RUSTFS_QUICKSTART.md) - File storage configuration
- [Notification System](docs/NOTIFICATION_SYSTEM.md) - Email & alerts
- [UI Style Guide](docs/guides/DAISYUI_QUICK_REFERENCE.md) - Component patterns

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the terms in [LICENSE.txt](LICENSE.txt).

## 🙏 Acknowledgments

Built with:
- [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- [PostgreSQL](https://www.postgresql.org/)
- [RustFS](https://github.com/rustfs/rustfs)
- [Tailwind CSS](https://tailwindcss.com/)
- [DaisyUI](https://daisyui.com/)

---

**⭐ Star this repo if you find it helpful!**