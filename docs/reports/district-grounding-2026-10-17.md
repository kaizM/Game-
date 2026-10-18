# District building grounding and visual QA — 2026-10-17

## Changes

- Added a concrete foundation and collider below every one of the sixteen modular building shells.
- Sampled terrain at the corners of each rotated building footprint. Each foundation top is 0.025 m above the highest sampled corner; its bottom reaches 0.08 m below the lowest corner and overhangs the footprint by 0.38 m.
- Aligned each building model's lowest renderer with the foundation top so it cannot float above its footing.
- Moved the Old Ferry Office inland after its former footprint crossed the shoreline.
- Replaced blank non-entry ground-floor facade bays with framed glazing; entry bays retain doors.

## Verification

- Audited all sixteen rotated footprints. Maximum terrain-height range across a footprint: 1.09 m. Minimum shoreline clearance: 4.68 m.
- Unity 6000.5.4f1 regenerated the building prefabs and built `Builds/Harborlight-Modular-District-QA.app` successfully.
- The Mac player launched into the district without startup errors.
- Captured a 1280×720 crop of the game viewport only, with no desktop or window chrome: `screenshots/Harborlight-Grounded-Foundations-Game-Only.png`.

## Limits

This pass establishes that the foundations intersect the sampled terrain and that the prefab build and player startup succeed. It does not verify foot collision traversal on sloped lots, Android rendering, or production art quality. The image is an honest prototype view: the district still has open grass parcels, repeated facade kits, sparse props, and no building interiors. The foundations are structural placement corrections, not a finished art pass.

## Follow-up source correction — 2026-10-18

The follow-up review found why the central foundations still looked lifted: the waterfront-to-ridge terrain rose 4.2 m over only 94 m, leaving as much as 1.09 m of height difference beneath a flat building footprint. The shared terrain grade now eases from z=-58 m to z=-800 m. Roads, the ground mesh, buildings, and distributed player spawns all read the same function.

A source-derived geometry audit recalculated the rotated footprints for all 16 landmark buildings and all 722 outer shells. The largest sampled relief is now 0.038 m for the landmark buildings and 0.156 m for an outer shell. The maximum terrain grade is about 0.85%.

## Follow-up runtime verification — 2026-10-18

- Built the updated 14.24 km² Unity source as `Builds/Harborlight-Terrain-Grade-QA-20261018.app`; Unity 6000.5.4f1 reported `Build Finished, Result: Success`.
- Launched the isolated QA player at 1280×720 with a low-priority process and captured only Unity's rendered game frame: `screenshots/Harborlight-Terrain-Grade-QA-Game-Only.png`.
- The runtime entered the starter district, streamed two nearby outer-building visuals, and produced no exception/error. The game-only frame shows the central block at ground level; it is visual evidence of the starter view, not a walk/collision or full-map traversal test.
- `pmset -g therm` reported no thermal or performance warning after the build and short runtime.

This verifies that the terrain correction compiles and renders in the current build. The screenshot also confirms that the world still looks like a prototype: repeated facade kits, broad empty parcels, simple street dressing, and placeholder characters. This pass fixes ground contact; it does not make production-realistic or 4K art. Full-map traversal, distributed spawn placement, shell unload/reload, multiplayer on these bounds, Android, and collision remain unverified.
