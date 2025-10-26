# Linksy Infrastructure as Code (Bicep)

This directory contains Infrastructure as Code (IaC) templates for deploying Linksy to Azure using Bicep.

## Overview

The Bicep templates define a complete serverless infrastructure for Linksy including:

- **Azure SQL Database** (Serverless) — Multi-tenant relational database with append-only audit tables
- **Azure Storage Account** — Blob containers (transient cache, quarantine, audit exports) with lifecycle policies
- **Azure Key Vault** — Secure credential storage and rotation
- **Azure Functions** — Consumption plan for sync orchestrations (Durable Functions)
- **Azure Static Web Apps** — Frontend deployment
- **Application Insights** — Observability, tracing, and alerting
- **Log Analytics Workspace** — Centralized logging
- **Managed Identity** — Service principal for secure inter-service communication
- **Virtual Network** — Network isolation and service endpoints

## Files

### Templates

- **`main.bicep`** — Complete infrastructure template with all Azure resources
  - Parameterized for dev, staging, and production environments
  - Includes all networking, security, and observability configurations
  - ~900 lines of Bicep code

- **`secret-provisioning.bicep`** — Secret provisioning module (called by `main.bicep`)
  - Auto-generates strong SQL admin passwords using deployment scripts
  - Stores secrets directly in Key Vault (no manual steps required)
  - Uses managed identity with least-privilege RBAC

### Parameters

- **`parameters.dev.json`** — Development environment parameters
  - Single-region deployment (Sweden Central)
  - Consumption plan Functions (Y1 - lowest cost)
  - 1-hour SQL autoPause delay
  - 7-day backup retention
  - Standard storage tier
  - **No manual secrets required** ✓

- **`parameters.prod.json`** — Production environment parameters
  - Single-region deployment (Sweden Central, upgrade for multi-region in Phase 3)
  - Premium plan Functions (EP1 - for reserved capacity)
  - 15-minute SQL autoPause delay
  - 35-day backup retention (compliance)
  - Standard storage tier
  - Higher criticality tags
  - **No manual secrets required** ✓

### Scripts

- **`deploy-infra.sh`** (created in T004) — Automated deployment script
- **`validate.sh`** (future) — Bicep validation and testing

## Prerequisites

### Local Development

```bash
# Install Azure CLI
brew install azure-cli

# Install Bicep CLI
az bicep install

# Login to Azure
az login
```

### Azure Permissions

- `Contributor` role on the target subscription (to create resources)
- **No manual Key Vault setup required** — the deployment script handles secret generation and storage automatically

## Automatic Secret Generation

The `secret-provisioning.bicep` module automatically:

1. **Creates a managed identity** with limited permissions
2. **Grants Key Vault Secrets Officer** role scoped to your Key Vault
3. **Runs a PowerShell deployment script** that:
   - Generates a cryptographically secure 24-character password
   - Ensures complexity (uppercase, lowercase, digits, symbols)
   - Stores the secret in Key Vault without exposing it in logs or outputs
   - Supports idempotent re-runs (create once, optionally rotate)

**Result**: No manual password management, no secrets in parameters/outputs, fully compliant with security best practices.

## Resource Naming Convention

All resources follow this naming pattern:

```
{appName}[-{component}][-{environment}]
```

Examples:
- **Dev**: `linksy-sql-dev`, `linksy-sa-dev`, `linksy-kv-dev`
- **Prod**: `linksy-sql`, `linksy-sa`, `linksy-kv` (no suffix)

## Deployment

### Deploy to Development

**One-step deployment** (secret generation is automated):

```bash
# Set variables
export SUBSCRIPTION_ID="your-subscription-id"
export RESOURCE_GROUP="linksy-dev-rg"

# Create resource group
az group create \
  --name "$RESOURCE_GROUP" \
  --location "swedencentral"

# Deploy (includes automatic secret generation via deployment script)
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters @infra/parameters.dev.json
```

After deployment:
1. Check Key Vault for the auto-generated `SqlAdminPassword` secret
2. Use this secret to configure your application runtime

### Deploy to Production

**One-step deployment** (secret generation is automated):

```bash
# Set variables
export SUBSCRIPTION_ID="your-subscription-id"
export RESOURCE_GROUP="linksy-prod-rg"

# Create resource group
az group create \
  --name "$RESOURCE_GROUP" \
  --location "swedencentral"

# Deploy (includes automatic secret generation via deployment script)
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters @infra/parameters.prod.json
```

### Secret Rotation

To regenerate the SQL admin password on subsequent deployments:

```bash
az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file infra/main.bicep \
  --parameters @infra/parameters.dev.json \
  --parameters rotate=true
```

**Note**: The `secret-provisioning.bicep` module handles all secret management. No manual Key Vault setup is required.

## Resources Created

### Core Services

| Resource | Dev SKU | Prod SKU | Purpose |
|----------|---------|----------|---------|
| SQL Database | GP_S_Gen5_1 | GP_S_Gen5_1 | Multi-tenant schema, audit trail |
| Storage Account | Standard_LRS | Standard_LRS | Blobs (cache/quarantine/exports), Queues |
| Key Vault | Standard | Standard | Credential storage (RBAC-only) |
| Functions | Y1 (Consumption) | EP1 (Premium) | Durable Functions orchestration |
| Static Web App | Standard | Standard | React frontend |
| App Insights | Pay-as-you-go | Pay-as-you-go | Observability |
| Managed Identity | — | — | Secret provisioning (auto-generated secrets) |

