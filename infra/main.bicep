// Linksy Infrastructure as Code (Bicep)
// Defines all Azure resources for dev, staging, and production environments
// Includes: SQL Serverless, Storage, Key Vault, App Insights, Functions, Static Web App

targetScope = 'resourceGroup'

@description('Environment name (dev, staging, prod)')
param environment string

@description('Azure region for resource deployment')
param location string = resourceGroup().location

@description('Application name prefix for resource naming')
param appName string = 'linksy'

@description('Resource naming suffix (derived from environment)')
param namingSuffix string = environment == 'prod' ? '' : '-${environment}'

@description('SQL Server administrator login username')
@secure()
param sqlAdminUsername string

@description('SQL Server administrator login password (leave empty to auto-generate and store in Key Vault)')
@secure()
param sqlAdminPassword string = ''

@description('Key Vault administrator object ID (Azure AD user or service principal)')
param keyVaultAdminObjectId string

@description('Enable automatic password generation via deployment script (recommended: true)')
param enableSecretProvisioning bool = true

@description('App Service Plan SKU for Functions (B1, B2, P1V2, etc.)')
param functionsPlanSku string = 'Y1'

@description('Storage account tier (Standard/Premium)')
param storageAccountTier string = 'Standard'

@description('SQL Database max autoPause delay in minutes')
param sqlMaxAutoPauseDelayInMinutes int = 60

@description('SQL Database backup retention in days')
param sqlBackupRetentionDays int = 7

@description('Tags to apply to all resources')
param tags object = {
  environment: environment
  application: appName
  createdBy: 'bicep'
  createdDate: utcNow('u')
}

// ============================================================================
// Variables
// ============================================================================

var storageAccountName = '${replace(appName, '-', '')}stor${namingSuffix}' // Must be 3-24 chars, alphanumeric only
var keyVaultName = '${appName}-kv${namingSuffix}'
var sqlServerName = '${appName}-sql${namingSuffix}'
var sqlDatabaseName = '${appName}-db${namingSuffix}'
var appInsightsName = '${appName}-ai${namingSuffix}'
var logAnalyticsName = '${appName}-law${namingSuffix}'
var functionAppName = '${appName}-func${namingSuffix}'
var functionsPlanName = '${appName}-plan${namingSuffix}'
var staticWebAppName = '${appName}-swa${namingSuffix}'
var userAssignedIdentityName = '${appName}-identity${namingSuffix}'
var vnetName = '${appName}-vnet${namingSuffix}'
var containerTransientCache = 'transient-cache'
var containerQuarantine = 'quarantine'
var containerAuditExports = 'audit-exports'
var queueNameManualHolds = 'manual-holds'
var queueNamePoison = '${queueNameManualHolds}-poison'
var actualSqlAdminPassword = empty(sqlAdminPassword) ? uniqueString(resourceGroup().id, 'sql-pw') : sqlAdminPassword

// ============================================================================
// User-Assigned Managed Identity
// ============================================================================

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: userAssignedIdentityName
  location: location
  tags: tags
}

// ============================================================================
// Virtual Network & Networking
// ============================================================================

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: 'default'
        properties: {
          addressPrefix: '10.0.1.0/24'
          serviceEndpoints: [
            {
              service: 'Microsoft.Storage'
            }
            {
              service: 'Microsoft.Sql'
            }
            {
              service: 'Microsoft.KeyVault'
            }
          ]
          delegations: [
            {
              name: 'delegation'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

// ============================================================================
// SQL Database (Serverless)
// ============================================================================

resource sqlServer 'Microsoft.Sql/servers@2021-11-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: actualSqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
  dependsOn: [
    secretProvisioning
  ]
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB
    autoPauseDelay: sqlMaxAutoPauseDelayInMinutes
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Geo'
    isLedgerOn: false
  }
}

// SQL Database backup retention policy
resource sqlDatabaseBackupShortTermRetention 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2021-11-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    retentionDays: sqlBackupRetentionDays
    diffBackupIntervalInHours: 24
  }
}

// Allow Azure services to access SQL Server
resource sqlFirewallRuleAzureServices 'Microsoft.Sql/servers/firewallRules@2021-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Server audit policy
// Note: Using diagnostic settings for audit instead of deprecated auditingPolicies API
// Audit can be configured post-deployment via diagnostic settings if needed

// ============================================================================
// Azure Storage Account
// ============================================================================

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: '${storageAccountTier}_LRS'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      virtualNetworkRules: [
        {
          id: '${vnet.id}/subnets/default'
          action: 'Allow'
        }
      ]
      ipRules: []
    }
  }
}

