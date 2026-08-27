# Website JavaScript

This folder contains JavaScript loaded by the MontanaMesh website.

## Current Scripts

- `site.js` handles global client-side behavior such as theme toggling, repository stats display, and reduced-motion-aware reveal animations.
- `nodeData.js` fetches `/api/nodes/stats`, updates the homepage telemetry panel, and leaves visible fallback dashes if the request fails.

## Documentation

Whenever client-side behavior, API consumption, DOM IDs, or script loading changes in this folder, keep this service-local note current in the same change.
