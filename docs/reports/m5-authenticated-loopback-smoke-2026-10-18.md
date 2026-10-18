# Authenticated loopback smoke — 2026-10-18

## Environment

- Unity 6000.5.4f1 macOS development player, rebuilt from the current Harborlight source as `Builds/Harborlight-M5-Authenticated-QA.app`.
- One Unity server-mode process and one Unity client on loopback, using the existing local PostgreSQL 16.14 service and a disposable database with a restricted runtime role.
- The run generated a temporary RSA key pair and server secret. The client received only the public key; the private key remained in the server process environment. Temporary database, role, key files, and processes were removed after the run.

## Result

All checks passed:

- API readiness after migrations and guest session creation.
- Client request for a short-lived realtime ticket; ticket encryption and server-side, single-use consumption.
- NGO connection approval and stable backend `playerId` binding before player spawn.
- Player spawn on both server and client, client locomotion replication, movement input, validated beacon interaction, and client interaction acknowledgment.
- Both smoke processes exited normally. The earlier NGO warning about writing a `NetworkVariable` before the `NetworkObject` spawned did not recur.

## Evidence

- Unity build log: `/tmp/Harborlight-M5-Authenticated-QA.log` (`Build Finished, Result: Success`).
- Server log: `/tmp/Harborlight-M5-E2E-fixed-20261018-6d8437-server.log`.
- Client log: `/tmp/Harborlight-M5-E2E-fixed-20261018-6d8437-client.log`.
- API log: `/tmp/Harborlight-M5-E2E-fixed-20261018-6d8437-api.log`.

## Limits

This was a one-client, one-server Mac loopback smoke, not a load, internet-transport, Android, reconnection-persistence, or production authentication test. It verified join/authentication and an interaction acknowledgment, not mission persistence. A later check verifies one beacon acceptance through the Unity/API/PostgreSQL path; see `m5-mission-persistence-e2e-2026-10-17.md`. Direct Unity Transport remains loopback-only.
