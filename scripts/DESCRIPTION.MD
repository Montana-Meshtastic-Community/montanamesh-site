# Website Scripts

This folder contains helper scripts for the MontanaMesh website.

## Scripts

- `update-node-stats.sh` samples MontanaMesh MQTT traffic, extracts Meshtastic node IDs from JSON payloads, protobuf `ServiceEnvelope` packets, or MQTT topic suffixes, updates `data/node-database.json` with unique node IDs plus first-seen and last-seen timestamps, and writes `data/node-stats.json`. `totalNodes` is the count of unique IDs in the node database; the 30-minute, 2-hour, and 24-hour counts are calculated from last-seen timestamps.

## Configuration

The script can read environment values from the parent `.env` file and supports:

- `MQTT_STATS_BROKER_URL`, default `mqtt://mqtt5:1883` in the master-control stack; standalone script fallback is `mqtt://127.0.0.1:1883`
- `MQTT_STATS_TOPIC`, default `msh/US/#`
- `MQTT_STATS_USERNAME` and `MQTT_STATS_PASSWORD`, default to `MQTT_USERNAME` and `MQTT_PASSWORD` if present
- `MQTT_STATS_TLS`, default `false`
- `MQTT_STATS_SAMPLE_SECONDS`, default `60`
- `MQTT_STATS_CONNECT_TIMEOUT_SECONDS`, default `15`
- `NODE_STATS_DATA_DIR`, default `montanamesh-site/data`
- `NODE_STATS_DATABASE_FILE`, default `montanamesh-site/data/node-database.json`
- `NODE_STATS_ENV_FILE`, default parent repo `.env`

## Documentation

Whenever script behavior, required tools, output files, or environment variables change in this folder, keep this service-local note current in the same change.
