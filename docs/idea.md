## Project Overview: AEC File Sync

### Vision

AEC File Sync is a modern, user-friendly platform designed to synchronize files seamlessly between major online services used in the Architecture, Engineering, and Construction (AEC) industry. The platform eliminates complexity and high costs found in existing tools, providing a reliable and affordable integration layer for firms managing files across ecosystems like Autodesk Construction Cloud and SharePoint.

### Core Objectives

1. **Ease of Use** – Deliver a frictionless setup and configuration experience, with intuitive UI and smart defaults.
2. **Affordability** – Offer flexible pricing models suitable for small teams and large enterprises alike.
3. **Reliability** – Ensure data consistency, version control, and integrity across connected services.
4. **Extensibility** – Build a scalable foundation to easily add support for new platforms like Trimble Connect, Dropbox, OneDrive, etc.
5. **Security & Compliance** – Handle authentication, data transfer, and storage in compliance with industry standards (ISO 19650, GDPR, etc.).

### Phase 1 Scope

* **Supported Services**:

  * Autodesk Construction Cloud (Docs module)
  * Microsoft SharePoint Online
* **Core Features**:

  * Application-level OAuth (headless) authentication for all connectors.
  * Scheduled or on-demand sync between chosen folders.
  * Bidirectional sync with conflict resolution rules.
  * File metadata and version tracking.
  * Activity logs and sync reports.
* **Target Users**:

  * BIM Managers and Digital Engineers.
  * Project coordinators managing cross-platform document flows.
  * Companies using mixed ecosystems (e.g., Autodesk + Microsoft 365).

### Technical Foundations

* **Frontend**: React + TypeScript (Vite or Next.js)
* **Backend**: .NET 8 Web API or ASP.NET Core Minimal API
* **Storage**: PostgreSQL for metadata and configuration; Azure Blob for temporary file cache.
* **Auth & Integrations**: Azure Entra ID for user management; OAuth2 client credentials for external connectors.
* **Deployment**: Azure App Service or Container Apps; optional on-prem connector agents.

### Architecture Blueprint

#### High-Level Components

* **Gateway API (SaaS, multi-tenant)**: Public HTTPS API for UI and automation clients. Auth via Entra ID (OIDC). Enforces tenant scoping.
* **Sync Orchestrator**: Accepts sync requests, expands to **sync jobs**, assigns to workers, manages retries and backoff.
* **Job Queue**: Durable queue (e.g., Azure Storage Queues/Service Bus). Ensures at-least-once execution.
* **Workers**: Stateless executors running **connectors**. Pull jobs, perform delta queries, upload/download, resolve conflicts, emit telemetry.
* **Connector SDK**: Shared abstractions for auth, paging, throttling, delta tokens, conflict policies, and structured logging.
* **Connectors (Phase 1)**: ACC Docs, SharePoint/OneDrive (Graph). Future: Trimble Connect, Procore, Google Drive.
* **Change Detection**: Webhooks where supported; fallback to scheduled delta polling. Store server cursors/delta tokens per binding.
* **Mapping & Rules Engine**: Folder-to-folder bindings, path transforms, include/exclude patterns, filename normalization, metadata policy.
* **Metadata Store**: PostgreSQL schemas for Tenants, Connectors, Bindings, Credentials (external), Jobs, Checkpoints, Versions, Audit.
* **Content Cache (optional)**: Short-lived cache for chunk re-use and retry (Azure Blob). Encrypted at rest.
* **Observability**: OpenTelemetry traces/metrics/logs; per-tenant dashboards; audit trails for all mutations.

#### Data Flow (bidirectional binding)

1. Scheduler enqueues **Scan** jobs for each binding or receives webhook.
2. Worker fetches a **delta** from Source (using stored delta token/cursor), producing a change set.
3. Worker maps changes and checks local **state table** for last known versions/ETags.
4. For each change, choose action in Target (upload, update, move, delete, noop).
5. Apply **conflict policy**; write-through metadata, update checkpoints, emit telemetry.
6. Repeat from Target→Source if binding is bidirectional.

