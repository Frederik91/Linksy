# Tasks: Project Initialization with .NET Web API, Aspire, React + Vite + shadcn/ui

**Feature**: 002-project-setup  
**Branch**: `002-project-setup`  
**Date**: October 25, 2025  
**Status**: Ready for Implementation  
**Total Tasks**: 42  
**Estimated Duration**: 5-7 days (full team) | 10-14 days (single developer)

---

## Overview

This document defines all tasks to bootstrap a full-stack development environment for Linksy using:
- **.NET 8 Web API** with ASP.NET Core (backend)
- **React 19+ with TypeScript + Vite + shadcn/ui** (frontend)
- **.NET Aspire** orchestration (local development)

Each task is independently executable and organized by user story, enabling parallel development and incremental testing.

---

## Implementation Strategy

### MVP Scope (Recommended Phase 1 Target)
- **User Story 1**: Project initialization and structure (all tasks in Phase 1)
- **User Story 2**: Backend API setup and health checks (all tasks in Phase 2)
- **User Story 3**: Frontend React setup and component library (all tasks in Phase 3)
- **User Story 4**: Aspire orchestration (selected tasks in Phase 4)
- **User Story 5**: Documentation and onboarding (all tasks in Phase 5)

### Parallel Execution Opportunities
- **Phase 1 (Setup)**: All tasks can run in parallel (independent)
- **Phase 2 (Backend)**: T007-T014 can run in parallel after T005 completes
- **Phase 3 (Frontend)**: T015-T025 can run in parallel after T013 completes
- **Phase 4 (Aspire)**: T026-T032 depend on Phases 2 & 3 completing
- **Phase 5 (Documentation)**: T033-T042 can run in parallel after Phase 4 completes

### Task Dependencies & Completion Order

```
Phase 1 (Setup)
  └─ T001-T006 (all independent) ─ COMPLETE ─→

Phase 2 (Backend API)
  └─ T007-T014 (parallel after T005) ─ COMPLETE ─→

Phase 3 (Frontend React)
  └─ T015-T025 (parallel after T013) ─ COMPLETE ─→

Phase 4 (Aspire Orchestration)
  └─ T026-T032 (sequential: Aspire config) ─ COMPLETE ─→

Phase 5 (Documentation & Polish)
  └─ T033-T042 (parallel: docs independent) ─ COMPLETE ─→

TOTAL: 42 tasks across 5 phases
MVP: Complete Phases 1-3, selected Phase 4 tasks (T026, T027)
```

---

## Phase 1: Project Setup & Scaffolding

**Goal**: Initialize project structure, install templates, prepare for backend and frontend development.

**Independent Test Criteria**:
- `dotnet --version` returns 8.x
- `node --version` returns 22.x
- `npm --version` returns 8.x or later
- Project directory structure created with all folders
- `.gitignore` configured for .NET and Node.js projects
- `README.md` contains setup instructions

### Tasks

- [x] T001 Initialize git repository and commit initial structure
- [x] T002 [P] Create `.gitignore` file excluding .NET (bin, obj), Node.js (node_modules), and environment files (.env)
- [x] T003 [P] Create project root directory structure: `Linksy.AppHost/`, `Linksy.Api/`, `Linksy.ServiceDefaults/`, `frontend/`, `scripts/`, `docs/`
- [x] T004 [P] Install `Aspire.ProjectTemplates` NuGet package via `dotnet new install Aspire.ProjectTemplates`
- [x] T005 Scaffold Aspire project using `dotnet new aspire --output ./` in repository root
- [x] T006 [P] Verify scaffolded structure: Linksy.AppHost, Linksy.Api, Linksy.ServiceDefaults, Linksy.sln exist

---

## Phase 2: Backend API (.NET 8) Setup

**Goal**: Configure .NET 8 Web API with Aspire service registration, health checks, and proper project structure.

