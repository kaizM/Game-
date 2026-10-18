# Harborlight production goal

Build Harborlight as an original, Android-first 3D open-world roleplay game, with iOS as a supported platform. The OneState reference is limited to high-level genre and system categories. Harborlight must use its own source, world, characters, vehicles, writing, interface, name, and networking design.

## Completion target

Deliver a reproducible Unity project, an authoritative multiplayer service and database, an installable Android build, documented iOS support, tested connected gameplay, licensed/original assets, and release evidence. The player path should connect character creation, exploration, a job, driving, earnings, progression, property/business ownership, and social play.

## Milestones

| Milestone | Goal |
| --- | --- |
| M0 | Verify project, reference, tools, constraints, and evidence ledger. |
| M1 | Isolated version control, reproducible client/service builds, local development environment, migrations, and health checks. |
| M2 | One visually developed, touch-playable district with map/navigation, character controls, local profile, and safe save migration. |
| M3 | Owned vehicles, driving, garage, persistence, and tested entry/exit. |
| M4 | Server-authoritative multiplayer with two real clients, synchronization, and reconnect. |
| M5 | Jobs, inventory, bank/economy, and persistent verified rewards. |
| M6 | Police and emergency careers with permissions and complete workflows. |
| M7 | Factions, gangs, combat, safe areas, and moderation protections. |
| M8 | Homes, businesses, ownership, revenue, and audited transfers. |
| M9 | Social presence, friends, chat, groups, reporting, and blocking. |
| M10 | Expand districts and replace tracked prototype assets with original or cleared production assets. |
| M11 | Security, restore, concurrency, device, thermal, and regression hardening. |
| M12 | Android release candidate, store preparation, privacy disclosures, and release readiness evidence. |

## Working rules

- Keep feature status evidence-based: `NOT_STARTED`, `IN_PROGRESS`, `BLOCKED`, `IMPLEMENTED_UNVERIFIED`, `VERIFIED`, `FAILED`, or `DATA_UNKNOWN`.
- Keep world state local only until the authoritative multiplayer milestone is implemented; never present the current API as a multiplayer server.
- Keep money, inventory, property, combat, role permissions, and important progression server-authoritative once networking is added.
- Preserve existing project/save data and avoid touching unrelated folders.
- Treat REA observations as bounded reference evidence, not as source material. Do not extract or reuse OneState code, maps, textures, audio, UI, names, or backend design.
- Do not claim device performance, store readiness, or feature completion without the corresponding test evidence.

See `docs/REFERENCE_AUDIT.md`, `docs/FEATURE_MATRIX.md`, and `STATUS.md` for the verified baseline and next steps.
