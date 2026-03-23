// ============================================================================
// Entra ID Administrator - PostgreSQL Flexible Server
// ============================================================================
// Registers a managed identity as an Entra ID administrator on the
// PostgreSQL server, enabling passwordless (token-based) authentication.
//
// This is a separate module because the administrator resource name must be
// the principal's object ID, which is a runtime value from the Container App.
// ============================================================================

@description('Name of the PostgreSQL Flexible Server')
param postgresServerName string

@description('Object ID of the managed identity to register as Entra admin')
param principalId string

@description('Display name of the managed identity (typically the Container App name)')
param principalName string

@description('Azure AD tenant ID')
param tenantId string

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' existing = {
  name: postgresServerName
}

resource postgresEntraAdmin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2023-12-01-preview' = {
  parent: postgresServer
  name: principalId
  properties: {
    principalName: principalName
    principalType: 'ServicePrincipal'
    tenantId: tenantId
  }
}
