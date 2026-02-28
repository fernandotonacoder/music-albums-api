// ============================================================================
// Identity API - Minimal Azure Infrastructure
// ============================================================================
// Dev tool for generating JWT tokens. Deploy manually when needed.
// Tear down when done to avoid unnecessary costs.
// ============================================================================

@description('Location for all resources')
param location string = 'westeurope'

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

// ============================================================================
// Resources
// ============================================================================

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'identity-api-env-${deploymentSuffix}'
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

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'identity-api-${deploymentSuffix}'
  location: location
  properties: {
    managedEnvironmentId: containerEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 5003
        transport: 'auto'
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
          name: 'identity-api'
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
        minReplicas: 1
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
