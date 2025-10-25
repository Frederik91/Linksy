# Implementation Plan: Project Initialization with .NET Web API, Aspire, React + Vite + shadcn/ui

**Branch**: `002-project-setup` | **Date**: October 25, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-project-setup/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Bootstrap a full-stack development environment for Linksy using .NET 8 Web API orchestrated by Aspire as the central platform, with a React + Vite + shadcn/ui frontend managed through Aspire's `AddNpmApp`. Developers execute `dotnet new aspire`, configure the AppHost to wire frontend and backend, run automated setup scripts to verify prerequisites and install dependencies, and launch both services with unified observability. This feature is purely infrastructure/scaffolding; no domain logic or data persistence is introduced.

## Technical Context

**Language/Version**: .NET 8 (backend), Node.js 22 (frontend React with TypeScript only)  
**Primary Dependencies**: 
  - Backend: ASP.NET Core 8, Aspire (.NET AppHost orchestration)
  - Frontend: React 19+ (TypeScript only, no JavaScript), Vite 5+, TypeScript 5+, shadcn/ui, Tailwind CSS 4+
  - Local orchestration: .NET Aspire (via `dotnet new aspire`)

**Storage**: No persistent storage for this feature; infrastructure only. (PostgreSQL and Azure services configured in future phases)  
**Testing**: 
  - Backend: xUnit or similar .NET testing framework (included in template)
  - Frontend: Vitest, React Testing Library with TypeScript (standard Vite template setup, .test.tsx files)
  - Infrastructure: Manual validation of service startup, health checks, environment variable injection

**Target Platform**: macOS / Linux / Windows (any host with .NET 8 SDK, Node.js 22 LTS)  
**Project Type**: Web (full-stack: backend API + frontend React SPA orchestrated locally via Aspire, TypeScript-first)  
**Performance Goals**: 
  - API startup: <5 seconds (per SC-002)
  - React dev server startup: <5 seconds
  - Code change reflection: <3 seconds (HMR per SC-003)
  - Full setup time: 15 minutes (per SC-001)

**Constraints**: 
  - Fixed ports (API 5000, React 5173); no dynamic fallback if ports occupied
  - Local dev only; production deployment separate (React CDN, API cloud—out of scope)
  - Aspire manages both services; no manual Node/dotnet process spawning

**Scale/Scope**: Single repository, 2 services (API + React frontend), 1 AppHost orchestrator, ~11 functional requirements, ~7 success criteria

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Constitution v1.2.0 Alignment** (Linksy):

| Principle | Status | Rationale |
|-----------|--------|-----------|
| **I. Frictionless Adoption** | ✅ PASS | Setup scripts automate prerequisite checks and dependency installation; 15-minute onboarding goal. Documentation includes troubleshooting and dry-run-like local dev. |
| **II. Deterministic Sync Integrity** | ℹ️ DEFERRED | N/A for this scaffolding feature; sync logic defined in future phases when connector SDK and orchestrator are built. This feature provides the platform foundation. |
| **III. Security & Compliance** | ✅ PASS | Local dev environment isolated; no production credentials. Future phases (connectors, OAuth, Key Vault) build on this scaffold. |
| **IV. Extensible Connector Platform** | ℹ️ DEFERRED | N/A for scaffolding; Connector SDK, contract tests, feature flags designed in Phase 2+. This feature establishes backend/frontend/orchestration baseline. |
| **V. Observable & Ops** | ✅ PASS | Aspire dashboard provides unified logging and metrics for both API and React frontend. Local telemetry baseline ready for agent instrumentation in Phase 2. |

**Frontend Stack Check**: React + TypeScript + shadcn/ui ✅ (per constitution v1.2.0 Delivery Workflow section)  
**Backend Stack Check**: .NET 8 Web API ✅ (per constitution v1.2.0 Delivery Workflow section)  
**Orchestration Check**: .NET Aspire ✅ (per constitution v1.2.0 Delivery Workflow section)  
**Testing Discipline**: xUnit (backend), Vitest/RTL (frontend) ✅ (required by constitution)  
**Developer Experience**: Local Aspire orchestration with shadcn/ui ✅ (per constitution v1.2.0 Delivery Workflow section)

