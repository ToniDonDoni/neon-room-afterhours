import { expect, test } from 'playwright/test';

test('player chooses a look and club, walks to an NPC, talks, and hears the recorded reply', async ({ page }) => {
  page.on('console', message => {
    if (message.type() === 'error') console.error(`Browser console: ${message.text()}`);
  });
  page.on('pageerror', error => console.error(`Browser page error: ${error.message}`));

  await page.addInitScript(() => {
    const originalConnect = AudioNode.prototype.connect;
    window.__neonRoomAudioMeter = null;
    AudioNode.prototype.connect = function (destination, ...args) {
      if (destination instanceof AudioDestinationNode && !window.__neonRoomAudioMeter) {
        const analyser = this.context.createAnalyser();
        analyser.fftSize = 2048;
        analyser.smoothingTimeConstant = 0;
        window.__neonRoomAudioMeter = analyser;
        originalConnect.call(this, analyser);
        return originalConnect.call(analyser, destination, ...args);
      }
      return originalConnect.call(this, destination, ...args);
    };
  });

  await page.goto('/');
  const canvas = page.locator('#unity-canvas');
  await expect(canvas).toBeVisible();
  await expect(page.locator('#unity-loading-bar')).toBeHidden({ timeout: 120_000 });
  const state = page.locator('#neon-room-state');
  await expect(state).toBeVisible();
  await expect(state).toHaveAttribute('data-screen', 'start-screen');
  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();
  const scale = Math.sqrt((bounds.width / 1600) * (bounds.height / 900));
  const clickUi = async (x, y) => {
    await canvas.evaluate(element => element.focus());
    await canvas.click({ position: { x: bounds.width / 2 + x * scale, y: bounds.height / 2 - y * scale } });
    await page.waitForTimeout(100);
  };
  await page.screenshot({ path: 'test-results/01-start-screen.png' });
  await expectVisibleEnglishUi(page, 'ENTER THE NIGHT');

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
  await clickUi(-300, -100);
  await expect(state).toHaveAttribute('data-location', 'neon-room');
  await clickUi(145, -220);
  await clickUi(0, -300);
  await expect(state).toHaveAttribute('data-screen', 'club-hud');
  await expectVisibleEnglishUi(page, 'NEON ROOM');
  await page.screenshot({ path: 'test-results/03-club.png' });

  await page.keyboard.down('w');
  await expect(state).toHaveAttribute('data-near-npc', 'true', { timeout: 10_000 });
  await page.keyboard.up('w');
  await clickUi(0, -345);
  await expectVisibleEnglishUi(page, 'TALK');
  await clickUi(-170, -200);
  await expect(state).toHaveAttribute('data-reply-playing', 'true');
  await expect(state).toHaveAttribute('data-voice-clip', 'npc-voice');
  await expect(state).toHaveAttribute('data-reply', 'LUNA  ·  I RECOGNIZE THIS MUSIC');
  await expectVisibleEnglishUi(page, 'I RECOGNIZE THIS MUSIC');

  const audibleReply = await page.waitForFunction(() => {
    const analyser = window.__neonRoomAudioMeter;
    if (!analyser) return false;
    const samples = new Float32Array(analyser.fftSize);
    analyser.getFloatTimeDomainData(samples);
    const rms = Math.sqrt(samples.reduce((sum, sample) => sum + sample * sample, 0) / samples.length);
    return rms > 0.0001 ? rms : false;
  }, null, { timeout: 15_000 });

  expect(await audibleReply.jsonValue()).toBeGreaterThan(0.0001);
  await page.screenshot({ path: 'test-results/04-npc-reply-playing.png' });
});

async function expectVisibleEnglishUi(page, expectedCopy) {
  const copy = page.locator('#neon-room-ui-copy');
  await expect(copy).toContainText(expectedCopy);
  const displayedText = await copy.textContent();
  expect(displayedText).not.toMatch(/[\u0400-\u04ff]/u);
}
