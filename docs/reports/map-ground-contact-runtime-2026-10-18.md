# Map and building ground contact — 2026-10-18

## Change

- Reduced the building foundation overhang from 0.38 m to 0.12 m so the pad does not read as a large raised platform.
- Reduced the foundation's proud edge from 0.025 m to 0.005 m above the highest sampled terrain point. The foundation remains buried at its low edge and supports the building continuously.
- Tightened the map smoke so it checks the restored save against the clamped loaded coordinate, inspects foundation/building/terrain alignment at streamed sites, and fails when the spawn obstruction fallback is used.

## Verification

- Unity 6000.5.4f1 built `Builds/Harborlight-Ground-Contact-QA-20261018.app`; the build log reports `Build Finished, Result: Success` with no C# compiler errors.
- An isolated out-of-bounds save at `(0, -5000)` restored at the playable boundary `(0, -3026)` without using the obstructed-spawn fallback.
- Runtime probes passed all 360 outer road crossings and 51 sidewalk positions.
- Building contact checks passed at five streamed map locations. Each checked building's lowest renderer matched its foundation top; foundation tops were 0.005 m above sampled terrain. Streaming checks loaded 4–6 nearby shells at each location with none outside the 560 m unload radius.
- A 1280×720 game-only starter-district capture is `screenshots/Harborlight-Ground-Contact-2026-10-18-Game-Only.png`.
- The player exited normally. `pmset -g therm` reported no thermal or performance warning.

## Limits

This verifies sampled ground contact, selected road/sidewalk colliders, save clamping, and local shell streaming. It does not verify all 722 shell sites, a full map walk or drive, multiplayer arrival placement, Android, or production performance. The view remains a blockout with repeated facade kits, sparse parcels, simple props, and no interiors; this pass fixes placement and does not complete the production art upgrade.
