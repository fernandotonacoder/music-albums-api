# Infrastructure as Code - Modular Bicep

This infrastructure uses a **modular Bicep architecture** for the Music Albums API. The Identity API helper tool is optional and can be deployed separately.

## Structure

```
infra/
├── main/                              # Music Albums API
│   ├── main.bicep                     # Orchestrator
│   ├── main.sample.bicepparam         # Sample parameters
│   └── modules/
│       ├── monitoring.bicep           # Log Analytics + Application Insights
│       ├── database.bicep             # PostgreSQL Flexible Server
│       ├── security.bicep             # Key Vault + Secrets
│       ├── compute.bicep              # Container Apps
│       └── network.bicep              # VNet + Private Endpoints
└── optional-identity-api-helper-tool/ # Identity API (optional)
    └── identity-api-helper-tool.bicep
```

## Modules

### monitoring.bicep
- Log Analytics Workspace
- Application Insights

### database.bicep
- PostgreSQL Flexible Server (v18)
- Private endpoint integration

### security.bicep
- Azure Key Vault
- Secrets for DB, JWT, API keys

### compute.bicep
- Container Apps Environment
- Container App with managed identity
- Auto-scaling (0-3 replicas)

### network.bicep
- Virtual Network
- Subnets (compute, database, private endpoints)
- Private DNS zones

---

## Deployment

### Music Albums API (Automatic)

Push to `main` branch triggers `azure-pipelines.yml`:

```bash
# Local deployment
cd infra/main/
az deployment group create \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam
```

### Identity API Helper Tool (Manual)

Queue `azure-pipelines-identity-api.yml` manually to deploy or destroy.

Options:
- `deployInfra`: Deploy infrastructure and image
- `destroyInfra`: Cleanup resources
- `environment`: `dev` or `prod`

---

## Validation

```bash
# Validate without deploying
az deployment group validate \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam

# Preview changes
az deployment group what-if \
  --resource-group <your-rg> \
  --template-file main.bicep \
  --parameters main.bicepparam
```

---

## Resource Naming

Resources are named following the pattern: `<baseName>-<resource>-<suffix>`

Example: `music-albums-api-dev`, `music-albums-db-dev`

All resources are tagged with:
- `application`: music-albums-api
- `environment`: dev/prod
- `managedBy`: bicep

---

## Key Vault Integration

The Container App uses a **system-assigned managed identity** to read secrets from Key Vault:

- DB connection string
- JWT signing key
- API key
- PostgreSQL credentials

RBAC assignment in `main.bicep` grants the Container App the `Key Vault Secrets User` role.

---

## Azure DevOps Setup

Create variable groups in Azure DevOps:

**Group: `music-albums-dev`**
- `RESOURCE_GROUP` (example: `music-albums-rg-dev`)
- `LOCATION` (optional, defaults to `westeurope`)
- `BASE_NAME` (example: `music-albums`)
- `aspNetCoreEnvironment` (`Development`)
- `pg-admin-login` (secret)
- `pg-admin-password` (secret)
- `jwt-key` (secret, min 32 chars)
- `api-key` (secret)

**Group: `music-albums-prod`**
Same structure with `prod` suffix and `Production` environment.

---

**For more details:** See `infra/main/main.bicep` and individual module files.
