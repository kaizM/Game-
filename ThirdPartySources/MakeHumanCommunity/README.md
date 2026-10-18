# Harborlight character source

- Editable profile: `HarborlightResident-Male.mhm` (adult male, moderate build, 179 cm). The profile references the selected clothes, shoes, and hair by their MakeHuman proxy UUIDs.
- Preserved neutral starter: `HarborlightResident-Base.mhm`.
- Selected MakeHuman system assets required to reopen the profile are under `assets/`; the asset files retain their CC0 notices.
- Tool: MakeHuman Community 1.2.0. The official license page says core graphical assets and exported models are CC0. MakeHuman application code has separate AGPL terms and is not included in this project.
- License: https://static.makehumancommunity.org/about/license.html
- Unity FBX export guide (GameEngine rig): https://static.makehumancommunity.org/makehuman/docs/saving_models_for_unity_and_how_to_import_them_there.html
- Harborlight-authored profile: gender set to male, age 25, muscle 58%, weight 97%, height 179.27 cm, mixed ethnicity slider weights.
- Current source textures for shirt, jeans, shoes, hair, skin, and eyes are bundled with the selected MakeHuman CC0 assets.
- Exported model: `Assets/Resources/Characters/HarborlightResident/HarborlightResident.fbx`, with six linked textures beside it. Unity imported a valid Humanoid Avatar and five skinned mesh parts (31,288 combined vertices; 42 body bones). The linked textures are 1K or 2K.
- The exported model is an original Harborlight profile made from MakeHuman CC0 graphical assets. The model audit and runtime prefab do not use OneState code or assets.
