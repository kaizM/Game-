# Expanded-map multiplayer smoke — 2026-10-18

## Setup

- Unity 6000.5.4f1 development player: `Builds/Harborlight-Terrain-Grade-QA-20261018.app`, built from the 4,600 × 3,096 m (14.24 km²) source.
- One local server and two authenticated local clients connected through Unity Transport loopback. The test used a temporary API, PostgreSQL database, restricted runtime role, RSA ticket keys, and unique client profiles.
- All Unity processes ran at low priority in batch/no-graphics mode. Development smoke mode capped the clients at 30 FPS and exited after 25 seconds.
- The server placed players 700 m apart in the southern expanded map at z=-1,200 m. It moved them to 900 m separation and back to 700 m to exercise server-side avatar interest management.

## Results

- Both authenticated clients connected and spawned; the server accepted two distinct player identities.
- At 700 m, both clients saw two player objects. At 900 m, both saw only their own object. On return to 700 m, both saw the other player again.
- All six client assertions passed: three phases on each client. Server logs recorded all three phases, two hide operations, and two re-show operations.
- Both clients and the server reached their smoke exits. The test database and restricted role were dropped, the temporary RSA files and client profiles were removed, and no game/API process remained.
- macOS reported no thermal or performance warning.

## Evidence

Filtered runtime logs are in `/tmp/Harborlight-Expanded-Map-Multiplayer-20261018015321_78606-skgiwner/` (`server.log`, `client-a.log`, `client-b.log`, and `api.log`). Cleanup was checked against PostgreSQL: the test database count and exact runtime-role count were both zero; both client profile paths were absent.

## Limits

This verifies authenticated two-client avatar visibility transitions on the current expanded-map build. It does not verify full-map walking/driving, distributed entry spawns for later client IDs, collision, vehicle/NPC replication, bandwidth savings, ten-client load, remote transport, Android behavior, or a player-capacity target. The city is still prototype art with repeated building kits and open parcels.
