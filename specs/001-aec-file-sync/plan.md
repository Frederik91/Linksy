# Implementation Plan: Linksy

**Branch**: `001-aec-file-sync` | **Date**: 2025-10-25 | **Spec**: `/specs/001-aec-file-sync/spec.md`
**Input**: Feature specification from `/specs/001-aec-file-sync/spec.md`

## Summary

Linksy is a multi-tenant, serverless sync engine for bidirectional file synchronization between Autodesk Construction Cloud Docs and Microsoft SharePoint Online. Users (BIM managers, project coordinators) connect platforms via OAuth, set up bindings with conflict policies (SourceWins, TargetWins, LastWriterWins, ManualHold), and the system runs deterministic, observable, auditable sync jobs via Durable Functions with append-only audit trails and compliance-grade immutability.

**Technical approach**: Azure serverless + event-driven architecture (Functions, Durable Functions, SQL Serverless, Storage Queues, Blob with lifecycle policies, Key Vault, App Insights) minimizes idle cost and operational overhead while supporting 99.5% daily job success and <10 minute propagation latency.

## Technical Context

**Language/Version**: .NET 9.0 (backend) + TypeScript 5.3+ (frontend)  
**Primary Dependencies**: 
  - Backend: ASP.NET Core Minimal API, Azure Functions, Durable Functions, Azure Identity, EF Core, OpenTelemetry 1.13.0
  - Frontend: React 19, Vite 5.0+, Tailwind CSS 4.0+, shadcn/ui, TypeScript (strict mode)
  
**Storage**: Azure SQL Database (Serverless) + Azure Blob Storage (transient cache, quarantine, audit exports) + Azure Table Storage (optional state cache)  
**Testing**: xUnit 2.9.3 (backend), Vitest 1.0+ (frontend), contract tests for ACC/SP connectors  
**Target Platform**: Azure cloud (Functions consumption plan, Static Web Apps, SQL Serverless)  
**Project Type**: Web application (distributed backend + React SPA frontend)  
**Performance Goals**: 
  - Onboarding: 90% users complete in <10 minutes (SC-001)
  - Sync success: 99.5% daily completion rate (SC-002)
  - Propagation: 95% of changes within 10 minutes (SC-003)
  - API response: <200ms p95 for control plane endpoints
  
**Constraints**:
  - No persistent file content; transient cache max 24 hours (compliance requirement)
  - 7-day quarantine window for deleted files (auto-purge)
  - Append-only audit tables with DB-level delete constraints
  - 5-minute binding-level oscillation hold per file
  - Exponential backoff retry: 1s → 2s → 4s → 8s → 16s → 30s → 40s (max 7 retries, 120s window)
  
**Scale/Scope**: 
  - Phase 1: Single-tenant MVP with 2 connectors (ACC Docs, SharePoint Online)
  - Target: 10k+ business users, 100+ concurrent sync jobs, 1000+ bindings
  - Estimated backend LOC: 3-5k (core APIs, orchestrations, activities, connectors)
  - Estimated frontend LOC: 2-3k (wizard, activity log, conflict resolver, audit export)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **I. Frictionless Adoption** – Onboarding wizard validates connectors with test R/W; dry-run preview before binding activation (FR-002, FR-003).  
✅ **II. Deterministic Sync Integrity** – Headless Durable Functions with delta tokens, per-binding checkpoints, conflict policies, versioning, audit trail (FR-005, FR-006, FR-015).  
✅ **III. Security & Compliance by Design** – App-only OAuth, KV-backed credentials, 24h transient cache, append-only audit with hash chain, no file bodies persisted (FR-008, FR-010, FR-010a).  
✅ **IV. Extensible Connector Platform** – (Defer to Phase 2) Connector SDK pattern for ACC Docs + SP; activity functions for delta/upload/conflict ops.  
✅ **V. Observable & Cost-Conscious Operations** – OpenTelemetry traces/metrics, App Insights per tenant, manual-hold queues, quarantine safety lever, Azure serverless pricing model (FR-009, FR-014).  

