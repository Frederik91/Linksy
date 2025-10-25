# Research & Decisions: Project Initialization with .NET Web API, Aspire, React + Vite + shadcn/ui

**Date**: October 25, 2025  
**Phase**: 0 (Research & Clarification consolidation)  
**Status**: Complete — all unknowns resolved in /speckit.clarify workflow

## Overview

This document consolidates findings from the clarification workflow and research best practices. No outstanding NEEDS CLARIFICATION markers remain in the specification.

---

## Decision 1: Aspire Template Selection

**Topic**: Which Aspire template to use for initial project scaffolding?

**Decision**: `dotnet new aspire` (Aspire Empty App template)

**Rationale**: 
- The Aspire Empty App template (`dotnet new aspire`) provides the minimal, clean scaffold: AppHost, .NET API service, and ServiceDefaults.
- Avoids extraneous frameworks (e.g., Blazor) that would require removal.
- Provides built-in service discovery and orchestration plumbing.
- Template is maintained by Microsoft and updated with each .NET LTS release.
- Aligns with constitution v1.2.0 requirement for "Local development MUST run via .NET Aspire orchestrating… the backend API, and the React frontend."

**Alternatives Considered**:
- ❌ ASP.NET Core project template alone: Would lack Aspire orchestration, requiring manual AppHost creation.
- ❌ Aspire Starter (starter-app): Includes Blazor UI which must be removed; Empty App is cleaner.
- ❌ Manual Aspire setup: Higher friction; template is proven and tested.

**Command**: `dotnet new aspire --output ./`

---

## Decision 2: React + Aspire Integration

**Topic**: How should React integrate with the Aspire orchestration host?

**Decision**: React runs as a Node.js process via `AddNpmApp` in the Aspire AppHost. Aspire injects environment variables (e.g., `VITE_API_URL`) to discover the .NET API endpoint.

**Rationale**:
- React is not a .NET service; it runs as a separate Node process.
- Aspire's `AddNpmApp` decorator manages the React dev server lifecycle (start/stop with the AppHost).
- Environment variable injection (`WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))`) is the standard pattern for service-to-service communication in Aspire.
- Vite reads environment variables from `.env` or build config; `VITE_API_URL` is a convention.
- Both services appear unified in the Aspire dashboard with shared telemetry and logging.

**Alternatives Considered**:
- ❌ Docker Compose for local dev: Out of scope per constitution (Aspire is the local orchestration platform).
- ❌ Manual React dev server: Loses Aspire integration, unified logging, and service discovery.
- ❌ React as a .NET-hosted app (SSR): Contradicts spec requirement for independent React build and production CDN deployment.

**Implementation Pattern**:
```csharp
var builder = DistributedApplication.CreateBuilder(args);
var api = builder.AddProject<Projects.Linksy_Api>("api");
builder.AddNpmApp("frontend", "../frontend")
    .WithReference(api)
    .WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))
    .WaitFor(api);
builder.Build().Run();
```

---

## Decision 3: Port Configuration Strategy

**Topic**: Should ports be dynamically assigned or fixed?

**Decision**: Fixed ports for local development. Defaults: API on port 5000, React dev server on port 5173.

**Rationale**:
- Fixed ports reduce friction for developers: predictable, easy to remember, documented in README.
- No need for dynamic port fallback in local dev; if a port is occupied, developer manually frees it or edits config.
- Vite standard convention: port 5173 for dev server.
- .NET convention: port 5000 for HTTPS-capable APIs.
- Simplifies setup scripts and environment variable defaults.

**Alternatives Considered**:
- ❌ Dynamic port assignment: Adds complexity to setup; ports must be published from Aspire dashboard or env vars. Increases friction.
- ❌ Configuration file per developer: Introduces drift and merge conflicts.

**Documentation**: README includes instructions for reconfiguring ports if needed (edit vite.config.ts, AppHost port assignment).

---

## Decision 4: Setup & Initialization Process

**Topic**: How should developers initialize the project after cloning?

**Decision**: Automated setup scripts (setup.sh for macOS/Linux, setup.ps1 for Windows) that verify prerequisites, restore dependencies, and display launch instructions.

**Rationale**:
- Automation reduces human error and ensures consistency across team.
- Prerequisite checks (.NET 8 SDK, Node.js 18+) fail early with clear error messages.
- Scripts call `dotnet restore` and `npm install` to prepare dependencies.
- Scripts display final "launch" instructions (e.g., `dotnet run --project Linksy.AppHost`).
- Aligns with SC-001 goal: 15-minute setup time for new developers.

**Script Behavior**:
```bash
# setup.sh (macOS/Linux)
1. Check for .NET 8 SDK → fail with install link if missing
2. Check for Node.js 18+ → fail with install link if missing
3. Run: dotnet restore
4. Run: npm install --prefix ./src/frontend
5. Print: "Setup complete! Launch with: dotnet run --project Linksy.AppHost"
```

**Alternatives Considered**:
- ❌ Manual README-driven setup: Higher friction, no automated checks; error-prone.
- ❌ Docker setup: Out of scope per constitution (local Aspire dev is primary).
- ❌ Makefile targets: Less portable across Windows/macOS/Linux.

---

## Decision 5: Production Deployment Scope

**Topic**: How should the application be deployed to production?

**Decision**: React and .NET API are deployed separately. React builds to static files (dist folder) deployed to CDN or static hosting; .NET API deployed independently to cloud/container platform. Production deployment strategy is deferred to a future infrastructure/deployment specification.

