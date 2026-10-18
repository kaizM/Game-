# Test plan

## Current coverage

- Resident surface recovery: read-only editor asset validation plus a bounded development-player capture/movement harness. Commands, expected pre-fix failure, exact scope, four screenshots, and runtime PASS markers are in `reports/character-surface-recovery-2026-10-18.md`. These checks do not verify keyboard/touch input, a distinct run animation, or Android/iOS.
- Camera recovery: current macOS QA player passed ten bounded checks covering camera framing/follow, thin-wall obstruction, subject hiding, recovery, retargeting, teleport recovery, and walking/vehicle camera mode transitions. Exact command, outputs, screenshots, and limits are in `reports/camera-recovery-2026-10-18.md`; touch orbit and vehicle handling remain unverified.

- The Unity source has no dedicated `Assets/Tests` directory or visible Unity test assembly in the current tree.
- `harborlight-services/tests` contains an ASP.NET Core test project; the latest service/database run and evidence limits are recorded below and in `docs/reports/backend-api-tests-2026-10-18.md`.
- Backend service/database tests were rerun against the local PostgreSQL 16 service: 20 passed, 0 failed, 0 skipped. Per-test schemas were confirmed removed; this run included bank transfers, concurrent idempotent replay, balance limits, and bank history. The report is `docs/reports/m5-bank-integration-2026-10-18.md`.
- Saved APKs and Mac/iOS builds are artifacts, not proof of device gameplay.
- `Builds/Android/Harborlight-Development-ARM64-20261018-CurrentSource.apk` was built before the resident surface correction; `aapt` verifies package `com.meridian.harborlight`, min SDK 26/target SDK 36, and `arm64-v8a` libraries, while `apksigner` verifies the v2 debug signature. `adb devices -l` returned an empty device list, so install, gameplay, touch, and performance checks remain open. See `reports/android-current-source-build-2026-10-18.md`.
- The latest isolated macOS map QA build verified save bounds, all 598 road crossings, 84 sidewalk positions, all 900 unique in-bounds spawn coordinates, five streamed building ground contacts, five local shell-streaming waypoints, and camera range through the 560 m unload radius. Full-route traversal, all-site contact, later-client runtime spawns, Android rendering, and performance remain untested; see `reports/large-multiplayer-map-expansion-2026-10-18.md` and `reports/large-multiplayer-map-smoke-2026-10-18.log`.
- The M10 Mac QA build ran one authenticated client against one loopback server, API, and disposable PostgreSQL database. The client traversed and completed all three job chains; all eight actions were accepted and acknowledged. Final progress was stages 2/3/3, 425 credits, 4 reputation, one delivery, and eight receipts. This verifies central-district job routes and persistence, not reconnect after a full chain, concurrent clients, remote transport, wallet/inventory integration, or Android. See `reports/m5-mission-persistence-e2e-2026-10-17.md`.
- A two-client Mac loopback on the prior 4,600 × 3,096 m build verified avatar visibility at 700→900→700 m. The later 6,000 × 4,116 m build passed map layout and coordinate-spawn checks, but expanded-map two-client movement, long routes, server load, and Android remain open. See `reports/expanded-map-multiplayer-smoke-2026-10-18.md` and `reports/player-interest-management-smoke-2026-10-18.md`.
- The M7 Mac build succeeded and its runtime log recorded three nearby outer-building visual loads. This does not verify far-distance unload/reload, reduced renderer/memory counts at distance, multiplayer traversal, or Android; see `reports/world-streaming-2026-10-17.md`.
- Unity 6000.5.4f1 batch script compilation succeeded after adding the bank ATM, HUD, API client, and v8-to-v9 local-save migration. The bank source was not built into or exercised in a player; see `reports/m5-bank-integration-2026-10-18.md`.
- Re-ran the backend suite: 16/16 tests passed against PostgreSQL 16.14. An isolated HTTP-to-PostgreSQL run verified server-key rejection, progression, replay, rewards, snapshots, and database rows. The latest one-client/one-server M10 Mac loopback verified all three Unity job chains and eight PostgreSQL receipts. See `reports/m5-mission-persistence-e2e-2026-10-17.md`.
- No physical Android/iOS device or sustained thermal test is recorded.
- A bounded headless Mac smoke used one server-mode Unity development player and two client processes. It verified both joins, movement input, Animator locomotion parameter replication, server-validated interaction acknowledgments, and a separate disconnect/reconnect after restarting the server. It did not measure performance or inspect rendered animation. See `reports/network-player-prefab-2026-10-17.md` and `reports/network-runtime-smoke-2026-10-17.md`.

