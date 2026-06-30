# Website Models

This folder contains small C# model classes used by the MontanaMesh website.

## Models

- `ErrorViewModel.cs` holds the ASP.NET error request ID and whether it should be displayed.
- `NodeStatsResponse.cs` is the response model for `/api/nodes/stats`. It returns total nodes, 30-minute active nodes, 2-hour active nodes, 24-hour active nodes, and the UTC update timestamp as strings for simple frontend rendering.

## Maintenance Rule

Whenever model fields, defaults, or API response shapes change in this folder, update this `DESCRIPTION.MD` in the same change.
