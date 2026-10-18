# Harborlight outer road hierarchy — 2026-10-18

## Reference boundary

The map review used public OneState map imagery and district naming only as broad readability references: make neighborhoods distinct, make routes legible, and give players recognizable destinations. The supplied Google image-cache URLs did not open directly. No OneState map geometry, labels, code, art, or assets were used. See `harborlight-map-shape-reference-2026-10-18.md` and the [public community map guide](https://onestatebr.com.br/noticias/mapa-de-qths-da-policia-onestate).

## Harborlight layout

- Retained the 6.00 × 4.12 km playable envelope, north coast with two inlets, tapered southern peninsula, ten Harborlight district labels, and 36 multiplayer entry zones.
- Kept the denser northern city core. Outside it, reduced local east-west roads and retained eight north-south arterial corridors, plus a southern collector.
- Added original collision-backed Southgate Connector and Cedar Vale Scenic Route to complement Bayline Parkway.
- Moved nearby building sites out of road corridors; current generated shell sites: 924.
- The full map screen shows the coast, route hierarchy, entry markers, and existing functional service markers in the starter harbor hub. It does not represent functional services across all districts.

## Build and smoke evidence

- Unity 6000.5.4f1 macOS QA build: `Builds/Harborlight-Map-Connector-QA-20261018.app`, isolated bundle ID `com.meridian.harborlight.qa`.
- Build log: `harborlight-map-connectors-build-2026-10-18.log`, ending with `Build Finished, Result: Success`.
- Runtime log: `harborlight-map-connectors-runtime-2026-10-18.log`.
- Full map capture: `screenshots/Harborlight-NonGrid-CityMap-2026-10-18.png`.
- Smoke passed 179/179 active road probes, 748/748 connector/arterial/collector surface samples, 92/92 sidewalks, 900 unique spawn slots across 36 zones, 5/5 streamed building contacts, save bounds, and 5/5 selected streamed waypoints. The layout smoke summary reports 924 shell sites.

## Remaining gaps and risk

This is a map-screen and bounded surface smoke, not full vehicle traversal, a mobile test, or a live multiplayer test of later-client spawns. The city remains sparse and prototype-level; local street patterns, road signage, district-specific landmarks, and functional destinations are incomplete. The starter hub is the only area with the listed functional service markers.

The runtime log emits a Unity warning that the `Northshore playable ground mesh` has triangle edges over 500 units. A source-derived calculation from the current grid generation predicts edges under 32 m, so the warning does not match the modeled spacing; the cooked runtime collider geometry and its map-query stability still need direct diagnosis. The smoke’s sampled road queries passed, but this warning remains open.
