# Current-source Android build — 2026-10-18

## Artifact

- Unity 6000.5.4f1 completed an ARM64 IL2CPP development build from the current Harborlight source, including the 24.70 km² multiplayer map: `Builds/Android/Harborlight-Development-ARM64-20261018-Expanded-Map.apk` (120 MB).
- `aapt` reports package `com.meridian.harborlight`, min SDK 26, and target SDK 36. The APK contains `lib/arm64-v8a/libil2cpp.so` and `libunity.so`.
- `apksigner` verifies the APK with the Android debug certificate using signature scheme v2. This is a development APK, not a release-signed package.
- `adb devices -l` returned no attached devices. Install, launch, touch controls, network play, frame rate, memory, and thermal performance on Android remain untested.

## Build note

Unity's build log reported that Xcode's Metal toolchain (`metal` and `metal-objdump`) was unavailable during shader processing. The Android build still ended with `Build Finished, Result: Success`, and the resulting package contains its ARM64 libraries. No Xcode components were installed as part of this build. A physical Android device is still needed for runtime acceptance.

Filtered build evidence: `android-expanded-map-build-2026-10-18.log`.
