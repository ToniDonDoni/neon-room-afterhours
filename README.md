# NEON ROOM — After Hours

**NEON ROOM** is a browser game prototype about dressing for an after-hours club, exploring a neon-lit 3D space, and moving to tech house. It is designed for phone screens first and also supports keyboard and mouse.

## JavaScript graphics engine

The current version uses **[PlayCanvas Engine](https://github.com/playcanvas/engine) 2.23.1**, an open-source JavaScript engine for real-time browser 3D graphics. The game uses its code API directly from plain JavaScript ES modules. See the [PlayCanvas documentation](https://developer.playcanvas.com/) for the engine API and examples.

| Component | Implementation |
| --- | --- |
| Club and wardrobe rendering | PlayCanvas entities, meshes, cameras and GLB model loading |
| Neon and glossy surfaces | Emissive and PBR materials, environment reflections, bloom and ACES tone mapping |
| Scene construction and animation | Project JavaScript in `src/rendering.js`, `src/club-renderer.js`, `src/wardrobe-renderer.js` and `src/avatars.js` |
| Avatar model generation | Offline Three.js 0.169.0 scripts export articulated GLBs; gltfpack optimizes the exported models |
| Interface and audio | HTML/CSS, JavaScript and Web Audio API |
| Browser bundle | esbuild 0.25.11 bundles PlayCanvas and the application into `dist/app.js` |

The original published version uses Three.js. This branch contains the PlayCanvas port, published at its own URL below. All engine and model assets are bundled locally for deployment.

## Play

- **Public PlayCanvas version:** [Open the separate experiment](https://neon-room-playcanvas-style.jazzyknoll3.chatgpt.site)
- **Original ChatGPT Sites version (preserved):** [Open the published prototype](https://neon-room-afterhours.jazzyknoll3.chatgpt.site)
- **GitHub Pages:** deployment workflow is included; the target URL after first-time setup is https://tonidondoni.github.io/neon-room-afterhours/.

The published site runs as a static browser application. Both hosting targets publish the same application. The canonical source lives in `src/`; esbuild bundles PlayCanvas and application JavaScript into `dist/`, alongside local models, images and audio. GitHub Actions tests and builds the project before publishing `dist/` to GitHub Pages. ChatGPT Sites also publishes `dist/`.

## Current prototype

Choose a nickname and one of ten looks, select a control scheme, then enter the NEON ROOM. Explore the club, meet stylized NPCs, send gestures, dance, pick up a drink at the bar, and listen to original procedural tech-house loops. The wardrobe has full-body, face and clothing inspection views with rotation and zoom.

The current build is a local prototype: its club characters are NPCs. Online multiplayer, friends, voice chat and VR are future work.

Near a character, open Actions and choose **Talk**, heart, wave or high-five to hear the supplied recorded voice reply. Club music continues and briefly becomes quieter during speech. Repeated taps do not overlap replies; the speaking indicator includes a Stop button. See [`Tasks/spec-002.md`](Tasks/spec-002.md).

## First-time GitHub Pages setup

The repository must have Pages enabled before the workflow can publish. In **Settings → Pages → Build and deployment**, set **Source** to **GitHub Actions**. Then open **Actions → Test and deploy GitHub Pages → Run workflow**, or re-run the failed deployment. Future changes to `main` are published automatically by the workflow.

The initial GitHub Actions test job passed; publication was blocked because GitHub Pages had not yet been enabled in the repository settings.

## Run locally

Install Node.js 20 or newer, then build and serve the static output:

```sh
npm ci
npm test
npx serve dist
```

After source edits, run `npm run build` again. All runtime URLs are relative to `dist/app.js`, including on GitHub Pages' project subpath. No external engine CDN or model service is required.

## PlayCanvas experiment

This branch, `codex/playcanvas-preserve-style`, ports main commit `90a1755141288e5d538367db8ff03b96dabcf4e7` to **PlayCanvas 2.23.1**. The original neon club reference, UI, ten outfit designs, club layout, controls, three music loops and recorded NPC voice remain the target. Cyber City is not used as a visual reference. See [`Tasks/spec-003.md`](Tasks/spec-003.md).

The browser uses native PlayCanvas entities, render components, cameras, PBR materials, environment reflections and bloom. The existing detailed avatars and first-person hands are baked into GLBs with named moving limbs. A woven fabric normal map is recreated at runtime. Three.js is an offline authoring dependency only, and the build rejects it in the browser dependency graph.

To regenerate models after an authoring change:

```sh
npm run bake:avatars
npm test
```

All twenty avatar variants occupy about 9.9 MiB together; the wardrobe loads only the selected detailed model, while the club caches ten lighter crowd models for twenty-four guests and the DJ. The scene batches static architecture and animated guest parts and caps pixel ratio. The low-quality setting lowers pixel ratio and bloom. This does not establish a measured frame-rate target.

Tests cover simultaneous touch sticks, voice playback through the shipped action buttons, music ducking/cancellation, native parsing of every GLB, articulated poses, scene construction/frame submission, collision boundaries and relative asset URLs. Native graphics tests use PlayCanvas's NullGraphicsDevice, so Captured GPU output was visually reviewed against the supplied reference; physical phone performance remains unmeasured.

The managed preview was unavailable, so the user explicitly requested direct local Playwright. Chromium screenshots and a 390×844 touch test were then run locally. The touch test passed with two active independent pointers and six reachable action targets, with no JavaScript errors. The experiment is isolated from the original published game and main remains unchanged. Branch CI runs checks; GitHub Pages deployment is restricted to `main`.

## Project layout

- `src/` — browser UI/audio/input modules, native PlayCanvas rendering and GLB assets.
- `Tasks/` — numbered product specifications; read `Tasks/spec-001.md` first.
- `tests/` — input, game/audio and native PlayCanvas regression tests.
- `.github/workflows/deploy-pages.yml` — checks and GitHub Pages deployment.
- `dist/` — generated static output for both hosting targets.
- `scripts/build-site.js` — bundles the engine/app and copies static assets.
- `scripts/avatar-source/` and `scripts/bake-avatars.mjs` — reproducible offline avatar authoring and optimization.
- `AGENTS.md` — project instructions and the spec-driven workflow.

See [`AGENTS.md`](AGENTS.md) for contributor instructions and [`Tasks/spec-001.md`](Tasks/spec-001.md) for the first specification.

## Visual comparison and browser checks

See [`Tasks/spec-004.md`](Tasks/spec-004.md) and [`artifacts/visual/review.md`](artifacts/visual/review.md) for five visual passes, captures and remaining differences. The final result is still stylized; it does not reproduce the reference's photographic characters.

For local browser checks, install Playwright's Chromium once, then run:

```sh
npx playwright install chromium
npm run test:visual -- final
node scripts/mobile-review.cjs
node scripts/compare-reference.cjs /path/to/reference.png
```

`PLAYWRIGHT_CHROMIUM_EXECUTABLE` optionally selects an already installed Chromium binary. The visual scripts serve `dist/` on loopback only and close their servers/browsers when finished. The final action capture pauses Playwright's game clock to capture the brief hand pose.

The game interface is now entirely in English, including onboarding, all ten outfit descriptions, controls, settings, NPC panels, notifications and accessible labels. See [`Tasks/spec-005.md`](Tasks/spec-005.md). Player-entered names and the original prerecorded voice are preserved.

## Unity WebGL port

This branch also contains the Unity port in [`unity/`](unity/), documented in [`tasks/spec-006.md`](tasks/spec-006.md). Its WebGL build is produced separately from the PlayCanvas static site.
