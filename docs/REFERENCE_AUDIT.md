# M0 — Reference and project audit

**Audit date:** 2026-10-17  
**Status:** `VERIFIED` for project identity, installed tool inventory, and bounded package metadata. Gameplay reproduction, Android device testing, and full production readiness remain unverified.

## Project identity and scope

- **Selected project:** `/Users/admin/Desktop/RP game/harborlight-city` — Harborlight, an original Unity city-life game. The project README identifies Unity `6000.5.4f1` / Unity 6.5.4f1, iOS and Android targets, and a compact offline waterfront slice.
- **Related code:** `/Users/admin/Desktop/RP game/harborlight-services` (ASP.NET Core API, PostgreSQL migrations, admin console, infrastructure and tests) and `/Users/admin/Desktop/RP game/game-admin-sdk` (engine-neutral admin DTOs and Unity adapter contract).
- **Existing reference notes:** `/Users/admin/Desktop/RP game/one-state-analysis/code-structure.md` and `harborlight-city/REA-REFERENCE-LEDGER.md`.
- **Workspace boundary:** the active shell workspace `/Users/admin/Documents/ChatGPT/AI merger` contains many unrelated projects and a broad, uncommitted Git root with no commits. Harborlight and its related service/SDK folders are outside that root and have no Git metadata. Do not stage or modify the broad workspace as a substitute for a Harborlight repository.
- **Existing project state:** `harborlight-city/docs/` did not exist before this audit. The project already has a README, art guide, reference ledger, release checklist, research notes, build scripts, saved builds, and a Unity Library cache. Existing files and saves are preserved.

## Installed software and hardware observed

| Item | Observed value |
| --- | --- |
| Host | macOS 26.2 (build 25C56), Mac15,13, Apple M3, 16 GiB RAM |
| Unity Hub editors | `6000.3.20f1`, `6000.5.4f1`; Harborlight pins `6000.5.4f1` |
| Unity render pipeline | URP `17.5.0` |
| Xcode | 26.5, build 17F42 |
| Android SDK platforms | `android-34`, `android-36` |
| Android Build Tools | `34.0.0`, `36.0.0` |
| Android NDK | `28.2.13676358` |
| Java | No system Java runtime resolved by `java`; Unity's Android module includes `PlaybackEngines/AndroidPlayer/OpenJDK` |
| .NET | SDK `10.0.400`, runtime `10.0.11`, arm64 |
| Docker | CLI `29.8.2`; `docker compose` subcommand unavailable; daemon readiness not checked |
| PostgreSQL client | `psql` 16.14 |
| Free space | 25 GiB of a 460 GiB volume (95% used). Unity `Library` is about 8.9 GiB and saved `Builds` about 13 GiB. |

Unity's current release page identifies Unity 6.3 LTS as the LTS line through December 2027; this project pins `6000.5.4f1`. Treat that as a release-channel compatibility risk and validate any migration on a copy before changing the working project. [Unity 6 release support](https://unity.com/releases/unity-6/support)

## Skills and tools actually found

- **REA skill:** `/Users/admin/.agents/skills/reverse-engineer-anything/SKILL.md`, skill version 40.
- **REA MCP:** server/package `rea-agents` 6.4.0, 187 MCP tools. The current session reported Ghidra 12.1.4 available; Hopper's launcher is missing and IDA is not configured. Android ADB/JADX providers are unavailable. A previous ledger entry saying Ghidra was unconfigured is historical and no longer current.
- **REA operation performed:** opened the installed OneState executable for identity only, then inspected its Info.plist. No function pseudocode, gameplay source, scene, map, or asset extraction was performed. The REA session was closed afterward.
- **Graphify skill:** `/Users/admin/.codex/skills/graphify/SKILL.md`. A read-only query of Harborlight's existing `graphify-out/graph.json` returned 17 nodes and only three script files (`HarborlightBootstrap`, `HarborlightGameState`, `HarborlightPlayer`). The graph is incomplete/stale for this project and is not a substitute for source review.
- **Other relevant available skills:** `karpathy-guidelines`, `ponytail-reference`, and `imagegen`. No installed `Replica`, game-development, Unity, or 3D-modelling skill was found in the current skill catalog or skill folders. `Replica` status is `DATA_UNKNOWN` (no matching skill or provider path found); continue with verified tools and do not invent that capability.
- **Local source checkout:** `/Users/admin/Documents/ChatGPT/AI merger/rea` exists, separate from the installed skill and active MCP server.

