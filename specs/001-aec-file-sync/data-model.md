# Phase 1 Design: Data Model & Schema

**Date**: 2025-10-25  
**Status**: Design Complete  
**Next Step**: Generate `contracts/` (API spec, webhook schemas)

---

## Overview

This document defines the complete data model for the AEC File Sync Platform. It includes:
1. **Entity Definitions** — Core domain objects and relationships
2. **Database Schema** — PostgreSQL DDL with constraints, indexes, and immutability enforcement
3. **State Machines** — Workflow transitions for Binding and SyncJob lifecycles
4. **Validation Rules** — Business logic enforced at schema + application layers
5. **Scalability Considerations** — Partitioning, archival, and retention policies

---

## Entity Catalog

### 1. Tenant

**Purpose**: Represents a customer organization; acts as primary data isolation boundary.

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated on create |
| `name` | VARCHAR(255) | NOT NULL, UNIQUE per org_id | Customer display name |
| `org_id` | VARCHAR(255) | NOT NULL, UNIQUE | External org identifier (e.g., Azure AD org ID) |
| `email` | VARCHAR(255) | NOT NULL | Billing/contact email |
| `status` | ENUM | NOT NULL, DEFAULT='active' | active \| paused \| suspended |
| `subscription_tier` | ENUM | DEFAULT='standard' | free \| standard \| premium (future use) |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Immutable |
| `updated_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Updated on config changes |

**Relationships**:
- 1-to-many with Connectors (1 tenant → N connectors)
- 1-to-many with Bindings (1 tenant → N bindings)
- 1-to-many with CredentialSecrets (1 tenant → N credential vaults)
- 1-to-many with AuditEntries (1 tenant → N audit records)

**Validation**:
- `org_id` must be globally unique (prevents tenant spoofing)
- `status='suspended'` prevents new binding creation (compliance hold)
- Soft-delete: never delete; set `status='deleted'` if needed for retention

---

### 2. Connector

**Purpose**: Represents an external platform integration instance (e.g., ACC Docs instance, SharePoint site).

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated |
| `tenant_id` | UUID | FK(tenants.id), NOT NULL | Multi-tenant isolation |
| `type` | ENUM | NOT NULL | ACC_DOCS \| SHAREPOINT (extensible for Phase 3) |
| `display_name` | VARCHAR(255) | NOT NULL | e.g., "ACC Docs - Project A" |
| `credential_secret_id` | UUID | FK(credential_secrets.id), NOT NULL | Encrypted credential reference |
| `scopes` | TEXT[] | NOT NULL | OAuth scopes granted (e.g., files.read, files.write) |
| `health_status` | ENUM | DEFAULT='unknown' | unknown \| healthy \| degraded \| unavailable |
| `last_health_check_at` | TIMESTAMPTZ | NULLABLE | Timestamp of last successful health probe |
| `last_error` | TEXT | NULLABLE | Most recent error message (transient failures) |
| `created_at` | TIMESTAMPTZ | NOT NULL | Immutable |
| `updated_at` | TIMESTAMPTZ | DEFAULT NOW() | Updated on health/error changes |

**Relationships**:
- Many-to-1 with Tenant
- Many-to-1 with CredentialSecret (1 connector → 1 secret at time T; history in secret rotation)
- 1-to-many with Bindings (1 connector → N bindings)

**Validation**:
- `credential_secret_id` must exist and belong to same tenant
- `scopes` must include minimum required for sync (files.read, files.write)
- Health checks run every 5 minutes (background task)

**State Transitions**:
```
[unknown] --[health check passes]--> [healthy]
         --[health check fails]--> [degraded]
