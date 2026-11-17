# Website Models

This folder contains small C# model classes used by the MontanaMesh website.

## Models

- `ErrorViewModel.cs` holds the ASP.NET error request ID and whether it should be displayed.
- `NodeStatsResponse.cs` defines the string count fields, update timestamp, and optional source in `/api/nodes/stats`.
- `CommunityLinks.cs` supplies the community links used across views.
- `RadioProfile.cs` supplies the recommended Meshtastic settings used in both guide tables and CLI examples.

## Documentation

Whenever model fields, defaults, or API response shapes change in this folder, keep this service-local note current in the same change.
