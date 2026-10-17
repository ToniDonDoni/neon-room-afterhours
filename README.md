# NEON ROOM — After Hours

**NEON ROOM** is a browser game prototype about dressing for an after-hours club, exploring a neon-lit 3D space, and moving to tech house. It is designed for phone screens first and also supports keyboard and mouse.

## Play

- **ChatGPT Sites:** [Open the published prototype](https://neon-room-afterhours.jazzyknoll3.chatgpt.site)
- **GitHub Pages:** deployment workflow is included; the target URL after first-time setup is https://tonidondoni.github.io/neon-room-afterhours/.

The published site runs as a static browser application. On this experiment branch the build script copies the PlayCanvas port to `dist/` and the original Three.js application from `src/` to `dist/classic/`. GitHub Actions tests and builds the project before publishing `dist/` to GitHub Pages. ChatGPT Sites also publishes `dist/`.

## Current prototype

Choose a nickname and one of ten looks, select a control scheme, then enter the NEON ROOM. Explore the club, meet stylized NPCs, send gestures, dance, pick up a drink at the bar, and listen to original procedural tech-house loops. The wardrobe has full-body, face and clothing inspection views with rotation and zoom.

The current build is a local prototype: its club characters are NPCs. Online multiplayer, friends, voice chat and VR are future work.

Near a character, open Actions and choose **Поговорить / Talk**, heart, wave or high-five to hear the supplied recorded voice reply. Club music continues and briefly becomes quieter during speech. Repeated taps do not overlap replies; the speaking indicator includes a Stop button. See [`Tasks/spec-002.md`](Tasks/spec-002.md).

## PlayCanvas port experiment

The branch `codex/playcanvas-port` rebuilds the same club, mechanics, procedural
music and recorded NPC replies on the [PlayCanvas](https://playcanvas.com/products/engine)
engine. The PlayCanvas build is the site root; the original Three.js build stays
available under `dist/classic/` for comparison.

- Specification: [`Tasks/spec-003.md`](Tasks/spec-003.md)
- Performance notes: [`playcanvas/PERFORMANCE.md`](playcanvas/PERFORMANCE.md)
- Visual comparison against the brief: [`playcanvas/VISUAL-NOTES.md`](playcanvas/VISUAL-NOTES.md)

Serve the port locally with `npx serve playcanvas`, or after `npm run build` with
`npx serve dist`. The headless tooling (Python + Playwright) is:

- `npm run smoke:pc` — gameplay, voice, mobile and canvas checks.
- `npm run bench:pc` — PlayCanvas vs Three.js frame rate in the same harness.
- `npm run shots:pc` — 14 gameplay screenshots and reference comparison sheets.

## GitHub Pages

Pages is enabled for this repository (**Settings → Pages → Build and deployment →
Source: GitHub Actions**). A push to `main` or `codex/playcanvas-port` that touches a watched
path runs the tests and publishes the built site.

- PlayCanvas port (site root): https://tonidondoni.github.io/neon-room-afterhours/
- Three.js reference: https://tonidondoni.github.io/neon-room-afterhours/classic/

## Run locally

Serve `src/` with any local static web server; ES modules and WebGL require HTTP(S).

```sh
npx serve src
```

Open the local URL shown by the server. No build step or dependency installation is required. To run the project checks with Node.js 20 or newer:

```sh
npm test
```

The optional build command refreshes `dist/` from the canonical source:

```sh
npm run build
```

## Project layout

- `src/` — original browser application, 3D assets and bundled Three.js runtime.
- `playcanvas/` — experimental PlayCanvas port served as the site root on this branch.
- `playcanvas/reference/` — the brief's club/meeting mood image.
- `playcanvas/screenshots/` — generated gameplay screenshots and comparison sheets.
- `Tasks/` — numbered product specifications; read `Tasks/spec-001.md` first.
- `tests/` — automated tests for input behavior.
- `.github/workflows/deploy-pages.yml` — checks and GitHub Pages deployment.
- `dist/` — generated static output for both hosting targets.
- `scripts/build-site.js` — builds `dist/` from the PlayCanvas port plus `dist/classic/` from `src/`.
- `scripts/pc-smoke.py` — headless gameplay, voice and canvas smoke test.
- `scripts/pc-bench.py` — PlayCanvas vs Three.js frame-rate benchmark.
- `scripts/pc-shots.py` — Playwright screenshot capture for visual comparison.
- `AGENTS.md` — project instructions and the spec-driven workflow.

See [`AGENTS.md`](AGENTS.md) for contributor instructions and [`Tasks/spec-001.md`](Tasks/spec-001.md) for the first specification.
