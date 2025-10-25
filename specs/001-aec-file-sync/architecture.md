# AEC File Sync Platform – Solution Architecture

**Feature**: 001-aec-file-sync | **Date**: 2025-10-25  
**Purpose**: Multi-tenant SaaS file synchronization between Autodesk Construction Cloud and Microsoft SharePoint Online

---

## 1. High-Level System Architecture

```mermaid
graph TB
    subgraph "External Systems"
        ACC["Autodesk Construction Cloud<br/>(ACC Docs)"]
        SP["Microsoft SharePoint<br/>Online"]
    end

    subgraph "Frontend Layer"
        React["React 19 SPA<br/>(TypeScript + Vite)<br/>localhost:5173"]
    end

    subgraph "API Layer"
        Gateway["API Gateway<br/>(ASP.NET Core 9)<br/>localhost:5000"]
    end

    subgraph "Application Services"
        AuthSvc["Auth Service<br/>(JWT + Identity)"]
        SyncSvc["Sync Engine<br/>(Change Tracking)"]
        BindingSvc["Binding Service<br/>(CRUD + Config)"]
        AuditSvc["Audit Service<br/>(Append-Only Log)"]
        ConnectorSvc["Connector Service<br/>(ACC + SP SDKs)"]
        RetroSvc["Retry Service<br/>(Exponential Backoff)"]
        ConflictSvc["Conflict Resolution<br/>(Policy Engine)"]
    end

    subgraph "Data Layer"
        DB["SQL Server<br/>(Identity + Domain)"]
        Cache["Redis Cache<br/>(Session + Tokens)"]
    end

    subgraph "Infrastructure"
        Aspire["Aspire Orchestration<br/>(Service Discovery)"]
        OTEL["OpenTelemetry<br/>(Metrics + Traces)"]
    end

    React -->|JWT Bearer| Gateway
    Gateway -->|Dispatch| AuthSvc
    Gateway -->|Dispatch| SyncSvc
    Gateway -->|Dispatch| BindingSvc
    Gateway -->|Dispatch| AuditSvc

    SyncSvc -->|Orchestrate| ConnectorSvc
    SyncSvc -->|Orchestrate| ConflictSvc
    SyncSvc -->|Orchestrate| RetroSvc

    ConnectorSvc -->|OAuth App-Only| ACC
    ConnectorSvc -->|Microsoft Graph| SP

    AuthSvc -->|Read/Write| DB
    SyncSvc -->|Read/Write| DB
    BindingSvc -->|Read/Write| DB
    AuditSvc -->|Append| DB

    AuthSvc -->|Token Storage| Cache
    SyncSvc -->|Job State| Cache

    Gateway -->|Register| Aspire
    React -->|Register| Aspire

    Gateway -->|Emit| OTEL
    SyncSvc -->|Emit| OTEL

    style React fill:#61dafb,stroke:#333,color:#000
    style Gateway fill:#512bd4,stroke:#333,color:#fff
    style AuthSvc fill:#00a4ef,stroke:#333,color:#fff
    style SyncSvc fill:#00a4ef,stroke:#333,color:#fff
    style DB fill:#336791,stroke:#333,color:#fff
    style ACC fill:#ff6b35,stroke:#333,color:#fff
    style SP fill:#0078d4,stroke:#333,color:#fff
```

---

## 2. Frontend Architecture

```mermaid
graph LR
    subgraph "React 19 Application"
        subgraph "Pages & Routing"
            Login["Login / SSO"]
            Dashboard["Dashboard"]
            Onboarding["Onboarding Wizard"]
            BindingMgmt["Binding Management"]
            ActivityLog["Activity Log"]
            AuditViewer["Audit Viewer"]
            AdminPanel["Admin Panel"]
        end

        subgraph "Components (shadcn/ui)"
            UI["Button, Dialog, Table<br/>Form, Input, etc."]
        end

        subgraph "State & Hooks"
            AuthHook["useAuth<br/>(JWT + Refresh)"]
            RoleHook["useRole<br/>(RBAC Check)"]
            WSHook["useWebSocket<br/>(Real-time)"]
            QueryHook["useQuery<br/>(API Cache)"]
        end

        subgraph "Services"
            APIClient["API Client<br/>(Axios)"]
            AuthSvc["Auth Service<br/>(SSO, Token Mgmt)"]
        end
    end

    Login -->|authenticate| AuthSvc
    AuthSvc -->|issue JWT| AuthHook
    Dashboard -->|check role| RoleHook
    ActivityLog -->|subscribe| WSHook
    Onboarding -->|POST /api/bindings| APIClient
    BindingMgmt -->|GET /api/bindings| APIClient
    APIClient -->|interceptor: JWT| APIClient

    style React fill:#61dafb,stroke:#333,color:#000
    style AuthHook fill:#fbbf24,stroke:#333,color:#000
    style RoleHook fill:#fbbf24,stroke:#333,color:#000
    style WSHook fill:#fbbf24,stroke:#333,color:#000
```

