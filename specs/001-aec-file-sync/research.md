# Phase 0 Research: Linksy

**Date**: 2025-10-25  
**Status**: Research Execution Complete  
**Next Phase**: Phase 1 Design (data-model.md, contracts/, quickstart.md)

---

## Research Task 1: Credential Vault & Encryption Strategy

**Unknown**: Which credential storage pattern best supports per-tenant isolation + secure rotation?

**Decision**: Per-tenant encryption with centralized credential vault (Azure KeyVault in production, encrypted SQL in dev).

**Rationale**:
- **Per-tenant encryption**: Each tenant's credentials encrypted with a tenant-specific key derived from tenant_id + master key, ensuring data isolation at database layer
- **Azure KeyVault** (production): Industry-standard HSM-backed secret management; supports automatic key rotation, audit logging, access control
- **Encrypted SQL columns** (development): PostgreSQL pgcrypto extension; encrypted_payload stored in `credential_secrets` table; decrypted on-demand via app layer
- **Rotation pattern**: Manual trigger (FR-008a) → system generates new credential via connector OAuth → updates `credential_secrets.encrypted_payload` → queues pending jobs → resumes post-validation
- **Key versioning**: `credential_secrets.key_version` tracks which tenant key version was used; old keys retained for rotation window (configurable, default 7 days)

**Alternatives Considered**:
- ❌ Vault in app memory: No, violates FR-008 (no persistent storage) and creates security gaps during restarts
- ❌ Raw credentials in env vars: No, violates security principle (credentials accessible to entire container)
- ❌ SSH keys (symmetric only): Insufficient; requires asymmetric encryption for multi-tenant isolation

**Implementation Pattern**:
```csharp
// CredentialVault service (Linksy.Api/Services/CredentialVault.cs)
public interface ICredentialVault
{
    Task<ConnectorCredentials> GetCredentialsAsync(Guid tenantId, ConnectorType type);
    Task RotateCredentialsAsync(Guid tenantId, Guid connectorId, ConnectorCredentials newCreds);
    Task<bool> ValidateCredentialsAsync(Guid tenantId, ConnectorType type);
}

// Decrypt on-demand: SELECT encrypted_payload FROM credential_secrets WHERE tenant_id = $1
// Decrypt: AES_DECRYPT(encrypted_payload, DERIVE_KEY(tenant_id, master_key))
```

**Testing**:
- Unit test: Verify encryption/decryption round-trip per tenant
- Integration test: Rotate credentials, verify pending jobs pause/resume
- Security test: Attempt to decrypt tenant A's secret with tenant B's key (must fail)

---

## Research Task 2: Connector Implementation Pattern

**Unknown**: How to handle OAuth2 refresh tokens, delta tokens, and pagination across both connectors?

**Decision**: Implement `IConnector` interface with OAuth2 refresh flow, delta token checkpointing, and standardized pagination.

**Rationale**:
- **OAuth2 Refresh Flow**: Both ACC Docs and SharePoint support OAuth2 with refresh tokens; store refresh_token in `credential_secrets`; silently refresh access_token if 401/403 detected
- **Delta Tokens**: Both platforms support delta queries (incremental changes); store `delta_token` per binding (`bindings.delta_token_source`, `delta_token_target`); resume from checkpoint on retry
- **Pagination**: Page size 100–500 items; resume via continuation tokens (SharePoint) or offset (ACC); avoid re-fetching unchanged items

**Alternatives Considered**:
- ❌ Full re-sync each job: No, violates FR-005 (delta tokens) and SC-003 (10-min propagation target)
- ❌ Separate token managers per connector: No, causes inconsistency; centralize in IConnector
- ❌ Webhook-only (no polling): No, violates FR-013 fallback requirement; webhooks can fail/delay

