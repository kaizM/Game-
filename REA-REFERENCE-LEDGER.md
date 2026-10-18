# REA reference ledger

This ledger keeps OneState observations separate from Harborlight design decisions. It uses package-level REA evidence and the existing report at `../one-state-analysis/code-structure.md`; it does not use OneState gameplay code, maps, art, UI, names, or networking as Harborlight implementation material.

## Current OneState package inspected (2026-10-17)

- The running app used a temporary wrapper path, and its executable hash matched the installed nested app at `/Applications/OneState.app/Wrapper/OneState.app/OneState`. REA identified that executable as arm64 Mach-O, SHA-256 `5ac3753f2d47527d1979db82d65e967c660516ddfce9e8e0cbfff6c972c7a03b` (Evidence `ev_4a5a347d02e52ce3ec4d36ea8d23e1805b8b87830c915a48de7c4472a3f2198d`).
- The nested app's `Info.plist` identifies bundle `com.Chillgaming.oneState`, version 1.3.1 / build 18088, iOS platform, landscape orientation, and Unity version metadata 2022.3.76f1 (Evidence `ev_20bc1dc06bbb11562f428bc0b70c6717f88941d54c65f052a754f40513d6eaba`).
- REA's Mach-O pass also records an arm64 iOS executable with minimum OS 16.0, SDK 26.5, and an `LC_ENCRYPTION_INFO_64` record whose `cryptid` is 1 (Evidence `ev_4a5a347d02e52ce3ec4d36ea8d23e1805b8b87830c915a48de7c4472a3f2198d`). This is a binary-header observation; it does not establish that every game component is encrypted or recover gameplay source. The plist also declares microphone use for in-game voice chat, remote notifications, tracking/ad-personalization settings, and Facebook/AppsFlyer-related integrations. Those are package declarations, not proof of runtime calls.
- REA's complete read-only inventory of `/Applications/OneState.app/Wrapper/OneState.app` covers 6,304 content nodes, 6,356 path occurrences, and 6,355 edges, with no integrity contradictions (summary Evidence `ev_0f145c9280104be33c57c2cbad703bc1f04198d1d67fb4b775e5f10faaee107d`; retained parent `ev_49218e20a7eaf3be7dfde769846843a5a857cecf3cac0194d880f400072ca7a0`; inventory Evidence `ev_df289883335c840219d666fd43932d65b662c5831763bec7879a37f8502d236c`). The inventory includes `Data/Managed/Metadata/global-metadata.dat` (32,678,080 bytes) and many hashed `.bundle` files under `Data/Raw/aa/iOS`; those paths do not establish the bundles' decoded content.
- The app contains `Frameworks/UnityFramework.framework/UnityFramework`. REA's Mach-O inspection of that large binary exceeded its 60-second limit. No function bodies or decoded scene/map data were recovered in this pass. Ghidra is unconfigured, Hopper is absent, and IDA is unconfigured.
- The running app's `OneState` executable matched the installed bundle byte-for-byte at SHA-256 `5ac3753f2d47527d1979db82d65e967c660516ddfce9e8e0cbfff6c972c7a03b`. REA verified its signature as bundle `com.Chillgaming.oneState`, team `HKZ246KUJ5`, under Apple iPhone OS signing authorities (Evidence `ev_b6e31b40bf102b4b86b344fa0d941314dea140ec92cf0f04ec504989e638b6d9`).
- A live CUA screenshot showed the in-world tutorial `A DRIVE FOR BABY` / `Talk to Anne`, with a visible `Keep listening` dialogue button. The scene includes a third-person avatar and NPC, a detailed street/building facade, minimap, needs bars, chat/moderation notices, and store/currency/task controls. The click action failed with `noWindowsAvailable`; Return and W left the frame unchanged. Rendering was verified, but movement, driving, mission progress, and saving were not.
- A fresh observation of the same running session showed the dialogue overlay expanded: Anne recommends buying a ride to travel around the city. The player remains facing her; the persistent mission card and map/chat/moderation/task/store/currency HUD remain visible behind the dialogue. This adds visible tutorial copy and panel layering to the observations; it does not reveal mission logic or vehicle handling.
- REA reopened the running wrapper executable as arm64 Mach-O, SHA-256 `5ac3753f2d47527d1979db82d65e967c660516ddfce9e8e0cbfff6c972c7a03b`. Its current session reports Ghidra unconfigured, Hopper missing, and IDA unconfigured. `observe_native_ui` could not bind the Unity window because Accessibility geometry returned no matching AX window for the selected CG bounds. CUA still captured the screenshot, but coordinate clicks returned `noWindowsAvailable`; Return and E did not visibly advance the dialogue. The game stayed open and no store, chat, reward, or account controls were used.

## Earlier saved package analysis (historical)

- A prior snapshot identified `/Applications/OneState.app/Wrapper/OneState.app/OneState` as an arm64 Mach-O app executable with SHA-256 `56ac0533fdbcffa1b81f5db628d1fecdc8be6ea5b9ba2dac486702d7d2e81381`. That digest differs from the build inspected above; treat its findings as a separate historical package version. The bundle has a separate `UnityFramework.framework/UnityFramework`, but its code was not analyzed successfully in either pass.
- The current read-only package inventory covers `/Applications/OneState.app` (REA Evidence `ev_516de4b820e79e9309432d8a0eb9753b015ac092c7a4507e544ea7beb3688c01`; root SHA-256 `f40f471113593ff4d72df9eb1dc4f5be08ba6ca8f6cee2dc67112c1a1e02ac71`): 6,308 unique content nodes and 6,361 path occurrences, with no integrity contradictions. This confirms package structure only.
- The saved Unity metadata report describes the older package as Unity 2022.3.62f2 with an IL2CPP-style native runtime, Addressables content, and preserved type names spanning vehicle, pedestrian, quest, shop, customization, and UI categories. Those type names are declarations, not gameplay implementations, and do not establish that the newer inspected build has identical code or content.
- The prior runtime observation in that report reached a server-selection page after downloading remote content. It does not expose server behavior or the game’s full world.

## Inferences for Harborlight

- Keep Harborlight’s own gameplay code organized into focused systems for vehicles, jobs, interaction, customization, and HUD. Use data-driven content boundaries where they help patching and asset loading.
- Spend the visual budget on an original art direction, readable vehicle silhouettes, quality materials, lighting, and consistent environmental detail. The reference supports these as relevant system categories; it does not provide reusable art or source.

## Unknown or out of scope

- The package inventory does not decode OneState method bodies, scene graphs, catalog keys, full map data, server APIs, or runtime call relationships. `global-metadata.dat` and Addressables bundles remain outside this bounded reference pass.
- Harborlight will not use extracted OneState assets, source, maps, UI, names, or network design. Any further REA pass needs a specific technical question and should report observed facts, inferences, and limits separately.

## Harborlight visual work

- Original generated base-color textures are stored under `Assets/Resources/Art/Textures/` and are used only by Harborlight materials.
- Vehicle visual improvements are made in Harborlight’s own procedural model code; no OneState vehicle mesh or texture is used.
