# Phase 2 Tasks: AEC File Sync Platform

**Date**: 2025-10-25  
**Status**: Task Generation Complete  
**Input**: spec.md (3 user stories, 16 FR), plan.md (tech stack, project structure), research.md (8 design decisions)  
**Next Phase**: Phase 3 Implementation (code generation from these tasks)

---

## Overview

This document breaks down the AEC File Sync Platform into executable tasks organized by implementation phase and user story. Each task includes:
- **Task ID** (T[Phase][Category][Number]): Unique identifier
- **Priority**: P1 (critical path), P2 (required), P3 (polish/optional)
- **Story**: Which user story(ies) this task supports
- **Description**: Concrete deliverable
- **File Paths**: Specific locations for implementation
- **Dependencies**: Blocking tasks (if any)
- **Test Criteria**: How to validate completion

**Total Tasks**: ~75 across 6 implementation phases  
**Est. Duration**: 8-12 weeks for pilot (10-50 tenants)  
**Parallelization**: Tasks within Phase 2-3 can run in parallel once Phase 1 is complete

---

## Phase 1: Project Setup & Infrastructure

**Goal**: Initialize project structure, database schema, authentication framework, and local development environment.  
**Duration**: 1 week  
**Deliverables**: Solution compiles, database migrations run, Aspire orchestration works, frontend dev server runs

### 1.1 Backend Project Initialization

- [ ] **T1-001** [P1] Setup: Create `Linksy.Api.csproj` with ASP.NET Core 9.0 Minimal API template, reference `Linksy.ServiceDefaults`
  - **File**: `src/Linksy.Api/Linksy.Api.csproj`
  - **Dependencies**: None (first backend task)
  - **Test**: `dotnet build src/Linksy.Api` succeeds with zero warnings

- [ ] **T1-002** [P1] Setup: Add NuGet packages (EF Core 9.0, ASP.NET Core Identity, IdentityModel, OpenTelemetry, xUnit)
  - **File**: `src/Linksy.Api/Linksy.Api.csproj` (PackageReference additions)
  - **Dependencies**: T1-001
  - **Test**: `dotnet restore` completes; packages resolve with correct versions

- [ ] **T1-003** [P1] Setup: Create SQL Server LocalDB connection string in `appsettings.Development.json` with MSSQLLocalDB instance
  - **File**: `src/Linksy.Api/appsettings.Development.json`
  - **Dependencies**: None
  - **Test**: Connection string parses; LocalDB instance is accessible

- [ ] **T1-004** [P1] Setup: Create ApplicationDbContext with EF Core DbSet properties for all 9 entities (Tenant, ApplicationUser, TenantUserRole, Connector, Binding, SyncJob, ChangeItem, CredentialSecret, AuditEntry)
  - **File**: `src/Linksy.Api/Data/ApplicationDbContext.cs`
  - **Dependencies**: None (datamodel independent)
  - **Test**: `dotnet ef dbcontext info` reports DbContext; all DbSets are discoverable

- [ ] **T1-005** [P1] Setup: Create initial EF Core migration `001_InitialSchema` with all entity tables, indexes, and constraints (including append-only audit table constraint)
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_InitialSchema.cs`
  - **Dependencies**: T1-004
  - **Test**: `dotnet ef database update` creates schema; `SELECT * FROM __EFMigrationsHistory` shows applied migration

### 1.2 Identity Framework Setup (ASP.NET Core Identity + JWT)

- [ ] **T1-006** [P1] Auth: Configure ASP.NET Core Identity in `Program.cs` with ApplicationUser, IdentityRole, and custom user store (SQL Server)
  - **File**: `src/Linksy.Api/Program.cs`
  - **Dependencies**: T1-004, T1-005
  - **Test**: `app.MapIdentityApi<ApplicationUser>()` works; identity endpoints compile

- [ ] **T1-007** [P1] Auth: Create JwtTokenService to generate JWT tokens with claims (sub, email, display_name, roles) and 12-hour expiration
  - **File**: `src/Linksy.Api/Services/JwtTokenService.cs`
  - **Dependencies**: T1-006
  - **Test**: Service generates valid JWT; token contains correct claims; expiration is 12 hours

- [ ] **T1-008** [P1] Auth: Configure JWT Bearer authentication middleware in `Program.cs` to validate tokens from `Authorization: Bearer <token>` header
  - **File**: `src/Linksy.Api/Program.cs`
  - **Dependencies**: T1-007
  - **Test**: Unauthenticated requests return 401; valid tokens pass through

- [ ] **T1-009** [P2] Auth: Add refresh token endpoint `POST /auth/refresh` to issue new access tokens (no user re-authentication required)
  - **File**: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - **Dependencies**: T1-007, T1-008
  - **Test**: Refresh token reissues access token with same claims; expired access tokens trigger 401 → refresh succeeds

### 1.3 RBAC Setup (Tenant-Scoped Roles)

- [ ] **T1-010** [P1] Auth: Create TenantUserRole junction entity linking ApplicationUser, Tenant, and IdentityRole with role name constants (TenantAdmin, Operator, Auditor)
  - **File**: `src/Linksy.Api/Models/TenantUserRole.cs`
  - **Dependencies**: None (model definition)
  - **Test**: Model compiles; EF recognizes as junction entity with FK constraints

- [ ] **T1-011** [P1] Auth: Create migration `002_AddTenantRoles` to add TenantUserRole table with composite key (TenantId, UserId, RoleId)
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_AddTenantRoles.cs`
  - **Dependencies**: T1-005, T1-010
  - **Test**: `dotnet ef database update` creates TenantUserRole table with constraints

- [ ] **T1-012** [P1] Auth: Create AuthorizationService to check if user has required role for tenant (validates JWT claims against TenantUserRole table)
  - **File**: `src/Linksy.Api/Services/AuthorizationService.cs`
  - **Dependencies**: T1-010, T1-011
  - **Test**: Service correctly identifies role membership; returns false for users without role; returns false for wrong tenant

- [ ] **T1-013** [P2] Auth: Implement RBAC middleware `[Authorize(Roles = "TenantAdmin,Operator")]` filter on protected endpoints
  - **File**: `src/Linksy.Api/Middleware/RbacMiddleware.cs`
  - **Dependencies**: T1-012
  - **Test**: Requests with required role pass; requests without role return 403

