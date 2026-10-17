# NEON ROOM — After Hours

**NEON ROOM** is a browser game prototype about dressing for an after-hours club, exploring a neon-lit 3D space, and moving to tech house. It is designed for phone screens first and also supports keyboard and mouse.

## Play

- **ChatGPT Sites:** [Open the published prototype](https://neon-room-afterhours.jazzyknoll3.chatgpt.site)
- **GitHub Pages:** deployment workflow is included; the target URL after first-time setup is https://tonidondoni.github.io/neon-room-afterhours/.

The published site runs as a static browser application. Both hosting targets publish the same application. The canonical source lives in `src/`; esbuild bundles PlayCanvas and application JavaScript into `dist/`, alongside local models, images and audio. GitHub Actions tests and builds the project before publishing `dist/` to GitHub Pages. ChatGPT Sites also publishes `dist/`.

## Current prototype

Choose a nickname and one of ten looks, select a control scheme, then enter the NEON ROOM. Explore the club, meet stylized NPCs, send gestures, dance, pick up a drink at the bar, and listen to original procedural tech-house loops. The wardrobe has full-body, face and clothing inspection views with rotation and zoom.

The current build is a local prototype: its club characters are NPCs. Online multiplayer, friends, voice chat and VR are future work.

Near a character, open Actions and choose **Поговорить / Talk**, heart, wave or high-five to hear the supplied recorded voice reply. Club music continues and briefly becomes quieter during speech. Repeated taps do not overlap replies; the speaking indicator includes a Stop button. See [`Tasks/spec-002.md`](Tasks/spec-002.md).

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

All twenty avatar variants occupy about 8.4 MiB together; the wardrobe loads only the selected detailed model, while the club caches ten lighter crowd models for twelve guests and the DJ. The scene batches static architecture and caps pixel ratio. The low-quality setting lowers pixel ratio and bloom. This does not establish a measured frame-rate target.

Tests cover simultaneous touch sticks, voice playback through the shipped action buttons, music ducking/cancellation, native parsing of every GLB, articulated poses, scene construction/frame submission, collision boundaries and relative asset URLs. Native graphics tests use PlayCanvas's NullGraphicsDevice, so GPU appearance and physical phone performance still require browser/device review.

The managed browser preview was unavailable during implementation (`bwrap: Can't mount proc on /newroot/proc`). The experiment is isolated from the original published game and main remains unchanged. Branch CI runs checks; GitHub Pages deployment is restricted to `main`.

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
