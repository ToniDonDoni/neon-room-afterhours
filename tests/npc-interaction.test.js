import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';
import { Vec3 } from 'playcanvas';
import { nearestNpc, blocked } from '../src/club-layout.js';
import { createNpcInteraction } from '../src/npc-interaction.js';

// Exercise the shipped HTML buttons and app action handlers without a GPU.
const html = await readFile(new URL('../src/index.html', import.meta.url), 'utf8');
const app = (await readFile(new URL('../src/app.js', import.meta.url), 'utf8'))
  .replace(/^import .*;\s*$/gm, '');

function element() {
  const listeners = new Map();
  const classes = new Set();
  return {
    hidden: false, style: {}, dataset: {}, textContent: '', value: '',
    classList: { add: x => classes.add(x), remove: x => classes.delete(x), toggle: (x, on) => on ? classes.add(x) : classes.delete(x) },
    addEventListener: (name, handler) => listeners.set(name, handler),
    emit: name => listeners.get(name)?.(),
  };
}

function game() {
  const nodes = new Map();
  for (const [, id] of html.matchAll(/id="([^"]+)"/g)) nodes.set('#' + id, element());
  const buttons = [...html.matchAll(/data-action="([^"]+)"/g)].map(([, action]) => ({ ...element(), dataset: { action } }));
  const voice = nodes.get('#npc-voice');
  const src = html.match(/<audio id="npc-voice" src="([^"]+)"/);
  assert.ok(src, 'The app must contain the recorded voice media element');
  Object.assign(voice, {
    src: src[1], currentTime: 0, volume: 1, paused: true, playCalls: 0,
    play() { this.playCalls++; this.paused = false; return this.nextPlayback ?? Promise.resolve(); },
    pause() { this.paused = true; },
  });
  const events = new Map();
  const musicGains = [];
  const context = vm.createContext({
    Vec3, nearestNpc, blocked, createNpcInteraction, looks: Array.from({ length: 10 }, () => ['Look', '#fff']), lookNotes: [],
    createJoystick: () => ({ reset() {} }),
    matchMedia: () => ({ matches: false }), localStorage: { getItem: () => null, setItem() {} },
    innerWidth: 800, innerHeight: 600, performance: { now: () => 1000 }, requestAnimationFrame() {}, setTimeout() {}, clearTimeout() {},
    window: { addEventListener() {}, scrollTo() {} },
    document: {
      hidden: false, body: element(), pointerLockElement: null,
      createElement: () => ({ ...element(), getContext: () => ({ createRadialGradient: () => ({ addColorStop() {} }), fillRect() {} }) }),
      querySelector(selector) { if (!nodes.has(selector)) nodes.set(selector, element()); return nodes.get(selector); },
      querySelectorAll: selector => selector === '[data-action]' ? buttons : [],
      addEventListener: (name, fn) => events.set(name, fn),
    },
    musicGains, console,
  });
  vm.runInContext(app, context);
  vm.runInContext(`
    playing = true;
    profile.volume = .7;
    camera = {position:new Vec3()};
    camera.position.set(0, 1.66, 0);
    const guest = {position:new Vec3(),lookAt(){}};
    guest.position.set(0, 0, -1);
    guest.userData = {name: 'Luna'};
    npcs.push(guest);
    audio = {enabled: true, beat: 50, ctx: {currentTime: 10, suspend() {}, resume() {}}, master: {gain: {setTargetAtTime: value => musicGains.push(value)}}};
  `, context);
  return {
    voice, context, nodes, musicGains, events,
    run: code => vm.runInContext(code, context),
    click(action) { const button = buttons.find(b => b.dataset.action === action); assert.ok(button, `${action} button exists`); button.onclick(); },
  };
}

test('clicking Talk near an NPC plays the shipped voice, ducks music, then restores it', async () => {
  const g = game();
  g.click('talk');
  assert.equal(g.voice.playCalls, 1);
  assert.equal(g.voice.paused, false);
  assert.equal(g.voice.volume, .7);
  assert.equal(g.voice.loop, false);
  assert.equal(g.nodes.get('#npc-speaking').hidden, false);
  assert.ok(g.musicGains.at(-1) < .7 * .45);
  assert.equal(g.run('audio.enabled'), true);
  assert.equal(g.run('audio.beat'), 50, 'The music clock must not restart');
  const bytes = await readFile(new URL('../src/' + g.voice.src, import.meta.url));
  assert.ok(bytes.byteLength > 1000, 'The referenced recording must be packaged');
  assert.equal(bytes.subarray(0, 3).toString(), 'ID3');
  g.voice.emit('ended');
  assert.equal(g.nodes.get('#npc-speaking').hidden, true);
  assert.equal(g.musicGains.at(-1), .7 * .45);
  g.click('talk');
  assert.equal(g.voice.playCalls, 2, 'Another conversation can start after the reply ends');
});

