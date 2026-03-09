// ============================================================================
// Compute Module - Container Apps Environment & Container App
// ============================================================================

@description('Location for the compute resources')
param location string

@description('Name for the Container Apps Environment')
param environmentName string

@description('Name for the Container App')
param containerAppName string

@description('Container image to deploy')
param containerImage string

@description('Target port for the container')
param targetPort int = 8080

@description('CPU allocation')
param cpu string = '0.25'

@description('Memory allocation')
param memory string = '0.5Gi'

@description('Minimum replicas')
param minReplicas int = 0

@description('Maximum replicas')
param maxReplicas int = 3

@description('Log Analytics customer ID')
param logAnalyticsCustomerId string

@description('Log Analytics primary shared key')
@secure()
param logAnalyticsPrimarySharedKey string

@description('ASPNETCORE_ENVIRONMENT value')
@allowed([
  'Development'
  'Staging'
  'Production'
])
param aspNetCoreEnvironment string

@description('Database connection string secret URI from Key Vault')
param dbConnectionSecretUri string

@description('JWT key secret URI from Key Vault')
param jwtKeySecretUri string

@description('API key secret URI from Key Vault')
param apiKeySecretUri string

@description('JWT Issuer URL')
param jwtIssuer string

@description('JWT Audience URL')
param jwtAudience string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Tags to apply to compute resources')
param tags object = {}

@description('Resource ID of the subnet for the Container Apps Environment (prod only)')
param containerAppSubnetId string = ''

// ============================================================================
// Container Apps Environment
// ============================================================================

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsPrimarySharedKey
      }
    }
    vnetConfiguration: !empty(containerAppSubnetId) ? {
      infrastructureSubnetId: containerAppSubnetId
      internal: false
    } : null
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
// Container App
// ============================================================================

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    managedEnvironmentId: containerEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
        allowInsecure: false
      }
      secrets: [
        {
          name: 'db-connection-string'
          keyVaultUrl: dbConnectionSecretUri
          identity: 'system'
        }
        {
          name: 'jwt-key'
          keyVaultUrl: jwtKeySecretUri
          identity: 'system'
        }
        {
          name: 'api-key'
          keyVaultUrl: apiKeySecretUri
          identity: 'system'
        }
      ]
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/_health/live'
                port: targetPort
              }
              initialDelaySeconds: 15
              periodSeconds: 10
              timeoutSeconds: 5
              failureThreshold: 60
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/_health'
                port: targetPort
              }
              initialDelaySeconds: 30
              periodSeconds: 10
              timeoutSeconds: 3
              failureThreshold: 3
            }
          ]
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: aspNetCoreEnvironment
            }
            {
              name: 'Database__ConnectionString'
              secretRef: 'db-connection-string'
            }
            {
              name: 'Jwt__Key'
              secretRef: 'jwt-key'
            }
            {
              name: 'Jwt__Issuer'
              value: jwtIssuer
            }
            {
              name: 'Jwt__Audience'
              value: jwtAudience
            }
            {
              name: 'ApiKey'
              secretRef: 'api-key'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Container Apps Environment resource ID')
output containerEnvId string = containerEnv.id

@description('Container App resource ID')
output containerAppId string = containerApp.id

@description('Container App URL')
output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'

@description('Container App FQDN')
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn

@description('Container App principal ID (for RBAC assignments)')
output containerAppPrincipalId string = containerApp.identity.principalId
