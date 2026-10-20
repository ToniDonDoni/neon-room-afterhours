import { expect, test } from 'playwright/test';

test('phone players choose touch controls and use both sticks and the on-screen action wheel', async ({ browser }) => {
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
    deviceScaleFactor: 1,
    userAgent: 'Mozilla/5.0 (Linux; Android 14; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36'
  });
  const page = await context.newPage();
  await page.goto('/');
  const canvas = page.locator('#unity-canvas');
  const state = page.locator('#neon-room-state');
  await expect(page.locator('#unity-loading-bar')).toBeHidden({ timeout: 120_000 });
  await expect(state).toHaveAttribute('data-screen', 'start-screen');

  const scale = async () => {
    const bounds = await canvas.boundingBox();
    expect(bounds).not.toBeNull();
    return {
      bounds,
      scale: Math.sqrt((bounds.width / 1600) * (bounds.height / 900))
    };
  };
  const tapUi = async (x, y) => {
    const { bounds, scale: uiScale } = await scale();
    await page.touchscreen.tap(bounds.x + bounds.width / 2 + x * uiScale, bounds.y + bounds.height / 2 - y * uiScale);
    await page.waitForTimeout(160);
  };
  const chooseSuggestedName = async () => {
    await tapUi(-250, -112);
    await tapUi(0, -205);
  };

  await tapUi(0, -170);
  await expect(state).toHaveAttribute('data-screen', 'nickname-screen');
  await chooseSuggestedName();
  await expect(state).toHaveAttribute('data-screen', 'wardrobe-screen');
  await tapUi(0, -610);
  await expect(state).toHaveAttribute('data-screen', 'locations-screen');
  await tapUi(-190, -80);
  await tapUi(0, -310);
  await expect(state).toHaveAttribute('data-screen', 'control-selection-screen');
  await tapUi(-180, -70);
  await expect(state).toHaveAttribute('data-control-mode', 'mobile');
  await tapUi(0, -205);
  await expect(state).toHaveAttribute('data-screen', 'club-hud');
  await expect(state).toHaveAttribute('data-mobile-controls-visible', 'true');

  const initialPosition = await state.getAttribute('data-player-position');
  const initialYaw = await state.getAttribute('data-player-yaw');
  const { bounds } = await scale();
  const moveTouch = { x: bounds.x + bounds.width * 0.15, y: bounds.y + bounds.height * 0.88, id: 1, radiusX: 8, radiusY: 8, force: 1 };
  const lookTouch = { x: bounds.x + bounds.width * 0.85, y: bounds.y + bounds.height * 0.88, id: 2, radiusX: 8, radiusY: 8, force: 1 };
  const cdp = await context.newCDPSession(page);
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [moveTouch, lookTouch] });
  const movedTouch = { ...moveTouch, y: moveTouch.y - 34 };
  const turnedTouch = { ...lookTouch, x: lookTouch.x + 34 };
  for (let i = 0; i < 24; i += 1) {
    await cdp.send('Input.dispatchTouchEvent', {
      type: 'touchMove',
      touchPoints: [
        { ...movedTouch, y: movedTouch.y - i * 1.5 },
        { ...turnedTouch, x: turnedTouch.x + i * 1.5 }
      ]
    });
    await page.waitForTimeout(100);
  }
  await expect(state).toHaveAttribute('data-mobile-move', /^(?!0\.00,0\.00)/);
  await expect(state).toHaveAttribute('data-mobile-look', /^(?!0\.00,0\.00)/);
  await page.waitForTimeout(2_000);
  await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
  const finalPosition = (await state.getAttribute('data-player-position')).split(',').map(Number);
  const startPosition = initialPosition.split(',').map(Number);
  expect(Math.hypot(finalPosition[0] - startPosition[0], finalPosition[2] - startPosition[2])).toBeGreaterThan(0.5);
  expect(Number(await state.getAttribute('data-player-yaw'))).not.toBe(Number(initialYaw));

  await tapUi(0, 700);
  await expect(page.locator('#neon-room-ui-copy')).toContainText('TALK');
  await page.screenshot({ path: 'test-results/mobile-action-wheel.png' });
  await context.close();
});
