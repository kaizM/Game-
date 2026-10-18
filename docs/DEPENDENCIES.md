# Dependencies and development tools

## Verified local toolchain

| Component | Current observation |
| --- | --- |
| Unity editor | Project pins `6000.5.4f1`; Hub also has `6000.3.20f1` |
| URP | `17.5.0` |
| Xcode | `26.5` / build `17F42` |
| Android SDK | Platforms `android-34`, `android-36`; Build Tools `34.0.0`, `36.0.0`; NDK `28.2.13676358` |
| Android Java | Unity-bundled OpenJDK at `PlaybackEngines/AndroidPlayer/OpenJDK`; no system `java` is on the current shell path |
| .NET | SDK `10.0.400`, runtime `10.0.11` |
| PostgreSQL client | `psql` `16.14`; API database uses PostgreSQL via Npgsql |
| Docker | CLI `29.8.2`; Compose subcommand unavailable; daemon not checked |
| REA | MCP/server `rea-agents` `6.4.0`, skill version 40; Ghidra `12.1.4` currently available |
| Git | Harborlight has an isolated repository at `/Users/admin/Desktop/RP game/harborlight-city`, on `codex/harborlight-production`, with baseline and district commits. The related services and shared SDK do not have separate Git metadata. |

## Project packages

- `com.unity.render-pipelines.universal`: `17.5.0`
- `com.unity.multiplayer.center`: `1.0.1` (editor center only; not a realtime networking implementation)
- `com.unity.netcode.gameobjects`: `2.13.3` (pinned for the first client-server slice; the package declares Unity Transport `2.6.0`)
- Unity Transport: `6.5.0` (resolved from the Unity `6000.5.4f1` editor's built-in package set)
- `com.meridian.game-admin`: local package reference `file:../../game-admin-sdk`
- Unity built-in modules are managed by the pinned editor.

### Multiplayer dependency decision

Use Unity Netcode for GameObjects `2.13.3` for the initial M4 client-server implementation. Its package metadata declares Unity Transport `2.6.0`; the `6000.5.4f1` editor resolves the package to its built-in Unity Transport `6.5.0`. Unity's repository lists Unity 6, iOS, and Android targets, and the package metadata requires Unity `6000.0` or later. The package supports dedicated-server and listen-server models. Its Unity Companion License restricts use to Unity-dependent projects; Harborlight meets that condition. Pin the last 2.x release rather than 3.x for this first slice because 3.0 introduced a hard Netcode for Entities dependency that the current GameObject/MonoBehaviour project does not use. No Relay, matchmaking, or paid hosted service is selected; the first network test will use direct local transport. Internet connectivity, NAT traversal, and hosted capacity remain future deployment decisions.

Primary sources: [NGO 2.13.3 package metadata](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/v2.13.3/com.unity.netcode.gameobjects/package.json), [NGO 2.13.3 release notes](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases), [NGO license](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/v2.13.3/LICENSE.md), [Unity dedicated-server build guide](https://docs.unity3d.com/6000.0/Documentation/Manual/dedicated-server-build.html), [NGO server model FAQ](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects%402.6/manual/learn/faq.html).

## Release-channel gate

The project is already working on `6000.5.4f1`; avoid an in-place engine change during the first verification pass. Unity's current support page lists 6.3 LTS through December 2027 and 6.0 LTS through October 2026. Before production lock-in, compare a copied project under the supported LTS line and record compile, asset-import, Android, iOS, and runtime results. [Unity release support](https://unity.com/releases/unity-6/support)

## Missing/unverified environment pieces

- Docker Compose is absent; local instructions use `docker run`. Docker daemon/image availability is not yet checked.
- System Java is absent, but the Unity Android module's bundled JDK exists.
- Android signing identity, iOS provisioning, production OIDC tenant, billing credentials, ad account, Azure subscription/region, and store accounts are not supplied or verified. Do not place secrets in Unity assets or builds.