**Connector Interface**:
```csharp
public interface IConnector
{
    Task<IEnumerable<Change>> GetChangesAsync(string deltaToken, CancellationToken ct);
    Task<string> UploadFileAsync(FileMetadata metadata, Stream content, CancellationToken ct);
    Task<bool> DeleteFileAsync(string fileId, CancellationToken ct);
    Task<CredentialValidation> ValidateCredentialsAsync();
    Task<string> RefreshAccessTokenAsync(string refreshToken);
}

// Implementations
public class AccDocsConnector : IConnector { /* ... */ }
public class SharePointConnector : IConnector { /* ... */ }
```

**API Details**:
- **ACC Docs**: Microsoft Graph `/drive/items/delta` endpoint; supports `track=true` for deleted items; file chunking for >100MB via session uploads
- **SharePoint**: `/sites/{siteId}/drive/items/delta`; same pattern as OneDrive; supports version history queries for conflict resolution
- **Rate Limits**: 
  - ACC: ~1000 requests per minute per app (FR-016: 7 retries × 120s window spans ≥2 cycles)
  - SharePoint: ~1000–2000 requests per minute per app; throttling backoff: 60s+ recommended
- **Retry Strategy**: Implemented in `RateLimitManager` (see Task 5)

**Testing**:
- Unit test: Mock OAuth token refresh, verify retry on 401
- Integration test: Sandbox ACC Docs account + SharePoint site; verify delta token checkpointing
- Load test: Concurrent syncs from multiple bindings; verify rate-limit backoff prevents cascade

---

## Research Task 3: Database Schema for Audit Immutability

**Unknown**: How to enforce append-only audit table + hash chain validation in PostgreSQL?

**Decision**: Append-only table with constraints + RBAC + HMAC-SHA256 hash chain.

**Rationale**:
- **Append-Only**: PostgreSQL row-level security (RLS) + trigger to prevent UPDATE/DELETE; INSERT only by system role (app service account)
- **Hash Chain**: Each `AuditEntry` includes `previous_entry_hash` (SHA256 of prior entry) + `entry_hash` (SHA256 of current entry with payload); allows tamper detection on export
- **RBAC**: Audit table writable only by system account; tenants can read their own audit logs via API (no direct DB access)
- **Immutability Verification**: Export includes chain verification: `hash(entry_i) == SHA256(entry_i_payload + previous_entry_hash)`

**Alternatives Considered**:
- ❌ WORM Storage (Azure Blob Immutable): Overkill for pilot phase; adds infrastructure complexity
- ❌ Digital Signatures (RSA PKI): Higher complexity; asymmetric crypto slower; hash chain simpler for audit compliance
- ❌ Event Sourcing (full event log): Adds complexity; audit table sufficient for compliance requirements

**Schema**:
```sql
CREATE TABLE audit_entries (
    id BIGSERIAL PRIMARY KEY,  -- Immutable sequential ID (append-only guarantee)
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    actor TEXT NOT NULL,  -- 'system' or user_id
    action_type VARCHAR(50) NOT NULL,  -- e.g., 'BINDING_CREATED', 'CONFLICT_RESOLVED'
    binding_id UUID REFERENCES bindings(id),
    sync_job_id UUID REFERENCES sync_jobs(id),
    context_json JSONB NOT NULL,  -- e.g., { "conflict_policy": "SourceWins", "file_path": "..." }
    outcome VARCHAR(20) NOT NULL,  -- 'success', 'failure', 'escalated'
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW() AT TIME ZONE 'UTC',
    previous_entry_hash VARCHAR(64),  -- SHA256 of prior entry
    entry_hash VARCHAR(64) NOT NULL,  -- SHA256(id || actor || action_type || context_json || timestamp || previous_entry_hash)
    
    CONSTRAINT audit_immutable_delete CHECK (false),  -- Prevent DELETE
    CONSTRAINT audit_immutable_update CHECK (false)   -- Prevent UPDATE (via trigger is cleaner)
);

CREATE OR REPLACE FUNCTION prevent_audit_delete_update()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'Cannot modify audit entries (append-only table)';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER audit_prevent_modifications
BEFORE UPDATE OR DELETE ON audit_entries
FOR EACH ROW EXECUTE FUNCTION prevent_audit_delete_update();

-- RLS: Only system can insert; tenants can read own entries
ALTER TABLE audit_entries ENABLE ROW LEVEL SECURITY;
CREATE POLICY audit_insert_system ON audit_entries
    FOR INSERT WITH CHECK (current_user = 'linksy_system');
CREATE POLICY audit_read_own_tenant ON audit_entries
    FOR SELECT USING (tenant_id = current_setting('app.tenant_id')::UUID);
```