## Required milestone checks

### Client build and first district

- Rebuild Mac and ARM64 Android from the same source revision.
- Confirm a fresh profile starts at the intended spawn without changing another save.
- Verify touch movement, camera orbit/zoom, interaction targets, safe areas, mission markers, and map readability.
- In the expanded Mac multiplayer build, open the full-screen map with `M` and by tapping the minimap. Walk and drive across the 6,000-by-4,116-meter map; visit all ten named areas, including West Bluffs, East Bluffs, and Cedar Vale. Check road/sidewalk joins, collision boundaries, saved-position clamps, local building load/unload, and remote-player visibility. Use the coordinate audit to cover all 36 entry zones, then exercise selected later-client IDs in a live session and check for spawn overlaps. Repeat traversal and map use on Android before treating the enlarged map as device-verified.
- For visual streaming, visit outer sites at less than 450 m, between 450–560 m, and beyond 560 m; confirm load, hysteresis, unload, renderer/memory counts, and collision behavior. Repeat while a second client moves separately; verify each client streams from its local player while the server retains collision validation.
- Walk from Lantern Square across the new North Pier plaza, confirm the pavement transitions and stall/bench/planter fixtures collide as intended, and verify the pier railings prevent leaving the deck. Repeat in an ARM64 Android build before marking the district path verified.
- Drive every vehicle role; repeatedly enter/exit; collide, stop, reverse, and verify map boundaries. One short taxi entry-drive-exit path is already smoke-tested on Mac; it does not satisfy this full check.
- Complete beacon, courier, and dock-inspection paths, including invalid/out-of-order actions.
- Verify offline play, app interruption/resume, save/load, migration from each supported schema, corrupt/partial saves, and no player-data loss.

### Backend and data integrity

- Use a disposable PostgreSQL database. Confirm migrations, readiness health check, restart restore, and restricted runtime role.
- Run the API test project; verify guest sessions, identity token validation, wallet spend idempotency, duplicate store callbacks, refund/revocation behavior, and unauthorized admin requests.
- Test transaction races, negative/over-limit amounts, invalid payloads, rate limits, replayed idempotency keys, audit atomicity, and secret exclusion from the client.
- Bank API integration tests pass for atomic deposit/withdraw, duplicate replay, conflicting key reuse, concurrent replay, and insufficient balances. Still exercise the Unity online bank panel against an authenticated API, retry an ambiguous response after disconnect, verify ledger rendering, and test offline save/load from a v8 profile.
- Inventory API integration tests pass for cash-authoritative purchase, reload persistence, key replay/conflict, stack and capacity limits, equipment ownership/slot exclusivity, and concurrent duplicate requests. Still exercise authenticated HTTP endpoints and the Unity bag panel, including retry after an ambiguous response; no visual equipment effect or trading path exists yet.
- Keep Docker daemon availability explicit; the current host has Docker CLI but no `docker compose` subcommand.

### Multiplayer, careers, and economy

- The three Unity job chains have one-client loopback persistence evidence. Test restoration after a full chain, two-client mission concurrency, and vehicle-seat synchronization; repeat supported routes on Android devices.
- Exercise long-route multiplayer movement across the expanded 6,000-by-4,116-meter playable area. Measure route completion, disconnections, replication, and server load; current runtime movement evidence stays in the starter district. Include distributed entry spawns, outer visual load/unload, and collision checks at each client's distance thresholds.
- Measure 10/50/100 client load only when suitable infrastructure exists. Record server CPU/RAM, bandwidth, observed tick rate, entity counts, replication volume, and latency; never infer larger capacity from a successful two-client test.
- Test simultaneous wallet/inventory/trade/property operations for atomicity, duplication, permissions, and audit history.

### Android and release

- Install/run on minimum-supported and current mid/high-tier devices. Verify touch, resolution, safe areas, background/resume, thermal behavior, crashes/ANRs, memory, and audio.
- Report 30 FPS minimum-device and 60 FPS high-tier targets only when device-backed profiles demonstrate them.
- Verify signing, store metadata, privacy declarations, staged rollout/rollback, and release artifact reproducibility.

Every execution report must name the build/source revision, test device/environment, exact checks, failures, and artifacts. A compile alone is not a gameplay test.
