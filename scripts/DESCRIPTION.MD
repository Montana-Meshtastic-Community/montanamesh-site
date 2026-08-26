# Website Scripts

This folder contains helper scripts for the MontanaMesh website.

## Scripts

- `update-node-stats.sh` fetches node data from PotatoMesh, counts total nodes plus nodes heard in the last 30 minutes, 2 hours, and 24 hours, then writes `data/node-stats.json`.

## Configuration

The script can read environment values from the parent `.env` file and supports:

- `POTATOMESH_API_BASE`, default `http://127.0.0.1:8083`
- `POTATOMESH_NODE_LIMIT`, default `5000`
- `NODE_STATS_DATA_DIR`, default `montanamesh-site/data`
- `NODE_STATS_ENV_FILE`, default parent repo `.env`

## Documentation

Whenever script behavior, required tools, output files, or environment variables change in this folder, keep this service-local note current in the same change.
