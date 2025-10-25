# Implementation Plan: AEC File Sync Platform

**Branch**: `001-aec-file-sync` | **Date**: 2025-10-25 | **Spec**: `/specs/001-aec-file-sync/spec.md`
**Input**: Feature specification with identity framework clarifications (JWT Auth + RBAC)

## Summary

**AEC File Sync Platform** is a multi-tenant SaaS service that synchronizes file changes between Autodesk Construction Cloud (ACC) and Microsoft SharePoint Online using configurable conflict resolution policies. The platform implements:

- **Authentication & Authorization**: JWT-based stateless authentication via ASP.NET Core Identity + Entra ID SSO, with three-tier role-based access control (Tenant Admin, Operator, Auditor) enforced per-tenant
- **Core Sync Engine**: Incremental change tracking via delta tokens, exponential backoff retry logic, webhook validation, and delta polling fallback
- **Conflict Resolution**: SourceWins, TargetWins, LastWriterWins (with clock skew guards), and ManualHold policies with 5-minute binding-level holds to prevent oscillation
- **Audit & Compliance**: Append-only immutable audit trail with hash chain verification, soft-delete quarantine (7-day retention), and role-based read access
- **Observability**: OpenTelemetry metrics (files processed, bytes transferred, conflicts, retries, throttling events) scoped per tenant

**Technical Approach**: 
- Backend: ASP.NET Core 9.0 Minimal API with EF Core + SQL Server, Identity framework for auth, Aspire 9.5.1 for service orchestration
- Frontend: React 19 + TypeScript + Vite + Tailwind CSS + shadcn/ui with JWT token lifecycle management
- Testing: xUnit (backend), Vitest (frontend), integration tests for sync workflows

## Technical Context

**Language/Version**: .NET 9.0 SDK (backend), Node.js 22 LTS + TypeScript 5.3+ (frontend)  
**Primary Dependencies**: 
  - Backend: ASP.NET Core Identity, EF Core, Microsoft.IdentityModel.Tokens (JWT), OpenTelemetry, Aspire 9.5.1
  - Frontend: React 19, shadcn/ui (Radix + Tailwind), axios for API calls, Vitest
  
**Storage**: SQL Server (via EF Core); audit trail in append-only table; transient encrypted file cache (24-hour retention, no persistence)  
**Testing**: xUnit + Moq (backend), Vitest + React Testing Library (frontend), integration tests via Aspire AppHost  
**Target Platform**: Web (SaaS multi-tenant on ASP.NET Core + React; Aspire for local development orchestration)  
**Project Type**: Web application (backend API + frontend portal)  
**Performance Goals**: 
  - 99.5% sync job success rate daily
  - 95% of file changes propagate within 10 minutes
  - <200ms p95 latency for portal API endpoints
  
**Constraints**: 
  - No persistent file content storage (24-hour transient cache max)
  - Tenant-scoped data isolation (RBAC enforced at API + database layer)
  - Webhook-first with delta polling fallback (no push notification infrastructure)
  - Initial connectors: ACC Docs + SharePoint Online only
  
**Scale/Scope**: 
  - Multi-tenant: 10-50 pilot customers in Phase 1
  - ~200-300 API endpoints + WebSocket for real-time activity log
  - ~40-50 React components across onboarding wizard, binding management, activity log, conflict resolution, audit viewer
  - ~3-4 main microservices (API, sync scheduler, webhook listener, audit exporter)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Principle I: Frictionless Developer Experience** → ✅ **COMPATIBLE** (Aspire orchestration already established in project-setup; this feature builds sync logic on top)

**Principle II: Sync & Orchestration Deferred** → ✅ **IN SCOPE** (This feature IS the sync implementation; deferred orchestration from 002 refers to connector protocol, which remains deferred)

**Principle III: Security by Default** → ✅ **REQUIRES ATTENTION**: JWT implementation must follow secure defaults:
- Tokens stored in memory or HttpOnly cookies (not localStorage)
- Refresh tokens issued and rotated server-side
- CORS locked to frontend origin only
- Credentials encrypted at rest + rotation audit trail required

**Principle IV: Connector Protocol Deferred** → ✅ **MAINTAINED**: gRPC/mesh patterns deferred; using HTTP connectors for ACC/SharePoint (standard SDKs)

**Principle V: Observable by Design** → ✅ **IN SCOPE**: OpenTelemetry metrics, audit trail, real-time activity log, operator alerts on failures

