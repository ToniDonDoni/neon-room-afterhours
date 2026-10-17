# Playwright Manual Game Test Guide

## Purpose

Use this guide when you need to play-test a browser game with Playwright without opening a visible browser window for the user.

The example game is a canvas game in which neon cats fly across the screen. The player catches a cat with the mouse, drags it to a glass on the right side of the screen, releases it, and verifies that the cat becomes a dissolving pill and increases the score.

## Core idea

Playwright is the executor, not the game-playing strategy. The model must repeatedly:

1. Observe the current page with a screenshot and/or JavaScript state.
2. Locate a playable cat and the glass.
3. Convert game coordinates to browser coordinates.
4. Move the virtual mouse to the cat.
5. Press and hold the mouse button.
6. Drag through several intermediate points toward the glass.
7. Wait briefly while the game processes the drop.
8. Release the mouse button.
9. Verify the score, pill count, and cat state.
10. Take another screenshot if the result is unexpected and adapt the next attempt.

Playwright's headless browser still renders the real page. `page.screenshot()` captures the pixels that would be shown in a visible browser, while `page.mouse` sends browser mouse events to the page.

## Start the local game

Do not open the HTML file with `file://`. Start a local HTTP server from the game directory first:

```bash
python3 -m http.server 8765
```

Then use:

```text
http://localhost:8765/index.html
```

If the game has a camera mode, disable or deny camera access for a mouse-only test. A pending `getUserMedia()` request can leave the game on an `Initializing...` screen and prevent the mouse fallback from starting.

## Recommended browser setup

Use an isolated, headless Chromium context:

```js
import { chromium } from '@playwright/test';

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({
  viewport: { width: 1633, height: 860 }
});
const page = await context.newPage();

await page.goto('http://localhost:8765/index.html');
await page.waitForFunction(() =>
  window.__neonKitty?.state === 'PLAYING'
);
```

Use `headless: false` only when the user explicitly wants to watch the browser window.

## Observe the game

Canvas games often have very little useful DOM. Prefer an exposed test/debug object when one exists, but also capture screenshots so visual rendering is verified.

For the example game, `window.__neonKitty` exposes the useful state:

```js
const state = await page.evaluate(() => {
  const game = window.__neonKitty;
  const canvas = document.querySelector('#game-canvas');
  const rect = canvas.getBoundingClientRect();

  return {
    state: game?.state,
    score: game?.score,
    lives: game?.lives,
    speed: game?.gameSpeed,
    cats: game?.cats?.map(cat => ({
      x: cat.x,
      y: cat.y,
      state: cat.state,
      size: cat.size
    })),
    glass: game?.glass ? {
      x: game.glass.x,
      y: game.glass.y,
      width: game.glass.w,
      height: game.glass.h,
      openTop: game.glass.openTop
    } : null,
    canvas: {
      width: canvas.width,
      height: canvas.height,
      cssWidth: rect.width,
      cssHeight: rect.height
    }
  };
});

await page.screenshot({ path: 'game-observation.png' });
console.log(state);
```

If no debug state is available, use screenshots and computer vision to estimate the cat and glass positions. Do not assume that DOM element coordinates exist for objects drawn inside a canvas.

## Convert canvas coordinates correctly

The example game uses an internal canvas size of `1280x720`, but CSS may stretch it to the current browser viewport. Game coordinates and browser mouse coordinates are therefore not always identical.

Always calculate the scale from the actual canvas rectangle:

```js
const scaleX = state.canvas.cssWidth / state.canvas.width;
const scaleY = state.canvas.cssHeight / state.canvas.height;

const toBrowserPoint = (point) => ({
  x: point.x * scaleX,
  y: point.y * scaleY
});
```

If the canvas has a non-zero `left` or `top` offset, add those offsets to the converted coordinates.

This conversion is essential when the canvas is displayed at a size different from its internal drawing resolution.

## Perform a reliable cat drag

Read the cat position immediately before starting the drag. A cat is moving, so coordinates collected too early can already be stale.

