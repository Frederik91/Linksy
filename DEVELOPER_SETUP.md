# Developer Setup Guide

This guide will help you set up the Linksy development environment on your local machine.

## Prerequisites

Before you begin, ensure you have the following installed:

### Required

- **.NET 9 SDK** or later
  - [Download from microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)
  - Verify: `dotnet --version` (must show 9.x.x)
  - Includes .NET Aspire 9.5.1 support

- **Node.js 22 LTS** or later
  - [Download from nodejs.org](https://nodejs.org/)
  - Verify: `node --version` (must show v22.x.x or higher)

- **Docker Desktop** (for PostgreSQL)
  - [Download from docker.com](https://www.docker.com/products/docker-desktop/)
  - Required for running PostgreSQL in containers via Aspire

### Optional but Recommended

- **Visual Studio 2022** (17.8+) or **Visual Studio Code**
  - Install the C# Dev Kit extension for VS Code
  - Install the .NET Aspire workload:
    ```bash
    dotnet workload install aspire
    ```

- **Entity Framework Core tools**
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Quick Start (5 minutes)

### 1. Clone the Repository

```bash
git clone https://github.com/Frederik91/Linksy.git
cd Linksy
```

### 2. Run the Setup Script

This will verify prerequisites and install dependencies:

```bash
# macOS/Linux
./scripts/setup.sh

# Windows PowerShell
.\scripts\setup.ps1
```

### 3. Start the Application

```bash
cd src
dotnet run --project Linksy.AppHost
```

That's it! The application will:
1. ✅ Start PostgreSQL in a Docker container
2. ✅ Automatically apply database migrations
3. ✅ Start the .NET API backend
4. ✅ Start the React frontend
5. ✅ Open the Aspire Dashboard

### 4. Access the Application

Once started, you can access:

- **Frontend**: http://localhost:5173
- **API**: http://localhost:5000/api/info
- **Aspire Dashboard**: http://localhost:15217 (check logs and health)
- **OpenAPI/Swagger**: http://localhost:5000/openapi/v1.json

> **Note**: Wait ~10 seconds for all services to initialize. Check the Aspire Dashboard for status.

## Architecture Overview

The solution uses .NET Aspire for orchestration with the following projects:

```
src/
├── Linksy.AppHost/          # Aspire orchestrator (starts everything)
├── Linksy.ServiceDefaults/  # Shared Aspire configurations
├── Linksy.Migrations/       # Database migration service (runs first)
├── Linksy.Api/              # .NET 9 Web API backend
├── Linksy.Api.Tests/        # xUnit tests
└── frontend/                # React 19 + TypeScript + Vite
```

### Startup Order

When you run `dotnet run --project Linksy.AppHost`, Aspire orchestrates:

1. **PostgreSQL** container starts
2. **Migrations service** runs and applies schema
3. **API** starts after migrations complete
4. **Frontend** starts after API is ready

This ensures the database is always ready before the application starts!

## Development Workflow

### Backend Development

#### Run the Full Stack
```bash
cd src
dotnet run --project Linksy.AppHost
```

#### Run API in Isolation (for debugging)
```bash
# Requires PostgreSQL running separately
cd src/Linksy.Api
dotnet run
```

#### Run Tests
```bash
cd src
dotnet test
```

### Frontend Development

#### Run with Aspire (Recommended)
```bash
cd src
dotnet run --project Linksy.AppHost
```

#### Run Frontend in Isolation
```bash
cd src/frontend
npm run dev
```

Frontend changes are reflected instantly via Vite HMR.

## Database Migrations

### Automatic Migrations (Default Behavior)

Migrations are **automatically applied** when you start the application via Aspire. The `Linksy.Migrations` service runs first and ensures the database schema is up-to-date.

### Adding a New Migration

When you modify entity models in `Linksy.Api/Models/`, create a new migration:

```bash
# From repository root
./scripts/add-migration.sh MyNewMigration

# Windows
.\scripts\add-migration.ps1 MyNewMigration
```

Or manually:

```bash
cd src/Linksy.Api
dotnet ef migrations add MyNewMigration \
    --context ApplicationDbContext \
    --output-dir Data/Migrations
```

The new migration will be automatically applied next time you start the app.

### Resetting the Database

To start fresh:

```bash
# Stop the application
# Delete the PostgreSQL volume
docker volume rm linksy-postgres-data

# Restart - migrations will recreate everything
cd src
dotnet run --project Linksy.AppHost
```

## Troubleshooting

### Port Already in Use

If you see "port already in use" errors:

```bash
# Find and kill processes on common ports
lsof -ti:5000,5173,15217 | xargs kill -9  # macOS/Linux
netstat -ano | findstr :5000             # Windows
```

### Docker Desktop Not Running

```
Error: Cannot connect to Docker daemon
```

**Solution**: Start Docker Desktop and wait for it to fully initialize.

### Database Connection Errors

```
Npgsql.NpgsqlException: Connection refused
```

**Solution**:
1. Check Docker is running
2. Verify PostgreSQL container is healthy in Aspire Dashboard
3. Restart the application

### EF Core Tools Not Found

```
bash: dotnet-ef: command not found
```

**Solution**:
```bash
dotnet tool install --global dotnet-ef
```

### Node Version Mismatch

```
Error: Unsupported Node.js version
```

**Solution**:
```bash
# Using nvm (recommended)
nvm install 22
nvm use 22

# Or download Node.js 22 LTS
# https://nodejs.org/
```

## IDE Setup

### Visual Studio 2022

1. Open `src/Linksy.sln`
2. Set `Linksy.AppHost` as the startup project
3. Press F5 to run

### Visual Studio Code

1. Open the repository root folder
2. Install recommended extensions (prompted automatically)
3. Press F5 or use the terminal:
   ```bash
   cd src
   dotnet run --project Linksy.AppHost
   ```

### Rider

1. Open `src/Linksy.sln`
2. Right-click `Linksy.AppHost` → Run
3. Or use the terminal as shown above

## Environment Variables

### Frontend (.env.local)

Create `src/frontend/.env.local`:

```bash
VITE_API_URL=http://localhost:5000
```

This is set automatically by Aspire, but you can override it for custom configurations.

### Backend (User Secrets)

Aspire manages connection strings automatically. For custom configuration:

```bash
cd src/Linksy.AppHost
dotnet user-secrets set "CustomKey" "CustomValue"
```

## Next Steps

- [API Documentation](./docs/api.md) - Explore available endpoints
- [Feature Spec: AEC File Sync](./specs/001-aec-file-sync/spec.md) - Understand the product
- [Contributing Guide](./CONTRIBUTING.md) - Make contributions

## Getting Help

- Check the [Aspire Dashboard](http://localhost:15217) for logs and telemetry
- Review the [Troubleshooting](#troubleshooting) section above
- Open an issue on GitHub with error details

---

**Happy coding! 🚀**