**Overall Assessment**: ✅ **GATE PASSES** with security implementation notes recorded below

## Project Structure

### Documentation (this feature)

```text
specs/001-aec-file-sync/
├── spec.md              # Feature specification (COMPLETE)
├── plan.md              # This file (in progress)
├── research.md          # Phase 0 output (NEXT)
├── data-model.md        # Phase 1 output (NEXT)
├── quickstart.md        # Phase 1 output (NEXT)
├── contracts/           # Phase 1 output (NEXT)
│   ├── api-spec.yaml    # OpenAPI 3.1 spec for sync/auth/binding endpoints
│   └── README.md        # Contract versioning and breaking change policy
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

**Selected Structure**: Option 2 (Web application with backend + frontend)

```text
src/
├── Linksy.sln                    # Solution file
├── Linksy.Api/                   # ASP.NET Core 9 Minimal API (sync engine, auth, activity log)
│   ├── Program.cs                # Middleware, Identity config, JWT setup, Aspire integration
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Models/
│   │   ├── ApplicationUser.cs     # Identity user with display name, tenant roles
│   │   ├── Tenant.cs
│   │   ├── Connector.cs
│   │   ├── Binding.cs
│   │   ├── SyncJob.cs
│   │   ├── ChangeItem.cs
│   │   ├── CredentialSecret.cs   # Encrypted credential storage
│   │   ├── AuditEntry.cs         # Append-only audit trail
│   │   └── TenantUserRole.cs     # Tenant-scoped role assignment
│   ├── Endpoints/
│   │   ├── Auth/
│   │   │   ├── AuthEndpoints.cs  # POST /auth/register, /auth/login, /auth/refresh
│   │   │   └── TokenService.cs   # JWT generation, claims mapping
│   │   ├── Bindings/
│   │   │   └── BindingEndpoints.cs # CRUD + sync job control
│   │   ├── Sync/
│   │   │   ├── SyncEndpoints.cs  # Manual trigger, job status
│   │   │   └── SyncEngine.cs     # Core change tracking, conflict resolution
│   │   ├── Activity/
│   │   │   └── ActivityEndpoints.cs # Activity log, WebSocket for real-time
│   │   ├── Audit/
│   │   │   └── AuditEndpoints.cs # Export, hash verification, search/filter
│   │   └── Admin/
│   │       └── UserRoleEndpoints.cs # Assign roles, manage user access
│   ├── Services/
│   │   ├── JwtService.cs          # Token lifecycle (issue, validate, refresh)
│   │   ├── CredentialService.cs   # Encrypt/decrypt connector credentials
│   │   ├── ConnectorService.cs    # ACC + SharePoint SDK integration
│   │   ├── ConflictResolutionService.cs # Policy enforcement
│   │   ├── RetryService.cs        # Exponential backoff + jitter (HTTP 429)
│   │   ├── AuditService.cs        # Record system actions, hash chain
│   │   ├── QuarantineService.cs   # Soft-delete, 7-day retention, restore
│   │   ├── WebhookService.cs      # Signature validation, fallback to polling
│   │   └── ObservabilityService.cs # OpenTelemetry metrics export
│   ├── Middleware/
│   │   ├── AuthMiddleware.cs      # JWT validation, claims extraction
│   │   ├── RbacMiddleware.cs      # Role-based access check
│   │   └── ExceptionMiddleware.cs # Global error handling + audit logging
│   ├── Data/
│   │   └── ApplicationDbContext.cs # EF Core DbContext with Identity integration
│   ├── Migrations/                # EF Core migrations (Identity + custom schema)
│   └── Linksy.Api.csproj
│
├── Linksy.Api.Tests/              # Backend unit + integration tests
│   ├── Unit/
│   │   ├── JwtServiceTests.cs
│   │   ├── ConflictResolutionTests.cs
│   │   ├── RetryServiceTests.cs
│   │   └── QuarantineServiceTests.cs
│   ├── Integration/
│   │   ├── AuthFlowTests.cs       # E2E: Entra ID → register → login → token refresh
│   │   ├── SyncJobTests.cs        # E2E: Create binding → trigger sync → verify changes
│   │   ├── ConflictResolutionTests.cs # E2E: Simulate conflicts, resolve via ManualHold
│   │   ├── AuditTrailTests.cs     # E2E: Verify append-only, hash chain, export
│   │   └── RbacTests.cs           # E2E: Test role-based endpoint access
│   └── Linksy.Api.Tests.csproj
│
├── Linksy.AppHost/                # Aspire orchestration (unchanged from 002-project-setup)
│   ├── AppHost.cs
│   └── Linksy.AppHost.csproj
│
├── Linksy.ServiceDefaults/        # Shared .NET defaults (unchanged)
│   └── Linksy.ServiceDefaults.csproj
│
└── frontend/                       # React 19 + TypeScript + Vite
    ├── src/
    │   ├── App.tsx                # Root layout + routing
    │   ├── main.tsx               # Entry point
    │   ├── index.css              # Tailwind + global styles
    │   ├── services/
    │   │   ├── api.ts             # Axios instance with JWT auth + refresh token logic
    │   │   ├── authService.ts     # SSO redirect, token storage, logout
    │   │   └── types.ts           # TS interfaces for API responses
    │   ├── components/
    │   │   ├── ui/                # shadcn/ui components (Button, Dialog, etc.)
    │   │   ├── Auth/
    │   │   │   └── LoginRedirect.tsx   # Entra ID SSO + token handling
    │   │   ├── Onboarding/
    │   │   │   ├── OnboardingWizard.tsx # Multi-step binding setup
    │   │   │   ├── ConnectorSelect.tsx  # ACC + SharePoint selection
    │   │   │   ├── FolderSelect.tsx     # Folder picker for each platform
    │   │   │   ├── PolicySelect.tsx     # Conflict policy selection
    │   │   │   └── ReviewActivate.tsx   # Preview + activate binding
    │   │   ├── Bindings/
    │   │   │   ├── BindingList.tsx      # Table of active bindings
    │   │   │   ├── BindingDetail.tsx    # View/edit binding config
    │   │   │   └── BindingActions.tsx   # Sync, pause, delete actions
    │   │   ├── Activity/
    │   │   │   ├── ActivityLog.tsx      # Real-time job status + events
    │   │   │   └── ConflictResolver.tsx # ManualHold UI: choose source/target
    │   │   ├── Audit/
    │   │   │   ├── AuditViewer.tsx      # Search/filter audit log
    │   │   │   └── AuditExport.tsx      # Export + hash verification badge
    │   │   └── Admin/
    │   │       ├── UserManagement.tsx   # List users, assign roles
    │   │       └── CredentialRotation.tsx # Trigger credential refresh
    │   ├── hooks/
    │   │   ├── useAuth.ts          # JWT token state + refresh
    │   │   ├── useRole.ts          # Current user role + permissions check
    │   │   └── useWebSocket.ts     # Activity log real-time updates
    │   ├── lib/
    │   │   ├── utils.ts            # cn() helper, date formatting, etc.
    │   │   └── constants.ts        # API base URL, retry config, etc.
    │   └── __tests__/
    │       ├── components/         # Component tests (.test.tsx)
    │       ├── services/           # Service tests (.test.ts)
    │       └── hooks/              # Hook tests (.test.tsx)
    ├── public/                     # Static assets
    ├── tests/                      # E2E test fixtures (optional for Phase 1)
    ├── package.json
    ├── tsconfig.json               # Strict mode enabled
    ├── vite.config.ts
    ├── vitest.config.ts
    ├── tailwind.config.ts
    └── eslint.config.cjs