```js
async function findPlayableTarget(page) {
  return page.evaluate(() => {
    const game = window.__neonKitty;
    const canvas = document.querySelector('#game-canvas');
    const rect = canvas.getBoundingClientRect();
    const cat = game.cats.find(item =>
      item.state === 'FLYING' &&
      item.x > 40 && item.x < canvas.width - 40 &&
      item.y > 40 && item.y < canvas.height - 40
    );

    if (!cat || !game.glass) return null;

    return {
      cat: { x: cat.x, y: cat.y },
      glass: {
        x: game.glass.x + game.glass.w / 2,
        y: game.glass.y + game.glass.openTop + 80
      },
      canvas: {
        left: rect.left,
        top: rect.top,
        width: rect.width,
        height: rect.height,
        internalWidth: canvas.width,
        internalHeight: canvas.height
      },
      score: game.score
    };
  });
}

async function dragCatToGlass(page, target) {
  const scaleX = target.canvas.width / target.canvas.internalWidth;
  const scaleY = target.canvas.height / target.canvas.internalHeight;
  const toBrowserPoint = point => ({
    x: target.canvas.left + point.x * scaleX,
    y: target.canvas.top + point.y * scaleY
  });

  const start = toBrowserPoint(target.cat);
  const end = toBrowserPoint(target.glass);

  await page.mouse.move(start.x, start.y, { steps: 8 });
  await page.mouse.down();
  await page.waitForTimeout(100);

  // Use intermediate points so the browser emits a real drag path.
  for (let index = 1; index <= 24; index += 1) {
    const progress = index / 24;
    await page.mouse.move(
      start.x + (end.x - start.x) * progress,
      start.y + (end.y - start.y) * progress
    );
    await page.waitForTimeout(35);
  }

  // Give the game one or two frames to settle the cat inside the glass.
  await page.waitForTimeout(300);
  await page.mouse.up();
}
```

For a slow-motion game, do not multiply the mouse-follow delay by the game speed. The player-controlled cat should remain under the pointer while the rest of the simulation is slowed down.

## Verify success

The successful result should be checked through game state, not only by assuming that the mouse command completed:

```js
const before = await page.evaluate(() => ({
  score: window.__neonKitty.score,
  pills: window.__neonKitty.pills.length
}));

const target = await findPlayableTarget(page);
if (!target) {
  throw new Error('No playable cat was found');
}

await dragCatToGlass(page, target);
await page.waitForTimeout(200);

const after = await page.evaluate(() => ({
  score: window.__neonKitty.score,
  pills: window.__neonKitty.pills.length,
  state: window.__neonKitty.state
}));

if (after.score <= before.score) {
  await page.screenshot({ path: 'failed-cat-drop.png' });
  throw new Error(`Cat drop failed: ${JSON.stringify({ before, after })}`);
}

console.log(`Cat delivered successfully. Score: ${after.score}`);
```

A successful test should normally establish all of the following:

- The page reaches `PLAYING`.
- A cat reaches `CAUGHT` after mouse-down.
- The cat is delivered to the glass after mouse-up.
- The score increases.
- A pill is created or the pill animation begins.
- The game remains responsive after the drop.

## Troubleshooting

### The page stays on `Initializing...`

Check whether the page is waiting for a camera permission or a camera promise. For a mouse-only test, disable the camera path or explicitly configure the browser context so camera access is denied and the fallback starts.

### The visual click misses the cat

Check the canvas CSS rectangle and internal dimensions. Missing the scale conversion is the most common cause when the canvas uses `width: 100%` and `height: 100%`.

### The cat is caught but the drop fails

The cat may be following the pointer with interpolation and still be behind the cursor at mouse-up. Use a slower multi-point drag and pause over the glass, or make the caught cat track the pointer directly. Verify the cat position before releasing.

### The game ends before the test can act

Reduce the game speed, wait for a cat that is well inside the play area, and start the drag immediately after reading its position. A visible in-game speed slider is useful for repeatable manual and automated testing.

### A test passes without proving that a cat was caught

Do not assert only that the score element exists or that the score is non-negative. Assert that the score increases, the cat state changes, or a pill is created. Save a failure screenshot when the assertion fails.

## Cleanup

Close the browser after headless verification:

```js
await browser.close();
```

Do not inspect or modify browser cookies, saved credentials, or unrelated user tabs during a local game test.
