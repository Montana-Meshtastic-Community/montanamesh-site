# Shared Website Views

This folder contains Razor views shared across the website.

## Files

- `_Layout.cshtml` defines the HTML shell, navigation, footer, and script includes. Views supply page metadata and an optional hero image. The layout uses one URL for preloading and rendering that image.
- `Error.cshtml` renders the standard ASP.NET error page.

## Documentation

Whenever navigation, global scripts, layout markup, shared styling, or shared error behavior changes in this folder, keep this service-local note current in the same change.
