# Linux Ubuntu Docker Engine (CLI) + Portainer Setup

This guide provides a lightweight, high-performance Docker environment for Linux. By running containers natively on the Linux kernel, this setup eliminates the overhead of Docker Desktop. It is ideal for maximizing hardware performance on development machines running .NET Aspire containers. 

Both Portainer Community Edition and Docker Engine CLI are open-source and free to use for commercial and personal purposes. For more information on using Aspire locally, refer to the [Aspire Local Dev guide](ASPIRE_LOCAL_DEV.md).

---

## Quick Installation

### Step 1: Install Docker Engine (Natively)

Before installing Docker Engine, you need to uninstall any conflicting packages to ensure a clean installation. Run the following command:

```bash
sudo apt remove $(dpkg --get-selections docker.io docker-compose docker-compose-v2 docker-doc podman-docker containerd runc | cut -f1)
```

Now, run these commands to set up prerequisites and install the official Docker daemon:

```bash
# Update package index and install required system tools
sudo apt-get update
sudo apt-get install ca-certificates curl gnupg


# Add Docker's official GPG key
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

# Add the repository to Apt sources
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine components
sudo apt-get update
sudo apt-get install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

### Step 2: Manage Docker as a Non-Root User (No sudo)
Configure permissions so your IDEs and terminal can execute Docker commands seamlessly:

```bash
# Create the docker group (if it doesn't exist) and add your user
sudo groupadd docker
sudo usermod -aG docker $USER

# Enable Docker to start automatically on system boot
sudo systemctl enable docker.service
sudo systemctl enable containerd.service
```

**CRITICAL:** You must log out and log back in (or restart your PC) for these permission changes to take effect globally across all GUI applications (VS Code, Rider).

### Step 3: Install Portainer CE (Free for Commercial Use)
Spin up the lightweight Portainer web dashboard management console:

```bash
# Create a persistent volume for configurations
docker volume create portainer_data

# Run the Portainer container (Exposing HTTP Port 9000)
docker run -d -p 9000:9000 --name portainer --restart=always -v /var/run/docker.sock:/var/run/docker.sock -v portainer_data:/data portainer/portainer-ce:lts
```

Access the web dashboard at: `http://localhost:9000`

## Troubleshooting

### 1. Error: docker-credential-desktop: executable file not found
**Why it happens:** Artifact configurations left behind from a previous Docker Desktop installation.

**Fix:** Open `~/.docker/config.json`, find and delete the line `"credsStore": "desktop",`. Make sure to remove the trailing comma from the line immediately above it to keep the JSON syntax valid.

### 2. Error: Permission denied while trying to connect (in IDEs or Extensions)
**Why it happens:** The IDE (VS Code / Rider) was launched before the operating system refreshed your new docker user group privileges.

**Fix:** Close the IDE completely, log out of your Ubuntu session, log back in, and reopen the project. Alternatively, force-launch the IDE from a terminal instance where permissions are already active using `code .`

### 3. .NET Aspire: Container runtime 'docker' appears to be unhealthy
**Why it happens:** .NET Aspire's Orchestrator (DCP) evaluates the daemon health on startup and might get stuck searching for outdated Docker Desktop contexts or encounter socket constraints.

**Fix 1:** Permanently remove the obsolete desktop context configuration:

```bash
docker context rm desktop-linux
```

**Fix 2:** Ensure the socket file group permissions are fully set:

```bash
sudo chmod 660 /var/run/docker.sock
sudo chown root:docker /var/run/docker.sock
```

**Fix 3:** Force environment variables directly into your execution command context or save them inside your `~/.bashrc`:

```bash
DOCKER_HOST="unix:///var/run/docker.sock" DOCKER_SOCK="unix:///var/run/docker.sock" dotnet test
```

### 4. Forgot Portainer Admin Password
**Why it happens:** Credentials were lost or not saved during initial configuration.

**Fix:** Stop the container, execute the official password reset helper image, and restart the instance:

```bash
docker stop portainer
docker run --rm -v portainer_data:/data portainer/helper-reset-password
# Copy the temporary password string generated in the terminal output
docker start portainer
```

## Official Documentation & Alternative Operating Systems
* [Official Docker Engine Installation Guide for Ubuntu](https://docs.docker.com/engine/install/ubuntu/)
* [Docker Engine Installation Platform Overview (Debian, CentOS, etc.)](https://docs.docker.com/engine/install/)
* [Portainer Community Edition (CE) Deployment Guide for Linux](https://docs.portainer.io/start/install-ce/server/docker/linux)