**Hash Chain Algorithm**:
```csharp
// In AuditLogger.cs
public async Task LogAsync(AuditEntry entry)
{
    var previousEntry = await _db.AuditEntries
        .Where(a => a.TenantId == entry.TenantId)
        .OrderByDescending(a => a.Id)
        .FirstOrDefaultAsync();
    
    entry.PreviousEntryHash = previousEntry?.EntryHash ?? "0";  // Genesis entry
    entry.EntryHash = ComputeHash(entry.Id, entry.Actor, entry.ActionType, 
        entry.ContextJson, entry.Timestamp, entry.PreviousEntryHash);
    
    await _db.AuditEntries.AddAsync(entry);
    await _db.SaveChangesAsync();
}

private string ComputeHash(params object[] fields)
{
    var json = JsonConvert.SerializeObject(fields);
    using (var sha256 = System.Security.Cryptography.SHA256.Create())
    {
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(json)));
    }
}
```

**Export Verification**:
```csharp
// In audit export endpoint
public bool VerifyChain(List<AuditEntry> entries)
{
    for (int i = 0; i < entries.Count; i++)
    {
        var current = entries[i];
        var expected = ComputeHash(current.Id, current.Actor, current.ActionType,
            current.ContextJson, current.Timestamp, current.PreviousEntryHash);
        
        if (current.EntryHash != expected)
            return false;  // Tampered!
    }
    return true;
}
```

**Testing**:
- Unit test: Verify hash chain integrity on export
- Integration test: Attempt manual UPDATE on audit table (must fail)
- Compliance test: Export audit, verify contains no file content, verify chain

---

## Research Task 4: Conflict Resolution & Oscillation Prevention

**Unknown**: How to implement 5-minute binding-level hold period without deadlocks?

**Decision**: Binding-level state machine with optimistic locking + background job for hold expiry.

**Rationale**:
- **Oscillation Problem**: If both systems update same file within seconds, and policy is LastWriterWins, rapid retries ping-pong the file
- **Solution**: Set `bindings.oscillation_hold_until = NOW() + INTERVAL '5 minutes'` after conflict detected; queue successive changes, don't re-apply until hold expires
- **Distributed Lock**: Use advisory lock (PostgreSQL) or Redis key for binding during hold period; prevents concurrent sync jobs from colliding
- **Hold Expiry**: Background worker (`OscillationHoldExpiryWorker`) polls bindings table every 30s; finds expired holds; resumes sync for queued changes

**Conflict Resolution State Machine**:
```
[Ready] --[conflict detected]--> [ManualHold] (if policy=ManualHold)
         --[conflict detected]--> [OscillationHold] (if policy=LastWriterWins, SourceWins, TargetWins)
[OscillationHold] --[5 min elapsed]--> [Ready]  (background worker)
[ManualHold] --[operator chooses source/target]--> [Ready]
```

**Alternatives Considered**:
- ❌ Immediate re-apply with version check: Complex, risk of infinite loops; no visibility to operator
- ❌ Per-file locking: High lock contention; more complex distributed lock management
- ❌ Accept oscillation + detect retroactively: Fails compliance (audit trail polluted with dup records)