test('social gestures also play the recording; distant or absent NPCs do not', () => {
  for (const action of ['heart', 'wave', 'five']) {
    const g = game();
    g.click(action);
    assert.equal(g.voice.playCalls, 1, `${action} should trigger a reply`);
  }
  const far = game();
  far.run('npcs[0].position.z = -12');
  far.click('talk');
  assert.equal(far.voice.playCalls, 0);
  const empty = game();
  empty.run('npcs.length = 0');
  empty.click('talk');
  assert.equal(empty.voice.playCalls, 0);
});

test('proximity alone, dancing and getting a drink do not trigger speech', () => {
  const g = game();
  assert.equal(g.voice.playCalls, 0);
  g.click('dance');
  g.run('camera.position.set(-8, 1.66, 0)');
  g.click('drink');
  assert.equal(g.voice.playCalls, 0);
});

test('rapid repeated interactions cannot overlap or restart a reply', () => {
  const g = game();
  g.click('talk');
  g.voice.currentTime = 2;
  g.click('talk');
  g.click('wave');
  assert.equal(g.voice.playCalls, 1);
  assert.equal(g.voice.currentTime, 2);
});

test('blocked playback restores music and permits a later retry', async () => {
  const g = game();
  g.voice.nextPlayback = Promise.reject(new Error('Media playback blocked'));
  g.click('talk');
  await Promise.resolve();
  assert.equal(g.nodes.get('#npc-speaking').hidden, true);
  assert.equal(g.musicGains.at(-1), .7 * .45);
  assert.match(g.nodes.get('#toast').textContent, /Could not play the voice/);
  g.voice.nextPlayback = Promise.resolve();
  g.click('talk');
  assert.equal(g.voice.playCalls, 2);
});

test('opening a menu, hiding the tab or pressing Stop ends speech without muting music', () => {
  for (const stop of [
    g => g.nodes.get('#settings').onclick(),
    g => { g.context.document.hidden = true; g.events.get('visibilitychange')(); },
    g => g.nodes.get('#stop-npc-voice').onclick(),
  ]) {
    const g = game();
    g.click('talk');
    stop(g);
    assert.equal(g.voice.paused, true);
    assert.equal(g.voice.currentTime, 0);
    assert.equal(g.nodes.get('#npc-speaking').hidden, true);
    assert.equal(g.run('audio.enabled'), true);
  }
});


test('both input modes move the camera, simultaneous mobile look turns it, and walking stops dancing', () => {
  const g = game();
  g.run(`renderer = {draw(){}};document.querySelector('#modal').hidden=true;controlMode='desktop';keys.add('KeyW');dancing=true;animate(16)`);
  assert.ok(g.run('camera.position.z') < 0);
  assert.equal(g.run('dancing'),false);
  const old = g.run('camera.position.z');
  g.run(`keys.clear();controlMode='mobile';touchInput.y=-1;touchInput.lookX=1;animate(32)`);
  assert.ok(g.run('camera.position.z') < old);
  assert.ok(g.run('yaw') < 0);
  const position = g.run('camera.position.z');
  g.run(`document.querySelector('#modal').hidden=false;animate(48)`);
  assert.equal(g.run('camera.position.z'),position,'Menus pause movement');
});

test('bar drink restriction and toggle survive the engine migration', () => {
  const g=game();g.click('drink');assert.equal(g.run('drink'),false);
  g.run('camera.position.set(-6,1.66,1)');g.click('drink');assert.equal(g.run('drink'),false);
  g.run('camera.position.set(-7,1.66,1)');g.click('drink');assert.equal(g.run('drink'),true);
  assert.equal(g.nodes.get('#drink-view').hidden,false);
  g.click('drink');assert.equal(g.run('drink'),false);assert.equal(g.nodes.get('#drink-view').hidden,true);
});

test('all three original music loops and saved profile key remain available', () => {
  const g=game();
  assert.deepEqual(Array.from(g.run('tracks.map(t=>t.bpm)')),[124,118,132]);
  for(let i=0;i<3;i++) assert.equal(g.run(`tracks[${i}].notes.length`),8);
  assert.match(app,/localStorage.getItem\('neon-room'\)/);
});
