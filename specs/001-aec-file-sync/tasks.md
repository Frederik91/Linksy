# Implementation Tasks: Linksy

**Branch**: `001-aec-file-sync` | **Date**: 2025-10-25  
**Source Specification**: `/specs/001-aec-file-sync/spec.md` | **Source Plan**: `/specs/001-aec-file-sync/plan.md`

## Overview

This document breaks down the Linksy feature into granular, executable work items organized by implementation phase and user story priority. Each task includes effort estimate, dependencies, and file paths for context.

**Task ID Format**: `T###` (e.g., T001, T002, T100)  
**Priority Prefix**: `[P1]`, `[P2]`, `[P3]` (from spec user stories)  
**Story Label**: `[US1]`, `[US2]`, `[US3]` (from spec user stories: Onboarding, Monitoring, Audit)  
**Labels**: `[frontend]`, `[backend]`, `[infra]`, `[test]`, `[docs]`

---

## Phase 0: Setup & Configuration (11 days)

### Infrastructure & Project Initialization (5 tasks)

- [ ] **T001** `[P1]` `[infra]` Initialize Linksy.Sync project for Durable Functions
  - File: `src/Linksy.Sync/Linksy.Sync.csproj`
  - Create .NET 9 class library with Azure.Functions.Worker.Extensions.DurableTask
  - **Effort**: 0.5 day

- [ ] **T002** `[P1]` `[infra]` Configure Azure Functions local development environment
  - Files: `src/Linksy.Api/Program.cs`, `src/Linksy.Sync/Program.cs`, `local.settings.json`
  - Install Azure Functions Core Tools; configure Aspire orchestration
  - **Effort**: 1 day

- [ ] **T003** `[P1]` `[infra]` Add Bicep IaC templates for Azure infrastructure (Dev/Prod)
  - Files: `infra/main.bicep`, `infra/parameters.dev.json`, `infra/parameters.prod.json`
  - Create parameterized Bicep for SQL Serverless, Storage, Key Vault, App Insights, Static Web Apps
  - **Effort**: 1.5 days

- [ ] **T004** `[P1]` `[infra]` Deploy IaC to Dev environment
  - Files: `scripts/deploy-infra.sh`
  - Create automation script and validate resource creation in Azure Portal
  - **Effort**: 1 day

- [ ] **T005** `[P1]` `[infra]` Configure Aspire AppHost to orchestrate all services
  - Files: `src/Linksy.AppHost/AppHost.cs`
  - Add API, Sync Functions, React frontend, Azure Storage emulator to orchestration
  - **Effort**: 1 day

### Documentation & Contracts (4 tasks)

- [ ] **T006** `[P1]` `[docs]` Document API contract (OpenAPI/Swagger spec)
  - File: `specs/001-aec-file-sync/contracts/api-spec.yaml`
  - Define all endpoint groups and request/response models
  - **Effort**: 1 day

- [ ] **T007** `[P1]` `[docs]` Define Connector interface contract
  - File: `specs/001-aec-file-sync/contracts/connector-spec.md`
  - Document IConnector interface and change item schema
  - **Effort**: 0.5 day

- [ ] **T008** `[P1]` `[docs]` Document event and audit schema
  - File: `specs/001-aec-file-sync/contracts/events-schema.json`
  - Define JSON schemas for all event types
  - **Effort**: 0.5 day

- [ ] **T009** `[P1]` `[docs]` Create Phase 0 research notes
  - File: `specs/001-aec-file-sync/research.md`
  - Document technology decisions and POC links
  - **Effort**: 1 day

---

## Phase 1: Foundational Infrastructure (26 days)

### Database Schema & EF Core (4 tasks)

- [ ] **T010** `[P1]` `[backend]` `[test]` Define EF Core entities and DbContext
  - Files: `src/Linksy.Api/Models/`, `src/Linksy.Api/Data/LinksynContext.cs`
  - Create 11 core entity models with relationships and indexes
  - **Effort**: 1.5 days

