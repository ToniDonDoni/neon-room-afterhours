# Instructions for working in NEON ROOM

## Source of truth

- Read the applicable task specification in `Tasks/` before changing product behavior.
- Number new specs sequentially (`spec-002.md`, `spec-003.md`, …) and link them from the README when useful. `Tasks/spec-001.md` is the initial product specification.
- Treat `src/` as the canonical browser application. It is an ES-module app rendered by PlayCanvas 2.23.1, bundled with esbuild. Three.js is used only by the offline avatar authoring script in `scripts/avatar-source/`; never import it into `src/`. Keep asset and module paths relative so GitHub Pages' project subpath works.
- Install pinned dependencies with `npm ci`. Run `npm run bake:avatars` only when changing avatar geometry; it exports both detail levels with articulated node names and optimizes them with gltfpack. Commit the resulting GLBs.
- Run `npm run build` after changing the app. Both ChatGPT Sites and GitHub Pages publish the generated `dist/` directory.
- Do not commit secrets, generated credentials, or local environment files.

## Before finishing a change

- Run `npm test`.
- Check the mobile layout and both input modes when UI or controls change.
- Check the GitHub Pages project subpath for relative asset paths.
- Update the relevant task spec if acceptance criteria, scope, or known limitations change.
- Keep third-party package notices in `src/THIRD_PARTY_LICENSES.txt`; the build copies the notice into `dist/`.

## Product boundary

The shipped prototype is a local single-player browser scene with NPCs and prerecorded voice replies described in `Tasks/spec-002.md`. Do not describe the NPC scene as online multiplayer. Treat multiplayer, friends, chat, live voice chat, VR, streaming integrations and monetization as future work unless a new task explicitly brings them into scope.

## PlayCanvas port

- `Tasks/spec-003.md` governs this branch, based on main `90a1755141288e5d538367db8ff03b96dabcf4e7`. Preserve the original NEON ROOM palette, outfits, layout, interactions and sound. Cyber City is not a visual reference.
- The experiment is separate from the stable Site. Do not overwrite its Site ID or deploy the branch to the main GitHub Pages environment.
- `npm test` includes native PlayCanvas GLB parsing and scene/frame tests using its NullGraphicsDevice, plus UI/audio/input regression tests. These are not GPU screenshot or physical-phone performance tests. Report browser verification limits honestly.
