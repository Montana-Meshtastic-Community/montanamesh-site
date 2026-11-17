# Website JavaScript

This folder contains JavaScript loaded by the MontanaMesh website.

## Current Scripts

- `site.js` initializes theme toggling, repository stats, and reduced-motion-aware reveal animations independently. The page can switch themes when browser storage is unavailable.
- `nodeData.js` fetches `/api/nodes/stats` and updates the homepage counts, source, and update time. It shows dashes when a snapshot is unavailable and marks old counts as stale.

## Documentation

Whenever client-side behavior, API consumption, DOM IDs, or script loading changes in this folder, keep this service-local note current in the same change.