### 1.4 Frontend Project Initialization

- [ ] **T1-014** [P1] Setup: Create React 19 + Vite + TypeScript project in `src/frontend/` with `npm create vite@latest` template
  - **File**: `src/frontend/package.json`, `src/frontend/tsconfig.json`
  - **Dependencies**: None (frontend independent)
  - **Test**: `npm run dev` in `src/frontend/` starts dev server on port 5173; ESLint passes with zero errors

- [ ] **T1-015** [P1] Setup: Add dependencies (axios, React Router, shadcn/ui, Tailwind CSS 4) to `src/frontend/package.json`
  - **File**: `src/frontend/package.json`
  - **Dependencies**: T1-014
  - **Test**: `npm install` succeeds; all packages resolve; TypeScript `tsc --noEmit` passes

- [ ] **T1-016** [P1] Setup: Configure Vite environment variables for API base URL (`VITE_API_URL=http://localhost:5000`)
  - **File**: `src/frontend/vite.config.ts`, `.env.development`
  - **Dependencies**: T1-014
  - **Test**: `import.meta.env.VITE_API_URL` resolves correctly during dev build

- [ ] **T1-017** [P2] Setup: Create axios instance with JWT token interceptor (reads from cookie, handles 401 → refresh flow)
  - **File**: `src/frontend/src/lib/api.ts`
  - **Dependencies**: T1-016
  - **Test**: Axios requests include `Authorization` header; 401 responses trigger refresh; retried request succeeds

### 1.5 Aspire Orchestration

- [ ] **T1-018** [P1] Setup: Configure `Linksy.AppHost` to orchestrate backend API and frontend React dev server
  - **File**: `src/Linksy.AppHost/AppHost.cs`
  - **Dependencies**: T1-002, T1-014
  - **Test**: `dotnet run --project src/Linksy.AppHost` starts both services; Aspire dashboard available on port 15217

- [ ] **T1-019** [P2] Setup: Inject CORS policy in backend to allow frontend origin (`http://localhost:5173`) only
  - **File**: `src/Linksy.Api/Program.cs` (`.AddCors()`)
  - **Dependencies**: T1-018
  - **Test**: OPTIONS request from frontend succeeds; request from external origin blocked

### 1.6 Testing Framework Setup

- [ ] **T1-020** [P2] Setup: Create `Linksy.Api.Tests` xUnit project with Moq for mocking services
  - **File**: `src/Linksy.Api.Tests/Linksy.Api.Tests.csproj`
  - **Dependencies**: T1-001
  - **Test**: `dotnet test src/Linksy.Api.Tests` discovers and runs tests

- [ ] **T1-021** [P2] Setup: Create Vitest configuration for frontend unit tests with React Testing Library
  - **File**: `src/frontend/vitest.config.ts`, `src/frontend/tsconfig.json` (test settings)
  - **Dependencies**: T1-015
  - **Test**: `npm run test` in `src/frontend/` runs tests; zero errors

- [ ] **T1-022** [P3] Setup: Create integration test template for full stack (API + database + frontend)
  - **File**: `src/Linksy.Api.Tests/IntegrationTestBase.cs`
  - **Dependencies**: T1-020, T1-018
  - **Test**: Template compiles; integration test can initialize DB

---

## Phase 2: Authentication & Authorization Endpoints

**Goal**: Implement login, registration, and token refresh endpoints; validate JWT flow end-to-end.  
**Duration**: 1 week  
**Deliverables**: Auth endpoints work; frontend login form authenticates users; JWT lifecycle validated

### 2.1 Entra ID SSO Integration

- [ ] **T2-001** [P1] Auth: Register headless app in Entra ID (Azure AD); capture client ID, tenant ID, and client secret
  - **File**: Setup artifact (not code); document in docs/identity-framework.md
  - **Dependencies**: None
  - **Test**: Client credentials work in OAuth2 device flow or client credentials flow

- [ ] **T2-002** [P1] Auth: Implement Entra ID token exchange endpoint `POST /auth/entra-callback` that accepts auth code and returns JWT
  - **File**: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - **Dependencies**: T1-007, T1-008, T2-001
  - **Test**: POST with valid Entra ID auth code returns JWT; JWT contains user claims from Entra ID

- [ ] **T2-003** [P2] Auth: Add user provisioning logic to `POST /auth/entra-callback` that creates ApplicationUser + TenantUserRole on first login (assigns default role)
  - **File**: `src/Linksy.Api/Services/UserProvisioningService.cs`
  - **Dependencies**: T2-002
  - **Test**: First-time user login auto-creates user; subsequent logins reuse same user

### 2.2 Login & Registration Endpoints

- [ ] **T2-004** [P1] Auth: Implement `POST /auth/login` endpoint that accepts email + password (for non-SSO internal testing)
  - **File**: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - **Dependencies**: T1-008
  - **Test**: Valid credentials return JWT; invalid credentials return 401

- [ ] **T2-005** [P1] Auth: Implement `POST /auth/register` endpoint that creates new ApplicationUser with email + password
  - **File**: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - **Dependencies**: T2-004
  - **Test**: New user registration succeeds; duplicate email fails; password hashing validated

- [ ] **T2-006** [P1] Auth: Implement `POST /auth/logout` endpoint that invalidates tokens (optional; HttpOnly cookies can rely on expiration)
  - **File**: `src/Linksy.Api/Endpoints/AuthEndpoints.cs`
  - **Dependencies**: T2-004
  - **Test**: Logout succeeds; subsequent requests with old token fail (or return 401)

### 2.3 Token Refresh & Lifecycle

- [ ] **T2-007** [P1] Auth: Implement server-side refresh token storage in database (RefreshToken table: token_id, user_id, expires_at, revoked_at)
  - **File**: `src/Linksy.Api/Models/RefreshToken.cs`
  - **Dependencies**: T1-004
  - **Test**: RefreshToken model compiles; EF recognizes entity

- [ ] **T2-008** [P1] Auth: Update `POST /auth/refresh` to rotate refresh tokens (issue new refresh token, revoke old one)
  - **File**: `src/Linksy.Api/Services/JwtTokenService.cs`
  - **Dependencies**: T1-009, T2-007
  - **Test**: Refresh token changes each call; old token is revoked; new access token is valid

