# Player avatar interest-management smoke

## Setup

- Unity 6000.5.4f1 development build: `Builds/Harborlight-Interest-Transitions-Logged-QA-20261018.app`.
- One local server and two local clients connected through loopback, authenticated against a disposable local API and PostgreSQL database.
- Each process used a distinct `HARBORLIGHT_TEST_PROFILE`, keeping test saves and tokens under `/tmp/Harborlight-TestProfiles` rather than Unity's normal player profile.
- The test did not use remote transport, Android, production services, or additional clients.

## Results

The server moved the clients through three fixed separations. Both client logs reported the expected visible-player-object count at each phase:

| Separation | Expected visible objects per client | Client 1 | Client 2 |
| ---: | ---: | --- | --- |
| 700 m | 2, including the owner | PASS | PASS |
| 900 m | 1, the owner only | PASS | PASS |
| 700 m again | 2, including the owner | PASS | PASS |

Server logs recorded both hide operations at 900 m and both show operations after the clients returned to 700 m. Both clients reached the normal smoke-test exit. The source uses a 700 m show radius, an 850 m hide radius, 200 m spatial cells, and a 0.5-second refresh interval.

## Profile handling

The first attempt before profile isolation reused Unity's standard persistent-data path. It changed the saved player position, which was restored from the untouched v6 save, and wrote a test API token that was removed. The pre-test token state was not recorded, so recovery of a previous token cannot be confirmed. Subsequent test processes used unique temporary profiles.

After the run, the disposable API, database, database role, temporary profiles, and scratch credentials were removed. The API port closed, and the existing PostgreSQL service was left running. The standard profile save remained restored, and its session-token file was absent. macOS reported no thermal or performance warning; no Unity Editor or QA player remained running.

## Limits

This verifies avatar visibility transitions for two local clients on the earlier 7.91 km² map build. The later source expansion to 14.24 km² has not been compiled or run. The smoke does not establish bandwidth savings, player capacity, collision-free distributed spawns, remote networking, mobile performance, or synchronization of vehicles, NPCs, and other world entities.


## Follow-up on the 14.24 km² build — 2026-10-18

The later expanded-map two-client run passed all three visibility phases on both clients. Full setup, filtered logs, database/profile cleanup, and remaining limits are recorded in `expanded-map-multiplayer-smoke-2026-10-18.md`; the 7.91 km² results above remain historical evidence for that earlier build.
