# Linksy System Architecture

**Feature**: 001-aec-file-sync  
**Date**: 2025-10-25  
**Status**: Design Phase  
**Audience**: Engineering team, stakeholders, compliance auditors

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [System Context](#system-context)
3. [High-Level Architecture](#high-level-architecture)
4. [Platform & Hosting Strategy](#platform--hosting-strategy)
5. [Layered Architecture](#layered-architecture)
6. [Data Model & Schema](#data-model--schema)
7. [Job Orchestration Pattern](#job-orchestration-pattern)
8. [Security & Authentication](#security--authentication)
9. [Observability & Monitoring](#observability--monitoring)
10. [Connector Integration](#connector-integration)
11. [Deployment Architecture](#deployment-architecture)
12. [Technology Stack](#technology-stack)
13. [Architecture Decision Records](#architecture-decision-records)

---

## Executive Summary

**Linksy** is a multi-tenant, serverless SaaS platform for bidirectional file synchronization between Autodesk Construction Cloud (ACC) Docs and Microsoft SharePoint Online. The system operates as a deterministic, event-driven sync engine that:

- **Minimizes cost**: Azure serverless (Functions, Durable Functions, SQL Serverless) scale to near-zero idle cost
- **Ensures reliability**: 99.5% daily job success rate with automatic retries and manual hold queues
- **Guarantees compliance**: Append-only audit trail with hash chain verification, 24h transient cache, 7d soft-delete quarantine
- **Enables operators**: Real-time activity logging, conflict resolution portal, credential rotation, observable metrics per tenant

The platform targets **P1 Onboarding** (guided wizard + immediate sync), **P2 Operations** (job management + conflict resolution), and **P3 Compliance** (audit exports + hash validation).

---

## System Context

### Actors

- **Tenant Admin**: Configures connectors, creates bindings, assigns user roles, triggers credential rotation
- **Operator**: Monitors sync jobs, resolves manual-hold conflicts, restores quarantined files, views activity logs
- **Auditor**: Reviews audit trail, exports compliance reports, verifies immutability
- **System**: Runs scheduled syncs, processes webhooks, executes exponential backoff retries, enforces oscillation holds

### External Systems

- **Autodesk Construction Cloud (ACC) Docs**: OAuth app-only credentials; polling via delta tokens; webhook events with signature validation
- **Microsoft SharePoint Online**: Microsoft Graph; app-only permissions; polling via delta tokens; subscription webhooks
- **Entra ID**: SSO via OpenID Connect; identity provider for tenant admins
- **Azure Key Vault**: Encrypted credential storage per tenant; rotation audit trail

### Success Criteria

| Criterion | Target | Rationale |
|-----------|--------|-----------|
| **SC-001** | 90% of admins complete onboarding in <10 min | Usability gate; establishes product-market fit |
| **SC-002** | 99.5% daily job success rate | Operational reliability baseline |
| **SC-003** | 95% of changes propagate within 10 min | User experience; demonstrates real-time feel |
| **SC-004** | ≥4/5 satisfaction score (pilot) | Customer confidence |
| **SC-005** | Zero persisted file content in exports | Compliance requirement |

---

## High-Level Architecture

### System Boundaries Diagram

```mermaid
graph TB
    subgraph Linksy["Linksy SaaS Platform"]
        React["React 19 SPA<br/>(Frontend)"]
        EntraID["Auth Flow<br/>(Entra ID)"]
        API["API Gateway<br/>(Azure Functions)"]
        DF["Durable Functions<br/>• Orchestration<br/>• Activities<br/>• Entities"]
        Storage["Azure Storage<br/>• Queues<br/>• Blobs<br/>• Audit Exports"]
        SQL["Azure SQL Serverless<br/>• Tenants, Users<br/>• Bindings, Jobs<br/>• Audit Trail"]
        KV["Key Vault<br/>• Credentials<br/>• Rotation Trail"]
        AppInsights["App Insights<br/>+ OpenTelemetry<br/>• Metrics<br/>• Traces<br/>• Alerts"]
    end
    
    ACC["Autodesk ACC Docs<br/>(OAuth App-Only)<br/>• Delta tokens<br/>• Webhooks"]
    SP["SharePoint Online<br/>(Microsoft Graph)<br/>• Delta tokens<br/>• Webhooks"]
    
    React -->|HTTPS| API
    React -->|SSO| EntraID
    API -->|dispatch| DF
    API -->|dispatch| Storage
    DF -->|query/mutate| SQL
    DF -->|fetch secrets| KV
    API -->|emit| AppInsights
    DF -->|emit| AppInsights
    DF -->|polling & webhooks| ACC
    DF -->|polling & webhooks| SP
    
    style Linksy fill:#e1f5ff
    style React fill:#61dafb
    style API fill:#512bd4,color:#fff
    style DF fill:#00a4ef,color:#fff
    style SQL fill:#336791,color:#fff
    style ACC fill:#ff6b35,color:#fff
    style SP fill:#0078d4,color:#fff
```

### Data Flow: Sync Job Execution

```mermaid
graph TD
    Start["Trigger<br/>Timer | HTTP | Queue"] --> LoadBind["Load Binding Config<br/>Fetch Credentials from KV<br/>Load BindingState"]
    LoadBind --> GetDeltas["Poll Deltas in Parallel"]
    
    GetDeltas --> GetAcc["GetAccDeltas<br/>ACC delta token → ChangeItems"]
    GetDeltas --> GetSp["GetSpDeltas<br/>SP delta token → ChangeItems"]
    
    GetAcc --> Merge["Merge & Group by FileKey"]
    GetSp --> Merge
    
    Merge --> CheckHold{"Check HoldWindow<br/>Entity"}
    
    CheckHold -->|Hold Active| Queue["Queue Change<br/>Defer until expiration"]
    CheckHold -->|Hold Inactive| ApplyPolicy["Apply Conflict Policy"]
    
    Queue --> Wait["Wait for Hold Expiration"]
    Wait --> ApplyPolicy
    
    ApplyPolicy --> PolicyDecision{"Policy Type"}
    
    PolicyDecision -->|SourceWins| UseSrc["Use Source Version"]
    PolicyDecision -->|TargetWins| UseTgt["Use Target Version"]
    PolicyDecision -->|LastWriterWins| UseLatest["Use Latest Timestamp"]
    PolicyDecision -->|ManualHold| ToQuarantine["Move to Quarantine<br/>Alert Operator"]
    
    UseSrc --> ApplyChange["ApplyChange Activity<br/>Upload/Download/Delete<br/>+ Checksum Validation"]
    UseTgt --> ApplyChange
    UseLatest --> ApplyChange
    
    ApplyChange --> SyncResult{"Sync Result"}
    
    SyncResult -->|Success| WriteAudit["WriteAuditEntry<br/>Hash Chain"]
    SyncResult -->|HTTP 429| Backoff["BackoffRetry Activity<br/>Exponential: 1s, 2s, 4s, 8s, 16s, 30s, 40s<br/>Max 7 retries"]
    
    Backoff --> RetryResult{"Retries<br/>Exhausted?"}
    RetryResult -->|No| ApplyChange
    RetryResult -->|Yes| ToQuarantine
    
    WriteAudit --> UpdateState["Update BindingState<br/>New delta tokens<br/>Record metrics"]
    ToQuarantine --> UpdateState
    
    UpdateState --> Complete["Job Complete<br/>Status: Success | Partial | Failed<br/>Alert on SLO breach"]
    
    style Start fill:#90ee90
    style Complete fill:#90ee90
    style ToQuarantine fill:#ff6b6b,color:#fff
    style CheckHold fill:#fbbf24
    style PolicyDecision fill:#fbbf24
    style SyncResult fill:#fbbf24
    style RetryResult fill:#fbbf24
```

---

## Platform & Hosting Strategy

### Technology Stack by Layer

| Layer | Technology | Rationale | Notes |
|-------|-----------|-----------|-------|
| **Frontend** | Azure Static Web Apps + React 19 SPA | Global CDN, near-zero idle cost, built-in auth integrations | TypeScript strict mode, Vite, Tailwind CSS 4, shadcn/ui |
| **API Layer** | Azure Functions (HTTP-triggered, isolated .NET 9) | Pay-per-execution, auto-scale, minimal idle cost | Minimal API pattern, <200ms p95 latency target |
| **Job Orchestration** | Durable Functions (.NET 9) | Reliable state management, built-in retry logic, activity pattern | Orchestrators + Activities + Durable Entities for holds |
| **Identity** | Entra ID (B2E SSO) + ASP.NET Core Identity | Single sign-on, JWT issuance, RBAC claims | 12h JWT + refresh tokens, HttpOnly cookies |
| **Primary Store** | Azure SQL Database (Serverless tier) | Auto-pause idle, ACID transactions, RBAC via DB roles | Append-only audit constraints, tenant isolation via queries |
| **Messaging** | Azure Storage Queues (MVP) | Lowest cost option, poison queue support, visibility timeouts | Upgrade to Service Bus sessions if ordering critical |
| **Secrets** | Azure Key Vault | Encrypted credential storage, rotation audit trail, RBAC | Per-tenant credential isolation |
| **File Storage** | Azure Blob Storage with lifecycle policies | 24h transient cache auto-delete, 7d quarantine auto-delete, WORM audit exports | Compliance-grade immutability |
| **Observability** | Application Insights + OpenTelemetry 1.13.0 | Per-tenant metrics, distributed traces, alerting | Correlation IDs across API → Durable Functions |
| **Infrastructure** | Bicep or Terraform (IaC) | Parameterized templates for Dev/Staging/Prod | Single source of truth, repeatable deployments |

### Cost Model

- **Idle state**: Near-zero cost (SQL Serverless pauses, Functions scale to zero, Storage minimal)
- **Active state**: Pay per execution (Functions) + per-transaction (SQL) + per-job (Durable Functions)
- **Storage**: Transient cache (24h auto-delete) + Audit exports (WORM, versioned)
- **Observability**: App Insights Basic tier + OpenTelemetry export

---

## Layered Architecture

### Presentation Layer (Frontend)

```
React 19 SPA (TypeScript, strict mode)
├── Pages
│   ├── Login/SSO (Entra ID redirect)
│   ├── Dashboard (job overview, metrics)
│   ├── OnboardingWizard (connector validation, binding setup)
│   ├── BindingManager (CRUD, schedule, conflict policy)
│   ├── ActivityLog (job history, real-time WebSocket stream)
│   ├── ConflictResolver (quarantine view, one-click restore)
│   └── AuditExporter (filter, download, hash verification)
│
├── Components (shadcn/ui + Tailwind CSS 4)
│   ├── Button, Dialog, Form, Input, Table, etc.
│   ├── ConnectorAuthFlow (OAuth redirect handlers)
│   └── JobStatusIndicator (real-time status badges)
│
├── Hooks
│   ├── useAuth (JWT + refresh token lifecycle)
│   ├── useRole (RBAC check: Admin | Operator | Auditor)
│   ├── useWebSocket (Activity stream subscription)
│   └── useQuery (API cache / stale-while-revalidate)
│
├── Services
│   ├── api.ts (Axios client + JWT interceptor)
│   ├── auth.ts (SSO flow, token storage)
│   ├── telemetry.ts (OpenTelemetry client instrumentation)
│   └── audit.ts (export + hash chain verification)
│
└── Types (TypeScript interfaces)
    ├── Binding, Job, ChangeItem, AuditEntry
    └── Enums: ConflictPolicy, JobStatus, Role
```

### API Layer (Control Plane)

```mermaid
graph TB
    subgraph API["Azure Functions HTTP API (Minimal API, .NET 9)"]
        subgraph Auth["Authentication & Authorization"]
            A1["GET /auth/login"]
            A2["POST /auth/callback"]
            A3["POST /auth/refresh"]
            Middleware["JWT Validation + RBAC"]
        end
        
        subgraph Tenant["Tenant Management (Admin)"]
            T1["GET /tenants/{id}"]
            T2["POST /tenants/{id}/users"]
            T3["POST /tenants/{id}/users/{uid}/roles"]
        end
        
        subgraph Connector["Connector Management (Admin)"]
            C1["POST /connectors/validate"]
            C2["POST /connectors/credentials/rotate"]
            C3["GET /connectors"]
        end
        
        subgraph Binding["Binding Management (Admin + Operator)"]
            B1["GET /bindings"]
            B2["POST /bindings"]
            B3["PUT /bindings/{id}"]
            B4["DELETE /bindings/{id}"]
            B5["POST /bindings/{id}/sync/trigger"]
            B6["POST /bindings/{id}/pause"]
        end
        
        subgraph Monitor["Sync Job Monitoring (Admin + Operator)"]
            M1["GET /sync/jobs"]
            M2["GET /sync/jobs/{jobId}"]
            M3["GET /sync/jobs/{jobId}/changes"]
            M4["WS /ws/activity/stream"]
        end
        
        subgraph Conflict["Conflict Resolution (Operator)"]
            CF1["GET /conflicts/quarantine"]
            CF2["POST /conflicts/{id}/resolve"]
            CF3["POST /conflicts/{id}/delete"]
        end
        
        subgraph Audit["Audit Trail (Auditor)"]
            AU1["GET /audit?filter=..."]
            AU2["POST /audit/export"]
        end
        
        subgraph Obs["Observability"]
            OB1["GET /metrics"]
            OB2["GET /health"]
        end
    end
    
    A1 --> Middleware
    A2 --> Middleware
    A3 --> Middleware
    
    style API fill:#512bd4,color:#fff
    style Auth fill:#00a4ef,color:#fff
    style Tenant fill:#00a4ef,color:#fff
    style Connector fill:#00a4ef,color:#fff
    style Binding fill:#00a4ef,color:#fff
    style Monitor fill:#00a4ef,color:#fff
    style Conflict fill:#ff6b6b,color:#fff
    style Audit fill:#90ee90
    style Obs fill:#fbbf24,color:#000
```

### Service Layer (Business Logic)

```mermaid
graph TB
    subgraph Services["Linksy.Api Services (.NET 9)"]
        subgraph BindingService["BindingService"]
            BS1["CreateBinding(config)"]
            BS2["UpdateConflictPolicy()"]
            BS3["PauseBinding()"]
        end
        
        subgraph JobService["JobService"]
            JS1["TriggerManualSync()"]
            JS2["GetJobStatus()"]
            JS3["RetryJob()"]
        end
        
        subgraph AuditService["AuditService"]
            AS1["AppendEntry()"]
            AS2["ValidateIntegrity()"]
            AS3["ExportReport()"]
        end
        
        subgraph ConflictService["ConflictService"]
            CS1["ResolveManualHold()"]
            CS2["RestoreQuarantined()"]
            CS3["GetQuarantineList()"]
        end
        
        subgraph CredentialService["CredentialService"]
            CRS1["RotateCredential()"]
            CRS2["GetEncryptedSecret()"]
            CRS3["AuditRotation()"]
        end
        
        subgraph ConnectorService["ConnectorService"]
            CN1["ValidateAccess()"]
            CN2["GetConnectorHealth()"]
        end
    end
    
    style Services fill:#00a4ef,color:#fff
    style BindingService fill:#b3e5fc,color:#000
    style JobService fill:#b3e5fc,color:#000
    style AuditService fill:#b3e5fc,color:#000
    style ConflictService fill:#b3e5fc,color:#000
    style CredentialService fill:#b3e5fc,color:#000
    style ConnectorService fill:#b3e5fc,color:#000
```

### Orchestration Layer (Durable Functions)

```mermaid
graph TB
    subgraph Orchestration["RunBindingSyncOrchestrator"]
        O1["1. Load Binding + State"]
        O2["2. Fetch Credentials"]
        O3["3-4. GetDeltas Parallel"]
        O5["5. Group by FileKey"]
        O6["6. Check HoldWindow"]
        O7["7. ApplyChange Parallel"]
        O8["8. BackoffRetry on 429"]
        O9["9. Aggregate Results"]
        O10["10. WriteAuditEntry"]
        O11["11. Update BindingState"]
    end
    
    subgraph Activities["Activity Functions"]
        A1["GetAccDeltas()"]
        A2["GetSpDeltas()"]
        A3["ApplyChange()"]
        A4["BackoffRetry()<br/>7 retries, 120s"]
        A5["MoveToManualHold()"]
        A6["WriteAuditEntry()"]
    end
    
    subgraph Entities["Durable Entities"]
        E1["HoldWindowEntity<br/>tenant:binding:fileId<br/>• SetHold 5min<br/>• QueueChange<br/>• CheckExpired"]
    end
    
    subgraph Triggers["Trigger Sources"]
        T1["Timer: Scheduled"]
        T2["HTTP: Webhook"]
        T3["Queue: Backlog"]
    end
    
    T1 --> O1
    T2 --> O1
    T3 --> O1
    
    O1 --> O2
    O2 --> O3
    O3 --> A1
    O3 --> A2
    O5 --> O6
    A1 --> O5
    A2 --> O5
    O6 --> E1
    O6 --> O7
    O7 --> A3
    A3 --> O8
    O8 --> A4
    A4 --> O9
    O9 --> O10
    O10 --> A6
    O10 --> A5
    O10 --> O11
    
    style Orchestration fill:#00a4ef,color:#fff
    style Activities fill:#90ee90
    style Entities fill:#fbbf24,color:#000
    style Triggers fill:#61dafb,color:#000
```

### Data Access Layer (EF Core)

```mermaid
graph TB
    subgraph DAL["ApplicationDbContext (.NET 9 + EF Core)"]
        Tenant[("Tenant")]
        User[("ApplicationUser")]
        Role[("UserRole")]
        Connector[("Connector")]
        Binding[("Binding")]
        BindingState[("BindingState")]
        Job[("Job")]
        ChangeItem[("ChangeItem")]
        Quarantine[("Quarantine")]
        AuditEntry[("AuditEntry<br/>APPEND-ONLY")]
        CredRotation[("CredentialRotation")]
    end
    
    subgraph Constraints["Key Constraints"]
        C1["AuditEntry: DELETE blocked<br/>INSERT only via system role"]
        C2["Binding: Query filter<br/>by TenantId"]
        C3["Job: Soft-delete<br/>via Status column"]
        C4["Quarantine: Auto-purge<br/>at 7-day expiry"]
    end
    
    Tenant --> User
    Tenant --> Connector
    Tenant --> Binding
    Tenant --> AuditEntry
    User --> Role
    Connector --> Binding
    Connector --> CredRotation
    Binding --> BindingState
    Binding --> Job
    Binding --> Quarantine
    Binding --> AuditEntry
    Job --> ChangeItem
    
    style DAL fill:#336791,color:#fff
    style AuditEntry fill:#ff6b6b,color:#fff
    style Constraints fill:#c8e6c9
```

---

## Data Model & Schema

### Core Entities

```sql
-- Multi-tenancy root
CREATE TABLE Tenants (
    TenantId UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    BillingProfile NVARCHAR(MAX),  -- JSON
    Status NVARCHAR(50),             -- Active | Suspended
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- ASP.NET Core Identity users
CREATE TABLE ApplicationUsers (
    UserId NVARCHAR(450) PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Tenants(TenantId),
    Email NVARCHAR(256) UNIQUE NOT NULL,
    DisplayName NVARCHAR(256),
    EntraObjectId UNIQUEIDENTIFIER,  -- Entra ID link
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UNIQUE (TenantId, Email)
);

-- Tenant-scoped RBAC
CREATE TABLE UserRoles (
    UserId NVARCHAR(450) NOT NULL,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(50) NOT NULL,      -- Admin | Operator | Auditor
    PRIMARY KEY (UserId, TenantId, Role),
    FOREIGN KEY (UserId) REFERENCES ApplicationUsers(UserId),
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId)
);

-- External platform integrations
CREATE TABLE Connectors (
    ConnectorId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Tenants(TenantId),
    Kind NVARCHAR(50) NOT NULL,      -- ACC | SharePoint
    KeyVaultRef NVARCHAR(512),       -- Key Vault secret URI
    Health NVARCHAR(50),              -- Healthy | Warning | Error
    LastValidatedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_Tenant (TenantId)
);

-- Binding configurations
CREATE TABLE Bindings (
    BindingId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    AccConnectorId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Connectors(ConnectorId),
    SpConnectorId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Connectors(ConnectorId),
    AccPath NVARCHAR(512) NOT NULL,
    SpPath NVARCHAR(512) NOT NULL,
    Direction NVARCHAR(50) NOT NULL,  -- OneWay | BiDirectional
    ConflictPolicy NVARCHAR(50),      -- SourceWins | TargetWins | LastWriterWins | ManualHold
    Schedule NVARCHAR(100),           -- Cron expression
    Status NVARCHAR(50) DEFAULT 'Active',  -- Active | Paused | Error
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    INDEX IX_Tenant_Status (TenantId, Status),
    INDEX IX_Schedule (Schedule)
);

-- Binding state (delta tokens)
CREATE TABLE BindingState (
    BindingId UNIQUEIDENTIFIER PRIMARY KEY,
    AccDeltaToken NVARCHAR(MAX),
    SpDeltaToken NVARCHAR(MAX),
    LastSyncAt DATETIME2,
    AccChecksum NVARCHAR(128),
    SpChecksum NVARCHAR(128),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (BindingId) REFERENCES Bindings(BindingId)
);

-- Sync job execution records
CREATE TABLE Jobs (
    JobId UNIQUEIDENTIFIER PRIMARY KEY,
    BindingId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(50),              -- Pending | Running | Success | Failure | Partial
    StartedAt DATETIME2,
    EndedAt DATETIME2,
    StatsJson NVARCHAR(MAX),          -- {files, bytes, errors}
    TriggeredBy NVARCHAR(450),        -- UserId or NULL (system)
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (BindingId) REFERENCES Bindings(BindingId),
    INDEX IX_BindingId_Status (BindingId, Status),
    INDEX IX_Status_CreatedAt (Status, CreatedAt)
);

-- Individual file changes
CREATE TABLE ChangeItems (
    ChangeId UNIQUEIDENTIFIER PRIMARY KEY,
    JobId UNIQUEIDENTIFIER NOT NULL,
    FileKey NVARCHAR(512) NOT NULL,
    Action NVARCHAR(50),              -- Create | Update | Delete | Move | Rename
    SourceVersion NVARCHAR(256),
    TargetVersion NVARCHAR(256),
    Retries INT DEFAULT 0,
    Status NVARCHAR(50),              -- Pending | Processing | Success | ManualHold | Failed
    DlqReason NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (JobId) REFERENCES Jobs(JobId),
    INDEX IX_JobId_Status (JobId, Status),
    INDEX IX_FileKey (FileKey)
);

-- Quarantine (soft-deleted + conflicted files)
CREATE TABLE Quarantine (
    QuarantineId UNIQUEIDENTIFIER PRIMARY KEY,
    BindingId UNIQUEIDENTIFIER NOT NULL,
    FileKey NVARCHAR(512) NOT NULL,
    Reason NVARCHAR(50),              -- Conflict | SoftDelete
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2,              -- 7 days out
    VersionRefs NVARCHAR(MAX),        -- JSON: {accVersion, spVersion}
    RestoredAt DATETIME2,             -- NULL until restored
    FOREIGN KEY (BindingId) REFERENCES Bindings(BindingId),
    INDEX IX_BindingId_ExpiresAt (BindingId, ExpiresAt)
);

-- Append-only audit trail (IMMUTABLE)
CREATE TABLE AuditEntries (
    AuditId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId UNIQUEIDENTIFIER NOT NULL,
    BindingId UNIQUEIDENTIFIER,
    At DATETIME2 DEFAULT GETUTCDATE(),  -- Server-side timestamp
    Actor NVARCHAR(450),                -- UserId or NULL (system)
    Action NVARCHAR(256),               -- SyncCompleted | ConflictResolved | BindingCreated
    DetailsJson NVARCHAR(MAX),
    PrevHash NVARCHAR(64),              -- SHA256 of previous entry
    Hash NVARCHAR(64),                  -- SHA256 of this entry
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    INDEX IX_TenantId_At (TenantId, At),
    INDEX IX_BindingId_At (BindingId, At),
    -- Constraints: DELETE/UPDATE blocked by trigger or RBAC
    CHECK (At IS NOT NULL)
);

-- Credential rotation history
CREATE TABLE CredentialRotation (
    RotationId UNIQUEIDENTIFIER PRIMARY KEY,
    ConnectorId UNIQUEIDENTIFIER NOT NULL,
    TriggeredBy NVARCHAR(450),          -- UserId
    TriggeredAt DATETIME2 DEFAULT GETUTCDATE(),
    ValidatedAt DATETIME2,
    Status NVARCHAR(50),                -- Pending | Success | Failed
    ErrorMessage NVARCHAR(MAX),
    FOREIGN KEY (ConnectorId) REFERENCES Connectors(ConnectorId),
    INDEX IX_ConnectorId_Status (ConnectorId, Status)
);
```

### Entity Relationships

```
Tenant (1) ──→ (many) ApplicationUser
         ──→ (many) Connector
         ──→ (many) Binding
         ──→ (many) AuditEntry

ApplicationUser (1) ──→ (many) UserRole
                  ──→ (many) Job (TriggeredBy)

Connector (1) ──→ (many) Binding (as AccConnector or SpConnector)
          ──→ (many) CredentialRotation

Binding (1) ──→ (1) BindingState
        ──→ (many) Job
        ──→ (many) ChangeItem
        ──→ (many) Quarantine
        ──→ (many) AuditEntry

Job (1) ──→ (many) ChangeItem
    ──→ (many) AuditEntry
```

---

## Job Orchestration Pattern

### Durable Functions Workflow

The core sync process uses **Orchestrator-Activity pattern** with **Durable Entities** for state management:

```
┌─ Trigger (Timer | HTTP | Queue)
│
├─ RunBindingSyncOrchestrator
│  │
│  ├─ Load Binding from SQL (tenant isolation)
│  ├─ Fetch Credentials from Key Vault
│  │
│  ├─ Parallel Activities:
│  │  ├─ GetAccDeltas (Poll ACC API with delta token)
│  │  └─ GetSpDeltas (Poll SP API with delta token)
│  │
│  ├─ Merge ChangeItems, Group by FileKey
│  │
│  └─ For each file, check HoldWindowEntity(tenant:binding:fileId)
│     │
│     ├─ If Hold Expired: Proceed to conflict resolution
│     │
│     └─ If Hold Active: Queue change, defer processing
│        │
│        ├─ Apply ConflictPolicy
│        │  ├─ SourceWins: Use ACC version
│        │  ├─ TargetWins: Use SP version
│        │  ├─ LastWriterWins: Compare timestamps (with clock skew guard)
│        │  └─ ManualHold: Move to quarantine, alert operator
│        │
│        └─ ApplyChange Activity
│           ├─ Attempt upload/download with checksum validation
│           ├─ On success: WriteAuditEntry
│           ├─ On HTTP 429: BackoffRetry activity
│           │  └─ Exponential backoff: 1s, 2s, 4s, 8s, 16s, 30s, 40s
│           │     Max 7 retries over 120s window
│           │     If exhausted: MoveToManualHold + alert operator
│           └─ On other error: Retry 3x, then log failure
│
├─ Aggregate results (files processed, bytes, conflicts, retries)
├─ Update BindingState with new delta tokens
├─ Emit metrics to App Insights (per tenant)
└─ Return Job status (Success | Partial | Failed)
```

### Oscillation Prevention

The **5-minute binding hold** prevents sync loops during rapid changes:

```
File X is modified on ACC
├─ Sync runs: ACC version wins (or policy decides)
├─ SP is updated
├─ SetHold(tenant:binding:X, 5 min)
│
└─ Within 5 minutes, SP triggers webhook: "File X modified"
   ├─ New change queued by HoldWindowEntity
   ├─ Hold still active
   └─ Change deferred until hold expires
      │
      └─ After 5 min, hold releases
         └─ Change processed (if sync runs again)
```

**Alternative design**: Use **Durable Entities with state management** to queue changes during hold window, auto-releasing when hold expires.

---

## Security & Authentication

### Authentication Flow

```
User (Tenant Admin)
│
├─ (1) Click "Login"
│
├─ (2) React redirects to GET /auth/login
│  └─ Azure Function initiates Entra ID OpenID Connect flow
│
├─ (3) Entra ID login page
│  └─ User authenticates with org credentials
│
├─ (4) Entra ID callback → Azure Function
│  │   (POST /auth/callback with auth code)
│  │
│  ├─ Exchange code for access token (Entra ID)
│  ├─ Lookup/create ApplicationUser in SQL
│  ├─ Query UserRoles for tenant + roles
│  ├─ Issue JWT (12h expiry) with claims: {sub, email, roles, tenant_id}
│  ├─ Issue refresh token (HttpOnly cookie, 7d expiry)
│  └─ Redirect to React app with tokens
│
├─ (5) React stores JWT (in-memory or secure storage)
│  └─ Stores refresh token in HttpOnly cookie
│
└─ (6) Subsequent API requests
   └─ All requests include: Authorization: Bearer {JWT}
      └─ API middleware validates JWT signature + expiry
         └─ Extracts role claims for RBAC
            └─ If expired: POST /auth/refresh (uses HttpOnly cookie)
               └─ Issues new JWT, continues request
```

### Authorization Model

**Three-tier RBAC** scoped per tenant:

| Role | Permissions |
|------|------------|
| **Tenant Admin** | Create/edit/delete bindings, manage connectors, assign user roles, trigger credential rotation, view all activity |
| **Operator** | Trigger manual syncs, resolve ManualHold conflicts, restore quarantined files, view job status + activity logs, acknowledge alerts |
| **Auditor** | Read-only access to audit logs, filter by date/binding/actor, export compliance reports, verify hash chain |

**Enforcement**:
- API endpoints validate JWT claims: `req.HttpContext.User.IsInRole("Operator")`
- Database queries filtered by TenantId (tenant isolation)
- Audit entries record actor (UserId) for all mutations

### Credential Management

```mermaid
graph TD
    A["Binding created<br/>with Connectors"] --> B["Credentials collected<br/>during onboarding"]
    B --> C["Encrypt credentials"]
    C --> D["Store in Azure<br/>Key Vault"]
    D --> E["KV stores secret<br/>reference URI"]
    
    E --> F{"At Sync Time"}
    F --> G["Azure Function retrieves<br/>secret from KV"]
    G --> H["Decrypt in memory"]
    H --> I["Use for delta polling<br/>& file operations"]
    I --> J["Never persisted<br/>to blob/disk"]
    
    K["Credential Rotation"] --> L["Pause binding"]
    L --> M["Rotate secret in KV<br/>(new version)"]
    M --> N["Validate with test sync"]
    N --> O{"Valid?"}
    O -->|Yes| P["Resume binding"]
    O -->|No| Q["Rollback to<br/>previous version"]
    P --> R["Audit trail recorded"]
    Q --> R
    
    style A fill:#e1f5ff
    style K fill:#fff3e0
    style R fill:#f3e5f5
```

---

## Observability & Monitoring

### Metrics (Per Tenant)

Emitted to **Application Insights** via **OpenTelemetry 1.13.0**:

```
Counter: sync_jobs_total
├─ dimension: status (success | partial | failed)
├─ dimension: binding_id
└─ dimension: tenant_id

Gauge: sync_job_duration_seconds
├─ dimension: binding_id
└─ dimension: conflict_policy

Counter: files_synced_total
├─ dimension: action (create | update | delete | move | rename)
├─ dimension: binding_id
└─ dimension: direction (acc_to_sp | sp_to_acc)

Counter: conflicts_detected_total
├─ dimension: policy (manual_hold | source_wins | target_wins | last_writer_wins)
└─ dimension: binding_id

Counter: retries_total
├─ dimension: reason (http_429 | timeout | transient_error)
└─ dimension: retry_attempt

Gauge: http_429_throttle_events
├─ dimension: connector (acc | sp)
├─ dimension: binding_id
└─ dimension: retry_window_seconds

Gauge: quarantine_files_count
├─ dimension: binding_id
├─ dimension: reason (soft_delete | conflict)
└─ dimension: days_in_quarantine
```

### Traces (Distributed)

OpenTelemetry correlation IDs span entire flow:

```mermaid
graph TB
    A["Request ID: uuid"] --> B["API Endpoint Trace"]
    B --> B1["JWT validation"]
    B --> B2["RBAC check"]
    B --> B3["SQL query"]
    B --> B4["queue job"]
    A --> C["Durable Function Trace"]
    C --> C1["load binding state"]
    C --> C2["fetch ACC deltas"]
    C --> C3["fetch SP deltas"]
    C --> C4["apply changes"]
    C4 --> C4a["check hold window"]
    C4 --> C4b["apply conflict policy"]
    C4 --> C4c["upload/download"]
    C --> C5["write audit entries"]
```

### Alerting

**Application Insights alert rules**:

| Alert | Condition | Action |
|-------|-----------|--------|
| **HTTP 429 Spike** | >10 throttle events in 5 min | Email ops, create incident |
| **Job Failure Rate** | <99.5% daily success | Page on-call engineer |
| **Audit Trail Gap** | Hash chain validation fails | Security team escalation |
| **Quarantine Overflow** | >1000 files in quarantine | Notify tenant admin |
| **Latency SLO Breach** | p95 latency >1s | Investigate resource scaling |

---

## Connector Integration

### Connector Interface

Both ACC Docs and SharePoint Online connectors implement **IConnector**:

```csharp
public interface IConnector
{
    Task<DeltaResponse> GetDeltasAsync(string deltaToken, CancellationToken ct);
    Task<UploadResult> UploadFileAsync(FileContent content, CancellationToken ct);
    Task<DownloadResult> DownloadFileAsync(string fileKey, CancellationToken ct);
    Task<DeleteResult> DeleteFileAsync(string fileKey, CancellationToken ct);
    Task ValidateAccessAsync(CancellationToken ct);
    Task<HealthStatus> GetHealthAsync(CancellationToken ct);
}
```

### ACC Connector

```mermaid
graph TB
    subgraph ACC["Autodesk Construction Cloud"]
        A1["OAuth App-Only<br/>Client Credentials"]
        A2["Service principal from KV"]
        A3["Request token:<br/>POST /v2/token"]
        A4["Poll deltas:<br/>GET /deltav2"]
        A5["Returns ChangeItems<br/>file ID, version, action, timestamp"]
        A6["Upload file<br/>POST /content"]
        A7["Download file<br/>GET /content"]
        A8["Webhook events<br/>File.Created/Updated/Deleted"]
        A9["HMAC-SHA256 validation"]
    end
    
    A1 --> A2
    A2 --> A3
    A3 --> A4
    A4 --> A5
    A5 --> A6
    A5 --> A7
    A5 --> A8
    A8 --> A9
    
    style ACC fill:#ff6b35,color:#fff
```

### SharePoint Connector

```mermaid
graph TB
    subgraph SP["Microsoft Graph API"]
        S1["App-Only Auth<br/>Client Credentials"]
        S2["App registration<br/>with permissions"]
        S3["Request token:<br/>POST /oauth2/v2.0/token"]
        S4["Poll deltas:<br/>GET /drive/root/delta"]
        S5["Returns DriveItem changes<br/>ID, name, size, lastModified"]
        S6["Upload file<br/>PUT /content"]
        S7["Download file<br/>GET /content"]
        S8["Webhook subscriptions<br/>POST /subscriptions"]
        S9["Renewal every 3 days"]
        S10["X-MS-Token validation"]
    end
    
    S1 --> S2
    S2 --> S3
    S3 --> S4
    S4 --> S5
    S5 --> S6
    S5 --> S7
    S5 --> S8
    S8 --> S9
    S8 --> S10
    
    style SP fill:#0078d4,color:#fff
```

---

## Deployment Architecture

### Local Development (Aspire)

```mermaid
graph TB
    subgraph Aspire["Aspire Orchestration (localhost:15217)"]
        Frontend["Frontend<br/>React dev server<br/>localhost:5173<br/>HMR &lt;3s"]
        API["API<br/>Azure Functions<br/>localhost:5000"]
        DF["Durable Functions<br/>Local storage emulator"]
        SQL["SQL Server LocalDB<br/>Local instance"]
        Storage["Storage Emulator<br/>Queues, blobs"]
        Obs["Observability<br/>Prometheus, Jaeger, Loki"]
    end
    
    Frontend --> API
    API --> DF
    DF --> SQL
    DF --> Storage
    API --> Obs
    
    note["Setup: ./scripts/setup.sh<br/>Installs .NET 9, Node.js 22,<br/>runs migrations, seeds test data"]
    
    style Aspire fill:#61dafb,color:#000
```

### Staging Environment (Azure)

```mermaid
graph TB
    subgraph Staging["Staging: linksy-staging"]
        subgraph Frontend["Frontend"]
            F1["Static Web Apps<br/>React SPA + CI/CD"]
        end
        
        subgraph API["API Control Plane"]
            A1["App Service<br/>ASP.NET Core 9<br/>Autoscale 2-10"]
        end
        
        subgraph Orch["Job Orchestration"]
            O1["Function App<br/>Premium plan<br/>Durable Functions"]
        end
        
        subgraph Data["Data Store"]
            D1["Azure SQL<br/>Serverless<br/>Auto-pause"]
            D2["Storage Account<br/>Queues, blobs, tables"]
            D3["Key Vault<br/>Secrets, keys"]
        end
        
        subgraph Obs["Observability"]
            O2["App Insights<br/>Standard tier"]
            O3["Log Analytics<br/>Workspace"]
        end
        
        subgraph Net["Networking"]
            N1["VNet<br/>Private endpoints<br/>SQL, KV, Storage"]
        end
    end
    
    F1 --> A1
    A1 --> O1
    O1 --> D1
    O1 --> D2
    O1 --> D3
    A1 --> O2
    O1 --> O3
    D1 -.-> N1
    D2 -.-> N1
    D3 -.-> N1
    
    style Staging fill:#fff3e0
    style Data fill:#c8e6c9
```

### Production Environment (Multi-Region)

```mermaid
graph TB
    subgraph Primary["Primary Region: East US"]
        subgraph Frontend["Frontend"]
            F1["Static Web Apps<br/>Global CDN"]
        end
        
        subgraph API["API Control Plane"]
            A1["Function App<br/>Consumption plan<br/>Auto-scale to 200"]
            A2["Traffic Manager<br/>Global load balancing"]
        end
        
        subgraph Orch["Job Orchestration"]
            O1["Function App<br/>Premium plan<br/>Min 1, Scale 50+"]
            O2["Storage for State<br/>Durable Functions"]
        end
        
        subgraph Data["Data Store"]
            D1["Azure SQL<br/>Serverless + Backups<br/>GRS enabled"]
            D2["Storage Account<br/>RA-GRS + Lifecycle"]
            D3["Key Vault<br/>Geo-replication"]
        end
        
        subgraph Obs["Observability"]
            O3["App Insights<br/>HIPAA compliant"]
            O4["Log Analytics<br/>GRS enabled"]
        end
    end
    
    subgraph Secondary["Secondary: West Europe"]
        R1["Read-only SQL replicas"]
        R2["Failover storage"]
        R3["Hot standby Functions"]
    end
    
    D1 -->|Read replicas| R1
    D2 -->|Geo-redundant| R2
    A1 -.->|Failover| R3
    A2 -->|Health check 30s| R1
    
    style Primary fill:#e3f2fd
    style Secondary fill:#f5f5f5
    style Data fill:#c8e6c9
    style Obs fill:#fff9c4
```

**Deployment**: Infrastructure as Code (Bicep or Terraform) with parameterized environment configs.

---

## Technology Stack

### Backend (.NET 9.0)

| Component | Version | Purpose |
|-----------|---------|---------|
| ASP.NET Core | 9.0 | Minimal API pattern, fast HTTP endpoints |
| Azure Functions | v4 | Serverless compute (HTTP triggers, Durable Functions) |
| Azure Durable Functions | 2.x | Orchestration, state management, activity pattern |
| Entity Framework Core | 9.0 | ORM, SQL migrations, tenant isolation queries |
| Azure Identity | 1.11+ | Managed identity, Key Vault access |
| Azure Storage | 12.x | Queues, blobs, table storage SDKs |
| Azure Service Bus | 7.x | Messaging (future upgrade from Queues) |
| OpenTelemetry | 1.13.0 | Distributed tracing, metrics export |
| xUnit | 2.9.3 | Unit testing framework |
| Moq | Latest | Mocking library for integration tests |

### Frontend (TypeScript 5.3+)

| Component | Version | Purpose |
|-----------|---------|---------|
| React | 19.0+ | UI framework (strict mode) |
| Vite | 5.0+ | Modern bundler, HMR <3s |
| TypeScript | 5.3+ | Type safety, strict mode enabled |
| Tailwind CSS | 4.0+ | Utility-first styling |
| shadcn/ui | Latest | Accessible component library (Radix + Tailwind) |
| Axios | Latest | HTTP client with JWT interceptors |
| React Router | Latest | SPA routing |
| Vitest | 1.0+ | Unit testing (frontend) |
| React Testing Library | 15.0+ | Component testing utilities |

### Infrastructure & Observability

| Component | Version | Purpose |
|-----------|---------|---------|
| Azure Functions Runtime | 4.0 | Serverless execution |
| Azure SQL Database | Any (Serverless) | Multi-tenant data store |
| Azure Storage Account | Standard | Queues, blobs, tables |
| Azure Key Vault | — | Credential encryption |
| Application Insights | Standard | Metrics, traces, alerting |
| Azure Static Web Apps | — | Frontend CDN + auth |
| Bicep or Terraform | Latest | Infrastructure as Code |

---

## Architecture Decision Records

### ADR-001: Serverless-First Architecture

**Decision**: Deploy on Azure Functions (Consumption plan) + Durable Functions instead of AKS or App Service.

**Rationale**:
- **Cost**: Pay-per-execution scales to near-zero idle; avoids container orchestration overhead
- **Simplicity**: No cluster management, no container ops
- **Compliance**: Stateless functions align with audit trail immutability

**Trade-offs**:
- Cold start latency (~1-2s first request); mitigated by Premium plan for production
- Durable Functions state persisted to Storage; eventual consistency model
- Max execution timeout 10 minutes; long-running jobs split into activities

**Alternatives Considered**:
- Azure Kubernetes Service (AKS): overkill for MVP, higher operational cost
- App Service: fixed cost, less elastic for bursty workloads

---

### ADR-002: Append-Only Audit Trail with Hash Chain

**Decision**: Store audit entries in append-only SQL table with database-level DELETE constraints and SHA256 hash chain.

**Rationale**:
- **Compliance**: Immutable log required for regulated AEC environments (SOC 2, HIPAA)
- **Tamper Detection**: Hash chain validation detects post-hoc modifications
- **Auditability**: Complete record of all sync decisions, actor, timestamp

**Trade-offs**:
- Append-only table grows unbounded; requires archival/retention policy
- Hash chain computation adds 1-2ms per entry; acceptable for compliance requirement
- Export performance scales with audit volume; pagination mitigates

**Alternatives Considered**:
- Event Sourcing in Event Store: external service, adds complexity
- Blob-only audit (WORM): no queryability, harder to export by date range

---

### ADR-003: Durable Entities for Oscillation Prevention

**Decision**: Use Durable Entities (state actors) to implement 5-minute binding holds per file, queuing opposite-platform changes during hold window.

**Rationale**:
- **Deterministic**: Prevents sync loops without central lock coordination
- **Scalable**: Entity per `(tenant:binding:fileId)` distributes state
- **Built-in**: Durable Functions native feature, no external state store

**Trade-offs**:
- Hold window is approximate (Durable Function execution may delay release)
- Queue of pending changes stored in entity; memory overhead for high-change workloads
- Manual testing of hold behavior requires Durable Functions emulator setup

**Alternatives Considered**:
- Redis-backed hold window: external dependency, adds latency
- BindingState table with hold timestamp: coarse-grained (binding-level, not per-file)

---

### ADR-004: Multi-Connector Interface (IConnector Pattern)

**Decision**: Abstract connector implementation behind `IConnector` interface for ACC and SharePoint, enabling future connectors (OneDrive, Google Drive, Box).

**Rationale**:
- **Extensibility**: New connectors pluggable without core changes
- **Testability**: Mock connectors for unit tests
- **Consistency**: Delta token handling, checksum validation, backoff retry unified

**Trade-offs**:
- Requires upfront interface design; delays MVP if design incomplete
- Connector SDKs vary (ACC REST, Microsoft Graph); adapter overhead
- Activity functions hard-coded for ACC/SP; generalization deferred to Phase 2

**Alternatives Considered**:
- Monolithic controller (if/else on connector type): tightly coupled
- Plugin architecture (DLL loading): complex, risky at runtime

---

### ADR-005: React 19 + TypeScript Strict Mode

**Decision**: Frontend built exclusively in TypeScript with `strict: true` in `tsconfig.json`. No JavaScript files allowed in `src/frontend/src/`.

**Rationale**:
- **Type Safety**: Catches runtime errors at compile time
- **Compliance with Linksy Guidelines**: Enforced by project constitution
- **Maintainability**: Clear interfaces, self-documenting code

**Trade-offs**:
- Slower initial development (verbose type definitions)
- Requires discipline to avoid `any` types
- Larger initial bundle (mitigated by tree-shaking, Vite optimization)

**Alternatives Considered**:
- Mixed TypeScript + JavaScript: inconsistent, error-prone
- JavaScript only: loses type safety, conflicts with project standards

---

### ADR-006: JWT + HttpOnly Cookies (Stateless Auth)

**Decision**: Stateless JWT authentication with 12-hour expiry and refresh tokens stored in HttpOnly cookies.

**Rationale**:
- **Stateless**: No backend session store required; scales horizontally
- **XSS Resilience**: HttpOnly cookies prevent JavaScript access
- **OWASP Recommended**: Current best practice for SPAs
- **Mobile-Friendly**: Tokens can be passed via Authorization header

**Trade-offs**:
- Refresh token rotation required for key rotation
- Session revocation not immediate (JWT valid until expiry); mitigated by token blacklist for logout
- Requires CSRF tokens for state-changing operations (POST, PUT, DELETE)

**Alternatives Considered**:
- Session-based (cookie): requires backend session store, harder to scale
- Implicit flow (localStorage): XSS vulnerability, deprecated by OAuth 2.0
- Passwordless (FIDO2): future enhancement, not MVP

---

### ADR-007: Azure Blob Storage Lifecycle Policies for Compliance

**Decision**: Three blob containers (transient-cache, quarantine, audit-exports) with lifecycle policies for auto-deletion (24h, 7d) and WORM+versioning on exports.

**Rationale**:
- **Compliance**: Automatic lifecycle reduces manual data retention overhead
- **Cost**: Removes stale transient cache, automatic purge of quarantine
- **Immutability**: WORM + versioning on audit exports detect tampering

**Trade-offs**:
- Lifecycle policies are eventual (run once per day); short-lived files may persist briefly
- WORM adds storage cost (versioning); acceptable for audit trail (low churn)
- No fine-grained access control per file; container-level ACLs only

**Alternatives Considered**:
- Custom TTL in application code: fragile, manual cleanup burden
- Azure Data Lake for archive: over-engineered for MVP

---

### ADR-008: Storage Queues (MVP) → Service Bus (Future)

**Decision**: Use Azure Storage Queues for MVP (cheapest option). Upgrade to Service Bus with sessions for strict FIFO if per-binding ordering becomes critical.

**Rationale**:
- **MVP**: Low cost, sufficient for MVP scale; poison queue + visibility timeout handle retries
- **Future-Proof**: Service Bus upgrade transparent to application logic (both are queue abstractions)

**Trade-offs**:
- Storage Queues have eventual consistency (seconds delay); acceptable for retry backlog
- No native message sessions; per-binding FIFO requires application coordination
- Visibility timeout max 7 days; long-held retries move to DLQ

**Alternatives Considered**:
- Event Grid: event-driven, not ideal for retry queues
- Azure Service Bus from day 1: higher cost, unnecessary for MVP

---

## Conclusion

This architecture delivers a **secure, scalable, compliance-grade multi-tenant SaaS platform** with:

✅ **Cost Efficiency**: Serverless scales to near-zero idle; pay-per-execution model aligns with unpredictable sync workloads.  
✅ **Deterministic Sync**: Durable Functions + delta tokens + oscillation prevention ensure no data loss or duplication.  
✅ **Audit Compliance**: Append-only trail + hash chain + WORM exports satisfy SOC 2, HIPAA, regulatory requirements.  
✅ **Operational Excellence**: OpenTelemetry observability, per-tenant metrics, alerting, manual-hold safety valves.  
✅ **Developer Experience**: Aspire local orchestration, TypeScript strict mode, React 19 modern UX, comprehensive testing.  

All decisions documented with trade-offs and rationale for future evolution.
