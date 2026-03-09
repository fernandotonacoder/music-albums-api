// ============================================================================
// Shared Container App Environment
// ============================================================================
// Deploys a single Container App Environment + Log Analytics Workspace to a
// shared resource group. All Container Apps (main API, Identity API) reference
// this environment by resource ID instead of creating their own.
//
// Azure for Students limits: 1 Container App Environment per subscription.
//
// Deploy once:
//   az group create --name music-albums-rg-shared --location <location>
//   az deployment group create \
//     --resource-group music-albums-rg-shared \
//     --template-file infra/shared/shared-environment.bicep \
//     --parameters environmentName='music-albums-shared-env'
// ============================================================================

@description('Location for all resources')
param location string = resourceGroup().location

@description('Name for the Container App Environment')
param environmentName string = 'music-albums-shared-env'

@description('Tags to apply to resources')
param tags object = {
  application: 'music-albums'
  purpose: 'shared-environment'
  managedBy: 'bicep'
}

// ============================================================================
// Log Analytics Workspace (required by the Container App Environment)
// ============================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${environmentName}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ============================================================================
// Container App Environment
// ============================================================================

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Container App Environment resource ID (pass to other deployments)')
output environmentId string = containerEnv.id

@description('Container App Environment name')
output environmentName string = containerEnv.name
