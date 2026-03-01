// ============================================================================
// Network Module - VNet, Subnets, and Private DNS
// ============================================================================
// Creates the Virtual Network with:
// - A delegated subnet for PostgreSQL Flexible Server
// - A subnet for Container Apps Environment (minimum /23 required)
// - A subnet for private endpoints (Key Vault, etc.)
// - Private DNS Zones for PostgreSQL and Key Vault name resolution
// ============================================================================

@description('Location for the network resources')
param location string

@description('Name for the Virtual Network')
param vnetName string

@description('Address prefix for the VNet')
param vnetAddressPrefix string = '10.0.0.0/16'

@description('Subnet prefix for Container Apps Environment (minimum /23)')
param containerAppSubnetPrefix string = '10.0.0.0/23'

@description('Subnet prefix for PostgreSQL Flexible Server (delegated)')
param postgresSubnetPrefix string = '10.0.2.0/24'

@description('Subnet prefix for private endpoints')
param privateEndpointsSubnetPrefix string = '10.0.3.0/28'

@description('Tags to apply to network resources')
param tags object = {}

// ============================================================================
// Virtual Network
// ============================================================================

resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [vnetAddressPrefix]
    }
    subnets: [
      {
        name: 'postgres-subnet'
        properties: {
          addressPrefix: postgresSubnetPrefix
          delegations: [
            {
              name: 'postgresDelegation'
              properties: {
                serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers'
              }
            }
          ]
        }
      }
      {
        name: 'containerapp-subnet'
        properties: {
          addressPrefix: containerAppSubnetPrefix
          delegations: [
            {
              name: 'containerAppDelegation'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'private-endpoints-subnet'
        properties: {
          addressPrefix: privateEndpointsSubnetPrefix
        }
      }
    ]
  }
}

// ============================================================================
// Private DNS Zones
// ============================================================================
// Required for name resolution when services are accessed via private endpoints
// or VNet integration (PostgreSQL delegated subnet, Key Vault private endpoint).

resource postgresDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  tags: tags
}

resource postgresDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: postgresDnsZone
  name: '${vnetName}-pg-dns-link'
  location: 'global'
  tags: tags
  properties: {
    virtualNetwork: {
      id: vnet.id
    }
    registrationEnabled: false
  }
}

resource keyVaultDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}

resource keyVaultDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: keyVaultDnsZone
  name: '${vnetName}-kv-dns-link'
  location: 'global'
  tags: tags
  properties: {
    virtualNetwork: {
      id: vnet.id
    }
    registrationEnabled: false
  }
}

// ============================================================================
// Network Security Groups (NSGs)
// ============================================================================
// Implement least-privilege network segmentation between subnets.

resource postgresNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-postgres-subnet'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowFromContainerApp'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '5432'
          sourceAddressPrefix: containerAppSubnetPrefix
          destinationAddressPrefix: postgresSubnetPrefix
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
    ]
  }
}

resource containerAppNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-containerapp-subnet'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowHttpsFromInternet'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'Internet'
          destinationAddressPrefix: containerAppSubnetPrefix
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
    ]
  }
}

resource privateEndpointsNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-private-endpoints-subnet'
  location: location
  tags: tags
  properties: {
    securityRules: [
      {
        name: 'AllowVNetTraffic'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: 'VirtualNetwork'
          destinationAddressPrefix: 'VirtualNetwork'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
    ]
  }
}

// ============================================================================
// Outputs
// ============================================================================

@description('Resource ID of the PostgreSQL delegated subnet')
output postgresSubnetId string = vnet.properties.subnets[0].id

@description('Resource ID of the Container App subnet')
output containerAppSubnetId string = vnet.properties.subnets[1].id

@description('Resource ID of the Private DNS Zone for PostgreSQL')
output postgresDnsZoneId string = postgresDnsZone.id

@description('Resource ID of the private endpoints subnet')
output privateEndpointsSubnetId string = vnet.properties.subnets[2].id

@description('Resource ID of the Private DNS Zone for Key Vault')
output keyVaultDnsZoneId string = keyVaultDnsZone.id

@description('Virtual Network resource ID')
output vnetId string = vnet.id
