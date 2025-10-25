# Feature Specification: AEC File Sync Platform

**Feature Branch**: `001-aec-file-sync`  
**Created**: 2025-10-25  
**Status**: Draft  
**Input**: Create an app based on docs/idea.md (AEC File Sync concept)

## Clarifications

### Session 2025-10-25

- Q: When a ManualHold conflict occurs, how is it resolved inside the product? → A: Operators resolve conflicts directly in the portal by choosing Source or Target.
- Q: How is conflict oscillation prevented when LastWriterWins is configured? → A: Enforce a binding-level 5-minute hold period during which successive changes to the same file from the opposite platform are queued instead of immediately re-synced.
- Q: What is the credential rotation flow for operators? → A: Admins manually trigger rotation via the management portal; pending sync jobs for that binding are queued and paused; jobs resume automatically once new credentials are validated.
- Q: How do operators search, filter, and restore soft-deleted (quarantined) files? → A: Quarantined files appear in a dedicated "Deleted Items" view in the activity log; operators can filter by date range and binding; restoration is a one-click action that re-syncs the file to both platforms and removes it from quarantine; permanent deletion is automatic after 7 days.
- Q: What is the API rate-limit retry strategy for external connectors? → A: Exponential backoff with jitter (1s, 2s, 4s, 8s, 16s, 30s, 40s) up to max 120 seconds total elapsed time to ensure at least 2 minutes pass (allowing 2+ ACC rate-limit windows); max 7 retries per change item; move exhausted items to ManualHold queue and alert operator; expose throttling events as observability metrics.
- Q: How is audit trail immutability enforced? → A: Append-only database table with server-side timestamp and database constraints preventing deletion; audit records writable by system only via RBAC; compliance verification includes hash chain validation during export to detect post-hoc tampering.
- Q: How will the backend API authenticate requests from the React frontend? → A: JWT Bearer tokens issued by the backend via ASP.NET Core Identity with refresh token support for extended sessions; stateless, supports user-level RBAC and audit trails.
- Q: What user roles and permissions model will govern access to portal features? → A: Three-tier RBAC (Tenant Admin, Operator, Auditor) scoped per tenant; Tenant Admins assign roles via management portal; claims-based authorization in API enforces role-based access to binding management, job controls, conflict resolution, and audit logs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Onboard a New Tenant Sync (Priority: P1)

A BIM manager signs in, connects Autodesk Construction Cloud and SharePoint, selects folders, sets conflict policy, and activates the binding.

**Why this priority**: Without a guided onboarding flow there is no value; this establishes the first usable sync.

**Independent Test**: Walk through the onboarding wizard with sandbox credentials and confirm folders sync once activation completes.

**Acceptance Scenarios**:

1. **Given** a tenant admin with required platform consents, **When** they complete the onboarding wizard, **Then** both connectors validate access and a binding is created in active state.
2. **Given** onboarding completes, **When** the system performs the automatic test write/read step, **Then** results display before activation and the admin can retry if validation fails.

---

### User Story 2 - Monitor and Manage Sync Jobs (Priority: P2)

An operator reviews upcoming jobs, triggers an on-demand sync, monitors progress, and addresses conflicts or failures.

**Why this priority**: Operations teams must trust the service to keep projects in sync and quickly recover from issues.

**Independent Test**: Trigger a job on a binding with test data and verify telemetry, alerts, and retry controls without onboarding a new tenant.

**Acceptance Scenarios**:

1. **Given** a scheduled job is pending, **When** the operator triggers a manual run, **Then** the system processes the job immediately and reports status in the activity log.
2. **Given** a conflict occurs during a sync, **When** the configured policy is ManualHold, **Then** the file moves to the quarantine queue and the operator can resolve it in the portal by selecting the authoritative source or target version.
3. **Given** a file is deleted and enters soft-delete quarantine, **When** the operator navigates to the "Deleted Items" view and filters by the binding's date range, **Then** the deleted file appears; the operator can one-click restore it, which re-syncs the file to both platforms and removes it from quarantine; after 7 days, the system automatically purges the quarantine entry.

