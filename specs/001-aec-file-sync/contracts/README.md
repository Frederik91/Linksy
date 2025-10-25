# API Contracts Reference

This directory contains the formal API specifications and event schemas for the AEC File Sync Platform.

## Files

### `api-spec.yaml`

**OpenAPI 3.0 specification** for all REST endpoints.

**Coverage**:
- **Health**: Service liveness check (no auth required)
- **Tenants**: Multi-tenant management
- **Connectors**: External platform registration and health
- **Bindings**: Folder pair sync configuration
- **Jobs**: Sync job monitoring and retry controls
- **Conflicts**: Conflict detection and manual resolution (FR-012)
- **Quarantine**: Soft-deleted file recovery (FR-011)
- **Credentials**: Credential rotation workflow (FR-008a)
- **Audit**: Query and export immutable audit trail (FR-010, FR-010a)
- **Metrics**: OpenTelemetry observability (FR-014)

**Authentication**: All endpoints (except `/health` and `/metrics`) require Bearer token (OAuth2 access token from Entra ID SSO, FR-001).

**Usage**:
- Import into API clients (Swagger UI, Postman, etc.)
- Generate SDK stubs via OpenAPI tools
- Validate request/response payloads

---

### `webhook-events.schema.json`

**JSON Schema** for webhook payload validation.

**Event Types**:
- `file.created` — New file detected on source platform
- `file.updated` — File modified on source platform
- `file.deleted` — File removed on source platform
- `file.moved` — File relocated (with path change)

**Validation**:
- All webhooks signed with HMAC-SHA256 in `X-MS-Signature` header (FR-013)
- App validates signature before processing
- Failed validation: webhook rejected (400 Bad Request)

---

## Example Usage

### Scenario: Query Audit Log and Export

```bash
# Get audit entries for a binding (last 24 hours)
curl -X GET "http://localhost:5000/api/tenants/d3de24a2-1234-5678-abcd/audit?bindingId=b1234567-abcd-ef01&fromDate=2025-10-24T00:00:00Z&toDate=2025-10-25T00:00:00Z" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Response: Array of AuditEntry objects
[
  {
    "id": 12345,
    "tenantId": "d3de24a2-...",
    "actor": "user@company.com",
    "actionType": "BINDING_CREATED",
    "bindingId": "b1234567-...",
    "outcome": "success",
    "timestamp": "2025-10-25T14:32:15Z",
    "entryHash": "abc123def...",
    "context": {
      "conflictPolicy": "SourceWins",
      "direction": "ONE_WAY"
    }
  },
  ...
]

# Export audit report
curl -X POST "http://localhost:5000/api/tenants/d3de24a2-1234-5678-abcd/audit/export" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "format": "csv",
    "fromDate": "2025-10-24",
    "toDate": "2025-10-25"
  }' \
  -o audit-report.csv

# File downloaded: audit-report.csv with hash chain footer
```

### Scenario: Resolve Conflict

```bash
# Get pending conflicts for binding
curl -X GET "http://localhost:5000/api/bindings/b1234567-abcd-ef01/conflicts" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Response: Array of conflicting ChangeItem objects
[
  {
    "id": "c5555555-...",
    "filePath": "/Projects/design-v1.dwg",
    "status": "manual_hold",
    "conflictDetected": true,
    "conflictPolicyApplied": null
  }
]

# Operator resolves: keep ACC Docs version
curl -X POST "http://localhost:5000/api/conflicts/c5555555-../resolve" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "chosenSource": "ACC_DOCS"
  }'

# Response: Updated ChangeItem with status=in_progress
{
  "id": "c5555555-...",
  "status": "in_progress",
  "conflictPolicyApplied": "MANUAL_HOLD"
}
```

### Scenario: Trigger On-Demand Sync

```bash
# Trigger manual sync for binding
curl -X POST "http://localhost:5000/api/bindings/b1234567-abcd-ef01/manual-sync" \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Response: Sync job enqueued
{
  "jobId": "s9876543-...",
  "status": "pending"
}

# Poll job status
curl -X GET "http://localhost:5000/api/jobs/s9876543-..." \
  -H "Authorization: Bearer $ACCESS_TOKEN"

# Response: Job details
{
  "id": "s9876543-...",
  "bindingId": "b1234567-...",
  "status": "running",
  "processedFilesCount": 42,
  "bytesTransferred": 1048576,
  "conflictCount": 1,
  "startedAt": "2025-10-25T14:32:15Z",
  "endedAt": null
}
```

---

## Integration Checklist

- [ ] API spec imported into Swagger UI / API documentation tool
- [ ] Webhook schema validated in event-driven integration tests
- [ ] All endpoints tested with bearer token (OAuth2)
- [ ] Signature validation implemented for webhook ingestion (FR-013)
- [ ] Audit export hash chain verified on client side (FR-010a)
- [ ] Rate-limit handling verified (429 retries, FR-016)
- [ ] Conflict resolution workflow tested (FR-012)
- [ ] Credential rotation endpoint tested (FR-008a)

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2025-10-25 | Initial API spec; covers all MVP endpoints and schemas |

