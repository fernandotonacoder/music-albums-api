// ============================================================================
// Monitoring Module - Log Analytics & Application Insights
// ============================================================================

@description('Location for the monitoring resources')
param location string

@description('Name for the Log Analytics workspace')
param logAnalyticsName string

@description('Name for Application Insights')
param appInsightsName string

@description('Tags to apply to monitoring resources')
param tags object = {}

// ============================================================================
// Log Analytics Workspace
// ============================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
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
// Application Insights
// ============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Log Analytics customer ID')
output logAnalyticsCustomerId string = logAnalytics.properties.customerId

@description('Log Analytics primary shared key')
@secure()
output logAnalyticsPrimarySharedKey string = logAnalytics.listKeys().primarySharedKey

@description('Application Insights ID')
output appInsightsId string = appInsights.id

@description('Application Insights connection string')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Application Insights instrumentation key')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