**Independent Test Criteria**:
- Backend builds successfully: `dotnet build` returns exit code 0
- API service starts without errors: `dotnet run --project Linksy.Api`
- Health check endpoint responds: `curl http://localhost:5000/health` returns 200 OK
- Aspire service discovery works: API registers with AppHost
- API uses Minimal API pattern with clean structure
- Structured logging configured via ILogger

### Tasks

- [x] T007 [P] [US2] Configure `Linksy.Api.csproj` to target .NET 8 and include Aspire NuGet packages (Microsoft.Extensions.ServiceDiscovery, Aspire.Extensions.ServiceDiscovery)
- [x] T008 [P] [US2] Create `Linksy.Api/Program.cs` with Minimal API setup, dependency injection, and service configuration
- [x] T009 [P] [US2] Implement health check endpoint (`GET /health`) in `Linksy.Api/Program.cs` returning 200 OK with status JSON
- [x] T010 [P] [US2] Add structured logging configuration in `Linksy.Api/Program.cs` using `ILogger` and Aspire logging
- [x] T011 [US2] Create sample API endpoint (e.g., `GET /api/info`) in `Linksy.Api/Program.cs` demonstrating Minimal API pattern
- [x] T012 [P] [US2] Create `appsettings.json` in `Linksy.Api/` with port configuration (5000) and logging settings
- [x] T013 [P] [US2] Add `xUnit` test project `Linksy.Api.Tests/` with sample health check test in `Linksy.Api.Tests/HealthCheckTests.cs`
- [x] T014 [US2] Verify backend builds and runs: `dotnet build` and `dotnet run --project Linksy.Api` complete successfully

---

## Phase 3: Frontend React + Vite Setup

**Goal**: Initialize React 19+ project with TypeScript, Vite, shadcn/ui, and Tailwind CSS; configure environment variables for API communication.

**Independent Test Criteria**:
- Frontend builds successfully: `npm run build --prefix ./frontend` returns exit code 0
- React dev server starts: `npm run dev --prefix ./frontend` launches on port 5173
- TypeScript compilation passes: `npx tsc --noEmit` returns zero errors
- shadcn/ui components importable and renderable
- Tailwind CSS 4+ configured and working
- HMR enabled: code changes reflect within 3 seconds
- No `.js` files in `frontend/src/` (TypeScript-only)
- Environment variables (`VITE_API_URL`) accessible in code

### Tasks

- [x] T015 Create `frontend/` directory structure with `src/`, `public/`, `dist/` (placeholder), `tests/` folders
- [x] T016 Create `frontend/package.json` with React 19+, Vite 5+, TypeScript 5+, shadcn/ui, Tailwind CSS 4+, Vitest, React Testing Library dependencies
- [x] T017 [P] Create `frontend/tsconfig.json` with TypeScript strict mode enabled, React 19 JSX transform configuration
- [x] T018 [P] Create `frontend/vite.config.ts` configuring dev server (port 5173), React plugin, TypeScript support, environment variable handling
- [x] T019 [P] Create `frontend/src/main.tsx` entry point importing React, rendering App component, importing Tailwind CSS
- [x] T020 [P] Create `frontend/src/App.tsx` as root component with TypeScript interface; demonstrates shadcn/ui Button component import
- [x] T021 [P] Create `frontend/src/index.css` with Tailwind CSS 4+ base styles and shadcn/ui component styles
- [x] T022 [P] Create `frontend/.env.example` with `VITE_API_URL=http://localhost:5000` template
- [x] T023 [P] Configure shadcn/ui in `frontend/` using `npx shadcn-ui@latest init` (components.json, tsconfig.paths)
- [x] T024 [P] Create sample shadcn/ui components folder `frontend/src/components/ui/` with Button component imported via shadcn
- [x] T025 [P] Create `frontend/src/components/App.test.tsx` demonstrating Vitest + React Testing Library setup with TypeScript
- [x] T026 [US3] Verify frontend builds and runs: `npm run build --prefix ./frontend` and `npm run dev --prefix ./frontend` complete successfully
- [x] T027 [P] [US3] Verify no JavaScript files in `frontend/src/`: `find frontend/src -name "*.js" -o -name "*.jsx"` returns zero results
- [x] T028 [P] [US3] Verify TypeScript compilation: `npx tsc --noEmit --project frontend/tsconfig.json` returns zero errors