**Frontend stack alignment**: React 19 + TypeScript (strict mode) + shadcn/ui + Tailwind CSS 4 via Vite (✅ matches Linksy project guidelines).  
**Backend stack alignment**: .NET 9.0 + ASP.NET Core Minimal API + Durable Functions (✅ approved in earlier feature clarifications).  
**Storage decision**: Azure SQL Serverless (not PostgreSQL per constitution); aligns with serverless cost model and RBAC requirements.  

**Violations**: None. Architecture fully complies with constitution principles.

## Project Structure

### Documentation (this feature)

```text
specs/001-aec-file-sync/
├── spec.md              # Feature specification (clarifications, requirements, entities)
├── plan.md              # This file (technical context, design, contracts)
├── research.md          # Phase 0 output: technology decisions & best practices
├── data-model.md        # Phase 1 output: EF Core schema, validation rules, state transitions
├── quickstart.md        # Phase 1 output: dev setup, local run instructions, testing guide
├── contracts/           # Phase 1 output: API contracts & connector interfaces
│   ├── README.md
│   ├── api-spec.yaml    # OpenAPI 3.0 spec for control plane HTTP API
│   ├── connector-spec.md # Connector interface & contract tests
│   └── events-schema.json # Event & audit entry schema
├── architecture.md      # Architecture decision records (already present)
└── tasks.md             # Phase 2 output: work items with effort estimates
```

### Source Code (repository root)

```text
Linksy.sln
├── Linksy.ServiceDefaults/          # Shared .NET infrastructure
├── Linksy.Api/                      # Control plane API (HTTP Functions)
│   ├── Program.cs                   # Minimal API endpoints
│   ├── Models/
│   │   ├── Tenant.cs
│   │   ├── ApplicationUser.cs
│   │   ├── Binding.cs
│   │   ├── Job.cs
│   │   ├── ChangeItem.cs
│   │   └── AuditEntry.cs
│   ├── Services/
│   │   ├── BindingService.cs
│   │   ├── JobService.cs
│   │   ├── AuditService.cs
│   │   └── CredentialService.cs
│   ├── Handlers/
│   │   ├── OnboardingHandler.cs
│   │   ├── ConflictResolutionHandler.cs
│   │   └── AuditExportHandler.cs
│   └── Middleware/
│       └── JwtAuthMiddleware.cs
│
├── Linksy.Sync/                     # Durable Functions + connectors (.NET 9)
│   ├── Functions/
│   │   ├── RunBindingSyncOrchestrator.cs
│   │   ├── GetAccDeltasActivity.cs
│   │   ├── GetSpDeltasActivity.cs
│   │   ├── ApplyChangeActivity.cs
│   │   ├── BackoffRetryActivity.cs
│   │   ├── MoveToManualHoldActivity.cs
│   │   └── WriteAuditEntryActivity.cs
│   ├── Entities/
│   │   └── HoldWindowEntity.cs     # 5-minute oscillation prevention
│   ├── Triggers/
│   │   ├── ScheduledSyncTrigger.cs
│   │   ├── WebhookTrigger.cs
│   │   └── QueueTrigger.cs
│   ├── Connectors/
│   │   ├── IConnector.cs
│   │   ├── AccConnector.cs
│   │   └── SharePointConnector.cs
│   └── Models/
│       ├── SyncContext.cs
│       ├── ChangeItem.cs
│       └── SyncResult.cs
│
├── Linksy.Api.Tests/                # Backend unit + contract tests
│   ├── OnboardingTests.cs
│   ├── ConflictPolicyTests.cs
│   ├── AuditTrailTests.cs
│   ├── CredentialRotationTests.cs
│   └── ConnectorContractTests.cs
│
├── Linksy.AppHost/                  # Aspire orchestration (dev only)
│   └── AppHost.cs
│
└── src/frontend/                    # React SPA (TypeScript)
    ├── src/
    │   ├── main.tsx
    │   ├── App.tsx
    │   ├── index.css               # Tailwind + global styles
    │   ├── vite-env.d.ts
    │   ├── components/
    │   │   ├── ui/                 # shadcn/ui components
    │   │   ├── OnboardingWizard.tsx
    │   │   ├── BindingManager.tsx
    │   │   ├── ActivityLog.tsx
    │   │   ├── ConflictResolver.tsx
    │   │   └── AuditExporter.tsx
    │   ├── pages/
    │   │   ├── Dashboard.tsx
    │   │   ├── Settings.tsx
    │   │   └── AuditLog.tsx
    │   ├── services/
    │   │   ├── api.ts              # API client (axios/fetch)
    │   │   ├── auth.ts             # JWT + refresh token handling
    │   │   └── telemetry.ts        # Client-side tracing
    │   ├── hooks/
    │   │   └── useAuth.ts
    │   ├── types/
    │   │   ├── binding.ts
    │   │   ├── job.ts
    │   │   └── audit.ts
    │   └── lib/
    │       └── utils.ts            # cn() for Tailwind class merging
    ├── tests/
    │   ├── OnboardingWizard.test.tsx
    │   ├── ActivityLog.test.tsx
    │   └── ConflictResolver.test.tsx
    ├── public/
    ├── package.json
    ├── tsconfig.json               # strict: true
    ├── vite.config.ts
    ├── vitest.config.ts
    ├── tailwind.config.ts
    └── eslint.config.cjs
```

