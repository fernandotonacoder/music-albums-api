// ============================================================================
// Security Module - Key Vault & Secrets
// ============================================================================

@description('Location for the Key Vault')
param location string

@description('Name for the Key Vault')
param keyVaultName string

@description('Tenant ID for Key Vault')
param tenantId string

@description('Database connection string to store')
@secure()
param dbConnectionString string

@description('PostgreSQL admin login to store')
@secure()
param postgresAdminLogin string

@description('PostgreSQL admin password to store')
@secure()
param postgresAdminPassword string

@description('JWT signing key to store')
@secure()
param jwtKey string

@description('API key to store')
@secure()
param apiKey string

@description('Tags to apply to security resources')
param tags object = {}

// ============================================================================
// Key Vault
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 30
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// ============================================================================
// Secrets
// ============================================================================

resource secretDbConnection 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'db-connection-string'
  properties: {
    value: dbConnectionString
  }
}

resource secretPostgresAdminLogin 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'pg-admin-login'
  properties: {
    value: postgresAdminLogin
  }
}

resource secretPostgresAdminPassword 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'pg-admin-password'
  properties: {
    value: postgresAdminPassword
  }
}

resource secretJwtKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'jwt-key'
  properties: {
    value: jwtKey
  }
}

resource secretApiKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'api-key'
  properties: {
    value: apiKey
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Key Vault resource ID')
output keyVaultId string = keyVault.id

@description('Key Vault URI')
output keyVaultUri string = keyVault.properties.vaultUri

@description('Key Vault name')
output keyVaultName string = keyVault.name

@description('Database connection string secret URI')
output dbConnectionSecretUri string = secretDbConnection.properties.secretUri

@description('JWT key secret URI')
output jwtKeySecretUri string = secretJwtKey.properties.secretUri

@description('API key secret URI')
output apiKeySecretUri string = secretApiKey.properties.secretUri