- [ ] **T2-009** [P2] Auth: Add token validation unit tests (expiration, claims, signature validation)
  - **File**: `src/Linksy.Api.Tests/AuthTests.cs`
  - **Dependencies**: T1-020, T2-008
  - **Test**: Token tests pass with 95%+ code coverage on JwtTokenService

### 2.4 Frontend Auth Integration

- [ ] **T2-010** [P1] UI: Create Auth context hook `useAuth()` to manage user state, login, logout, and token refresh
  - **File**: `src/frontend/src/contexts/AuthContext.tsx`
  - **Dependencies**: T1-017
  - **Test**: Hook provides user, isLoading, login, logout functions; updates on auth state change

- [ ] **T2-011** [P1] UI: Create Login component with email + password form, submit to `POST /auth/login`, store JWT in memory or HttpOnly cookie
  - **File**: `src/frontend/src/components/Auth/LoginForm.tsx`
  - **Dependencies**: T2-010, T2-004
  - **Test**: Form submits credentials; success redirects to dashboard; error displays message

- [ ] **T2-012** [P1] UI: Create ProtectedRoute wrapper to enforce authentication; unauthenticated users redirect to login
  - **File**: `src/frontend/src/components/Auth/ProtectedRoute.tsx`
  - **Dependencies**: T2-010
  - **Test**: Unauthenticated access redirects to login; authenticated access allows; logout redirects to login

- [ ] **T2-013** [P2] UI: Implement token refresh flow in axios interceptor (auto-refresh on 401, retry request)
  - **File**: `src/frontend/src/lib/api.ts` (enhance T1-017)
  - **Dependencies**: T2-010, T1-017
  - **Test**: 401 response triggers refresh; retried request succeeds with new token

- [ ] **T2-014** [P3] Test: Write frontend auth integration tests (login flow, token refresh, redirect on logout)
  - **File**: `src/frontend/src/components/Auth/Auth.test.tsx`
  - **Dependencies**: T2-011, T2-012, T2-013
  - **Test**: All auth flows pass; 90%+ code coverage

---

## Phase 3: User Story 1 - Onboarding (Priority: P1)

**Goal**: Enable tenant admins to complete onboarding wizard, validate connector access, and create initial binding.  
**Duration**: 2 weeks  
**Deliverables**: Full wizard end-to-end; both connectors validate; binding created and sync ready

### 3.1 Connector Models & Validation

- [ ] **T3-001** [P1] [US1] Model: Create Connector entity (connector_id, tenant_id, connector_type [ACC/SharePoint], health_status, last_health_check_at, created_at)
  - **File**: `src/Linksy.Api/Models/Connector.cs`
  - **Dependencies**: T1-004
  - **Test**: Model compiles; EF recognizes FK to Tenant

- [ ] **T3-002** [P1] [US1] Model: Create CredentialSecret entity (id, tenant_id, connector_id, credential_type [OAuth], encrypted_payload, key_version, rotation_requested_at, created_at)
  - **File**: `src/Linksy.Api/Models/CredentialSecret.cs`
  - **Dependencies**: T1-004, T3-001
  - **Test**: Model compiles; includes EF attribute for encrypted column (immutable)

- [ ] **T3-003** [P1] [US1] Service: Create ConnectorValidationService to test OAuth credentials against ACC/SharePoint APIs
  - **File**: `src/Linksy.Api/Services/ConnectorValidationService.cs`
  - **Dependencies**: T3-002
  - **Test**: Service validates valid credentials; rejects invalid ones; logs attempt with timestamp

- [ ] **T3-004** [P1] [US1] Service: Create CredentialVaultService to encrypt/decrypt credentials, manage key versions, and rotation tracking
  - **File**: `src/Linksy.Api/Services/CredentialVaultService.cs`
  - **Dependencies**: T3-002, T3-003
  - **Test**: Encryption round-trip succeeds; tenant isolation enforced; key versioning tracks rotations

### 3.2 Binding Models & Database

- [ ] **T3-005** [P1] [US1] Model: Create Binding entity (binding_id, tenant_id, connector_source_id, connector_target_id, source_path, target_path, direction [bidirectional/one-way], conflict_policy [SourceWins/TargetWins/LastWriterWins/ManualHold], schedule_cadence, is_active, created_at, last_synced_at)
  - **File**: `src/Linksy.Api/Models/Binding.cs`
  - **Dependencies**: T1-004, T3-001
  - **Test**: Model compiles; FKs to Connector verified

- [ ] **T3-006** [P1] [US1] Migration: Create `003_AddBindings` migration with Binding table, indexes on (tenant_id, is_active), constraint on direction + conflict_policy combo
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_AddBindings.cs`
  - **Dependencies**: T3-005
  - **Test**: Migration applies; indexes created; queries on active bindings are efficient

- [ ] **T3-007** [P1] [US1] Model: Create BindingHoldState entity to track 5-minute conflict hold windows (binding_id, file_id, hold_until_at, conflicted_at, resolved_policy)
  - **File**: `src/Linksy.Api/Models/BindingHoldState.cs`
  - **Dependencies**: T1-004, T3-005
  - **Test**: Model compiles; used to query active holds during sync

- [ ] **T3-008** [P2] [US1] Migration: Create `004_AddBindingHoldState` migration
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_AddBindingHoldState.cs`
  - **Dependencies**: T3-007
  - **Test**: Migration applies; queries on active holds return correct results

### 3.3 Onboarding Endpoints

- [ ] **T3-009** [P1] [US1] Endpoint: `POST /api/onboarding/start` - Initiate onboarding for tenant (returns wizard session ID)
  - **File**: `src/Linksy.Api/Endpoints/OnboardingEndpoints.cs`
  - **Dependencies**: T1-012, T2-003
  - **Test**: Requires TenantAdmin role; returns session ID; session is storable

- [ ] **T3-010** [P1] [US1] Endpoint: `POST /api/onboarding/validate-connectors` - Validate ACC + SharePoint credentials and return connector IDs
  - **File**: `src/Linksy.Api/Endpoints/OnboardingEndpoints.cs`
  - **Dependencies**: T3-003, T3-004, T3-009
  - **Test**: Valid credentials return connector IDs; invalid credentials return 400 with error details

