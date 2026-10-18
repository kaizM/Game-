# Map grounding and visual review — 2026-10-18

## Ground contact

- The ground mesh, road grid, building foundations, and multiplayer spawn coordinates use the same terrain-height function.
- Foundations sample all four rotated footprint corners. They extend below the low side of the terrain, their top follows the high side, and each building's lowest rendered point is aligned to that top.
- Unity 6000.5.4f1 built `Builds/Harborlight-Visual-Grounding-QA-20261018.app` successfully.
- The current-source runtime smoke passed all 598 road crossings, 84 sidewalk samples, 900 unique multiplayer spawn coordinates, and five of five streamed-building ground-contact checks. The filtered evidence is `map-grounding-visual-smoke-2026-10-18.log`.
- No macOS thermal or performance warning was recorded after the low-priority build and short smoke run.

## Visual review

- Replaced the flat solid-color horizon with a procedural coastal sky, removed the bright sun disc, and reduced fog density from 0.0025 to 0.0009 so distant terrain and roads remain visible.
- `screenshots/Harborlight-Coastal-Sky-Grounding-QA-2026-10-18.png` is the earlier first-frame capture, before nearby streamed shells finished loading.
- The follow-up `screenshots/Harborlight-Outer-City-Game-Only-QA-2026-10-18.png` is an in-game 1280×720 capture with no desktop/window chrome. Its test profile is on a verified outer road row; it still shows a sparse road-level view rather than a close-up of every building form.
- The 2026-10-18 Quaternius asset integration adds three varied CC0 outer shell forms. Much of the city remains open terrain, central facades still repeat, the kit source maps are 2K, and interiors/traffic/4K source environment art are not complete. See `quaternius-city-assets-and-grounding-2026-10-18.md`.

## Limits

The runtime check samples five streamed buildings and selected road/sidewalk locations; it does not verify every building, full walking/driving routes, real multiplayer arrivals, Android rendering, or mobile performance. The city remains visually incomplete and M10 stays in progress.
