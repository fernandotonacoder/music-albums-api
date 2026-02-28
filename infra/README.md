# Infrastructure as Code - Modular Bicep Architecture

## Overview

This infrastructure is organized using a **modular Bicep architecture** following IaC best practices. The Music Albums API infrastructure is decomposed into specialized, reusable modules. The Identity API helper tool is optional and can be deployed separately on-demand.

## Directory Structure

```
infra/
├── main/                                # Music Albums API Infrastructure
│   ├── main.bicep                       # Orchestrator
│   ├── main.sample.bicepparam           # Sample parameters
│   └── modules/                         # Reusable modules
│       ├── monitoring.bicep             # Log Analytics & Application Insights
│       ├── database.bicep               # PostgreSQL Flexible Server
│       ├── security.bicep               # Key Vault & Secrets
│       ├── compute.bicep                # Container Apps Environment & App
├── optional-identity-api-helper-tool/   # (optional) Identity API Dev Helper Tool
│   └── identity-api-helper-tool.bicep   # Simple Container App for JWT generation
└── README.md                            # This file
```

**Root-level pipeline files:**
- `azure-pipelines.yml` - Music Albums API (automatic on push to main)
- `azure-pipelines-identity-api.yml` - Identity API helper tool (manual queue)

---

## Music Albums API Modules

### 🔍 monitoring.bicep
**Purpose:** Centralized logging and application monitoring

**Resources:**
- Log Analytics Workspace (logs aggregation)
- Application Insights (APM and telemetry)

**Outputs:**
- `logAnalyticsWorkspaceId`
- `logAnalyticsCustomerId`
- `logAnalyticsPrimarySharedKey` (secure)
- `appInsightsConnectionString`
- `appInsightsInstrumentationKey`

---

### 🗄️ database.bicep
**Purpose:** Managed PostgreSQL database infrastructure

**Resources:**
- PostgreSQL Flexible Server (v18)
- Firewall rules (Azure services access)

**Parameters:**
- SKU configuration (tier, size)
- Admin credentials
- Backup retention settings
- Storage size

**Outputs:**
- `postgresServerId`
- `postgresServerFqdn`
- `connectionString` (secure)

---

### 🔐 security.bicep
**Purpose:** Secrets management and secure configuration

**Resources:**
- Azure Key Vault
- Secrets (DB connection, JWT keys, API keys, PostgreSQL credentials)

**Note:** RBAC role assignments are handled in the orchestrator (`main.bicep`) to avoid circular dependencies between modules.

**Outputs:**
- `keyVaultId`
- `keyVaultUri`
- `keyVaultName`
- Secret URIs for all stored secrets

---

### 🚀 compute.bicep
**Purpose:** Container runtime and application hosting

**Resources:**
- Container Apps Environment
- Container App (with health probes, scaling rules)

**Key Features:**
- System-assigned managed identity
- Health checks (startup, liveness)
- Auto-scaling (0-3 replicas)
- Key Vault secret references
- Application Insights integration

**Outputs:**
- `containerAppUrl`
- `containerAppPrincipalId` (for RBAC)
- `containerAppFqdn`

---

## 🔑 Identity API Helper Tool

**Location:** `optional-identity-api-helper-tool/identity-api-helper-tool.bicep`

**Purpose:** Optional deployment of the Identity API service as a Container App for manual JWT token generation when testing remotely.

**Resources:**
- Container Apps Environment
- Container App (running Identity API with JWT token endpoint)

**Note:** This is a **dev helper tool only**. Deploy manually when needed, then destroy to save costs. No persistent data storage required.

**Parameters:**
- `jwtSecret` - JWT signing key (uses existing `JWT_KEY` from ADO Variable Group)
- `containerImage` - Container image URI
- `aspNetCoreEnvironment` - .NET environment (Development/Staging/Production)
- `deploymentSuffix` - Deployment identifier (dev/prod)
- `location` - Azure region

**Pipeline:** `azure-pipelines-identity-api.yml`
- Deploy: Builds image, deploys Container App to Azure
- Destroy: Removes Container App and Environment (cleanup)

**Typical Usage:**
```bash
# 1. Queue pipeline with action=deploy
# 2. Get the Identity API URL from pipeline output
# 3. Use the URL to generate JWT tokens for testing
# 4. When done, queue pipeline with action=destroy
```

---