- [ ] **T3-011** [P1] [US1] Endpoint: `POST /api/onboarding/create-binding` - Create binding with validated connectors, paths, and initial conflict policy
  - **File**: `src/Linksy.Api/Endpoints/OnboardingEndpoints.cs`
  - **Dependencies**: T3-005, T3-010
  - **Test**: Returns binding_id; binding is queryable; can trigger immediate sync

- [ ] **T3-012** [P1] [US1] Endpoint: `POST /api/onboarding/test-access` - Test read/write on both connectors' sandbox folders
  - **File**: `src/Linksy.Api/Endpoints/OnboardingEndpoints.cs`
  - **Dependencies**: T3-009, T3-010
  - **Test**: Returns { acc_readable: bool, acc_writable: bool, sharepoint_readable: bool, sharepoint_writable: bool }

### 3.4 Onboarding Frontend

- [ ] **T3-013** [P1] [US1] UI: Create Onboarding wizard component with 4 steps (credentials, test access, paths, review)
  - **File**: `src/frontend/src/components/Onboarding/OnboardingWizard.tsx`
  - **Dependencies**: T2-012, T1-017
  - **Test**: All 4 steps render; navigation works; form state persists across steps

- [ ] **T3-014** [P1] [US1] UI: Implement step 1 (ACC + SharePoint credential input with OAuth flow)
  - **File**: `src/frontend/src/components/Onboarding/CredentialsStep.tsx`
  - **Dependencies**: T3-013
  - **Test**: OAuth redirect works; returns auth code; credentials stored securely

- [ ] **T3-015** [P1] [US1] UI: Implement step 2 (test access validation, displays success/failure per platform)
  - **File**: `src/frontend/src/components/Onboarding/TestAccessStep.tsx`
  - **Dependencies**: T3-012, T3-013
  - **Test**: Calls `/api/onboarding/test-access`; displays pass/fail per platform; allows retry

- [ ] **T3-016** [P1] [US1] UI: Implement step 3 (path selection, conflict policy choice, direction toggle)
  - **File**: `src/frontend/src/components/Onboarding/PathsStep.tsx`
  - **Dependencies**: T3-013
  - **Test**: Path selectors are functional; conflict policy dropdown works; direction toggle toggles

- [ ] **T3-017** [P1] [US1] UI: Implement step 4 (review summary, submit to create binding, redirect to activity log)
  - **File**: `src/frontend/src/components/Onboarding/ReviewStep.tsx`
  - **Dependencies**: T3-011, T3-016
  - **Test**: Summary displays all selected options; submit calls create-binding; redirect on success

- [ ] **T3-018** [P2] [US1] UI: Add form validation and error handling for all steps
  - **File**: `src/frontend/src/components/Onboarding/OnboardingWizard.tsx` (enhance)
  - **Dependencies**: T3-013
  - **Test**: Invalid inputs show error messages; form not submittable until valid

- [ ] **T3-019** [P3] [US1] Test: Write integration tests for onboarding flow (create tenant, provision user, complete wizard)
  - **File**: `src/Linksy.Api.Tests/OnboardingIntegrationTests.cs`
  - **Dependencies**: T1-022, T3-011
  - **Test**: Full flow succeeds from login to binding creation

---

## Phase 4: User Story 2 - Monitor & Manage Sync Jobs (Priority: P2)

**Goal**: Enable operators to trigger syncs, monitor progress, resolve conflicts, and manage quarantined items.  
**Duration**: 2.5 weeks  
**Deliverables**: Manual sync trigger works; activity log displays jobs; conflicts can be resolved in UI; deleted items recoverable

### 4.1 Sync Job Models & Database

- [ ] **T4-001** [P2] [US2] Model: Create SyncJob entity (job_id, binding_id, status [queued/running/completed/failed/paused], started_at, completed_at, processed_file_count, error_count, retry_count, triggered_by_user_id, triggered_at)
  - **File**: `src/Linksy.Api/Models/SyncJob.cs`
  - **Dependencies**: T1-004, T3-005
  - **Test**: Model compiles; EF recognizes relationships

- [ ] **T4-002** [P2] [US2] Model: Create ChangeItem entity (item_id, job_id, source_platform [ACC/SharePoint], target_platform, file_path, operation_type [create/update/delete/move/rename], checksum, source_version_id, target_version_id, conflict_detected, conflict_policy_applied, hold_expires_at, created_at)
  - **File**: `src/Linksy.Api/Models/ChangeItem.cs`
  - **Dependencies**: T1-004, T4-001
  - **Test**: Model compiles; includes status tracking for holds

