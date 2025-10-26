// Linksy Secret Provisioning Module
// Generates and securely stores secrets in Key Vault using a managed identity
// No secrets are exposed in parameters, outputs, or deployment logs

targetScope = 'resourceGroup'

@description('Existing Key Vault name to store secrets in')
param keyVaultName string

@description('The secret name to create/rotate (e.g., SqlAdminPassword)')
param secretName string = 'SqlAdminPassword'

@description('If true, regenerate secret on every deploy; if false, only create if missing')
param rotate bool = false

@description('Secret description for Key Vault metadata')
@secure()
param secretDescription string

@description('Deployment script managed identity name')
param managedIdentityName string = 'linksy-secret-provisioner'

@description('Tags to apply to resources created here')
param tags object = {
  purpose: 'secret-provisioning'
  createdBy: 'bicep-deployment-script'
}

// ============================================================================
// Reference existing Key Vault
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// ============================================================================
// User-Assigned Managed Identity for Deployment Script
// ============================================================================

resource provisionerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: resourceGroup().location
  tags: tags
}

// ============================================================================
// RBAC: Grant "Key Vault Secrets Officer" role to the managed identity
// ============================================================================

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, provisionerIdentity.id, 'kv-secrets-officer')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'b86a8fe4-44ce-4948-aee5-eccb2c155cd7' // Key Vault Secrets Officer
    )
    principalId: provisionerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// Deployment Script: Generate Strong Password and Write to Key Vault
// ============================================================================

resource generateSecretScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'generate-${secretName}'
  location: resourceGroup().location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${provisionerIdentity.id}': {}
    }
  }
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '11.0'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
    timeout: 'PT5M'
    arguments: '-VaultName "${keyVault.name}" -SecretName "${secretName}" -Rotate ${rotate} -Description "${secretDescription}"'
    scriptContent: '''
param(
  [string] $VaultName,
  [string] $SecretName,
  [bool]   $Rotate,
  [string] $Description
)

# Authenticate using this script's managed identity
Connect-AzAccount -Identity -ErrorAction Stop | Out-Null

# Check if secret already exists and rotation is disabled
$existing = Get-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -ErrorAction SilentlyContinue
if ($existing -and -not $Rotate) {
  Write-Host "Secret '$SecretName' already exists and rotation is disabled. Skipping generation."
  return
}

# Character sets for password generation
$uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
$lowercase = 'abcdefghijklmnopqrstuvwxyz'
$digits = '0123456789'
$symbols = '@$!%*?&'
$allChars = ($uppercase + $lowercase + $digits + $symbols).ToCharArray()

# Function to generate random string from character set
function GenerateRandomString {
  param(
    [char[]]$CharSet,
    [int]$Length
  )
  $randomBytes = New-Object byte[] $Length
  [System.Security.Cryptography.RandomNumberGenerator]::Fill($randomBytes)
  $result = @()
  foreach ($byte in $randomBytes) {
    $result += $CharSet[$byte % $CharSet.Count]
  }
  return -join $result
}

# Generate password with guaranteed complexity:
# - 2 uppercase letters
# - 6 lowercase letters
# - 6 digits
# - 2 symbols
# - 8 random characters (any type)
$pwd = `
  (GenerateRandomString $uppercase.ToCharArray() 2) + `
  (GenerateRandomString $lowercase.ToCharArray() 6) + `
  (GenerateRandomString $digits.ToCharArray() 6) + `
  (GenerateRandomString $symbols.ToCharArray() 2) + `
  (GenerateRandomString $allChars 8)

# Shuffle the password to avoid predictable patterns
$pwd = -join ($pwd.ToCharArray() | Get-Random -Count $pwd.Length)

# Store in Key Vault as a secure string
$securePassword = ConvertTo-SecureString -String $pwd -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $VaultName -Name $SecretName -SecretValue $securePassword -Tags @{ 'description' = $Description } -ErrorAction Stop | Out-Null

Write-Host "Secret '$SecretName' successfully generated and stored in Key Vault '$VaultName'."
'''
  }
  dependsOn: [roleAssignment]
}

// ============================================================================
// Outputs
// ============================================================================

@description('Resource ID of the provisioner managed identity')
output provisionerIdentityId string = provisionerIdentity.id

@description('Principal ID of the provisioner managed identity (for RBAC assignments)')
output provisionerPrincipalId string = provisionerIdentity.properties.principalId

@description('Deployment script resource ID')
output deploymentScriptId string = generateSecretScript.id

@description('Secret name that was provisioned')
output secretName string = secretName

@description('Key Vault name where secret was stored')
output keyVaultName string = keyVault.name
