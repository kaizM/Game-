# Harborlight map cartography refinement — 2026-10-18

## Changes

- Replaced the full-screen map's axis-aligned district fill blocks with an original 320 × 224 color layer. District boundaries now curve across the peninsula and the land mask uses the same `CoastlineZ`, `WorldMinXAtZ`, and `WorldMaxXAtZ` functions as the playable world.
- Reduced the visual weight of local streets and separated arterial color/width from secondary roads. The existing Bayline Parkway, Southgate Connector, and Cedar Vale Scenic Route remain the most visible cross-district routes.
- The map still shows only the five currently supported starter-hub service markers; no nonfunctional destinations were added.

## Verification

- Unity 6000.5.4f1 macOS QA build succeeded: `harborlight-map-readability-build-2026-10-18.log` (`Build Finished, Result: Success`).
- Launched the current-source player at a 30 FPS cap with an isolated test profile and captured the expanded map at 1280 × 720: `screenshots/Harborlight-Map-Readability-QA-2026-10-18.png`.
- The runtime log records `HL_QA_EXPANDED_MAP_CAPTURE_REQUESTED` and no C# or Unity exception in this capture session: `harborlight-map-readability-runtime-2026-10-18.log`.
- The QA process was terminated after the capture, and its temporary profile directory was removed.

## Remaining gaps

- This is a cartographic UI improvement, not proof that the world roads are organic. The northern urban core and several outer districts still have a grid-based street layout.
- The map view does not verify full-route traversal, vehicle collision, later-client spawns, Android touch use, or device performance.
- Buildings and district fills remain prototype art, and the five service markers only represent existing starter-hub interactions.

OneState references were used only for general role-play map readability. Harborlight's map colors, district shapes, labels, and routes remain original.
