# MontanaMesh Site

ASP.NET Core (`net8.0`) website for the Montana Meshtastic Community.

## What this app does

- Serves the public website pages (`/`, `/connect`, `/setup`, `/recommended-configuration-settings`, `/resources`).
- Exposes a lightweight stats API at `/api/nodes/stats`.
- Reads node stats from `data/node-stats.json`.
- Includes a helper script (`scripts/update-node-stats.sh`) that builds `data/node-stats.json` from the PotatoMesh node API.

## Tech stack

- .NET 8 ASP.NET Core MVC
- Static assets in `wwwroot`
- Optional container runtime with Podman Compose

## Repository setup

This repo is configured to use SSH for GitHub:

```bash
git remote -v
# origin  git@github.com:Montana-Meshtastic-Community/montanamesh-site.git (fetch)
# origin  git@github.com:Montana-Meshtastic-Community/montanamesh-site.git (push)
```

## Run locally with host `dotnet`

### Prerequisites

- .NET SDK 8.x (or newer SDK that can build `net8.0`)
- ASP.NET Core runtime 8.x installed

### Start

```bash
cd montanamesh-site
dotnet run
```

Default dev URL from launch settings:

- `http://localhost:5123`

## Run with Podman Compose

From this directory:

```bash
cd montanamesh-site
podman-compose -f podman-compose.yml up -d --build
podman-compose -f podman-compose.yml ps
```

The container publishes:

- `http://localhost:8080`

Stop:

```bash
podman-compose -f podman-compose.yml down
```

## Node stats updater (optional)

The homepage stats call `/api/nodes/stats`. To keep that file fresh, run:

```bash
cd montanamesh-site
./scripts/update-node-stats.sh
```

The script uses PotatoMesh env vars from the parent repo `.env` when available (`../.env`), and writes:

- `data/node-stats.json`

Relevant settings:

- `POTATOMESH_API_BASE` defaults to `http://127.0.0.1:8083`
- `POTATOMESH_NODE_LIMIT` defaults to `5000`
- `NODE_STATS_DATA_DIR` can override the output directory

In the master-control stack, the `node-stats-updater` service runs this every 5 minutes.

## Screenshots

Home page screenshot:

![MontanaMesh Home](docs/screenshots/home.png)

## Basic deployment notes

1. Build/test locally:

```bash
dotnet build
```

2. Choose runtime:
- Host process with system `dotnet`, or
- Container using `Dockerfile` + `podman-compose.yml`.

3. Put a reverse proxy (Caddy/Nginx) in front for TLS and domain routing.

4. Ensure persistent storage for `data/` if you want node history retained across restarts.
