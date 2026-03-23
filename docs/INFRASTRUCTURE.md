# Infrastructure

Modular Bicep architecture for the Music Albums API. The Identity API helper tool is optional and deployed separately.

## Structure

```
infra/
├── main/
│   ├── main.bicep                     # Orchestrator
│   ├── main.sample.bicepparam         # Sample parameters
│   └── modules/
│       ├── network.bicep              # VNet, Subnets, NSGs, Private DNS (prod only)
│       ├── monitoring.bicep           # Log Analytics + Application Insights
│       ├── database.bicep             # PostgreSQL Flexible Server
│       ├── security.bicep             # Key Vault + Secrets
│       ├── compute.bicep              # Container Apps Environment + App
│       └── entra-admin.bicep          # Entra ID admin for PostgreSQL
└── optional-identity-api-helper-tool/
    └── optional-identity-api-helper-tool.bicep
```

## Dev vs Prod

| Concern | Dev | Prod |
|---|---|---|
| **Subscription** | Azure for Students | VS Professional ($50/month) |
| **Service Connection** | `azure-service-connection` | `azure-service-connection-prod` |
| **Network** | No VNet, no private endpoints | Full VNet with subnets, NSGs, private DNS |
| **PostgreSQL** | Public access | VNet-integrated (delegated subnet), no public access |
| **PostgreSQL auth** | Entra ID + password (both enabled) | Entra ID only (password disabled) |
| **DB credentials in Key Vault** | Yes (for pgAdmin/troubleshooting) | No |
| **Key Vault** | Public access, no private endpoint | Private endpoint only, public access disabled |
| **Container App** | Azure-managed networking | VNet-integrated |
| **Log retention** | 30 days | 90 days |
| **Purge protection** | Disabled (omitted) | Disabled (omitted) |

## Modules

### network.bicep (prod only)
- Virtual Network with 3 subnets (Container Apps, PostgreSQL, Private Endpoints)
- 3 NSGs with least-privilege rules
- Private DNS Zones for PostgreSQL and Key Vault
- In dev: no resources created, outputs return empty strings

### monitoring.bicep
- Log Analytics Workspace
- Application Insights

### database.bicep
- PostgreSQL Flexible Server (v18)
- Microsoft Entra ID authentication enabled (passwordless)
- VNet-integrated in prod, public access in dev
- Password auth enabled in dev (for pgAdmin), disabled in prod

### entra-admin.bicep
- Registers the Container App's managed identity as a PostgreSQL Entra ID administrator
- Enables the app to authenticate with tokens from `DefaultAzureCredential` instead of a password

### security.bicep
- Azure Key Vault (RBAC-based access)
- Secrets: JWT key, API key (both environments), DB connection string + PostgreSQL credentials (dev only, for pgAdmin)
- Private endpoint in prod only

### compute.bicep
- Container Apps Environment
- Container App with system-assigned managed identity
- Auto-scaling (0–3 replicas)
- Health probes (startup + liveness)
- Key Vault secret references via managed identity (JWT key, API key)
- Passwordless database connection string (plain env var, no credentials)

## Resource Groups

Each environment uses a single resource group containing all resources (main API, Identity API, database, networking, etc.):

| Environment | Resource Group | Subscription |
|---|---|---|
| Dev | `music-albums-rg-dev` | Azure for Students |
| Prod | `music-albums-rg-prod` | VS Professional |

## Orchestration

```
Network (prod only)
       ↓
Monitoring + Database (parallel, depend on Network outputs)
       ↓
Security (uses Database connection string — dev only secrets)
       ↓
Compute (uses Monitoring + Security outputs)
       ↓
RBAC (grants Container App → Key Vault Secrets User)
       ↓
Entra Admin (registers Container App identity as PostgreSQL Entra ID admin)
```

## Passwordless Database Authentication (Entra ID)

The app connects to PostgreSQL using Microsoft Entra ID tokens instead of a password. The Container App's system-assigned managed identity is registered as an Entra ID administrator on the PostgreSQL server, and the app uses `DefaultAzureCredential` to fetch short-lived tokens (refreshed every 55 minutes).

### Manual prerequisite (both dev and prod)

The PostgreSQL server's system-assigned managed identity must have the **Directory Readers** role in Microsoft Entra ID. This allows the server to validate Entra ID tokens. **Bicep cannot automate this** — it requires Azure AD tenant-level permissions.

For each environment:

