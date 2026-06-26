# Home Page Views

This folder contains the main public pages served by `HomeController`.

## Pages

- `Index.cshtml`: homepage with community intro, Discord CTA, map link, node stats loaded by `wwwroot/js/nodeData.js`, and three explanatory cards about Meshtastic, the project, and long-term network goals.
- `Setup.cshtml`: infrastructure transparency page describing the RackNerd VPS stack and Wormhole ingress node.
- `Connect.cshtml`: onboarding page for getting a radio, installing the Meshtastic app, and joining the Discord.
- `RecommendedConfigurationSettings.cshtml`: recommended Meshtastic settings and CLI examples.
- `Resources.cshtml`: links to Meshtastic docs, Discord, PotatoMesh, MeshMonitor, and supported hardware.
- `OtherMeshNetworks.cshtml`: links to nearby Montana mesh groups in Hamilton and the Flathead area.
- `Privacy.cshtml`: placeholder privacy page.

## Maintenance Rule

Whenever public page content, links, recommendations, or frontend data expectations change in this folder, update this `DESCRIPTION.MD` in the same change.
