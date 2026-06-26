# Background Images

This folder contains optimized local WebP background images used by `wwwroot/css/site.css`.

## Files

- `billings-skyline-2024.webp`: Billings page background, optimized from the Wikimedia Commons Billings skyline image.
- `missoula.webp`: Missoula page background, optimized from the Wikimedia Commons Missoula image.
- `great-falls-central-ave.webp`: Great Falls page background, optimized from the Wikimedia Commons Central Avenue and Civic Center image.
- `bozeman-main-st.webp`: Bozeman page background, optimized from the Wikimedia Commons Main Street Bozeman image.
- `butte-uptown.webp`: Butte page background, optimized from the Wikimedia Commons Butte uptown image.

## Operational Notes

These local assets avoid first-page-load requests to Wikimedia and keep each background under roughly 400 KB for faster cold loads.

## Maintenance Rule

Whenever background image files, source images, optimization settings, or CSS asset paths change in this folder, update this `DESCRIPTION.MD` in the same change.