**Implementation**:
```csharp
// ConflictResolver.cs
public async Task HandleConflictAsync(ChangeItem change, Binding binding)
{
    if (binding.ConflictPolicy == ConflictPolicy.ManualHold)
    {
        change.Status = ChangeItemStatus.ManualHold;
        await _auditLogger.LogAsync("CONFLICT_ESCALATED_MANUAL_HOLD", binding.Id, change.Id);
        await _notificationService.AlertOperatorAsync(binding.TenantId, change.Id);
    }
    else
    {
        // SourceWins, TargetWins, LastWriterWins: apply policy, enter oscillation hold
        var winner = ResolveConflict(change, binding.ConflictPolicy);
        change.Status = ChangeItemStatus.Completed;
        
        // Set hold period
        binding.OscillationHoldUntil = DateTime.UtcNow.AddMinutes(5);
        await _db.SaveChangesAsync();
        
        await _auditLogger.LogAsync("CONFLICT_RESOLVED_AUTO", binding.Id, 
            new { policy = binding.ConflictPolicy, winner });
    }
}

// OscillationHoldExpiryWorker.cs (background service)
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var expiredBindings = await _db.Bindings
            .Where(b => b.OscillationHoldUntil <= DateTime.UtcNow && b.OscillationHoldUntil != null)
            .ToListAsync(stoppingToken);
        
        foreach (var binding in expiredBindings)
        {
            binding.OscillationHoldUntil = null;  // Release hold
            await _db.SaveChangesAsync(stoppingToken);
            
            // Queue retry of pending changes for this binding
            await _syncOrchestrator.EnqueueBindingSyncAsync(binding.Id, "hold-expiry");
        }
        
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
    }
}
```

**Testing**:
- Unit test: Verify hold period applies after conflict detection
- Integration test: Simulate rapid updates on both systems; verify no oscillation
- Concurrency test: Multiple sync jobs for same binding during hold; verify single winner applied

---

## Research Task 5: Rate-Limit Retry & Backoff

**Unknown**: How to implement exponential backoff (1s, 2s, 4s, 8s, 16s, 30s, 40s) with jitter?

**Decision**: Use Polly library (resilience patterns) + Redis for distributed jitter seed.

**Rationale**:
- **Exponential Backoff**: Each retry waits 2x longer than prior (1, 2, 4, 8, 16, 30, 40s = ~120s total for 7 retries); spans 2+ ACC rate-limit cycles
- **Jitter**: Add randomness (±20%) to prevent thundering herd; Redis stores per-binding seed for reproducibility
- **Max Retries**: 7 total; 8th attempt moves item to ManualHold queue
- **Polly Integration**: `IAsyncPolicy<HttpResponseMessage>` with retry + timeout policies

**Alternatives Considered**:
- ❌ Linear backoff: Too slow to converge; doesn't respect rate-limit windows
- ❌ No jitter: Risk of thundering herd (all clients retry simultaneously)
- ❌ Custom backoff logic: Polly is battle-tested; avoid reinventing

**Backoff Schedule**:
- Retry 1: 1s + jitter (±0.2s) = 0.8–1.2s
- Retry 2: 2s + jitter (±0.4s) = 1.6–2.4s
- Retry 3: 4s + jitter (±0.8s) = 3.2–4.8s
- Retry 4: 8s + jitter (±1.6s) = 6.4–9.6s
- Retry 5: 16s + jitter (±3.2s) = 12.8–19.2s
- Retry 6: 30s + jitter (±6s) = 24–36s
- Retry 7: 40s + jitter (±8s) = 32–48s
- **Total**: ~120s minimum (meets FR-016 requirement: 2+ ACC cycles)

**Implementation**:
```csharp
// RateLimitManager.cs
private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

public RateLimitManager(IDistributedCache cache, ILogger<RateLimitManager> logger)
{
    _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<OperationCanceledException>()
        .OrResult<HttpResponseMessage>(r => (int)r.StatusCode == 429)
        .WaitAndRetryAsync(
            retryCount: 7,
            sleepDurationProvider: retryAttempt =>
            {
                var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                var jitter = GetJitterSeed(cache) * baseDelay.TotalMilliseconds * 0.2;
                return baseDelay.Add(TimeSpan.FromMilliseconds(jitter));
            },
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                logger.LogWarning($"Rate limit retry {retryCount}/{7}, waiting {timespan.TotalSeconds}s");
                // Update observability metric: throttle_event_count++
            }
        );
}

public async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action)
{
    try
    {
        return await _retryPolicy.ExecuteAsync(action);
    }
    catch (Exception ex)
    {
        logger.LogError($"Rate limit exhausted after 7 retries: {ex.Message}");
        // Escalate to ManualHold
        throw;
    }
}

private double GetJitterSeed(IDistributedCache cache)
{
    var seed = cache.GetString("jitter-seed") ?? Random.Shared.NextDouble().ToString();
    return double.Parse(seed);
}
```