1. Go to **Microsoft Entra ID** > **Roles and administrators** in the Azure portal
2. Find the **Directory Readers** role
3. Click **Add assignments**
4. Add the PostgreSQL server's managed identity:
   - Dev: `music-albums-db-dev`
   - Prod: `music-albums-db-prod`

Without this, the PostgreSQL server cannot validate Entra ID tokens and the app will fail to connect.

### How local dev works

Local development uses docker-compose with a plain PostgreSQL container and password auth. The connection string in user-secrets includes a `Password` field, so the Entra ID token provider is automatically skipped (see `ApplicationServiceCollectionExtensions.AddDatabase()`).

## Resource Naming

Pattern: `{baseName}-{resource}-{suffix}`

Example with `baseName=music-albums`, `suffix=dev`:

| Resource | Name |
|---|---|
| Container App | `music-albums-api-dev` |
| PostgreSQL | `music-albums-db-dev` |
| Key Vault | `music-albums-kv-dev` |
| App Insights | `music-albums-insights-dev` |
| Log Analytics | `music-albums-logs-dev` |
| VNet (prod) | `music-albums-vnet-prod` |

All resources tagged: `application=music-albums-api`, `environment=dev|prod`, `managedBy=bicep`.

## Pipelines

### Music Albums API — `.azure-pipelines/main-ci-cd.yml`

Triggers on push to `main` (when `src/`, `Dockerfile`, or `infra/main/` change).

Parameters:
- `targetEnvironment`: `dev` | `prod` (default: `dev`)
- `deployInfra`: `true` | `false` (default: `false`)

Stages: Build → Preview Infrastructure (What-If) → Deploy Infrastructure → Deploy Application

### Identity API — `.azure-pipelines/optional-identity-api.yml`

Manual queue only. Use to deploy a temporary JWT token generator for remote testing.

Both pipelines select the service connection automatically based on the target environment:
- `dev` → `azure-service-connection` (Azure for Students)
- `prod` → `azure-service-connection-prod` (VS Professional)

Parameters:
- `deployInfra` / `destroyInfra`: deploy or cleanup
- `environment`: `dev` | `prod`

## Azure DevOps Variable Groups

Create two variable groups: `music-albums-dev` and `music-albums-prod`.

### Main API Variables

| Variable | Example | Secret? |
|---|---|---|
| `RESOURCE_GROUP` | `music-albums-rg-dev` | No |
| `BASE_NAME` | `music-albums` | No |
| `LOCATION` | `swedencentral` | No |
| `aspNetCoreEnvironment` | `Development` | No |
| `pg-admin-login` | — | Yes (server creation only, not used at runtime) |
| `pg-admin-password` | — | Yes (server creation only, not used at runtime) |
| `jwt-key` (min 32 chars) | — | Yes |
| `api-key` | — | Yes |
| `GITHUB_TOKEN` | — | Yes |

> `GITHUB_TOKEN` is a GitHub Fine-grained PAT scoped to the repo with **Deployments: Read and write** permission. Used to create GitHub Deployments with environment URLs.

### Identity API

The Identity API pipeline also reads from `music-albums-dev` / `music-albums-prod` but only uses the shared variables (`RESOURCE_GROUP`, `BASE_NAME`, `LOCATION`, `GITHUB_TOKEN`, etc.). It derives its resource names from `BASE_NAME` (e.g. `id-api-music-albums-dev`) and deploys into the same resource group and Container App Environment as the main API.

## GitHub Actions

A scheduled GitHub Actions workflow (`.github/workflows/cleanup-ghcr.yml`) runs weekly to clean up old container images from GHCR. It keeps the 10 most recent versions of each package (`music-albums-api` and `identity-api`) and deletes the rest. Can also be triggered manually via **Actions → Cleanup GHCR → Run workflow**.

Requires both packages to have **Admin** role assigned to the repo under **Package Settings → Manage Actions access**.

## Local Deployment

```bash
# Copy sample params and fill in values
cp infra/main/main.sample.bicepparam infra/main/main.bicepparam

# Validate
az deployment group validate \
  --resource-group <your-rg> \
  --template-file infra/main/main.bicep \
  --parameters infra/main/main.bicepparam

# Preview
az deployment group what-if \
  --resource-group <your-rg> \
  --template-file infra/main/main.bicep \
  --parameters infra/main/main.bicepparam

# Deploy
az deployment group create \
  --resource-group <your-rg> \
  --template-file infra/main/main.bicep \
  --parameters infra/main/main.bicepparam
```

> Never commit `main.bicepparam` — it contains secrets.
