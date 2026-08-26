# Website Data

This folder stores runtime data consumed by the MontanaMesh website.

## Current Files

- `node-stats.json`: current node-count snapshot returned by `/api/nodes/stats`.
- `node-history.tsv`: historical node data if retained by local tooling or prior runs.

## How Data Is Updated

`montanamesh-site/scripts/update-node-stats.sh` calls the PotatoMesh nodes API, counts total and recently-heard nodes, and writes `node-stats.json`. In the root compose stack, the `node-stats-updater` container runs that script every five minutes by default.

## Documentation

Whenever the data format, update cadence, or files consumed by the website change in this folder, keep this service-local note current in the same change.