- [ ] **T011** `[P1]` `[backend]` `[test]` Configure audit table for append-only immutability
  - Files: `src/Linksy.Api/Data/LinksynContext.cs`, `src/Linksy.Api/Models/AuditEntry.cs`
  - Configure DB constraints preventing DELETE/UPDATE; implement hash chain
  - **Effort**: 1 day

- [ ] **T012** `[P1]` `[backend]` `[test]` Implement RBAC constraints in EF Core
  - Files: `src/Linksy.Api/Data/LinksynContext.cs`, `src/Linksy.Api/Services/RbacService.cs`
  - Create RbacService for tenant-scoped queries and claim-based authorization
  - **Effort**: 1 day

- [ ] **T013** `[P1]` `[backend]` `[test]` Create EF Core migrations
  - Files: `src/Linksy.Api/Migrations/001_InitialSchema.cs`, `002_AuditTableConstraints.cs`
  - Generate and configure migrations with database constraints
  - **Effort**: 0.5 day

- [ ] **T014** `[P1]` `[backend]` `[test]` Unit tests: EF Core models and migrations
  - File: `src/Linksy.Api.Tests/DataTests.cs`
  - Test relationships, soft-delete, audit constraints, RBAC filtering
  - **Effort**: 1 day

### Authentication & Authorization (6 tasks)

- [ ] **T015** `[P1]` `[backend]` `[test]` Implement ASP.NET Core Identity configuration
  - Files: `src/Linksy.Api/Services/AuthService.cs`, `src/Linksy.Api/Program.cs`
  - Configure Identity with custom ApplicationUser model and Entra ID integration
  - **Effort**: 1 day

- [ ] **T016** `[P1]` `[backend]` `[test]` Implement JWT token generation and refresh logic
  - Files: `src/Linksy.Api/Services/TokenService.cs`, `src/Linksy.Api/Models/RefreshToken.cs`
  - Create TokenService for JWT issuance (12h expiry) and refresh tokens
  - **Effort**: 1 day

- [ ] **T017** `[P1]` `[backend]` `[test]` Implement JWT authentication middleware
  - Files: `src/Linksy.Api/Middleware/JwtAuthMiddleware.cs`, `src/Linksy.Api/Program.cs`
  - Create middleware for token validation and RBAC claim extraction
  - **Effort**: 0.5 day

- [ ] **T018** `[P1]` `[backend]` `[test]` Implement role-based authorization attributes
  - Files: `src/Linksy.Api/Attributes/AuthorizeRoleAttribute.cs`, `src/Linksy.Api/Handlers/AuthorizationHandler.cs`
  - Create `[AuthorizeRole(...)]` attribute and policy-based authorization
  - **Effort**: 0.5 day

- [ ] **T019** `[P1]` `[backend]` `[test]` Implement Entra ID SSO integration (login endpoint)
  - Files: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - Create POST `/auth/login` endpoint for Entra ID token exchange and JWT issuance
  - **Effort**: 1 day

- [ ] **T020** `[P1]` `[backend]` `[test]` Unit tests: Authentication and authorization
  - File: `src/Linksy.Api.Tests/AuthTests.cs`
  - Test JWT lifecycle, refresh tokens, role-based access, Entra ID claims
  - **Effort**: 1 day

### Azure Services Configuration (5 tasks)

- [ ] **T021** `[P1]` `[infra]` Set up Azure Key Vault and credential rotation pattern
  - Files: `src/Linksy.Api/Services/CredentialService.cs`, `src/Linksy.Api/Models/CredentialRotation.cs`
  - Create CredentialService for credential storage/retrieval and rotation triggers
  - **Effort**: 1 day

- [ ] **T022** `[P1]` `[infra]` Configure Azure Storage Queues for local + cloud
  - Files: `src/Linksy.Sync/Triggers/QueueTrigger.cs`, `local.settings.json`
  - Set up queue names and poison queue configuration
  - **Effort**: 0.5 day

