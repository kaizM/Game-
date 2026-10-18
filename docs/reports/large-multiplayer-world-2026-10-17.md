# Harborlight large multiplayer world — 2026-10-17

## Change

- Expanded the shared playable bounds to 2,400 × 1,446 m (3.47 km²), about 5.7 times the previous 0.61 km² map.
- Extended the connected road grid to nine outer east-west routes and ten north-south routes. The intersections include crosswalks; road, sidewalk, and curb colliders remain split into short segments.
- Added 36 exterior building shells across West Works, East Docks, and Southgate, using the existing original Harborlight modular facades and grounded foundation placement. Added Southgate Commons with a paved walk, trees, and benches.
- Increased terrain mesh resolution for the wider footprint. Updated player movement bounds and the minimap from the same shared world constants; the full map title reports its live dimensions.
- The world is generated deterministically on each client, so static map geometry is local while networked player state remains replicated.

## Verification

- Unity 6000.5.4f1 macOS development build succeeded: `Builds/Harborlight-M6-Large-Multiplayer-Map-QA.app`. Build log: `/tmp/Harborlight-M6-Large-Multiplayer-Map-Build.log`.
- Launched the player and checked its `Player.log`; no startup errors or mesh/collider warnings were found. The player exited normally.
- Checked the full-screen map and game viewport from the new build: [full map](screenshots/Harborlight-Large-Multiplayer-Map-Game-Only.png) and [gameplay viewport](screenshots/Harborlight-Large-Map-Gameplay-Game-Only.png). Both are 1280 × 720 game-only captures.
- A static bounds check confirmed all 36 outer building sites are unique and inside the playable bounds. `git diff --check` passed.
- macOS reported no thermal or performance warning after the build and brief player launch.

## Limits and next checks

- This verifies world generation and player startup, not driving or walking the full route, road joins, collisions, save bounds, or a multiplayer traversal on the new build.
- The outer world is still a sparse prototype: 36 outer shells reuse two facade kits, there are large open parcels, no interiors or traffic, and the characters, vehicles, and props need a production art pass.
- The map is generated locally but is not streamed; there is no multiplayer interest management. The new 3.47 km² world has not been profiled on Android or tested with multiple clients. Do not infer a player-capacity claim from this map expansion.
