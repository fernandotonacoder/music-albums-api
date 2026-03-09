// ============================================================================
// Identity API - Minimal Azure Infrastructure
// ============================================================================
// Dev tool for generating JWT tokens. Deploy manually when needed.
// Tear down when done to avoid unnecessary costs.
// ============================================================================

@description('Location for all resources')
param location string = resourceGroup().location

@description('Base name for resource naming')
param baseName string = 'identity-api'

@description('Deployment suffix used in resource names')
@allowed([ 'dev', 'prod' ])
param deploymentSuffix string = 'dev'

@description('Container image to deploy')
param containerImage string

@description('JWT signing key - must match Music Albums API Jwt__Key')
@secure()
param jwtSecret string

@description('.NET runtime environment')
@allowed([ 'Development', 'Staging', 'Production' ])
param aspNetCoreEnvironment string = 'Development'

@description('Resource ID of an existing shared Container App Environment. When provided, skips creating a new one.')
param existingEnvironmentId string = ''

// ============================================================================
// Resources
// ============================================================================

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = if (empty(existingEnvironmentId)) {
  name: '${baseName}-env-${deploymentSuffix}'
  location: location
  properties: {
    zoneRedundant: false
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

var resolvedEnvironmentId = !empty(existingEnvironmentId) ? existingEnvironmentId : containerEnv.id

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${baseName}-${deploymentSuffix}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: resolvedEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 5003
        allowInsecure: false
      }
      secrets: [
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
      ]
    }
    template: {
      containers: [
        {
          name: baseName
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: aspNetCoreEnvironment
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Identity API URL')
output identityApiUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