- [ ] **T023** `[P1]` `[infra]` Configure Azure Blob Storage containers and lifecycle policies
  - Files: `infra/main.bicep` (blob definitions)
  - Create containers with lifecycle rules (24h, 7d, WORM+versioning)
  - **Effort**: 0.5 day

- [ ] **T024** `[P1]` `[infra]` Configure Application Insights for OpenTelemetry
  - Files: `src/Linksy.Api/Program.cs`, `src/Linksy.Sync/Program.cs`, `src/Linksy.ServiceDefaults/Extensions.cs`
  - Add OpenTelemetry 1.13.0 + App Insights exporter with tracer provider
  - **Effort**: 1 day

- [ ] **T025** `[P1]` `[test]` Integration tests: Azure service connectivity
  - File: `src/Linksy.Api.Tests/AzureIntegrationTests.cs`
  - Test connectivity to SQL Serverless, Storage, Key Vault
  - **Effort**: 1 day

### Connector Interface & Implementation (5 tasks)

- [ ] **T026** `[P1]` `[backend]` `[test]` Define IConnector interface
  - File: `src/Linksy.Sync/Connectors/IConnector.cs`
  - Define interface methods: GetDeltas, ApplyChange, ValidateAccess, GetServerTime
  - **Effort**: 0.5 day

- [ ] **T027** `[P1]` `[backend]` `[test]` Implement shared connector base class
  - File: `src/Linksy.Sync/Connectors/ConnectorBase.cs`
  - Implement common retry logic (exponential backoff with jitter)
  - **Effort**: 1 day

- [ ] **T028** `[P1]` `[backend]` `[test]` Implement Autodesk Construction Cloud Docs connector stub
  - File: `src/Linksy.Sync/Connectors/AccConnector.cs`
  - Implement IConnector for ACC: GetDeltas, ApplyChange, ValidateAccess with checksum validation
  - **Effort**: 1.5 days

- [ ] **T029** `[P1]` `[backend]` `[test]` Implement SharePoint Online connector stub
  - File: `src/Linksy.Sync/Connectors/SharePointConnector.cs`
  - Implement IConnector for SP: GetDeltas, ApplyChange, ValidateAccess with checksum validation
  - **Effort**: 1.5 days

- [ ] **T030** `[P1]` `[test]` Unit tests: Connector implementations
  - File: `src/Linksy.Api.Tests/ConnectorContractTests.cs`
  - Test GetDeltas, ApplyChange, ValidateAccess, rate-limit backoff
  - **Effort**: 1.5 days

### Durable Functions Orchestration Foundation (5 tasks)

- [ ] **T031** `[P1]` `[backend]` `[test]` Implement Durable Entity for oscillation hold window
  - File: `src/Linksy.Sync/Entities/HoldWindowEntity.cs`
  - Create entity with 5-minute operation window per (tenant, binding, fileId)
  - **Effort**: 1 day

- [ ] **T032** `[P1]` `[backend]` **Implement RunBindingSyncOrchestrator (core orchestration)**
  - File: `src/Linksy.Sync/Functions/RunBindingSyncOrchestrator.cs`
  - Implement orchestrator function with parallel delta fetching and activity fan-out
  - **Effort**: 2 days

- [ ] **T033** `[P1]` `[backend]` **Implement activity functions**
  - Files: `src/Linksy.Sync/Functions/GetAccDeltasActivity.cs`, `GetSpDeltasActivity.cs`, `ApplyChangeActivity.cs`, `BackoffRetryActivity.cs`, `MoveToManualHoldActivity.cs`, `WriteAuditEntryActivity.cs`
  - Implement all 6 activity functions with error handling and state management
  - **Effort**: 2 days

- [ ] **T034** `[P1]` `[backend]` `[test]` Implement queue trigger for manual-holds processing
  - File: `src/Linksy.Sync/Triggers/ManualHoldQueueTrigger.cs`
  - Create function for dequeueing and alerting operators via metrics
  - **Effort**: 0.5 day

