# Harborlight visual recovery — 2026-10-18

## Finding and change

A controlled North Pier capture placed the test character near the Tide Beacon. Its point light (`range = 14`, `intensity = 3`) washed out the character and nearby pavement. The beacon light is now `range = 10` and `intensity = 0.85`, retaining its warm local pool while reducing spill.

## Verification

- Unity 6000.5.4f1 Mac QA player build completed with `Build Finished, Result: Success` (see `harborlight-visual-recovery-build-2026-10-18.log`).
- Captured both images at 1280×720 using the same disposable QA save and coordinates. The runtime emitted `HL_QA_GAME_CAPTURE_REQUESTED`; no runtime exception or C# compiler error was found.
- The before/after images show the blown-out halo around the player is gone. The remaining scene still has sparse pier props, open parcels, and the development HUD; those remain follow-up quality issues.

## Captures

- Before: `screenshots/Harborlight-Visual-Baseline-2026-10-18.png`
- After: `screenshots/Harborlight-Visual-Recovery-2026-10-18.png`