[healthy] --[3 consecutive failures]--> [unavailable]
[unavailable] --[recovery succeeds]--> [healthy]
```

---

### 3. Binding

**Purpose**: Defines a pair of folders to sync, with direction, conflict policy, schedule, and state.

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated |
| `tenant_id` | UUID | FK(tenants.id), NOT NULL | Multi-tenant isolation |
| `source_connector_id` | UUID | FK(connectors.id), NOT NULL | Source platform |
| `target_connector_id` | UUID | FK(connectors.id), NOT NULL | Target platform (can equal source for bidirectional) |
| `source_folder_id` | VARCHAR(255) | NOT NULL | External folder ID on source (e.g., SharePoint item ID) |
| `target_folder_id` | VARCHAR(255) | NOT NULL | External folder ID on target |
| `direction` | ENUM | NOT NULL | ONE_WAY \| BIDIRECTIONAL |
| `conflict_policy` | ENUM | NOT NULL | SOURCE_WINS \| TARGET_WINS \| LAST_WRITER_WINS \| MANUAL_HOLD |
| `schedule_cadence` | ENUM | NOT NULL | REALTIME \| HOURLY \| DAILY \| WEEKLY \| MANUAL_ONLY |
| `delta_token_source` | TEXT | NULLABLE | Checkpoint for incremental sync (source) |
| `delta_token_target` | TEXT | NULLABLE | Checkpoint for incremental sync (target) |
| `oscillation_hold_until` | TIMESTAMPTZ | NULLABLE | When hold expires (set by ConflictResolver) |
| `status` | ENUM | NOT NULL, DEFAULT='active' | active \| paused \| disabled |
| `enabled_filters` | JSONB | DEFAULT='{}' | Future: exclude patterns, file types (Phase 2) |
| `created_at` | TIMESTAMPTZ | NOT NULL | Immutable |
| `updated_at` | TIMESTAMPTZ | DEFAULT NOW() | Updated on config changes |
| `last_sync_at` | TIMESTAMPTZ | NULLABLE | Last successful job completion |

**Relationships**:
- Many-to-1 with Tenant
- Many-to-1 with Connector (source)
- Many-to-1 with Connector (target)
- 1-to-many with SyncJob
- 1-to-many with ChangeItem

**Validation**:
- `source_connector_id` and `target_connector_id` must belong to same tenant
- For ONE_WAY: both connectors can differ
- For BIDIRECTIONAL: same connector instance allowed (self-sync scenario, rare)
- `direction` determines sync flow: ONE_WAY (source → target only) or BIDIRECTIONAL (bidirectional)
- `schedule_cadence='REALTIME'` requires webhooks (fallback to delta polling if webhook fails)
- `oscillation_hold_until > NOW()` implies binding in hold state (no new syncs)

**State Transitions**:
```
[active] --[pause requested]--> [paused]
         --[disable requested]--> [disabled]
[paused] --[resume requested]--> [active]
[disabled] --[cannot resume; must delete & recreate]--> [disabled]

-- Oscillation hold (transparent to operator)
[active] --[conflict in hold period]--> [active + oscillation_hold_until set]
         --[5 min elapsed]--> [active + oscillation_hold_until = NULL]
```

**Oscillation Hold Mechanism**:
- When conflict detected (policy ≠ MANUAL_HOLD), set `oscillation_hold_until = NOW() + 5 MINUTES`
- Background worker (`OscillationHoldExpiryWorker`) runs every 30s; finds bindings with expired hold; clears it
- During hold, new changes for same file are queued, not immediately applied

---

### 4. SyncJob

**Purpose**: Tracks execution instance of a binding sync (scheduled or manual).

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated |
| `binding_id` | UUID | FK(bindings.id), NOT NULL | Parent binding |
| `tenant_id` | UUID | FK(tenants.id), NOT NULL | Denormalized for audit queries |
| `triggered_by` | ENUM | NOT NULL | SCHEDULE \| MANUAL \| CREDENTIAL_ROTATION \| HOLD_EXPIRY |
| `started_at` | TIMESTAMPTZ | NOT NULL | Job start time |
| `ended_at` | TIMESTAMPTZ | NULLABLE | Job end time (NULL if running) |
| `status` | ENUM | NOT NULL, DEFAULT='pending' | pending \| running \| success \| failed \| partial |
| `processed_files_count` | INT | DEFAULT=0 | Number of file changes processed |
| `bytes_transferred` | BIGINT | DEFAULT=0 | Total bytes synced |
| `conflict_count` | INT | DEFAULT=0 | Number of conflicts encountered |
| `retry_count` | INT | DEFAULT=0 | Number of retries due to rate-limiting |
| `error_message` | TEXT | NULLABLE | Reason for failure (if status='failed') |
| `telemetry_ref` | VARCHAR(255) | NULLABLE | OpenTelemetry trace ID (for observability) |
| `webhook_changes_count` | INT | DEFAULT=0 | Number of changes detected via webhook |
| `polling_changes_count` | INT | DEFAULT=0 | Number of changes detected via delta polling |
| `created_at` | TIMESTAMPTZ | NOT NULL | Immutable |

**Relationships**:
- Many-to-1 with Binding
- Many-to-1 with Tenant (denormalized for query performance)
- 1-to-many with ChangeItem (changes processed in this job)
- 1-to-many with AuditEntry (audit records for this job)

**Validation**:
- `started_at` ≤ `ended_at` (if `ended_at` is set)
- `status='running'` implies `ended_at = NULL`
- `status='success'` implies `conflict_count = 0` AND no change status = 'failed'
- `status='partial'` implies some changes succeeded, some failed/quarantined

**State Transitions**:
```
[pending] --[job starts]--> [running]
[running] --[all changes processed successfully]--> [success]
         --[some changes failed]--> [partial]
         --[fatal error before processing]--> [failed]
