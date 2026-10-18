# Harborlight — North Pier

Harborlight is an original Unity 6000.5.4f1 (Unity 6.5.4f1) city-life game prototype targeting iOS and Android. Its current playable slice is a waterfront city map with walking, driving, touch and keyboard controls, three job chains, an eight-vehicle garage, vehicle purchases earned with job credits, an objective map, and offline local progression. Six garage vehicles use licensed meshes, and the streamed outer districts now use three Quaternius CC0 building forms with 2K source maps. The named waterfront core still repeats two modular facade sets; characters, traffic, props, and much of the district remain prototype art. This is a visual milestone, not final production art.

The current playable map has a 6,000 × 4,116 m (24.70 km²) bounding envelope. Its terrain now forms a broad northern coast with two inlets and a south-tapered peninsula, with a 23-by-26 outer road layout clipped to the shoreline, 924 current outer building sites across ten named districts, and 36 multiplayer entry zones with 900 unique remote-player spawn slots. The full-screen map opens with `M` or by tapping the minimap and marks nearby network players. The current Mac road-hierarchy smoke passed 179/179 active road probes, 748/748 curved-route/arterial/collector surface samples, 92/92 sidewalk probes, 900/900 land spawn slots, and five streamed-building ground checks. The latest full-screen map capture is `docs/reports/screenshots/Harborlight-Map-Readability-QA-2026-10-18.png`; the map-shape report explains the clean-room reference use. These checks do not verify full routes, Android performance, or multiplayer capacity. Server-side avatar visibility uses 200 m spatial cells: other avatars appear within 700 m and are hidden beyond 850 m. A two-client Mac loopback verified that visibility transition on the prior map build. The static world is generated locally and currently filters player avatars only, not vehicles, NPCs, or world objects. Outer building visuals load within 450 m of the local player and unload past 560 m, at up to two buildings per half-second; collision boxes, roads, terrain, and the central district remain loaded. Full traversal and live allocation of later client IDs, client/server load, and Android remain unverified. The three outer-shell forms reduce repetition, but the local street layout is still grid-based in several districts; open parcels, repeated central facades, prototype characters and props, and 2K source building maps remain. The city is not yet a realistic finished game or a 4K source-art target. See `ART-DIRECTION.md`, `Assets/Art/THIRD-PARTY-ASSET-REGISTER.md`, `docs/reports/expanded-multiplayer-world-2026-10-18.md`, `docs/reports/harborlight-map-shape-reference-2026-10-18.md`, `docs/reports/district-grounding-2026-10-17.md`, and `docs/reports/world-streaming-2026-10-17.md`.

## Open and play

Open this folder in Unity Hub and run `Assets/Scenes/Harborlight.unity`.

- Desktop: WASD to walk, Shift to jog, E to interact; W/S and A/D to drive; E to exit a vehicle; right mouse drag and wheel to orbit/zoom.
- Mobile: left virtual stick to walk/steer, drag the right side to orbit, pinch to zoom, and on-screen USE/GAS/BRAKE/EXIT buttons.
- F1 opens the local development admin panel in the Editor or a development build. Escape closes it, the garage, or the bank panel.

The first launch opens a short controls-and-first-job tutorial; existing saves skip it. The game writes a versioned v9 JSON save to Unity's private persistent-data folder and migrates `harborlight-save-v1` PlayerPrefs data and v2-v8 save files while preserving existing progress. World simulation, jobs, and local cash/bank transfers work without a network. Online bank balances and transfers use the authenticated API, and cosmetic paints use a separate gems wallet. Set the `ApiBaseUrl` field on **Harborlight → Create Backend Settings** to an HTTPS environment to enable a guest profile, wallet, bank, and cosmetic catalog; plain HTTP is accepted only for a loopback development URL.

## Mobile setup and builds

The project uses URP 17.5.0. Install Unity 6 with the Android or iOS build support needed for your target; Android builds also need the SDK, NDK, and OpenJDK, while iOS builds need Xcode. Generated players and APKs under `Builds/` are local QA outputs and are not committed. Use **Harborlight → Build iOS Development Project** or **Build Android Development APK** to export a development build. The latest source and map checks are documented in `docs/reports/`. Release signing, device provisioning, store SDKs, and store metadata still need to be supplied before submission.

## Service and reusable admin

The backend, admin console, and reusable admin SDK are maintained in separate neighboring workspaces named `harborlight-services` and `game-admin-sdk`; they are not included in this Unity repository.

The F1 overlay is **development-only** and changes only the local test save. Production admin actions require an OIDC token with the `game-admin` role and an MFA claim; the REST API exposes typed, allowlisted actions, idempotency, and audit history. There is no arbitrary SQL endpoint and no mechanism for the admin service to mutate a disconnected player's local game.

## Visual direction

Read `ART-AND-CONTENT-GUIDE.md` before adding art or missions. `RP-GAME-RESEARCH.md` summarizes current public RP-game feature references and Harborlight design takeaways. The game uses original names and layout; REA's saved OneState report is limited to high-level packaging/type-name observations and is not an art, map, or code source.