## OneState package boundary and observed facts

- The identified nested package is `/Applications/OneState.app/Wrapper/OneState.app`, a signed iOS app bundle; it is not a native macOS game executable.
- REA opened `/Applications/OneState.app/Wrapper/OneState.app/OneState` as an arm64 Mach-O executable, SHA-256 `5ac3753f2d47527d1979db82d65e967c660516ddfce9e8e0cbfff6c972c7a03b`.
- REA Info.plist evidence `ev_20bc1dc06bbb11562f428bc0b70c6717f88941d54c65f052a754f40513d6eaba` observes bundle `com.Chillgaming.oneState`, app version `1.3.1`, build `18088`. The plist carries Unity `2022.3.76f1` metadata and iOS platform declarations.
- `codesign -dv` reported Apple iPhone OS Application Signing, team `HKZ246KUJ5`; the bundle is roughly 3.5 GiB and contains `Data/Managed`, `Data/Raw`, `Data/Resources`, and `Frameworks/UnityFramework.framework`.
- The existing REA ledger reports a package inventory and a Mach-O `cryptid=1` header observation. That does not establish that every game component is encrypted, and it does not yield gameplay source. Its earlier attempt against the large UnityFramework exceeded its time limit and recovered no methods or decoded map.
- Prior saved runtime notes describe a tutorial screen and HUD from an earlier session. No OneState process was running during this audit, so those are historical observations, not a current play test.
- A provider being available does not make the app's proprietary code/assets a clean-room implementation source. This project uses OneState only for high-level roleplay genre categories and bounded package metadata; all Harborlight implementation remains original or properly licensed.

## Harborlight source audit

- The scene is a bootstrap scene; `HarborlightBootstrap.Awake()` constructs the district at runtime. The core runtime files are `HarborlightBootstrap.cs`, `HarborlightGameState.cs`, `HarborlightPlayer.cs`, `HarborlightVehicle.cs`, `HarborlightHud.cs`, `HarborlightTouchControls.cs`, `HarborlightGarageService.cs`, `HarborlightBackendClient.cs`, and `HarborlightAdminAdapter.cs`.
- The README and code describe walking, camera orbit, touch/keyboard controls, driving, a garage, eight vehicle entries, three job chains, and offline progression. Local saves use versioned JSON schema v8 and migrate earlier local schemas.
- Current world simulation is single-player and local. The backend supplies guest sessions and wallet/cosmetic APIs, and the admin API controls typed service-side settings. Neither is a shared-world networking server. The Unity manifest has a Unity Multiplayer Center package but no concrete realtime networking dependency.
- The service README says Apple/Google purchase verification deliberately fails closed until configured; native account linking, Ads, billing SDKs, and production commerce are not ready. The production admin API is typed, role/MFA protected, audited, and has no SQL endpoint.
- The city and many props/characters remain procedural or primitive. Six garage vehicles use Quaternius CC0 models, with two procedural vehicles. The asset register and art guide preserve current licensing and prototype limitations.
- Existing Android APKs and an iOS Xcode export exist, but their timestamps predate the current playable-ground work. No physical Android/iOS device test is recorded. The Unity project has no test directory; the service has a .NET test project whose execution status was not established during M0.
- Source files have recent uncommitted changes from the preceding coastal-ground pass. `Builds/Harborlight-Coastal-Playable-QA.app` (about 0.50 GiB) and `Logs/Harborlight-Coastal-Playable-QA.log` show a successful Unity build at 19:51 on 2026-10-17. Two later small source changes (the QA application identifier and seawall opening condition) were not in that binary; its actual visual result has not yet been smoke-checked.

## M0 decision and next critical path

Keep Harborlight as the target and preserve its current working Unity version for bounded iteration. Create an isolated version-control root for `/Users/admin/Desktop/RP game` rather than altering the broad workspace Git root. Then verify a reproducible Android ARM64 build, smoke-test the coastal district on an emulator or device when possible, add a small Unity test seam for deterministic gameplay state, and profile the Android slice. Do not begin city expansion until the first district, save migration, and vehicle loop are repeatably verified.

The next architecture gate is the multiplayer stack: compare current Unity-supported server/client options against the actual pinned editor, mobile targets, server headless support, licensing, and operational cost before adding any package. Keep the backend's wallet and admin actions distinct from realtime world simulation.
