# Website Controllers

This folder contains the ASP.NET Core controllers for the MontanaMesh website.

## Controllers

- `HomeController.cs` serves the public MVC pages, including resource subpages, and sets a `BackgroundKey` used by the layout/CSS for page-specific presentation.
- `NodesController.cs` exposes the `/api/nodes/stats` API. It reads `data/node-stats.json`, accepts either camelCase or PascalCase property names, and returns safe zero/default values if the file is missing or unreadable.

## Behavior Notes

The node stats API is intentionally tolerant: if the updater has not run yet or a bad JSON file is present, the website still responds instead of failing the page.

## Maintenance Rule

Whenever routes, controller behavior, API contracts, or error handling change in this folder, update this `DESCRIPTION.MD` in the same change.
