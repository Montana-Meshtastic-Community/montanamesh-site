# Asset JavaScript

This folder is an alternate or legacy JavaScript asset path under `assets`.

## Current Note

The main layout currently loads scripts from `wwwroot/js`. `nodeData.js` is kept in sync as a legacy duplicate so older references get the same stats fallback behavior if they are ever restored.

## Documentation

Whenever JavaScript files in this folder change or references move between `assets/js` and `wwwroot/js`, keep this service-local note current in the same change.
