import test from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

import {
  looks, lookNotes, lookStyles, tracks, zoneForPosition, isBlocked,
  sanitizeProfile, defaultControlMode, nearestNpc, canTakeDrink, mapTargets,
  npcNames, npcPositions, dancingNpcCount, defaultProfile
} from '../playcanvas/logic.js';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const read = path => readFileSync(join(root, path), 'utf8');

test('ten looks, notes and silhouette families are present', () => {
  assert.equal(looks.length, 10);
  assert.equal(lookNotes.length, 10);
  assert.equal(lookStyles.length, 10);
  assert.ok(lookStyles.every(style => Number.isInteger(style) && style >= 0 && style <= 5));
});

test('the PlayCanvas look table matches the canonical Three.js source', () => {
  const source = read('src/fashion.js');
  const start = source.indexOf('export const looks = [');
  const end = source.indexOf('];', start);
  const block = source.slice(start, end);
  const canonical = [...block.matchAll(/\[\s*'([^']+)'\s*,\s*'(#[0-9a-fA-F]+)'\s*,\s*'([^']+)'\s*\]/g)]
    .map(match => [match[1], match[2], match[3]]);
  assert.deepEqual(looks, canonical);
});

test('three procedural tracks are available', () => {
  assert.equal(tracks.length, 3);
  for (const track of tracks) {
    assert.ok(track.bpm > 0);
    assert.equal(track.notes.length, 8);
  }
});

test('zone detection follows the club layout', () => {
  assert.equal(zoneForPosition(0, 10), 'entry');
  assert.equal(zoneForPosition(-8, 0), 'bar');
  assert.equal(zoneForPosition(8, 0), 'lounge');
  assert.equal(zoneForPosition(0, -8), 'stage');
  assert.equal(zoneForPosition(0, 0), 'dancefloor');
});

test('the collision map matches the reference rectangles', () => {
  assert.equal(isBlocked(0, 0), false);
  assert.equal(isBlocked(0, 20), true);
  assert.equal(isBlocked(-9, 0), true);
  assert.equal(isBlocked(0, -9), true);
  assert.equal(isBlocked(9, 5), true);
  assert.equal(isBlocked(7.8, -1), true);
  assert.equal(isBlocked(5, 5), false);
});

test('profile sanitising clamps outfit and volume', () => {
  assert.equal(sanitizeProfile({ outfit: 99 }).outfit, 9);
  assert.equal(sanitizeProfile({ outfit: -3 }).outfit, 0);
  assert.equal(sanitizeProfile({ volume: 5 }).volume, 1);
  assert.equal(sanitizeProfile({ volume: -1 }).volume, 0);
  assert.equal(sanitizeProfile({ volume: 'nope' }).volume, defaultProfile.volume);
  assert.equal(sanitizeProfile({ controls: 'gamepad' }).controls, undefined);
  assert.equal(sanitizeProfile({ controls: 'mobile' }).controls, 'mobile');
});

test('default control mode prefers the saved choice', () => {
  assert.equal(defaultControlMode({}, true), 'mobile');
  assert.equal(defaultControlMode({}, false), 'desktop');
  assert.equal(defaultControlMode({ controls: 'desktop' }, true), 'desktop');
});

test('nearest npc uses 3d distance', () => {
  const target = nearestNpc({ x: 0, y: 1.66, z: 0 }, [{ x: 0, y: 0, z: 1 }, { x: 0, y: 0, z: -5 }]);
  assert.equal(target.npc.z, 1);
});

test('a drink is only available near the bar', () => {
  assert.equal(canTakeDrink(-8, 0), true);
  assert.equal(canTakeDrink(0, 0), false);
  assert.equal(canTakeDrink(0, 8), false);
});

test('the map offers four destinations', () => {
  assert.deepEqual(Object.keys(mapTargets).sort(), ['0', '1', '2', '3']);
  for (const target of Object.values(mapTargets)) {
    assert.ok(Number.isFinite(target.x) && Number.isFinite(target.z) && Number.isFinite(target.yaw));
  }
});

test('twelve named npcs populate the club and seven dance', () => {
  assert.equal(npcNames.length, 12);
  assert.equal(npcPositions.length, 12);
  assert.equal(dancingNpcCount, 7);
});

test('shared input and interaction modules stay in sync with src/', () => {
  assert.equal(read('playcanvas/joysticks.js'), read('src/controls.js'));
  assert.equal(read('playcanvas/npc-interaction.js'), read('src/npc-interaction.js'));
});

test('the PlayCanvas entry point loads the engine and the app', () => {
  const html = read('playcanvas/index.html');
  assert.match(html, /vendor\/playcanvas\.min\.js/);
  assert.match(html, /type="module" src="app\.js"/);
  assert.ok(read('playcanvas/vendor/playcanvas.min.js').length > 100000);
});

test('the app uses a PlayCanvas application and keeps every action', () => {
  const app = read('playcanvas/app.js');
  assert.match(app, /new pc\.Application/);
  for (const action of ['talk', 'heart', 'wave', 'five', 'dance', 'drink']) {
    assert.ok(app.includes(action), 'missing action ' + action);
  }
  assert.match(app, /npcInteraction\.interact/);
  assert.match(app, /createClubAudio/);
});

test('the scene builds the club, the crowd and the lasers', () => {
  const scene = read('playcanvas/scene.js');
  assert.match(scene, /export function buildClub/);
  assert.match(scene, /npcPositions/);
  assert.match(scene, /lasers/);
  assert.match(scene, /scene.crowd/);
});

test('the world is revealed before the canvas is created', () => {
  const app = read('playcanvas/app.js');
  const enterClub = app.slice(app.indexOf('function enterClub()'));
  const reveal = enterClub.indexOf("classList.add('playing')");
  const init = enterClub.indexOf('initWorld()');
  assert.ok(reveal >= 0 && init >= 0, 'enterClub must reveal the world and init it');
  assert.ok(reveal < init, 'body.playing must be set before initWorld so the canvas is not 0x0');
  assert.match(app, /app.resizeCanvas\(/);
  assert.match(read('scripts/pc-smoke.py'), /canvasSized/);
});

test('the brief reference and the screenshot automation are committed', () => {
  assert.ok(existsSync(join(root, 'playcanvas/reference/club-meeting-reference.webp')));
  assert.ok(existsSync(join(root, 'playcanvas/VISUAL-NOTES.md')));
  const shots = read('scripts/pc-shots.py');
  assert.match(shots, /shots=1/);
  assert.match(shots, /focus\(/);
  assert.ok(read('playcanvas/app.js').includes('preserveDrawingBuffer'));
});