**Observability Metrics** (FR-014):
- `sync_throttle_events_total`: Counter for 429 responses per binding
- `sync_retry_attempts_total`: Counter for all retries per binding
- `sync_backoff_delay_seconds`: Histogram of actual backoff durations

**Testing**:
- Unit test: Verify exponential backoff schedule matches specification
- Integration test: Mock 429 responses; verify retry counts and delays
- Load test: 100 concurrent bindings hitting rate limit; verify backoff + jitter prevents cascade
- Compliance test: Verify 7 retries span ≥120s and ≥2 rate-limit cycles

---

## Research Task 6: Webhook Validation & Delta Polling Fallback

**Unknown**: How to detect webhook failures and trigger delta polling without gaps?

**Decision**: Webhook signature validation (HMAC) + fallback trigger based on delivery tracking.

**Rationale**:
- **Webhook Signature**: Both ACC Docs and SharePoint send HMAC-SHA256 signature in header; app validates before processing
- **Delivery Tracking**: Log each webhook receipt; track last delivery timestamp per binding; if gap > threshold (e.g., 15 min), trigger delta polling
- **Delta Polling Worker**: Runs every 10 minutes; checks for bindings missing webhook deliveries; enqueues delta poll job
- **No Duplicates**: Use `ChangeItem.checksum` to deduplicate (if webhook + polling both detect same change)

**Alternatives Considered**:
- ❌ Webhook-only, no polling: Violates FR-013 fallback requirement; webhooks can fail silently
- ❌ Polling-only: Slow (10+ min lag); misses real-time sync benefit
- ❌ Manual operator intervention: Not user-friendly; automatic fallback required

**Webhook Handler**:
```csharp
// WebhookController.cs
[HttpPost("webhook/acc-docs")]
public async Task<IActionResult> ReceiveAccDocsWebhookAsync([FromHeader] string x_ms_signature, [FromBody] dynamic payload)
{
    // Validate signature
    var expectedSignature = ComputeHMAC(JsonConvert.SerializeObject(payload), _config["AccWebhookSecret"]);
    if (x_ms_signature != expectedSignature)
        return Unauthorized("Invalid signature");
    
    // Track receipt
    var binding = await _db.Bindings.FirstOrDefaultAsync(b => b.SourceConnectorId == /* ACC */);
    binding.LastWebhookReceivedAt = DateTime.UtcNow;
    await _db.SaveChangesAsync();
    
    // Queue change processing
    await _changeQueue.EnqueueAsync(payload);
    return Ok();
}

// WebhookRecoveryWorker.cs (background service)
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        var staleBindings = await _db.Bindings
            .Where(b => (DateTime.UtcNow - b.LastWebhookReceivedAt).TotalMinutes > 15)
            .ToListAsync(stoppingToken);
        
        foreach (var binding in staleBindings)
        {
            logger.LogWarning($"No webhook for binding {binding.Id} in 15 minutes; triggering delta poll");
            await _syncOrchestrator.EnqueueDeltaPollAsync(binding.Id);
        }
        
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
    }
}
```

**Deduplication**:
```csharp
// In SyncOrchestrator
public async Task ProcessChangesAsync(IEnumerable<Change> changes, Binding binding)
{
    foreach (var change in changes)
    {
        var checksum = ComputeChecksum(change.FileId, change.FileHash);
        
        // Check if already processed in last sync job
        var existing = await _db.ChangeItems
            .Where(ci => ci.BindingId == binding.Id && ci.Checksum == checksum)
            .OrderByDescending(ci => ci.SyncJob.StartedAt)
            .FirstOrDefaultAsync();
        
        if (existing?.SyncJob?.StartedAt > DateTime.UtcNow.AddMinutes(-5))
        {
            logger.LogInformation($"Change already processed (webhook + polling): {checksum}");
            continue;  // Skip duplicate
        }
        
        // Process change
        await HandleChangeAsync(change, binding);
    }
}
```

