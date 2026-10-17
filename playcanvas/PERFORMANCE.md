# NEON ROOM — PlayCanvas port performance notes

## How it was measured

- `scripts/pc-smoke.py` drives the full onboarding and gameplay flow in headless
  Chromium at 1280×800 and records frame rate, frame time and draw calls.
- The two built sites were compared on the same machine from the same build output:
  `dist/index.html` (PlayCanvas port) against `dist/classic/index.html` (the
  Three.js prototype).
- Frame rate is the number of `requestAnimationFrame` callbacks over three seconds
  after entering the club; PlayCanvas additionally reports its own frame statistics.

Environment of the recorded run:

- Chromium 151, headless, **SwiftShader software rasteriser** (no GPU), arm64.
- Viewport 1280×800, device pixel ratio 1.

Because rendering is software-rasterised, absolute numbers are smoke signals, not
device results.

## Results

| Build | rAF FPS (2 runs) | PlayCanvas FPS | Draw calls |
| --- | --- | --- | --- |
| PlayCanvas port | 59.0 / 60.7 | 59 / 60 | 545 / 549 |
| Three.js prototype | 1.7 / 2.0 | — | — |

`npm run smoke:pc` on this machine reported medians between **43 and 60 fps**,
~**16.6-16.7 ms per frame** and **545-575 draw calls** at the entrance (the worst-case
view of the club). The same script drives a 390×844 touch context and confirms
that both virtual joysticks acquire separate pointers at the same time.

## Interpretation

- The PlayCanvas port holds a full frame budget on the software rasteriser at the
  desktop viewport. The Three.js prototype does not, because its characters are
  high-tessellation procedural meshes built from hundreds of small surfaces each.
- This compares whole applications with different character budgets, not the two
  engines in isolation.
- Draw calls (one per mesh) are the main optimisation target if more detail is
  added: the crowd already uses low-detail avatars and the scene shares materials,
  so batching the crowd and truss would be the next step.
- Reference-phone validation, multitouch and audio policy checks remain in
  `Tasks/spec-003.md`'s ToDo list.

Reproduce with:

```sh
npm test           # checks, unit tests and the static build
npm run smoke:pc   # headless gameplay, voice and performance smoke test
```