### Storage Containers

| Container | Retention | Policy | Purpose |
|-----------|-----------|--------|---------|
| `transient-cache` | 24 hours | Auto-delete | Temporary file cache |
| `quarantine` | 7 days | Auto-delete + soft-delete | Soft-deleted files |
| `audit-exports` | 2555 days (7 years) | WORM + versioning | Immutable audit exports |

### Storage Queues

| Queue | Purpose | DLQ |
|-------|---------|-----|
| `manual-holds` | Conflict escalations | `manual-holds-poison` |

## Network Topology

- **Virtual Network**: 10.0.0.0/16
- **Default Subnet**: 10.0.1.0/24 (service endpoints for SQL, Storage, KV)
- **Service Endpoints**: Enabled for Sql, Storage, KeyVault
- **Delegations**: Microsoft.Web/serverFarms (for Functions)

## Security Features

### Identity & Access

- **Managed Identity**: User-assigned identity for secure inter-service communication
- **RBAC**: Least-privilege role assignments
  - Functions → Key Vault: `Key Vault Secrets User`
  - Functions → Storage: `Storage Blob Data Contributor`

### Compliance & Auditing

- **SQL Audit Policy**: All authentication, object changes, permission changes logged
- **Storage Soft-Delete**: 7-day recovery window
- **Immutable Blobs**: Audit exports in WORM mode with versioning
- **Key Vault Logging**: All access attempts logged (via App Insights)

### Network Security

- **Firewall**: All services allow Azure services
- **TLS**: Minimum TLS 1.2 enforced
- **Service Endpoints**: VNet integration for SQL, Storage, KV

## Monitoring & Alerts

### Application Insights Metrics

- Request rate, response time, failures
- Dependency tracking (SQL, Storage, external APIs)
- Custom metrics (job stats, sync latency)

### Alert Rules

1. **High Error Rate**: Alert if >5% of requests fail in 15 min
2. **Sync Throttling**: Alert if >10 throttle events in 15 min

(See Phase 2 for additional observability dashboards and alerts)

## Cost Estimation

### Development (Monthly)

| Service | Tier | Est. Cost |
|---------|------|-----------|
| SQL Database | GP_S_Gen5_1 (serverless) | $20-40 |
| Storage | Standard (100 GB) | $2-5 |
| Key Vault | Standard | ~$0.50 |
| Functions | Consumption | $1-10 |
| App Insights | Pay-as-you-go | $2-5 |
| Static Web App | Standard | $10 |
| **Total** | | **~$35-70** |

### Production (Monthly)

| Service | Tier | Est. Cost |
|---------|------|-----------|
| SQL Database | GP_S_Gen5_1 (serverless) | $50-100 |
| Storage | Standard (500 GB) | $10-20 |
| Key Vault | Standard | ~$0.50 |
| Functions | Premium (EP1) | $150-200 |
| App Insights | Pay-as-you-go | $10-30 |
| Static Web App | Standard | $10 |
| **Total** | | **~$230-360** |

## Customization

### Changing Region

Edit parameters file to change `location`:

```json
"location": {
  "value": "westeurope"
}
```

### Changing Function Plan

For production, upgrade from EP1 to EP2/EP3 for more reserved capacity:

```json
"functionsPlanSku": {
  "value": "EP3"
}
```

### Adding Storage Containers

Add to `main.bicep` storage container definitions:

```bicep
resource containerCustom 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: blobService
  name: 'custom-container'
  properties: {
    publicAccess: 'None'
  }
}
```

## Troubleshooting

### Deployment Fails: "Insufficient Quota"

Increase quota in the target region or reduce function tier (EP1 → Y1).

### Key Vault Access Denied

Ensure:
1. Your user has `Key Vault Administrator` role
2. Key Vault network rules allow your IP (if firewall enabled)
3. Managed identity has `Key Vault Secrets User` role

### Functions Can't Connect to SQL

Check:
1. SQL Firewall rule allows Azure services (`0.0.0.0 - 0.0.0.0`)
2. Connection string in Key Vault is correct
3. Functions managed identity has SQL login/permissions (Phase 1 task)

## Next Steps

1. **Deploy Dev**: Follow "Deploy to Development" section
2. **Validate Connectivity**: Run Phase 1 integration tests
3. **Deploy Staging**: Test full pipeline before prod
4. **Deploy Prod**: Follow "Deploy to Production" section
5. **Configure CI/CD**: Add GitHub Actions workflow for automated deployments (Phase 2)

## References

- [Bicep Documentation](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure SQL Database Serverless](https://learn.microsoft.com/en-us/azure/azure-sql/database/serverless-tier-overview)
- [Azure Functions Premium Plan](https://learn.microsoft.com/en-us/azure/azure-functions/functions-premium-plan)
- [Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/)
- [Managed Identities](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)

## Support

For issues or questions:
1. Check Azure Portal diagnostics
2. Review Application Insights traces
3. Consult [Linksy documentation](../specs/001-aec-file-sync/)
4. Review Bicep code comments in `main.bicep`
