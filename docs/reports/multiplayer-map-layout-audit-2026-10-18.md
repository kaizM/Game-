# Multiplayer map layout audit — 2026-10-18

## Current world

- Playable world bounds: 4,600 × 3,096 m, or **14.24 km²**.
- Generated outer road network: 18 north–south roads and 20 east–west roads, with 360 connected crossing locations.
- Generated outer district collision sites: 722. Building visuals load near each local player; static terrain, road meshes, and collision boxes remain local to each matching game client.
- Distributed multiplayer arrivals: 24 entry zones, at least 750 m apart. Each has 25 reserved locations on a 5 × 5 grid with 3 m spacing. The source-derived audit checked 600 unique positions for client IDs 8–607; client IDs 0–7 still use the starter area.
- All generated sites and distributed spawn locations fall inside the current playable bounds. The closest distributed spawn is 80.6 m from an outer building center.

These layout counts and distances were reproduced from the current Unity map source. They establish coordinate coverage and road-grid connectivity, not driving, collision, or multiplayer capacity.

## Runtime map review

- Unity 6000.5.4f1 built `Builds/Harborlight-Map-Review-QA-20261018.app`; the build log ends with `Build Finished, Result: Success`.
- A short Mac QA launch used an isolated save profile and a 30 FPS cap. The game-rendered 1280 × 720 capture shows the expanded map labeled **4.60 × 3.10 km**, seven outer district labels, and 24 multiplayer entry markers: `reports/screenshots/Harborlight-Multiplayer-Map-Current-Game-Only.png`.
- The runtime streamed ten nearby outer building visuals during the brief review. macOS recorded no thermal or performance warning. This was not a frame-rate or memory benchmark.
- The previous `Harborlight-Expanded-Multiplayer-Map-Game-Only.png` capture predates the expansion and shows the former 1.04 × 0.61 km map. Use the current capture above when reviewing the 14.24 km² layout.

## Remaining checks

- No end-to-end walking or driving route has been traversed across the expanded districts.
- Entry placement for client IDs above seven has only source-derived coordinate checks; it has not been exercised by connecting that many clients.
- Building grounding, road/sidewalk collisions, save-position bounds, Android performance, remote transport, and multiplayer capacity still need runtime tests.
- The map remains prototype blockout art with repeated facades and large open areas. The map review verifies scale and layout, not production-quality buildings or terrain textures.
