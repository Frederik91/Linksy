# Quickstart Guide: Linksy

**Date**: 2025-10-25  
**Target Audience**: Developers, QA engineers, product managers validating MVP functionality

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [First Binding Walkthrough](#first-binding-walkthrough)
4. [Conflict Simulation](#conflict-simulation)
5. [Audit Export Verification](#audit-export-verification)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Software Requirements

- **Node.js 22 LTS** ([download](https://nodejs.org/)) — Required for React frontend build
- **.NET 9 SDK** ([download](https://dotnet.microsoft.com/download)) — Required for ASP.NET Core backend
- **PostgreSQL 14+** ([download](https://www.postgresql.org/download/)) — Or use Docker: `docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=localdev postgres:latest`
- **Git** — For repository access
- **VS Code** (optional) — Recommended for editing, debugging

### External Credentials (Sandbox)

For development, you'll need test credentials for two platforms:

1. **Autodesk Construction Cloud (ACC) Docs**
   - Create a sandbox ACC account: https://developer.autodesk.com/
   - Register an OAuth2 app for local testing (redirect URI: `http://localhost:5173/callback`)
   - Save Client ID and Client Secret

2. **Microsoft SharePoint Online**
   - Use existing Microsoft 365 tenant or create sandbox
   - Register an OAuth2 app in Azure AD (redirect URI: `http://localhost:5173/callback`)
   - Grant delegated permissions: `Files.ReadWrite.All`, `Sites.ReadWrite.All`
   - Save Client ID and Client Secret

**Note**: For pilot/staging, use non-production environments. Never commit credentials to git; use `.env.local` (see setup).

---

## Local Development Setup

### Step 1: Clone Repository & Install Dependencies

```bash
git clone https://github.com/Frederik91/Linksy.git
cd /Users/frederik/repos/Linksy

# Run setup script (installs npm packages, validates prerequisites)
./scripts/setup.sh

# Verify setup
dotnet --version   # Should output 9.x.x
node --version     # Should output v22.x.x
```

### Step 2: Configure Local Environment

Create `.env.local` file in `src/` directory (NOT committed to git):

```bash
# src/.env.local

# Backend API configuration
ASPNETCORE_ENVIRONMENT=Development
CORS_ALLOWED_ORIGINS=http://localhost:5173

# Database (local PostgreSQL)
DB_CONNECTION_STRING=Server=localhost;Port=5432;Database=linksy_dev;User Id=postgres;Password=localdev;

# OAuth2 Sandbox Credentials (from prerequisites)
ACC_DOCS_CLIENT_ID=<YOUR_ACC_CLIENT_ID>
ACC_DOCS_CLIENT_SECRET=<YOUR_ACC_CLIENT_SECRET>
SHAREPOINT_CLIENT_ID=<YOUR_SP_CLIENT_ID>
SHAREPOINT_CLIENT_SECRET=<YOUR_SP_CLIENT_SECRET>

# Encryption master key (generate via: openssl rand -base64 32)
ENCRYPTION_MASTER_KEY=<GENERATE_BASE64_32_BYTES>

# Aspire dashboard
ASPIRE_DASHBOARD_ENABLED=true
ASPIRE_DASHBOARD_PORT=15217
```

### Step 3: Initialize Database

```bash
# Create database
createdb -U postgres linksy_dev

# Run migrations (using Entity Framework Core)
cd src/Linksy.Api
dotnet ef database update --context LinksynDbContext
cd ../..
```

### Step 4: Start Development Environment

From the repository root:

```bash
cd src
dotnet run --project Linksy.AppHost
```

**Expected Output**:
```
Building Aspire AppHost...
Starting services:
  - Linksy.Api (listening on http://localhost:5000)
  - React Frontend (dev server at http://localhost:5173)

Aspire Dashboard: http://localhost:15217

Waiting for services to start...
Services started successfully.
```

### Step 5: Verify Services

Open your browser and check:

- **Frontend**: http://localhost:5173 — React app should load
- **API Health**: http://localhost:5000/health — Returns `{ "status": "Healthy" }`
- **Aspire Dashboard**: http://localhost:15217 — View logs, metrics, service traces

---

## First Binding Walkthrough

### Scenario: Sync Files from ACC Docs to SharePoint

#### 1. Sign In

1. Navigate to http://localhost:5173
2. Click "Sign In"
3. **Development mode**: Uses mock Entra ID; enter any email (e.g., `test@example.com`)
4. Confirm redirect to dashboard

#### 2. Register Connectors

**Register ACC Docs Connector**:

1. Go to **Settings** → **Connectors**
2. Click **+ Add Connector**
3. Select **Autodesk Construction Cloud (ACC) Docs**
4. Click **Authorize** → Browser redirects to ACC sandbox login
5. Grant permissions and authorize
6. Confirm: "ACC Docs connected as `test@example.com`"

**Register SharePoint Connector**:

1. Click **+ Add Connector** again
2. Select **Microsoft SharePoint Online**
3. Click **Authorize** → Browser redirects to Microsoft login
4. Grant permissions to your sandbox SharePoint site
5. Confirm: "SharePoint connected"

#### 3. Create Binding

1. Go to **Bindings** → **+ New Binding**
2. **Configuration**:
   - **Name**: "Test Sync: ACC → SharePoint"
   - **Source**: ACC Docs (select connector above)
   - **Source Folder**: Browse ACC Docs and select a test folder (or create `Test` folder)
   - **Target**: SharePoint (select connector above)
   - **Target Folder**: Browse SharePoint and select/create `Linksy-Sync` folder
   - **Direction**: One-Way (ACC → SharePoint)
   - **Conflict Policy**: SourceWins
   - **Schedule**: Manual Only (for testing)
3. Click **Create Binding**
4. **Validation Step**: System performs test read/write (should succeed within 10s)
5. Click **Activate**

#### 4. First Sync

1. Navigate to **Dashboard** and locate your new binding
2. Click **Manual Sync Now**
3. **Watch the progress**:
   - Status changes from "Pending" → "Running" → "Success"
   - File count shows number of files processed
   - Duration shown (expect <30s for <100 files)

#### 5. Verify Files Synced

1. Open your SharePoint site in a browser
2. Navigate to `Linksy-Sync` folder
3. Confirm files from ACC Docs test folder are present
4. Check timestamps: modification dates should be recent (within last minute)

#### 6. Check Audit Trail

1. Go to **Audit Log**
2. Filter by your binding name
3. Confirm entries:
   - `BINDING_CREATED` — When you created binding
   - `JOB_STARTED` — When you clicked "Manual Sync Now"
   - `JOB_COMPLETED` — Job finished, files synced
   - Per-file entries: `FILE_CREATED`, `FILE_UPDATED` (depending on actions)

---

## Conflict Simulation

### Scenario: Test ManualHold Conflict Resolution

#### 1. Create Test File in ACC Docs

1. Use ACC Docs sandbox web UI
2. Upload a test file: `design-v1.dwg` to your test folder
3. Run a sync (see First Binding Walkthrough step 4)
4. Confirm file now in SharePoint

#### 2. Edit File in Both Platforms Simultaneously

1. **In SharePoint**: 
   - Open `design-v1.dwg`
   - Add a comment "Updated by SP team"
   - Save (modify timestamp)

2. **In ACC Docs** (simultaneously):
   - Open `design-v1.dwg`
   - Add a different comment "Updated by ACC team"
   - Save (modify timestamp)

3. **Trigger a sync** in Linksy dashboard

#### 3. Resolve Conflict

1. Dashboard shows conflict icon next to binding
2. Click **View Conflicts** (or **Dashboard** → **Conflicts** tab)
3. See pending conflict: `design-v1.dwg` (ACC: v2 vs SP: v3)
4. Click **Resolve**
5. Modal appears:
   - Option A: Keep ACC Docs version (newer)
   - Option B: Keep SharePoint version
6. Select Option A (ACC version wins per SourceWins policy)
7. Click **Resolve** button
8. Confirm: SharePoint version is now overwritten with ACC version
9. Audit log shows: `CONFLICT_RESOLVED` entry with policy applied

#### 4. Test ManualHold Policy

1. Edit binding: Change Conflict Policy to **ManualHold**
2. Repeat steps 1–3 above
3. On conflict detection, file moves to **Quarantine** (not auto-resolved)
4. Dashboard shows operator alert: "1 conflict awaiting manual resolution"
5. Operator must resolve manually via **Conflicts** modal (same UI as step 3 above)

---

## Audit Export Verification

### Export Audit Log for Compliance

1. Go to **Audit Log**
2. Filter: Binding = "Test Sync: ACC → SharePoint", Date = "Last 24 hours"
3. Click **Export as CSV** (or JSON)
4. **Downloaded file contains**:
   - Columns: Timestamp, Actor, Action Type, Binding, Outcome, Context
   - NO file content (only metadata like file path, checksum)
   - Example row: `2025-10-25 14:32:15, system, FILE_CREATED, Test Sync, success, { "file_path": "/design-v1.dwg", "size": 2048000 }`
5. **Verify immutability** (dev test):
   - Try to manually edit exported CSV (you can, it's a file)
   - **Production**: Hash chain metadata in export footer allows detection of tampering
   - Compute SHA256 chain via export API; verify no modifications

---

## Troubleshooting

### Issue: "Failed to start Aspire AppHost"

**Symptoms**: `dotnet run --project Linksy.AppHost` fails with "Port 5000 already in use"

**Solution**:
```bash
# Find process using port 5000
lsof -i :5000
# Kill process
kill -9 <PID>
# Retry
dotnet run --project Linksy.AppHost
```

### Issue: "TypeScript compilation errors in React"

**Symptoms**: React dev server won't start; errors like `Type 'any' is not assignable to type 'string'`

**Solution**:
```bash
cd src/frontend
npm run type-check  # Identify errors
npm run lint        # Fix linting issues
```

### Issue: "Database connection refused"

**Symptoms**: `Exception connecting to PostgreSQL: Server not found`

**Solution**:
```bash
# Verify PostgreSQL is running
psql -U postgres -h localhost -c "SELECT version();"

# If not running, start Docker container:
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=localdev --name linksy-db postgres:latest

# Verify connection string in .env.local
```

### Issue: "ACC Docs API returns 401 Unauthorized"

**Symptoms**: Sync fails with "Invalid or expired OAuth token"

**Solution**:
1. Check `.env.local` for correct `ACC_DOCS_CLIENT_ID` and `ACC_DOCS_CLIENT_SECRET`
2. Verify sandbox credentials are still valid (may expire after 90 days)
3. Re-authorize connector: **Settings** → **Connectors** → **Reauthorize**

### Issue: "Conflict not appearing in dashboard"

**Symptoms**: Manual sync completes but no conflict shown in UI

**Solution**:
1. Verify binding has **Conflict Policy = ManualHold** (not SourceWins)
2. Check both platforms were actually modified (timestamps should differ)
3. Inspect audit log for `CONFLICT_DETECTED` entry; if not present, no actual conflict occurred
4. Refresh dashboard (F5)

### Issue: "Rate-limit errors during testing"

**Symptoms**: Sync jobs fail with "HTTP 429 Too Many Requests"

**Solution**:
1. Wait 1–2 minutes (ACC rate-limit window is per-minute)
2. System automatically retries with exponential backoff (7 retries over 120s)
3. If still failing, check sync job logs in Aspire Dashboard for retry details

---

## Next Steps

After validating the quickstart, proceed to:

1. **Create more bindings** with different configurations (bidirectional, different conflict policies)
2. **Run stress tests** (sync 1000+ files, measure throughput)
3. **Test credential rotation** (Settings → Connectors → Rotate Credentials)
4. **Monitor observability** (Aspire Dashboard → Metrics tab; export audit logs for compliance)
5. **Review API contracts** in `contracts/api-spec.yaml` for integration with third-party tools

---

## Support & Feedback

- **Issues**: File GitHub issues in the repository
- **Questions**: Check `.github/README.md` for team contacts
- **Performance tuning**: See `plan.md` for scalability recommendations

---

**Quickstart Complete** ✅ You now have a working Linksy instance running locally!
