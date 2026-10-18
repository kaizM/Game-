# Harborlight large multiplayer map — 2026-10-18

## Map expansion

- Increased the playable bounds from 4,600 × 3,096 m (14.24 km²) to 6,000 × 4,116 m (24.70 km²), a 73% increase in area.
- Extended the connected outer road grid to 23 north–south routes and 25 outer east–west routes, plus the existing core connector row. The runtime probe covered all 598 generated road crossings.
- Increased the outer building layout from 722 to 1,200 grounded collision sites and named the additional areas West Bluffs, East Bluffs, and Cedar Vale. The full-screen map now displays the new 6.00 × 4.12 km bounds and 36 entry zones.
- Distributed multiplayer arrivals across a 6 × 6 layout. Each entry zone reserves 25 deterministic positions, giving 900 unique coordinate slots for NGO client IDs 8–907; IDs 0–7 retain the starter-area spawn.

## Build and runtime checks

- Unity 6000.5.4f1 built `Builds/Harborlight-Multiplayer-Expanded-QA-20261018.app`; `large-multiplayer-map-build-2026-10-18.log` records `Build Finished, Result: Success`.
- The 30 FPS isolated-profile map smoke passed all 598 road crossings, 84 sampled sidewalk colliders, all 900 in-bounds and unique distributed spawn positions, and five of five selected building streaming/ground-contact waypoints. It also passed the save-bounds and camera-versus-streaming-range assertions. Filtered assertions: `large-multiplayer-map-smoke-2026-10-18.log`.
- The game-only 1280 × 720 capture of the expanded map is `screenshots/Harborlight-Large-Multiplayer-Map-Game-Only-2026-10-18.png`.

## Limits

The smoke checked the spawn-position table and map geometry; it did not connect 900 clients or exercise NGO owner IDs 8–907 in a live session. The prior authenticated two-client avatar-visibility run used the previous 14.24 km² layout. Long walking/driving routes, multi-client traversal on this expanded layout, later-client collision behavior, remote transport, Android performance, and multiplayer capacity remain unverified. The new area uses the same two repeated facade kits and still has large open parcels, so this expansion is not production-quality city art.
