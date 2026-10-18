# Harborlight art and content guide

## Identity

Harborlight is a quiet, lived-in coastal city about repairing public places and meeting the people who rely on them. The first district is North Pier: Lantern Square, South Market, Foundry Row, the Old Ferry Office, the pier, and a small independent garage.

Use original Harborlight names, silhouettes, layouts, dialogue, UI, and iconography. Do not derive assets, scenes, text, or designs from OneState or another commercial game. REA notes are technical research only.

## Visual direction

- **Palette:** fog blue and harbor teal for the environment; warm brass and lantern amber for objectives; restrained brick, weathered concrete, and painted timber for buildings.
- **Lighting:** soft late-afternoon coastal light, readable shadows, warm shop windows, and restrained bloom. Keep roads, interactables, and objective markers legible on small displays.
- **Materials:** subtle surface variation; avoid high-frequency noise and mirror-like vehicle paint. Reserve brighter accents for interaction and navigation.
- **Camera:** shoulder-height third person with a stable horizon, collision-aware follow distance, and a comfortable mobile orbit range.
- **Motion/audio:** grounded walking and low-speed vehicle movement; sparse gulls, water, traffic ambience, footsteps, and distinct interaction cues.

## Asset pipeline and rights

Replace procedural boxes, capsules, and cylinders with original commissioned or properly licensed assets. Keep a third-party asset register with source URL, creator, license, acquisition date, allowed platforms, attribution wording, and modified-file list. Keep source files and any required notices with the project. Do not ship unverified marketplace or generated assets.

For each imported character, vehicle, building kit, texture, sound, or font, record its license before it enters a release build. Use original mission text and locally authored signs. Capture screenshots and trailer footage only from the Harborlight build with assets cleared for marketing use.

## First-district content

Build the waterfront as one continuous, walkable/drivable district with clear landmarks, accessible turns, low-speed collision boundaries, and no blocked mission route. Content should include the beacon repair, market courier, and East Pier inspection chains; repeatable free roam; an accessible garage; and local progression. Any future additions should deepen this district before increasing its footprint.

## Production checks

- Test scene readability at the lowest supported mobile resolution and with safe-area insets.
- Check contrast for text, objective markers, and touch targets in bright and dim environments.
- Keep touch controls away from notches, home indicators, and store/system gestures.
- Profile overdraw, texture memory, shadows, and thermal load on the minimum target device.
- Confirm that every shipped source asset has a rights record and that no placeholder geometry is presented as final art.

## Current status

Six of the eight garage vehicles now use Quaternius Cars Pack low-poly FBX meshes under CC0 1.0; the two unmatched roles remain procedurally modeled. The original archive and license notice are kept in `ThirdPartySources/QuaterniusCarsPack/`, with the license record in `Assets/Art/THIRD-PARTY-ASSET-REGISTER.md`. The imported models have roughly 5,000–5,600 triangles each and URP Lit materials; they are stylized first-pass assets, not realistic final car art. Buildings, props, and characters still use Unity primitives. A production vertical slice still needs higher-fidelity vehicle and character art, animation/audio, accessibility review, and mobile device profiling.
