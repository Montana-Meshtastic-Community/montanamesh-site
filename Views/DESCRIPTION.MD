# Website Views

This folder contains Razor views for the MontanaMesh public website.

## Structure

- `Home/` contains page-specific views.
- `Shared/` contains the common layout, error page, and validation script partial.
- `_ViewImports.cshtml` and `_ViewStart.cshtml` set common Razor imports and the default layout.

## Maintenance Rule

Whenever page structure, shared rendering behavior, layout selection, or Razor imports change in this folder, update this `DESCRIPTION.MD` in the same change.