- [ ] **T035** `[P1]` `[test]` Unit tests: Durable orchestration and activities
  - File: `src/Linksy.Api.Tests/OrchestrationTests.cs`
  - Test orchestrator logic, activity mocks, hold window behavior, backoff
  - **Effort**: 1.5 days

---

## Phase 2: User Story 1 – Onboarding (P1) (16 days)

### Models & Services (4 tasks)

- [ ] **T036** `[P1]` `[US1]` `[backend]` Create Binding and BindingState models
  - Files: `src/Linksy.Api/Models/Binding.cs`, `BindingState.cs`
  - Define properties for paths, direction, conflict policy, schedule, status
  - **Effort**: 0.5 day

- [ ] **T037** `[P1]` `[US1]` `[backend]` `[test]` Implement BindingService
  - File: `src/Linksy.Api/Services/BindingService.cs`
  - Implement CRUD, validation, and activation with test sync triggering
  - **Effort**: 1 day

- [ ] **T038** `[P1]` `[US1]` `[backend]` `[test]` Implement OnboardingService
  - File: `src/Linksy.Api/Services/OnboardingService.cs`
  - Implement ValidateConnector, DryRunSync, GetOnboardingStatus
  - **Effort**: 1 day

- [ ] **T039** `[P1]` `[US1]` `[backend]` `[test]` Implement ConnectorService
  - File: `src/Linksy.Api/Services/ConnectorService.cs`
  - Implement connector CRUD, health validation, credential management
  - **Effort**: 0.5 day

### API Endpoints (3 tasks)

- [ ] **T040** `[P1]` `[US1]` `[backend]` Implement Connectors endpoints
  - File: `src/Linksy.Api/Endpoints/ConnectorsEndpoints.cs`
  - Implement POST/GET `/api/connectors`, validate, rotate-credentials endpoints
  - **Effort**: 1 day

- [ ] **T041** `[P1]` `[US1]` `[backend]` Implement Bindings endpoints
  - File: `src/Linksy.Api/Endpoints/BindingsEndpoints.cs`
  - Implement POST/GET/PATCH/DELETE `/api/bindings` and `/activate` endpoint
  - **Effort**: 1 day

- [ ] **T042** `[P1]` `[US1]` `[backend]` Implement onboarding workflow endpoints
  - File: `src/Linksy.Api/Endpoints/OnboardingEndpoints.cs`
  - Implement `/onboarding/validate-connector`, `/dry-run`, `/status` endpoints
  - **Effort**: 1 day

### Frontend – Onboarding Wizard (5 tasks)

- [ ] **T043** `[P1]` `[US1]` `[frontend]` Create OnboardingWizard component structure
  - File: `src/frontend/src/components/OnboardingWizard.tsx`
  - Multi-step form: Select → Configure → Test → Review & Activate
  - **Effort**: 1 day

- [ ] **T044** `[P1]` `[US1]` `[frontend]` Implement Step 1: Select connectors (OAuth)
  - File: `src/frontend/src/components/OnboardingWizard.tsx`
  - UI to list/register connectors with OAuth flow and health status
  - **Effort**: 1 day

- [ ] **T045** `[P1]` `[US1]` `[frontend]` Implement Step 2: Configure binding (paths, conflict policy, direction)
  - File: `src/frontend/src/components/OnboardingWizard.tsx`
  - UI for folder selection, direction, and conflict policy configuration
  - **Effort**: 1 day

- [ ] **T046** `[P1]` `[US1]` `[frontend]` Implement Step 3: Test access (dry-run)
  - File: `src/frontend/src/components/OnboardingWizard.tsx`
  - Call validate/dry-run endpoints; display results and file preview
  - **Effort**: 1 day

- [ ] **T047** `[P1]` `[US1]` `[frontend]` Implement Step 4: Review and activate
  - File: `src/frontend/src/components/OnboardingWizard.tsx`
  - Display summary; call activate endpoint; show success/redirect
  - **Effort**: 0.5 day