**Rationale**:
- This feature focuses on local development orchestration, not production architecture.
- Separate deployment aligns with constitution v1.2.0 phase 1 scope, which covers local dev and connector platform foundation.
- Production deployment (CI/CD, containerization, scaling) is a separate concern and will be addressed in a future feature.
- Local Aspire setup remains the baseline; production diverges.

**Alternatives Considered**:
- ❌ Integrated static serving (React from .NET wwwroot): Would tie frontend and backend releases; contradicts microservice principles.
- ❌ Docker Compose for production: Out of scope for Phase 1; Azure App Service or Container Apps to be defined in infra feature.

**Noted for Future**: A separate feature `/speckit.specify` will address production deployment, CI/CD, and containerization.

---

## Technology & Dependency Versions

### Backend (.NET 8 API)
- **Runtime**: .NET 8 LTS (latest stable)
- **Framework**: ASP.NET Core Minimal API or MVC (per template)
- **Orchestration**: Microsoft.Extensions.ServiceDiscovery, Aspire SDKs
- **Testing**: xUnit (default in template) or NUnit
- **Logging**: Structured logging via ILogger (built-in)

### Frontend (React)
- **React**: 18.x LTS
- **Build Tool**: Vite 5.x
- **Language**: TypeScript
- **Component Library**: shadcn/ui (built on Radix UI, Tailwind CSS)
- **Styling**: Tailwind CSS 4.x
- **Testing**: Vitest + React Testing Library (standard Vite+React stack)
- **Node.js**: 18+ LTS

### Local Orchestration (Aspire)
- **.NET Aspire**: 8.1 or later (from Aspire.ProjectTemplates NuGet package)
- **Dashboard**: Built-in Aspire dashboard (http://localhost:18888 or configured port)
- **Service Discovery**: Aspire built-in DNS/service resolution

---

## Environment Variables & Configuration

### Development
- **VITE_API_URL**: Injected by Aspire AppHost; points to API service endpoint (e.g., `http://api:5000`)
- **.env.example**: Checked into repo; shows expected format
- **appSettings.json**: Backend configuration; no sensitive data in repo

### Prerequisites Verification
- `.NET 8 SDK` installed (verify: `dotnet --version`)
- `Node.js 18+` installed (verify: `node --version`)
- `npm` or `yarn` available (verify: `npm --version`)

---

## Testing Strategy

### Unit Tests
- **Backend**: xUnit tests in `Linksy.Api.Tests/` project
- **Frontend**: Vitest tests in `src/frontend/tests/` or co-located `*.test.tsx` files

### Integration Tests
- **Backend**: Test API endpoints in isolation (HTTP requests to localhost:5000)
- **Frontend**: React component rendering with mocked API responses
- **E2E (out of scope for Phase 0)**: Playwright/Cypress tests verifying full flow (deferred to Phase 2)

### Infrastructure Validation (Manual)
1. Run `dotnet run --project Linksy.AppHost`
2. Verify both API and React dev server start within 5 seconds
3. Access http://localhost:5173 in browser; page loads and can communicate with API at http://localhost:5000
4. Access Aspire dashboard at http://localhost:18888; both services visible with health ✅
5. Edit React component; HMR reflects change within 3 seconds
6. Edit API endpoint; restart reflects change

---

## Best Practices Applied

### Backend (.NET)
- ✅ Minimal API pattern for clean, modern code
- ✅ Structured logging with ILogger
- ✅ Service discovery via Aspire
- ✅ OpenAPI/Swagger for API documentation
- ✅ Health checks endpoint (`/health`)

### Frontend (React)
- ✅ TypeScript for type safety
- ✅ Vite for fast dev server and optimized build
- ✅ shadcn/ui for accessible, maintainable components
- ✅ Tailwind CSS for utility-first styling
- ✅ Environment variable injection via Vite's `import.meta.env`
- ✅ HMR enabled by default in Vite dev

### Project Structure
- ✅ Monorepo-friendly layout (backend, frontend, scripts co-located)
- ✅ Clear separation of concerns (API, UI, orchestration)
- ✅ Gitignore excludes build artifacts, node_modules, sensitive files

---

## Outstanding Items for Later Phases

| Item | Phase | Reason |
|------|-------|--------|
| Production deployment (CI/CD, containerization) | 2+ | Out of scope for local dev setup |
| Connector SDK & contract tests | 2+ | Connector platform foundation; this feature is infra only |
| Sync orchestrator & job queue | 2+ | Domain logic; deferred after platform scaffold complete |
| Database schema, migrations | 2+ | No persistence in this feature; future phases will define |
| End-to-end tests, Playwright | 2+ | Integration testing; deferred after core services stable |
| Observability dashboards (Application Insights) | 2+ | Telemetry baseline in place; dashboards added in Phase 2 |

---

## Conclusion

All clarifications from the `/speckit.clarify` workflow are consolidated above with rationale and alternatives. No remaining unknowns block Phase 1 (Design & Contracts). The feature is ready for:

1. **Phase 1a**: Generate data-model.md (N/A for scaffolding), contracts/ (N/A for scaffolding), quickstart.md
2. **Phase 1b**: Update agent context via `/speckit/scripts/bash/update-agent-context.sh`
3. **Phase 2**: Generate tasks.md and begin implementation