---

## 3. Backend Layered Architecture

```mermaid
graph TB
    subgraph "Presentation Layer"
        REST["REST Endpoints<br/>(Minimal API)"]
        WS["WebSocket<br/>(Real-time Activity)"]
    end

    subgraph "Middleware & Security"
        AuthMW["Auth Middleware<br/>(JWT Validation)"]
        RbacMW["RBAC Middleware<br/>(Role Check)"]
        ExcMW["Exception Middleware<br/>(Global Error)"]
    end

    subgraph "Application Services"
        AuthSvc["AuthService<br/>(JWT Generation)"]
        SyncSvc["SyncService<br/>(Job Orchestration)"]
        BindingSvc["BindingService<br/>(CRUD)"]
        ConflictSvc["ConflictResolutionService<br/>(Policy Enforcement)"]
        RetroSvc["RetryService<br/>(Exponential Backoff)"]
        AuditSvc["AuditService<br/>(Immutable Logging)"]
        QuarSvc["QuarantineService<br/>(Soft-Delete)"]
        WebhookSvc["WebhookService<br/>(Signature Validation)"]
        ObsSvc["ObservabilityService<br/>(OpenTelemetry)"]
    end

    subgraph "Domain Models"
        Models["Entities:<br/>ApplicationUser, Tenant<br/>Binding, SyncJob<br/>ChangeItem, AuditEntry"]
    end

    subgraph "Data Access Layer"
        DBCtx["ApplicationDbContext<br/>(EF Core)"]
        Repo["Repository Pattern<br/>(if needed)"]
    end

    subgraph "Infrastructure"
        SQL["SQL Server<br/>Database"]
        Scheduler["Background Scheduler<br/>(Sync Job Runner)"]
        PollingFallback["Polling Service<br/>(Webhook Fallback)"]
    end

    REST -->|validate JWT| AuthMW
    REST -->|check role| RbacMW
    REST -->|handle errors| ExcMW

    AuthMW -->|delegate| AuthSvc
    ExcMW -->|delegate| SyncSvc
    ExcMW -->|delegate| BindingSvc
    ExcMW -->|delegate| AuditSvc

    SyncSvc -->|uses| ConflictSvc
    SyncSvc -->|uses| RetroSvc
    SyncSvc -->|uses| QuarSvc
    SyncSvc -->|uses| WebhookSvc

    AuthSvc -->|persist| DBCtx
    SyncSvc -->|query/mutate| DBCtx
    BindingSvc -->|query/mutate| DBCtx
    AuditSvc -->|append| DBCtx

    DBCtx -->|EF Core| SQL

    Scheduler -->|trigger| SyncSvc
    PollingFallback -->|fallback| WebhookSvc

    SyncSvc -->|emit| ObsSvc
    ObsSvc -->|export| OTEL["OpenTelemetry Exporter"]

    style AuthMW fill:#fbbf24,stroke:#333,color:#000
    style RbacMW fill:#fbbf24,stroke:#333,color:#000
    style AuthSvc fill:#00a4ef,stroke:#333,color:#fff
    style SyncSvc fill:#00a4ef,stroke:#333,color:#fff
    style SQL fill:#336791,stroke:#333,color:#fff
```

---

## 4. Data Model Architecture