### Integration & Testing (2 tasks)

- [ ] **T048** `[P1]` `[US1]` `[test]` Unit tests: OnboardingWizard component
  - File: `src/frontend/src/components/__tests__/OnboardingWizard.test.tsx`
  - Test form transitions, validation errors, API calls using React Testing Library
  - **Effort**: 1 day

- [ ] **T049** `[P1]` `[US1]` `[test]` End-to-end test: Onboarding workflow (sandbox)
  - File: `specs/001-aec-file-sync/quickstart.md` (manual test steps)
  - Document and execute: Sign in → Register connectors → Create binding → Activate
  - **Effort**: 1 day

---

## Phase 3: User Story 2 – Monitoring & Job Management (P2) (17 days)

### Models & Services (3 tasks)

- [ ] **T050** `[P2]` `[US2]` `[backend]` Create Job, ChangeItem, and Quarantine models
  - Files: `src/Linksy.Api/Models/Job.cs`, `ChangeItem.cs`, `Quarantine.cs`
  - Define properties for status, stats, retries, quarantine metadata
  - **Effort**: 0.5 day

- [ ] **T051** `[P2]` `[US2]` `[backend]` `[test]` Implement JobService
  - File: `src/Linksy.Api/Services/JobService.cs`
  - Implement GetJob, ListJobs, TriggerManualSync, GetJobStats, ListChangeItems
  - **Effort**: 1 day

- [ ] **T052** `[P2]` `[US2]` `[backend]` `[test]` Implement ConflictResolutionService and QuarantineService
  - Files: `src/Linksy.Api/Services/ConflictResolutionService.cs`, `QuarantineService.cs`
  - Implement ResolveConflict, ListQuarantined, RestoreFile, PermanentlyDelete
  - **Effort**: 1 day

### API Endpoints (5 tasks)

- [ ] **T053** `[P2]` `[US2]` `[backend]` Implement Jobs endpoints
  - File: `src/Linksy.Api/Endpoints/JobsEndpoints.cs`
  - Implement GET/POST `/api/jobs`, GET/POST `/api/jobs/{bindingId}/manual-sync`, etc.
  - **Effort**: 1 day

- [ ] **T054** `[P2]` `[US2]` `[backend]` Implement Activity Log endpoints
  - File: `src/Linksy.Api/Endpoints/ActivityLogEndpoints.cs`
  - Implement GET `/api/activity-log` with filtering by binding, event type, date
  - **Effort**: 0.5 day

- [ ] **T055** `[P2]` `[US2]` `[backend]` Implement Conflict endpoints
  - File: `src/Linksy.Api/Endpoints/ConflictEndpoints.cs`
  - Implement GET `/api/conflicts/manual-holds`, POST `/api/conflicts/{itemId}/resolve`
  - **Effort**: 1 day

- [ ] **T056** `[P2]` `[US2]` `[backend]` Implement Quarantine endpoints
  - File: `src/Linksy.Api/Endpoints/QuarantineEndpoints.cs`
  - Implement GET/POST/DELETE `/api/quarantine` for listing, restoring, purging
  - **Effort**: 1 day

- [ ] **T057** `[P2]` `[US2]` `[backend]` `[test]` Unit tests: Job management endpoints
  - File: `src/Linksy.Api.Tests/JobsEndpointsTests.cs`
  - Test listing, filtering, manual sync, role-based access
  - **Effort**: 1 day

### Frontend – Activity Log & Conflict Resolution (5 tasks)

- [ ] **T058** `[P2]` `[US2]` `[frontend]` Create ActivityLog component
  - File: `src/frontend/src/components/ActivityLog.tsx`
  - Display job timeline with status badges, filtering by binding/date/status
  - **Effort**: 1.5 days

- [ ] **T059** `[P2]` `[US2]` `[frontend]` Create JobDetails modal
  - File: `src/frontend/src/components/JobDetails.tsx`
  - Display job stats, change list, propagation timeline
  - **Effort**: 1 day

