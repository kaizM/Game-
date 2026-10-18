# Resident surface recovery — 2026-10-18

Status: VERIFIED for this Mac surface-shading correction; the overall character and game quality gates remain IN_PROGRESS.

Local recovery baseline: `3d5b19b` (preserves the project previously uploaded as GitHub `5d24c9e`). The correction commit is recorded in `CHECKPOINT.json` under `qualityRecovery.lastVerifiedCommit`.

## Defects and correction

1. **Severe: black, mottled resident surfaces.** The imported FBX normals produced visibly broken lighting on both front and back, including with a neutral URP material. Unity-calculated mesh normals and Mikk tangents eliminate that defect. The original rig, model scale, avatar, meshes, animation controller, ownership, saves, and network contracts are retained.
2. **Incorrect texture import:** garment and shoe normal maps were imported as sRGB color textures. They now import as linear normal maps. The asset setup command preserves these settings on future rebuilds.
3. **Skin color override:** the runtime tint replaced the source skin color with nearly white. It now multiplies the source complexion by the subtle character tint.

Shader, metallic workflow, ambient probe, property-block removal, and sunlight-shadow diagnostics did not fix the original appearance. An import-unit normalization trial distorted humanoid retargeting and was reverted. Source geometry checks alone had not detected the lighting defect; the rendered comparison is the evidence for the correction.

## Reproduction and evidence

- Same disposable profile, location `(0, 0.28, 28)`, frozen idle sample, 1280×720, and fixed camera offsets for each before/after pair.
- `screenshots/character-surface-recovery-2026-10-18/` contains front, back, and profile pairs and the corrected walking capture.
- A fixed garment ROI (`crop=80:200:600:300`, grayscale values below 8) contained 53% near-black pixels before and 7% after using FFmpeg `blackframe`. This measures only clipping in that region; it is not a general art-quality score.
- `character-surface-validation-before-2026-10-18.log`: expected pre-fix failure for two incorrectly imported normal maps.
- `character-surface-recovery-build-2026-10-18.log`: material/normal-import assertions passed; `Build Finished, Result: Success`.
- `character-surface-recovery-runtime-2026-10-18.log`: four captures completed; movement travelled 4.506 m in 1.001 s and the Animator Speed parameter settled to 0.0118 after stopping. Process exited successfully; no runtime exception was found.

The motion test directly exercises `ApplyAuthoritativeMovement`, the CharacterController, and the existing Animator. It does not verify keyboard/touch input, network replication, sprint/run animation, driving, or a complete mission. The harness restores the disposable profile's original position and exits automatically. Diagnostic material/baked-mesh comparisons require the additional `-harborlight-character-diagnostics` flag and are development-only.

## Repeat commands

From the project root:

```sh
HARBORLIGHT_MAC_BUILD_PATH='Builds/Harborlight-Character-QA-20261018.app' nice -n 15 /Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$PWD" -executeMethod Harborlight.Editor.HarborlightCharacterAssetAudit.BuildVerifiedResidentQaPlayer -job-worker-count 2 -background-job-worker-count 2 -logFile /tmp/harborlight-character-build.log
HARBORLIGHT_TEST_PROFILE=CodexGoalVisualQA_20261018_1205 nice -n 15 'Builds/Harborlight-Character-QA-20261018.app/Contents/MacOS/Harborlight' -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile /tmp/harborlight-character-runtime.log -harborlight-character-qa /tmp/harborlight-character-captures
```

Use `Harborlight.Editor.HarborlightCharacterAssetAudit.VerifyResidentPresentation` as the read-only asset validation command. A freshly created test profile also needs tutorial completion and the fixed position to reproduce the same composition.

## Remaining quality priorities

1. Character motion/camera: the current carry-walk clip is still used for ordinary locomotion, and sprint has no distinct run blend. Validate acceleration, foot placement, turns, and a closer default camera with collision.
2. Author the existing North Pier/Lantern Square district with editable scene/prefab content, richer licensed props, and coherent shopfronts. Do not enlarge the map.
3. Replace remaining prototype vehicle presentation/handling and migrate the player HUD from IMGUI after the core character/city work.

Detailed skin textures and final character art remain unfinished. Physical Android/iOS checks, sustained frame timing, CPU/GPU cost, and memory profiling remain unverified; the short QA run is capped at 30 FPS to limit host load. No device-performance claim follows from it.