// Blob service configuration
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-09-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    changeFeed: {
      enabled: true
      retentionInDays: 7
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

// Transient cache container (24-hour retention)
resource containerTransientCacheResource 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: blobService
  name: containerTransientCache
  properties: {
    publicAccess: 'None'
  }
}

// Transient cache lifecycle policy (auto-delete after 24 hours)
resource storageAccountManagementPolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2021-09-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'DeleteTransientCacheAfter24Hours'
          enabled: true
          type: 'Lifecycle'
          definition: {
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 1
                }
              }
              snapshot: {
                delete: {
                  daysAfterCreationGreaterThan: 1
                }
              }
            }
            filters: {
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                containerTransientCache
              ]
            }
          }
        }
        {
          name: 'DeleteQuarantineAfter7Days'
          enabled: true
          type: 'Lifecycle'
          definition: {
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 7
                }
              }
              snapshot: {
                delete: {
                  daysAfterCreationGreaterThan: 7
                }
              }
            }
            filters: {
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                containerQuarantine
              ]
            }
          }
        }
      ]
    }
  }
}

// Quarantine container (7-day retention, soft-delete)
resource containerQuarantineResource 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: blobService
  name: containerQuarantine
  properties: {
    publicAccess: 'None'
  }
  dependsOn: [
    containerTransientCacheResource
  ]
}

// Audit exports container (WORM - immutable, versioning enabled)
resource containerAuditExportsResource 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-09-01' = {
  parent: blobService
  name: containerAuditExports
  properties: {
    publicAccess: 'None'
    immutableStorageWithVersioning: {
      enabled: true
    }
  }
  dependsOn: [
    containerQuarantineResource
  ]
}

// Queue service configuration
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2021-09-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

// Manual holds queue
resource queueManualHolds 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  parent: queueService
  name: queueNameManualHolds
  properties: {}
}

// Poison queue for dead-letter messages
resource queuePoison 'Microsoft.Storage/storageAccounts/queueServices/queues@2021-09-01' = {
  parent: queueService
  name: queueNamePoison
  properties: {}
}

// ============================================================================
// Secret Provisioning Module
// Generates and stores SQL admin password in Key Vault automatically
// ============================================================================

module secretProvisioning 'secret-provisioning.bicep' = if (enableSecretProvisioning) {
  name: 'secret-provisioning'
  params: {
    keyVaultName: keyVaultName
    secretName: 'SqlAdminPassword'
    rotate: false  // Only generate once; set to true to regenerate on redeploy
    secretDescription: 'SQL Server admin password (auto-generated by deployment script)'
    managedIdentityName: '${appName}-secret-provisioner${namingSuffix}'
    tags: tags
  }
}

// ============================================================================
// Key Vault
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2021-11-01-preview' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    enabledForDeployment: false
    enabledForTemplateDeployment: false
    enabledForDiskEncryption: false
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: keyVaultAdminObjectId
        permissions: {
          keys: [
            'get'
            'list'
            'create'
            'delete'
            'update'
            'recover'
            'purge'
          ]
          secrets: [
            'get'
            'list'
            'set'
            'delete'
            'recover'
            'purge'
          ]
          certificates: [
            'get'
            'list'
            'create'
            'delete'
            'update'
            'recover'
            'purge'
          ]
        }
      }
      {
        tenantId: subscription().tenantId
        objectId: userAssignedIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
          keys: [
            'get'
            'list'
          ]
        }
      }
    ]
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      virtualNetworkRules: [
        {
          id: '${vnet.id}/subnets/default'
        }
      ]
      ipRules: []
    }
  }
}

// Key Vault secrets for SQL connection
resource kvSecretSqlConnectionString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: keyVault
  name: 'SqlConnectionString'
  properties: {
    value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabase.name};Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
  }
}

// Key Vault secret for Storage connection
resource kvSecretStorageConnectionString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: keyVault
  name: 'StorageConnectionString'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
  }
}

// ============================================================================
// Application Insights & Log Analytics
// ============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    WorkspaceResourceId: logAnalyticsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Alert rule: High error rate
resource alertHighErrorRate 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appInsightsName}-high-error-rate'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when error rate exceeds 5%'
    scopes: [
      appInsights.id
    ]
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    severity: 2
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.MultipleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Failed requests'
          metricName: 'failedRequests'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
  }
}

// Alert rule: Sync job throttling
resource alertSyncThrottling 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${appInsightsName}-sync-throttling'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when sync jobs are throttled'
    scopes: [
      appInsights.id
    ]
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    severity: 2
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.MultipleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Throttle events'
          metricName: 'customMetrics'
          operator: 'GreaterThan'
          threshold: 10
          timeAggregation: 'Total'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
  }
}