[success/partial/failed] --[terminal]--> [success/partial/failed]

-- Retry (external)
[failed] --[operator clicks retry]--> [pending] (new SyncJob created)
```

**Observability Metrics**:
- Job duration: `ended_at - started_at`
- Success rate: `COUNT(status='success') / COUNT(*)`
- Average conflicts per job: `AVG(conflict_count)` (target: <0.1 per SC-004)

---

### 5. ChangeItem

**Purpose**: Represents a single file/folder delta detected during sync.

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated |
| `sync_job_id` | UUID | FK(sync_jobs.id), NOT NULL | Parent job |
| `binding_id` | UUID | FK(bindings.id), NOT NULL | Denormalized for queries |
| `tenant_id` | UUID | FK(tenants.id), NOT NULL | Denormalized for audit |
| `external_file_id` | VARCHAR(255) | NOT NULL | Platform-specific file ID (e.g., SharePoint item ID) |
| `file_path` | VARCHAR(1024) | NOT NULL | Folder path + filename (e.g., /Projects/A/drawing.dwg) |
| `file_name` | VARCHAR(255) | NOT NULL | Filename only (for UI display) |
| `action` | ENUM | NOT NULL | CREATE \| UPDATE \| DELETE \| MOVE \| RENAME |
| `source_platform` | ENUM | NOT NULL | ACC_DOCS \| SHAREPOINT |
| `target_platform` | ENUM | NOT NULL | ACC_DOCS \| SHAREPOINT |
| `checksum` | VARCHAR(64) | NULLABLE | SHA256 of file content (for deduplication, optional) |
| `version_id_source` | VARCHAR(255) | NULLABLE | Platform-specific version identifier |
| `version_id_target` | VARCHAR(255) | NULLABLE | Platform-specific version identifier (on target after apply) |
| `file_size_bytes` | BIGINT | NULLABLE | Size of file |
| `conflict_detected` | BOOLEAN | DEFAULT=FALSE | Whether this change conflicted with target state |
| `conflict_policy_applied` | ENUM | NULLABLE | SOURCE_WINS \| TARGET_WINS \| LAST_WRITER_WINS \| MANUAL_HOLD (NULL if no conflict) |
| `status` | ENUM | NOT NULL, DEFAULT='pending' | pending \| in_progress \| completed \| failed \| quarantined \| manual_hold |
| `quarantine_until` | TIMESTAMPTZ | NULLABLE | When item expires from soft-delete quarantine (7 days for deletes) |
| `error_message` | TEXT | NULLABLE | Reason for failure (if status='failed') |
| `updated_at` | TIMESTAMPTZ | DEFAULT NOW() | Updated on status change |
| `resolved_at` | TIMESTAMPTZ | NULLABLE | When operator resolved (if manual_hold) |

**Relationships**:
- Many-to-1 with SyncJob
- Many-to-1 with Binding (denormalized)
- Many-to-1 with Tenant (denormalized)

**Validation**:
- For DELETE action: `quarantine_until = NOW() + INTERVAL '7 days'` (FR-011)
- For conflict: `conflict_detected=TRUE` and `conflict_policy_applied` must be set
- `status='manual_hold'` means operator must resolve (UI prompt)
- `status='quarantined'` means item in 7-day soft-delete window (searchable in "Deleted Items" view)

**State Transitions**:
```
[pending] --[change application starts]--> [in_progress]
         --[conflict detected, policy=MANUAL_HOLD]--> [manual_hold]
         --[conflict detected, policy=AUTO]--> [in_progress + conflict_policy_applied set]