**Testing**:
- Unit test: Verify HMAC-SHA256 signature validation
- Integration test: Webhook delivery gaps trigger delta poll; verify no duplicate changes
- Resilience test: Simulate webhook delivery timeouts; verify delta polling catches up
- End-to-end test: Mix webhook + polling during sync; verify idempotent results

---

## Summary of Research Findings

| Task | Decision | Key Implementation Point | Risk Mitigation |
|------|----------|-------------------------|-----------------|
| 1. Credential Vault | Per-tenant AES encryption + Azure KeyVault | CredentialVault service interface | Key rotation pattern; audit all key access |
| 2. Connector Pattern | OAuth2 + delta tokens + IConnector interface | Standardized pagination, token refresh | Mock connectors for testing; sandbox OAuth creds |
| 3. Audit Immutability | Append-only table + HMAC chain | PostgreSQL RLS + triggers | Hash chain verification on export; backup retention |
| 4. Conflict Resolution | 5-min binding-level hold + state machine | OscillationHoldExpiryWorker background service | Distributed locking via advisory locks; observable state |
| 5. Rate-Limit Backoff | Exponential + jitter (1–40s, 7 retries) | Polly resilience policies + Redis jitter | Metrics on throttle events; manual escalation to ManualHold |
| 6. Webhook Validation | HMAC validation + 15-min fallback trigger | WebhookRecoveryWorker + change deduplication | Checksum-based dedup; logged gaps in audit trail |

---

## Next Phase

**Phase 1 Deliverables**:
- `data-model.md` — Entity schemas, validation rules, state machines (detailed SQL in `/contracts/schema.sql`)
- `contracts/api-spec.yaml` — OpenAPI 3.0 with request/response examples
- `contracts/webhook-events.schema.json` — Webhook payload validation
- `quickstart.md` — Dev environment setup + first binding walkthrough
- Agent context updated via `update-agent-context.sh copilot`

**Estimated Effort**: ~1 week for Phase 1 design + contract generation.

---

## Research Task 7: JWT Authentication + Refresh Token Lifecycle (Identity Framework)

**Unknown**: How to implement secure JWT issuance, storage, and refresh in React SPA with ASP.NET Core Identity?

**Decision**: Implement dual-storage strategy with HttpOnly cookies for production, in-memory fallback for dev.

**Rationale**:
- **OWASP Recommendation**: HttpOnly cookies are the secure default for SPA authentication; prevents XSS token theft via JavaScript
- **Automatic Transmission**: Browser automatically sends HttpOnly cookies on every request matching domain; reduces client-side token management
- **Stateless Backend**: JWT access tokens (12-hour lifetime) remain stateless; refresh tokens can be rotated server-side
- **Seamless Refresh**: axios interceptor detects 401 → calls `/auth/refresh` → gets new token in cookie → retries original request

