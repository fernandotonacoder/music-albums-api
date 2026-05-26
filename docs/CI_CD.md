# CI/CD

Pipeline configuration for the Music Albums API. Two Azure Pipelines handle deployment, plus a scheduled GitHub Actions workflow for image cleanup. The infrastructure they deploy is documented separately in [Infrastructure](INFRASTRUCTURE.md).

## Pipelines

### Music Albums API — `.azure-pipelines/main-ci-cd.yml`

Triggers on push to `main` (when `src/`, `Dockerfile`, or `infra/main/` change).

Parameters:

- `targetEnvironment`: `dev` | `prod` (default: `dev`)
- `deployInfra`: `true` | `false` (default: `false`) — deploy or update infrastructure via Bicep
- `destroyInfra`: `true` | `false` (default: `false`) — manual only, deletes the entire resource group. Useful for cost savings when the environment is no longer needed; re-deploy from scratch with `deployInfra=true`

Stages: Build → Preview Infrastructure (What-If) → Deploy Infrastructure → Deploy Application

### Identity API — `.azure-pipelines/optional-identity-api.yml`

Manual queue only. Use to deploy a temporary JWT token generator for remote testing.

Parameters:

- `deployInfra` / `destroyInfra`: deploy or cleanup
- `environment`: `dev` | `prod`

