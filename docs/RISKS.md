# Risks and blockers

| Risk | Status | Mitigation / next check |
| --- | --- | --- |
| Durable progression and production networking are incomplete | `OPEN / HIGH` | Guest identity is bound before spawn. All three mission chains now pass one-client Unity-to-API-to-PostgreSQL loopback progression with idempotent receipts. Test restoration after the complete chain, multi-client action races, and connect rewards to wallet/inventory before treating M5 as complete. Direct transport remains loopback-only until secure remote transport is configured. |
| Existing slice is single-player and uses local credits | `OPEN / HIGH` | Keep offline saves separate from future authoritative wallet/inventory; design explicit reconciliation. |
| Expanded city has no large-world multiplayer scaling | `OPEN / HIGH` | Current source generates a 6,000 × 4,116 m (24.70 km²) map with 1,200 outer building sites and 36 entry zones. The 6.00 × 4.12 km HUD map and 900-coordinate spawn table are smoke-tested; the authenticated two-client avatar-visibility run used the previous 14.24 km² build. Roads, terrain, and all collision boxes remain resident; only player avatars are networked and distance-filtered. Verify long-route traversal, live later-client spawn allocation/collisions, shell unload, expanded-map multi-client movement, remote transport, and device/server load before setting a player-count target. |
| Current city and props are largely procedural primitives | `OPEN / HIGH` | Track prototype content; clear/commission original architecture, environment, character, and vehicle assets before production claims. |
| Harborlight engine version `6000.5.4f1` is not the listed current LTS line | `OPEN / MEDIUM` | Preserve current builds; compare on a copy against Unity's supported 6.3 LTS before production lock-in. |
| No Android/iOS device verification | `OPEN / HIGH` | Build ARM64 APK and run on real devices before claiming install/play/performance acceptance. |
| Save migration has no automated coverage | `OPEN / HIGH` | Create deterministic migration tests and protect real user saves during QA. |
| Purchases, refunds, Apple/Google identity, and ads are not production configured | `OPEN / HIGH` | Keep purchase route fail-closed; add platform verification, privacy consent, idempotency, and rollback tests before enabling. |
| Admin service is not realtime game administration | `OPEN / MEDIUM` | Keep server availability controls separate from player/game-world mutations; add typed actions only with authoritative server handlers. |
| Shared admin SDK Git history | `OPEN / MEDIUM` | Unity and the Harborlight service have isolated Git histories; the shared admin SDK root is not a Git repository. Add its source to version control before production release work. |
| Mac disk pressure and heat | `OPEN / MEDIUM` | The current disk has 19 GiB free at 96% capacity. The 548 MB map-review build was kept selective; its 30 FPS capture run was closed immediately after review and macOS recorded no thermal/performance warning. Keep future builds selective and remove only confirmed disposable build artifacts. |
| “Replica” capability requested but not installed | `DATA_UNKNOWN` | No matching skill/provider was found. Continue without claiming it. |
| Commercial title/name availability | `DATA_UNKNOWN` | Check trademark/store availability before naming a release candidate. |
| Store signing and privacy | `NOT_STARTED` | Obtain approved signing/provisioning and complete platform data/privacy disclosures before release. |