- [ ] **T060** `[P2]` `[US2]` `[frontend]` Create ConflictResolver component
  - File: `src/frontend/src/components/ConflictResolver.tsx`
  - Display ManualHold items with side-by-side comparison and resolution UI
  - **Effort**: 1 day

- [ ] **T061** `[P2]` `[US2]` `[frontend]` Create QuarantineView component (Deleted Items)
  - File: `src/frontend/src/components/QuarantineView.tsx`
  - Display quarantined files with filtering and one-click restore
  - **Effort**: 1 day

- [ ] **T062** `[P2]` `[US2]` `[frontend]` `[test]` Unit tests: Job monitoring components
  - File: `src/frontend/src/components/__tests__/ActivityLog.test.tsx`, etc.
  - Test rendering, filtering, API calls, user interactions
  - **Effort**: 1 day

### Observability & Monitoring (2 tasks)

- [ ] **T063** `[P2]` `[US2]` `[backend]` Emit job metrics to Application Insights
  - Files: `src/Linksy.Sync/Functions/*.cs`
  - Log metrics: FilesProcessed, BytesTransferred, ConflictCount, RetryCount, ThrottleCount
  - **Effort**: 1 day

- [ ] **T064** `[P2]` `[US2]` `[backend]` Implement alert rules for job failures and throttling
  - File: `infra/main.bicep` (App Insights alert rules)
  - Create alerts for job success rate and throttling spikes
  - **Effort**: 1 day

---

## Phase 4: User Story 3 – Audit & Compliance (P3) (12 days)

### Models & Services (3 tasks)

- [ ] **T065** `[P3]` `[US3]` `[backend]` Create AuditEntry model with hash chain
  - File: `src/Linksy.Api/Models/AuditEntry.cs`
  - Implement hash chain computation (SHA-256) for tamper detection
  - **Effort**: 1 day

- [ ] **T066** `[P3]` `[US3]` `[backend]` `[test]` Implement AuditService
  - File: `src/Linksy.Api/Services/AuditService.cs`
  - Implement WriteAuditEntry, GetAuditEntries, ExportAuditLog, ValidateAuditChain
  - **Effort**: 1.5 days

- [ ] **T067** `[P3]` `[US3]` `[backend]` `[test]` Implement audit export to Blob Storage
  - File: `src/Linksy.Api/Services/AuditService.cs`
  - ExportAuditLog writes JSON to Blob with WORM + versioning
  - **Effort**: 1 day

### API Endpoints (2 tasks)

- [ ] **T068** `[P3]` `[US3]` `[backend]` Implement Audit endpoints
  - File: `src/Linksy.Api/Endpoints/AuditEndpoints.cs`
  - Implement GET `/api/audit-log`, POST `/api/audit-log/export`, `/validate-chain`
  - **Effort**: 1 day

- [ ] **T069** `[P3]` `[US3]` `[backend]` `[test]` Unit tests: Audit endpoints
  - File: `src/Linksy.Api.Tests/AuditEndpointsTests.cs`
  - Test listing, filtering, export, chain validation, RBAC
  - **Effort**: 1 day

### Frontend – Audit Log & Compliance (3 tasks)

- [ ] **T070** `[P3]` `[US3]` `[frontend]` Create AuditLog component
  - File: `src/frontend/src/components/AuditLog.tsx`
  - Display audit table with filtering by binding, date, action type
  - **Effort**: 1 day

- [ ] **T071** `[P3]` `[US3]` `[frontend]` Create AuditExporter component
  - File: `src/frontend/src/components/AuditExporter.tsx`
  - UI to select export parameters and download JSON file
  - **Effort**: 1 day

- [ ] **T072** `[P3]` `[US3]` `[frontend]` Create ComplianceReport component
  - File: `src/frontend/src/components/ComplianceReport.tsx`
  - Display compliance verification and audit chain validation results
  - **Effort**: 1 day

