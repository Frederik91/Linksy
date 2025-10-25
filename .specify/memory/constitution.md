<!--
Sync Impact Report
- Version change: 1.1.0 → 1.2.0
- Modified principles: None
- Modified sections: Project naming, Delivery Workflow & Quality Gates (frontend stack)
- Added sections: None
- Removed sections: None
- Templates requiring updates: ✅ .specify/templates/plan-template.md, ✅ .specify/templates/spec-template.md, ✅ .specify/templates/tasks-template.md
- Follow-up TODOs: None
-->

# Linksy Constitution

## Core Principles

### I. Frictionless Adoption
- The onboarding wizard MUST let a new tenant connect Autodesk Construction Cloud (ACC) and SharePoint in under 10 minutes with reversible defaults.
- Management UI, APIs, and documentation MUST assume BIM managers and project coordinators as the primary personas with concise copy and safe guardrails.
- Every configuration change MUST provide preview and dry-run options before activation to prevent accidental disruption.

*Rationale: Our target users adopt the platform only if setup is intuitive, fast, and low risk, eliminating the heavy lift that current integration tools impose.*

### II. Deterministic Sync Integrity
- All bindings MUST run headless via the orchestrator using scheduled or on-demand jobs; no manual file handling or per-user tokens are permitted.
- Workers MUST maintain per-binding delta tokens, checkpoint tables, and conflict policies (SourceWins default, TargetWins, LastWriterWins, ManualHold) so every change is replayable and idempotent.
- Sync jobs MUST persist version metadata, emit structured change logs, and honour delete ceilings to protect against data loss.

*Rationale: The core value is trustworthy bidirectional synchronization; deterministic, auditable processing is the only acceptable baseline.*

### III. Security & Compliance by Design
- Only application-level OAuth (client credentials) is allowed for external connectors; secrets MUST be encrypted per tenant using Azure Key Vault–backed keys.
- Persistent storage MUST exclude file bodies; only metadata (paths, hashes, timestamps, versions) may be retained, and transient blob caches MUST expire within 24 hours.
- Audit trails MUST capture who/what/when for every mutation with 90-day retention and exportability to meet ISO 19650, GDPR, and customer due diligence.

*Rationale: AEC firms require regulated handling of project data; security baked into architecture protects trust and unlocks enterprise adoption.*

### IV. Extensible Connector Platform
- New connectors MUST be authored against the shared Connector SDK covering auth, throttling, paging, mapping, and logging, with contract tests passing before rollout.
- Each connector MUST expose consistent capabilities: list, delta detection, upload/download, rename/move, versioning, and conflict policy enforcement.
- Compatibility matrices and feature flags MUST guard partially implemented connectors so Phase 1 platforms (ACC Docs, SharePoint/OneDrive) remain stable while future platforms onboard iteratively.

*Rationale: Sustainable growth depends on a reusable integration spine; the SDK keeps connectors uniform, testable, and maintainable as coverage expands.*

### V. Observable & Cost-Conscious Operations
- Every job MUST emit OpenTelemetry traces, metrics (files/sec, bytes/sec, conflict rate, retry rate), and structured logs partitioned by tenant for live diagnostics.
- Safety levers—dry-run mode, delete caps (≤100 per job), quarantine queues, and scoped previews—MUST be enforced before production rollout.
- Operations MUST track resource consumption per tenant to inform tiered pricing and ensure affordability goals remain measurable.

*Rationale: Comprehensive telemetry and guardrails keep synchronisation reliable, empower support teams, and protect affordability by tying usage to spend.*

## Phase 1 Scope Guardrails

Phase 1 delivery MUST include:

- Supported services: Autodesk Construction Cloud Docs module and Microsoft SharePoint Online (Graph Sites.Selected scope).
- Core capabilities: application-level OAuth setup, scheduled or on-demand bidirectional sync, version-aware conflict resolution, metadata and activity logging, and sync reporting.
- Target personas: BIM managers, digital engineers, and project coordinators operating mixed Autodesk/Microsoft ecosystems.

Any deviation requires governance approval plus a mitigation plan for affected tenants.

## Delivery Workflow & Quality Gates

- **Architecture stack**: Frontend in React + TypeScript using shadcn/ui for component primitives (Vite or Next.js), backend in .NET 8 Web API/Minimal API, PostgreSQL metadata store, Azure Blob transient cache, Azure App Service/Container Apps deployment.
- **Orchestration**: Sync Orchestrator and job queue (Azure Storage Queues/Service Bus) MUST guarantee at-least-once delivery with exponential backoff (2s → 2m, 5 attempts).
- **Testing discipline**: Unit, contract, integration, and chaos scenarios (429/timeouts) MUST be automated; connector contract tests MUST run before enabling a tenant.
- **Developer experience**: Local development MUST run via .NET Aspire orchestrating PostgreSQL, Azurite, queue emulators, the backend API, and the React frontend with shadcn/ui; pull requests MUST include telemetry validation and migration scripts when schema changes.
- **Operational readiness**: Observability dashboards per tenant, alerting on error budget breaches (p95 change-to-sync ≤10 minutes, ≥99.5% daily success), and documented support playbooks MUST exist before GA.

## Governance

- **Authority**: This constitution supersedes other delivery playbooks for AEC File Sync; product, engineering, and operations teams are accountable for compliance.
- **Amendments**: Proposed changes require written RFC, review by platform leads, and sign-off from security/compliance stakeholders. Approved changes MUST update this document, affected templates, and traceable tickets.
- **Versioning**: Semantic versioning applies—MAJOR for governance-breaking changes, MINOR for new principles or sections, PATCH for clarifications. Each amendment updates the Sync Impact Report.
- **Compliance review**: Quarterly audits verify principle adherence, telemetry health, and affordability KPIs; violations trigger remediation plans tracked to closure.

**Version**: 1.2.0 | **Ratified**: 2025-10-25 | **Last Amended**: 2025-10-25