#### Conflict Policies (per binding)

* **SourceWins** (default for system syncs).
* **TargetWins** (compat mode).
* **LastWriterWins** with clock skew guard.
* **ManualHold**: quarantine to a review queue.

#### Multi-Tenancy & Isolation

* Logical tenant partition keys across DB, queues, and storage containers.
* KMS-backed encryption for per-tenant external credentials.
* Rate-limit and per-tenant concurrency caps to respect API quotas.

---

### Authentication & Authorization Strategy

> **Principle:** Automation is fully headless. Only **application permissions** (2-legged OAuth / client credentials) are used across all integrations.

#### 1) Platform Access to Our App (UI/API)

* **User Auth to our UI/API**: Entra ID (OIDC) for humans accessing the management portal.
* **RBAC**: Roles per tenant (Admin, Operator, Auditor). Row-level and tenant scoping on every API call.

#### 2) Connector Auth to External Platforms (Headless Only)

* **Single Mechanism:** OAuth **client credentials** / app-only flows.
* **Options for App Ownership:**

  * **Managed App (default):** We host the multi-tenant app; customers grant tenant/project-scoped consent.
  * **BYO App (enterprise option):** Customer provides their own app registration and secrets/certs. Same app-only flow; no delegated fallback.

#### Token Handling & Secrets

* All external secrets are tenant-isolated and **KMS-encrypted** (Azure Key Vault + per-tenant data-protection keys).
* Strict scope minimization; periodic secret rotation.

#### Permissions by Platform (Phase 1)

* **SharePoint/OneDrive via Microsoft Graph**

  * **Application permissions only** using **Sites.Selected** for least privilege.
  * Admin grants app permission and assigns specific sites/libraries to the app.
  * **Change Detection:** Prefer Graph **webhooks**; fallback to `/delta` queries.
* **Autodesk Construction Cloud (ACC Docs)**

  * **2‑legged app-only OAuth** for server-to-server automation, with enterprise/project-scoped consent.
  * All endpoints accessed using **`x-user-id`** header to impersonate a user within the enterprise that the app is authorized for.
  * Required scopes: `data:read`, `data:write`, `bucket:read`, `bucket:create`, `bucket:write`.
  * **Change Detection:** ACC webhooks for Docs where available; otherwise incremental listing with `updatedSince`.

#### Tenant Consent Flows

* Single, headless onboarding wizard per connector that verifies app-only consent and performs test operations (list site/project, read/write to a sandbox folder).
* Exportable **permissions manifest** for security review.

#### Authorization Inside Our System

* Policy-based authorization (e.g., `CanManageBindings`, `CanViewAudit`).
* Per-binding ACLs for create/modify/run.

---

### Phase 1 Connector Details

#### SharePoint Connector

* Uses Graph **Sites.Selected** + application permissions.
* Stores per-binding: siteId, driveId/library, rootPath, delta token.
* Supports uploads with chunking, ETag compare-and-swap, move/rename, soft-delete handling.

#### ACC Docs Connector

* Uses **2‑legged OAuth** with `x-user-id` impersonation.
* Stores: accountId, projectId, folderUrn, region, delta/checkpoint markers.
* Supports list, upload, move, rename, and versioned file sync operations.
* Validated endpoints: `projects`, `folders`, `items`, `versions`, and upload workflows through Object Storage (OSS).

---

### Telemetry, Auditing, and Safety

* **Tracing**: One trace per job; spans for API calls, uploads, and conflict decisions.
* **Metrics**: Files/sec, bytes/sec, conflict rate, retry rate, API throttles, queue depth per tenant.
* **Audit Log**: Who/what/when for each change with before/after metadata.
* **Safety Levers**: Dry-run mode; scope previews; per-binding delete ceilings; quarantine bucket.