---

### User Story 3 - Audit Compliance Activity (Priority: P3)

An auditor reviews historical changes, exports an audit log for a project, and verifies that sensitive content was not persisted.

**Why this priority**: Compliance validation protects the business contractually and differentiates the product in regulated AEC environments.

**Independent Test**: Run a sync that creates, updates, and deletes files, then export the audit log and confirm the record includes metadata only and is immutable.

**Acceptance Scenarios**:

1. **Given** an auditor with read permissions, **When** they filter the audit log by project and date, **Then** the system returns all relevant events with timestamps, actors, and action types.
2. **Given** the auditor exports a report, **When** it downloads, **Then** the file contains no persisted file content and includes verification of encryption status for transient caches.

---

### Edge Cases

- SharePoint or ACC permissions are revoked after onboarding; system must pause the binding, notify admins, and queue retries without data loss.
- Large file uploads exceed default chunk sizing or experience network failures; system must resume from last checkpoint without duplicating versions.
- Simultaneous updates occur on both platforms within seconds; conflict policies must prevent oscillation via a 5-minute binding-level hold period during which successive changes to the same file are queued, and must log the final authority decision.
- Credential rotation occurs while sync jobs are scheduled; system must queue pending jobs, pause the binding, and auto-resume once validation succeeds, preserving job history.
- External connector rate limiting (HTTP 429) occurs; system must retry up to 7 times over 120 seconds minimum to span 2+ ACC rate-limit windows, then escalate exhausted items to ManualHold with operator alert.
- Webhook delivery is delayed or skipped; scheduler must detect gaps with delta polling and avoid missing or replaying changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Tenant admins MUST authenticate through Entra ID SSO before accessing the management portal.
- **FR-001a**: Upon successful Entra ID authentication, the backend MUST issue a JWT Bearer token via ASP.NET Core Identity with user claims (sub, email, display name, roles); the token MUST expire after 12 hours and include a refresh token mechanism allowing clients to obtain new access tokens without re-authentication.
- **FR-001b**: The React frontend MUST include the JWT token in the `Authorization: Bearer <token>` header for all API requests; refresh tokens MUST be stored securely (in-memory or HttpOnly cookies) and automatically reissued when access tokens expire.
- **FR-001c**: The system MUST enforce role-based access control (RBAC) with three tenant-scoped roles: **Tenant Admin** (create/edit bindings, manage credentials, assign user roles, view all activity), **Operator** (trigger manual syncs, resolve ManualHold conflicts, view job status and activity logs), and **Auditor** (read-only access to audit logs and compliance reports). Role assignment is managed by Tenant Admins via the management portal. API endpoints MUST validate user role claims in JWT tokens and reject requests lacking required authorization.
- **FR-002**: The onboarding wizard MUST validate headless application access to both Autodesk Construction Cloud Docs and SharePoint, including a test read/write on a sandbox folder.
- **FR-003**: Admins MUST be able to configure folder bindings with direction (one-way or bidirectional), scheduling cadence, and conflict policy.
- **FR-004**: The system MUST support SourceWins, TargetWins, LastWriterWins (with clock skew guard), and ManualHold conflict policies per binding.
- **FR-005**: The platform MUST capture and store delta tokens or equivalent checkpoints for each binding to enable incremental syncs.
- **FR-006**: Sync jobs MUST process create, update, move, delete, and rename events while preserving file versions and metadata between platforms.
- **FR-007**: The service MUST provide on-demand sync triggers and allow pausing or disabling individual bindings without deleting configuration.
- **FR-008**: The system MUST encrypt and isolate external connector credentials per tenant, with rotation reminders surfaced to admins.
- **FR-008a**: Admins MUST be able to manually trigger credential rotation from the management portal; upon initiation, pending sync jobs for affected bindings MUST be queued and paused, and resume automatically once new credentials are validated.
- **FR-009**: Operators MUST have access to real-time job status, retry controls, and failure notifications via the activity log.
- **FR-010**: The platform MUST maintain an immutable audit trail of all sync actions, including actor, timestamp, source, target, and policy decisions, with export capability. Audit entries MUST be stored in an append-only database table with server-side timestamps and database constraints preventing deletion; audit records MUST be writable by the system only via role-based access control.
- **FR-010a**: Audit exports MUST include hash chain validation to detect post-hoc tampering; each exported audit report MUST contain verification metadata confirming integrity of the audit trail.
- **FR-011**: The system MUST enforce soft-delete handling by quarantining deletions for seven days before permanent removal and allowing manual restoration. Quarantined files MUST appear in a dedicated "Deleted Items" view within the activity log, filterable by date range and binding, with a one-click restore action that re-syncs the file to both platforms and removes it from quarantine.
- **FR-012**: For ManualHold conflicts, operators MUST be able to resolve items within the portal by selecting either the source or target version, after which the sync resumes for that item.
- **FR-013**: The service MUST validate webhook signatures and fall back to scheduled delta polling when webhooks are unavailable or unhealthy.
- **FR-014**: The platform MUST expose observability metrics (files processed, bytes transferred, conflicts, retries, throttling events) scoped per tenant.
- **FR-015**: When a conflict policy is configured for LastWriterWins or SourceWins/TargetWins, the system MUST enforce a 5-minute binding-level hold period during which successive changes to the same file from the opposite platform are queued instead of immediately re-applied, preventing oscillation.
- **FR-016**: The system MUST handle external API rate limiting (HTTP 429) via exponential backoff with jitter, retrying up to 7 times over a minimum 120-second window to span at least 2 ACC rate-limit cycles; change items exhausting all retries MUST move to ManualHold queue and trigger operator alerts; throttling events MUST be exposed as observability metrics.