### Data Export & Testing (2 tasks)

- [ ] **T073** `[P3]` `[US3]` `[backend]` Implement WORM + versioning validation
  - File: `src/Linksy.Api/Services/ComplianceService.cs`
  - Verify Blob policies; document compliance checklist
  - **Effort**: 0.5 day

- [ ] **T074** `[P3]` `[US3]` `[test]` Test audit export and immutability
  - File: `src/Linksy.Api.Tests/ComplianceTests.cs`
  - Test export generation, WORM enforcement, no file content
  - **Effort**: 1 day

---

## Phase 5: Polish & Operations (21 days)

### Documentation & Guides (4 tasks)

- [ ] **T075** `[P3]` `[docs]` Create data model documentation
  - File: `specs/001-aec-file-sync/data-model.md`
  - Document all entities, relationships, state transitions, validation rules
  - **Effort**: 1 day

- [ ] **T076** `[P3]` `[docs]` Create operational playbooks
  - Files: `specs/001-aec-file-sync/playbooks/credential-rotation.md`, etc.
  - Step-by-step procedures for operators and support teams
  - **Effort**: 1.5 days

- [ ] **T077** `[P3]` `[docs]` Create developer guide
  - File: `specs/001-aec-file-sync/dev-guide.md`
  - Local setup, testing, adding connectors, debugging traces
  - **Effort**: 1 day

- [ ] **T078** `[P3]` `[docs]` Update quickstart guide with all workflows
  - File: `specs/001-aec-file-sync/quickstart.md`
  - Add sections: Setup, Onboarding, Job Management, Monitoring, Audit
  - **Effort**: 1 day

### Observability & Logging (3 tasks)

- [ ] **T079** `[P3]` `[backend]` Implement comprehensive structured logging
  - Files: `src/Linksy.Api/Program.cs`, `src/Linksy.Sync/Program.cs`
  - Add correlation IDs, job lifecycle logs, API call logging
  - **Effort**: 1 day

- [ ] **T080** `[P3]` `[backend]` Create Application Insights dashboard
  - File: `infra/monitoring-dashboard.bicep`
  - Dashboard: Job success rate, latency, throttle events, ManualHold count
  - **Effort**: 1 day

- [ ] **T081** `[P3]` `[backend]` Implement health check endpoints
  - File: `src/Linksy.Api/Endpoints/HealthEndpoints.cs`
  - Implement `/health` and `/health/deep` endpoints
  - **Effort**: 0.5 day

### Performance & Resilience (3 tasks)

- [ ] **T082** `[P3]` `[backend]` Add caching layer for connector metadata
  - File: `src/Linksy.Api/Services/ConnectorService.cs`
  - Cache connector details (TTL 5 minutes) with invalidation
  - **Effort**: 0.5 day

- [ ] **T083** `[P3]` `[backend]` Implement circuit breaker for external APIs
  - File: `src/Linksy.Sync/Connectors/ConnectorBase.cs`
  - Use Polly library for circuit breaker pattern
  - **Effort**: 1 day

- [ ] **T084** `[P3]` `[backend]` `[test]` Load test: Job orchestration under concurrent bindings
  - File: `src/Linksy.Api.Tests/LoadTests.cs`
  - Simulate 100+ concurrent bindings; verify SC-003 compliance
  - **Effort**: 1.5 days

### Frontend Refinements (4 tasks)

- [ ] **T085** `[P3]` `[frontend]` Implement token refresh UI flow
  - File: `src/frontend/src/lib/api.ts`
  - Intercept 401; auto-refresh token and retry
  - **Effort**: 0.5 day

- [ ] **T086** `[P3]` `[frontend]` Add loading and error states to all components
  - Files: `src/frontend/src/components/*.tsx`
  - Use shadcn/ui Spinner for loading; ErrorAlert for errors
  - **Effort**: 1 day