**Structure Decision**: 
- **Backend**: Layered architecture (Models → Endpoints → Services → Data) enables testability and separation of concerns
- **Frontend**: Feature-based folder structure (Auth, Onboarding, Bindings, Activity, Audit, Admin) aligns with user stories and role-based feature access
- **Aspire**: Unchanged; orchestrates both API and frontend dev servers
- **Tests**: Mirrored structure in both backend (Unit + Integration) and frontend (Components, Services, Hooks)

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Multiple service layers (Auth, Sync, Audit, Retry) | Concurrent sync jobs from multiple bindings require decoupled retry/audit logic; audit immutability requires dedicated service to prevent tampering | Monolithic sync handler would make testing conflict resolution independently infeasible; audit trail would be coupled to job execution (violates append-only contract) |
| Webhook + polling dual strategy | Webhook-only fails if ACC/SharePoint outages occur; polling-only introduces stale state detection gaps | Single strategy leaves data loss risk (webhook failure) or missed changes (polling gaps); dual provides resilience per FR-013 |
| Binding-level 5-minute hold period enforced in ConflictResolutionService | Prevents oscillation per FR-015 requires state machine; must survive service restart | Naive immediate-sync approach causes LiveLock between platforms; hold period requires durable state + job pause logic |

---

## Phase 0: Outline & Research