[in_progress] --[application succeeds]--> [completed]
             --[application fails, retry exhausted]--> [failed]
             --[DELETE action] --> [quarantined] (7-day hold)
[completed] --[DELETE action, 7 days elapsed]--> [permanently deleted (row retention only)]
[manual_hold] --[operator resolves]--> [in_progress] --> [completed]
[quarantined] --[operator restores]--> [in_progress] --> [completed]
           --[7 days elapsed]--> [permanently deleted]
```

**Soft-Delete Lifecycle**:
1. DELETE detected on source
2. ChangeItem created with `action=DELETE`, `status=quarantined`, `quarantine_until=NOW()+7days`
3. Operator can browse "Deleted Items" view and restore (FK to previous version)
4. After 7 days, item marked as "purged" (logical delete); audit trail retained forever

---

### 6. CredentialSecret

**Purpose**: Stores encrypted OAuth2 credentials and refresh tokens per connector, with rotation history.

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | UUID | PK | Auto-generated |
| `tenant_id` | UUID | FK(tenants.id), NOT NULL | Multi-tenant isolation |
| `connector_type` | ENUM | NOT NULL | ACC_DOCS \| SHAREPOINT |
| `encrypted_payload` | BYTEA | NOT NULL | AES-256 encrypted JSON { access_token, refresh_token, expires_at, scopes } |
| `encryption_key_version` | INT | NOT NULL | Which tenant key version was used (for key rotation) |
| `rotation_status` | ENUM | NOT NULL, DEFAULT='current' | current \| rotated \| scheduled |
| `scheduled_rotation_at` | TIMESTAMPTZ | NULLABLE | When automatic rotation is scheduled (future use) |
| `expires_at` | TIMESTAMPTZ | NULLABLE | When current token expires (informational) |
| `created_at` | TIMESTAMPTZ | NOT NULL | When secret first stored |
| `rotated_at` | TIMESTAMPTZ | NULLABLE | When credential was rotated to new value |
| `previous_secret_id` | UUID | NULLABLE | FK to prior CredentialSecret for audit trail |

**Relationships**:
- Many-to-1 with Tenant
- 1-to-many with Connector (1 secret → N connectors historically)
- 1-to-1 with current Connector (at time T)

**Validation**:
- `encrypted_payload` never stored in plaintext in logs or API responses
- `rotation_status='current'` → single per (tenant, connector_type) pair
- `rotation_status='rotated'` → retained for 7-day grace period (for in-flight requests)
- Decryption key derived from: `tenant_id + master_encryption_key + encryption_key_version`

**Rotation Workflow** (FR-008a):
1. Admin triggers `POST /api/tenants/{id}/connectors/{connectorId}/rotate-credentials`
2. System queues pending SyncJob(s) for bindings using this connector
3. System prompts admin to provide new OAuth credential (via browser re-auth or manual token paste)
4. New token encrypted and stored as new CredentialSecret with `rotation_status='current'`
5. SyncJob(s) resume; old credential kept in `rotation_status='rotated'` for 7 days

---

### 7. AuditEntry

**Purpose**: Immutable append-only audit trail for compliance and observability. Enforced via DB constraints + RLS.

**Fields**:
| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| `id` | BIGSERIAL | PK | Immutable sequential ID (append-only guarantee) |
| `tenant_id` | UUID | NOT NULL, FK(tenants.id) | Multi-tenant isolation |
| `actor` | VARCHAR(255) | NOT NULL | 'system' or user_id (e.g., 'user@example.com') |
| `action_type` | VARCHAR(64) | NOT NULL | BINDING_CREATED \| BINDING_PAUSED \| BINDING_DISABLED \| JOB_STARTED \| JOB_COMPLETED \| CONFLICT_DETECTED \| CONFLICT_RESOLVED \| FILE_CREATED \| FILE_UPDATED \| FILE_DELETED \| CREDENTIAL_ROTATED \| QUARANTINE_ITEM_RESTORED \| AUDIT_EXPORTED |
| `binding_id` | UUID | NULLABLE, FK(bindings.id) | Which binding (if applicable) |
| `sync_job_id` | UUID | NULLABLE, FK(sync_jobs.id) | Which job (if applicable) |
| `change_item_id` | UUID | NULLABLE, FK(change_items.id) | Which change (if applicable) |
| `context_json` | JSONB | NOT NULL | Structured metadata: { "conflict_policy": "SourceWins", "file_path": "...", "outcome": "success" \| "failure", ... } |
| `outcome` | VARCHAR(20) | NOT NULL | success \| failure \| escalated (escalated = manual hold) |
| `timestamp` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Server-generated timestamp (UTC) |
| `previous_entry_hash` | VARCHAR(64) | NOT NULL | SHA256 of prior entry in chain (or '0' for genesis) |
| `entry_hash` | VARCHAR(64) | NOT NULL, UNIQUE | SHA256 of this entry (used to detect tampering) |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Immutable |

**Relationships**:
- Many-to-1 with Tenant (scoped audit per tenant)
- Many-to-1 with Binding (optional)
- Many-to-1 with SyncJob (optional)
- Many-to-1 with ChangeItem (optional)

**Immutability Enforcement** (FR-010, FR-010a):
```sql
-- Prevent DELETEs
ALTER TABLE audit_entries ADD CONSTRAINT audit_immutable_no_delete 
    CHECK (false);

