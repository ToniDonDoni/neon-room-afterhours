import { defineConfig } from 'playwright/test';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const buildPath = process.env.UNITY_WEBGL_BUILD_PATH || path.resolve(here, '../../build/webgl');

export default defineConfig({
  testDir: '.',
  testMatch: '**/*.spec.js',
  timeout: 240_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  use: {
    baseURL: 'http://127.0.0.1:4173',
    browserName: 'chromium',
    channel: process.env.PLAYWRIGHT_BROWSER_CHANNEL || undefined,
    headless: true,
    viewport: { width: 1600, height: 900 },
    launchOptions: { args: ['--autoplay-policy=no-user-gesture-required'] },
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
    video: 'retain-on-failure'
  },
  webServer: {
    command: `node ${JSON.stringify(path.join(here, 'serve-webgl.mjs'))}`,
    url: 'http://127.0.0.1:4173',
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
    env: { UNITY_WEBGL_BUILD_PATH: buildPath }
  },
  outputDir: 'test-results'
});
