# Website Data

This folder stores runtime data consumed by the MontanaMesh website.

## Current Files

- `node-stats.json`: current node-count snapshot returned by `/api/nodes/stats`.
- `node-history.tsv`: historical node data if retained by local tooling or prior runs.

## How Data Is Updated

`montanamesh-site/scripts/update-node-stats.sh` samples the Meshtastic MQTT broker, extracts node IDs from matching topics, keeps the latest observation timestamp per node in `node-history.tsv`, and writes `node-stats.json`. In the root compose stack, the `node-stats-updater` container runs that script every five minutes by default.

## Maintenance Rule

Whenever the data format, update cadence, or files consumed by the website change in this folder, update this `DESCRIPTION.MD` in the same change.
