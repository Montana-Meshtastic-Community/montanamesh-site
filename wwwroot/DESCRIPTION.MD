# Website Static Assets

This folder is the ASP.NET Core web root. Files here are served directly as static assets.

## Structure

- `css/`: site-wide CSS.
- `js/`: site-wide JavaScript.
- `assets/`: images, icons, web manifest, fonts, and duplicate legacy asset paths.
- `lib/`: vendored frontend libraries such as Bootstrap and jQuery. Treat as third-party/generated unless intentionally updating dependencies.
- `favicon.ico`: root favicon.

## Documentation

Whenever static asset behavior, client-side scripts, CSS, images, or frontend dependencies change in this folder, keep this service-local note current in the same change.
