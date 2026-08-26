# Website Launch Properties

This folder contains ASP.NET Core launch settings for local development.

## Current Use

`launchSettings.json` defines local development profiles and ports used by `dotnet run` or IDE launch buttons. The production container does not rely on these settings; it uses the Dockerfile and `ASPNETCORE_URLS=http://+:8080`.

## Documentation

Whenever local launch URLs, environment variables, or development profiles change in this folder, keep this service-local note current in the same change.