Both pipelines select the Azure service connection automatically based on the target environment (see [Service Connections](#azure-devops-service-connections) below).

## Manual Deployment from local Azure CLI

Alternative to the automated pipeline — useful for testing Bicep changes or pushing a quick fix without queueing a full pipeline run. Refer to [Infrastructure](INFRASTRUCTURE.md) for what the Bicep templates create.

### Prerequisites

1. Install [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) and [Docker](https://docs.docker.com/get-docker/) (only needed for app deploys).

2. Log in and select the target subscription. Confirm with `az account show` before running anything destructive:

   ```bash
   az login
   az account set --subscription "Azure for Students"   # or "VS Professional" for prod
   az account show --query "{name:name, id:id}" -o table
   ```

### Infrastructure

For changes to Bicep (`infra/main/`).

1. Create the resource group if it doesn't exist yet (the deployment commands below deploy *into* an existing RG):

   ```bash
   # Dev
   az group create \
     --name music-albums-rg-dev \
     --location swedencentral \
     --tags application=music-albums-api environment=dev managedBy=bicep

   # Prod
   az group create \
     --name music-albums-rg-prod \
     --location swedencentral \
     --tags application=music-albums-api environment=prod managedBy=bicep
   ```

   > The Bicep modules tag every resource inside the RG, but RG-level tags are set here (Bicep can't tag its own enclosing RG from inside a `group`-scope deployment).

2. Copy the sample parameter file and fill in your values:

   ```bash
   cp infra/main/main.sample.bicepparam infra/main/main.bicepparam
   ```

   > Never commit `main.bicepparam` — it contains secrets.

3. Validate, preview, and deploy. Replace `<your-rg>` with `music-albums-rg-dev` or `music-albums-rg-prod`:

   ```bash
   # Validate (syntax + ARM template generation)
   az deployment group validate \
     --resource-group <your-rg> \
     --template-file infra/main/main.bicep \
     --parameters infra/main/main.bicepparam

   # Preview (what-if — shows what would change without applying)
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

### Application

For pushing a new image of either API. The Bicep above already created the Container Apps; this just points them at a new image tag.

1. Log in to GHCR with a GitHub PAT that has `write:packages`:

   ```bash
   echo $GHCR_PAT | docker login ghcr.io -u fernandotonacoder --password-stdin
   ```

2. Build and push the image. Pick **one** of the two APIs:

   **Music Albums API** (Dockerfile at repo root):

   ```bash
   TAG=manual-$(date +%Y%m%d-%H%M%S)
   docker build -t ghcr.io/fernandotonacoder/music-albums-api:$TAG .
   docker push ghcr.io/fernandotonacoder/music-albums-api:$TAG
   ```

   **Identity API** (Dockerfile at `tools/Identity.Api/Dockerfile`, build context is the repo root):

   ```bash
   TAG=manual-$(date +%Y%m%d-%H%M%S)
   docker build -t ghcr.io/fernandotonacoder/identity-api:$TAG -f tools/Identity.Api/Dockerfile .
   docker push ghcr.io/fernandotonacoder/identity-api:$TAG
   ```

3. Point the Container App at the new image. Replace `<env>` with `dev` or `prod`:

   **Music Albums API:**

   ```bash
   az containerapp update \
     --name music-albums-api-<env> \
     --resource-group music-albums-rg-<env> \
     --image ghcr.io/fernandotonacoder/music-albums-api:$TAG
   ```

   **Identity API:**

   ```bash
   az containerapp update \
     --name id-api-music-albums-<env> \
     --resource-group music-albums-rg-<env> \
     --image ghcr.io/fernandotonacoder/identity-api:$TAG
   ```

   > Use a unique tag every time (e.g. timestamp) rather than reusing `latest` — otherwise Container Apps may not detect a new revision and won't roll out.

## Azure DevOps Service Connections

Configure under **Project Settings → Service connections**. All five are required for the pipelines to run end-to-end:

| Name                          | Type                   | Purpose                                                                                                    |
| ----------------------------- | ---------------------- | ---------------------------------------------------------------------------------------------------------- |
| `azure-service-connection`      | Azure Resource Manager | Deploys to the **dev** subscription (Azure for Students). Selected when `targetEnvironment=dev`.           |
| `azure-service-connection-prod` | Azure Resource Manager | Deploys to the **prod** subscription (VS Professional). Selected when `targetEnvironment=prod`.            |
| `github-ghcr`                 | Docker Registry        | Pushes container images to GitHub Container Registry (`ghcr.io/fernandotonacoder/{music-albums-api,identity-api}`). Uses a GitHub PAT with `write:packages`. |
| `sonarqube-ft`                | SonarQube Server self-hosted on Azure     | Used by the `SonarQubePrepare` / `SonarQubeAnalyze` tasks in the Build stage for code quality + coverage analysis. |
| `fernandotonacoder`           | GitHub                 | Source repository connection — lets Azure DevOps pull from `github.com/fernandotonacoder/music-albums-api`. Distinct from the `GITHUB_TOKEN` variable, which is used at runtime for the Deployments API. |

> The Azure service connections (`azure-service-connection` / `-prod`) must be granted **Contributor** at the subscription scope so Bicep can create/update resource groups and Container Apps.

## Azure DevOps Variable Groups

Create two variable groups: `music-albums-dev` and `music-albums-prod`.

### Main API Variables

| Variable                 | Example               | Secret?                                         |
| ------------------------ | --------------------- | ----------------------------------------------- |
| `RESOURCE_GROUP`         | `music-albums-rg-dev` | No                                              |
| `BASE_NAME`              | `music-albums`        | No                                              |
| `LOCATION`               | `swedencentral`       | No                                              |
| `aspNetCoreEnvironment`  | `Development`         | No                                              |
| `pg-admin-login`         | —                     | Yes (server creation only, not used at runtime) |
| `pg-admin-password`      | —                     | Yes (server creation only, not used at runtime) |
| `jwt-key` (min 32 chars) | —                     | Yes                                             |
| `api-key`                | —                     | Yes                                             |
| `GITHUB_TOKEN`           | —                     | Yes                                             |

> **`GITHUB_TOKEN`** — Mirrors Azure DevOps deployments into GitHub's native Deployments feature (Environments tab, PR/commit markers, "View deployment" link to the live Container App). GitHub **Fine-grained PAT** scoped to this repo with **Deployments: Read and write** — nothing else.

### Identity API

The Identity API pipeline also reads from `music-albums-dev` / `music-albums-prod` but only uses the shared variables (`RESOURCE_GROUP`, `BASE_NAME`, `LOCATION`, `GITHUB_TOKEN`, etc.). It derives its resource names from `BASE_NAME` (e.g. `id-api-music-albums-dev`) and deploys into the same resource group and Container App Environment as the main API.

## GitHub Actions

A scheduled GitHub Actions workflow (`.github/workflows/cleanup-ghcr.yml`) runs weekly to clean up old container images from GHCR. It keeps the 10 most recent versions of each package (`music-albums-api` and `identity-api`) and deletes the rest. Can also be triggered manually via **Actions → Cleanup GHCR → Run workflow**.

Requires both packages to have **Admin** role assigned to the repo under **Package Settings → Manage Actions access**.