**Structure Decision**: 
  - **Backend**: Three .NET projects (Linksy.Api for HTTP control plane, Linksy.Sync for Durable Functions orchestrations & connectors, Linksy.Api.Tests for contract/integration tests).
  - **Frontend**: Single React SPA in `src/frontend/` (TypeScript-only, Vite, Tailwind, shadcn/ui).
  - **Orchestration**: Linksy.AppHost (Aspire) for local dev; Azure Functions consumption plan for production.
  - **Rationale**: Separation of concerns (control plane vs. background jobs), independent testing, cost efficiency (Functions consumption = pay-per-execution), compliance (append-only audit, RBAC).

## Complexity Tracking

No constitutional violations. Architecture aligns with all five principles:
  - ✅ Frictionless: Wizard + preview
  - ✅ Deterministic: Durable Functions + checkpoints
  - ✅ Security: KV + RBAC + append-only
  - ✅ Extensible: Connector interface
  - ✅ Observable: OpenTelemetry + App Insights

---

## Phase 0: Research & Technology Decisions

### Research Tasks

**Task 0.1: Azure Durable Functions patterns for sync orchestration**
- Decision: Use Orchestrator-Activity pattern with Durable Entities for state (5-min hold window).
- Rationale: Built-in reliability, state persistence, sub-orchestrations for complex flows.
- Alternatives: Raw Azure Service Bus queues (too low-level), Logic Apps (limited customization).

**Task 0.2: EF Core + Azure SQL Serverless for RBAC & append-only audit**
- Decision: EF Core DbContext with Fluent API; database role constraints for append-only.
- Rationale: Type-safe ORM, migration support, RBAC roles enforce intent.
- Alternatives: Dapper (lower-level), Entity Framework Core with value converters (more verbosity).

**Task 0.3: ASP.NET Core Identity + JWT for stateless auth**
- Decision: Identity DbContext separate from app schema; issue JWT (12h) + refresh tokens.
- Rationale: Built-in password hashing, role claims, integrates with Entra ID via OIDC.
- Alternatives: Custom token issuer (risks), Azure AD Direct (adds service cost).

**Task 0.4: React 19 + Vite + shadcn/ui + Tailwind CSS 4 best practices**
- Decision: Use React Server Components where applicable; Tailwind for styling; shadcn/ui for UI components.
- Rationale: Vite HMR <3s, Tailwind utility-first prevents CSS bloat, shadcn/ui builds on Radix (accessible).
- Alternatives: Next.js (overkill for SPA), Material-UI (bundle size), Bootstrap (less composability).

**Task 0.5: Azure Blob Storage lifecycle policies for compliance**
- Decision: 3 containers (transient-cache, quarantine, audit-exports); WORM on audit-exports.
- Rationale: Auto-expiration reduces compliance cost, WORM + versioning detect tampering.
- Alternatives: Custom TTL logic in code (fragile), Cosmos DB (unnecessary scale).

