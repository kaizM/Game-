# Harborlight map shape and public references — 2026-10-18

## Reference takeaways

- The user-provided Google image-cache URLs were not directly accessible, so I used the public map-guide image descriptions and label structure only as broad references. The guide presents named neighborhoods and 30 numbered destinations; that supports a readability cue for Harborlight—named districts, clear routes, and recognizable destination markers. Its coordinates, labels, and map artwork are not used.
- OneState's public site describes an LA-set social role-play world and highlights distinct player lives and city interaction. Its 2026 launch announcement says the open-world map was rebuilt and city districts were reworked. These support clearer district identity as a design goal; public descriptions do not specify street coordinates or map geometry.
- The official Grand Theft Auto V page refers to Los Santos and Blaine County. As a broad open-world pattern, a dense city connected to varied outskirts gives players a reason to travel between different kinds of places. This is a Harborlight design inference, not a claim about the other game's exact layout.
- No OneState map, extracted assets, code, or street layout was used.

## Harborlight changes

- Replaced the nearly straight north shore with two broad, asymmetric inlets and a central headland. The terrain mesh, sand strip, water plane, seawall, and HUD use one shared `CoastlineZ` function.
- Added the original curving Bayline Parkway and collision-backed Southgate Connector and Cedar Vale Scenic Route, with shoulders and center markings. The northern core remains a dense local grid. Beyond it, the road hierarchy now uses fewer east-west links and eight north-south arterials; crosswalks stay on selected intersections.
- Shaped the actual terrain and map display into a north-broad, south-tapered peninsula, shifted slightly east at its southern end. A low outer-water mesh surrounds the new land edge; roads, building sites, player movement, saved positions, and multiplayer spawns stay on playable land.
- Kept the 6.00 × 4.12 km bounding envelope and all 36 multiplayer entry zones. The shoreline/terrain mesh, minimap mask, and road endpoints share the same boundary. The land mask, Parkway, connectors, and active outer-road corridors filter the current outer-building layout to 924 sites.
- The northern core and some district corridors remain grid-based. This pass reduces the outer grid, adds two original cross-district routes, and keeps the irregular land boundary. It does not complete hand-authored local street patterns, fill all districts with functional RP destinations, or close the remaining open parcels.

## Implementation evidence

- Mac development build: `Builds/Harborlight-Driving.app`.
- Earlier peninsula evidence is preserved in `harborlight-peninsula-build-2026-10-18.log` and `harborlight-peninsula-runtime-2026-10-18.log`. The current road-hierarchy build/smoke and screenshot are recorded in `harborlight-map-road-hierarchy-2026-10-18.md`.

## Sources

- [OneState official site](https://onestate.com/game)
- [Community OneState destination map guide](https://onestatebr.com.br/noticias/mapa-de-qths-da-policia-onestate)
- [OneState global launch announcement: rebuilt map and reworked districts](https://www.gamespress.com/pt/Social-RolePlay-Phenomenon-OneState-Releases-Globally-Launch-Version)
- [Grand Theft Auto V — PlayStation official page](https://www.playstation.com/en-us/games/grand-theft-auto-v/)
