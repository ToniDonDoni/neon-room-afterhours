# Instructions for working in NEON ROOM

## Source of truth

- Read the applicable task specification in `Tasks/` before changing product behavior.
- Number new specs sequentially (`spec-002.md`, `spec-003.md`, …) and link them from the README when useful. `Tasks/spec-001.md` is the initial product specification.
- Treat `src/` as the canonical browser application. It is a dependency-free, buildless ES-module app using the vendored Three.js runtime. Keep asset and module paths relative so GitHub Pages' project subpath works.
- Run `npm run build` after changing the app. Both ChatGPT Sites and GitHub Pages publish the generated `dist/` directory.
- Do not commit secrets, generated credentials, or local environment files.

## Branch experiment: PlayCanvas port

On the branch `codex/playcanvas-port` the canonical browser application is
`playcanvas/` (PlayCanvas 2.7.4, vendored under `playcanvas/vendor/`). `src/`
stays untouched as the reference Three.js build, and `npm run build` publishes
`playcanvas/` to `dist/` with the reference build at `dist/classic/`. Read
`Tasks/spec-003.md` before changing the port, and run `npm run smoke:pc` after
changes that touch the scene, controls or audio.

## Before finishing a change

- Run `npm test`.
- Check the mobile layout and both input modes when UI or controls change.
- Check the GitHub Pages project subpath for relative asset paths.
- Update the relevant task spec if acceptance criteria, scope, or known limitations change.
- Keep third-party package notices in `src/THIRD_PARTY_LICENSES.txt` (and `playcanvas/THIRD_PARTY_LICENSES.txt` for the port); the build copies each notice into its output directory.

## Product boundary

The shipped prototype is a local single-player browser scene with NPCs and prerecorded voice replies described in `Tasks/spec-002.md`. Do not describe the NPC scene as online multiplayer. Treat multiplayer, friends, chat, live voice chat, VR, streaming integrations and monetization as future work unless a new task explicitly brings them into scope.