-- Prevent UPDATEs
CREATE OR REPLACE FUNCTION prevent_audit_update()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Cannot update audit entries (append-only table)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_prevent_update
    BEFORE UPDATE ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_update();

-- RLS: Only 'linksy_system' role can INSERT
ALTER TABLE audit_entries ENABLE ROW LEVEL SECURITY;
CREATE POLICY audit_insert_by_system ON audit_entries
    FOR INSERT WITH CHECK (current_user = 'linksy_system');
CREATE POLICY audit_read_own_tenant ON audit_entries
    FOR SELECT USING (tenant_id = current_setting('app.tenant_id')::UUID);
```

**Hash Chain Validation** (export verification):
- Each entry includes `previous_entry_hash` (SHA256 of prior entry's `entry_hash`)
- Export includes verification: for each entry i, compute `SHA256(i.id || i.actor || i.action_type || i.context_json || i.timestamp || i.previous_entry_hash)` and verify matches `i.entry_hash`
- If any mismatch found, report tamper detection

**Example Entry**:
```json
{
  "id": 12345,
  "tenant_id": "d3de24a2-...",
  "actor": "user@company.com",
  "action_type": "CONFLICT_RESOLVED",
  "binding_id": "b1234567-...",
  "sync_job_id": "s9876543-...",
  "change_item_id": "c5555555-...",
  "context_json": {
    "conflict_policy": "SourceWins",
    "file_path": "/Projects/Building-A/structural.dwg",
    "winner_platform": "ACC_DOCS",
    "target_platform": "SHAREPOINT",
    "outcome": "success"
  },
  "outcome": "success",
  "timestamp": "2025-10-25T14:32:15Z",
  "previous_entry_hash": "abc123def...",
  "entry_hash": "xyz789uvw...",
  "created_at": "2025-10-25T14:32:15Z"
}
```

---

## Database Schema (DDL)

### Core Tables

```sql
-- Tenants
CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    org_id VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'paused', 'suspended', 'deleted')),
    subscription_tier VARCHAR(20) DEFAULT 'standard',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_tenants_org_id ON tenants(org_id);
CREATE INDEX idx_tenants_status ON tenants(status);

-- Connectors
CREATE TABLE connectors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    type VARCHAR(20) NOT NULL CHECK (type IN ('ACC_DOCS', 'SHAREPOINT')),
    display_name VARCHAR(255) NOT NULL,
    credential_secret_id UUID NOT NULL,  -- FK added below after credential_secrets created
    scopes TEXT[] NOT NULL,
    health_status VARCHAR(20) DEFAULT 'unknown' CHECK (health_status IN ('unknown', 'healthy', 'degraded', 'unavailable')),
    last_health_check_at TIMESTAMPTZ,
    last_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_connectors_tenant_id ON connectors(tenant_id);
CREATE INDEX idx_connectors_health_status ON connectors(health_status);
CREATE UNIQUE INDEX idx_connectors_tenant_type_unique ON connectors(tenant_id, type) WHERE status != 'deleted';

