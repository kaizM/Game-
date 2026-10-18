# Current-source Android development build — 2026-10-18

## Artifact

- APK: `Builds/Android/Harborlight-Development-ARM64-20261018-CurrentSource.apk` (120 MB).
- Unity: 6000.5.4f1; IL2CPP; ARM64 only; minimum SDK 26; target SDK 36.
- Package: `com.meridian.harborlight`.
- Build log ends with `Build Finished, Result: Success`: `android-current-source-build-2026-10-18.log`.

## Package checks

- `aapt dump badging` reports the expected package and SDK range.
- APK contents include `lib/arm64-v8a/libil2cpp.so`, `libunity.so`, and other ARM64 runtime libraries.
- `apksigner verify --verbose --print-certs` succeeds with APK Signature Scheme v2. The certificate is Android's debug certificate; this APK is for development and is not release signed.
- Unity's bundled OpenJDK was used for `apksigner`; no system-wide Java setting was changed.

## Runtime and release status

- `adb devices -l` returned no attached devices. Install, launch, touch controls, networking, rendering, memory, frame rate, and thermal behavior remain unverified on Android hardware.
- No release keystore, Android App Bundle, store disclosures, or release candidate was produced in this step.
- The artifact is a current-source development build, not proof that Android runtime acceptance or release readiness is complete.
