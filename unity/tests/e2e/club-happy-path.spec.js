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
  const bounds = await canvas.boundingBox();
  expect(bounds).not.toBeNull();
  const scale = Math.sqrt((bounds.width / 1600) * (bounds.height / 900));
  const clickUi = (x, y) => page.mouse.click(bounds.x + bounds.width / 2 + x * scale, bounds.y + bounds.height / 2 - y * scale);
  await page.screenshot({ path: 'test-results/01-start-screen.png' });

  clickUi(0, -170);
  await page.waitForTimeout(300);
  clickUi(0, -30);
  await page.keyboard.type('Afterhours Guest');
  clickUi(0, -205);
  await page.waitForTimeout(500);
  clickUi(560, -133);
  await page.screenshot({ path: 'test-results/02-wardrobe.png' });
  clickUi(435, -225);
  await page.waitForTimeout(400);
  clickUi(145, -220);
  clickUi(0, -300);
  await page.waitForTimeout(1500);
  await page.screenshot({ path: 'test-results/03-club.png' });

  await page.keyboard.down('w');
  await page.waitForTimeout(1750);
  await page.keyboard.up('w');
  clickUi(0, -345);
  await page.waitForTimeout(250);
  clickUi(-170, -200);

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
