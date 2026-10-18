# Harborlight release checklist

Status snapshot: 2026-10-18. This marks implementation artifacts separately from device/store acceptance; a compiling project is not a release candidate.

## Implemented locally

- [x] Original North Pier bootstrap scene, third-person walking, collision-aware camera orbit/zoom, drive/exit flow, keyboard fallback, touch movement/drive controls, and safe-area-aware mobile HUD/map.
- [x] Exclude the player collider from camera obstruction casts; launched the current Mac build and confirmed the initial camera no longer collapses onto the player.
- [x] Three original jobs: beacon repair, market parcel delivery, and East Pier circuit inspection.
- [x] Eight-car garage: starter taxi, pier sedan, delivery van, Mistline hatchback, Breakwater Utility SUV, Coastal Sport Coupe, Tideglass Atelier Serein R, and North Pier Motor Works Vela GT. The later cars remain original, fictional, and unbranded; online paint cosmetics remain server-entitled.
- [x] Version 8 JSON local save, atomic replace, pause/quit save, migration from `PlayerPrefs harborlight-save-v1` and v2-v7 files, and a first-run controls/job tutorial that returning saves skip.
- [x] Five original Harborlight base-color surface textures with mobile import settings, original taxi and four-vehicle concept art, and an asset-rights register. Six garage vehicles use Quaternius CC0 1.0 meshes with in-game paint and original display names; the van and Vela GT remain procedural fallbacks.
- [x] URP 17.5.0, mobile application IDs/orientation/build menu, configurable HTTPS backend client, offline fallback, typed admin API and reusable contract.
- [x] .NET API build, eight PostgreSQL-backed API tests, Bicep compilation, and successful eight-vehicle Mac and Android ARM64 IL2CPP development player builds. The Mac app launches; the Android package identity and debug signature verify. Both artifacts are in `Builds/`.

## Required before beta

- [x] Install Unity iOS Build Support and export the unsigned Xcode project; verify Xcode recognizes the generated targets.
- [x] Install Unity Android Build Support, SDK/NDK, and OpenJDK; build, inspect, and verify the signature of the ARM64 debug APK with the current camera fix. No Android device or emulator is attached.
- [x] Rebuilt the current-source ARM64 APK after the peninsula and curved-map-cartography updates. `aapt` confirms package, SDK range, and ARM64 libraries; `apksigner` verifies its v2 debug signature. Physical-device installation and runtime checks remain open. See `docs/reports/android-current-source-build-2026-10-18.md`.
- [x] Install the Xcode iOS platform and Simulator runtime; export the current camera-fix Unity Xcode project.
- [x] Build the `UnityFramework` target on the preceding six-car export (before the one-line camera-collider fix).
- [ ] Regenerate the iOS export from the current eight-vehicle source, then build `UnityFramework`; the existing Xcode export predates the latest save and vehicle updates.
- [ ] Build the full iOS app target: an earlier native compile reached Apple's `ibtool` launch-screen step and stalled; Unity's generated Xcode project currently advertises `iphoneos` only.
- [ ] Sign builds and run the onboarding, touch, collision, garage, all mission paths, save migration, interruption, and airplane-mode matrix on real devices.
- [ ] Measure 30 FPS on the chosen minimum device, 60 FPS target on higher-end devices, memory, crashes, ANRs, heat, and network reconnection.
- [ ] Replace remaining procedural building, character, and prop primitives; replace or upgrade the stylized Quaternius vehicle meshes with higher-fidelity original or licensed art; add animation, sound, and fonts; clear all asset and marketing rights. The current mesh integration is a prototype pass only.
- [ ] Review the new tutorial and all touch controls for accessibility; inspect safe-area behavior across notches, tablets, and small phones.
- [ ] Implement actual Apple/Google Billing SDKs and server transaction/notification verifiers, including acknowledgement, duplicate, delayed, refund, revocation, restore, and account-link tests. Current store adapters fail closed.
- [ ] Implement AdMob with consent/privacy behavior, the one-ad-per-three-completed-jobs cap, no-ad tutorial state, and server kill switch.
- [ ] Implement native Apple/Google sign-in UI and profile recovery. The API has token verification hooks; the native SDK/UI is absent.
- [ ] Build and penetration-test the authenticated operator console; exercise MFA, roles, audit trails, rate limits, and outage/rollback controls.
- [ ] Choose Azure region; complete the calculator estimate; set an owned budget-alert mailbox; deploy staging only after security review. No cloud resources have been provisioned.
- [ ] Prepare privacy policy and disclosures, store listings, original screenshots/trailer, signing assets, beta cohorts, crash/latency/purchase alerts, and staged rollout/rollback plans.

## Commerce/admin test matrix

The service test suite currently covers hashed guest tokens, cosmetic spend idempotency and balances, replay-safe purchase grants, unconfigured-store fail-closed behavior, identity-verifier fail-closed behavior, allowlisted/audited admin actions, idempotent game registration, and a restricted runtime database role that can write game data but cannot run DDL. Store-provider sandbox, MFA identity-provider integration, unauthorized token cases, refund/revocation, and the operator UI remain untested because their production integrations are not present.