---

## Phase 4: Aspire Orchestration & Local Development

**Goal**: Configure Aspire AppHost to orchestrate both .NET API and React frontend services with unified dashboard, service discovery, environment variable injection.

**Independent Test Criteria**:
- Aspire AppHost starts: `dotnet run --project Linksy.AppHost` launches without errors
- Aspire Dashboard accessible: http://localhost:18888 responds
- Both services visible in dashboard with health checks passing
- React receives `VITE_API_URL` environment variable
- API and React logs unified in dashboard
- Full stack starts within 5 seconds
- HMR works: code changes to React reflect within 3 seconds

### Tasks

- [x] T029 [US4] Configure `Linksy.AppHost/Program.cs` to register .NET API via `AddProject<Projects.Linksy_Api>("api")`
- [x] T030 [US4] Configure `Linksy.AppHost/Program.cs` to register React frontend via `AddNpmApp("frontend", "../frontend")`
- [x] T031 [US4] Add service reference from React to API: React `WithReference(api)` and inject environment variable `WithEnvironment("VITE_API_URL", api.GetEndpoint("http"))`
- [x] T032 [P] [US4] Add startup ordering: React waits for API via `.WaitFor(api)` ensuring API health check passes before React starts
- [x] T033 Verify Aspire orchestration: `dotnet run --project Linksy.AppHost` starts both services with Aspire Dashboard accessible at http://localhost:18888

---

## Phase 5: Setup Scripts, Documentation & Polish

**Goal**: Create automated setup scripts for macOS/Linux/Windows, comprehensive README, troubleshooting guide, and validate all success criteria.

**Independent Test Criteria**:
- `./scripts/setup.sh` runs on macOS/Linux CI agent, exits with status 0, prints "Setup complete"
- `./scripts/setup.ps1` runs on Windows CI agent, exits with status 0, prints "Setup complete"
- Both scripts verify .NET 8 SDK installed (fail with status 1 if missing)
- Both scripts verify Node.js 22 installed (fail with status 1 if missing)
- README.md includes all 5 user stories, prerequisites, setup steps, troubleshooting
- All 7 success criteria (SC-001 through SC-007) validated
- All 11 functional requirements (FR-001 through FR-011) implemented and tested

### Tasks

- [x] T034 [P] Create `scripts/setup.sh` (macOS/Linux) checking for .NET 8 SDK, Node.js 22+, running `dotnet restore` and `npm install --prefix ./frontend`
- [x] T035 [P] Create `scripts/setup.ps1` (Windows PowerShell) with same checks and commands as setup.sh, adapted for PowerShell syntax
- [x] T036 [P] Create `scripts/verify-prereqs.sh` standalone prerequisite verification script for CI/automated testing
- [x] T037 Create `README.md` with project overview, architecture diagram, prerequisites, setup instructions, troubleshooting (top 5 common issues), useful commands
- [x] T038 [P] Add prerequisites section to README.md: .NET 8 SDK, Node.js 22 LTS (enforce in setup script)
- [x] T039 [P] Add setup instructions to README.md covering both macOS/Linux (`./scripts/setup.sh`) and Windows (`./scripts/setup.ps1`)
- [x] T040 [P] Add troubleshooting section to README.md: port conflicts, missing prerequisites, HMR issues, API connection failures
- [x] T041 [P] Add useful commands section to README.md: starting Aspire, viewing logs, rebuilding, production build commands
- [x] T042 Create `docs/.env.example` template in repository root documenting required environment variables for React (`VITE_API_URL=http://localhost:5000`)

---

## Phase 6: Quality Validation & Testing

