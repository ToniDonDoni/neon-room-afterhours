# NEON ROOM — PlayCanvas port visual comparison

## Reference

`reference/club-meeting-reference.webp` is the mood reference from the brief
(`reference/club-meeting-reference.webp` is the original). It shows the target:
a crowded neon club washed in hot magenta on one side and electric cyan on the
other, a platinum-haired NPC in dark glossy leather and shades, the player's
first-person hands and bracelets forming a heart, and the round action wheel on
the right with heart / wave / dance / drink plus the "people" button.

## Automation

`scripts/pc-shots.py` serves the built site, drives the whole onboarding flow
(headless Chromium + SwiftShader), poses the debug camera surface
(`window.__neonRoom.pose / focus / act`), triggers the action wheel and gestures,
and writes 14 PNGs plus a manifest to `screenshots/`:

| File | What it shows |
| --- | --- |
| `00-landing.jpg` | landing hero |
| `01-wardrobe.jpg` | wardrobe with the live avatar preview |
| `10-entrance.jpg` | entrance gallery looking into the club |
| `20-dancefloor.jpg` | dancefloor towards the DJ stage |
| `21-crowd.jpg` | dancing crowd close up |
| `30-bar.jpg` / `31-drink.jpg` | bar and the neon tonic |
| `40-stage.jpg` / `41-lounge.jpg` | DJ stage and lounge booths |
| `50-meeting-wheel.jpg` | NPC encounter with the action wheel open |
| `51-meeting-heart.jpg` | heart gesture, NPC reply, wheel open |
| `52-meeting-wave.jpg` | wave gesture close up |
| `60-people.jpg` / `61-map.jpg` | people and map modals |
| `comparison-meeting.jpg` | reference vs the meeting shot, stacked |
| `comparison-club.jpg` | reference vs the dancefloor shot, stacked |

`index.html?shots=1` enables `preserveDrawingBuffer` so the headless browser can
capture the WebGL frame; normal play keeps the faster default.

## What matches

- **Palette and light:** hot magenta vs cyan wash, violet haze, acid-green UI
  accents, glossy dark materials with neon trim.
- **Composition:** a close NPC in dark leather with platinum hair and shades,
  first-person hands and bracelets in the foreground, the glowing action wheel
  on the right, the people control at the lower right.
- **Scene:** dense crowd around the dancefloor, bar line with glowing cups,
  lounge booths, DJ stage with rings and speakers, ceiling truss.

## What differs

- The port is built from PlayCanvas primitives, so people, stage and furniture
  are stylised low-poly shapes rather than the photoreal render of the brief.
- Crowd density is faked with ~26 non-interactive silhouettes; the 12
  interactive NPCs from the spec are unchanged.
- God rays, depth of field and bloom from the reference are approximated with
  additive glow quads, not a post-processing stack (keeps the buildless,
  dependency-free constraint).

Verdict: the composition and mood read as the same club and the same kind of
encounter; it is a stylised port of the reference, not a photoreal copy.
