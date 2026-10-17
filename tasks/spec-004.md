# Specification 004: Compare and refine the PlayCanvas club against the supplied reference

## 1. Problem and current state

The engine migration preserves gameplay but the first real browser captures reveal overexposed skin/materials, a sparse room and a rectangular action menu. The user requests automated screenshots and iterative visual comparison with their original pink/cyan club reference, not Cyber City.

## 2. Desired state

Bring the club's lighting, material contrast, architecture, crowd density, first-person gesture and radial HUD closer to the reference. Keep the original published game available at its existing URL. Publish the PlayCanvas experiment alongside it, publicly accessible without sign-in.

## 3. Acceptance criteria

> As a player on a phone or desktop, I want to enter the public PlayCanvas version, recognize the original neon-club direction, and use the existing movement, music and interactions while the old version remains available for comparison.

- [x] Run real Chromium/Playwright screenshots locally, inspect rendered images and compare against the user-supplied reference.
- [x] Record successive visual passes, including the initial overexposure and corrected lighting.
- [x] Preserve dark clothes and skin detail under magenta/cyan lighting, add ceiling/wall details and floor sheen, and retain all ten outfit selections.
- [x] Use a radial action HUD with explicit SVG icons and a dedicated first-person heart pose.
- [x] Keep twelve original guest positions; add twelve guests using cached assets, with animated dynamic batching.
- [x] Check the phone viewport, independent simultaneous touch pointers and reachable action targets in a real browser.
- [x] Keep recorded replies and the original three music loops. Interaction still triggers the supplied voice.
- [x] Keep the stable Site intact; the new Site has separate hosting identity and public access.
- [x] Document remaining visual differences honestly; do not claim photorealism or a measured phone frame rate.

## 4. Analysis

The first screenshot showed that copying Three.js light intensity numbers into PlayCanvas overexposed surfaces. Lowering light power fixed the white clipping but the second pass became too dark. Later passes balanced a softer fill with colored lights, clearcoat on shiny materials, layered ceiling rings, ribbed walls and inexpensive authored reflection streaks. These streaks are a stylistic approximation, not real-time planar reflections.

The scene keeps the existing procedural character designs, with smoother hair mass, smaller facial protrusions, sunglasses and a pale evening hairstyle. These remain stylized models and do not match the reference's photographic human fidelity. Future high-quality authored characters, skin/hair textures and improved reflections would be needed to close that gap.

More guests increased rendering cost, so native dynamic batching combines meshes while keeping named limb transforms animated. Software-rendered local screenshots are not a performance benchmark for physical phones.

## 5. Proposed solution

- Keep `Tasks/spec-003.md` as the engine migration and this spec as its visual follow-up.
- Use local Playwright against a loopback static server. The screenshot runner freezes the game clock for the brief heart-action frame; this is a native rendered frame, not a generated or retouched image.
- Store captures and a side-by-side comparison under `artifacts/visual/`; keep reusable desktop, mobile and comparison scripts under `scripts/`.
- Deploy only the separate PlayCanvas Site at `https://neon-room-playcanvas-style.jazzyknoll3.chatgpt.site`. Preserve the original `https://neon-room-afterhours.jazzyknoll3.chatgpt.site`.

## 6. ToDo

- [ ] Test frame rate and thermal behavior on selected physical phones.
- [ ] Replace the stylized avatar heads/hair with suitably licensed authored models if photographic fidelity remains the target.
- [ ] Evaluate higher-quality reflections within the mobile rendering budget.
