# Tickflo

> A modern, multi-tenant ticketing and workspace management system

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
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

- [.NET SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [dbmate](https://github.com/amacneil/dbmate#installation)

### Installation

```bash
# Clone the repository
git clone https://github.com/tickflo/tickflo.git
cd tickflo

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

# Run the app
dotnet run --project Tickflo.Web
```

Open [https://localhost:5262](https://localhost:5262) in your browser.

## 🏗️ Architecture

```
Tickflo.Web/         # Razor Pages web application
Tickflo.Core/        # Business logic & data access
Tickflo.API/         # REST API
Tickflo.CoreTest/    # Tests
db/                  # Database schema & migrations
```

**Tech Stack:**
- ASP.NET Core + Entity Framework Core
- PostgreSQL
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

## 🤝 Contributing

Contributions are welcome!

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