**Implementation Pattern**:
```csharp
// Backend (Linksy.Api/Endpoints/Auth/TokenService.cs)
public class TokenService : ITokenService {
    public string GenerateJwtToken(ApplicationUser user, Guid tenantId, List<string> roles) {
        var claims = new List<Claim> {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim("displayName", user.DisplayName),
            new Claim("tenant", tenantId.ToString()),
            new Claim(ClaimTypes.Role, string.Join(",", roles)) // "Admin,Operator"
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// Login endpoint sets HttpOnly cookie
app.MapPost("/auth/login", LoginEndpoint)
    .WithName("Login")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status401Unauthorized);

async Task<IResult> LoginEndpoint(LoginRequest req, UserManager<ApplicationUser> userMgr, ITokenService tokenSvc, HttpContext ctx) {
    var user = await userMgr.FindByEmailAsync(req.Email);
    if (user == null || !await userMgr.CheckPasswordAsync(user, req.Password))
        return Results.Unauthorized();
    
    var tenant = req.TenantId; // or derive from user's primary tenant
    var roles = await GetUserRolesAsync(user.Id, tenant);
    
    var token = tokenSvc.GenerateJwtToken(user, tenant, roles);
    
    // Set HttpOnly cookie (secure, samesite=strict)
    ctx.Response.Cookies.Append(
        "access_token",
        token,
        new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(12)
        }
    );
    
    return Results.Ok(new { message = "Login successful" });
}

// Refresh endpoint issues new token
app.MapPost("/auth/refresh", RefreshEndpoint)
    .WithName("RefreshToken")
    .RequireAuthorization();

async Task<IResult> RefreshEndpoint(HttpContext ctx, ITokenService tokenSvc) {
    var user = ctx.User;
    var userId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    var tenantId = Guid.Parse(user.FindFirst("tenant")?.Value ?? "");
    
    var appUser = await userMgr.FindByIdAsync(userId!);
    var roles = await GetUserRolesAsync(userId, tenantId);
    
    var newToken = tokenSvc.GenerateJwtToken(appUser!, tenantId, roles);
    
    ctx.Response.Cookies.Append("access_token", newToken, new CookieOptions { HttpOnly = true, ... });
    return Results.Ok(new { message = "Token refreshed" });
}
```

```typescript
// Frontend (services/api.ts)
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  withCredentials: true, // Include cookies in requests
});

// Interceptor: Handle 401 refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      try {
        await api.post('/auth/refresh'); // Gets new token in HttpOnly cookie
        return api.request(error.config); // Retry original request
      } catch {
        window.location.href = '/login'; // Refresh failed; redirect to login
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

**Testing**:
- Unit: Mock JWT generation; verify claims include tenant + roles
- Integration: Login flow → extract JWT → verify claims → call protected endpoint → verify 401 on expired token → refresh → retry succeeds

---

## Research Task 8: Multi-Tenant RBAC with ASP.NET Core Identity

**Unknown**: How to implement tenant-scoped role assignments that scale across multiple users/tenants?

**Decision**: Extend ApplicationUser with TenantUserRole junction table; store roles in JWT claims scoped by tenant.

**Rationale**:
- **Scalability**: Many-to-many relationship allows users to hold different roles across different tenants without duplicating identity records
- **Claims-Based Authorization**: JWT includes roles claim; API validates role without database lookup (stateless)
- **Tenant Isolation**: Each TenantUserRole is filtered by TenantId; queries include WHERE TenantId == context.Tenant
- **Three Roles**: Admin (full access), Operator (job management), Auditor (read-only audit access)

**Schema Pattern**:
```csharp
public class ApplicationUser : IdentityUser {
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public ICollection<TenantUserRole> TenantRoles { get; set; } = new List<TenantUserRole>();
}

public class TenantUserRole {
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TenantRole Role { get; set; } // enum: Admin=0, Operator=1, Auditor=2
    
    public Tenant Tenant { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

public enum TenantRole {
    Admin = 0,
    Operator = 1,
    Auditor = 2
}
```

**API Authorization**:
```csharp
// Require specific roles for endpoints
app.MapPost("/api/bindings", CreateBindingEndpoint)
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/api/sync/{bindingId}/trigger", TriggerSyncEndpoint)
    .RequireAuthorization(policy => policy.RequireRole("Admin", "Operator"));

app.MapGet("/api/audit/export", ExportAuditEndpoint)
    .RequireAuthorization(policy => policy.RequireRole("Admin", "Auditor"));
```

**Testing**:
- Unit: Verify roles serialized correctly in JWT claims
- Integration: Create users with different roles; verify Admin can create bindings, Operator can trigger sync, Auditor can export audit

---

**Research Complete** ✅ All 8 tasks resolved; no blockers for Phase 1 design.
