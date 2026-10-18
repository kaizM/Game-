# Harborlight visual direction

## Target

Give North Pier the feel of a lived-in working waterfront: muted coastal greens, weathered red brick, pale poured concrete, dark aggregate asphalt, blue-green harbor water, and a distinctive cream-and-teal taxi. Use believable material response and readable vehicle shapes while keeping mobile performance in view.

## Original art created for Harborlight

The following base-color images were generated specifically for Harborlight and are not extracted from OneState. They remain as fallback or secondary-use textures; the playable ground now uses the Poly Haven PBR set below:

- `Assets/Resources/Art/Textures/Harborlight-Asphalt.png` — repeatable road base color.
- `Assets/Resources/Art/Textures/Harborlight-Brick.png` — repeatable warm masonry base color.
- `Assets/Resources/Art/Textures/Harborlight-Grass.png` — repeatable coastal ground base color.
- `Assets/Resources/Art/Textures/Harborlight-Concrete.png` — repeatable paving base color.
- `Assets/Resources/Art/Textures/Harborlight-Water.png` — repeatable blue-green water base color.
- `Assets/Art/Concepts/Harborlight-Taxi-Concept.png` — original car proportion/color reference; it is concept art, not an in-game model.
- `Assets/Art/Concepts/Harborlight-Vehicle-Lineup-Concept.png` — original four-vehicle palette and silhouette sheet (hatchback, utility pickup, sport coupe, delivery van); it is concept art, not an in-game model.

The playable district has a collision-backed coastal ground mesh, a shaped shoreline, sand between land and water, a graded inland area, a generated road grid, textured raised sidewalks, and timber pier supports. Fourteen landmark building shells use collidable Unity prefabs assembled from two Poly Haven CC0 modular facade sets. Their URP materials include albedo and normal maps. The prefab layouts, roof caps, and collision volumes are Harborlight-authored; buildings are still closed shells with no interiors, the façades repeat two styles, and most blocks remain open. The current 208-by-196-meter playable district is still a sparse blockout rather than a finished city map. Grass Ground, Coast Sand 05, and Pavement 05 add 2K diffuse and normal maps to terrain and walkways; Unity flips the OpenGL normal-map green channel at import. Roads and general masonry use the project's 4K Poly Haven PBR textures, with mobile import caps. Dedicated roughness and ambient-occlusion handling still needs a complete material audit. The concept images guide palette and proportions; they are not runtime models. Garage indices 0, 1, 3, 4, 5, and 6 use low-poly Quaternius CC0 meshes. The market delivery van (index 2) and grand tourer (index 7) are still procedural Harborlight shapes. Vehicles have no separate interiors or wheel-animation controller yet, so the fleet is licensed prototype art, not finished realistic car art. See `Assets/Art/THIRD-PARTY-ASSET-REGISTER.md` for the exact source assets.

The ground mesh, shore, sidewalks, and pier tops use Poly Haven CC0 grass, sand, and pavement maps. Road and general masonry use the project's Poly Haven CC0 4K diffuse and DirectX normal maps. Building facades use the two imported 2K CC0 modular sets. Runtime uses Unity's procedural sky shader with Harborlight-authored colors; the Docklands HDRI and its old panoramic material are archived outside `Resources` and are excluded from player builds. The desktop Ultra tier has a separate URP asset with 4x MSAA, four shadow cascades, a 4096 px main-light shadow map, and a 120 m shadow distance. Mobile stays on its Medium tier with 2K PBR maps. A restrained ACES grade and color adjustment run on both targets; bloom is desktop-only. Exact sources and files are listed in `Assets/Art/THIRD-PARTY-ASSET-REGISTER.md`.

The open corridor from Lantern Square to North Pier now has a collidable PBR-paved plaza with four benches, two raised planters, a small market stall, and guardrails along the north pier. The paving uses the existing licensed CC0 surface; the fixture geometry is authored from Unity primitives in `HarborlightBootstrap`. These fixtures are still blockout-quality prototypes; the rendered pass reduces one large bare-ground gap but does not satisfy the production-art gate.

## Production art gates

- Fill and refine the current 208-by-196-meter district with authored block shapes, plazas, shore details, varied elevation, and playable interiors; replace its open grass gaps and repeated façade styles with a coherent city layout.
- Add further architectural sets so every street does not repeat the apartment and factory facade styles; replace the remaining primitive trees, characters, and props with original or properly licensed models.
- Add roughness/occlusion maps to major surfaces and validate all channel conventions in Unity.
- Author separate vehicle meshes and interiors with working wheel/door animations, LODs, and colliders.
- Review texture repetition, color grading, shadow quality, and frame time on actual low/mid-tier iOS and Android devices.
- Keep the art direction and all source assets distinct from the OneState reference; see `REA-REFERENCE-LEDGER.md` for evidence and limits.
