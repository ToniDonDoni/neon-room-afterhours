# Specification 003: Port NEON ROOM to PlayCanvas

## 1. Problem and current state

The main branch uses Three.js. It already includes the wardrobe, ten detailed outfits, first-person club exploration, mobile and desktop controls, NPC gestures, procedural music and recorded NPC replies.

## 2. Desired state

Run the same experience with native PlayCanvas rendering on a new branch from the current main commit `90a1755141288e5d538367db8ff03b96dabcf4e7`. Preserve the user's existing visual reference: a dark club with pink/violet neon, cyan accents, shiny clothing, chrome and first-person hands. Cyber City is not a visual reference for this port.

## 3. Acceptance criteria

> As a returning player, I want to choose the same nickname, outfit and controls, explore the same club and perform the same actions with music and recorded replies, while the scene is rendered by PlayCanvas.

- [x] No Three.js renderer or runtime module is loaded by the browser.
- [x] All ten detailed outfit silhouettes and their material details remain available, with rotation, zoom and face/clothing inspection.
- [x] The club retains the dance floor, DJ stage, bar, lounge, NPC placement and collision boundaries.
- [x] First-person hands and NPC gesture/dance animations remain visible.
- [x] Mobile simultaneous movement/look, desktop WASD/mouse, switching controls, map navigation and reduced-motion/quality settings work.
- [x] Talk, heart, wave, high-five, dance and drink actions retain their behavior and proximity constraints.
- [x] All three procedural tracks, volume controls, recorded voice replies, music ducking and cancellation remain functional.
- [x] Profile settings remain compatible with the existing localStorage key.
- [x] The static output works from a project subpath and bundles the engine, models and recording.
- [x] Regression tests cover the port's gameplay and audio behavior; visual/browser checks are reported accurately.

## 4. Analysis

Use PlayCanvas entities, cameras, meshes, lights and materials directly. Reuse the original detailed avatar geometry by exporting authored procedural models to glTF/GLB with named articulated limbs. Three.js is allowed only in the offline asset-authoring script, never in the shipped browser runtime. Keep UI, input and Web Audio behavior independent of the renderer.

## 5. Proposed solution

- Pin PlayCanvas and bundle JavaScript locally with esbuild.
- Package detailed wardrobe and lighter crowd GLBs derived from the original avatar source; retain editable generation scripts.
- Rebuild the club and wardrobe rendering using native PlayCanvas APIs and preserve the original scene layout and palette.
- Maintain gameplay/input/audio modules with regression coverage, and publish a separate experiment for comparison.

## 6. ToDo

- [x] Implement the port; syntax/build and 16 regression tests pass.
- [ ] Review GPU appearance and mobile/desktop behavior in a real browser. Managed preview was blocked by `bwrap: Can't mount proc on /newroot/proc: Operation not permitted`. Native graphics tests use NullGraphicsDevice and do not validate pixels.
- [ ] Measure performance on the selected physical phones; no frame-rate claim is made before measurement.

## Verification notes

The renderer tests load every optimized GLB through PlayCanvas, instantiate real render entities, validate shoulder pivots/materials, construct and batch the club, submit frames, and check gesture/dance/reduced-motion poses. UI-level tests exercise the shipped action buttons, prerecorded audio and music gain, simultaneous mobile movement/look, keyboard movement, menu pauses and drink constraints. Input tests verify two independent touch pointers. The static build rejects Three.js in its dependency graph and copies all media locally. GPU appearance and physical-device frame rate remain unverified.
