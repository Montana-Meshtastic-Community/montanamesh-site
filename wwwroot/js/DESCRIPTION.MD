# Website JavaScript

This folder contains JavaScript loaded by the MontanaMesh website.

## Current Scripts

- `site.js` handles global client-side behavior such as theme toggling and repository stats display.
- `nodeData.js` fetches `/api/nodes/stats` and updates the homepage node count cards.

## Maintenance Rule

Whenever client-side behavior, API consumption, DOM IDs, or script loading changes in this folder, update this `DESCRIPTION.MD` in the same change.