**Task 0.6: OpenTelemetry 1.13.0 + Application Insights for observability**
- Decision: OTel Trace Exporter to App Insights; custom metrics for sync metrics.
- Rationale: Industry standard, free tier in App Insights, correlation IDs across tiers.
- Alternatives: Application Insights SDK only (less flexible), Datadog (external cost).

**Task 0.7: Azure Storage Queues (MVP) vs. Service Bus (future)**
- Decision: Start with Storage Queues; upgrade to Service Bus sessions if per-binding FIFO required.
- Rationale: Lowest cost MVP, poison queues sufficient for DLQ, visibility timeout handles retries.
- Alternatives: Event Grid (event-driven but less reliable for sync), direct DB polling (inefficient).

**Task 0.8: Credential rotation flow with Key Vault**
- Decision: Rotation function reads old secret, requests new from connector, validates, updates KV, unpauses binding.
- Rationale: Zero-downtime rotation, clean audit trail, connector SDK handles secret swap.
- Alternatives: Manual operator intervention (error-prone), automated full rotation (risky if validation fails).

---

## Phase 1: Design & Contracts

### 1.1 Data Model (EF Core)

**File**: `data-model.md` (generated)

Core entities extracted from spec with validation rules, FK relationships, and state transitions:

- **Tenant**: Id (PK), Name, BillingProfile (JSON), CreatedAt, Status
- **ApplicationUser**: Id (PK), TenantId (FK), Email (unique per tenant), DisplayName, EntraObjectId, CreatedAt
- **UserRole**: UserId (FK), TenantId (FK), Role (enum: Admin/Operator/Auditor)
- **Connector**: Id (PK), TenantId (FK), Kind (enum: ACC/SP), KeyVaultRef, Health, LastValidatedAt
- **Binding**: Id (PK), TenantId (FK), AccConnectorId, SpConnectorId, AccPath, SpPath, Direction, ConflictPolicy, Schedule, Status, CreatedAt, UpdatedAt
- **BindingState**: BindingId (PK/FK), AccDeltaToken, SpDeltaToken, LastSyncAt, AccChecksum, SpChecksum
- **Job**: Id (PK), BindingId (FK), Status (Pending/Running/Success/Failed), StartedAt, EndedAt, StatsJson (files, bytes, errors), TriggeredBy
- **ChangeItem**: Id (PK), JobId (FK), FileKey, Action (Create/Update/Move/Delete/Rename), SourceVersion, TargetVersion, Retries, Status, DlqReason
- **Quarantine**: Id (PK), BindingId (FK), FileKey, Reason (Conflict/SoftDelete), CreatedAt, ExpiresAt (7d), VersionRefs (JSON), RestoredAt (nullable)
- **AuditEntry**: Id (PK), TenantId (FK), BindingId (FK), At (server timestamp), Actor (UserId or NULL for system), Action, DetailsJson, PrevHash, Hash
  - **Constraint**: Database view/trigger prevents DELETE/UPDATE on AuditEntry; system role writes only
- **CredentialRotation**: Id (PK), ConnectorId (FK), TriggeredBy (UserId), TriggeredAt, ValidatedAt, Status (Pending/Success/Failed)

### 1.2 Phase 2 Planning (Next Step)

The following work items will be generated in `/speckit.tasks`:

**P1 (Onboarding & Core Sync)**
- EF Core DbContext models + migrations
- JWT issuer + Entra SSO integration
- ACC & SP connectors
- Durable Functions orchestrator + activities
- Frontend onboarding wizard + activity log

**P2 (Operations & Compliance)**
- HoldWindowEntity for oscillation prevention
- Quarantine service + conflict resolution
- Audit trail + export with hash chain
- Credential rotation + Key Vault integration

**P3 (Observability & Polish)**
- OpenTelemetry integration
- App Insights metrics & dashboards
- Alerting rules
- Documentation & support playbooks

---

## Next Steps

1. ✅ Specification clarified + architecture documented
2. ✅ Constitution check passed (no violations)
3. ⏭ Run `/speckit.tasks` to generate detailed work breakdown with effort estimates
4. ⏭ Create Bicep IaC for Dev/Staging/Prod environments
5. ⏭ Begin Phase 1 implementation
