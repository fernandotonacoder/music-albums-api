// ============================================================================
// Database Module - PostgreSQL Flexible Server
// ============================================================================

@description('Location for the database server')
param location string

@description('Name for the PostgreSQL server')
param postgresServerName string

@description('PostgreSQL administrator login')
param postgresAdminLogin string

@secure()
@description('PostgreSQL administrator password')
param postgresAdminPassword string

@description('PostgreSQL version')
param postgresVersion string = '18'

@description('SKU name for PostgreSQL server')
param skuName string = 'Standard_B1ms'

@description('SKU tier for PostgreSQL server')
@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param skuTier string = 'Burstable'

@description('Storage size in GB')
param storageSizeGB int = 32

@description('Backup retention days')
param backupRetentionDays int = 7

@description('Tags to apply to database resources')
param tags object = {}

@allowed([
  'dev'
  'prod'
])
@description('Deployment environment. Prod uses VNet integration; dev uses public access for direct connectivity.')
param deploymentEnvironment string = 'dev'

// ============================================================================
// VNet Integration Parameters (prod only)
// ============================================================================

@description('Resource ID of the delegated subnet for PostgreSQL Flexible Server (prod only)')
param postgresSubnetId string = ''

@description('Resource ID of the Private DNS Zone for PostgreSQL (prod only)')
param postgresDnsZoneId string = ''

var isProduction = deploymentEnvironment == 'prod'

// ============================================================================
// PostgreSQL Flexible Server
// ============================================================================
// prod: VNet integration via delegated subnet + private DNS zone. No public access.
//       Only resources inside the VNet (Container App) can reach the server.
// dev:  Public network access enabled. Connect directly from any machine.
//       No VPN or bastion required.

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    version: postgresVersion
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: storageSizeGB
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: isProduction ? {
      delegatedSubnetResourceId: postgresSubnetId
      privateDnsZoneArmResourceId: postgresDnsZoneId
      publicNetworkAccess: 'Disabled'
    } : {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource postgresAllowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = if (!isProduction) {
  parent: postgres
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('PostgreSQL server resource ID')
output postgresServerId string = postgres.id

@description('PostgreSQL server FQDN')
output postgresServerFqdn string = postgres.properties.fullyQualifiedDomainName

@description('PostgreSQL server name')
output postgresServerName string = postgres.name

@description('Database connection string (includes password)')
@secure()
output connectionString string = 'Server=${postgres.properties.fullyQualifiedDomainName};Database=postgres;Port=5432;User Id=${postgresAdminLogin};Password=${postgresAdminPassword};Ssl Mode=Require;'