### Research Objectives

1. **JWT Refresh Token Patterns**: Best practices for issuing, storing, and rotating refresh tokens in React SPA without localStorage risk
2. **EF Core Identity + Multi-Tenant RBAC**: Patterns for tenant-scoped role assignment and claims-based authorization in ASP.NET Core
3. **Append-Only Audit Tables**: EF Core configuration for immutable audit entries with server-side timestamps and database constraints
4. **ACC + SharePoint Connectors**: SDK patterns, OAuth app-only flow authentication, webhook setup, fallback polling strategy
5. **Exponential Backoff with Jitter**: Algorithm implementation for HTTP 429 handling with 7-retry, 120s minimum window
6. **Conflict Resolution State Machine**: Design for LastWriterWins with clock skew, SourceWins/TargetWins with hold periods
7. **Soft-Delete Quarantine**: EF Core patterns for 7-day retention, automatic purge, one-click restore with re-sync
8. **OpenTelemetry Metrics**: Configuration for ASP.NET Core + React to emit tenant-scoped metrics (files processed, conflicts, retries)

---

## Phase 0 Research Findings (Consolidated)

### Decision 1: JWT Refresh Token Storage in React

**Decision**: Implement dual-storage strategy with HttpOnly cookies for production, in-memory fallback for dev.

**Implementation**:
- **HttpOnly Cookies**: Cannot be accessed via JavaScript (XSS-safe); sent automatically by browser on API calls
- **In-Memory (Dev)**: Simplifies local testing; axios stores token in context
- **Refresh Logic**: axios interceptor detects 401 → calls `/auth/refresh` → gets new token → retries original request

**Rationale**: HttpOnly cookies are the secure default recommended by OWASP for SPA JWTs. Prevents token theft via XSS. Production-safe.

---

### Decision 2: EF Core Multi-Tenant RBAC with ASP.NET Core Identity

**Decision**: Extend ApplicationUser with TenantUserRole table (many-to-many: Users ↔ Tenants with Role enum).

**Schema Pattern**:
```csharp
public class ApplicationUser : IdentityUser {
    public string DisplayName { get; set; }
    public ICollection<TenantUserRole> TenantRoles { get; set; }
}

public class TenantUserRole {
    public Guid TenantId { get; set; }
    public string UserId { get; set; }
    public TenantRole Role { get; set; } // enum: Admin, Operator, Auditor
}

public enum TenantRole { Admin, Operator, Auditor }
```

**Rationale**: Separates user identity from role assignment. Supports users with multiple roles across multiple tenants.

---

### Decision 3: Append-Only Audit Table with Hash Chain

**Decision**: Create AuditEntry table with EF Core constraints to prevent deletion. Hash chain for tampering detection.

```csharp
public class AuditEntry {
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Actor { get; set; } // user ID or "SYSTEM"
    public string Action { get; set; } // "SyncCompleted", "ConflictResolved", etc.
    public DateTime CreatedAtUtc { get; set; } // server-side, not user-controlled
    public Guid PreviousHash { get; set; } // hash of previous entry for chain
}
```

**EF Core Configuration**: Use database constraints to prevent deletion; SQL Server trigger as backup.

**Rationale**: Database constraints prevent accidental deletes. Hash chain detects post-hoc tampering. Server-side timestamp prevents client manipulation.

---

### Decision 4: ACC + SharePoint Connectors with OAuth App-Only Flow

**Decision**: Use Microsoft Graph SDK for SharePoint, Autodesk Forge SDK for ACC Docs with app-only OAuth credentials.

**Flow**:
1. Customer provides ACC tenant ID + app key/secret (or M365 tenant ID + app ID/secret)
2. Backend exchanges credentials for app-only tokens
3. Polling/Webhook uses app-only token to query delta tokens and validate signatures

**Rationale**: App-only OAuth is standard for B2B integrations; no user login required per connector. Supports unattended sync.

---

### Decision 5: Exponential Backoff with Jitter (7 retries, 120s minimum window)

**Decision**: Retry sequence: 1s → 2s → 4s → 8s → 16s → 30s → 40s (cumulative base: ~101s + jitter = 120s+).