```mermaid
erDiagram
    TENANT {
        uuid tenant_id PK
        string name
        string subscription_tier
        datetime created_at
    }

    APPLICATION_USER {
        string user_id PK
        string email
        string display_name
        datetime created_at
    }

    TENANT_USER_ROLE {
        uuid tenant_id FK
        string user_id FK
        string role "Admin|Operator|Auditor"
    }

    CONNECTOR {
        uuid connector_id PK
        uuid tenant_id FK
        string type "ACC|SharePoint"
        string credential_vault_ref
        datetime health_check_at
        boolean is_healthy
    }

    BINDING {
        uuid binding_id PK
        uuid tenant_id FK
        uuid source_connector_id FK
        uuid target_connector_id FK
        string direction "OneWay|BiDirectional"
        string conflict_policy "SourceWins|TargetWins|LastWriterWins|ManualHold"
        string source_folder_path
        string target_folder_path
        datetime schedule_cron
        string status "Active|Paused|Error"
        datetime created_at
    }

    SYNC_JOB {
        uuid job_id PK
        uuid binding_id FK
        datetime started_at
        datetime completed_at
        string status "Pending|Running|Success|Failure|Partial"
        int total_changes
        int successful_changes
        int failed_changes
        string error_detail
    }

    CHANGE_ITEM {
        uuid change_id PK
        uuid sync_job_id FK
        uuid binding_id FK
        string source_path
        string target_path
        string operation "Create|Update|Delete|Move|Rename"
        string status "Pending|Processing|Success|ManualHold|Failed"
        string checksum
        datetime created_at
    }

    CONFLICT_RECORD {
        uuid conflict_id PK
        uuid binding_id FK
        uuid change_id FK
        string conflict_type "SourceWins|TargetWins|LastWriterWins|ManualHold"
        string source_version
        string target_version
        string resolution "Pending|AppliedSource|AppliedTarget|Purged"
        datetime detected_at
        datetime resolved_at
    }

    CREDENTIAL_SECRET {
        uuid credential_id PK
        uuid connector_id FK
        string encrypted_secret_material
        string credential_type "ACC_APP_KEY|SP_CLIENT_SECRET"
        datetime created_at
        datetime rotated_at
        boolean is_active
    }

    AUDIT_ENTRY {
        uuid audit_id PK
        uuid tenant_id FK
        string actor "UserId or SYSTEM"
        string action "SyncCompleted|ConflictResolved|BindingCreated"
        string context_json "Job details, binding ID, etc."
        uuid previous_hash
        uuid current_hash
        datetime created_at_utc "Server-side"
    }

    QUARANTINE_ENTRY {
        uuid quarantine_id PK
        uuid binding_id FK
        string file_path
        string reason "SoftDelete"
        datetime quarantined_at
        datetime expires_at "7 days out"
        boolean is_restored
    }

    BINDING_HOLD_STATE {
        uuid hold_id PK
        uuid binding_id FK
        string file_path
        datetime expires_at "5 minutes out"
        string queued_changes_json
    }

    TENANT ||--o{ APPLICATION_USER : has
    APPLICATION_USER ||--o{ TENANT_USER_ROLE : assigned
    TENANT_USER_ROLE }o--|| TENANT : scopes
    TENANT ||--o{ CONNECTOR : has
    TENANT ||--o{ BINDING : contains
    CONNECTOR ||--o{ BINDING : "source\ntarget"
    BINDING ||--o{ SYNC_JOB : triggers
    SYNC_JOB ||--o{ CHANGE_ITEM : contains
    BINDING ||--o{ CHANGE_ITEM : tracks
    CHANGE_ITEM ||--o{ CONFLICT_RECORD : generates
    BINDING ||--o{ CONFLICT_RECORD : scopes
    CONNECTOR ||--o{ CREDENTIAL_SECRET : stores
    TENANT ||--o{ AUDIT_ENTRY : logs
    BINDING ||--o{ QUARANTINE_ENTRY : manages
    BINDING ||--o{ BINDING_HOLD_STATE : maintains
```

---

## 5. Authentication & Authorization Flow

```mermaid
sequenceDiagram
    participant User as Frontend User
    participant React as React SPA
    participant Entra as Entra ID SSO
    participant API as ASP.NET Core API
    participant JWT as JWT Service
    participant DB as SQL Server

    User->>React: Click "Login"
    React->>Entra: Redirect to Entra ID
    Entra->>Entra: Authenticate User
    Entra->>React: Return Auth Code
    React->>API: POST /auth/register (code)
    API->>Entra: Exchange Code for Token (via backend)
    Entra->>API: Access Token + Claims
    API->>JWT: Generate JWT + Refresh Token
    JWT->>DB: Lookup/Create ApplicationUser
    DB->>JWT: User + TenantRoles
    JWT->>API: JWT + Refresh Token
    API->>React: JWT (HttpOnly Cookie) + Refresh Token
    React->>React: Store Tokens Securely
    React->>API: GET /api/bindings (+ JWT)
    API->>API: Auth Middleware validates JWT
    API->>API: RBAC Middleware checks role claim
    API->>DB: Query Bindings for Tenant
    DB->>API: Bindings
    API->>React: 200 OK + Bindings JSON

    User->>User: 12 hours pass
    React->>API: POST /auth/refresh (refresh_token)
    API->>JWT: Validate Refresh Token
    JWT->>JWT: Issue New JWT
    JWT->>API: New JWT
    API->>React: New JWT (HttpOnly Cookie)
    React->>React: Auto-use New JWT on next request
```

