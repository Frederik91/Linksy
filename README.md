# Linksy - AEC File Synchronization Platform

A modern, full-stack application for synchronizing Architecture, Engineering, and Construction (AEC) files with a focus on frictionless developer experience and deterministic sync integrity.

## Technology Stack

- **Backend**: .NET 9.0 Web API (ASP.NET Core Minimal API + Aspire 9.5.1)
- **Frontend**: React 19+ with TypeScript, Vite, shadcn/ui
- **Orchestration**: .NET Aspire 9.5.1 (latest stable)
- **Database**: PostgreSQL (Phase 2+)
- **Node.js**: 22 LTS (required)

## Prerequisites

Before you begin, ensure you have the following installed:

### Required

- **.NET 9 SDK** or later (Aspire 9.5.1 requires .NET 9.0+)
  - [Download from microsoft.com](https://dotnet.microsoft.com/download/dotnet/9.0)
  - Verify: `dotnet --version` (must show 9.x.x)

- **Node.js 22 LTS** or later
  - [Download from nodejs.org](https://nodejs.org/)
  - Verify: `node --version`
  - Should output `v22.x.x` or higher

- **npm** (included with Node.js)
  - Verify: `npm --version`

### Optional

- **nvm** (Node Version Manager)
  - Manages Node.js versions automatically via `.nvmrc` file
  - [Installation guide](https://github.com/nvm-sh/nvm)

## Quick Start (5 minutes)

### 1. Clone and Setup

```bash
git clone https://github.com/Frederik91/Linksy.git
cd Linksy

# Run setup script (macOS/Linux)
./scripts/setup.sh

# OR for Windows PowerShell
.\scripts\setup.ps1
```

The setup script will:
- ✅ Verify .NET 9 SDK installation
- ✅ Verify Node.js 22 LTS installation
- ✅ Verify Docker Desktop is running
- ✅ Restore .NET dependencies
- ✅ Install npm packages

### 2. Start the Application

```bash
cd src
dotnet run --project Linksy.AppHost
```

**What happens automatically:**
1. PostgreSQL container starts
2. Database migrations are applied
3. API backend starts
4. React frontend starts
5. Aspire Dashboard opens

### 3. Access the Application

Once started (wait ~10 seconds), access:

- **Frontend**: http://localhost:5173
- **API**: http://localhost:5000/api/info
- **Aspire Dashboard**: http://localhost:15217

> **📖 New to the project?** See [DEVELOPER_SETUP.md](./DEVELOPER_SETUP.md) for detailed setup instructions, troubleshooting, and development workflows.

## Project Structure

```
Linksy/
├── src/
│   ├── Linksy.AppHost/                # 🎯 Aspire orchestrator (START HERE)
│   │   ├── AppHost.cs                 # Service orchestration & startup order
│   │   └── Linksy.AppHost.csproj
│   │
│   ├── Linksy.Migrations/             # 🗄️ Database migration service
│   │   ├── MigrationService.cs        # Automatic schema updates on startup
│   │   └── Program.cs
│   │
│   ├── Linksy.Api/                    # 🌐 .NET 9 Web API backend
│   │   ├── Data/                      # EF Core DbContext & migrations
│   │   ├── DTOs/                      # API data transfer objects
│   │   ├── Endpoints/                 # Minimal API endpoints
│   │   ├── Models/                    # Domain entities
│   │   ├── Program.cs                 # API configuration
│   │   └── Linksy.Api.csproj
│   │
│   ├── Linksy.Api.Tests/              # 🧪 xUnit tests for API
│   │   └── Linksy.Api.Tests.csproj
│   │
│   ├── Linksy.ServiceDefaults/        # ⚙️ Aspire service defaults
│   │   └── Linksy.ServiceDefaults.csproj
│   │
│   ├── frontend/                      # ⚛️ React 19 + TypeScript frontend
│   │   ├── src/
│   │   │   ├── components/            # React components
│   │   │   ├── lib/                   # API client & utilities
│   │   │   ├── pages/                 # Dashboard, Onboarding
│   │   │   ├── types/                 # TypeScript definitions
│   │   │   ├── App.tsx                # Root component with routing
│   │   │   └── main.tsx               # Entry point
│   │   ├── package.json
│   │   ├── vite.config.ts             # Vite config
│   │   └── tailwind.config.ts         # Tailwind CSS config
│   │
│   └── Linksy.sln                     # Visual Studio solution
│
├── scripts/
│   ├── setup.sh                       # Setup script (macOS/Linux)
│   ├── setup.ps1                      # Setup script (Windows)
│   ├── add-migration.sh               # Create EF Core migrations
│   └── add-migration.ps1              # (Windows version)
│
├── specs/                             # Feature specifications
│   └── 001-aec-file-sync/             # AEC File Sync platform spec
│
├── README.md                          # This file
├── DEVELOPER_SETUP.md                 # Detailed developer guide
└── .gitignore
```

### Startup Flow

When you run `dotnet run --project Linksy.AppHost`:

```
┌─────────────────────────────────────────────┐
│         Aspire AppHost (Orchestrator)       │
└─────────────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        ▼                         ▼
┌──────────────┐         ┌──────────────────┐
│  PostgreSQL  │         │   Dependencies   │
│  Container   │         │    (NPM, etc)    │
└──────┬───────┘         └──────────────────┘
       │
       ▼
┌──────────────────┐
│   Migrations     │  ◄─── Runs first, applies schema
│     Service      │       Exits after completion
└────────┬─────────┘
         │
         ▼
    ┌────────────────┐
    │   .NET API     │  ◄─── Waits for migrations
    │   Backend      │       Serves REST endpoints
    └────────┬───────┘
             │
             ▼
        ┌────────────────┐
        │ React Frontend │  ◄─── Waits for API
        │   (Vite HMR)   │       Auto-refreshes on change
        └────────────────┘
```

## Development Workflow

### Backend Development

1. **Edit** files in `Linksy.Api/`
2. The API will **automatically recompile** when running under Aspire
3. **Check logs** in Aspire Dashboard

Example: Adding a new endpoint

```csharp
// Linksy.Api/Program.cs
app.MapGet("/api/users", () => new[] { "User 1", "User 2" })
    .WithName("GetUsers")
    .WithOpenApi();
```

### Frontend Development

1. **Edit** files in `src/frontend/src/`
2. Vite's **Hot Module Replacement (HMR)** instantly reflects changes
3. **Browser auto-refreshes** within 3 seconds

Example: Creating a new component

```bash
# src/frontend/src/components/MyComponent.tsx
export default function MyComponent() {
  return <div>Hello from MyComponent</div>
}
```

## Useful Commands

### Backend

```bash
# Build the API
dotnet build Linksy.Api/Linksy.Api.csproj

# Run API tests
dotnet test Linksy.Api.Tests/Linksy.Api.Tests.csproj

# Start API in isolation
dotnet run --project Linksy.Api
```

### Frontend

```bash
# Install new npm package
npm install package-name --prefix ./src/frontend

# Run frontend in isolation
npm run dev --prefix ./src/frontend

# Build frontend for production
npm run build --prefix ./src/frontend

# Run frontend tests
npm run test --prefix ./src/frontend

# Lint and format
npm run lint --prefix ./src/frontend
npm run format:check --prefix ./src/frontend
```

### Aspire Orchestration

```bash
# Start full stack (recommended)
dotnet run --project Linksy.AppHost

# Access Aspire Dashboard
# http://localhost:15217
```

## Troubleshooting

### Port Already in Use

If you get "port already in use" errors:

**Option A**: Free the port
```bash
# macOS/Linux - find process on port 5000
lsof -i :5000
kill -9 <PID>

# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

**Option B**: Change port in configuration
- **API**: Edit `Linksy.AppHost/AppHost.cs` and modify port binding
- **React**: Edit `src/frontend/vite.config.ts` and change `server.port`

### Node.js Version Mismatch

```bash
# Check current Node version
node --version

# If using nvm, switch to correct version
nvm use 22

# Or download Node.js 22 LTS directly
# https://nodejs.org/
```

### .NET SDK Not Found

```bash
# Verify .NET is installed
dotnet --version

# Install .NET 8 SDK
# https://dotnet.microsoft.com/download/dotnet/8.0
```

### React Dev Server Won't Start

```bash
# Clean node_modules and reinstall
rm -rf src/frontend/node_modules src/frontend/package-lock.json
npm install --prefix ./src/frontend
npm run dev --prefix ./src/frontend
```

### API Connection Errors from React

- Check Aspire Dashboard: http://localhost:15217
- Verify API is running (should show health status ✅)
- Check browser console for CORS or network errors
- Verify `VITE_API_URL` environment variable is set correctly

## Architecture Overview

```
┌──────────────────────────────────────────┐
│         Aspire AppHost (Port 15217)      │
│      (Unified Dashboard & Logs)          │
├──────────────────────────────────────────┤
│                                          │
│  ┌─────────────────┐  ┌──────────────┐  │
│  │ .NET 8 API      │  │ React App    │  │
│  │ :5000           │◄─┤ :5173        │  │
│  │                 │  │              │  │
│  │ - REST API      │  │ - TypeScript │  │
│  │ - Health Checks │  │ - Vite HMR   │  │
│  │ - Logging       │  │ - shadcn/ui  │  │
│  └─────────────────┘  └──────────────┘  │
│                                          │
└──────────────────────────────────────────┘
```

## Best Practices

### Frontend (TypeScript-Only)

- ✅ All source files must be `.tsx` or `.ts`
- ✅ No `.js` or `.jsx` files allowed
- ✅ Use strict TypeScript mode (`"strict": true`)
- ✅ Prefer typed components over untyped

### Backend (.NET 8)

- ✅ Use Minimal API pattern for lightweight endpoints
- ✅ Structured logging via `ILogger`
- ✅ Health checks for observability
- ✅ CORS configured for localhost development

### General

- ✅ Commit setup scripts to version control
- ✅ Document environment variables in `.env.example`
- ✅ Use Aspire Dashboard for debugging
- ✅ Keep `.gitignore` up-to-date

## Environment Variables

### Frontend

Create `src/frontend/.env.local`:

```bash
VITE_API_URL=http://localhost:5000
```

See `src/frontend/.env.example` for template.

### Backend

Configure in `Linksy.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes and commit: `git commit -am 'Add feature'`
3. Push to the branch: `git push origin feature/my-feature`
4. Submit a pull request

## Support

For issues or questions:

1. Check the **Troubleshooting** section above
2. Review the **Aspire Dashboard** logs
3. Open a GitHub issue with error details

## License

[MIT License](LICENSE) - See LICENSE file for details

---

**Developed with** 🚀 **by the Linksy Team**

Last Updated: October 25, 2025
