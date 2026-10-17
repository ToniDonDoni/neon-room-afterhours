# NEON ROOM — After Hours

**NEON ROOM** is a browser game prototype about dressing for an after-hours club, exploring a neon-lit 3D space, and moving to tech house. It is designed for phone screens first and also supports keyboard and mouse.

## Play

- **ChatGPT Sites:** [Open the published prototype](https://neon-room-afterhours.jazzyknoll3.chatgpt.site)
- **GitHub Pages:** deployment workflow is included; the target URL after first-time setup is https://tonidondoni.github.io/neon-room-afterhours/.

The published site runs as a static browser application. Both hosting targets publish the same application. The canonical source lives in `src/`; the build script copies it to `dist/`. GitHub Actions tests and builds the project before publishing `dist/` to GitHub Pages. ChatGPT Sites also publishes `dist/`.

## Current prototype

Choose a nickname and one of ten looks, select a control scheme, then enter the NEON ROOM. Explore the club, meet stylized NPCs, send gestures, dance, pick up a drink at the bar, and listen to original procedural tech-house loops. The wardrobe has full-body, face and clothing inspection views with rotation and zoom.

The current build is a local prototype: its club characters are NPCs. Online multiplayer, friends, voice chat and VR are future work.

## First-time GitHub Pages setup

The repository must have Pages enabled before the workflow can publish. In **Settings → Pages → Build and deployment**, set **Source** to **GitHub Actions**. Then open **Actions → Test and deploy GitHub Pages → Run workflow**, or re-run the failed deployment. Future changes to `main` are published automatically by the workflow.

The initial GitHub Actions test job passed; publication was blocked because GitHub Pages had not yet been enabled in the repository settings.

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

- `src/` — browser application, 3D assets and bundled Three.js runtime.
- `Tasks/` — numbered product specifications; read `Tasks/spec-001.md` first.
- `tests/` — automated tests for input behavior.
- `.github/workflows/deploy-pages.yml` — checks and GitHub Pages deployment.
- `dist/` — generated static output for both hosting targets.
- `scripts/build-site.js` — copies the source into the static output directory.
- `AGENTS.md` — project instructions and the spec-driven workflow.

See [`AGENTS.md`](AGENTS.md) for contributor instructions and [`Tasks/spec-001.md`](Tasks/spec-001.md) for the first specification.
