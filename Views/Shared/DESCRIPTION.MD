# Shared Website Views

This folder contains Razor views shared across the website.

## Files

- `_Layout.cshtml` defines the HTML shell, Bootstrap include, navigation menus, including the Resources dropdown links, theme toggle button, footer, and script includes.
- `_Layout.cshtml.css` contains layout-scoped styles if used by Razor CSS isolation.
- `_ValidationScriptsPartial.cshtml` includes jQuery validation scripts for forms.
- `Error.cshtml` renders the standard ASP.NET error page.

## Maintenance Rule

Whenever navigation, global scripts, layout markup, shared styling, or shared error behavior changes in this folder, update this `DESCRIPTION.MD` in the same change.