---

## 6. Sync Job Orchestration & Conflict Resolution

```mermaid
graph TB
    Start["Sync Job Triggered<br/>(Scheduled or Manual)"] -->|Validate| BindingCheck{"Binding<br/>Active?"}

    BindingCheck -->|No| Error1["❌ Pause Binding<br/>Notify Admin"]
    BindingCheck -->|Yes| GetToken["Get Credentials<br/>(from Vault)"]

    GetToken -->|Valid| FetchDelta["Fetch Delta Tokens<br/>(Webhook + Polling)"]
    GetToken -->|Invalid| CredError["❌ Escalate to<br/>Manual Credential<br/>Rotation"]

    FetchDelta -->|Changes Found| GroupByFile["Group Changes<br/>by File Path"]
    FetchDelta -->|No Changes| Complete["✅ Sync Complete<br/>Log Event"]

    GroupByFile --> CheckHold{"Binding Hold<br/>Active?"}

    CheckHold -->|Yes| Queue["Queue Changes<br/>Until Hold Expires"]
    CheckHold -->|No| ApplyPolicy["Apply Conflict<br/>Policy"]

    Queue -->|Hold Expires| ApplyPolicy

    ApplyPolicy --> PolicyDecision{"Policy<br/>Decision"}

    PolicyDecision -->|SourceWins| UseSrc["Use Source<br/>Version"]
    PolicyDecision -->|TargetWins| UseTgt["Use Target<br/>Version"]
    PolicyDecision -->|LastWriterWins| CheckTS{"Timestamp<br/>Valid?"}
    PolicyDecision -->|ManualHold| Manual["Move to Quarantine<br/>Alert Operator"]

    CheckTS -->|Within Clock Skew| Resolve["Resolve by<br/>Latest Timestamp"]
    CheckTS -->|Outside Skew| Ambiguous["❓ Treat as<br/>Manual Hold"]

    UseSrc --> Sync["Sync to Target<br/>via Connector SDK"]
    UseTgt --> Sync
    Resolve --> Sync

    Sync -->|Success| AuditLog["✅ Audit Log<br/>+ Metrics"]
    Sync -->|HTTP 429| Backoff["Exponential Backoff<br/>(7 retries, 120s min)"]

    Backoff -->|Retries Exhausted| Escalate["❌ Escalate to<br/>ManualHold<br/>Alert Operator"]
    Backoff -->|Success| AuditLog

    Sync -->|Other Error| Retry["Retry<br/>(up to 3x)"]
    Retry -->|Success| AuditLog
    Retry -->|Failed| LogError["❌ Log Error<br/>Continue"]

    AuditLog --> EnforceHold["Enforce 5-min Hold<br/>on Same File"]
    EnforceHold -->|Hold Active| QueueOpp["Queue Opposite<br/>Platform Changes"]
    EnforceHold -->|Hold Inactive| NextChange["Process Next<br/>Change"]

    QueueOpp --> NextChange
    NextChange -->|More Changes| CheckHold
    NextChange -->|No More Changes| Final["✅ Job Complete<br/>Record Metrics"]

    Manual --> OperatorWait["⏳ Awaiting<br/>Operator Decision"]
    Escalate --> OperatorWait

    OperatorWait -->|Resolve: Use Source| UseSrc
    OperatorWait -->|Resolve: Use Target| UseTgt
    OperatorWait -->|Delete Both| PurgeBoth["Delete from Both<br/>Platforms"]

    PurgeBoth --> Final

    Error1 --> End["Job End"]
    CredError --> End
    Complete --> End
    Final --> End

    style Start fill:#90ee90,stroke:#333,color:#000
    style Complete fill:#90ee90,stroke:#333,color:#000
    style Final fill:#90ee90,stroke:#333,color:#000
    style Manual fill:#fbbf24,stroke:#333,color:#000
    style Escalate fill:#ff6b6b,stroke:#333,color:#fff
    style Error1 fill:#ff6b6b,stroke:#333,color:#fff
    style OperatorWait fill:#ffeb3b,stroke:#333,color:#000
```

---

## 7. Audit Trail & Compliance