**Gate Outcome**: ✅ **PASS** — All material principles either directly satisfied or properly deferred to Phase 2+ connector/sync logic. No violations or exceptions required.

## Technology & Dependency Versions

### Backend (.NET 8 API)
- **Runtime**: .NET 8 LTS (latest stable)
- **Framework**: ASP.NET Core Minimal API or MVC (per template)
- **Orchestration**: Microsoft.Extensions.ServiceDiscovery, Aspire SDKs
- **Testing**: xUnit (default in template) or NUnit
- **Logging**: Structured logging via ILogger (built-in)

### Frontend (React)
- **React**: 19.x LTS
- **Build Tool**: Vite 5.x
- **Language**: TypeScript 5.x (mandatory, no JavaScript allowed)
- **Component Library**: shadcn/ui (built on Radix UI, Tailwind CSS)
- **Styling**: Tailwind CSS 4.x
- **Testing**: Vitest + React Testing Library with TypeScript (.test.tsx files only)
- **Node.js**: 22 LTS (current stable)

### Local Orchestration (Aspire)
- **.NET Aspire**: 8.1 or later (from Aspire.ProjectTemplates NuGet package)
- **Dashboard**: Built-in Aspire dashboard (http://localhost:18888 or configured port)
- **Service Discovery**: Aspire built-in DNS/service resolution

## Project Structure

### Documentation (this feature)

```text
specs/002-project-setup/
├── spec.md              # Feature specification (completed in /speckit.specify)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command) — research on template versions, best practices
├── data-model.md        # Phase 1 output (/speckit.plan command) — N/A (no domain entities for scaffolding)
├── quickstart.md        # Phase 1 output (/speckit.plan command) — getting started with Aspire + React + API
├── contracts/           # Phase 1 output (/speckit.plan command) — N/A (no API contracts for scaffolding)
├── checklists/
│   └── requirements.md  # Quality validation (completed in /speckit.clarify)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root after initialization)

```text
# Full-stack structure after `dotnet new aspire` + React setup

Linksy/                                  # Root repository
├── Linksy.AppHost/                     # .NET Aspire orchestrator (scaffolded by template)
│   ├── Program.cs                      # AppHost with AddProject("api") + AddNpmApp("frontend")
│   ├── Linksy.AppHost.csproj
│   └── appsettings.json
│
├── Linksy.Api/                         # .NET 8 Web API service (scaffolded by template)
│   ├── Program.cs                      # Minimal API setup, service registration
│   ├── Properties/
│   ├── Linksy.Api.csproj
│   └── appsettings.json
│
├── Linksy.ServiceDefaults/             # Aspire service defaults (scaffolded by template)
│   ├── Extensions.cs
│   └── Linksy.ServiceDefaults.csproj
│
├── src/frontend/                       # React + Vite + TypeScript + shadcn/ui
│   ├── src/
│   │   ├── components/                 # shadcn/ui imported components
│   │   ├── pages/
│   │   ├── App.tsx
│   │   ├── main.tsx
│   │   └── index.css                   # Tailwind + shadcn/ui styles
│   ├── public/
│   ├── package.json                    # React 19+, Vite 5+, shadcn/ui, Tailwind CSS 4+
│   ├── vite.config.ts                  # Vite config with VITE_API_URL env var
│   ├── tsconfig.json
│   └── .env.example                    # VITE_API_URL=http://api:5000
│
├── scripts/
│   ├── setup.sh                        # macOS/Linux setup script
│   ├── setup.ps1                       # Windows setup script
│   └── verify-prereqs.sh               # Check .NET 8 SDK, Node.js 22+
│
├── .gitignore                          # Excludes bin, obj, dist, node_modules, .env
├── README.md                           # Setup instructions, troubleshooting, local dev guide
└── Linksy.sln                          # Visual Studio solution including AppHost, Api, ServiceDefaults

**Structure Decision**: Full-stack orchestrated via Aspire AppHost. The template `dotnet new aspire` creates AppHost, Api, and ServiceDefaults; React frontend added separately and wired into AppHost via `AddNpmApp`. Setup scripts automate environment checks and dependency install.
```

