# Mobile role-play game reference research

**Checked:** 2026-10-17  
**Purpose:** identify player-facing systems that can guide Harborlight’s original offline city-life game. These are public storefront descriptions written by publishers; they describe advertised features, not independently verified implementation details.

## Reference games

| Game | Publisher-described strengths | What to learn for Harborlight |
|---|---|---|
| OneState RP | Character identity and appearance, player-selected life roles, jobs and businesses, law/medic/service careers, vehicle collection and tuning, seasonal updates, and a persistent multiplayer city. Its listing advertises servers with up to 900 real players. | Give players a clear identity, career progression, recognizable places to return to, and reasons to personalize a vehicle. Harborlight remains offline single-player, so use authored NPCs and local progression instead of claiming a live social world. |
| Grand Mobile | A start-from-scratch role-play loop, legal and criminal career paths, businesses, faction conflict, missions, vehicle collection, tuning, and street racing. | Make work and vehicle ownership reinforce each other: jobs fund cars and cosmetics, while each district has distinct career opportunities. Harborlight’s existing courier, beacon, and pier chains are a foundation; they need stronger narrative arcs and visible consequences. |
| Car Parking Multiplayer 2 | Open-world walking and driving, service locations, police/taxi/drone/drag activities, tuning, fuel and suspension systems, interiors, character customization and animation, and structured driving challenges. | Cars need more than a catalog: distinctive handling, readable customization, interiors, and repeatable driving challenges. A smaller set of polished original vehicles is a better fit for Harborlight’s first district than copying another game’s roster size. |

## Current official-storefront check (2026-10-17)