```mermaid
graph TB
    subgraph "Write Path"
        SyncAction["Sync Action<br/>(e.g., File Updated)"]
        AuditSvc["AuditService<br/>appends AuditEntry"]
        PrevHash["Calculate<br/>Previous Hash<br/>(from DB)"]
        CreateEntry["Create New Entry<br/>+ Current Hash"]
        InsertDB["INSERT into<br/>AuditEntry (append-only)"]
    end

    subgraph "Database Constraints"
        DBConstraint["❌ DELETE trigger blocks<br/>✅ Server-side timestamp<br/>✅ Insert-only policy"]
    end

    subgraph "Export & Verification"
        ExportReq["Auditor Requests<br/>Export"]
        FilterAudit["Filter by Date<br/>+ Binding"]
        BuildChain["Rebuild Hash Chain<br/>from DB"]
        VerifyIntegrity["Verify No Gaps<br/>Hash Match"]
        GenerateReport["Generate PDF/CSV<br/>+ Verification Badge"]
        ReturnReport["Return to Auditor"]
    end

    subgraph "Tampering Detection"
        TamperCheck["❌ If Hash Mismatch<br/>Detected"]
        TamperAlert["🚨 Alert:<br/>Post-hoc tampering"]
    end

    SyncAction --> AuditSvc
    AuditSvc --> PrevHash
    PrevHash --> CreateEntry
    CreateEntry --> InsertDB
    InsertDB --> DBConstraint

    ExportReq --> FilterAudit
    FilterAudit --> BuildChain
    BuildChain --> VerifyIntegrity
    VerifyIntegrity -->|All Hashes Match| GenerateReport
    VerifyIntegrity -->|Hash Mismatch| TamperCheck
    TamperCheck --> TamperAlert

    GenerateReport --> ReturnReport

    style SyncAction fill:#61dafb,stroke:#333,color:#000
    style DBConstraint fill:#90ee90,stroke:#333,color:#000
    style VerifyIntegrity fill:#fbbf24,stroke:#333,color:#000
    style TamperAlert fill:#ff6b6b,stroke:#333,color:#fff
```

---

## 8. Deployment Architecture (Multi-Tenant SaaS)

```mermaid
graph TB
    subgraph "Development (Local)"
        AspireDev["Aspire Orchestrator<br/>(localhost:15217)"]
        FrontendDev["React Dev Server<br/>(localhost:5173)"]
        APIDev["API Server<br/>(localhost:5000)"]
        LocalDB["SQL Server LocalDB<br/>(local)"]
        AspireDev -->|manages| FrontendDev
        AspireDev -->|manages| APIDev
        APIDev -->|connects| LocalDB
    end

    subgraph "Staging (Azure)"
        AppServiceStg["App Service<br/>(ASP.NET Core)"]
        FrontendStg["Static Web App<br/>(React SPA)"]
        SQLStg["SQL Database<br/>(Managed)"]
        KeyVaultStg["Key Vault<br/>(Secrets)"]
        AppInsightsStg["Application Insights<br/>(Monitoring)"]

        FrontendStg -->|API Calls| AppServiceStg
        AppServiceStg -->|Queries| SQLStg
        AppServiceStg -->|Fetch Secrets| KeyVaultStg
        AppServiceStg -->|Emit Traces| AppInsightsStg
    end

    subgraph "Production (Azure + Multi-Region)"
        subgraph "Primary Region"
            LoadBalancer["Load Balancer"]
            AKSPri["AKS Cluster<br/>(API Pods)"]
            CDNPri["CDN<br/>(React SPA)"]
            SQLPri["SQL Database<br/>(Primary + Read Replicas)"]
            KeyVaultPri["Key Vault"]
            AppInsightsPri["Application Insights"]
        end

        subgraph "Secondary Region"
            AKSSec["AKS Cluster<br/>(Standby)"]
            SQLSec["SQL Database<br/>(Replica)"]
        end

        LoadBalancer -->|Route| AKSPri
        LoadBalancer -->|Route| AKSSec
        CDNPri -->|Cache| CDNPri
        AKSPri -->|Primary| SQLPri
        AKSSec -->|Read| SQLSec
        AKSPri -->|Replicate| SQLSec
        AKSPri -->|Fetch Secrets| KeyVaultPri
        AKSPri -->|Emit Metrics| AppInsightsPri
    end

    style AspireDev fill:#512bd4,stroke:#333,color:#fff
    style AppServiceStg fill:#0078d4,stroke:#333,color:#fff
    style LoadBalancer fill:#ff6b35,stroke:#333,color:#fff
    style AKSPri fill:#0078d4,stroke:#333,color:#fff
    style SQLPri fill:#336791,stroke:#333,color:#fff
```

---

## 9. API Gateway & Endpoint Structure