- [ ] **T087** `[P3]` `[frontend]` Implement dark mode support
  - Files: `src/frontend/src/index.css`, `tailwind.config.ts`
  - Configure Tailwind dark mode; add theme toggle
  - **Effort**: 0.5 day

- [ ] **T088** `[P3]` `[frontend]` Add accessibility improvements (a11y)
  - Files: `src/frontend/src/components/**/*.tsx`
  - Add ARIA labels, keyboard navigation, screen reader testing
  - **Effort**: 1 day

### Testing & QA (4 tasks)

- [ ] **T089** `[P3]` `[test]` Integration test suite: Full workflow end-to-end
  - File: `src/Linksy.Api.Tests/E2eTests.cs`
  - Test complete journey: Sign in → Onboard → Sync → Monitor → Resolve → Audit
  - **Effort**: 2 days

- [ ] **T090** `[P3]` `[test]` Performance test: Sync latency and throughput
  - File: `src/Linksy.Api.Tests/PerformanceTests.cs`
  - Measure end-to-end latency and throughput; verify SC-003
  - **Effort**: 1.5 days

- [ ] **T091** `[P3]` `[test]` Security test: RBAC enforcement and audit trail
  - File: `src/Linksy.Api.Tests/SecurityTests.cs`
  - Verify role enforcement and audit coverage
  - **Effort**: 1 day

- [ ] **T092** `[P3]` `[test]` Manual testing checklist (QA)
  - File: `specs/001-aec-file-sync/testing-checklist.md`
  - Document smoke/regression tests with expected results
  - **Effort**: 0.5 day

### Deployment & Handoff (3 tasks)

- [ ] **T093** `[P3]` `[infra]` Create production IaC and deployment pipeline
  - Files: `infra/parameters.prod.json`, `.github/workflows/deploy-prod.yml`
  - Production Bicep; deploy script with validation
  - **Effort**: 1.5 days

- [ ] **T094** `[P3]` `[docs]` Create runbooks for Ops team
  - Files: `specs/001-aec-file-sync/runbooks/scaling.md`, etc.
  - Procedures for scaling, incident response, credential rotation
  - **Effort**: 1 day

- [ ] **T095** `[P3]` `[docs]` Create user onboarding guide for customers
  - File: `specs/001-aec-file-sync/customer-guide.md`
  - How to sign up, authenticate, troubleshoot with screenshots
  - **Effort**: 1 day

---

## Effort Summary

| Phase | Tasks | Effort | Duration |
|-------|-------|--------|----------|
| Phase 0 (Setup) | 9 | 11 days | 2 weeks |
| Phase 1 (Foundation) | 26 | 26 days | 4-5 weeks |
| Phase 2 (US1 Onboarding) | 16 | 16 days | 2-3 weeks |
| Phase 3 (US2 Monitoring) | 17 | 17 days | 2-3 weeks |
| Phase 4 (US3 Audit) | 12 | 12 days | 2 weeks |
| Phase 5 (Polish) | 21 | 21 days | 3 weeks |
| **TOTAL** | **102** | **~103 days** | **6-7 months** (with parallelization) |

---

## Success Criteria Mapping

| Success Criterion | Key Tasks |
|-------------------|-----------|
| **SC-001**: 90% users onboard in <10 min | T043-T048 (Wizard UX) |
| **SC-002**: 99.5% daily job success | T032-T035 (Orchestration) |
| **SC-003**: 95% of changes in <10 min | T032-T034 (Latency) |
| **SC-004**: ≥4/5 customer satisfaction | T058-T062 (UI/UX) |
| **SC-005**: Zero file content persisted | T065-T074 (Audit) |

---

## Notes & Assumptions

- **Test Data**: Sandbox ACC and SharePoint accounts available
- **External APIs**: ACC and SP APIs stable; plan includes fault tolerance
- **Team Composition**: 2 backend, 1 frontend, 1 DevOps engineer
- **Deployment Target**: Azure (dev and prod tenants)
- **Scope Exclusions**: Multi-region DR, webhook hardening, connector SDK marketplace, API Management