**Goal**: Validate all success criteria, constitution checks, and prepare for feature completion.

**Independent Test Criteria**:
- All 7 success criteria (SC-001 through SC-007) tested and passing
- All 11 functional requirements (FR-001 through FR-011) implemented
- All 5 user stories (US1-US5) independently testable
- Constitution v1.2.0 principles verified (I-V)
- TypeScript strict mode enabled, zero compilation errors
- No JavaScript files in `frontend/src/`
- Node.js 22 LTS requirement enforced in setup scripts and package.json
- Backend builds and tests pass
- Frontend builds and tests pass
- Aspire orchestration verified

### Tasks

- [x] T043 [P] Validate SC-001: New developer can clone and run setup script, complete in under 15 minutes
- [x] T044 [P] Validate SC-002: API health check responds within 5 seconds; React dev server launches within 5 seconds
- [x] T045 [P] Validate SC-003: Edit React component, verify HMR reflects change within 3 seconds
- [x] T046 [P] Validate SC-004: Aspire Dashboard at http://localhost:15217 shows both services with health ✅
- [x] T047 [P] Validate SC-005: Project structure follows .NET 9 and React + Vite best practices
- [x] T048 [P] Validate SC-006: README includes prerequisites, setup, troubleshooting enabling new developer success
- [x] T049 [P] Validate SC-007: .gitignore configured; `git status` shows no build artifacts or node_modules
- [x] T050 [P] Validate FR-001 through FR-011: All functional requirements implemented and tested
- [x] T051 [P] Validate Constitution Principle I (Frictionless Dev): Setup scripts work end-to-end
- [x] T052 [P] Validate Constitution Principle III (Security): CORS localhost-only, .env excluded from git
- [x] T053 [P] Validate Constitution Principle V (Observable): Aspire Dashboard unified logs working
- [x] T054 [P] Validate TypeScript compilation: `npx tsc --noEmit` returns zero errors
- [x] T055 [P] Validate Node.js 22 enforcement: `setup.sh` accepts Node.js 22+; package.json specifies `"engines": {"node": "^22.0.0"}`
- [x] T056 [P] Validate no JavaScript in frontend: `find frontend/src -name "*.js" -o -name "*.jsx"` returns zero
- [x] T057 Run backend tests: `dotnet test Linksy.Api.Tests` passes all health check tests ✅
- [x] T058 Run frontend tests: `npm run test --prefix ./frontend` passes initial Vitest setup tests ✅
- [x] T059 Manual smoke test: Full stack integration (Aspire starts, both services running, React communicates with API)

---

## Phase 7: Pull Request & Merge

**Goal**: Create pull request, pass all checks, merge to `develop` branch, and prepare for Phase 2.

**Tasks**:

- [x] T060 Create pull request from `002-project-setup` to `develop` with title "002: Project Initialization with .NET 9 Aspire, React 19 + Vite 5 + shadcn/ui"
- [x] T061 [P] Enable auto-complete on PR for automated merge when all checks pass
- [x] T062 Update `.github/copilot-instructions.md` with tech stack: .NET 9, Node.js 22 LTS, React 19+, Vite 5+, shadcn/ui, Tailwind CSS 4+, Aspire 9.0.0 ✅
- [ ] T063 Merge PR to `develop` branch upon approval

---

## Summary

| Phase | Title | Task Count | Duration | Key Dependencies |
|-------|-------|-----------|----------|-----------------|
| 1 | Project Setup & Scaffolding | 6 | 30-45 min | None |
| 2 | Backend API (.NET 8) | 8 | 1-2 hrs | Phase 1 ✅ |
| 3 | Frontend React + Vite | 14 | 2-3 hrs | Phase 1 ✅ |
| 4 | Aspire Orchestration | 5 | 1-2 hrs | Phase 2 ✅, Phase 3 ✅ |
| 5 | Setup Scripts & Docs | 9 | 1-2 hrs | All phases |
| 6 | Quality Validation | 17 | 1-2 hrs | All phases |
| 7 | PR & Merge | 4 | 30 min | All phases ✅ |

