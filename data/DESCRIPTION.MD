# Website Data

This folder stores runtime data consumed by the MontanaMesh website.

## Current Files

- `node-stats.json`: current node-count snapshot returned by `/api/nodes/stats`, including the update time and source.
- `node-database.json`: generated MQTT node database keyed by unique node ID with first-seen and last-seen timestamps. This remains the fallback stats source when the MeshMonitor SQLite DB is not mounted.
- `node-observations.json`: legacy generated MQTT node last-seen state. The stats updater can migrate from it if present.
- `dataprotection-keys/`: ASP.NET Core Data Protection keys for local runs only. The root stack keeps production keys in a separate named volume. Git-ignored; never commit.
- `node-history.tsv`: legacy historical node data if retained by local tooling or prior runs. The stats updater can seed the generated node database from it.

The checkout and container build exclude these host-local data files. Before a snapshot is available the API returns its default payload for compatibility, and the homepage shows unavailable counts.

## How Data Is Updated

`montanamesh-site/scripts/update-node-stats.sh` samples MontanaMesh MQTT traffic and records unique node IDs in `node-database.json`. When `MESHMONITOR_DB_PATH` points at a readable MeshMonitor SQLite DB, `node-stats.json` uses distinct IDs from MeshMonitor's `nodes` table even during a broker outage. A successful MQTT sample supplies fallback counts when the SQLite source is unavailable. If both sources fail, the site keeps the previous snapshot and timestamp. The updater can migrate IDs from legacy `node-observations.json` and seed IDs from `node-history.tsv`. An undated TSV entry supplies an initial observation for a new node. In the root Compose stack, the `node-stats-updater` runs about every five minutes by default.

## Documentation

Whenever the data format, update cadence, or files consumed by the website change in this folder, keep this service-local note current in the same change.
