# Feature Constitution: Project Initialization (002-project-setup)

## Principle I: Frictionless Developer Experience (LOCKED)

**Requirement**: Setup must be automated to achieve 15-minute onboarding goal.

**Enforcement**:
- Setup scripts (`setup.sh`, `setup.ps1`) must run end-to-end without manual intervention
- All prerequisites verified automatically (exit 1 on missing .NET 8 SDK or Node.js 22 LTS)
- Troubleshooting guide must address top 5 common setup failures
- On success, developers can run `dotnet run` in AppHost directory to start full stack

**Quality Gates**:
- ✅ setup.sh/setup.ps1 execute successfully on fresh CI agents
- ✅ First-time developer feedback <15 minutes from git clone to running app
- ✅ All prerequisite checks include version detection (e.g., .NET 8.x, Node.js 22.x)

**Status**: READY FOR IMPLEMENTATION

---

## Principle II: Sync & Orchestration Deferred (LOCKED)

**Requirement**: This feature focuses on local dev setup; file sync and cross-service orchestration patterns are deferred.

**Scope Exclusion**:
- File sync logic (Linksy's core AEC capability) → Phase 3 (001-aec-file-sync)
- Database persistence → Future phase (not required for local dev in Phase 0)
- Aspire orchestration deep-dive → Research completed; implementation uses `AddNpmApp` standard pattern

**Quality Gates**:
- ✅ No file sync code in scaffold (placeholder repos/APIs only)
- ✅ Schema/migration scripts optional; focus on rapid local bootstrap

**Status**: PROPERLY DEFERRED

---

## Principle III: Security by Default (LOCKED)

**Requirement**: Secure configuration from first run.

**Enforcement**:
- `.env` files excluded from git (added to `.gitignore`)
- CORS configured in API to allow only local dev URLs (http://localhost:5173)
- API health endpoint requires no authentication (simplifies Aspire discovery); auth layer deferred to Phase 2
- Setup script runs as user (no sudo required for npm/dotnet operations)

**Quality Gates**:
- ✅ No secrets committed to repository
- ✅ `.env.local` template provided with placeholder values
- ✅ CORS defaults to localhost only (no wildcard)
- ✅ README warns against exposing .env files

**Status**: READY FOR IMPLEMENTATION

---

## Principle IV: Connector Protocol Deferred (LOCKED)

**Requirement**: Connector protocol is a system-wide concern; local dev scaffold does not implement cross-environment communication.

**Scope Exclusion**:
- gRPC/HTTP connector configuration → Phase 2+ (multi-environment deployment)
- Service mesh or sidecar patterns → Infrastructure spec (not local dev)

**Quality Gates**:
- ✅ React-API communication uses simple HTTP requests to localhost:5000 (no connectors)
- ✅ Research.md Decision 2 documents why `AddNpmApp` + env vars are sufficient for Phase 0

**Status**: PROPERLY DEFERRED

---

## Principle V: Observable by Design (LOCKED)

**Requirement**: Developers can immediately see logs and trace execution without setup.

**Enforcement**:
- Aspire Dashboard auto-launches with AppHost startup (http://localhost:15217)
- Both React dev server and .NET API logs stream unified to dashboard
- React component mounts logged to browser console (React DevTools integration)
- API requests logged with timestamps and status codes (ILogger via Aspire)

**Quality Gates**:
- ✅ Dashboard connection automatic (no manual URL registration)
- ✅ Logs from React and .NET appear in unified view
- ✅ Developers can filter by service/severity without config changes

**Status**: READY FOR IMPLEMENTATION

---

## Delivery Workflow & Quality Gates

### Technology & Dependency Enforcement

- **Frontend Stack** (locked):
- Runtime: Node.js 22 LTS (mandatory, non-negotiable minimum version)
- Language: TypeScript 5.x (mandatory; no JavaScript files allowed in `src/`)
- Framework: React 19+ LTS
- Build tool: Vite 5+
- UI Library: shadcn/ui component primitives
- Styling: Tailwind CSS 4+
- Testing: Vitest + React Testing Library (all test files must be `.test.tsx` or `.test.ts`)
- Linting: ESLint 8+ with TypeScript parser
- Type checking: TypeScript strict mode enabled (tsconfig.json: `"strict": true`)

**Backend Stack** (locked):
- Runtime: .NET 8 SDK
- Framework: ASP.NET Core Web API (Minimal API pattern)
- Orchestration: Aspire 8.1+
- Testing: xUnit, Moq for unit tests
- Logging: ILogger (built-in .NET logging provider)
- Package Manager: dotnet CLI (NuGet packages)

**Infrastructure** (locked):
- Development: Aspire AppHost on localhost (unified dashboard)
- Ports: API (5000), React Dev Server (5173), Aspire Dashboard (15217)
- Package Managers: npm or yarn (no pnpm yet; standardize on npm for team consistency)

### Quality Gates Before Commit

All commits to `002-project-setup` branch must satisfy:

1. **TypeScript Compilation**:
   - ✅ `npx tsc --noEmit` passes in `frontend/` directory with zero errors
   - ✅ No `any` types in production code (except where explicitly needed; documented with `// @ts-ignore: <reason>`)
   - ✅ All React components typed with proper `React.FC` or function signatures

2. **Node.js Version Compliance**:
   - ✅ `setup.sh` and `setup.ps1` explicitly check for Node.js 22.x (fail if 18.x or earlier detected)
   - ✅ `.nvmrc` file present with `22` to support nvm users
   - ✅ `package.json` includes `"engines": { "node": "^22.0.0" }`

3. **No JavaScript in Frontend**:
   - ✅ `find frontend/src -name "*.js" -o -name "*.jsx"` returns zero results
   - ✅ All config files (webpack, vite.config, eslint.config) are `.ts` or `.cjs` (CommonJS for Node tooling)
   - ✅ README prominently warns: "React frontend must be 100% TypeScript; no .js files allowed"

4. **Linting & Format**:
   - ✅ `npm run lint` passes with zero errors in frontend
   - ✅ `npm run format:check` passes (Prettier formatting consistent)
   - ✅ `.NET build` passes with no warnings in backend

5. **Setup Script Validation**:
   - ✅ `./setup.sh` runs successfully on fresh Linux/macOS CI agent (simulated)
   - ✅ `./setup.ps1` runs successfully on fresh Windows CI agent (PowerShell)
   - ✅ Scripts exit with status 1 if .NET 8 SDK not found
   - ✅ Scripts exit with status 1 if Node.js 22 not found
   - ✅ Scripts exit with status 0 and print "Setup complete" on success

6. **Documentation Completeness**:
   - ✅ README includes Node.js 22 LTS as first prerequisite
   - ✅ Quickstart.md specifies TypeScript-only policy
   - ✅ Troubleshooting section covers "TypeScript compilation errors" and "Node version mismatch"

7. **No Breaking Changes**:
   - ✅ Feature does not modify existing git history
   - ✅ Scaffold creates new directories only (no overwrites of existing code)

### Definition of Done

Feature is complete when:

1. ✅ Scaffold created via `dotnet new aspire` + React Vite + TypeScript setup
2. ✅ Setup scripts (setup.sh, setup.ps1) verified on CI agents
3. ✅ All 11 FRs implemented and tested (manual smoke test acceptable for Phase 0)
4. ✅ All 7 SCs validated (timing tests run on low-end machine)
5. ✅ All 5 user stories covered by quickstart.md
6. ✅ Constitution check: All 5 principles addressed or properly deferred
7. ✅ Node.js 22 LTS + TypeScript-only enforcement in place
8. ✅ Pull request merged to `develop` with auto-complete enabled
9. ✅ Agent context updated (`.github/copilot-instructions.md`) with tech stack including Node.js 22
10. ✅ Next feature (Phase 2: `/speckit.tasks`) can use this scaffold as foundation

---

## Version History

| Date | Author | Change |
|------|--------|--------|
| 2025-10-25 | AI Agent | v1.0.0: Initial constitution aligned with 5 principles; Node.js 22 LTS + TypeScript-only enforcement |

---

## Governance Notes

**Design Philosophy**: This feature prioritizes **developer velocity** (Principle I) and **observable debugging** (Principle V) over early optimization. Security (Principle III) defaults to conservative (localhost-only CORS); production hardening deferred. File sync orchestration (Principles II, IV) intentionally deferred to preserve feature scope and enable parallel work on other aspects of Linksy.

**Enforcement Mechanism**: Constitution checks are automated via:
- `npm run lint` + TypeScript strict mode (frontend)
- `dotnet build` with warning-as-error (backend)
- Setup script tests (CI validation of .sh and .ps1 on multiple agents)
- Manual review: TypeScript-only file extensions, no .js in src/

**Escalation Path**: If Node.js 22 requirement conflicts with external dependency, escalate to Architecture Review (deferred to Phase 2 planning).
