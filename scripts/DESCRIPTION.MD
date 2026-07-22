# Website Scripts

This folder contains helper scripts for the MontanaMesh website.

## Scripts

- `update-node-stats.sh` samples Meshtastic MQTT topics, extracts node IDs from topic segments like `!da656c88`, updates `data/node-history.tsv`, counts total nodes plus nodes heard in the last 30 minutes, 2 hours, and 24 hours, then writes `data/node-stats.json`.

## Configuration

The script can read environment values from the parent `.env` file and supports:

- `MQTT_HOST` or `NODE_STATS_MQTT_HOST`, default `127.0.0.1`
- `MQTT_PORT` or `NODE_STATS_MQTT_PORT`, default `1883`
- `MQTT_TOPIC` or `NODE_STATS_MQTT_TOPIC`, default `msh/US/#`
- `MQTT_SAMPLE_SECONDS` or `NODE_STATS_MQTT_SAMPLE_SECONDS`, default `20`
- `MQTT_NODE_HISTORY_SECONDS` or `NODE_STATS_MQTT_NODE_HISTORY_SECONDS`, default `2592000`
- `MQTT_USERNAME`, optional MQTT username
- `MQTT_PASSWORD`, optional MQTT password
- `NODE_STATS_DATA_DIR`, default `montanamesh-site/data`
- `NODE_STATS_ENV_FILE`, default parent repo `.env`

## Maintenance Rule

Whenever script behavior, required tools, output files, or environment variables change in this folder, update this `DESCRIPTION.MD` in the same change.