```mermaid
graph TB
    Gateway["API Gateway<br/>(ASP.NET Core Minimal API)<br/>Base: /api/v1"]

    subgraph "Auth Endpoints"
        EP1["POST /auth/register<br/>(Entra ID + create user)"]
        EP2["POST /auth/login<br/>(JWT issue)"]
        EP3["POST /auth/refresh<br/>(Refresh token)"]
        EP4["POST /auth/logout<br/>(Revoke)"]
    end

    subgraph "Binding Endpoints"
        EP5["GET /bindings<br/>(List user's bindings)"]
        EP6["POST /bindings<br/>(Create binding)"]
        EP7["GET /bindings/{id}<br/>(View binding)"]
        EP8["PUT /bindings/{id}<br/>(Update config)"]
        EP9["DELETE /bindings/{id}<br/>(Soft-delete)"]
        EP10["POST /bindings/{id}/sync/trigger<br/>(Manual sync)"]
        EP11["POST /bindings/{id}/pause<br/>(Pause binding)"]
    end

    subgraph "Sync Job Endpoints"
        EP12["GET /sync/jobs<br/>(List jobs)"]
        EP13["GET /sync/jobs/{jobId}<br/>(Job status)"]
        EP14["GET /sync/jobs/{jobId}/changes<br/>(Changes detail)"]
    end

    subgraph "Activity Log Endpoints"
        EP15["GET /activity<br/>(Activity history)"]
        EP16["WS /ws/activity/stream<br/>(Real-time)"]
    end

    subgraph "Conflict Resolution Endpoints"
        EP17["GET /conflicts/quarantine<br/>(List quarantined)"]
        EP18["POST /conflicts/{conflictId}/resolve<br/>(Choose source/target)"]
    end

    subgraph "Audit Endpoints"
        EP19["GET /audit<br/>(Search audit trail)"]
        EP20["POST /audit/export<br/>(Export with hash verify)"]
    end

    subgraph "Admin Endpoints"
        EP21["GET /tenants/{tenantId}/users<br/>(List users)"]
        EP22["POST /tenants/{tenantId}/users/{userId}/roles<br/>(Assign role)"]
        EP23["POST /connectors/credentials/rotate<br/>(Rotate creds)"]
    end

    Gateway -->|Auth| EP1
    Gateway -->|Auth| EP2
    Gateway -->|Auth| EP3
    Gateway -->|Auth| EP4

    Gateway -->|Binding| EP5
    Gateway -->|Binding| EP6
    Gateway -->|Binding| EP7
    Gateway -->|Binding| EP8
    Gateway -->|Binding| EP9
    Gateway -->|Binding| EP10
    Gateway -->|Binding| EP11

    Gateway -->|Sync| EP12
    Gateway -->|Sync| EP13
    Gateway -->|Sync| EP14

    Gateway -->|Activity| EP15
    Gateway -->|Activity| EP16

    Gateway -->|Conflict| EP17
    Gateway -->|Conflict| EP18

    Gateway -->|Audit| EP19
    Gateway -->|Audit| EP20

    Gateway -->|Admin| EP21
    Gateway -->|Admin| EP22
    Gateway -->|Admin| EP23

    style Gateway fill:#512bd4,stroke:#333,color:#fff
    style EP1 fill:#00a4ef,stroke:#333,color:#fff
    style EP2 fill:#00a4ef,stroke:#333,color:#fff
    style EP10 fill:#fbbf24,stroke:#333,color:#000
    style EP16 fill:#90ee90,stroke:#333,color:#000
    style EP18 fill:#ff6b6b,stroke:#333,color:#fff
```

---

## 10. Security Architecture

