# NEON ROOM — PlayCanvas port performance notes

## Correction: an earlier measurement was invalid

While capturing visual screenshots we found that the world canvas was created
while `#world` was still `display:none`, so its drawing buffer stayed **0×0**
and nothing was actually rasterised. Earlier figures of 43-60 fps measured an
empty buffer. `enterClub()` now reveals the world before `initWorld()` and calls
`app.resizeCanvas()`, `scripts/pc-smoke.py` asserts `canvasSized`, and the numbers
below come from that fixed build.

## How it was measured

- `scripts/pc-bench.py` runs the same onboarding flow against either build at
  1280×800, then counts `requestAnimationFrame` callbacks for four seconds while
  standing in the club. This is an apples-to-apples whole-app comparison.
- `scripts/pc-smoke.py` additionally drives all the gameplay flows and reads
  PlayCanvas' own frame statistics and draw-call count.

Environment: Chromium 151, headless, **SwiftShader software rasteriser** (no GPU),
arm64, viewport 1280×800, device pixel ratio 1.

## Results

| Build | rAF FPS | Frame time | Draw calls | Canvas |
| --- | --- | --- | --- | --- |
| PlayCanvas port | **2.3-2.4** | ~500 ms | 534-562 | 1280×800 |
| Three.js prototype | **1.4** | — | — | 1280×800 |

Both builds are far below real time because the rasteriser is software. The
relative result is the meaningful one: at the same viewport and with the same
scene concept, the PlayCanvas build renders about **1.6× faster** than the
Three.js prototype. On GPU hardware this scene (≈550 draw calls, shared
low-poly materials) is comfortably real-time.

`npm run smoke:pc` samples PlayCanvas' own counter while also driving input, so
it reports a lower median (≈1.5 fps) than the dedicated bench pass; both agree
that the software path is the bottleneck, not the game loop.

## Interpretation

- The bottleneck is software fragment shading at 1280×800, not CPU game logic.
  Trimming lights (6 → 5), glow quads and sparkles changed fps only marginally
  (2.3 → 2.4), which confirms fill rate dominates.
- Draw calls are the next optimisation target if more detail is added: the crowd
  already uses low-detail silhouettes and the scene shares materials, so batching
  the crowd and truss would be the first step.
- Reference-phone validation and audio policy checks remain in
  `Tasks/spec-003.md`'s ToDo list.

Reproduce with:

```sh
npm test            # checks, unit tests and the static build
npm run bench:pc    # PlayCanvas vs Three.js rAF fps, same harness
npm run smoke:pc    # headless gameplay, voice and state checks
npm run shots:pc    # gameplay screenshots for visual comparison
```