---

### Future Roadmap

* **Phase 2:** Add Trimble Connect and local file system agent.
* **Phase 3:** Introduce workflow automation (auto-publish models, change notifications).
* **Phase 4:** Expand to Google Drive, Procore, Dropbox.

### Business Model & Licensing

* SaaS subscription tiers based on number of connected platforms and sync frequency.
* Enterprise features (SSO, private cloud, advanced auditing) as premium add-ons.

### Success Criteria

* Successful headless sync between Autodesk and SharePoint in production environments.
* Onboarding time under 10 minutes.
* Positive feedback from pilot AEC firms on reliability and simplicity.

---

### Implementation Policies & Defaults

#### Compliance & Security

* **Data classification**: Only metadata (paths, hashes, timestamps) stored. No persistent file content. Transient encrypted Blob cache <24h TTL.
* **Keys**: Per-tenant AES-256 key derived from Key Vault master. Master rotated quarterly, tenant keys yearly.
* **Auditing**: Log all mutations + job lifecycle events. Immutable PostgreSQL append log, 90-day retention, exportable via portal.
* **Isolation**: TenantId partition key enforced via row-level-security. Per-tenant queues and Blob prefixes.
* **Webhook security**: HMAC-SHA256 signature validation with timestamp ±5 min. Secrets rotated every 90 days.

#### Platform Semantics

* **Paths**: Normalize to POSIX '/' internally; replace invalid characters for SharePoint; preserve display name.
* **Versioning**: ACC version mapped to SharePoint version with ACC URN in comment; checksum compare via SHA-256.
* **Deletes & moves**: Soft-delete queue for 7 days; manual restore via portal. Moves tracked via identical hash + new path.
* **Clock skew**: All timestamps in UTC; 5s tolerance.
* **Large files**: 10 MB chunks, 5 concurrent threads, resumable uploads with exponential backoff.

#### Autodesk Construction Cloud (ACC Docs)

* **Install**: Admin installs app, selects dedicated service user, grants project-level folder access.
* **Endpoints**: Only confirmed 2-legged-compatible (projects, folders, items, versions, uploads).
* **Regions**: Auto-detect project region (EU/US) and route requests accordingly.
* **Rate limits**: Default 5 concurrent API calls/tenant; backoff on 429 using Retry-After.

#### SharePoint / Microsoft Graph

* **Sites.Selected automation**: Onboarding wizard runs PowerShell/Graph API script to grant access.
* **Delta & webhooks**: Subscribe to Graph webhooks; fallback to /delta every 30 minutes.
* **Revocation handling**: 403 triggers binding invalidation + admin alert, pauses sync.

#### Architecture & Reliability

* **Idempotency**: JobId = hash(binding + window + direction); duplicates skipped via state table.
* **Checkpoints**: Table schema for binding cursors and version maps.
* **Retries**: Retryable = 429/5xx/timeouts. 5 attempts, exponential backoff (2s→2m).
* **SLOs**: p95 change-to-sync ≤10 min; error budget 99.5% daily success.
* **Safety**: Delete cap 100/job; dry-run and preview for new bindings.

#### Product & Operations

* **Onboarding wizard**: 5 steps (SSO, consent, folder select, test write, activate). <10 min average.
* **Pricing**: Bill on successful file ops + GB transferred, tiered by volume/platforms.
* **Support**: TraceId on all jobs, exportable diagnostic bundle.
* **Marketplace**: Register Autodesk APS multi-tenant SaaS app, attach DPA and ISO 19650 compliance note.

#### Developer Experience

* **SDKs**: Use Microsoft.Graph v5 + APS Forge v2 with adapter layer.
* **Local dev**: Docker Compose (Postgres, Azurite, Queue emulator). Stub connectors for tests.
* **Testing**: Unit + contract tests, chaos 429/network tests, nightly integration pipeline.

---