### Key Entities *(include if feature involves data)*

- **Tenant**: Represents a customer organization; holds portal users, access policies, connector consents, and billing profile.
- **Connector**: Describes an external platform integration instance (e.g., ACC Docs, SharePoint) including credential metadata, scopes, and health status.
- **Binding**: Defines a pair of folders, sync direction, conflict policy, filters, and schedule cadence within a tenant.
- **Sync Job**: Tracks execution instance for a binding with start/end times, status, processed changes, retry count, and telemetry reference.
- **Change Item**: Represents an individual file or folder delta with source metadata, target action, checksum, and version identifiers.
- **Credential Secret**: Encapsulates encrypted app-only credential material linked to a connector and rotation history.
- **Audit Entry**: Immutable log record capturing user or system action, context, affected binding, and outcome classification. Stored in append-only table with server-side timestamp, database constraints preventing deletion, and RBAC enforcement (system writes only); includes hash chain metadata for tamper detection during export.
- **ApplicationUser**: Represents a platform user with identity claims (email, display name), role assignments per tenant, and authentication metadata managed by ASP.NET Core Identity. Linked to Entra ID via SSO and associated with one or more tenant-scoped roles (Tenant Admin, Operator, Auditor).

### Assumptions

- Initial release targets Autodesk Construction Cloud Docs and Microsoft SharePoint Online; additional connectors are out of scope for this phase.
- Customers will grant the necessary app-only permissions and provide a service account capable of performing sync operations.
- No persistent storage of file contents beyond a transient encrypted cache with a maximum 24-hour retention window.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 90% of tenant admins complete initial onboarding and activation in under 10 minutes without human support.
- **SC-002**: 99.5% of scheduled sync jobs across tenants complete successfully each day, with automatic retries resolving transient errors.
- **SC-003**: 95% of detected file changes propagate to the target system within 10 minutes of detection during pilot deployments.
- **SC-004**: Pilot customers report a ≥4/5 satisfaction score for reliability and simplicity in post-implementation surveys.
- **SC-005**: Audit exports contain zero instances of persisted file content and include complete metadata coverage for 100% of sync actions.
