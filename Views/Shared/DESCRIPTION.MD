# Shared Website Views

This folder contains Razor views shared across the website.

## Files

- `_Layout.cshtml` defines the HTML shell, skip link, Bootstrap include, preload hints for local fonts and active page backgrounds, responsive navigation menus, Resources dropdown links, GitHub repo stats link, theme toggle button, 2026 footer, and script includes.
- `_Layout.cshtml.css` contains layout-scoped styles if used by Razor CSS isolation.
- `_ValidationScriptsPartial.cshtml` includes jQuery validation scripts for forms.
- `Error.cshtml` renders the standard ASP.NET error page.

## Documentation

Whenever navigation, global scripts, layout markup, shared styling, or shared error behavior changes in this folder, keep this service-local note current in the same change.
