// ============================================================================
// Music Albums API - Azure Infrastructure (Modularized)
// ============================================================================
// Prerequisites and setup instructions: see README.md
// 
// This file orchestrates the deployment of all infrastructure modules:
// - Network: VNet, Subnets, Private DNS (prod only)
// - Monitoring: Log Analytics & Application Insights
// - Database: PostgreSQL Flexible Server (private in prod, public in dev)
// - Security: Key Vault & Secrets (private endpoint in prod only)
// - Compute: Container Apps Environment & Container App (VNet in prod only)
// ============================================================================

@description('Location for all resources')
param location string = 'westeurope'

@description('Base name for all resources')
param baseName string = 'music-albums'

@allowed([
  'dev'
  'prod'
])
@description('Deployment suffix used in resource names')
param deploymentSuffix string = 'dev'

@description('PostgreSQL administrator login')
param postgresAdminLogin string

@secure()
@description('PostgreSQL administrator password')
param postgresAdminPassword string

@description('Container image tag')
param imageTag string = 'latest'

@allowed([
  'Development'
  'Staging'
  'Production'
])
@description('.NET runtime environment for the container app')
param aspNetCoreEnvironment string = 'Development'

@secure()
@description('JWT signing key (min 32 characters)')
param jwtKey string

@description('JWT Issuer (must match the value used when generating tokens)')
param jwtIssuer string = 'MusicAlbumsIdentity'

@description('JWT Audience (must match the value used when generating tokens)')
param jwtAudience string = 'MusicAlbumsApi'

@secure()
@description('API Key for admin operations')
param apiKey string

var resourceNames = {
  containerApp: '${baseName}-api-${deploymentSuffix}'
  environment: '${baseName}-env-${deploymentSuffix}'
  postgresServer: '${baseName}-db-${deploymentSuffix}'
  appInsights: '${baseName}-insights-${deploymentSuffix}'
  logAnalytics: '${baseName}-logs-${deploymentSuffix}'
  keyVault: '${baseName}-kv-${deploymentSuffix}'
}

var containerImage = 'ghcr.io/fernandotonacoder/music-albums-api:${imageTag}'

var commonTags = {
  application: 'music-albums-api'
  environment: deploymentSuffix
  managedBy: 'bicep'
}

// ============================================================================
// Module: Network (VNet, Subnets, Private DNS) — prod only
// ============================================================================

module network './modules/network.bicep' = {
  name: 'network-deployment'
  params: {
    location: location
    vnetName: '${baseName}-vnet-${deploymentSuffix}'
    deploymentEnvironment: deploymentSuffix
    tags: commonTags
  }
}

// ============================================================================
// Module: Monitoring (Log Analytics & Application Insights)
// ============================================================================

module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  params: {
    location: location
    logAnalyticsName: resourceNames.logAnalytics
    appInsightsName: resourceNames.appInsights
    deploymentEnvironment: deploymentSuffix
    tags: commonTags
  }
}

// ============================================================================
// Module: Database (PostgreSQL Flexible Server)
// ============================================================================

module database './modules/database.bicep' = {
  name: 'database-deployment'
  params: {
    location: location
    postgresServerName: resourceNames.postgresServer
    postgresAdminLogin: postgresAdminLogin
    postgresAdminPassword: postgresAdminPassword
    deploymentEnvironment: deploymentSuffix
    postgresSubnetId: network.outputs.postgresSubnetId
    postgresDnsZoneId: network.outputs.postgresDnsZoneId
    tags: commonTags
  }
}

// ============================================================================
// Module: Security (Key Vault & Secrets)
// ============================================================================

module security './modules/security.bicep' = {
  name: 'security-deployment'
  params: {
    location: location
    keyVaultName: resourceNames.keyVault
    tenantId: subscription().tenantId
    dbConnectionString: database.outputs.connectionString
    postgresAdminLogin: postgresAdminLogin
    postgresAdminPassword: postgresAdminPassword
    jwtKey: jwtKey
    apiKey: apiKey
    privateEndpointSubnetId: network.outputs.privateEndpointsSubnetId
    keyVaultDnsZoneId: network.outputs.keyVaultDnsZoneId
    deploymentEnvironment: deploymentSuffix
    tags: commonTags
  }
}

// ============================================================================
// Module: Compute (Container Apps Environment & Container App)
// ============================================================================

module compute './modules/compute.bicep' = {
  name: 'compute-deployment'
  params: {
    location: location
    environmentName: resourceNames.environment
    containerAppName: resourceNames.containerApp
    containerImage: containerImage
    aspNetCoreEnvironment: aspNetCoreEnvironment
    logAnalyticsCustomerId: monitoring.outputs.logAnalyticsCustomerId
    logAnalyticsPrimarySharedKey: monitoring.outputs.logAnalyticsPrimarySharedKey
    dbConnectionSecretUri: security.outputs.dbConnectionSecretUri
    jwtKeySecretUri: security.outputs.jwtKeySecretUri
    apiKeySecretUri: security.outputs.apiKeySecretUri
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    containerAppSubnetId: network.outputs.containerAppSubnetId
    tags: commonTags
  }
}

// ============================================================================
// RBAC: Grant Container App access to Key Vault secrets
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: resourceNames.keyVault
}

resource keyVaultSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, resourceNames.containerApp, 'Key Vault Secrets User')
  scope: keyVault
  properties: {
    principalId: compute.outputs.containerAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Container App URL')
output containerAppUrl string = compute.outputs.containerAppUrl

@description('PostgreSQL server FQDN')
output postgresServer string = database.outputs.postgresServerFqdn

@description('Application Insights connection string')
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString

@description('Key Vault URI')
output keyVaultUri string = security.outputs.keyVaultUri

@description('Key Vault name')
output keyVaultName string = security.outputs.keyVaultName