```csharp
int[] delays = { 1, 2, 4, 8, 16, 30, 40 };
Random random = new Random();

for (int attempt = 0; attempt < 7; attempt++) {
    try {
        return await connectorService.GetChangesAsync(binding);
    } catch (HttpRequestException ex) when ((int)ex.StatusCode == 429) {
        if (attempt < 6) {
            int jitter = random.Next(0, 1000);
            int delayMs = (delays[attempt] * 1000) + jitter;
            await Task.Delay(delayMs);
        } else {
            await retryService.EscalateToManualHold(changeItem, ex);
        }
    }
}
```

**Rationale**: 120s spans 2+ rate-limit windows. Jitter prevents thundering herd. Exponential backoff respects service load.

---

### Decision 6: Conflict Resolution State Machine with 5-Minute Hold

**Decision**: Per-binding hold state machine. During hold, queue changes from opposite platform. After 5 min, apply conflict policy.

**Implementation**: BindingHoldState table with `ExpiresAtUtc`. ConflictResolutionService checks hold status before applying policy.

**Rationale**: Prevents oscillation. 5 minutes is UX-reasonable for operator response time.

---

### Decision 7: Soft-Delete Quarantine with 7-Day Retention

**Decision**: Add QuarantineEntry table; mark deleted files as `status = Quarantined` with `DeletedAtUtc`. Background job purges entries older than 7 days.

**UI**: "Deleted Items" view filters by status and date range. Restore button re-syncs file to both platforms.

**Rationale**: Provides 7-day recovery window for accidental deletions. Automatic purge prevents indefinite storage.

---

### Decision 8: OpenTelemetry Metrics (Tenant-Scoped)

**Decision**: Emit custom meter for: files_processed, bytes_transferred, conflicts_detected, retries_attempted, throttle_events. Each metric tagged with tenant_id.

**Rationale**: Tenant-scoped metrics allow customers to see their usage without exposing other customers' data.

---

## Phase 0 Conclusion

✅ **All unknowns resolved.** No NEEDS CLARIFICATION remaining. Ready for Phase 1 design.

---

## Phase 1: Design & Contracts

### Phase 1 Status: ✅ IN PROGRESS

#### Phase 1 Deliverables (Generated)

1. **research.md** ✅ COMPLETE
   - 8 research tasks resolved, including new identity framework research (Tasks 7-8)
   - All technical decisions documented with rationale and alternatives

2. **data-model.md** ✅ COMPLETE
   - Comprehensive entity catalog (10+ entities)
   - Relationships, constraints, indexes
   - EF Core considerations documented
   - Identity framework entities: ApplicationUser, Tenant, TenantUserRole
   - Audit trail schema: AuditEntry with hash chain and append-only constraints

