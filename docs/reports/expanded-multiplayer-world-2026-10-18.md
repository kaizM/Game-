# Expanded Harborlight multiplayer world

## Scope

Expanded the original Harborlight district from a 208 × 196 m playable rectangle to 1,032 × 592 m (about 0.61 km²). The visual map frame spans approximately 1.04 × 0.61 km. The new area adds a connected outer road grid, sidewalks, curbs, and crosswalks, with West Works, East Docks, and Southgate districts. Twelve grounded modular landmark shells, Ternwood Park, and additional street lighting give the outer routes recognizable destinations. The expanded minimap supports a full-screen map opened with `M` or by tapping the minimap.

Road and sidewalk collider meshes are divided into sections no longer than 40 m to avoid Unity's oversized-triangle collider warning. The generated world remains static and is built locally by the same bootstrap; this work does not replicate the map over the network or add world streaming/interest management.

## Verification

- Built `Builds/Harborlight-M5-Authenticated-QA.app` with Unity 6000.5.4f1 after the map changes. Unity reported `Build Finished, Result: Success`.
- Captured and visually checked the 1280 × 720 game-only view: `reports/screenshots/Harborlight-Expanded-Multiplayer-Map-Game-Only.png`.
- Re-ran the local one-client/one-server authenticated smoke against that final build. Guest join-ticket approval, stable identity binding, player spawn, movement replication, interaction acknowledgment, and normal process exit passed. No oversized-triangle or pre-spawn NetworkVariable warning appeared.
- Closed the Unity player and removed temporary database/role/key material created for the smoke. macOS reported no thermal or performance warning.

## Limits

At the time of the initial map build, runtime evidence covered only a local loopback client/server and did not verify outer traversal, remote transport, multi-client load, Android, or production art. The landmark shells have no interiors.

## Follow-up district population pass — 2026-10-18

- Kept the playable bounds at 3,600 × 2,196 m (7.91 km²) and increased the generated outer building layout from 105 to 390 sites. Each of the 13 outer rows has two grounded building sites in each of 15 road-grid blocks.
- Expanded distributed entry locations from eight to 16 road intersections. The HUD now identifies them as multiplayer entry zones and displays their count. Client IDs 0–7 use eight unique starter-area slots; ID 8 onward rotates through the expanded entry list with deterministic nearby slots.
- A static placement audit passed: all 390 sites are unique and inside the world, clear of road/sidewalk footprints, all 16 entry zones lie on distinct road intersections, and the source indexing yields eight unique starter slots plus distinct deterministic map positions across the configured zone slots. This is not a runtime capacity or collision claim. `git diff --check` passed.
- Unity 6000.5.4f1 CLI build attempts stalled before starting the Editor, so no new binary or runtime screenshot is claimed. The M10 screenshot above represents the earlier 105-site/eight-entry layout.

## Multiplayer map visibility — 2026-10-18

- The in-game map now draws blue markers for other spawned network players, while retaining the local-player marker and the 16 gold entry-zone markers. The expanded map includes a color legend. Remote-player positions refresh once per second while the map is visible.
- The prior Mac QA build compiled the map display and server-side visibility code. The saved screenshots do not show this HUD update.

The larger site count increases the number of resident collision boxes; visual shells remain streamed locally at two loads per half-second. Avatar interest management is now implemented in source, but its range behavior, outer traversal, distributed spawn collisions, Android performance, and server load still need runtime checks.

## Server-side avatar interest management — 2026-10-18

- Player network objects now spawn hidden from other clients. The server shows each avatar to its owner and clients within 700 m, hides it beyond 850 m, and checks visibility every 0.5 seconds. A 200 m spatial-cell index narrows candidate clients; the larger hide radius prevents repeated show/hide transitions near the boundary. The host retains all avatars locally.
- The Unity 6000.5.4f1 Mac QA build `Builds/Harborlight-Interest-Transitions-Logged-QA-20261018.app` compiled successfully. A two-client loopback used an isolated test API and disposable PostgreSQL database. At 700 m both clients saw two player objects; at 900 m both saw only themselves; when returned to 700 m both saw the other player again. All six phase assertions passed, and both clients exited normally. This confirms local avatar visibility behavior for two clients, not bandwidth savings, capacity, remote transport, or Android.
- The test was run with unique `HARBORLIGHT_TEST_PROFILE` values so each process used a temporary profile under `/tmp/Harborlight-TestProfiles`. The first attempt before profile isolation reused Unity's standard profile: the test changed the saved player position, which was restored from the untouched v6 save, and removed the test API token. The pre-test token state was not recorded, so recovery of an earlier token cannot be confirmed. All subsequent runs used isolated profiles.
- Static map generation and building shell streaming remain client-local; cars, NPCs, and other world entities are not network-replicated. Full-map traversal, production capacity, remote transport, and Android remain unverified.

## Multiplayer map expansion — 2026-10-18

- Current source expands the bounds to 4,600 × 3,096 m (14.24 km²). It generates an 18-by-20 outer road grid, 722 grounded building sites, and 24 entry zones distributed across the grid; the map adds Southport Fields as a named southern area.
- Terrain mesh subdivisions now scale with map dimensions at roughly 15 m spacing. The estimated ground mesh has 63,448 vertices, below Unity's 16-bit mesh-index limit. Building shells keep their local distance-loading behavior; all 722 collision boxes and the road/terrain meshes remain resident.
- The map expansion is source-only and has not been compiled or run. The two-client range evidence above came from the previous 7.91 km² build. No claim is made about traversal, spawn separation, mobile performance, multiplayer capacity, or production art on the larger map.

## Distributed multiplayer entry pads — 2026-10-18

- The 24 map entry markers now sit 20 m diagonally off their selected road intersections. Each marker reserves a deterministic 5-by-5 spawn grid at 3 m spacing, keeping new arrivals out of the road and sidewalk footprint.
- A source-derived static audit checked 600 unique slots for client IDs 8–607. All are inside the 4.6 × 3.096 km world, at least 14 m from the nearest road centerline, and at least 80.6 m from an outer building center. This checks generated coordinates, not live collision behavior.
- Full-map district fills now use distinct muted colors, and the map no longer issues a GUI rectangle draw for each of the 722 building sites on every repaint. This is a source-level UI cost reduction, not a measured frame-rate result.
- `git diff --check` and the coordinate audit passed. The updated map remains unbuilt and unrun; no Android, two-client distributed-spawn, traversal, or capacity evidence is claimed.

## Follow-up QA build — 2026-10-18

The expanded source, including the eased terrain grade, was later built as `Builds/Harborlight-Terrain-Grade-QA-20261018.app`. Unity reported a successful build. A short low-priority runtime launch entered the starter district and streamed two nearby shells; the game-only frame is `screenshots/Harborlight-Terrain-Grade-QA-Game-Only.png`. This updates the earlier build-status note above. It does not verify outer traversal, distributed spawns, expanded-map two-client behavior, or capacity.


## Authenticated two-client check on expanded bounds — 2026-10-18

The current 14.24 km² Mac build then passed an authenticated two-client avatar-interest smoke at z=-1,200 m. Both clients passed initial 700 m visibility, hidden 900 m visibility, and re-shown 700 m visibility; the server accepted both players and logged all three phases. See `expanded-map-multiplayer-smoke-2026-10-18.md`. Full map traversal, later-client entry spawns, remote transport, load, and Android remain open.