-- Bindings
CREATE TABLE bindings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    source_connector_id UUID NOT NULL REFERENCES connectors(id),
    target_connector_id UUID NOT NULL REFERENCES connectors(id),
    source_folder_id VARCHAR(255) NOT NULL,
    target_folder_id VARCHAR(255) NOT NULL,
    direction VARCHAR(20) NOT NULL CHECK (direction IN ('ONE_WAY', 'BIDIRECTIONAL')),
    conflict_policy VARCHAR(20) NOT NULL CHECK (conflict_policy IN ('SOURCE_WINS', 'TARGET_WINS', 'LAST_WRITER_WINS', 'MANUAL_HOLD')),
    schedule_cadence VARCHAR(20) NOT NULL CHECK (schedule_cadence IN ('REALTIME', 'HOURLY', 'DAILY', 'WEEKLY', 'MANUAL_ONLY')),
    delta_token_source TEXT,
    delta_token_target TEXT,
    oscillation_hold_until TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'paused', 'disabled')),
    enabled_filters JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_sync_at TIMESTAMPTZ
);

CREATE INDEX idx_bindings_tenant_id ON bindings(tenant_id);
CREATE INDEX idx_bindings_status ON bindings(status);
CREATE INDEX idx_bindings_oscillation_hold ON bindings(oscillation_hold_until) WHERE oscillation_hold_until > NOW();

-- Sync Jobs
CREATE TABLE sync_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    binding_id UUID NOT NULL REFERENCES bindings(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    triggered_by VARCHAR(20) NOT NULL CHECK (triggered_by IN ('SCHEDULE', 'MANUAL', 'CREDENTIAL_ROTATION', 'HOLD_EXPIRY')),
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'running', 'success', 'failed', 'partial')),
    processed_files_count INT DEFAULT 0,
    bytes_transferred BIGINT DEFAULT 0,
    conflict_count INT DEFAULT 0,
    retry_count INT DEFAULT 0,
    error_message TEXT,
    telemetry_ref VARCHAR(255),
    webhook_changes_count INT DEFAULT 0,
    polling_changes_count INT DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sync_jobs_binding_id ON sync_jobs(binding_id);
CREATE INDEX idx_sync_jobs_tenant_id ON sync_jobs(tenant_id);
CREATE INDEX idx_sync_jobs_status ON sync_jobs(status);
CREATE INDEX idx_sync_jobs_started_at ON sync_jobs(started_at DESC);

-- Change Items
CREATE TABLE change_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sync_job_id UUID NOT NULL REFERENCES sync_jobs(id),
    binding_id UUID NOT NULL REFERENCES bindings(id),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    external_file_id VARCHAR(255) NOT NULL,
    file_path VARCHAR(1024) NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    action VARCHAR(20) NOT NULL CHECK (action IN ('CREATE', 'UPDATE', 'DELETE', 'MOVE', 'RENAME')),
    source_platform VARCHAR(20) NOT NULL CHECK (source_platform IN ('ACC_DOCS', 'SHAREPOINT')),
    target_platform VARCHAR(20) NOT NULL CHECK (target_platform IN ('ACC_DOCS', 'SHAREPOINT')),
    checksum VARCHAR(64),
    version_id_source VARCHAR(255),
    version_id_target VARCHAR(255),
    file_size_bytes BIGINT,
    conflict_detected BOOLEAN DEFAULT FALSE,
    conflict_policy_applied VARCHAR(20),
    status VARCHAR(20) NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'in_progress', 'completed', 'failed', 'quarantined', 'manual_hold')),
    quarantine_until TIMESTAMPTZ,
    error_message TEXT,
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);

CREATE INDEX idx_change_items_sync_job_id ON change_items(sync_job_id);
CREATE INDEX idx_change_items_binding_id ON change_items(binding_id);
CREATE INDEX idx_change_items_status ON change_items(status);
CREATE INDEX idx_change_items_quarantine ON change_items(quarantine_until) WHERE status = 'quarantined';
CREATE INDEX idx_change_items_checksum ON change_items(checksum);  -- For deduplication

