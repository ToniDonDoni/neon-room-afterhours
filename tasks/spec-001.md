# Specification 001: NEON ROOM — a browser nightclub game

## 1. Problem and current state

We need a browser-accessible game about virtual parties: players put together an outfit and enter a nightclub where they can move freely, listen to music and interact with characters. The product vision includes first-person play, future VR support and online parties.

The first published version is an interactive single-player prototype. It includes a showroom with nickname selection and ten outfits, one NEON ROOM club, stylized NPCs, music and basic player actions. Server-side player synchronization, friends, text or voice chat and VR sessions are not yet implemented.

## 2. Desired state

The game runs in a modern browser on phones and computers. Players choose a nickname, outfit and control scheme before entering the interactive club.

The art direction is a dark acid-electro club: a black base, purple, pink and cyan neon, reflective materials, chrome, light beams, haze and fashion details. The wardrobe offers ten stylized looks inspired by dark asymmetric fashion, oversized club streetwear, draping, layers, heavy footwear and metal accessories.

The club includes an entrance, dance floor, DJ stage, bar and lounge. Procedural tech-house music starts after a player action and supports volume and track selection. NPCs populate the scene and respond to player actions.

## 3. Acceptance criteria

### User story

> As a new player, I want to open the game, select a suggested nickname or enter my own, choose an outfit and controls, enter the club, move freely and perform actions to tech-house music so that I can experience a virtual party.

- [x] Players can select a suggested nickname or enter their own.
- [x] Ten outfits are available, and the selection is saved in the local browser.
- [x] The wardrobe supports rotating the 3D model and inspecting the face or clothing up close.
- [x] Players can choose phone controls or keyboard and mouse before entering.
- [x] On phones, independent multitouch joysticks support moving and looking around simultaneously.
- [x] On computers, WASD moves the player, the mouse controls the view and E opens the action menu.
- [x] Players can visit the dance floor, DJ stage, bar and lounge.
- [x] Gestures, dancing and a drink are available; NPCs respond to player actions.
- [x] Procedural tech-house tracks start after a player action; players can switch tracks and adjust the volume.
- [x] Players can return to the wardrobe and change their outfit without reloading the app.
- [x] The browser app runs without compilation or an application server.
- [ ] Full online multiplayer, chat, friends and shared rooms.
- [ ] VR mode and validation on a selected headset.
- [ ] Frame-rate and visual-quality validation on specified reference phones.

## 4. Analysis

The app is a buildless ES-module project. The same static page can be served by GitHub Pages and ChatGPT Sites without maintaining separate builds. Three.js and the geometry utility module are vendored in the repository rather than loaded from an external CDN.

The club uses Three.js and WebGL 2. Characters use detailed procedural geometry. The wardrobe displays detailed models, while the crowd uses a lighter version. Emissive materials, limited glow, environment maps, simplified reflections, a baked environment and reduced quality on weaker devices preserve the neon atmosphere at a reasonable rendering cost.

Audio is synthesized with the Web Audio API, so the scene does not require music files to start. The initial game is a local prototype with NPCs; real online play will require separate decisions about room servers, position synchronization, voice and user safety.

## 5. Proposed solution

- **Client:** HTML, CSS and JavaScript ES modules without a build framework.
- **3D:** Three.js, WebGL 2, procedural character models and a club scene.
- **Audio:** Web Audio API and procedural tech-house rhythm loops.
- **Controls:** independent multitouch virtual sticks on phones; WASD, pointer input and keyboard on computers.
- **Profile:** nickname, outfit, music settings and controls are saved locally.
- **Source:** `src/` is the canonical app; tests live in `tests/`; task specifications live in `tasks/`.
- **GitHub Pages:** GitHub Actions runs `npm test`, including checks and `dist/` generation, then publishes `dist/`.
- **ChatGPT Sites:** the same project supports the existing static publication from `dist/`.
- **Future development:** define a separate specification and server architecture for online play and VR after validating interest in the core mechanics.

## 6. ToDo

- [ ] Select reference phones and validate performance, multitouch, device temperature and loading.
- [ ] Confirm the final wardrobe silhouettes and refine 3D materials against the selected references.
- [ ] Select a target VR headset and locomotion scheme.
- [ ] Specify online rooms, player synchronization, friends, blocking and voice chat.
- [ ] Define the scope and budget for integrating requests from stream viewers.
- [ ] Verify the public GitHub Pages deployment after the first workflow run. If Actions publication is not enabled, select Settings → Pages → Build and deployment → Source: GitHub Actions.
