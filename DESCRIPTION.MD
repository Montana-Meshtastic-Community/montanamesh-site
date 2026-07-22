# MontanaMesh Website

This folder is the ASP.NET Core MVC website for the Montana Meshtastic Community.

## What This Project Does

- Serves the public website pages for MontanaMesh.
- Provides onboarding pages for joining the Discord, choosing hardware, and configuring Meshtastic nodes.
- Links out to PotatoMesh and MeshMonitor.
- Exposes `/api/nodes/stats`, which returns node-count data read from `data/node-stats.json`.
- Includes a script that refreshes node stats from the Meshtastic MQTT broker.

## Main Routes

- `/`: homepage with Discord, map link, and live node stats.
- `/setup`: hosting/setup transparency page describing the VPS stack and Wormhole ingress node.
- `/connect`: user onboarding and Meshtastic app/radio guidance.
- `/recommended-configuration-settings`: recommended Meshtastic settings.
- `/resources`: docs, dashboard, map, and community links.
- `/api/nodes/stats`: JSON endpoint consumed by the homepage.

## Project Structure

- `Controllers/`: MVC and API controllers.
- `Models/`: small response/view models.
- `Views/`: Razor pages and shared layout.
- `wwwroot/`: CSS, JavaScript, images, favicon assets, fonts, and client libraries.
- `data/`: node stats data read by the API.
- `scripts/`: helper scripts, currently the MQTT-backed node stats updater.
- `docs/`: project screenshots and supporting documentation assets.
- `Properties/`: launch settings for local development.

## Runtime

The project targets `.NET 8` and listens on port `8080` inside the container. In the root stack it is exposed locally on `8082` and proxied publicly by Caddy at `montanamesh.com`.

## Maintenance Rule

Whenever website behavior, routes, data contracts, styling, or deployment behavior in this folder change, update this `DESCRIPTION.MD` in the same change.