-- Credential Secrets
CREATE TABLE credential_secrets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    connector_type VARCHAR(20) NOT NULL CHECK (connector_type IN ('ACC_DOCS', 'SHAREPOINT')),
    encrypted_payload BYTEA NOT NULL,
    encryption_key_version INT NOT NULL,
    rotation_status VARCHAR(20) NOT NULL DEFAULT 'current' CHECK (rotation_status IN ('current', 'rotated', 'scheduled')),
    scheduled_rotation_at TIMESTAMPTZ,
    expires_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    rotated_at TIMESTAMPTZ,
    previous_secret_id UUID REFERENCES credential_secrets(id)
);

CREATE INDEX idx_credential_secrets_tenant_id ON credential_secrets(tenant_id);
CREATE INDEX idx_credential_secrets_rotation_status ON credential_secrets(rotation_status);
CREATE INDEX idx_credential_secrets_rotated_at ON credential_secrets(rotated_at) WHERE rotation_status = 'rotated';

-- Foreign key for connectors.credential_secret_id (defined after credential_secrets)
ALTER TABLE connectors ADD CONSTRAINT fk_connector_credential_secret
    FOREIGN KEY (credential_secret_id) REFERENCES credential_secrets(id);

-- Audit Entries (immutable, append-only)
CREATE TABLE audit_entries (
    id BIGSERIAL PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    actor VARCHAR(255) NOT NULL,
    action_type VARCHAR(64) NOT NULL,
    binding_id UUID REFERENCES bindings(id),
    sync_job_id UUID REFERENCES sync_jobs(id),
    change_item_id UUID REFERENCES change_items(id),
    context_json JSONB NOT NULL,
    outcome VARCHAR(20) NOT NULL CHECK (outcome IN ('success', 'failure', 'escalated')),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW() AT TIME ZONE 'UTC',
    previous_entry_hash VARCHAR(64) NOT NULL,
    entry_hash VARCHAR(64) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Immutability: prevent DELETE
ALTER TABLE audit_entries ADD CONSTRAINT audit_no_delete CHECK (false);

-- Immutability: prevent UPDATE via trigger
CREATE OR REPLACE FUNCTION prevent_audit_update()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Cannot update audit entries (append-only table)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_prevent_update
    BEFORE UPDATE ON audit_entries
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_update();

-- RLS for audit (system writes, tenants read own)
ALTER TABLE audit_entries ENABLE ROW LEVEL SECURITY;

CREATE POLICY audit_insert_by_system ON audit_entries
    FOR INSERT WITH CHECK (current_user = 'linksy_system');

CREATE POLICY audit_read_own_tenant ON audit_entries
    FOR SELECT USING (tenant_id = current_setting('app.tenant_id')::UUID);

CREATE INDEX idx_audit_entries_tenant_id ON audit_entries(tenant_id);
CREATE INDEX idx_audit_entries_timestamp ON audit_entries(timestamp DESC);
CREATE INDEX idx_audit_entries_action_type ON audit_entries(action_type);
CREATE INDEX idx_audit_entries_binding_id ON audit_entries(binding_id) WHERE binding_id IS NOT NULL;
```

---

## Scalability & Maintenance

### Partitioning Strategy

For production scale (billions of rows):

1. **audit_entries**: Partition by RANGE (tenant_id, timestamp) — monthly partitions for recent data
2. **change_items**: Partition by RANGE (created_at) — weekly partitions, archive older than 90 days
3. **sync_jobs**: Partition by RANGE (created_at) — monthly partitions

### Archival Policy

- **Audit entries**: Retain 7 years (compliance for AEC projects)
- **Change items**: Retain 90 days in hot storage; archive to cold storage thereafter
- **Sync jobs**: Retain 1 year in hot storage; aggregate metrics for archival

### Performance Targets

- **Binding lookup**: <5ms (by tenant_id, status)
- **Audit query** (date range + binding): <500ms (indexed on timestamp, binding_id, tenant_id)
- **Change item insert** (bulk): <50ms per 100 items (batched)
- **Conflict detection**: <100ms per binding (indexed on quarantine_until, status)

---

## Next Phase

**Phase 1b Deliverables**:
- `contracts/api-spec.yaml` — OpenAPI 3.0 REST specification
- `contracts/webhook-events.schema.json` — Webhook payload schemas
- `contracts/schema.sql` — Full database DDL (production-ready)

**Estimated Effort**: ~3 days for API contract generation + webhook schema design.

---

**Data Model Complete** ✅ All entities, relationships, and constraints defined. Ready for API contract generation.
