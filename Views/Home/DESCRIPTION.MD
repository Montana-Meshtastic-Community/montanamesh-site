# Home Page Views

This folder contains the main public pages served by `HomeController`.

## Pages

- `Index.cshtml`: signal-operations homepage with a first-viewport hero, Discord/map/radio CTAs, live node telemetry loaded by `wwwroot/js/nodeData.js`, explanatory cards about Meshtastic and Montana Mesh, and route links for next steps.
- `Setup.cshtml`: infrastructure transparency page describing the RackNerd VPS stack, Wormhole ingress node, and MQTT bridge traffic.
- `Connect.cshtml`: onboarding page for getting a radio, installing the Meshtastic app, and joining the Discord.
- `RecommendedConfigurationSettings.cshtml`: recommended Meshtastic settings and CLI examples.
- `Resources.cshtml`: links to Meshtastic docs, Discord, MeshMonitor, other mesh networks, and supported hardware.
- `OtherMeshNetworks.cshtml`: links to nearby Montana mesh groups in Hamilton and the Flathead area.
- `Privacy.cshtml`: placeholder privacy page.

## Documentation

Whenever public page content, links, recommendations, or frontend data expectations change in this folder, keep this service-local note current in the same change.