- [ ] **T4-003** [P2] [US2] Migration: Create `005_AddSyncJobsAndChanges` migration with SyncJob and ChangeItem tables
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_AddSyncJobsAndChanges.cs`
  - **Dependencies**: T4-001, T4-002
  - **Test**: Migration applies; queries on job status and hold state are efficient

### 4.2 Sync Engine & Job Execution

- [ ] **T4-004** [P2] [US2] Service: Create SyncEngineService to orchestrate connector polling, delta tracking, conflict detection, and retry logic
  - **File**: `src/Linksy.Api/Services/SyncEngineService.cs`
  - **Dependencies**: T3-003, T4-001, T4-002
  - **Test**: Service accepts binding; returns processed change count; logs all operations

- [ ] **T4-005** [P2] [US2] Service: Create ConflictResolutionService to apply conflict policies (SourceWins, TargetWins, LastWriterWins, ManualHold) and enforce 5-minute hold windows
  - **File**: `src/Linksy.Api/Services/ConflictResolutionService.cs`
  - **Dependencies**: T4-004, T3-007
  - **Test**: Each policy applies correctly; hold window prevents re-application for 5 minutes

- [ ] **T4-006** [P2] [US2] Service: Create RetryService to handle exponential backoff (up to 7 retries over 120+ seconds) for transient failures and HTTP 429 throttling
  - **File**: `src/Linksy.Api/Services/RetryService.cs`
  - **Dependencies**: T4-004
  - **Test**: Backoff timing is exponential; 7th retry escalates to ManualHold; metrics recorded

- [ ] **T4-007** [P2] [US2] Service: Create QuarantineService to move conflicted/failed items to ManualHold state and track 7-day soft-delete window
  - **File**: `src/Linksy.Api/Services/QuarantineService.cs`
  - **Dependencies**: T4-002, T4-005, T4-006
  - **Test**: Items enter quarantine; 7-day expiration tracked; manual resolve removes from quarantine

### 4.3 Sync Endpoints

- [ ] **T4-008** [P2] [US2] Endpoint: `POST /api/bindings/{binding_id}/sync` - Trigger manual sync for binding (creates SyncJob, queues execution)
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T1-012, T4-004
  - **Test**: Requires Operator role; returns job_id immediately; job begins execution

- [ ] **T4-009** [P2] [US2] Endpoint: `GET /api/jobs/{job_id}` - Get job status, progress, and statistics
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-008, T4-001
  - **Test**: Returns status, file count, error details; polling supported

- [ ] **T4-010** [P2] [US2] Endpoint: `GET /api/bindings/{binding_id}/jobs` - List all jobs for a binding with pagination
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-009
  - **Test**: Returns paginated list; filterable by status

- [ ] **T4-011** [P2] [US2] Endpoint: `GET /api/jobs/{job_id}/changes` - Get all change items for a job (paginated, filterable by status and operation_type)
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-002, T4-009
  - **Test**: Returns paginated changes; includes conflict_detected flag

- [ ] **T4-012** [P2] [US2] Endpoint: `POST /api/changes/{item_id}/resolve` - Resolve ManualHold conflict by selecting source or target version
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-005, T4-011
  - **Test**: Requires Operator role; applies decision; removes hold; logs resolution

- [ ] **T4-013** [P2] [US2] Endpoint: `POST /api/changes/{item_id}/restore` - Restore soft-deleted item (remove from quarantine, re-sync to both platforms)
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-007, T4-012
  - **Test**: Requires Operator role; item re-synced; quarantine entry removed

- [ ] **T4-014** [P2] [US2] Endpoint: `GET /api/bindings/{binding_id}/quarantine` - List all quarantined items for a binding (filterable by date range)
  - **File**: `src/Linksy.Api/Endpoints/SyncEndpoints.cs`
  - **Dependencies**: T4-007, T4-013
  - **Test**: Returns only items in quarantine; filters by date work

### 4.4 Real-Time Activity Log (WebSocket)

- [ ] **T4-015** [P2] [US2] Endpoint: `WebSocket /ws/activity?binding_id={id}` - Real-time activity stream for job and change events
  - **File**: `src/Linksy.Api/Endpoints/ActivityEndpoints.cs`
  - **Dependencies**: T4-008, T4-009
  - **Test**: WebSocket connects; receives job status updates; disconnects cleanly

- [ ] **T4-016** [P2] [US2] Service: Create ActivityBroadcastService to emit job and change events to connected WebSocket clients
  - **File**: `src/Linksy.Api/Services/ActivityBroadcastService.cs`
  - **Dependencies**: T4-015
  - **Test**: Events broadcast to all connected clients for binding

### 4.5 Sync Management Frontend

- [ ] **T4-017** [P2] [US2] UI: Create Activity Log component displaying jobs with status, file count, error count, timestamps
  - **File**: `src/frontend/src/components/Activity/ActivityLog.tsx`
  - **Dependencies**: T4-010, T1-017
  - **Test**: Displays all jobs; supports pagination; updates on manual refresh

- [ ] **T4-018** [P2] [US2] UI: Create Job Details panel showing all changes for a job with conflict indicators
  - **File**: `src/frontend/src/components/Activity/JobDetailsPanel.tsx`
  - **Dependencies**: T4-011, T4-017
  - **Test**: Displays changes; highlights conflicts; includes operation types

- [ ] **T4-019** [P2] [US2] UI: Implement WebSocket hook `useActivityStream()` for real-time updates
  - **File**: `src/frontend/src/hooks/useActivityStream.ts`
  - **Dependencies**: T4-015, T4-017
  - **Test**: Hook connects to WebSocket; updates UI on events; reconnects on disconnect

- [ ] **T4-020** [P2] [US2] UI: Create Conflict Resolution modal for ManualHold items (select source or target, confirm)
  - **File**: `src/frontend/src/components/Activity/ConflictModal.tsx`
  - **Dependencies**: T4-012, T4-018
  - **Test**: Modal displays both versions; selection works; submit resolves conflict

- [ ] **T4-021** [P2] [US2] UI: Create Deleted Items view with filters (date range, binding, status)
  - **File**: `src/frontend/src/components/Activity/DeletedItemsView.tsx`
  - **Dependencies**: T4-013, T4-014
  - **Test**: Lists quarantined items; filters work; restore button functional

- [ ] **T4-022** [P2] [US2] UI: Add "Trigger Sync" button on binding details (calls POST /api/bindings/{id}/sync)
  - **File**: `src/frontend/src/components/Bindings/BindingDetails.tsx`
  - **Dependencies**: T4-008, T4-017
  - **Test**: Button visible to Operator role; click triggers sync; activity log updates

- [ ] **T4-023** [P3] [US2] Test: Write integration tests for manual sync, conflict resolution, and quarantine workflows
  - **File**: `src/Linksy.Api.Tests/SyncIntegrationTests.cs`
  - **Dependencies**: T1-022, T4-012, T4-013
  - **Test**: Full sync workflows pass; conflicts resolved correctly

---

## Phase 5: User Story 3 - Audit Compliance Activity (Priority: P3)

**Goal**: Enable auditors to review historical changes, export immutable logs, and verify no file content persisted.  
**Duration**: 1.5 weeks  
**Deliverables**: Audit log queryable; exports include hash verification; read-only access enforced for Auditor role

### 5.1 Audit Trail Models & Database

- [ ] **T5-001** [P3] [US3] Model: Create AuditEntry entity (entry_id, tenant_id, actor_id, action_type [create/update/delete/resolve_conflict/rotate_credentials/etc], binding_id, item_id, source_metadata, target_metadata, policy_applied, timestamp_utc, content_hash_verification, created_at)
  - **File**: `src/Linksy.Api/Models/AuditEntry.cs`
  - **Dependencies**: T1-004
  - **Test**: Model compiles; includes immutable server timestamp

- [ ] **T5-002** [P3] [US3] Migration: Create `006_AddAuditTrail` migration with AuditEntry table; add database constraint preventing deletion (ON DELETE RESTRICT); add UNIQUE constraint on (entry_id, created_at) to enforce ordering
  - **File**: `src/Linksy.Api/Migrations/[TIMESTAMP]_AddAuditTrail.cs`
  - **Dependencies**: T5-001
  - **Test**: Migration applies; DELETE attempt fails with constraint violation

- [ ] **T5-003** [P3] [US3] Service: Create AuditService to log all sync actions, conflict resolutions, credential rotations, and user actions with hash chain verification
  - **File**: `src/Linksy.Api/Services/AuditService.cs`
  - **Dependencies**: T5-001, T5-002
  - **Test**: Service logs actions; hash chain computed correctly; entry immutable after creation

- [ ] **T5-004** [P3] [US3] Service: Create AuditHashChainService to compute and verify hash chain for tamper detection (SHA-256 of previous_hash + current_entry_data)
  - **File**: `src/Linksy.Api/Services/AuditHashChainService.cs`
  - **Dependencies**: T5-003
  - **Test**: Hash chain computes correctly; tampering detected; verification passes for valid chains

### 5.2 Audit Endpoints

- [ ] **T5-005** [P3] [US3] Endpoint: `GET /api/audit/entries` - Query audit log with filters (date range, actor, binding_id, action_type); pagination support
  - **File**: `src/Linksy.Api/Endpoints/AuditEndpoints.cs`
  - **Dependencies**: T1-012, T5-003
  - **Test**: Requires Auditor role; returns filtered entries; pagination works

- [ ] **T5-006** [P3] [US3] Endpoint: `GET /api/audit/entries/{entry_id}` - Get single audit entry with full metadata and hash verification status
  - **File**: `src/Linksy.Api/Endpoints/AuditEndpoints.cs`
  - **Dependencies**: T5-005, T5-004
  - **Test**: Returns entry with hash_valid: true/false; read-only access enforced

- [ ] **T5-007** [P3] [US3] Endpoint: `POST /api/audit/export` - Export audit log as CSV/JSON with hash chain verification metadata
  - **File**: `src/Linksy.Api/Endpoints/AuditEndpoints.cs`
  - **Dependencies**: T5-005, T5-004
  - **Test**: Requires Auditor role; export includes verification metadata; file contains no persisted content

- [ ] **T5-008** [P3] [US3] Endpoint: `GET /api/audit/verify?export_id={id}` - Verify exported audit report integrity (re-compute hash chain from export file)
  - **File**: `src/Linksy.Api/Endpoints/AuditEndpoints.cs`
  - **Dependencies**: T5-007
  - **Test**: Verification succeeds for valid exports; fails if export was tampered with

### 5.3 Audit Frontend

- [ ] **T5-009** [P3] [US3] UI: Create Audit Log component with filters (date range, user, binding, action type)
  - **File**: `src/frontend/src/components/Audit/AuditLog.tsx`
  - **Dependencies**: T5-005, T2-012
  - **Test**: Displays audit entries; filters work; pagination enabled

- [ ] **T5-010** [P3] [US3] UI: Create Audit Entry detail view showing action, actor, timestamp, affected items, and hash verification status
  - **File**: `src/frontend/src/components/Audit/AuditEntryDetail.tsx`
  - **Dependencies**: T5-006, T5-009
  - **Test**: Shows entry details; hash status displayed; no file content visible

- [ ] **T5-011** [P3] [US3] UI: Create Export button and modal for audit log export (selects format, date range, confirms no-content notice)
  - **File**: `src/frontend/src/components/Audit/AuditExportModal.tsx`
  - **Dependencies**: T5-007, T5-009
  - **Test**: Modal displays; export format choice works; confirmation required

- [ ] **T5-012** [P3] [US3] UI: Add Auditor role check to routes (render Audit menu only for Auditor role)
  - **File**: `src/frontend/src/components/Navigation/NavMenu.tsx`
  - **Dependencies**: T2-010, T5-009
  - **Test**: Audit menu visible only to Auditor; Other roles see 403 on direct URL access

- [ ] **T5-013** [P3] [US3] Test: Write integration tests for audit logging, export, and verification workflows
  - **File**: `src/Linksy.Api.Tests/AuditIntegrationTests.cs`
  - **Dependencies**: T1-022, T5-007, T5-008
  - **Test**: Full audit workflows pass; export integrity verified

---

## Phase 6: Observability, Testing, & Polish

**Goal**: Add metrics, error handling, documentation, and comprehensive test coverage across all features.  
**Duration**: 2 weeks  
**Deliverables**: 80%+ code coverage; observability dashboard populated; all error cases handled; docs complete

### 6.1 Observability & Metrics

- [ ] **T6-001** [P2] Observability: Add OpenTelemetry metrics to SyncEngineService (files_processed, bytes_transferred, conflicts_detected, retries_attempted, throttle_events)
  - **File**: `src/Linksy.Api/Services/SyncEngineService.cs` (enhance)
  - **Dependencies**: T4-004
  - **Test**: Metrics emitted per binding; visible in Aspire dashboard

- [ ] **T6-002** [P2] Observability: Configure OpenTelemetry exporter for Aspire dashboard and optional Jaeger/Datadog integration
  - **File**: `src/Linksy.Api/Program.cs`, `src/Linksy.AppHost/AppHost.cs`
  - **Dependencies**: T1-018, T6-001
  - **Test**: Dashboard shows metrics; traces from API calls visible

- [ ] **T6-003** [P2] Observability: Add structured logging to all services (ILogger with correlation IDs for tracing)
  - **File**: All Service/*.cs files (enhance)
  - **Dependencies**: T6-001
  - **Test**: Logs include correlation ID; searchable by binding_id and user_id

- [ ] **T6-004** [P3] Observability: Create admin dashboard component to display tenant-level metrics (jobs run, success rate, avg duration)
  - **File**: `src/frontend/src/components/Admin/MetricsDashboard.tsx`
  - **Dependencies**: T6-001, T1-017
  - **Test**: Dashboard displays metrics; admin-only access enforced

### 6.2 Error Handling & Resilience

- [ ] **T6-005** [P1] Error: Create GlobalExceptionMiddleware to catch unhandled exceptions and return standardized error responses (400/401/403/500)
  - **File**: `src/Linksy.Api/Middleware/GlobalExceptionMiddleware.cs`
  - **Dependencies**: T1-008
  - **Test**: Unhandled exceptions return 500; stack traces never leaked to client

- [ ] **T6-006** [P2] Error: Add validation middleware for all API request bodies (prevent malformed JSON, invalid enum values)
  - **File**: `src/Linksy.Api/Middleware/ValidationMiddleware.cs`
  - **Dependencies**: T6-005
  - **Test**: Malformed requests return 400 with validation details

- [ ] **T6-007** [P2] Error: Create ConnectorErrorService to handle and retry connector-specific errors (API rate limits, auth failures, network timeouts)
  - **File**: `src/Linksy.Api/Services/ConnectorErrorService.cs`
  - **Dependencies**: T4-006
  - **Test**: Retryable errors are retried; non-retryable errors fail fast

- [ ] **T6-008** [P3] Error: Add user-friendly error messages in frontend error boundaries
  - **File**: `src/frontend/src/components/ErrorBoundary.tsx`
  - **Dependencies**: T2-012
  - **Test**: Errors display user-friendly messages; error log available for debugging

### 6.3 Security Hardening

- [ ] **T6-009** [P1] Security: Add rate limiting middleware to auth endpoints (max 5 login attempts per IP per minute)
  - **File**: `src/Linksy.Api/Middleware/RateLimitMiddleware.cs`
  - **Dependencies**: T1-008
  - **Test**: Rate limiting enforced; requests exceed limit get 429

- [ ] **T6-010** [P2] Security: Implement CSRF token validation for state-changing requests (POST, PUT, DELETE)
  - **File**: `src/Linksy.Api/Middleware/CsrfMiddleware.cs`
  - **Dependencies**: T1-019
  - **Test**: Requests without CSRF token fail; requests with valid token succeed

- [ ] **T6-011** [P2] Security: Add Content-Security-Policy and X-Frame-Options headers to all responses
  - **File**: `src/Linksy.Api/Program.cs`
  - **Dependencies**: T1-008
  - **Test**: Headers present in response; browser security policies enforced

- [ ] **T6-012** [P2] Security: Implement input sanitization for file paths (prevent path traversal attacks)
  - **File**: `src/Linksy.Api/Services/PathSanitizationService.cs`
  - **Dependencies**: T4-004
  - **Test**: Path traversal attempts blocked; valid paths allowed

### 6.4 Comprehensive Testing

- [ ] **T6-013** [P1] Test: Add unit tests for JwtTokenService (token generation, expiration, claims)
  - **File**: `src/Linksy.Api.Tests/AuthTests.cs` (enhance)
  - **Dependencies**: T1-020, T2-008
  - **Test**: 95%+ code coverage on JwtTokenService

- [ ] **T6-014** [P1] Test: Add unit tests for ConflictResolutionService (all policies, hold windows)
  - **File**: `src/Linksy.Api.Tests/ConflictTests.cs`
  - **Dependencies**: T1-020, T4-005
  - **Test**: 90%+ code coverage on ConflictResolutionService

- [ ] **T6-015** [P1] Test: Add unit tests for AuditHashChainService (hash computation, verification)
  - **File**: `src/Linksy.Api.Tests/AuditTests.cs`
  - **Dependencies**: T1-020, T5-004
  - **Test**: 95%+ code coverage on AuditHashChainService

- [ ] **T6-016** [P2] Test: Add integration tests for end-to-end onboarding flow (create tenant, login, complete wizard)
  - **File**: `src/Linksy.Api.Tests/OnboardingE2ETests.cs`
  - **Dependencies**: T1-022, T3-019
  - **Test**: Full flow passes; binding created and ready for sync

- [ ] **T6-017** [P2] Test: Add integration tests for end-to-end sync workflow (trigger job, process changes, resolve conflicts)
  - **File**: `src/Linksy.Api.Tests/SyncE2ETests.cs`
  - **Dependencies**: T1-022, T4-023
  - **Test**: Sync job completes; conflicts resolved; quarantine works

- [ ] **T6-018** [P2] Test: Add frontend unit tests for all React components (Login, Onboarding wizard, Activity log, Audit viewer)
  - **File**: `src/frontend/src/components/**/*.test.tsx`
  - **Dependencies**: T1-021, T3-013, T4-017, T5-009
  - **Test**: 80%+ code coverage on components

- [ ] **T6-019** [P2] Test: Add frontend integration tests with mock API server (complete user workflows)
  - **File**: `src/frontend/src/__tests__/integration/*.test.tsx`
  - **Dependencies**: T1-021, T6-018
  - **Test**: User workflows pass with mocked API; error handling works

- [ ] **T6-020** [P3] Test: Load test sync engine with large file counts (10k+ files per job)
  - **File**: `src/Linksy.Api.Tests/LoadTests.cs`
  - **Dependencies**: T1-020, T4-004
  - **Test**: Sync handles 10k files; memory usage reasonable; performance acceptable

### 6.5 Documentation & Deployment

- [ ] **T6-021** [P2] Docs: Write API documentation (OpenAPI 3.1 spec with all endpoints, request/response schemas)
  - **File**: `specs/001-aec-file-sync/contracts/api-spec.yaml`
  - **Dependencies**: T3-011, T4-008, T5-005
  - **Test**: Spec validates with `openapi-generator-cli`; endpoints documented

- [ ] **T6-022** [P2] Docs: Write developer guide (local dev setup, running tests, deployment steps)
  - **File**: `docs/aec-sync-developer-guide.md`
  - **Dependencies**: T1-018
  - **Test**: New developer can follow guide and run project locally

- [ ] **T6-023** [P2] Docs: Write operator guide (triggering syncs, resolving conflicts, managing quarantine)
  - **File**: `docs/aec-sync-operator-guide.md`
  - **Dependencies**: T4-017, T4-020, T4-021
  - **Test**: Operator can follow guide and perform key tasks

- [ ] **T6-024** [P2] Docs: Write auditor guide (reviewing audit logs, exporting, verifying integrity)
  - **File**: `docs/aec-sync-auditor-guide.md`
  - **Dependencies**: T5-009, T5-011
  - **Test**: Auditor can follow guide and export verified logs

- [ ] **T6-025** [P3] Docs: Create troubleshooting guide (common errors, recovery steps)
  - **File**: `docs/aec-sync-troubleshooting.md`
  - **Dependencies**: T6-005, T6-007
  - **Test**: All error scenarios documented with recovery steps

- [ ] **T6-026** [P2] Deploy: Create Dockerfile for API service (production-ready image)
  - **File**: `Dockerfile.api`
  - **Dependencies**: T1-001
  - **Test**: Image builds; container runs; health check works

- [ ] **T6-027** [P2] Deploy: Create Dockerfile for React frontend (production build)
  - **File**: `Dockerfile.frontend`
  - **Dependencies**: T1-014
  - **Test**: Image builds; serves optimized React bundle; routing works

- [ ] **T6-028** [P2] Deploy: Create Docker Compose configuration for local multi-container development
  - **File**: `docker-compose.yml`
  - **Dependencies**: T6-026, T6-027, T1-003
  - **Test**: `docker-compose up` starts all services; health checks pass

- [ ] **T6-029** [P3] Deploy: Set up CI/CD pipeline (GitHub Actions: build, test, deploy to staging)
  - **File**: `.github/workflows/ci-cd.yml`
  - **Dependencies**: T1-020, T1-021
  - **Test**: Pipeline runs on PR; tests pass; artifacts uploaded

### 6.6 Final Validation

- [ ] **T6-030** [P1] Validate: Verify all 5 success criteria are measurable and met (SC-001 through SC-005)
  - **File**: Metrics collection scripts
  - **Dependencies**: All phases
  - **Test**: Metrics dashboard shows all SCs tracked; pilot customers surveyed

- [ ] **T6-031** [P1] Validate: Ensure 80%+ code coverage across backend (API + Services)
  - **File**: Coverage reports
  - **Dependencies**: T6-013 through T6-019
  - **Test**: `dotnet test /p:CollectCoverage=true` reports 80%+ coverage

- [ ] **T6-032** [P1] Validate: Ensure 80%+ code coverage across frontend (React components)
  - **File**: Coverage reports
  - **Dependencies**: T6-018, T6-019
  - **Test**: `npm run coverage` reports 80%+ coverage

- [ ] **T6-033** [P2] Validate: Run full end-to-end test suite (all 3 user stories, all roles, all error scenarios)
  - **File**: Test results document
  - **Dependencies**: T6-016, T6-017, T6-019
  - **Test**: All E2E tests pass; no known blockers

- [ ] **T6-034** [P3] Validate: Conduct security review (OWASP Top 10, JWT implementation, encryption, auth flow)
  - **File**: Security audit report
  - **Dependencies**: T6-009 through T6-012
  - **Test**: No high-severity findings; all low findings documented with remediation plan

---

## Task Dependencies & Parallelization

### Critical Path (Blocking Dependencies)

```
Phase 1 Setup (T1-001 → T1-022)
    ↓
Phase 2 Auth (T2-001 → T2-014)
    ↓
Phase 3 Onboarding (T3-001 → T3-019) [can run in parallel with Phase 4/5 after T2-014]
    ↓ (depends on bindings)
Phase 4 Sync (T4-001 → T4-023)
    ↓ (depends on SyncJob/ChangeItem models)
Phase 5 Audit (T5-001 → T5-013)
    ↓
Phase 6 Polish (T6-001 → T6-034)
```

### Parallelizable Sections

- **Phase 1**: Backend (T1-001 to T1-022) can run in parallel with Frontend (T1-014 to T1-017)
- **Phase 2**: Auth endpoints (T2-001 to T2-009) can run in parallel with Frontend auth (T2-010 to T2-014)
- **Phase 3**: Connector models (T3-001 to T3-004) can run in parallel with Binding models (T3-005 to T3-008)
- **Phase 4**: Sync engine (T4-001 to T4-007) can run in parallel with Frontend activity log (T4-017 to T4-022)
- **Phase 5**: Audit backend (T5-001 to T5-008) can run in parallel with Frontend audit (T5-009 to T5-012)
- **Phase 6**: Unit tests (T6-013 to T6-015) can run in parallel with Documentation (T6-021 to T6-025)

### Estimated Duration by Phase

- **Phase 1**: 7 days (infrastructure)
- **Phase 2**: 7 days (auth + JWT lifecycle)
- **Phase 3**: 10 days (P1 onboarding feature)
- **Phase 4**: 12 days (P2 sync + monitor feature)
- **Phase 5**: 8 days (P3 audit feature)
- **Phase 6**: 10 days (testing, observability, docs, deployment)

**Total**: ~54 days (~8 weeks) for MVP with 80%+ code coverage and full test suite

---

## Role-Based Task Assignment Recommendations

### Backend Developer
- Phase 1: T1-001 through T1-022 (all backend setup)
- Phase 2: T2-001 through T2-009 (auth endpoints)
- Phase 3: T3-001 through T3-012 (connector/binding models + endpoints)
- Phase 4: T4-001 through T4-016 (sync engine + endpoints)
- Phase 5: T5-001 through T5-008 (audit backend)
- Phase 6: T6-001 through T6-008, T6-013 through T6-015, T6-021 through T6-029

### Frontend Developer
- Phase 1: T1-014 through T1-017, T1-021 (React setup + testing framework)
- Phase 2: T2-010 through T2-014 (auth UI)
- Phase 3: T3-013 through T3-019 (onboarding wizard)
- Phase 4: T4-017 through T4-023 (activity log + conflict resolution)
- Phase 5: T5-009 through T5-012 (audit viewer)
- Phase 6: T6-004, T6-008, T6-018 through T6-019, T6-022 through T6-034

### QA / Test Engineer
- Phase 1: T1-020, T1-021, T1-022 (test framework setup)
- Phases 2-5: T*-End (integration tests for each phase)
- Phase 6: T6-013 through T6-033 (comprehensive test suite + coverage)

---

## Success Criteria Validation

Before marking Phase complete, verify:
- ✅ All P1 (critical path) tasks completed
- ✅ All P2 (required) tasks completed
- ✅ P3 (polish) tasks completed or deferred with documented reason
- ✅ Integration tests pass for all completed features
- ✅ No known blockers or tech debt
- ✅ Code coverage >= target (80%+)
- ✅ Documentation updated

---

## Next Steps

1. **Assign Tasks**: Distribute tasks to backend and frontend developers based on expertise and availability
2. **Create Backlog**: Import tasks into Azure DevOps or GitHub Projects (link to this file)
3. **Set Milestones**: Map phases to sprint/release cycles
4. **Generate Code**: Use Phase 1 tasks to generate project scaffold, then Phase 2 for auth stubs, etc.
5. **Track Progress**: Update task status weekly; escalate blockers immediately