## Orchestration Flow

The `main/main.bicep` file coordinates module deployment with proper dependency management:

```
Monitoring + Database (parallel)
         ↓
      Security (uses Database connection string)
         ↓
      Compute (uses Monitoring logs + Key Vault secrets)
         ↓
      RBAC (assigns Compute identity → Key Vault access)
```

### Deployment Order

1. **Monitoring & Database** (parallel) - Independent resources
2. **Security** - Depends on database connection string
3. **Compute** - Depends on monitoring logs and Key Vault secret URIs
4. **RBAC** - Grants Container App identity access to Key Vault

---

## Benefits of Modularization

✅ **Maintainability:** Each module is focused on a single concern  
✅ **Reusability:** Modules can be imported in different projects  
✅ **Testability:** Test individual modules independently  
✅ **Scalability:** Easy to extend with new modules  
✅ **Team Collaboration:** Different team members can work on separate modules  
✅ **Version Control:** Better diff tracking and code review  
✅ **Standardization:** Consistent patterns, naming, and tagging across resources  

---

## Deployment

### Music Albums API

**Automatic deployment via CI/CD:**
- Push to main branch → `azure-pipelines.yml` runs automatically
- Deploys/updates database, monitoring, security, and compute resources

**Local deployment:**
```bash
cd main/
az deployment group create \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam
```

### Identity API Helper Tool

**Manual deployment via pipeline:**
1. Queue `azure-pipelines-identity-api.yml` pipeline manually
2. Select action: `deploy` or `destroy`
3. Select environment: `dev` or `prod`
4. Pipeline runs build → push → deploy/destroy

**Local deployment:**
```bash
cd optional-identity-api-helper-tool/
az deployment group create \
  --resource-group <your-rg> \
  --template-file identity-api-helper-tool.bicep \
  --parameters \
    location=westeurope \
    deploymentSuffix=dev \
    containerImage='ghcr.io/your-org/identity-api:latest' \
    jwtSecret='your-jwt-key'
```

---

## Resource Tagging

All Music Albums API modules support consistent tagging via the `tags` parameter. The orchestrator applies these common tags:

```bicep
var commonTags = {
  application: 'music-albums-api'
  environment: deploymentSuffix    // 'dev' or 'prod'
  managedBy: 'bicep'
}
```

---

## Validation

To validate the bicep files without deploying:

```bash
# Validate Music Albums API
cd main/
az deployment group validate \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam

# Preview changes (what-if)
az deployment group what-if \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam

# Validate Identity API Helper Tool
cd ../optional-identity-api-helper-tool/
az deployment group validate \
  --resource-group <your-rg> \
  --template-file identity-api-helper-tool.bicep
```

---

## Module Development Guidelines

When creating or modifying modules:

1. **Parameters:** Clearly document all parameters with `@description()`
2. **Defaults:** Provide sensible defaults where appropriate
3. **Outputs:** Export all IDs, URIs, and connection strings needed by other modules
4. **Security:** Mark sensitive outputs with `@secure()`
5. **Naming:** Use consistent resource naming patterns
6. **Tags:** Always accept and apply a `tags` parameter
7. **API Versions:** Use stable, recent API versions
8. **Comments:** Add section headers for clarity

---

## Troubleshooting

### Circular Dependencies
If you encounter circular dependency issues between modules, consider:
- Moving RBAC assignments to the orchestrator
- Using conditional deployments
- Splitting modules further

### Secret Outputs
Bicep linter will warn about secrets in outputs. Mark them with `@secure()` to suppress warnings.

### Container App Won't Start
- Check logs: Azure Portal → Container App → Logs
- Verify image exists and is accessible
- Ensure Key Vault secrets are properly referenced
- Check RBAC assignments are correct

### Module Not Found
Ensure module paths are correct relative to orchestrator:
```bicep
module database './modules/database.bicep' = { ... }
```

---

## Future Enhancements

Consider adding these modules as the project grows:

- **network.bicep** - VNet, subnets, NSGs, Private Endpoints
- **cdn.bicep** - Azure Front Door or CDN profiles
- **alerts.bicep** - Azure Monitor alert rules and action groups
- **backup.bicep** - Backup policies and recovery vaults

---

**Last Updated:** February 28, 2026  
**Maintained By:** Infrastructure Team  
**Questions?** Refer to [Azure Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
