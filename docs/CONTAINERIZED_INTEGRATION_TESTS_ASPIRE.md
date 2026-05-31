# Containerized Integration Tests (Aspire)

This project features integration tests that boot a slimmed-down version of the full Aspire AppHost (`MusicAlbums_AppHost`) via `DistributedApplicationTestingBuilder`. This automates bringing up local containers (like PostgreSQL) to run end-to-end tests against real infrastructure logic.

## 🚀 Running the tests

From **Visual Studio / Rider**: Open the Test Explorer and run anything under `MusicAlbums.Tests.Integration`.

From the **CLI**:

```bash
dotnet test tests/MusicAlbums.Tests.Integration/MusicAlbums.Tests.Integration.csproj
```

## 🏗️ How it works

Tests use a single instance of the application across the entire test run, managed by `DistributedAppFixture` (which implements `IAsyncLifetime`).

1. **Bootstraps the AppHost:** Calls `DistributedApplicationTestingBuilder.CreateAsync` with `--TestMode=true` to build the app and boot the orchestrated containers (PostgreSQL).
2. **Health Checks:** Uses Aspire's `WaitForResourceHealthyAsync` to pause execution until the API and Identity APIs are fully responding.
3. **HTTP Clients:** Automatically creates dedicated, pre-configured `HttpClient` instances bounded to those services (`MusicAlbumsApiClient` and `IdentityApiClient`).
4. **Database Reset (Respawner):** Resolves the PostgreSQL connection string and resets the schema between runs via [Respawner](https://github.com/jbogard/Respawn) to ensure isolated test phases without having to restart the Postgres container itself (which would be much slower).

---

## 🔒 Corporate Networks & Zscaler TLS Issues (Troubleshooting)

If you are developing inside a corporate network, proxies like **Zscaler** often intercept TLS to external registries like `mcr.microsoft.com` or `docker.io`. 

The underlying Windows/macOS host often trusts the corporate CA, but the embedded container VMs (WSL2, Podman machine, etc.) do not. Every attempt by Aspire to pull images during tests fails with: `x509: certificate signed by unknown authority`.

Choose **one** of the options below to fix it for your Container Runtime:

### Option 1 — Mark registries as insecure _(Recommended)_
This configures the daemon to skip TLS verification for specific registries (which Aspire pulls from constantly).

**For Podman Desktop (inside the Podman VM):**
```powershell
podman machine ssh "sudo mkdir -p /etc/containers/registries.conf.d"

@'
[[registry]]
location = "mcr.microsoft.com"
insecure = true

[[registry]]
location = "docker.io"
insecure = true
'@ | podman machine ssh "sudo tee /etc/containers/registries.conf.d/corporate-insecure.conf > /dev/null"

# Restart Podman
podman machine stop
podman machine start
```

**For Docker Engine (WSL2 or Linux Desktop):**
Edit `/etc/docker/daemon.json` to include `"insecure-registries"`:
```json
{
  "insecure-registries" : ["mcr.microsoft.com", "docker.io"]
}
```
Then run `sudo systemctl restart docker`.

### Option 2 — Trust Zscaler's CA in the Podman VM 
*(Use this if using Podman on Windows and you want a fully secure authenticated pipeline by syncing Windows certificates)*

Imports your machine's Zscaler certificates into the Podman VM so all future pulls just work. Run from an elevated PowerShell:

```powershell
$outDir = "C:\zscaler-certs"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
Get-ChildItem Cert:\LocalMachine\Root, Cert:\LocalMachine\CA |
  Where-Object { $_.Subject -like "*Zscaler*" } |
  Sort-Object Thumbprint -Unique |
  ForEach-Object {
    $path = Join-Path $outDir "zscaler-$($_.Thumbprint).crt"
    $pem = "-----BEGIN CERTIFICATE-----`n" +
           [Convert]::ToBase64String($_.RawData, 'InsertLineBreaks') +
           "`n-----END CERTIFICATE-----"
    Set-Content -Path $path -Value $pem
  }

podman machine ssh "sudo mkdir -p /etc/pki/ca-trust/source/anchors"
Get-ChildItem C:\zscaler-certs\*.crt | ForEach-Object {
    Get-Content $_.FullName | podman machine ssh "sudo tee /etc/pki/ca-trust/source/anchors/$($_.Name) > /dev/null"
}
podman machine ssh "sudo update-ca-trust"
podman machine stop
podman machine start
```

### Option 3 — Pull images manually _(Manual override)_
Aspire tests usually pull explicitly pinned tags. You can bypass the runtime proxy errors by deliberately pulling them with `--tls-verify=false`:

```bash
docker pull --tls-verify=false mcr.microsoft.com/dotnet/aspire/dashboard:latest
docker pull --tls-verify=false postgres:16-alpine
```

*(You will need to manually re-run this every time the project updates the container tag versions).*