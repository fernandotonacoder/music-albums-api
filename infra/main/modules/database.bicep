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

// ============================================================================
// VNet Integration Parameters
// ============================================================================

@description('Resource ID of the delegated subnet for PostgreSQL Flexible Server')
param postgresSubnetId string

@description('Resource ID of the Private DNS Zone for PostgreSQL')
param postgresDnsZoneId string

// ============================================================================
// PostgreSQL Flexible Server
// ============================================================================
// The server is deployed into a delegated subnet (VNet integration).
// No public access or firewall rules are needed. All connectivity is private.
// Resources in the same VNet (e.g., Container App) can reach the server directly.
// To connect from your laptop, use a VPN/bastion or temporarily add a firewall
// rule in the Azure portal.

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
    network: {
      delegatedSubnetResourceId: postgresSubnetId
      privateDnsZoneArmResourceId: postgresDnsZoneId
      publicNetworkAccess: 'Disabled'
    }
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
