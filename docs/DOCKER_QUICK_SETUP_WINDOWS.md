# Windows Docker Engine (CLI) + Portainer Setup via WSL2

This guide provides a high-performance Windows Docker environment. By running Docker Engine natively inside a Linux WSL2 distribution and exposing it to Windows, this setup eliminates the heavyweight resource usage and commercial licensing restrictions of Docker Desktop. It is ideal for local development with .NET Aspire containers.

---

## 🚀 Installation Guide

### Step 1: Install WSL2 and Ubuntu
Open **PowerShell as Administrator** and install Windows Subsystem for Linux (WSL) along with Ubuntu:

```powershell
wsl --install
```

> **Note:** If WSL is already installed but you need Ubuntu, run `wsl --install -d Ubuntu`.

Reboot your machine if prompted. Once rebooted, launch **Ubuntu** from your Start menu and complete the initial UNIX user creation.

### Step 2: Enable Systemd in Ubuntu
The official Docker daemon requires `systemd`. Inside your Ubuntu terminal, run:

```bash
sudo tee /etc/wsl.conf > /dev/null <<EOF
[boot]
systemd=true
EOF
```

Restart WSL completely to apply the changes. Open a **PowerShell** window and run:
```powershell
wsl --shutdown
```

Reopen your **Ubuntu** terminal to continue.

### Step 3: Install Docker Engine Natively inside Ubuntu
Run the following commands in your **Ubuntu** terminal to install Docker Engine:

```bash
# Clean up conflicting packages and install prerequisites
sudo apt remove $(dpkg --get-selections docker.io docker-compose docker-compose-v2 docker-doc podman-docker containerd runc | cut -f1)
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

# Add your user to the docker group
sudo groupadd docker
sudo usermod -aG docker $USER
```

*Close the Ubuntu terminal and reopen it to apply the new user group permissions.*

### Step 4: Expose Docker Daemon to Windows
To allow .NET Aspire and your IDEs on Windows to communicate with the Docker daemon running inside WSL, you need to expose its TCP port.

In your **Ubuntu** terminal, edit the Docker daemon configuration:

```bash
sudo mkdir -p /etc/docker
sudo tee /etc/docker/daemon.json > /dev/null <<EOF
{
  "hosts": ["unix:///var/run/docker.sock", "tcp://0.0.0.0:2375"]
}
EOF
```

Then, update the systemd Docker service so it doesn't override the `hosts` argument:

```bash
sudo mkdir -p /etc/systemd/system/docker.service.d/
sudo tee /etc/systemd/system/docker.service.d/override.conf > /dev/null <<EOF
[Service]
ExecStart=
ExecStart=/usr/bin/dockerd
EOF

sudo systemctl daemon-reload
sudo systemctl restart docker.service
```

### Step 5: Configure Windows to use the exposed Docker daemon
Back in **PowerShell on Windows**, set the `DOCKER_HOST` environment variable so all Windows processes know where Docker lives:

```powershell
[Environment]::SetEnvironmentVariable("DOCKER_HOST", "tcp://localhost:2375", "User")
```

**CRITICAL:** Close your IDEs (Visual Studio / VS Code / Rider) and any open PowerShell windows and reopen them so they pick up the new environment variable.

### Step 6: Install Portainer CE (Free for Commercial Use)
In your **Ubuntu** terminal, spin up the Portainer web dashboard:

```bash
docker volume create portainer_data

docker run -d -p 9000:9000 --name portainer --restart=always -v /var/run/docker.sock:/var/run/docker.sock -v portainer_data:/data portainer/portainer-ce:lts
```

👉 Access the web dashboard from your Windows browser at: `http://localhost:9000`

---

## 🔧 Troubleshooting

### 1. Aspire complains "Container runtime 'docker' appears to be unhealthy"
**Why it happens:** The `DOCKER_HOST` environment variable isn't visible to the Aspire process, or WSL is stopped.
**Fix:** Open a PowerShell window, run `echo $env:DOCKER_HOST`. It should print `tcp://localhost:2375`. If not, rethink Step 5. Next, ensure WSL is running by typing `wsl` in your prompt. Start Docker inside WSL if stopped (`sudo systemctl start docker`).

### 2. Forgot Portainer Admin Password
Stop the container from **Ubuntu**, run the official password reset helper image, and restart:

```bash
docker stop portainer
docker run --rm -v portainer_data:/data portainer/helper-reset-password
# Copy the temporary password string generated in the terminal output
docker start portainer
```

## 📚 Official Documentation
* [Official Docker Engine Installation Guide for Ubuntu](https://docs.docker.com/engine/install/ubuntu/)
* [WSL Systemd setup](https://learn.microsoft.com/en-us/windows/wsl/systemd)
* [Portainer Deployment Guide](https://docs.portainer.io/start/install-ce/server/docker/linux)