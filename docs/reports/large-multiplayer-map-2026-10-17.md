# Harborlight multiplayer map extension — 2026-10-17

## Map changes

- Expanded the shared playable bounds from 2,400 × 1,446 m (3.47 km²) to 3,600 × 2,196 m (7.91 km²).
- Extended the generated outer road grid to fourteen east-west and fourteen north-south routes. The coastline stays on the north edge; the new southern area follows the existing graded plateau.
- Generated 105 unique outer building sites across West Works, East Docks, Southgate, West Ridge, East Ridge, and Southport. Shell geometry continues to use the existing visual-streaming radius and slope-aware foundation placement; collision boxes remain available on each process.
- Added Southport Commons, streetlights placed along outer sidewalks, six region labels, and eight marked multiplayer entry areas on road intersections.
- Kept the first eight NGO client IDs in the starter district. Later IDs distribute across the eight entry areas, with deterministic nearby slots. The new spawn behavior is code-level only; it has not been exercised in a multiplayer runtime.
- Shared world bounds drive player/vehicle clamps, saved-position loading, ground/road generation, and the map view. Road meshes select 32-bit indices when the expanded geometry exceeds the 16-bit vertex limit.

## Build and checks

- Unity 6000.5.4f1 macOS development build `Builds/Harborlight-M9-Multiplayer-Map-QA.app` completed successfully (`Build Finished, Result: Success`) with no C# compiler errors. Build log: `/tmp/Harborlight-M9-Multiplayer-Map-Build.log`.
- A static bounds audit found 105 unique building sites and eight unique entry points, all inside the new world. `git diff --check` passed.
- The new player was not launched after this build. The build result does not verify runtime mesh creation, road joins, traversal, collision, distributed spawning, or device performance.

## Limits

The map geometry is generated locally, not transferred over NGO. All roads, terrain, and outer collision boxes remain resident, and NGO has no interest management, so this expansion does not establish a multiplayer player-capacity claim. Outer facade kits repeat and have no interiors; large-scale terrain detail, traffic, and production art are still open. Remote transport, multi-client travel, map traversal, Android, memory, and server/client load have not been measured.

## Follow-up runtime evidence — 2026-10-18

- Built `Builds/Harborlight-M10-All-Jobs-QA-Fix.app` successfully after the map expansion. One low-priority loopback server and one authenticated client booted the current world and completed all three job chains. This confirms the current full-world bootstrap can start in both network processes; the player remained in the starter district.
- A brief visual QA pass on the same M10 build opened the full-screen map. The game-only 1280 × 720 capture shows the live `3.60 × 2.20 KM` bounds, six named outer districts, road grid, and eight entry markers: `screenshots/Harborlight-M10-Multiplayer-Map-Game-Only.png`.
- The map view does not verify traversal through the outer grid, streamed-shell unload, collision, or actual later-client entry spawns. Server transport is still loopback-only and no capacity claim is implied.
- Build log: `/tmp/Harborlight-M10-All-Jobs-Build-Fix.log`. The shared Unity/API/PostgreSQL run is recorded in `m5-mission-persistence-e2e-2026-10-17.md`.