```mermaid
graph TB
    subgraph "Authentication"
        EntID["Entra ID SSO<br/>(OpenID Connect)"]
        JWT["JWT Token<br/>(Stateless)"]
        RefreshToken["Refresh Token<br/>(HttpOnly Cookie)"]
    end

    subgraph "Frontend Security"
        CORS["CORS Policy<br/>(localhost:5173 only)"]
        HTTPOnly["HttpOnly Cookies<br/>(XSS-safe)"]
        MemToken["In-Memory Token<br/>(dev fallback)"]
    end

    subgraph "Backend Security"
        AuthMW["Auth Middleware<br/>(JWT validation)"]
        RbacMW["RBAC Middleware<br/>(Role check)"]
        TenantIsolation["Tenant Isolation<br/>(Query filter + API check)"]
        EncryptionAtRest["Encryption at Rest<br/>(SQL TDE)"]
        EncryptionInTransit["TLS 1.2+<br/>(HTTPS only)"]
    end

    subgraph "Credential Management"
        VaultRef["Credential Vault Ref<br/>(never in code)"]
        KeyVault["Azure Key Vault<br/>(Secrets)"]
        RotationAudit["Rotation Audit Trail<br/>(immutable)"]
    end

    subgraph "Audit & Monitoring"
        AuditLog["Audit Trail<br/>(append-only)"]
        HashChain["Hash Chain<br/>(tampering detect)"]
        SIEM["SIEM Integration<br/>(via logs)"]
    end

    EntID -->|Issue| JWT
    JWT -->|Valid for| AuthMW
    RefreshToken -->|Refresh| JWT

    CORS -->|Allow| HTTPOnly
    HTTPOnly -->|Store| JWT
    MemToken -->|Dev| JWT

    AuthMW -->|Extract Claims| RbacMW
    RbacMW -->|Tenant| TenantIsolation
    TenantIsolation -->|Validate| EncryptionAtRest

    EncryptionInTransit -->|Transmit| VaultRef
    VaultRef -->|Fetch| KeyVault
    KeyVault -->|Manage| RotationAudit

    AuthMW -->|Log Action| AuditLog
    AuditLog -->|Chain| HashChain
    HashChain -->|Export| SIEM

    style EntID fill:#0078d4,stroke:#333,color:#fff
    style JWT fill:#fbbf24,stroke:#333,color:#000
    style KeyVault fill:#336791,stroke:#333,color:#fff
    style AuditLog fill:#90ee90,stroke:#333,color:#000
    style HashChain fill:#90ee90,stroke:#333,color:#000
```

---

## 11. Observability & Monitoring

```mermaid
graph TB
    subgraph "Instrumentation"
        FrontendTrace["Frontend Traces<br/>(React Components)"]
        APITrace["API Traces<br/>(Endpoints, Services)"]
        DBTrace["Database Traces<br/>(Queries)"]
        MetricsEmit["Metrics Emission<br/>(Custom Counters)"]
    end

    subgraph "OpenTelemetry Collector"
        Collector["OTEL Collector<br/>(Local or Remote)"]
        BatchProcessor["Batch Processor<br/>(Reduce Network)"]
    end

    subgraph "Observability Backend"
        Prometheus["Prometheus<br/>(Metrics Storage)"]
        Jaeger["Jaeger<br/>(Trace Backend)"]
        Loki["Loki<br/>(Log Aggregation)"]
    end

    subgraph "Visualization"
        Grafana["Grafana Dashboard<br/>(Metrics + Traces)"]
        AzureMonitor["Azure Monitor<br/>(Production)"]
    end

    subgraph "Alerting"
        AlertMgr["Alertmanager<br/>(Alert Routing)"]
        Slack["Slack Notifications"]
        PagerDuty["PagerDuty<br/>(On-call)"]
    end

    FrontendTrace -->|Export| Collector
    APITrace -->|Export| Collector
    DBTrace -->|Export| Collector
    MetricsEmit -->|Export| Collector

    Collector -->|Process| BatchProcessor
    BatchProcessor -->|Push| Prometheus
    BatchProcessor -->|Push| Jaeger
    BatchProcessor -->|Push| Loki

    Prometheus -->|Query| Grafana
    Jaeger -->|Query| Grafana
    Loki -->|Query| Grafana

    Grafana -->|Display| Grafana

    Prometheus -->|Alert Rule| AlertMgr
    AlertMgr -->|Route| Slack
    AlertMgr -->|Route| PagerDuty

    Prometheus -->|Production| AzureMonitor

    style Collector fill:#00a4ef,stroke:#333,color:#fff
    style Prometheus fill:#ff6b35,stroke:#333,color:#fff
    style Grafana fill:#ff9830,stroke:#333,color:#000
    style Slack fill:#36c5f0,stroke:#333,color:#000
```

---

## 12. Connector Integration Pattern