These are publisher-advertised feature sets from the current Google Play listings, not independent verification of implementation. OneState currently advertises role choice, jobs and businesses, real-estate investment, law/medic/service careers, car tuning, appearance customization, and seasonal events. Grand Mobile emphasizes legal/business and criminal careers, player-run multiplayer role-play, and a range from everyday cars to supercars. Car Parking Multiplayer 2 advertises free walking, car services, taxi/police/drone/drag activities, tuning, fuel and suspension systems, vehicle interiors, building interiors, and 82 driving challenges; its latest notes mention character movement, weather/physics, rally, tire, and gearbox updates. [OneState listing](https://play.google.com/store/apps/details?id=com.Chillgaming.oneState), [Grand Mobile listing](https://play.google.com/store/apps/details?id=com.grand.launcher), [Car Parking Multiplayer 2 listing](https://play.google.com/store/apps/details?hl=en-GB&id=com.olzhas.carparking.multyplayer2).

**Design use:** Harborlight’s three job chains and eight fictional vehicles give it a playable foundation, but the current build still needs stronger character identity, distinct career progression, reliable mission completion, and repeatable driving activities. For the offline first release, author those around NPCs, local saves, and earned vehicle cosmetics; do not imply multiplayer or use another game’s brands, assets, dialogue, or progression rules.

## Earlier OneState hub observations

**Observed on 2026-10-17:** The running OneState window reached a city hub with a character and car preview over a night skyline. The hub exposed profile/progression, currency, store, events, tasks, stories, businesses, gangs/factions, updates, account linking, and a chat field. A limited-time offer appeared before the hub. No purchase, reward claim, or social linking was performed.

A fresh capture of the still-running Mac session confirmed the hub remains open. It showed a level-2 profile with an XP bar, cash and premium-currency balances, rotating promotions, task/event badges, a character-and-car preview, and a live chat announcement. The preview is a hub presentation; it does not establish that the background is a navigable or loaded city scene. I attempted to enter through `PLAY` using the native UI automation bridge, but the bridge returned `noWindowsAvailable`; I did not retry by injecting input through another tool. The earlier attempt in this session had opened a guest-account linking panel and returned to the hub when closed.

Profile screens showed daily and weekly tasks with progress, rewards, and `GO`/`CLAIM` actions; a story board organized content into episodes and chapters with completed, in-progress, and locked states; a business board represented careers as cards with hiring requirements, timers or earnings, and unlock conditions; and a progress screen showed a rank/status with reward milestones. These are observations of the visible interface, not proof of how those systems are implemented.

The `PLAY` action opened a guest-account linking panel in this session. Closing it returned to the hub, so in-world play was not reached. The panel may be a prompt rather than a mandatory gate; that remains unverified. World controls, missions in progress, vehicle handling, save behavior, and offline behavior were not tested.

**Design use:** Harborlight can borrow the general clarity of a city hub, visible progression, task/story boards, and career unlocks. Keep its interface, names, art, mission content, and progression rules original; keep the planned single-player loop local and offline. The OneState content and screen layout are not project assets.

**Method limits:** The app was observed through local window screenshots and a few menu interactions. A process sample showed about 214% CPU, but no frame-time or FPS measurement was available. REA’s native UI provider failed during capture, and the CUA bridge could capture the hub but not perform the `PLAY` interaction (`noWindowsAvailable`). No REA decompiler provider is configured on this Mac (Ghidra is unconfigured, Hopper is absent, and IDA is unconfigured). In-world play, vehicle handling, missions, saving, and offline behavior remain untested. This pass did not decompile code or extract game assets.

## Latest OneState in-world observation (2026-10-17)

The running app was already in a city tutorial. The screen showed the player facing Anne, a mission card `A DRIVE FOR BABY` / `Talk to Anne`, and dialogue recommending that the player buy a ride to travel around the city. The rendered street included a multi-story facade, pavement, hedges, a parking sign, the player character and an NPC. The HUD showed a minimap, health/needs meters, level/progress, currency, store/tasks/notification controls, chat and moderation notices, and touch-style movement/action controls. This verifies a rendered in-world tutorial state, not mission completion or the underlying mechanics.

I captured the live window through CUA and left the game open. Coordinate clicks failed with `noWindowsAvailable`; pressing Return and W left the visible frame unchanged. The `Keep listening` prompt remains unadvanced. No purchase, reward claim, chat message, or account-setting change was made. The app rendered successfully, but movement, driving, mission completion, saving, and offline behavior were not tested.

A fresh capture showed Anne's dialogue recommending that the player buy a vehicle for getting around the city. The scene layers a large character portrait and dialogue panel over the live third-person view, while keeping the mission card and the map/chat/tasks/store/currency controls visible. I retried the visible dialogue with CUA and REA's native UI observer; CUA clicks still returned `noWindowsAvailable`, keyboard Return/E did not change the frame, and REA could not match Accessibility window geometry to the selected Unity window. The tutorial therefore remains unadvanced; this is a UI automation limitation, not a gameplay result.

The nested iOS app package identifies itself as arm64, bundle version 1.3.1 (build 18088), with Unity version metadata `2022.3.76f1`. REA inventoried 6,304 content nodes and 6,356 path occurrences without integrity contradictions. It found `Data/Managed/Metadata/global-metadata.dat` and hashed `.bundle` files under `Data/Raw/aa/iOS`; the inventory does not decode their contents. REA timed out after 60 seconds while inspecting `UnityFramework.framework/UnityFramework`. Ghidra is unconfigured, Hopper is absent, and IDA is unconfigured, so this pass did not recover code bodies or map data. These package facts are summarized with evidence IDs in `REA-REFERENCE-LEDGER.md`.

**Design use:** Harborlight can use the high-level ideas of an in-world first job, a persistent objective card, a compact minimap/status HUD, and dialogue that points toward vehicle ownership. Keep Harborlight’s interface, names, art, mission content, dialogue, and progression rules original; the OneState screen and contents are reference evidence, not project assets.

## Harborlight direction derived from the research

1. **A life loop that reads clearly:** choose a local career, meet an authored character, complete a job, receive a visible reward, improve reputation, and unlock a new job or cosmetic. Keep every step usable without network access.
2. **A car identity loop:** keep the current original vehicle names and licensed base meshes; add distinct handling, garage presentation, paint, and a small set of meaningful upgrades. Do not imply that fictional cars are real-world branded models.
3. **A believable single-player city:** use named neighborhoods, landmarks, service businesses, and NPC interactions to create social texture without multiplayer, voice chat, or a custom live-world server.
4. **A fair progression loop:** keep mission rewards sufficient to earn gameplay vehicles. Reserve purchases for optional cosmetics as the production plan specifies.
5. **A production-art gate:** the current runnable build still shows a primitive player figure and a sparse waterfront view. Replace the character placeholder, add animation, and make the harbor read as a connected district before describing the game as realistic or production-ready.

## Free vehicle asset scan (2026-10-17)

I checked current creator and marketplace listings for vehicles that can legally ship in a Unity game. This is a short, screened scan rather than a claim that every free listing online was reviewed.

| Source | Listing evidence | Harborlight fit |
|---|---|---|
| [Kenney Car Kit](https://kenney.nl/assets/car-kit) | 45 3D transport assets; CC0. | Safe for commercial prototypes and background traffic, but its generic kit style does not meet the realistic hero-car target. |
| [Unity Simple Car Pack](https://assetstore.unity.com/packages/3d/vehicles/simple-car-pack-278056) | Free, marked low-poly, 856.5 KB, and listed as URP compatible under the Unity Asset Store EULA. | Easy Unity import, but the low-poly style is not a visual upgrade over the licensed Quaternius cars already in the project. Check the EULA before release; it is not CC0. |
| [Fab Vehicle Variety Pack](https://www.fab.com/listings/dc1ada50-2523-44b1-b0e2-a72d14076fb4) | Free-for-life listing advertises five drivable vehicles with interiors, but only lists Unreal Engine format. | Not a direct Unity import; don’t add without an authorized interoperable source format and verified license terms. |
| [Fab Off-Road Vehicle](https://www.fab.com/listings/57a1bc10-b879-4d1d-b1fe-46f8abb8dee5) | Lists FBX/Unity URP support, a 69,947-triangle assembled model, interior/exterior parts, and 4K base-color, normal, and packed occlusion/roughness/metallic maps. The listing content did not establish that it is free. | A stronger fidelity candidate for a detailed utility vehicle, but its size needs LODs and a mobile budget; price and license must be checked before acquisition. |

**Decision:** the free assets I verified are stylized or low-poly, so adding more of them will not solve the current realism gap. Keep the CC0 Quaternius models as prototypes/traffic; prioritize a small set of original fictional hero cars with detailed interiors, PBR maps, and LODs. Do not add real car badges or real-world model names without an explicit license.

## Scope and evidence limits

- The OneState listings and bounded runtime observations above inform broad role-play feature categories. The latest session reached a tutorial scene, but interactive progress beyond its first dialogue and other game mechanics remain unverified.
- The OneState REA package inventory is preserved in `../one-state-analysis/current-build-snapshot.md`. It gives Unity package metadata and type-name categories, not source gameplay code, decoded map data, or server internals.
- No OneState code, maps, art, UI, names, or network design are used by this project. The references below guide original design choices only.

## Sources

- [OneState RP — Google Play listing](https://play.google.com/store/apps/details?id=com.Chillgaming.oneState)
- [Grand Mobile: RP Open World — Google Play listing](https://play.google.com/store/apps/details?id=com.grand.launcher)
- [Car Parking Multiplayer 2 — Google Play listing](https://play.google.com/store/apps/details?id=com.olzhas.carparking.multyplayer2)