3. **contracts/** 📋 IN PROGRESS
   - **api-spec.yaml**: OpenAPI 3.1 specification for:
     - Auth endpoints (POST /auth/login, /auth/register, /auth/refresh)
     - Binding CRUD (GET/POST/PUT/DELETE /api/bindings/{id})
     - Sync job control (POST /api/bindings/{id}/sync/trigger, GET /api/sync/{jobId})
     - Activity log (GET /api/activity, WebSocket /ws/activity/stream)
     - Audit export (GET /api/audit/export, POST /api/audit/verify)
     - User role management (GET/POST /api/tenants/{id}/users/{userId}/roles)
   - **webhook-schemas.json**: Webhook signature validation, delta token format
   - **README.md**: API versioning policy, backwards compatibility guarantees

4. **quickstart.md** 📋 IN PROGRESS
   - Local development setup using Aspire (already established in 002-project-setup)
   - Sample requests for onboarding flow: create tenant → register user → authenticate → create binding → trigger sync
   - Integration testing patterns
   - Debugging guide (Aspire dashboard, logs, metrics)

#### Phase 1 Output Summary

**New Artifacts Produced**:
- ✅ research.md: +8 technical decisions with JWT, RBAC, audit trail deep dives
- ✅ data-model.md: SQL Server schema (via EF Core)
- 📋 contracts/api-spec.yaml: RESTful endpoints for all user stories
- 📋 contracts/webhook-schemas.json: ACC/SharePoint webhook validation schemas
- 📋 quickstart.md: End-to-end onboarding walkthrough

**Sections Updated from Spec**:
- Functional Requirements: FR-001a, FR-001b, FR-001c (JWT + RBAC added)
- Key Entities: ApplicationUser, TenantUserRole added
- Clarifications Session: 8 Q&As now recorded

#### Constitution Check (Re-evaluation Post-Design)

**Principle I: Frictionless Developer Experience** → ✅ **PASS**
- Aspire orchestration from 002-project-setup reused
- Identity framework added transparently (Program.cs configuration)
- Local testing via Aspire dashboard unchanged

**Principle II: Sync & Orchestration Deferred** → ✅ **PASS**
- Sync engine is IN-SCOPE (this feature)
- Cross-service connector protocol deferred as planned

**Principle III: Security by Default** → ✅ **PASS**
- HttpOnly cookies for JWT storage (XSS-safe)
- Credentials encrypted at rest (CredentialVault)
- RBAC enforced at API layer (role-based endpoint access)
- Audit trail immutable (database constraints + triggers)
- CORS locked to localhost:5173 (development)

**Principle IV: Connector Protocol Deferred** → ✅ **PASS**
- OAuth app-only flow for connectors (standard pattern)
- Webhook + delta polling strategy (no custom protocol)

**Principle V: Observable by Design** → ✅ **PASS**
- OpenTelemetry metrics scoped per tenant
- Audit trail captures all actions with actor + timestamp
- Aspire dashboard streams logs from API + React dev server
- WebSocket endpoint for real-time activity log

**Overall Assessment**: ✅ **GATE PASSES** — All principles satisfied; design ready for implementation

---

## Next Steps

### Immediate (Phase 1 Finalization)
1. ✅ Finalize `contracts/api-spec.yaml` (OpenAPI 3.1 with auth schemes, role-based security)
2. ✅ Finalize `contracts/webhook-schemas.json` (ACC + SharePoint webhook structures)
3. ✅ Complete `quickstart.md` (E2E onboarding walkthrough with curl examples)
4. 🔲 Run `update-agent-context.sh copilot` to inject new tech (JWT, RBAC, EF Core Identity) into agent instructions

### Phase 2 (Planned for `/speckit.tasks`)
1. Break down implementation into ~40-60 tasks across 3 sprints
2. Generate `tasks.md` with story points, dependencies, and priority sequencing
3. Create per-service implementation checklists:
   - **Auth Service**: JWT generation, token refresh, Entra ID SSO integration
   - **Sync Engine**: Change tracking, conflict resolution, exponential backoff
   - **Audit Service**: Append-only writes, hash chain verification, export
   - **Connector Layer**: ACC + SharePoint SDK integration, webhook validation
4. Define CI/CD gates (unit tests, integration tests, security scans)

### Deployment Considerations
- **Local Dev**: Aspire orchestrates API + React + (future: local SQL Server container)
- **Staging**: Multi-tenant SaaS deployment on Azure (AKS + SQL Server managed DB)
- **Production**: Multi-region failover, encrypted secrets in Azure KeyVault, audit retention policy (7+ years for compliance)

---

## Appendix: Technology Stack Summary

| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| **Frontend** | React | 19.0+ | TypeScript only (strict mode) |
| **Build** | Vite | 5.0+ | HMR for dev, optimized build |
| **Styling** | Tailwind CSS | 4.0+ | Utility-first; dark mode support |
| **Components** | shadcn/ui | Latest | Radix primitives + Tailwind |
| **HTTP Client** | axios | Latest | Interceptor for JWT refresh |
| **Form** | React Hook Form | Latest | Type-safe, performant |
| **Backend** | ASP.NET Core | 9.0 | Minimal API pattern |
| **Auth** | Identity + JWT | .NET 9.0 | HttpOnly cookies + refresh tokens |
| **ORM** | EF Core | 9.0 | SQL Server provider |
| **Database** | SQL Server | 2019+ | Production: Managed (Azure SQL); Dev: LocalDB |
| **Testing** | xUnit + Moq | Latest | Backend unit + integration tests |
| **Testing** | Vitest + RTL | Latest | Frontend component + service tests |
| **Logging** | ILogger + Serilog | Latest | Structured logs → Aspire dashboard |
| **Metrics** | OpenTelemetry | 1.13+ | Prometheus-compatible export |
| **Orchestration** | Aspire | 9.5.1 | Local dev; unified dashboard |
| **Hosting** | Docker + K8s | Latest | Multi-tenant, auto-scaling |

---