```mermaid
graph LR
    subgraph "Linksy Platform"
        BindingSvc["Binding Service"]
        ConnectorSvc["Connector Service"]
        WebhookListener["Webhook Listener"]
        PollingScheduler["Polling Scheduler"]
    end

    subgraph "ACC Integration"
        ACCDocs["Autodesk ACC Docs"]
        ACCWebhook["ACC Webhook<br/>(File Changed)"]
        ACCAPI["ACC REST API<br/>(Delta Token)"]
    end

    subgraph "SharePoint Integration"
        SPOnline["SharePoint Online"]
        SPSubscription["SP Subscription<br/>(File Changed)"]
        SPAPI["Microsoft Graph<br/>(Delta Token)"]
    end

    BindingSvc -->|Config| ConnectorSvc
    ConnectorSvc -->|OAuth App-Only| ACCDocs
    ConnectorSvc -->|Microsoft Graph| SPOnline

    ACCDocs -->|POST| ACCWebhook
    ACCWebhook -->|Validate Signature| WebhookListener
    WebhookListener -->|Parse| BindingSvc

    SPOnline -->|POST| SPSubscription
    SPSubscription -->|Validate| WebhookListener
    WebhookListener -->|Parse| BindingSvc

    PollingScheduler -->|Query| ACCAPI
    ACCAPI -->|Delta Token| ConnectorSvc

    PollingScheduler -->|Query| SPAPI
    SPAPI -->|Delta Token| ConnectorSvc

    ConnectorSvc -->|Pull Changes| BindingSvc

    style ConnectorSvc fill:#00a4ef,stroke:#333,color:#fff
    style ACCDocs fill:#ff6b35,stroke:#333,color:#fff
    style SPOnline fill:#0078d4,stroke:#333,color:#fff
```

---

## Architecture Decision Records (ADRs)

| Decision | Rationale | Trade-offs |
|----------|-----------|-----------|
| **Minimal API Pattern** | Lightweight, modern endpoint definition; excellent for microservices | Less middleware flexibility than full ASP.NET Core; requires manual dependency injection |
| **JWT + HttpOnly Cookies** | XSS-safe token storage; OWASP recommended for SPAs | Slightly more complex than localStorage; requires CSRF tokens for mutations |
| **Tenant-Scoped RBAC** | Multi-tenant isolation enforced at API + DB layer | Requires tenant context on every request; query filtering adds complexity |
| **Append-Only Audit Trail** | Compliance requirement for immutability; hash chain detects tampering | Append-only grows unbounded; must implement retention/archival |
| **Webhook + Polling Fallback** | Resilient to provider outages; delta polling detects missed webhooks | Dual strategy increases complexity; polling adds latency |
| **5-Minute Binding Hold** | Prevents sync oscillation; UX-reasonable operator response time | May cause temporary stale state; requires state management complexity |
| **Exponential Backoff (7 retries, 120s)** | Respects service rate limits; spans 2+ rate-limit windows | May fail if limits are <120s; requires tuning per connector |
| **Soft-Delete Quarantine (7 days)** | Operator recovery window; automatic purge prevents indefinite storage | Complicates storage model; requires background purge job |

---

## Technology Stack Summary

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Frontend** | React | 19.0+ | UI framework (TypeScript only) |
| **Frontend Build** | Vite | 5.0+ | Modern bundler w/ HMR |
| **Frontend Styling** | Tailwind CSS | 4.0+ | Utility-first CSS |
| **Frontend Components** | shadcn/ui | Latest | Radix primitives + Tailwind |
| **Backend** | ASP.NET Core | 9.0 | REST API (Minimal API pattern) |
| **Backend Auth** | Identity + JWT | .NET 9.0 | Stateless token-based auth |
| **Backend ORM** | EF Core | 9.0 | SQL Server data access |
| **Database** | SQL Server | 2019+ | Multi-tenant data store |
| **Logging** | ILogger + Serilog | Latest | Structured logs → Aspire/SIEM |
| **Metrics** | OpenTelemetry | 1.13+ | Prometheus-compatible export |
| **Orchestration** | Aspire | 9.5.1 | Service discovery + health checks |
| **Testing** | xUnit + Moq | Latest | Backend unit + integration tests |
| **Testing** | Vitest + RTL | Latest | Frontend component + service tests |

---

## Summary

This architecture delivers a **secure, scalable, multi-tenant SaaS platform** for AEC file synchronization:

- ✅ **Frontend**: React 19 SPA with JWT + RBAC enforcement
- ✅ **Backend**: Layered ASP.NET Core services with clear separation of concerns
- ✅ **Data**: Append-only audit trail, soft-delete quarantine, tenant isolation
- ✅ **Connectors**: Webhook-first with polling fallback for resilience
- ✅ **Observability**: OpenTelemetry metrics, structured logs, real-time activity stream
- ✅ **Security**: HTTPS, TLS, encrypted credentials, RBAC, audit trail + hash chain
- ✅ **Deployment**: Local dev (Aspire), staging (Azure App Service), production (AKS + multi-region failover)

All decisions are documented with trade-offs and rationale for future maintainability and evolution.
