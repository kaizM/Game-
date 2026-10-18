# Third-person camera recovery — 2026-10-18

Status: VERIFIED for the scoped Mac framing/collision/mode checks below. Full camera input and device quality remain IN_PROGRESS.

Baseline local commit: `350e449`. Current verified code commit is recorded in `CHECKPOINT.json`.

## Change

- Moved the existing `HarborlightCamera` into its own Unity component source file, retaining the class name, target contract, and planar movement directions. Follow distance, shoulder offset, pitch, and touch sensitivities are serialized for authoring.
- Default walking distance is 4.2 m with an 18-degree pitch and 0.25 m shoulder offset, replacing 6.8 m and 27 degrees. The same-location capture now gives the character more screen space.
- Added smoothed target/orbit tracking, a 7.2 m vehicle distance with a vehicle-body focus, and snap recovery on retargeting or large teleports.
- Sphere casts now start at the actual target focus, with a reusable hit buffer and self/occupied-vehicle filtering. The old cast began 0.65 m away and could skip a nearby thin wall. Camera movement retracts immediately for an obstacle and returns gradually after clearance. The local subject is hidden at very close range and restored after clearance or camera disable.

## Verified checks

`camera-recovery-build-2026-10-18.log` ends in `Build Finished, Result: Success`. `camera-recovery-runtime-2026-10-18.log` reports 10/10 PASS checks:

1. Default walking framing: measured 4.207 m (includes the shoulder offset).
2. Moving follow: the camera stayed within its expected following range and the target-to-camera segment stayed clear during direct character movement.
3. Stop/reposition recovery: walking distance restored.
4. Thin wall at 0.45 m: the camera remained on the character side, covering the former cast-origin gap.
5. Close-wall subject hiding.
6. Recovery after removing the wall, including subject visibility.
7. Replacement target binding.
8. Teleport recovery.
9. Parked vehicle mode: measured 7.204 m, without colliding against the occupied vehicle.
10. Exit/restoration to walking mode: measured 4.207 m.

`camera-character-regression-runtime-2026-10-18.log` also passed the four-view resident smoke: 4.504 m movement in 1.001 s, Animator stop parameter 0.0121. The disposable profile was restored to `(0, 28)` and outside the vehicle, with 125 credits and 0 reputation.

Captures in `screenshots/camera-recovery-2026-10-18/`: previous follow, corrected walking follow, moving follow, and parked vehicle follow. All use the same 1280×720 Mac player, profile, district, and lighting. The movement tests call the real movement method and vehicle entry/exit methods directly; they do not simulate keyboard or touch input and do not prove driving physics or multiplayer seating.

## Repeat

Build using the resident-validated Mac command in `character-surface-recovery-2026-10-18.md`, then run:

```sh
HARBORLIGHT_TEST_PROFILE=CodexGoalVisualQA_20261018_1205 nice -n 15 'Builds/Harborlight-Character-QA-20261018.app/Contents/MacOS/Harborlight' -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile /tmp/harborlight-camera-qa.log -harborlight-camera-qa /tmp/harborlight-camera-captures
```

The harness requires a disposable profile, creates/removes temporary test fixtures, restores test transforms, and exits automatically. Use the same completed-tutorial profile and `(0, 28)` position for matching screenshots.

## Remaining

Ordinary walk/jog/run animations and movement feel are next: the current carry-walk pose is still used with empty hands. Touch/keyboard orbit and zoom, long driving routes, busy-world obstruction cases, sustained performance, physical Android/iOS testing, final character textures, authored district art, and the production HUD remain open. The 30 FPS cap on these short tests limits host load and is not a device-performance result. No new world area was added.
