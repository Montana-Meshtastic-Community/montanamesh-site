# Website Controllers

This folder contains the ASP.NET Core controllers for the MontanaMesh website.

## Controllers

- `HomeController.cs` routes public pages and errors. The views supply page metadata.
- `NodesController.cs` exposes the non-cacheable `/api/nodes/stats` API. It reads `data/node-stats.json`, accepts either camelCase or PascalCase property names, and returns the compatibility defaults if the file is missing or unreadable.

## Behavior Notes

The node stats API is intentionally tolerant: if the updater has not run yet or a bad JSON file is present, the website still responds instead of failing the page.

## Documentation

Whenever routes, controller behavior, API contracts, or error handling change in this folder, keep this service-local note current in the same change.
