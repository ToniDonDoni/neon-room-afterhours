# Specification 003: PlayCanvas port experiment for NEON ROOM

## 1. Problem and current state

The prototype in `src/` runs on a vendored Three.js runtime. The team wants to evaluate
PlayCanvas as the rendering and scene-graph engine for NEON ROOM before committing to a
long-term engine choice. The port must prove that the existing club concept, art direction
and gameplay survive a move to PlayCanvas without regressing the player experience.

This is an experiment on the branch `codex/playcanvas-port`. The Three.js application on
`main` stays untouched and remains the reference implementation; the two versions are
compared side by side from their branches.

## 2. Desired state

A player opens the branch build, chooses a nickname and one of ten looks, picks a control
scheme and enters the same neon after-hours club: entrance, dance floor, DJ stage, bar and
lounge, populated by twelve stylized NPCs. The acid-neon art direction, reflective and
emissive materials and the fashion silhouettes are recreated with PlayCanvas primitives and
materials.

Every mechanic of the Three.js prototype is preserved: wardrobe inspection with rotation and
zoom, independent multitouch joysticks, WASD/pointer-lock desktop controls, the action wheel
(talk, heart, wave, high-five, dance, drink), club zones, map teleport, settings (volume,
quality, calm light, track), procedural tech-house music and the recorded NPC voice reply
with music ducking. Profile data keeps using the same `localStorage` key so the two builds
share local state.

The branch build is a standalone, dependency-free static site served by the same GitHub Pages
workflow as the reference build.

## 3. Acceptance criteria

### User story

> As a new player on the PlayCanvas branch, I want to open the game, choose a look and
> controls, walk the neon club, perform actions and hear music and an NPC voice reply, so
> that I can judge whether PlayCanvas is a credible replacement for Three.js.

Scope of this experiment: parity of concept, scene, mechanics, audio and deployability, plus
a measured performance smoke test. Pixel-identical character meshes are not required.

- [x] `Tasks/spec-003.md` documents the port and the branch exists from current `main`.
- [x] The PlayCanvas app is a buildless ES-module static site and boots in a modern browser.
- [x] Nickname selection accepts a suggested name or free input.
- [x] Ten outfits are available; the choice is stored and restored from `localStorage`.
- [x] The wardrobe preview supports full-body, face and clothing views plus rotation and zoom.
- [x] Players choose phone joysticks or keyboard and mouse before entering.
- [x] On phones, two independent joysticks move and look at the same time.
- [x] On desktop, WASD moves, the mouse controls the view and E opens the action wheel.
- [x] Entrance, dance floor, DJ stage, bar and lounge exist and the zone label tracks position.
- [x] Twelve NPCs populate the club; at least seven dance to the beat.
- [x] Talk, heart, wave and high-five within range trigger the recorded voice reply.
- [x] Repeated interactions cannot overlap or restart a reply; Stop ends it.
- [x] Music ducks to 35% while the reply plays and is restored afterwards.
- [x] Dancing toggles on and off; a drink can be taken only near the bar.
- [x] Procedural tech-house music has three tracks, volume control and a music toggle.
- [x] Map teleport moves the player to dance floor, bar, lounge or entrance.
- [x] Settings allow volume, quality, calm light and track changes.
- [x] Returning to the wardrobe and re-entering the club works without a page reload.
- [x] The app runs without compilation or an application server.
- [x] `npm test` passes, including new PlayCanvas tests.
- [x] `npm run build` produces `dist/` with the PlayCanvas build at the site root and the
      Three.js build under `dist/classic/`.
- [x] A headless browser smoke test loads the built site, reports no console errors and
      paints WebGL frames.
- [x] A performance smoke test records a median frame rate at the desktop viewport and the
      result is written down in the branch notes.
- [ ] The experiment is committed on `codex/playcanvas-port`, pushed, and published to
      GitHub Pages from that branch.
- [x] Online multiplayer, friends, chat, live voice, VR, streaming and monetization remain out
      of scope.

## 4. Analysis

PlayCanvas is a component/entity engine that owns rendering, the scene graph and the update
loop. Moving from Three.js means rewriting scene construction, materials and the render
loop, but the surrounding product logic (profile, screens, input mapping, Web Audio
synthesis, NPC interaction rules and HTML UI) is engine-agnostic and can be carried over.

The engine ships as a single vendored UMD bundle, so the buildless, dependency-free model of
the project is preserved: no bundler, no npm install, relative paths only. The Web Audio
synthesizer produces music without asset files and can be reused unchanged. The recorded NPC
voice stays a bundled MP3 and keeps the existing distance, ducking and single-reply rules.

Procedural character meshes in `src/fashion.js` depend on Three.js geometry utilities. The
port recreates the ten looks with a PlayCanvas primitive humanoid and per-look color, style
and accessory parameters. Visual parity is approximate; silhouette families (draped, tailor,
ritual, nomad, veil, mass) and the acid/violet/ash palette are preserved.

Performance is measured with a headless Chromium smoke test. The result is a smoke signal,
not a claim about reference phones; device QA is still required.

## 5. Proposed solution

- **Branch:** `codex/playcanvas-port` from current `main`, three.js reference kept intact.
- **Engine:** vendored `playcanvas` UMD build under `playcanvas/vendor/`; no build step.
- **App:** `playcanvas/` holds `index.html`, `style.css`, `app.js`, shared logic modules and
  copied audio assets. It is self-contained so the built site root is standalone.
- **Scene:** PlayCanvas entities and primitive meshes for the room, stage, bar, lounge, laser
  cones, particles and neon signs; standard materials with metalness/roughness and emissive
  neon; environment lighting via light entities.
- **Characters:** a parameterized primitive avatar module driven by the ten-look table, with a
  low-detail variant for the crowd and a detailed variant for the wardrobe preview.
- **UI:** the existing HTML/CSS screens and action wheel, re-implemented in `playcanvas/`.
- **Input:** a shared joystick module and desktop key/pointer handling matching the reference.
- **Audio:** the same Web Audio synthesizer for three tech-house tracks; the recorded MP3 for
  NPC replies with ducking, single-reply and stop behavior.
- **Build:** `scripts/build-site.js` copies the PlayCanvas build to `dist/` and the Three.js
  build to `dist/classic/`.
- **Tests:** Node unit tests for the pure PlayCanvas logic modules, plus a headless browser
  smoke and performance script used during development.
- **Delivery:** commit on the branch, push it and run the Pages workflow from the branch.

## 6. ToDo

- [x] Confirm at least seven NPCs and zone detection match the reference numbers.
- [ ] Validate multitouch, autoplay and audio policies on reference iOS and Android devices.
- [x] Compare median frame rate and draw calls between the PlayCanvas and Three.js builds.
- [ ] Decide the long-term engine after the comparison and either promote this spec or archive it.
- [ ] Recreate full-body clothing geometry with real garment meshes if PlayCanvas is chosen.
