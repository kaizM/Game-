## A. My review of your GitHub game

I accessed your repository: [kaizM/Game-](https://github.com/kaizM/Game-?utm_source=chatgpt.com).

The project is called Harborlight — North Pier. It is a Unity 6 project using C#, the Universal Render Pipeline (URP), and Unity Netcode for GameObjects.

The main issue is already apparent from the repository: Codex is spending effort expanding the size and complexity of the world before finishing its visual quality and core gameplay.

Here are the findings confirmed by the uploaded source and project documentation:

|   |
| - |

Area

|   |
| - |

What I found

|   |
| - |

City

|   |
| - |

Approximately 24.7 km² of generated world, but still sparse and repetitive

|   |
| - |

Buildings

|   |
| - |

Reused modular building facades; most buildings have no playable interiors

|   |
| - |

Characters

|   |
| - |

A resident model and animation assets exist, but final character presentation is incomplete

|   |
| - |

Vehicles

|   |
| - |

Eight garage entries; several vehicles still use prototype art

|   |
| - |

Gameplay

|   |
| - |

Walking, driving, jobs, and local progression are implemented but not fully device-verified

|   |
| - |

Multiplayer

|   |
| - |

Two-client loopback tests exist, but large-scale gameplay is not verified

|   |
| - |

Android

|   |
| - |

A development APK was reportedly built; physical-device performance is unverified

|   |
| - |

Architecture

|   |
| - |

A single `HarborlightBootstrap.cs` file is approximately 133 KB, concentrating substantial world-generation and setup logic

The project is a developing prototype, not a finished open-world roleplay game. The repository's own feature matrix confirms that police careers, gangs, homes, businesses, and social systems are not finished.

Expanding the city again will not fix the underlying quality problem.

## B. Additional problems found in the actual code

Driving physics are overly simplified

[HarborlightVehicle.cs](https://github.com/kaizM/Game-/blob/main/Assets/Scripts/HarborlightVehicle.cs?utm_source=chatgpt.com) changes the vehicle's position and rotation directly through Rigidbody movement methods. There is no developed wheel suspension, tire grip, or vehicle dynamics model in that class.

The HUD needs a production implementation

[HarborlightHud.cs](https://github.com/kaizM/Game-/blob/main/Assets/Scripts/HarborlightHud.cs?utm_source=chatgpt.com) contains roughly 1,000 lines and draws major interface sections using Unity IMGUI. This is a development-oriented approach, not the foundation I would choose for a polished mobile roleplay interface.

Too much responsibility sits inside the bootstrap

[HarborlightBootstrap.cs](https://github.com/kaizM/Game-/blob/main/Assets/Scripts/HarborlightBootstrap.cs?utm_source=chatgpt.com) generates terrain, buildings, props, vehicles, characters, camera, and HUD systems. This makes visual iteration and regression isolation unnecessarily difficult.

Mobile quality has not been demonstrated

The repository records successful Android packaging, but no physical Android device test. There is also no dedicated Unity test assembly visible in the repository.

The repository contains gameplay screenshots, including [the district gameplay capture](https://github.com/kaizM/Game-/blob/main/docs/reports/screenshots/Harborlight-District-Game-Only.png?utm_source=chatgpt.com) and [the outer-city capture](https://github.com/kaizM/Game-/blob/main/docs/reports/screenshots/Harborlight-Outer-City-Game-Only-QA-2026-10-18.png?utm_source=chatgpt.com). I could inspect their accompanying reports, but I couldn't render the image files directly here. My visual-quality conclusions are therefore grounded in the source code and QA reports, not a frame-by-frame visual inspection.

## C. Replacement Codex Goal Mode instruction

This is what I would use instead of the existing goal. It is specific to Harborlight and addresses the actual code rather than starting a new generic game-building project.

# HARBORLIGHT — MASTER RECOVERY AND FULL GAME COMPLETION GOAL

## ROLE AND OPERATING DIRECTIVE

You are the lead technical director, senior Unity engineer, 3D gameplay developer, art director, technical artist, performance engineer, multiplayer architect, and QA lead for Harborlight.

Repository:\
[https://github.com/kaizM/Game-](https://github.com/kaizM/Game-)

Project:\
Harborlight — North Pier

**MISSION: Transform the existing Harborlight project from an unfinished procedural Unity prototype into a visually polished, performant, fully playable, original 3D open-world multiplayer roleplay game.**

The intended experience should reach professional standards for a modern mobile open-world roleplay game, using OneState RP as a limited gameplay-quality and feature-reference benchmark, never as a source of proprietary assets or code.

This is an implementation mandate, not a documentation assignment.

The work must proceed through persistent, evidence-backed checkpoints until all completion criteria are satisfied, or an external constraint genuinely prevents further execution.

Do not claim a completed game merely because the project compiles.

---

# 1. KNOWN PROJECT BASELINE

The review of GitHub establishes the following:

- Unity version: `6000.5.4f1`.
- URP version: `17.5.0`.
- Existing Netcode for GameObjects integration.
- Existing generated 6,000 × 4,116 m game world.
- An original coastal city called Harborlight.
- Eight garage vehicle entries.
- Player movement and camera scripts.
- Existing mission, garage, banking, inventory, save, and multiplayer foundations.
- Existing third-party assets with licensing records.
- Local macOS development and Android export procedures.

Important technical debt:

1. `HarborlightBootstrap.cs` is a large, tightly coupled runtime composition and procedural world-generation system.
2. `HarborlightHud.cs` uses IMGUI for substantial gameplay UI.
3. `HarborlightVehicle.cs` uses simplified Rigidbody position/rotation movement without developed suspension or tire dynamics.
4. The city repeats limited building kits and contains substantial empty space.
5. Character and vehicle assets are not yet at final presentation quality.
6. Physical Android testing is missing.
7. The backend and shared admin SDK exist in separate local projects.
8. `Packages/manifest.json` refers to `file:../../game-admin-sdk`, meaning the GitHub client checkout is not fully self-contained.
9. The available repository documentation reports some successful local tests, but does not establish production-quality gameplay or release readiness.
10. The development Mac has previously shown limited storage and heat constraints. Reassess those conditions before expensive operations.

**Do not discard existing progress. Correct these weaknesses systematically.**

---

# 2. THE NON-NEGOTIABLE PRIORITY

STOP enlarging the map.

STOP adding thousands of additional procedural building sites.

STOP creating new menu-based features before basic gameplay quality improves.

STOP treating documentation, build success, or synthetic geometry tests as proof of game quality.

The first priority is a polished, playable city-life experience inside the existing North Pier / Lantern Square district.

Retain the existing large-world data and infrastructure, but temporarily isolate the polished central district from incomplete outer content during focused QA.

Preserve all existing saves and completed network, API, and gameplay systems.

The development sequence is:

**VISUAL QUALITY → CHARACTER FEEL → DRIVING QUALITY → CITY INTERACTIVITY → MOBILE PERFORMANCE → INTEGRATION → WORLD EXPANSION → REMAINING MULTIPLAYER SYSTEMS → RELEASE HARDENING.**

---

# 3. GOAL MODE AUTONOMY

Operate continuously within the active authorized Codex execution session.

Do not ask the user to confirm ordinary engineering decisions, routine refactors, builds, tests, or non-destructive local changes.

Use this exact cycle:

1. Inspect the latest project state.
2. Read the outstanding goals and defect list.
3. Select the highest-impact unfinished task.
4. Implement the task.
5. Compile the affected components.
6. Run targeted automated tests.
7. Launch the actual game when visual or gameplay validation is necessary.
8. Capture evidence.
9. Fix failures.
10. Run relevant regression checks.
11. Commit verified improvements.
12. Update the checkpoint.
13. Continue to the next highest-priority task.

Do not stop just to ask what to do next when the goal already defines it.

If a session ends, persist enough state that the next Codex session can automatically identify and resume the next unfinished task.

Do not invent a perpetual background process or claim that execution continues while no agent session is active.

---

# 4. DEVELOPMENT WORKSPACE AND SKILLS

Before modifying source:

- Confirm the active checkout is the Harborlight project.
- Check Git working-tree status.
- Record the starting commit SHA.
- Preserve the current working state.
- Create a dedicated quality-recovery feature branch.
- Inspect any uncommitted user work before modifying it.
- Verify whether the separate service and admin-SDK folders are present.

Read installed skills before relying on them.

Use the existing REA skill only for authorized reference analysis.

Check for Replica or comparable supported skills. The prior repository audit did not find a verified Replica installation; if still missing, record `DATA_UNKNOWN` and proceed with verified capabilities.

Never invent skill functionality.

Use available image-generation, 3D modeling, Unity-editor automation, and legitimate asset-library tooling when useful.

Respect third-party licenses and maintain asset provenance.

---

# 5. VISUAL QUALITY RECOVERY

## 5.1 Establish objective evidence

Capture the current running game at fixed camera locations:

- Character standing in the main street.
- Character walking.
- Close-up character appearance.
- Vehicle parked.
- Vehicle driving.
- North Pier waterfront.
- Typical residential building.
- Nighttime environment.
- HUD and full-screen map.

Capture a short gameplay recording demonstrating movement, driving, and interaction.

Use consistent resolution, graphics preset, lighting conditions, and camera positions when comparing improvements.

Assess:

- Character quality.
- Vehicle quality.
- World geometry.
- Environmental density.
- Texturing.
- Lighting and shadows.
- Camera composition.
- Animation transitions.
- Visual consistency.
- Mobile readability.

Create an explicit severity-ranked visual defect list.

## 5.2 Replace unfinished city presentation

Handcraft a coherent production-quality central district using editable Unity scenes and prefabs.

Keep procedural generation for appropriate supporting systems, but do not rely on primitive-based procedural assembly as the sole solution for the hero district.

Improve:

- Road surface alignment.
- Curbs and sidewalks.
- Building shape variation.
- Street furniture.
- Traffic signals.
- Signage.
- Shopfronts.
- Parking spaces.
- Pedestrian areas.
- Landscaping.
- Waterfront structures.
- Proper collision.
- Small environmental storytelling details.
- Material and lighting consistency.

The scene must feel deliberately designed rather than randomly populated.

Prioritize a smaller visually complete neighborhood over a huge empty map.

## 5.3 Asset standards

Use original or properly licensed meshes.

For each important asset, verify:

- Mesh quality and topology.
- Material assignment.
- Scale.
- Orientation.
- UV mapping.
- Normals.
- Correct texture channels.
- Collision geometry.
- LOD suitability.
- Mobile texture compression.
- Copyright and license records.

Avoid importing large high-resolution assets without platform-specific budgets.

Set mesh, texture, renderer, and memory budgets from profiling results rather than arbitrary preferences.

## 5.4 Lighting

Improve the URP presentation with controlled:

- Main directional light.
- Ambient lighting.
- Environmental reflections.
- Shadow quality.
- Exposure.
- Color grading.
- Atmospheric depth.
- Appropriate fog.
- Interior and nighttime lights.

Tune for realistic-looking stylized visuals without sacrificing device stability.

Capture before-and-after comparisons.

---

# 6. CHARACTER CONTROLLER AND ANIMATION

Inspect:

- `HarborlightPlayer.cs`
- `HarborlightBootstrap.cs`
- `HarborlightResident.prefab`
- `HarborlightResidentAnimator.controller`
- Existing animation sources.

The current character experience must be professionally reworked.

## Required movement

- Smooth acceleration.
- Smooth deceleration.
- Responsive directional control.
- Natural turning.
- Walking.
- Jogging.
- Sprinting.
- Idle motion.
- Foot-ground alignment.
- Predictable jumping if included.
- Proper slope and stair behavior.
- Correct collision.

## Animation

Create or repair proper Animator states and blend trees.

Required transitions include:

- Idle to walk.
- Walk to run.
- Run to stop.
- Turn in place where appropriate.
- Enter vehicle.
- Seated driving.
- Exit vehicle.
- Interaction animations.

Eliminate obvious animation sliding, abrupt snapping, feet floating, and character penetration.

## Camera

The existing camera uses a relatively distant third-person range. Reevaluate framing for a modern character-focused mobile roleplay experience.

Implement:

- Configurable follow distance.
- Adjustable shoulder or centered perspective.
- Smooth tracking.
- Camera collision.
- Obstruction handling.
- Natural rotation damping.
- Vehicle camera modes.
- Touch sensitivity settings.
- Stable camera behavior during entry and exit.

Validate camera and character behavior in actual gameplay.

---

# 7. REPLACE THE SIMPLIFIED VEHICLE PHYSICS

Inspect `HarborlightVehicle.cs`.

The current movement model updates the Rigidbody directly with computed translation and yaw. This is insufficient as the final vehicle-driving foundation.

Replace or progressively upgrade it with a tested vehicle dynamics implementation appropriate for mobile.

Requirements:

- Wheel contact behavior.
- Suspension.
- Acceleration curves.
- Speed-dependent steering.
- Braking.
- Reverse gear.
- Tire grip and lateral slip.
- Appropriate mass and center of gravity.
- Collision reactions.
- Smooth low-speed maneuvers.
- Stable behavior on slopes.
- Vehicle recovery from invalid states.
- Tunable handling profiles.
- Visual wheel rotation and steering.
- Proper driver seating and exit placement.
- Functional third-person driving camera.
- Vehicle sound effects.

Evaluate Unity WheelCollider or an alternative verified vehicle controller based on behavior, maintainability, and performance.

Do not simply increase movement speed and declare improved physics.

Preserve garage identifiers, purchasing, vehicle ownership, and existing persistence contracts.

### Driving acceptance

A player must be able to:

1. Approach a parked vehicle.
2. Enter with an appropriate transition.
3. Start driving.
4. Accelerate smoothly.
5. Follow a curved road.
6. Brake reliably.
7. Reverse.
8. Park.
9. Exit without clipping into geometry.
10. Reenter and repeat.

Test multiple cycles and report actual behavior.

---

# 8. REPLACE DEVELOPMENT HUD WITH PRODUCTION MOBILE UI

Inspect `HarborlightHud.cs`.

Gradually migrate player-facing IMGUI panels to a maintainable Unity UI system appropriate for runtime Android and iOS, using uGUI or runtime UI Toolkit where verified suitable.

Required UI:

- Character HUD.
- Joystick and touch actions.
- Mini-map.
- Full-screen map.
- Vehicle dashboard.
- Mission objectives.
- Inventory.
- Garage.
- Bank.
- Character profile.
- Settings.
- Social interactions when implemented.

Design a consistent visual system:

- Clear typography.
- Proper iconography.
- Responsive positioning.
- Safe-area handling.
- Accessible touch-target sizes.
- Consistent component states.
- Smooth panel transitions.
- Minimal screen obstruction.

Keep internal development diagnostics separate from the production player interface.

The interface must be demonstrated on mobile-resolution captures.

No dead buttons or decorative controls counted as working functionality.

---

# 9. MAKE THE CITY INTERACTIVE

The current game already has jobs and economy foundations.

Connect those systems to the polished world.

Required playable journey:

1. Start with a new character.
2. Walk around North Pier.
3. Speak to a job NPC.
4. Accept a delivery assignment.
5. Locate the required item.
6. Enter an available vehicle.
7. Drive to the destination.
8. Complete the delivery.
9. Receive a validated reward.
10. Visit the garage or store.
11. Purchase or equip an authorized item.
12. Save or persist progress.
13. Restart or reconnect.
14. Confirm progression is retained.

All relevant UI, animation, backend, and world interactions must operate as one game.

Do not create fake interactions that only display a successful message.

---

# 10. ARCHITECTURE RECOVERY

Refactor the oversized bootstrap incrementally.

Extract responsibilities into clear modules, for example:

- World composition and streaming.
- District scene management.
- Lighting and environmental setup.
- Character spawning and presentation.
- Camera configuration.
- Vehicle spawning and presentation.
- NPC placement.
- Interaction registration.
- Game session and lifecycle.
- Gameplay UI composition.

These are proposed architectural boundaries, not instructions to create empty abstractions.

Move working behavior only when the affected tests and scene results can be preserved.

Prefer serialized prefabs and authored scenes for artist-controlled objects.

Keep existing public contracts stable where possible.

Avoid a large rewrite that stops all development progress.

### Repository reproducibility

The main Unity repository currently references a neighboring admin-SDK package.

Identify the exact dependency versions and ensure a clean checkout can restore or build with a documented, reproducible dependency process.

Also document the separately maintained backend repository and its version compatibility.

Never copy private secrets or deployment credentials into GitHub.

---

# 11. PERFORMANCE AND DEVELOPMENT SPEED

## Runtime performance

Profile on actual hardware.

Record:

- Frame time.
- FPS distribution.
- CPU main-thread time.
- GPU time where available.
- Memory consumption.
- Asset loading spikes.
- Draw calls.
- Texture memory.
- Physics workload.
- Garbage collection.
- UI update cost.
- Network replication cost when applicable.

Target stable 30 FPS on the chosen baseline Android device and 60 FPS on suitable higher-tier hardware, subject to actual measurement.

Apply evidence-driven optimizations.

Examples:

- Reduce avoidable runtime GameObject creation.
- Profile the synchronous world creation performed during bootstrap.
- Stream distant content appropriately.
- Reduce always-resident collision objects where safe.
- Pool recurring objects.
- Optimize material and texture memory.
- Avoid excessive IMGUI work.
- Batch static geometry appropriately.
- Use LOD and culling.
- Eliminate unnecessary frame allocations.

## Development speed

Stop repeated long Unity sessions that do not deliver a verified improvement.

Use:

- Focused source modifications.
- Incremental compilation.
- Targeted tests.
- Short gameplay QA sessions.
- Automated screenshots.
- Reusable test scenes.
- Clear dependency boundaries.
- Minimal repetitive documentation.
- Checkpoint-based work.

Respect the host's thermal and disk-space constraints. Verify remaining storage before large imports and builds.

Do not delete existing user files, saved progress, or valuable build artifacts without authorization.

Avoid leaving unused Unity processes running.

---

# 12. FULL GAME ROADMAP AFTER THE QUALITY GATE

The eventual goal is not limited to the polished district.

After the character, driving, city, mission, UI, and Android-quality gates pass, continue implementing the remaining full-game systems:

### Phase A — Police and emergency services

Duty system, uniforms, vehicle permissions, department roles, dispatch, traffic enforcement, arrest/custody, reports, and progression.

### Phase B — Gangs and faction gameplay

Membership, permissions, rivalries, territories, reputation, safe zones, and validated combat.

### Phase C — Properties and business ownership

Homes, garages, stores, ownership, upgrades, revenue, transfers, and server-authoritative economic settlement.

### Phase D — Social world

Friends, chat, player interactions, invitations, moderation, reporting, and blocking.

### Phase E — Multiplayer world expansion

Authoritative vehicle synchronization, network interest management, connected job and economy state, reconnect recovery, multi-client testing, and measured concurrency scaling.

### Phase F — City expansion

Extend the world district-by-district only when the existing polished art, gameplay, and performance standards can be maintained.

### Phase G — Release hardening

Save recovery, security, multiplayer load tests, physical-device QA, release signing, privacy requirements, and Android/iOS store preparation.

Do not mark a phase complete without the acceptance tests relevant to that phase.

---

# 13. STRICT COMPLETION EVIDENCE

For every major improvement record:

- Git commit SHA.
- Changed systems.
- Build result.
- Automated test result.
- Gameplay test result.
- Screenshot or video where visual behavior matters.
- Observed performance.
- Remaining defects.
- Next highest-priority task.

Use these states:

`NOT_STARTED`\
`IN_PROGRESS`\
`IMPLEMENTED_UNVERIFIED`\
`VERIFIED`\
`FAILED`\
`BLOCKED`\
`DATA_UNKNOWN`

A feature may only become VERIFIED when the appropriate test actually passes.

### Visual quality gate

The polished district must have:

- Coherent architecture and materials.
- Appropriate environmental density.
- No major visible primitive placeholders.
- No obviously floating or sunken buildings.
- No repeated severe collision defects.
- Correct lighting.
- A credible humanoid character.
- Smooth animation.
- Visually complete primary vehicle.
- Readable production-style UI.
- Actual gameplay footage demonstrating these features.

The goal is visual coherence and playability, not falsely claiming an objective percentage of similarity to OneState.

### Gameplay gate

A full gameplay session must demonstrate:

- Movement.
- Camera.
- Interaction.
- Entering a vehicle.
- Driving.
- Mission completion.
- Rewards.
- Inventory or garage use.
- State persistence.
- Safe recovery from known failure cases.

### Device gate

Install and launch on actual Android hardware, exercise touch controls, capture crashes and performance measurements, and verify app suspend/resume behavior.

If no device is available, explicitly mark device verification BLOCKED.

---

# 14. PERSISTENT AUTONOMOUS EXECUTION

Maintain these existing project files:

- `GOAL.md`
- `WORKING-GOAL.md`
- `STATUS.md`
- `CHECKPOINT.json`
- `docs/FEATURE_MATRIX.md`
- `docs/TEST_PLAN.md`

Do not repeatedly rewrite these files in place of development.

At each checkpoint record:

- Current phase.
- Last verified commit.
- Current defect or feature.
- Completion evidence.
- Next executable task.
- Known blockers.
- Required test commands and environments.

When a Codex session resumes:

1. Read the checkpoint.
2. Confirm repository state.
3. Confirm which tasks are already verified.
4. Continue the highest-priority unfinished task.
5. Do not repeat completed work unless regression evidence requires it.

Do not bypass approval or resource constraints by launching an uncontrolled background loop.

If execution can no longer continue due to a session boundary or external blocker, leave a precise resumable checkpoint and report the condition.

---

# 15. FIRST REQUIRED EXECUTION — DO THIS NOW

Start with the following concrete tasks in order.

**Task 1: Preserve baseline**

Create a quality-recovery branch and verify source integrity, dependencies, and working project state.

**Task 2: Capture the existing experience**

Launch the current Mac development build using the shortest safe workflow. Capture the live player, vehicle, world, and HUD at fixed locations.

**Task 3: Identify the worst three defects**

Use actual gameplay evidence and code analysis to rank the three problems most responsible for the low-quality experience.

**Task 4: Implement the highest-impact correction**

Fix the most important defect using actual Unity source changes and usable assets. Do not produce only a plan.

**Task 5: Run and evaluate**

Build the game, demonstrate the correction, run relevant tests, and compare with the baseline.

**Task 6: Commit and continue**

Record verified results and immediately select the next highest-impact issue.

Repeat the process through the quality gate, then continue the remaining full-game roadmap.

---

# FINAL SUCCESS DEFINITION

Harborlight is complete only when:

- It looks and feels like a coherent commercial-quality original mobile roleplay game.
- Its core character and driving controls are polished.
- Its primary districts are visually finished.
- Its important buildings and activities are meaningfully interactive.
- Its careers, property, businesses, economy, and social systems work.
- Its intended multiplayer interactions are authoritative and persistent.
- Its supported Android and iOS targets have been tested.
- Performance meets measured supported-device targets.
- Security, persistence, and recovery tests pass.
- Remaining known release blockers are resolved.
- Final playable builds and reproducible source are delivered.

**Never convert unfinished work into a success claim.**

Do not trade visual quality for more empty map area.

Do not trade gameplay stability for additional menus.

Do not trade actual implementation for documentation.

**BEGIN WITH THE EXISTING PROJECT, FIX ITS QUALITY, VERIFY EVERY IMPROVEMENT, AND CONTINUE THROUGH CHECKPOINTED EXECUTION UNTIL THE DEFINED GOAL IS SATISFIED OR A REAL BLOCKER IS REACHED.**

## D. How I would prioritize the first improvements

ORDER OF EXECUTION

1. Replace prototype player and vehicle presentation with polished, functioning gameplay assets.
2. Fix the third-person camera, animation transitions, and car handling.
3. Complete the visual design of the existing North Pier district instead of creating more empty streets.
4. Replace the development HUD with proper mobile controls and gameplay interfaces.
5. Test the integrated gameplay loop, then optimize the actual Android build.

One limitation matters: Codex cannot be guaranteed to execute indefinitely across session limits, machine shutdowns, or exhausted resources. The goal above requires it to keep implementing within an active session and save a precise checkpoint for resumption. That is the reliable approach to sustained development.

Your existing game does have useful foundations. I would keep the project and replace weak implementations incrementally, rather than order another complete rewrite that could discard the working networking, persistence, and backend integration.