# Map building visibility range — 2026-10-18

## Change

- Increased the gameplay camera far clip from 240 m to 600 m so it covers the 450 m visual-load radius and 560 m unload radius.
- Reduced exponential-squared fog density from 0.004 to 0.0025 so streamed district shells remain legible through their load range.
- Added a runtime check that fails if the camera far clip is shorter than the outer-building unload radius.

## Verification

- Unity 6000.5.4f1 rebuilt `Builds/Harborlight-Ground-Contact-QA-20261018.app`; the build log reports `Build Finished, Result: Success` with no C# compiler errors.
- Runtime logged `HL_MAP_BUILDING_VISIBILITY result=PASS farClip=600 loadRadius=450 unloadRadius=560`.
- The same map smoke passed the boundary save restore, all 360 road crossings, 51 sidewalk probes, five building-contact checks, and five streaming locations.
- `screenshots/Harborlight-District-Visibility-2026-10-18-Game-Only.png` is a 1280×720 capture from the district at z=-700 m. It shows the longer view and honestly records the still-open grassy parcels.
- No thermal or performance warning was reported during the short QA run.

## Limits

This verifies camera range against local building streaming, not frame rate or visual quality on Android. The larger city still has sparse blocks and repeated facade kits; draw distance does not add content. Full-map traversal, shell unload/reload, and multiplayer capacity remain unverified.