// ============================================================================
// Azure Functions (Consumption Plan)
// ============================================================================

resource functionsPlan 'Microsoft.Web/serverfarms@2022-09-01' = if (functionsPlanSku == 'Y1') {
  name: functionsPlanName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionPlanPremium 'Microsoft.Web/serverfarms@2022-09-01' = if (functionsPlanSku != 'Y1') {
  name: functionsPlanName
  location: location
  tags: tags
  kind: 'app'
  sku: {
    name: functionsPlanSku
    tier: functionsPlanSku
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    serverFarmId: functionsPlanSku == 'Y1' ? functionsPlan.id : functionPlanPremium.id
    siteConfig: {
      alwaysOn: false
      linuxFxVersion: 'DOTNET|8.0'
      use32BitWorkerProcess: false
      http20Enabled: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'linksyfunctioncontent'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'SqlConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=SqlConnectionString)'
        }
        {
          name: 'StorageConnectionString'
          value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=StorageConnectionString)'
        }
        {
          name: 'ENVIRONMENT'
          value: environment
        }
      ]
    }
    httpsOnly: true
  }
}

// Function app configuration
resource functionAppConfig 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionApp
  name: 'web'
  properties: {
    numberOfWorkers: 1
    defaultDocuments: []
    netFrameworkVersion: 'v6.0'
    requestTracingEnabled: false
    remoteDebuggingEnabled: false
    httpLoggingEnabled: false
    detailedErrorLoggingEnabled: false
    publishingUsername: '.${functionAppName}'
    scmType: 'None'
    use32BitWorkerProcess: false
    webSocketsEnabled: false
    managedPipelineMode: 'Integrated'
    virtualApplications: [
      {
        virtualPath: '/'
        physicalPath: 'site\\wwwroot'
        preloadEnabled: false
      }
    ]
    loadBalancing: 'LeastRequests'
    experiments: {
      rampUpRules: []
    }
    autoHealEnabled: true
    autoHealRules: {
      triggers: {
        statusCodes: [
          {
            status: 500
            subStatus: 0
            count: 50
            timeInterval: '00:01:00'
          }
        ]
      }
      actions: {
        actionType: 'Recycle'
        minProcessExecutionTime: '00:01:00'
      }
    }
    cors: {
      allowedOrigins: [
        'http://localhost:5173'
        'http://localhost:3000'
      ]
      supportCredentials: true
    }
    localMySqlEnabled: false
    ipSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 1
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictions: [
      {
        ipAddress: 'Any'
        action: 'Allow'
        priority: 1
        name: 'Allow all'
        description: 'Allow all access'
      }
    ]
    scmIpSecurityRestrictionsUseMain: false
    http20Enabled: true
    minTlsVersion: '1.2'
    scmMinTlsVersion: '1.2'
    preWarmedInstanceCount: 0
    functionAppScaleLimit: 200
    healthCheckPath: '/api/health'
    functionsRuntimeScaleMonitoringEnabled: true
    websiteTimeZone: 'UTC'
    minimumElasticInstanceCount: 0
  }
}

// ============================================================================
// Static Web App (Frontend)
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    provider: 'GitHub'
    branch: 'main'
    buildProperties: {
      appLocation: 'src/frontend'
      outputLocation: 'dist'
      appBuildCommand: 'npm run build'
    }
  }
}

// ============================================================================
// Role Assignments
// ============================================================================

// Managed identity role assignment for Key Vault
resource roleAssignmentKeyVault 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, userAssignedIdentity.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Managed identity role assignment for Storage
resource roleAssignmentStorage 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, userAssignedIdentity.id, 'Storage Blob Data Contributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Function app role assignment for Key Vault
resource roleAssignmentFunctionKeyVault 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, functionApp.id, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Resource Group name')
output resourceGroupName string = resourceGroup().name

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('SQL Database name')
output sqlDatabaseName string = sqlDatabase.name

@description('Storage Account name')
output storageAccountName string = storageAccount.name

@description('Storage Account connection string')
@secure()
output storageConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'

@description('Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Application Insights instrumentation key')
@secure()
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Application Insights connection string')
@secure()
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Functions App name')
output functionAppName string = functionApp.name

@description('Static Web App URL')
output staticWebAppUrl string = staticWebApp.properties.defaultHostname

@description('User Assigned Identity resource ID')
output userAssignedIdentityId string = userAssignedIdentity.id

@description('Log Analytics Workspace ID')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
