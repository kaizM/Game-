# M5 mission persistence loopback — 2026-10-17

## API and database

- Backend test project rerun on .NET 10 and PostgreSQL 16.14: **16 passed, 0 failed, 0 skipped**. Tests cover beacon, courier, and dock-inspection transition order, rewards, replay idempotency, and concurrent duplicate requests.
- A live HTTP check used a disposable database and restricted runtime role. Missing server authorization returned HTTP 401. Beacon acceptance returned stage 1 and 125 credits; replay returned `replayed=true`. Repair then returned stage 2, 210 credits, and 1 reputation; replay did not grant the reward twice. The HTTP snapshot and SQL row both read stage 2, 210 credits, 1 reputation, and two receipts.
- The disposable database, runtime role, temporary API process, and test schema were removed.

## Unity-to-API end-to-end

- Ran `Builds/Harborlight-M7-Streamed-World-QA.app` as one loopback server and one headless client with a temporary RSA key pair and temporary home directory. API and Unity processes used a disposable PostgreSQL database and restricted runtime role.
- The client obtained a guest profile and join ticket; the server consumed the ticket, bound the backend player ID, spawned the player, accepted the nearby beacon-contract interaction, and returned the owner interaction acknowledgement.
- Server log marker: `HL_NET_MISSION_ACTION player=b175542a-8857-4f4b-85b4-699dde3a81cf action=AcceptBeaconRepair accepted=True replayed=False credits=125`.
- The matching database row was `BeaconStage=1`, `Credits=125`, `Reputation=0`, with one action receipt. This directly verifies the Unity action reached the authoritative API and persisted under the authenticated player identity. The earlier stage-0 discrepancy did not recur.
- Both Unity processes exited on their automated smoke timers. The temporary database and runtime role were removed; a follow-up query found neither present. `pmset -g therm` reported no thermal or performance warning.

## Limits

This proves one beacon-accept action through the Mac loopback client/server/API path, not a complete job or production multiplayer. Courier and inspection are covered at the API service layer but not in Unity. Beacon repair, wallet/inventory integration, two-client mission concurrency, Android networking, remote transport, and sustained load remain open. Temporary runtime logs and keys were removed after evidence extraction.


## Server restart and rejoin

- Built `Builds/Harborlight-M8-Reconnect-QA.app`; Unity reported success with no C# errors. The first reconnect attempt used the default 15-second smoke lifetime and ended before the second server recorded a mission request, so it was repeated with the development-only `-harborlight-smoke-test-seconds 60` option.
- Server 1 accepted `AcceptBeaconRepair` for player `72b7bab3-0abf-4b80-82e0-321703d9daf3` and persisted stage 1 with 125 credits. The client disconnected, scheduled a one-second retry, and connected to the restarted server. Server 2 restored the same player ID; the repeat acceptance was rejected and credits stayed at 125.
- The final row was `BeaconStage=1`, `Credits=125`, `Reputation=0`, with two action receipts. The repeated action did not grant credits again.
- Logs: `/tmp/Harborlight-M8-Reconnect-Server1.log`, `/tmp/Harborlight-M8-Reconnect-Server2.log`, and `/tmp/Harborlight-M8-Reconnect-Client.log`. The disposable database and runtime role were removed; no Unity player was left running, and `pmset -g therm` reported no thermal or performance warning.

This confirms reconnect restoration for the beacon stage only. It does not cover full courier/inspection chains, account recovery, Android, remote transport, wallet/inventory, or scale.

## Full three-job Unity-to-PostgreSQL smoke — 2026-10-18

### Setup

- Built the current Unity 6000.5.4f1 Mac development player as `Builds/Harborlight-M10-All-Jobs-QA-Fix.app`; Unity reported success.
- Ran one low-priority server process and one authenticated client over loopback, backed by the local ASP.NET API and a disposable PostgreSQL database. The run used a restricted runtime role, temporary RSA credentials, and an isolated player profile. Development smoke mode capped both players at 30 FPS; the client had a 90-second exit limit.
- After the run, the temporary database, runtime role, RSA files, and isolated profile were removed. The database query was captured before cleanup.

### Result

- The player walked and completed all eight server-validated actions: `AcceptBeaconRepair`, `RepairBeacon`, `AcceptCourierDelivery`, `CollectCourierParcel`, `DeliverCourierParcel`, `AcceptDockInspection`, `InspectDock`, and final `AcceptDockInspection` report.
- Server logs recorded **8 accepted actions**; the client received all mission acknowledgments and logged `HL_NET_ALL_JOBS_COMPLETE` before exiting normally.
- The final PostgreSQL snapshot was beacon stage 2, courier stage 3, inspection stage 3, **425 credits**, **4 reputation**, and **1 delivery**. The mission-action receipt table held **8 receipts**.
- The prior NGO warning about writing mission variables before player spawn is absent. The loaded server snapshot is now applied after `SpawnAsPlayerObject`.
- No thermal or performance warning was reported. No game, API, or test database process remained afterward.

### Evidence

- Unity build: `/tmp/Harborlight-M10-All-Jobs-Build-Fix.log`
- Server: `/tmp/Harborlight-M10-Jobs-20261018-000827-24057/server.log`
- Client: `/tmp/Harborlight-M10-Jobs-20261018-000827-24057/client.log`
- API: `/tmp/Harborlight-M10-Jobs-20261018-000827-24057/api.log`
- Captured database totals: `/tmp/Harborlight-M10-Jobs-20261018-000827-24057/database-summary.txt`

### Limits

This is a single authenticated player on one Mac loopback server, not multi-client concurrency, remote multiplayer, Android, or production transport. The route used Harborlight's starter district; it does not verify traversal of the 7.91 km² outer map or actual distributed entry spawns. A separate visual pass verifies the map UI layout only. Reconnect persistence after completing all three jobs remains untested. Wallet and inventory integration remain outside this mission-progress table.
