# Third-party asset register

## Quaternius Cars Pack

- Creator: Quaternius (LowPoly Models).
- Source: https://quaternius.com/packs/cars.html (download folder linked by the creator).
- License: CC0 1.0 Universal; the included license notice is preserved under `ThirdPartySources/QuaterniusCarsPack/License.txt`. The creator’s page says the pack is free for personal and commercial projects.
- Acquired: 2026-10-16.
- Platforms: CC0 permits use on iOS, Android, and other platforms; no attribution is required.
- Credit (optional): “Quaternius — Cars Pack (CC0 1.0).”
- Source archive: `ThirdPartySources/QuaterniusCarsPack/FBX.zip`.
- Imported files: `Assets/Resources/VehicleModels/*.fbx` (Taxi, Cop, SUV, NormalCar1, NormalCar2, SportsCar, SportsCar2).
- Modifications: source FBX files are unchanged. Unity runtime wrappers, material assignment, and paintability are project-authored.

## Poly Haven Harborlight PBR surfaces and coastal lighting

- Creator/source: Poly Haven; assets are released under CC0 1.0.
- Acquired: 2026-10-16.
- Platforms: CC0 permits use on iOS, Android, and other platforms. Poly Haven requests a visible credit when its live API is integrated into a product; these files were downloaded directly for this project, and the game credit is still included as a courtesy.
- Assets: [Asphalt 03](https://polyhaven.com/a/asphalt_03), [Brick Wall 001](https://polyhaven.com/a/brick_wall_001), [Concrete Wall 009](https://polyhaven.com/a/concrete_wall_009), [White Stucco 02](https://polyhaven.com/a/white_stucco_02), and [Docklands 01 HDRI](https://polyhaven.com/a/docklands_01).
- Authors: Dario Barresi and Charlotte Baglioni (Asphalt 03); Rob Tuytel and Dimitrios Savva (Brick Wall 001); Charlotte Baglioni (Concrete Wall 009); Dimitrios Savva (White Stucco 02); Savva Zakharov (Docklands 01).
- Imported files: 4K diffuse and DirectX normal maps in `Assets/Resources/Art/Textures/PolyHaven/`; the Docklands 01 HDR panorama and its former skybox material are archived under `Assets/Art/SourceArchive/PolyHaven/` and are excluded from player builds. The prior Red Hill Cloudy panorama is also retained in that source archive and is not used by the game.
- Additional playable-ground surfaces: [Grass Ground](https://polyhaven.com/a/grass_ground), [Coast Sand 05](https://polyhaven.com/a/coast_sand_05), and [Pavement 05](https://polyhaven.com/a/pavement_05). Authors: Charlotte Baglioni (Grass Ground); Dario Barresi and Rob Tuytel (Coast Sand 05); Jan Burghardt (Pavement 05). Their 2K diffuse and OpenGL normal maps are stored in `Assets/Resources/Art/Textures/PolyHaven/`; Unity flips the OpenGL normal green channel during import.
- Import settings: downloaded PBR textures keep their source resolution on desktop; mobile imports are capped at 2K and the HDR panorama at 1K. The materials, terrain mesh, coastal blend, paver placement, and environment setup are Harborlight-authored.
- Attribution: “Surface materials and coastal environment from Poly Haven (CC0 1.0).”

## Harborlight-authored North Pier fixtures

- Source code: `Assets/Scripts/HarborlightBootstrap.cs` (`CreateNorthPierPromenade` and its fixture builders).
- The plaza's pavement material uses the Poly Haven CC0 surface listed above. Benches, market stall geometry, planter troughs, and pier guardrails are assembled from Harborlight-authored Unity primitives and project materials; they are not copied from third-party art or OneState.
- These fixtures are prototype geometry and need higher-quality production meshes, collision review, and mobile-device profiling.

## Modular district architecture

- Creator: James Ray Cock.
- Sources: [Modular Urban Apartments Facade](https://polyhaven.com/a/modular_urban_apartments_facade) and [Modular Factory Facade](https://polyhaven.com/a/modular_factory_facade).
- License: CC0 1.0 Universal for both models; no attribution is required.
- Acquired: 2026-10-17.
- Imported source: 2K FBX and matching texture maps in `Assets/Art/Models/PolyHaven/ModularUrbanApartmentsFacade/` and `Assets/Art/Models/PolyHaven/ModularFactoryFacade/`.
- Runtime prefabs: `Assets/Resources/Art/Buildings/HarborlightApartmentBlock.prefab` and `HarborlightFoundryBlock.prefab`; the mesh selections, four-sided assembly, colliders, and roof are Harborlight-authored.
- The prefab generator uses the assets' URP materials and normal maps. Mobile receives source 2K textures; device performance and texture memory still need measurement.
- Credit (optional): “Architecture models and PBR textures by James Ray Cock via Poly Haven (CC0 1.0).”

## Downtown city buildings

- Creator: Quaternius.
- Source: [Downtown City MegaKit](https://quaternius.com/packs/downtowncitymegakit.html); the Standard free pack is provided under CC0 1.0 Universal.
- Acquired: 2026-10-18. The included license notice is preserved at `ThirdPartySources/QuaterniusDowntownCity/`. The 234 MB source archive remains in the local project but is excluded from GitHub; its imported FBX models and textures are included under `Assets/Art/Models/QuaterniusDowntownCity/`.
- Imported models: `Building_Small_1`, `Building_Medium_2_001`, and `Building_Large_2` from the Unity FBX export. Their referenced brick, trim, concrete, marble, and interior textures are stored beside the source exports under `Assets/Art/Models/QuaterniusDowntownCity/`.
- Runtime prefabs: `Assets/Resources/Art/Buildings/HarborlightBuilding_Small_1.prefab`, `HarborlightBuilding_Medium_2_001.prefab`, and `HarborlightBuilding_Large_2.prefab`. Harborlight normalizes each model to the streamed-building footprint, centers it, and aligns its lowest rendered point with the foundation top. Original FBX geometry and texture maps are unchanged.
- Import settings: 4K maximum for desktop, mipmapped compressed textures, and 2K platform overrides for Android and iPhone.
- Credit (optional): “Downtown City MegaKit by Quaternius (CC0 1.0).”

## Harborlight resident character

- Creator/tool: MakeHuman Community 1.2.0; the adjustable profile and final appearance were authored for Harborlight.
- License: MakeHuman’s official license page identifies its core graphical assets and exported models as CC0. The selected hair, suit, shoes, and eyes retain the CC0 notices recorded in `ThirdPartySources/MakeHumanCommunity/README.md`. The MakeHuman application code is AGPL and is not included in Harborlight.
- Source profile: `ThirdPartySources/MakeHumanCommunity/HarborlightResident-Male.mhm`.
- Imported model and textures: `Assets/Resources/Characters/HarborlightResident/`. Unity verified a valid Humanoid Avatar, five skinned mesh parts, 31,288 combined vertices, and six 1K/2K textures.
- The resident uses the original CC0 MakeHuman suit diffuse and normal maps. Runtime applies a subtle Harborlight tint to the imported suit material; it does not replace the garment atlas.
- Locomotion source: Quaternius Universal Animation Library 2, CC0 1.0; archive and license are preserved in `ThirdPartySources/QuaterniusUAL2/`. Harborlight uses its idle and carry-walk clips retargeted through Unity Humanoid.
- Scope: one original resident model is reused for the player and named NPCs, with Harborlight-authored clothing tint variation. Additional body types, outfits, faces, and performance art passes remain necessary before production.
- References: [MakeHuman license](https://static.makehumancommunity.org/about/license.html); [MakeHuman Unity export guide](https://static.makehumancommunity.org/makehuman/docs/saving_models_for_unity_and_how_to_import_them_there.html).
