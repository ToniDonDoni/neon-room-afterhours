import { expect, test } from 'playwright/test';

test('player chooses a look and club, walks to an NPC, talks, and hears the recorded reply', async ({ page }) => {
  page.on('console', message => {
    if (message.type() === 'error') console.error(`Browser console: ${message.text()}`);
  });
  page.on('pageerror', error => console.error(`Browser page error: ${error.message}`));

  await page.addInitScript(() => {
    const originalConnect = AudioNode.prototype.connect;
    window.__neonRoomAudioMeters = [];
    AudioNode.prototype.connect = function (destination, ...args) {
      if (destination instanceof AudioDestinationNode) {
        const output = originalConnect.call(this, destination, ...args);
        const analyser = this.context.createAnalyser();
        analyser.fftSize = 2048;
        analyser.smoothingTimeConstant = 0;
        window.__neonRoomAudioMeters.push(analyser);
        window.__neonRoomPeakRms = 0;
        originalConnect.call(this, analyser);
        const silentMonitor = this.context.createGain();
        silentMonitor.gain.value = 0;
        originalConnect.call(analyser, silentMonitor);
        originalConnect.call(silentMonitor, destination);
        if (!window.__neonRoomAudioSampling) {
          window.__neonRoomAudioSampling = window.setInterval(() => {
            for (const meter of window.__neonRoomAudioMeters) {
              const samples = new Float32Array(meter.fftSize);
              meter.getFloatTimeDomainData(samples);
              const rms = Math.sqrt(samples.reduce((sum, sample) => sum + sample * sample, 0) / samples.length);
              window.__neonRoomPeakRms = Math.max(window.__neonRoomPeakRms, rms);
            }
          }, 20);
        }
        return output;
      }
      return originalConnect.call(this, destination, ...args);
    };
  });

  await page.goto('/');
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible();
  await expect(page.locator('#unity-build-title')).toHaveText('NEON ROOM');
  await expect(page).toHaveTitle('Unity Web Player | NEON ROOM');
  await expect(page.locator('#unity-loading-bar')).toBeHidden({ timeout: 120_000 });
  const state = page.locator('#neon-room-state');
  await expect(state).toBeVisible();
  await expect(state).toHaveAttribute('data-screen', 'start-screen');
  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();
  await expect.poll(async () => await canvas.evaluate(element => element.width * element.height)).toBeGreaterThan(500_000);
  const scale = Math.sqrt((bounds.width / 1600) * (bounds.height / 900));
  const clickUi = async (x, y) => {
    await canvas.evaluate(element => element.focus());
    await canvas.click({ position: { x: bounds.width / 2 + x * scale, y: bounds.height / 2 - y * scale } });
    await page.waitForTimeout(100);
  };
  await expectVisibleEnglishUi(page, 'ENTER THE NIGHT');
  // Capture the actual Unity canvas. The PlayMode acceptance test also asserts
  // that the Unity splash is disabled, so this frame is the game's start screen.
  const startScreenCapture = await canvas.screenshot({ path: 'test-results/01-start-screen.png' });
  expect(startScreenCapture.byteLength).toBeGreaterThan(15_000);

  await clickUi(0, -170);
  await expect(state).toHaveAttribute('data-screen', 'nickname-screen');
  await expectVisibleEnglishUi(page, 'WHAT SHOULD WE CALL YOU?');
  await page.waitForTimeout(300);
  await clickUi(0, -30);
  await page.keyboard.type('Afterhours Guest');
  await clickUi(0, -205);
  await expect(state).toHaveAttribute('data-player-name', 'Afterhours Guest');
  await expect(state).toHaveAttribute('data-screen', 'wardrobe-screen');
  await expectVisibleEnglishUi(page, 'DRESS FOR YOURSELF');
  await clickUi(560, -133);
  await expect(state).toHaveAttribute('data-outfit', '9');
  await page.screenshot({ path: 'test-results/02-wardrobe.png' });
  await clickUi(435, -225);
  await expect(state).toHaveAttribute('data-screen', 'locations-screen');
  await expectVisibleEnglishUi(page, 'FOLLOW THE FREQUENCY');
  await expectVisibleEnglishUi(page, 'DANCE FLOOR');
  await clickUi(-300, -100);
  await expect(state).toHaveAttribute('data-location', 'neon-room');
  await clickUi(0, -250);
  await expect(state).toHaveAttribute('data-screen', 'control-selection-screen');
  await clickUi(180, -70);
  await clickUi(0, -205);
  await expect(state).toHaveAttribute('data-screen', 'club-hud');
  await expectVisibleEnglishUi(page, 'NEON ROOM');
  await page.evaluate(() => { window.__neonRoomPeakRms = 0; });
  await page.waitForFunction(() => window.__neonRoomPeakRms > 0.0001, null, { timeout: 15_000 });
  await page.screenshot({ path: 'test-results/03-club.png' });

  await page.keyboard.down('w');
  await expect(state).toHaveAttribute('data-near-npc', 'true', { timeout: 10_000 });
  await page.keyboard.up('w');
  await clickUi(0, -345);
  await expectVisibleEnglishUi(page, 'TALK');
  await page.screenshot({ path: 'test-results/04-action-wheel.png' });
  await clickUi(0, 10);
  await expect(state).toHaveAttribute('data-reply-playing', 'true');
  await expect(state).toHaveAttribute('data-voice-clip', 'npc-voice');
  await expect(state).toHaveAttribute('data-voice-load-state', 'Loaded');
  expect(Number(await state.getAttribute('data-voice-volume'))).toBeGreaterThan(0);
  await expect(state).toHaveAttribute('data-reply', 'LUNA  ·  I RECOGNIZE THIS MUSIC');
  await expectVisibleEnglishUi(page, 'I RECOGNIZE THIS MUSIC');
  await page.evaluate(() => { window.__neonRoomPeakRms = 0; });
  await page.waitForFunction(() => window.__neonRoomPeakRms > 0.0001, null, { timeout: 15_000 });
  expect(await page.evaluate(() => window.__neonRoomPeakRms)).toBeGreaterThan(0.0001);
  await page.screenshot({ path: 'test-results/05-npc-reply-playing.png' });
});

async function expectVisibleEnglishUi(page, expectedCopy) {
  const copy = page.locator('#neon-room-ui-copy');
  await expect(copy).toContainText(expectedCopy);
  const displayedText = await copy.textContent();
  expect(displayedText).not.toMatch(/[\u0400-\u04ff]/u);
}
