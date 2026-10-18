# Network runtime smoke test

## Result

The macOS development player built successfully with Unity 6000.5.4f1. A bounded, low-priority headless run started a loopback server on `127.0.0.1:7777` and two clients. All three processes exited with code 0.

Both clients connected and spawned their network player. The server accepted movement input from both owners and validated each owner's `AcceptBeaconRepair` request against its own scene interactables and player distance. Each owning client received the replicated interaction acknowledgment. One player moved about 0.47 metres server-side toward the contact before it entered the interaction radius. The action currently reports acknowledgment only; it does not complete or persist a job.

Evidence logs from the last run are `/tmp/Harborlight-Network-Server.log`, `/tmp/Harborlight-Network-Client1.log`, and `/tmp/Harborlight-Network-Client2.log`. They contain `HL_NET_CONNECTED`, `HL_NET_INPUT_ACCEPTED`, `HL_NET_INTERACTION_VALIDATED`, and `HL_NET_INTERACTION_ACK` markers for both clients, followed by clean disconnects during the test shutdown. No exception or thermal/performance warning was reported.

In a follow-up build, both clients also logged `HL_NET_ANIMATION_SPEED ... value=1.00` for the owner and remote player, confirming the server-written locomotion variable reached the client Animator parameter. This was a headless run, so the rendered animation was not visually inspected.

The client reconnect follow-up ran one client against server process 1, terminated that server after the first connection, then started server process 2 on the same loopback address and port. The client logged `HL_NET_DISCONNECTED`, `HL_NET_RECONNECT_SCHEDULED attempt=1 delay=1`, a reconnect attempt, and a second `HL_NET_CONNECTED`. All processes exited with code 0. That run intentionally interrupted the session immediately after initial connection and did not test a job interaction or preservation of online player state. Logs: `/tmp/Harborlight-Reconnect-Server1.log`, `/tmp/Harborlight-Reconnect-Server2.log`, and `/tmp/Harborlight-Reconnect-Client.log`.

## Fixes found while testing

- NGO registers its generated network-message handlers in an `AfterSceneLoad` callback. Starting `NetworkManager` from Harborlight's `Awake` ran before that callback and produced an empty message table. Network session startup now runs from `HarborlightBootstrap.Start`.
- Unity rejects a nested `NetworkManager`; the manager and transport now share the persistent session root.
- Road dashes and crosswalk stripes were primitive cubes with colliders at ankle height. Their colliders are removed while road and curb collision remain.
- Clients retry an unexpected disconnect with exponential delays (1, 2, 4 seconds, capped at 30 seconds) and reset the delay after connecting. Intentional shutdown suppresses retries.
- `-harborlight-smoke-test` is development-build-only, exits after 15 seconds, and uses test-only spawn positions near the Harborlight beacon contact. Normal player spawn placement is unchanged.

## Remaining validation

- The headless run verifies locomotion parameter replication but not rendered animation quality or sustained open-route movement.
- Verify repeated interaction limits and both clients traversing open roads and sidewalks; the reconnect test does not preserve online state across a server restart.
- Verify transport and movement on Android devices. The local loopback test does not exercise NAT, internet hosting, authentication, persistence, or production security.
- Implement authoritative job-state changes before calling the beacon interaction a completed gameplay loop.
- CPU, memory, bandwidth, observed tick rate, replication volume, and latency were not measured in these functional smoke runs; they are not performance or capacity tests.

## Build

The build log is `/tmp/Harborlight-Network-QA-Build.log` and ended with `Build Finished, Result: Success.` The reusable QA app is `Builds/Harborlight-Network-QA.app`. Build invocation:

```sh
nice -n 15 env HARBORLIGHT_MAC_BUILD_PATH=Builds/Harborlight-Network-QA.app \
  "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath "/Users/admin/Desktop/RP game/harborlight-city" \
  -executeMethod Harborlight.Editor.HarborlightProjectSetup.BuildMacQaPlayer \
  -logFile /tmp/Harborlight-Network-QA-Build.log
```
