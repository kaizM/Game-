# Outer building visual streaming — 2026-10-17

## Implementation

- The 36 outer-district building shells are now registered as stream sites instead of being instantiated together during world startup.
- Each local player loads nearby building visuals within 450 m and unloads them beyond 560 m. The 110 m hysteresis avoids rapid visibility toggling at one threshold.
- The updater checks every 0.5 seconds and instantiates at most two missing visuals per check to limit burst work.
- A simple collision box for every outer shell remains present independently of its visual instance. The existing central landmark buildings, roads, terrain, and environment props remain loaded.
- Building foundations and shell bases use the terrain height function. The outer rows sit on the 4.2 m plateau; each shell's lowest renderer is aligned to its foundation top.

## Verification

- Unity 6000.5.4f1 built `Builds/Harborlight-M7-Streamed-World-QA.app` successfully. Build log: `/tmp/Harborlight-M7-Streamed-World-Build.log` (`Build Finished, Result: Success.`).
- The Mac player launched and logged three near-player shell loads: `East Docks ... 05`, `West Works ... 04`, and `East Docks ... 06`. No startup/runtime error was found in the captured player log.
- The player was closed after capture. `pmset -g therm` reported no thermal or performance warning.
- Game-only 1280×720 capture: `screenshots/Harborlight-M7-Streamed-Gameplay-Game-Only.png`.

## Limits and next checks

- Far-distance unload/reload has not been observed during traversal; only the initial loads are evidenced.
- Two-client travel through the expanded bounds, client-specific load radii, server collision behavior, renderer/memory reduction, Android performance, and sustained thermal behavior remain unverified.
- This is not full-world chunk streaming. Terrain and road meshes stay resident, all outer collision boxes stay resident, and NGO interest management is not implemented.
- The city remains prototype quality: open parcels, repeated facade kits, no interiors or traffic, and no 4K art pass.
