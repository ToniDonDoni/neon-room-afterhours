# Unity WebGL end-to-end test

This Playwright happy path loads the built Unity WebGL player in Chromium, chooses a player name and outfit, enters NEON ROOM, walks to an NPC, starts a conversation, and measures non-silent Web Audio output from the browser destination while the recorded reply plays. It saves screenshots for the start screen, wardrobe, club, and reply.

Build the `neon_club` scene for WebGL in Unity, then run:

```sh
npm ci --prefix unity/tests/e2e
UNITY_WEBGL_BUILD_PATH=/path/to/webgl-build npm test --prefix unity/tests/e2e
```

Set `PLAYWRIGHT_BROWSER_CHANNEL=chrome` when using an installed Google Chrome instead of Playwright's downloaded Chromium. Otherwise install Chromium with `npx playwright install chromium`.

Playwright reports and screenshots are written under `unity/tests/e2e/test-results/`.