**Total**: 63 tasks | **Estimated Duration**: 5-7 days (full team parallel) | **MVP Duration**: 3-4 days (Phases 1-3)

---

## Parallel Execution Examples

### Team of 3 (Recommended for MVP)

```
Developer 1: Phase 1 (Setup) → Phase 2 (Backend API)
Developer 2: Phase 1 (Setup) → Phase 3 (Frontend React)
Developer 3: Documentation & QA (Phase 5 after Phase 1)

Timeline:
- Day 1: Phase 1 (parallel T001-T006) + Phase 2 starts
- Day 2-3: Phase 2 (T007-T014) and Phase 3 (T015-T028) in parallel
- Day 4: Phase 4 (Aspire) + Phase 5 (Docs) in parallel
- Day 5: Phase 6 (QA) + Phase 7 (PR/Merge)
```

### Solo Developer

```
Day 1: Phase 1 (Setup) - ~45 min
Day 2: Phase 2 (Backend) - ~2 hrs
Day 3: Phase 3 (Frontend) - ~3 hrs
Day 4: Phase 4 (Aspire) - ~2 hrs
Day 5: Phase 5 (Docs & Scripts) - ~2 hrs
Day 6: Phase 6 (Validation & Testing) - ~3 hrs
Day 7: Phase 7 (PR & Merge) - ~1 hr

Total: ~14 hrs (7-8 days at 2 hrs/day)
```

---

## Reference Documentation

- **Specification**: `specs/002-project-setup/spec.md` (5 user stories, 11 FRs, 7 SCs)
- **Implementation Plan**: `specs/002-project-setup/plan.md` (tech stack, structure, decisions)
- **Research & Decisions**: `specs/002-project-setup/research.md` (5 key decisions with rationale)
- **Quick Start**: `specs/002-project-setup/quickstart.md` (15-minute onboarding guide)
- **Constitution**: `specs/002-project-setup/constitution.md` (5 principles, quality gates)
- **Requirements Checklist**: `specs/002-project-setup/checklists/requirements.md` (validation checklist)

---

## Next Steps (Phase 2+)

Upon completion of this feature:

1. ✅ Update `.github/copilot-instructions.md` with new tech stack (T062)
2. ✅ Merge to `develop` branch (T063)
3. → **Phase 3** (001-aec-file-sync): Implement file synchronization connector
4. → **Phase 4** (004-database-setup): Add PostgreSQL persistence
5. → **Phase 5** (005-authentication): Add OAuth and user management

---

## Quality Gates Checklist

Before marking the feature as complete, verify:

- [ ] All 63 tasks completed and tested
- [ ] All 7 success criteria (SC-001 through SC-007) validated
- [ ] All 11 functional requirements (FR-001 through FR-011) implemented
- [ ] All 5 user stories (US1-US5) independently testable
- [ ] Constitution v1.2.0 principles satisfied (I, III, V certified; II, IV deferred)
- [ ] TypeScript strict mode enabled, zero compilation errors
- [ ] No JavaScript files in `frontend/src/`
- [ ] Node.js 22 LTS enforced in setup scripts
- [ ] Backend tests passing (xUnit)
- [ ] Frontend tests passing (Vitest)
- [ ] Aspire orchestration verified (full stack startup, dashboard, logs)
- [ ] Pull request approved and merged to `develop`
- [ ] Agent context updated (`.github/copilot-instructions.md`)

---

## Notes

- Tasks are marked with `[P]` if they can execute in parallel with others in the same phase
- Tasks are marked with `[US1]`, `[US2]`, etc. to indicate which user story they satisfy
- Each phase has clear entry/exit criteria enabling incremental validation
- MVP scope covers Phases 1-3 + selected Phase 4 tasks (T029, T030, T031, T032, T033)
- Production deployment (Phase 2+) deferred per specification assumptions
