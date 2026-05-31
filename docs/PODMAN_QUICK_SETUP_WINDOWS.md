# Windows Podman (CLI) + Portainer Setup

This guide provides a lightweight, high-performance container environment for Windows. By running containers inside a minimal Podman-managed Linux VM (WSL2), this setup eliminates the overhead and commercial licensing restrictions of Docker Desktop. It is ideal for local development with .NET Aspire containers.

Both Portainer Community Edition and Podman CLI are open-source and free to use for commercial and personal purposes. For more information on using Aspire locally, refer to the [Aspire Local Dev guide](ASPIRE_LOCAL_DEV.md).

---

## Prerequisites

These are one-time, per-developer-machine steps.

### 1. WSL2

Required by the container runtime.

```powershell
wsl --install
```

Reboot after install.

### 2. Install Podman

Choose one:

**Option A — Podman CLI only** _(lightweight — no Desktop app; pair with Portainer for a GUI)_

```powershell
winget install -e --id RedHat.Podman
```

Then initialise and start the machine manually:

```powershell
podman machine init
podman machine start
```

**Option B — Podman Desktop** _(GUI included — heavier)_

Download and install from <https://podman-desktop.io/>. The first-run wizard creates a Podman machine automatically.

Verify either way:

```powershell
podman version
podman machine list
```

### 3. Tell Aspire to use Podman

```powershell
[Environment]::SetEnvironmentVariable("ASPIRE_CONTAINER_RUNTIME", "podman", "User")
```

Close and reopen Visual Studio / VS Code / Rider and any terminal you'll run `dotnet test` from. Verify:

```powershell
$env:ASPIRE_CONTAINER_RUNTIME    # should print: podman
```

---

## GUI: Portainer CE _(CLI-only install only)_

If you installed Podman CLI without Podman Desktop, run Portainer CE as a container for a web-based management UI:

```powershell
podman volume create portainer_data
podman run -d -p 9000:9000 --name portainer --restart=always `
  -v /run/user/1000/podman/podman.sock:/var/run/docker.sock `
  -v portainer_data:/data `
  portainer/portainer-ce:lts
```

Access at `http://localhost:9000` from your Windows browser.

---

## Troubleshooting

### `Container runtime 'docker' not found` (Aspire error)
`ASPIRE_CONTAINER_RUNTIME` is not set or not visible to the current process. Redo Step 3 and restart your IDE.

### Aspire complains the container runtime is unhealthy
The Podman machine isn't running.

```powershell
podman machine list     # check status
podman machine start    # start if stopped
```

### Podman machine is broken or corrupted
Recreate the machine:

```powershell
podman machine stop
podman machine rm
podman machine init
podman machine start
```

### `x509: certificate signed by unknown authority` during image pull
Corporate TLS proxies intercepting registry traffic. See:
- [Podman Desktop — Adding certificates to a Podman machine](https://podman-desktop.io/docs/podman/adding-certificates-to-a-podman-machine)
- [x509 certificate signed by unknown authority (Docker forums)](https://forums.docker.com/t/zscaler-docker-pull-and-failed-to-verify-certificate-x509-certificate-signed-by-unknown-authority/149339)

---

## Official Documentation

- [Podman Desktop](https://podman-desktop.io/)
- [.NET Aspire — Container runtime configuration](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling)
