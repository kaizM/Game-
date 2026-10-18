# Downtown City assets and ground contact — 2026-10-18

## Asset integration

- Added Quaternius's free Standard Downtown City MegaKit release under its included CC0 1.0 notice. The original 224 MB archive is preserved at `ThirdPartySources/QuaterniusDowntownCity/Downtown-City-MegaKit-Standard.zip`; source page: [Quaternius Downtown City MegaKit](https://quaternius.com/packs/downtowncitymegakit.html).
- Imported three Unity FBX buildings and their 14 referenced texture maps. The source meshes contain 18,152, 25,612, and 44,786 triangles. The FBX materials resolve brick, concrete, trim, marble, and lit-interior color/normal maps.
- Generated `HarborlightBuilding_Small_1`, `HarborlightBuilding_Medium_2_001`, and `HarborlightBuilding_Large_2` prefabs. Each is centered and normalized to the existing streamed-shell bounds, with its lowest rendered point at local ground level and a bounded footprint collider.
- The 1,200 outer sites now select among the three variants: industrial sites use the large model, residential sites use the medium model, and every fourth indexed site uses the small model. The named waterfront core still uses the existing Poly Haven facade prefabs.
- These kit textures are 2K source files. Desktop import allows up to 4K but does not upscale them; Android and iPhone texture imports are capped at 2K. This asset pass does not meet a 4K source-texture target.

## Verification

- Unity 6000.5.4f1 built `Builds/Harborlight-Downtown-City-Loaded-Visual-QA-20261018.app`; the build log ends with `Build Finished, Result: Success` and has no C# compile errors.
- The latest player map smoke passed 598/598 road probes, 84/84 sidewalk probes, 900/900 unique multiplayer spawn slots, and five of five streamed-building ground checks. At each sampled site the building's lowest renderer matched the foundation top within 0.01 m; foundation tops were 0.005 m above the highest sampled terrain corner.
- The viewport-only runtime capture is `screenshots/Harborlight-Outer-City-Game-Only-QA-2026-10-18.png`. It has no desktop or window chrome. Six building shells had loaded at capture time and the process streamed more afterward; the view still shows a sparse road-level area and does not provide close-up art approval for all three variants.
- The build and short map smoke used low process priority. macOS reported no thermal or performance warning. This was not a sustained FPS or memory test.

## Remaining visual work

The city still has large open parcels and a repeated shell set, especially in the named core. There are only three new outer-shell variants, no playable building interiors, and the player, traffic, and props remain prototype art. Android visual quality and performance were not retested. M10 remains in progress.
