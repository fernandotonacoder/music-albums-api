// ============================================================================
// Network Module - VNet, Subnets, and Private DNS
// ============================================================================
// Creates the Virtual Network with:
// - A delegated subnet for PostgreSQL Flexible Server
// - A subnet for Container Apps Environment (minimum /23 required)
// - A Private DNS Zone for PostgreSQL name resolution within the VNet
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
        }
      }
    ]
  }
}

// ============================================================================
// Private DNS Zone for PostgreSQL
// ============================================================================
// Required for name resolution when PostgreSQL Flexible Server is deployed
// into a delegated subnet (VNet integration).

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

// ============================================================================
// Outputs
// ============================================================================

@description('Resource ID of the PostgreSQL delegated subnet')
output postgresSubnetId string = vnet.properties.subnets[0].id

@description('Resource ID of the Container App subnet')
output containerAppSubnetId string = vnet.properties.subnets[1].id

@description('Resource ID of the Private DNS Zone for PostgreSQL')
output postgresDnsZoneId string = postgresDnsZone.id

@description('Virtual Network resource ID')
output vnetId string = vnet.id
