# Website Data

This folder stores runtime data consumed by the MontanaMesh website.

## Current Files

- `node-stats.json`: current node-count snapshot returned by `/api/nodes/stats`.
- `node-database.json`: generated MQTT node database keyed by unique node ID with first-seen and last-seen timestamps.
- `node-observations.json`: legacy generated MQTT node last-seen state. The stats updater can migrate from it if present.
- `node-history.tsv`: legacy historical node data if retained by local tooling or prior runs. The stats updater can seed the generated node database from it.

## How Data Is Updated

`montanamesh-site/scripts/update-node-stats.sh` samples MontanaMesh MQTT traffic, records unique node IDs in `node-database.json`, counts total nodes from that unique ID database, calculates recently-heard windows from each node's last-seen timestamp, and writes `node-stats.json`. The updater can migrate IDs from legacy `node-observations.json` and merge IDs from `node-history.tsv`. In the root compose stack, the `node-stats-updater` container runs that script every five minutes by default.

## Documentation

Whenever the data format, update cadence, or files consumed by the website change in this folder, keep this service-local note current in the same change.
