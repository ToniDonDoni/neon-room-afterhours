// NEON ROOM - PlayCanvas port of the Three.js prototype in src/.
// Product logic, UI and audio follow src/app.js; rendering and scene graph use PlayCanvas.

import {
  looks, lookNotes, tracks, zoneForPosition, zoneLabels, isBlocked,
  sanitizeProfile, defaultControlMode, mapTargets, canTakeDrink
} from './logic.js';
import { createClubAudio } from './audio.js';
import { createJoystick } from './joysticks.js';
import { createNpcInteraction } from './npc-interaction.js';
import { createAvatar, createFirstPersonHands } from './avatar.js';
import { buildClub } from './scene.js';
import { standard, neon, light, cylinder, torus } from './primitives.js';

const $ = s => document.querySelector(s);
const $$ = s => [...document.querySelectorAll(s)];
const outfits = looks;

let profile = { name: '', outfit: 5, volume: .35 };
try { profile = sanitizeProfile(Object.assign({}, profile, JSON.parse(localStorage.getItem('neon-room') || '{}'))); } catch { profile = sanitizeProfile(profile); }

function save() { try { localStorage.setItem('neon-room', JSON.stringify(profile)); } catch {} }

let playing = false, step = '', started = false, dancing = false, drink = false;
const lowMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;
let quality = matchMedia('(max-width:650px)').matches ? 'low' : 'high';
let controlMode = defaultControlMode(profile, matchMedia('(pointer:coarse)').matches);

const touchInput = { x: 0, y: 0, lookX: 0, lookY: 0 };
const sticks = [];
const keys = new Set();
let yaw = 0, pitch = 0, toastTimer;

function resetControls() { keys.clear(); sticks.forEach(s => s.reset()); }

function toast(text) {
  $('#toast').textContent = text;
  $('#toast').classList.add('show');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => $('#toast').classList.remove('show'), 3500);
}

function unlock() {
  if (document.pointerLockElement) document.exitPointerLock();
  resetControls();
}

function openModal(html, kind) {
  npcInteraction.stop();
  unlock();
  step = kind;
  $('#modal-content').innerHTML = html;
  $('#modal').hidden = false;
}

function closeModal() { step = ''; $('#modal').hidden = true; keys.clear(); }

$('#close-modal').onclick = closeModal;
$('#modal').onclick = e => { if (e.target === $('#modal')) closeModal(); };

let audio = null, npcSpeaking = false;

const npcInteraction = createNpcInteraction({
  voice: $('#npc-voice'),
  onSpeakingChange: speaking => {
    npcSpeaking = speaking;
    $('#npc-speaking').hidden = !speaking;
    if (audio) audio.setDucked(speaking);
    setVolume();
  },
  onError: () => toast('Could not play the voice reply. Try again.')
});
$('#stop-npc-voice').onclick = () => npcInteraction.stop();

let pendingTrack = 0;
function ensureAudio() {
  if (!audio) { audio = createClubAudio(tracks); audio.setTrack(pendingTrack); }
  return audio;
}
function setVolume() { npcInteraction.setVolume(profile.volume); if (audio) audio.setVolume(profile.volume); }
function toggleAudio() {
  try {
    const instance = ensureAudio();
    const on = instance.toggle();
    if (on) instance.setSilent(false);
    setVolume();
    $('#sound').style.color = on ? 'var(--acid)' : '';
    toast(on ? 'Music on · ' + instance.trackLabel : 'Music off');
  } catch (e) {
    toast('The browser blocked audio playback');
  }
}
$('#sound').onclick = toggleAudio;
$('#game-music').onclick = toggleAudio;

function nameScreen() {
  const suggestions = ['Luna', 'Void', 'Neon Kid', 'Kira', 'Acid Soul']
    .map(n => '<button data-name="' + n + '">' + n + '</button>').join('');
  openModal('<div class="name-screen"><span class="step-label">01 / YOUR NEW SELF</span><h2>What do they call you<br>after midnight?</h2><p>A name for tonight. Your real one, or the one you always wanted.</p><input id="nickname" aria-label="Your name" maxlength="24" placeholder="Your name" autocomplete="nickname"><div class="suggestions">' + suggestions + '</div><button id="next-name" class="primary full">CHOOSE A LOOK <span>→</span></button><p style="font-size:10px">No sign-up. Your look is saved in this browser.</p></div>', 'name');
  $('#nickname').value = profile.name;
  $$('[data-name]').forEach(b => b.onclick = () => $('#nickname').value = b.dataset.name);
  $('#next-name').onclick = () => {
    const name = $('#nickname').value.trim();
    if (!name) { toast('Pick a name or type your own'); $('#nickname').focus(); return; }
    profile.name = name;
    save();
    wardrobe();
  };
  $('#nickname').onkeydown = e => { if (e.key === 'Enter') $('#next-name').click(); };
}

function wardrobe() {
  const list = outfits.map((o, i) => '<button class="outfit-item ' + (i === profile.outfit ? 'selected' : '') + '" data-outfit="' + i + '"><span class="swatch" style="background:' + o[1] + '"></span><span><strong>' + o[0] + '</strong><small>' + o[2] + '</small></span></button>').join('');
  openModal('<span class="step-label">02 / DRESS FOR YOURSELF</span><h2>Find your reflection.</h2><div class="outfit-layout"><div class="avatar-preview" id="avatar-preview"><div class="model-views"><button data-view="full" class="active">Full body</button><button data-view="face">Face</button><button data-view="fabric">Garment</button></div><div class="model-zoom"><button id="zoom-in" aria-label="Zoom in">+</button><button id="zoom-out" aria-label="Zoom out">−</button></div><span class="avatar-caption">DRAG TO ROTATE ↔</span></div><div><p>AFTER DARK — six silhouettes, four variations.<br>Asymmetry, drape, volume and metal.</p><div class="outfit-list">' + list + '</div><div class="wardrobe-info" id="outfit-info">' + lookNotes[profile.outfit] + '</div><button class="primary full" id="next-outfit">FIND THE PARTY <span>→</span></button></div></div>', 'wardrobe');
  initPreview();
  $$('[data-outfit]').forEach(b => b.onclick = () => {
    profile.outfit = +b.dataset.outfit;
    save();
    $$('[data-outfit]').forEach(v => v.classList.toggle('selected', v === b));
    $('#outfit-info').textContent = lookNotes[profile.outfit];
    updatePreview();
  });
  $('#next-outfit').onclick = () => profile.name ? locations() : nameScreen();
}

function locations() {
  openModal('<span class="step-label">03 / FOLLOW THE FREQUENCY</span><h2>Where are we going?</h2><p>One door. A whole night ahead.</p><div class="location-card"><span class="chip">OPEN · LOCAL CLUB</span><span class="step-label">TECHNO / ACID HOUSE</span><h3>NEON ROOM</h3><p>Dancefloor · DJ stage · bar · lounge</p></div><div class="coming-soon"><div>ROOFTOP<small>NEXT CHAPTER</small></div><div>FOREST RAVE<small>NEXT CHAPTER</small></div><div>SPACE CLUB<small>NEXT CHAPTER</small></div></div><button class="primary full" id="enter-club">I AM IN <span>↗</span></button><p style="font-size:10px">An interactive 3D scene with stylized characters. Online and VR are coming in later versions.</p>', 'locations');
  $('#enter-club').onclick = () => profile.name ? controlScreen() : nameScreen();
}

function goHome() {
  npcInteraction.stop();
  unlock();
  playing = false;
  dancing = false;
  document.body.classList.remove('playing');
  $('#game-ui').hidden = true;
  closeModal();
  if (audio) audio.setSilent(true);
  window.scrollTo(0, 0);
}

$('#start').onclick = nameScreen;
$('#explore').onclick = locations;
$('#dance-card').onclick = nameScreen;
$$('[data-open]').forEach(b => b.onclick = () => b.dataset.open === 'wardrobe' ? wardrobe() : locations());
$('.brand').onclick = e => { e.preventDefault(); goHome(); };
$('[data-page="home"]').onclick = goHome;

function setControlMode(mode) {
  unlock();
  controlMode = mode;
  profile.controls = mode;
  save();
  document.body.classList.toggle('mobile-controls', mode === 'mobile');
  $('#game-help').textContent = mode === 'mobile'
    ? 'Left to move · right to look · you can do both at once'
    : 'Click the scene to look · WASD to move · E for actions · Esc for cursor';
}

function controlScreen() {
  openModal('<span class="step-label">04 / YOUR CONTROLS</span><h2>How will you play?</h2><p>Pick what feels right. You can always change it in settings.</p><div class="control-choices"><button data-control="mobile" class="control-choice ' + (controlMode === 'mobile' ? 'selected' : '') + '"><span class="control-icon">▣</span><strong>On a phone</strong><small>Left stick moves.<br>Right stick turns and looks.<br>All actions are on screen.</small></button><button data-control="desktop" class="control-choice ' + (controlMode === 'desktop' ? 'selected' : '') + '"><span class="control-icon">⌨</span><strong>Keyboard and mouse</strong><small>WASD moves.<br>Mouse turns and looks.<br>E opens the action menu.</small></button></div><button id="confirm-controls" class="primary full">ENTER THE CLUB <span>↗</span></button>', 'controls');
  $$('[data-control]').forEach(b => b.onclick = () => {
    setControlMode(b.dataset.control);
    $$('[data-control]').forEach(v => v.classList.toggle('selected', v === b));
  });
  $('#confirm-controls').onclick = () => { setControlMode(controlMode); enterClub(); };
}

function settings() {
  const trackOptions = tracks.map((t, i) => '<option value="' + i + '">' + t.label + '</option>').join('');
  openModal('<span class="step-label">MAKE IT YOURS</span><h2>Your frequency.</h2><div class="settings-row"><label for="control-mode">Controls</label><select id="control-mode"><option value="mobile">Phone · joysticks</option><option value="desktop">Keyboard and mouse</option></select></div><div class="settings-row"><label for="volume">Volume</label><input id="volume" type="range" min="0" max="100" value="' + profile.volume * 100 + '"></div><div class="settings-row"><label for="quality">Graphics</label><select id="quality"><option value="low">Efficient</option><option value="high">High</option></select></div><div class="settings-row"><label for="motion">Calm light</label><input id="motion" type="checkbox" ' + (calmMotion() ? 'checked' : '') + '></div><div class="settings-row"><label for="track">Track</label><select id="track">' + trackOptions + '</select></div><p>Original procedural loops. Music only starts when you tap.</p><button class="primary" id="audio-toggle">' + (audio && audio.enabled ? 'TURN OFF' : 'TURN ON') + ' MUSIC</button><p style="font-size:10px">Phone: move both sticks at once to walk and look around. Desktop: WASD + mouse.</p>', 'settings');
  $('#control-mode').value = controlMode;
  $('#control-mode').onchange = e => setControlMode(e.target.value);
  $('#quality').value = quality;
  $('#track').value = audio ? audio.track : pendingTrack;
  $('#volume').oninput = e => { profile.volume = +e.target.value / 100; save(); setVolume(); };
  $('#quality').onchange = e => {
    quality = e.target.value;
    if (app) app.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(devicePixelRatio, 1.5) : 1;
    if (previewApp) previewApp.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(devicePixelRatio, 1.5) : 1;
  };
  $('#motion').onchange = e => { motionOverride = e.target.checked; };
  $('#track').onchange = e => {
    // Do not create the audio graph here: the reference only starts music from
    // an explicit play action or when the club is entered.
    pendingTrack = +e.target.value;
    if (audio) audio.setTrack(pendingTrack);
    $('#track-label').textContent = tracks[pendingTrack].label;
    toast('New frequency selected');
  };
  $('#audio-toggle').onclick = () => {
    toggleAudio();
    $('#audio-toggle').textContent = (audio && audio.enabled ? 'TURN OFF' : 'TURN ON') + ' MUSIC';
  };
}

$('#settings').onclick = settings;

let motionOverride = null;
function calmMotion() { return motionOverride === null ? lowMotion : motionOverride; }

// ---------- wardrobe preview (separate PlayCanvas application) ----------
let previewApp = null, previewCamera = null, previewAvatar = null, previewCanvasEl = null;
let previewAngle = 0, previewDistance = 4.8, previewTargetY = 1.06;
let previewPointer = null;

function framePreview(view) {
  previewTargetY = view === 'face' ? 1.91 : view === 'fabric' ? 1.15 : 1.06;
  previewDistance = view === 'face' ? .72 : view === 'fabric' ? 2.25 : 4.8;
  $$('[data-view]').forEach(b => b.classList.toggle('active', b.dataset.view === view));
  positionPreview();
}

function positionPreview() {
  if (!previewCamera) return;
  previewCamera.setPosition(0, previewTargetY + .06, previewDistance);
  previewCamera.lookAt(0, previewTargetY, 0);
}

function disposeEntity(entity) {
  const materials = new Set();
  const walk = node => {
    if (node.render) node.render.meshInstances.forEach(mi => { if (mi.material) materials.add(mi.material); });
    node.children.forEach(walk);
  };
  walk(entity);
  entity.destroy();
  materials.forEach(material => material.destroy());
}

function updatePreview() {
  if (!previewApp) return;
  if (previewAvatar) { disposeEntity(previewAvatar); previewAvatar = null; }
  previewAvatar = createAvatar(profile.outfit, { detail: true });
  previewApp.root.addChild(previewAvatar);
}

function initPreview() {
  const host = $('#avatar-preview');
  if (!previewApp) {
    const canvas = document.createElement('canvas');
    previewCanvasEl = canvas;
    host.prepend(canvas);
    try {
      previewApp = new pc.Application(canvas, { graphicsDeviceOptions: { antialias: true, alpha: true } });
    } catch (e) {
      toast('3D preview is unavailable in this browser');
      return;
    }
    previewApp.setCanvasFillMode(pc.FILLMODE_NONE);
    previewApp.setCanvasResolution(pc.RESOLUTION_AUTO);
    previewApp.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(devicePixelRatio, 1.5) : 1;
    previewApp.scene.ambientLight = new pc.Color(.16, .13, .2);
    const cam = new pc.Entity('preview-camera');
    cam.addComponent('camera', { fov: 32, nearClip: .1, farClip: 30, clearColor: new pc.Color(0, 0, 0, 0) });
    cam.setPosition(0, 1.3, 4.5);
    cam.lookAt(0, 1, 0);
    previewApp.root.addChild(cam);
    previewCamera = cam;
    light(previewApp.root, { type: 'directional', color: '#e1edff', intensity: 2.1, rotation: [-40, 30, 0] });
    light(previewApp.root, { type: 'omni', color: '#b58de8', intensity: 1.6, range: 20, position: [-2, 2, -1] });
    light(previewApp.root, { type: 'omni', color: '#c6f7ff', intensity: 1.8, range: 20, position: [2, 3, 2] });
    cylinder(previewApp.root, standard({ color: '#382f4c', metal: .85, roughness: .18 }), [1.3, .08, 1.5], [0, 0, 0], null, 'base');
    torus(previewApp.root, neon('#d9ff64'), [1.31, 1.31, 1], [0, .05, 0], null, 'base-ring');
    previewApp.autoRender = false;
    previewApp.on('update', () => {
      if (step !== 'wardrobe') return;
      const el = $('#avatar-preview');
      if (!el) return;
      const w = el.clientWidth, h = el.clientHeight;
      const ratio = previewApp.graphicsDevice.maxPixelRatio;
      if (w && h && (canvas.width !== Math.round(w * ratio) || canvas.height !== Math.round(h * ratio))) {
        previewApp.resizeCanvas(w, h);
      }
      if (previewAvatar) {
        const spin = previewAngle + (calmMotion() ? 0 : Math.sin(performance.now() / 1000 * .35) * .15);
        previewAvatar.setLocalEulerAngles(0, spin * 180 / Math.PI, 0);
      }
      previewApp.render();
    });
    previewApp.start();
    canvas.addEventListener('wheel', e => {
      e.preventDefault();
      previewDistance = Math.max(.55, Math.min(5.5, previewDistance * (e.deltaY > 0 ? 1.08 : .92)));
      positionPreview();
    }, { passive: false });
    canvas.addEventListener('pointerdown', e => { previewPointer = e.clientX; canvas.setPointerCapture(e.pointerId); });
    canvas.addEventListener('pointermove', e => {
      if (previewPointer !== null) { previewAngle += (e.clientX - previewPointer) * .015; previewPointer = e.clientX; }
    });
    canvas.addEventListener('pointerup', () => previewPointer = null);
    canvas.addEventListener('pointercancel', () => previewPointer = null);
  } else if (previewCanvasEl && previewCanvasEl.parentNode !== host) {
    host.prepend(previewCanvasEl);
  }
  updatePreview();
  framePreview('full');
  $$('[data-view]').forEach(b => b.onclick = () => framePreview(b.dataset.view));
  $('#zoom-in').onclick = () => { previewDistance = Math.max(.55, previewDistance * .8); positionPreview(); };
  $('#zoom-out').onclick = () => { previewDistance = Math.min(5.5, previewDistance * 1.25); positionPreview(); };
}

// ---------- club world ----------
let app = null, camera = null, club = null, hands = null, worldStarted = false;
let worldCanvas = null, touch = null;
const _rotYaw = new pc.Quat(), _rotPitch = new pc.Quat();

function initWorld() {
  worldCanvas = document.createElement('canvas');
  $('#world').appendChild(worldCanvas);
  try {
    // preserveDrawingBuffer keeps the frame readable for screenshot automation
    // (index.html?shots=1); normal play keeps the faster default.
    const shotMode = /[?&]shots=1/.test(location.search);
    app = new pc.Application(worldCanvas, { graphicsDeviceOptions: { antialias: quality === 'high', alpha: false, preserveDrawingBuffer: shotMode } });
  } catch (e) {
    toast('Could not start WebGL. Try a browser with 3D support.');
    throw e;
  }
  app.setCanvasFillMode(pc.FILLMODE_FILL_WINDOW);
  app.setCanvasResolution(pc.RESOLUTION_AUTO);
  app.resizeCanvas(window.innerWidth, window.innerHeight);
  app.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(devicePixelRatio, 1.5) : 1;
  app.scene.fog.type = pc.FOG_EXP2;
  app.scene.fog.color = new pc.Color().fromString('#2a0d3c');
  app.scene.fog.density = .04;
  if ('toneMapping' in app.scene && pc.TONEMAP_ACES !== undefined) app.scene.toneMapping = pc.TONEMAP_ACES;
  if ('exposure' in app.scene) app.scene.exposure = 1.32;
  club = buildClub(app);
  camera = new pc.Entity('camera');
  camera.addComponent('camera', { fov: 70, nearClip: .05, farClip: 70, clearColor: new pc.Color().fromString('#10091c') });
  camera.setPosition(0, 1.66, 10);
  app.root.addChild(camera);
  hands = createFirstPersonHands(outfits[profile.outfit][1]);
  camera.addChild(hands);
  app.autoRender = false;
  app.on('update', onWorldUpdate);
  app.start();

  worldCanvas.addEventListener('click', () => {
    if (playing && $('#modal').hidden && controlMode === 'desktop' && worldCanvas.requestPointerLock) {
      const result = worldCanvas.requestPointerLock();
      if (result && result.catch) result.catch(() => {});
    }
  });
  worldCanvas.addEventListener('pointerdown', e => {
    if (controlMode === 'mobile' && $('#modal').hidden) {
      touch = { id: e.pointerId, x: e.clientX, y: e.clientY };
      worldCanvas.setPointerCapture(e.pointerId);
    }
  });
  worldCanvas.addEventListener('pointermove', e => {
    if (touch && touch.id === e.pointerId && controlMode === 'mobile' && $('#modal').hidden) {
      yaw -= (e.clientX - touch.x) * .005;
      pitch = Math.max(-1.1, Math.min(1.1, pitch - (e.clientY - touch.y) * .004));
      touch.x = e.clientX;
      touch.y = e.clientY;
    }
  });
  worldCanvas.addEventListener('pointerup', () => touch = null);
  worldCanvas.addEventListener('pointercancel', () => touch = null);
  worldStarted = true;
  started = true;
}

function setCameraRotation() {
  _rotYaw.setFromAxisAngle(pc.Vec3.UP, yaw);
  _rotPitch.setFromAxisAngle(pc.Vec3.RIGHT, pitch);
  _rotYaw.mul(_rotPitch);
  camera.setRotation(_rotYaw);
}

function animateHands(now, t, moving) {
  if (!hands) return;
  hands.setLocalPosition(0, moving && !calmMotion() ? Math.sin(t * 9) * .014 : 0, 0);
  const heart = hands.action === 'heart' && hands.until > now;
  hands.hands.forEach((h, i) => {
    const active = hands.until > now;
    const rx = active ? -.7 : dancing ? Math.sin(t * 6 + i) * .25 : 0;
    // Tilt the forearms towards each other so the heart gesture reads as two
    // hands meeting, not two parallel bars.
    const rz = heart ? (i ? -.62 : .62) : 0;
    h.setLocalEulerAngles(rx * 180 / Math.PI, 0, rz * 180 / Math.PI);
    if (heart) h.setLocalPosition(i ? .06 : -.06, h.baseY + .14, h.baseZ + .1);
    else h.setLocalPosition(h.baseX, h.baseY, h.baseZ);
  });
}

function animateNpcs(now, t) {
  if (!club) return;
  for (const n of club.npcs) {
    const base = n.getPosition();
    const dancingHere = n.dance && !calmMotion();
    n.setPosition(base.x, dancingHere ? Math.abs(Math.sin(t * 3 + n.phase)) * .07 : 0, base.z);
    const tilt = dancingHere ? Math.sin(t * 3 + n.phase) * .045 : 0;
    n.setLocalEulerAngles(0, n.baseYaw, tilt * 180 / Math.PI);
    n.arms.forEach((a, i) => {
      const rx = n.reactUntil > now ? -2 : n.dance ? -.15 + Math.sin(t * 3 + n.phase + i) * .22 : 0;
      const rz = n.dance ? Math.sin(t * 2 + n.phase + i) * .13 : 0;
      a.setLocalEulerAngles(rx * 180 / Math.PI, 0, rz * 180 / Math.PI);
    });
    n.legs.forEach((l, i) => {
      const rx = n.dance && !calmMotion() ? Math.sin(t * 3 + n.phase + i * Math.PI) * .055 : 0;
      l.setLocalEulerAngles(rx * 180 / Math.PI, 0, 0);
    });
  }
  if (club.crowd) {
    for (const c of club.crowd) {
      c.setLocalEulerAngles(0, c.baseYaw + (calmMotion() ? 0 : Math.sin(t * 2 + c.phase) * 6), 0);
    }
  }
}

function animateLasers(t) {
  if (!club) return;
  club.lasers.forEach((l, i) => {
    const rz = calmMotion() ? .15 : Math.sin(t * .3 + i) * .4;
    const rx = .4 + (calmMotion() ? 0 : Math.sin(t * .4 + i) * .3);
    l.setLocalEulerAngles(rx * 180 / Math.PI, 0, rz * 180 / Math.PI);
  });
}

function onWorldUpdate(dt) {
  if (!playing || !camera) return;
  const now = performance.now();
  const t = now / 1000;
  let f = (keys.has('KeyW') || keys.has('ArrowUp') ? 1 : 0) - (keys.has('KeyS') || keys.has('ArrowDown') ? 1 : 0);
  let s = (keys.has('KeyD') || keys.has('ArrowRight') ? 1 : 0) - (keys.has('KeyA') || keys.has('ArrowLeft') ? 1 : 0);
  if (controlMode === 'mobile') {
    f = -touchInput.y;
    s = touchInput.x;
    if ($('#modal').hidden) {
      yaw -= touchInput.lookX * dt * 2.1;
      pitch = Math.max(-1.1, Math.min(1.1, pitch - touchInput.lookY * dt * 1.5));
    }
  }
  if (!$('#modal').hidden) { f = 0; s = 0; }
  const moving = !!(f || s);
  if (moving) {
    dancing = false;
    $('#dance-state').hidden = true;
    const speed = dt * 3 / Math.max(1, Math.hypot(f, s));
    const dx = (-Math.sin(yaw) * f + Math.cos(yaw) * s) * speed;
    const dz = (-Math.cos(yaw) * f - Math.sin(yaw) * s) * speed;
    const p = camera.getPosition();
    let nx = p.x, nz = p.z;
    if (!isBlocked(p.x + dx, p.z)) nx = p.x + dx;
    if (!isBlocked(nx, p.z + dz)) nz = p.z + dz;
    camera.setPosition(nx, 1.66, nz);
  }
  setCameraRotation();
  animateHands(now, t, moving);
  animateNpcs(now, t);
  animateLasers(t);
  const pos = camera.getPosition();
  $('#zone').textContent = zoneLabels[zoneForPosition(pos.x, pos.z)];
  app.render();
}

function enterClub() {
  try {
    // #world is display:none until body.playing is set, so reveal it before the
    // canvas is created or the drawing buffer starts at 0x0 and stays black.
    document.body.classList.add('playing');
    if (!worldStarted) initWorld();
    playing = true;
    if (hands) {
      const color = outfits[profile.outfit][1];
      hands.braceletMaterials.forEach(material => { material.diffuse = new pc.Color().fromString(color); material.update(); });
    }
    closeModal();
    $('#game-ui').hidden = false;
    $('#action-wheel').hidden = true;
    camera.setPosition(0, 1.66, 10);
    yaw = 0;
    pitch = 0;
    window.scrollTo(0, 0);
    setControlMode(controlMode);
    if (!audio) toggleAudio();
    else { audio.setSilent(false); audio.resume(); setVolume(); }
    toast('Welcome, ' + profile.name + '. The night is yours.');
  } catch (e) {
    toast('Could not start WebGL. Try a browser with 3D support.');
    console.error(e);
  }
}

$('#backroom').onclick = wardrobe;
$('#actions-toggle').onclick = toggleWheel;

function toggleWheel() {
  unlock();
  $('#action-wheel').hidden = !$('#action-wheel').hidden;
}

function nearest() {
  let best = null, distance = Infinity;
  if (!camera || !club) return { npc: best, d: distance };
  const cp = camera.getPosition();
  for (const n of club.npcs) {
    const np = n.getPosition();
    const d = Math.sqrt((np.x - cp.x) ** 2 + (np.y - cp.y) ** 2 + (np.z - cp.z) ** 2);
    if (d < distance) { distance = d; best = n; }
  }
  return { npc: best, d: distance };
}

let effectTimer;
function effect(s) {
  $('#effect').textContent = s;
  $('#effect').classList.add('visible');
  clearTimeout(effectTimer);
  effectTimer = setTimeout(() => $('#effect').classList.remove('visible'), 1800);
}

function action(type) {
  $('#action-wheel').hidden = true;
  const near = nearest();
  if (type === 'dance') {
    dancing = !dancing;
    $('#dance-state').hidden = !dancing;
    toast(dancing ? 'Catch the rhythm. Moving will stop the dance.' : 'Dance stopped');
    return;
  }
  if (type === 'drink') {
    if (drink) { drink = false; $('#drink-view').hidden = true; toast('Drink put away'); return; }
    const cp = camera.getPosition();
    if (!canTakeDrink(cp.x, cp.z)) { toast('Go to the bar left of the dancefloor'); return; }
    drink = true;
    $('#drink-view').hidden = false;
    effect('✧');
    toast('Your Neon tonic. To this night!');
    return;
  }
  if (!['talk', 'heart', 'wave', 'five'].includes(type)) return;
  const reply = npcInteraction.interact(near, profile.volume);
  if (reply === 'out-of-range') { toast('Get closer to the character to talk'); return; }
  if (reply === 'busy') { toast('The character is still talking. Let them finish.'); return; }
  if (reply === 'unavailable') return;
  drink = false;
  $('#drink-view').hidden = true;
  near.npc.reactUntil = performance.now() + 2200;
  const cp = camera.getPosition();
  const np = near.npc.getPosition();
  near.npc.baseYaw = Math.atan2(cp.x - np.x, cp.z - np.z) * 180 / Math.PI;
  effect({ talk: '💬', heart: '♡', wave: '✋', five: '✦' }[type]);
  toast(near.npc.npcName + ({ talk: ' replies to you', heart: ' replies with a heart ♡', wave: ' waves back at you', five: ' high-fives you!' }[type]));
  if (hands) { hands.action = type; hands.until = performance.now() + 1800; }
}

$$('[data-action]').forEach(b => b.onclick = () => action(b.dataset.action));

$('#people').onclick = () => {
  const list = club ? club.npcs.map(n => '<div class="person"><span class="person-icon" style="background:' + outfits[n.lookIndex][1] + '">' + n.npcName[0] + '</span><div><b>' + n.npcName + '</b><small>NPC · ' + (n.dance ? 'On the dancefloor' : 'Chilling in the club') + '</small></div></div>').join('') : '';
  openModal('<span class="step-label">AROUND YOU</span><h2>On the same wavelength.</h2><p>The characters in this scene are NPCs. Walk up, pick “Talk” or send a gesture — they reply with voiced lines. Friends and chat arrive with online play.</p>' + list, 'people');
};

$('#map').onclick = () => {
  openModal('<span class="step-label">FIND YOUR SPACE</span><h2>Inside the night.</h2><p>Walk there or jump straight to a zone.</p><div class="map-grid"><button data-zone="0">◉ Dancefloor</button><button data-zone="1">♧ Bar</button><button data-zone="2">☾ Lounge</button><button data-zone="3">↗ Entrance</button></div>', 'map');
  $$('[data-zone]').forEach(b => b.onclick = () => {
    const target = mapTargets[+b.dataset.zone];
    if (camera) {
      camera.setPosition(target.x, target.y, target.z);
      yaw = target.yaw;
      pitch = 0;
    }
    closeModal();
  });
};

window.addEventListener('keydown', e => {
  if (!playing || !$('#modal').hidden || controlMode !== 'desktop') return;
  if (['KeyW', 'KeyA', 'KeyS', 'KeyD', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(e.code)) {
    e.preventDefault();
    keys.add(e.code);
  }
  if (e.code === 'KeyE') { e.preventDefault(); toggleWheel(); }
  if (e.code === 'Escape') $('#action-wheel').hidden = true;
});
window.addEventListener('keyup', e => keys.delete(e.code));
window.addEventListener('blur', resetControls);
document.addEventListener('visibilitychange', () => {
  resetControls();
  if (document.hidden) npcInteraction.stop();
  if (audio) {
    if (document.hidden) audio.suspend();
    else if (audio.enabled) audio.resume();
  }
});
document.addEventListener('mousemove', e => {
  if (playing && controlMode === 'desktop' && document.pointerLockElement && $('#modal').hidden) {
    yaw -= e.movementX * .002;
    pitch = Math.max(-1.1, Math.min(1.1, pitch - e.movementY * .002));
  }
});

sticks.push(createJoystick($('#move-stick'), (x, y) => { touchInput.x = x; touchInput.y = y; }));
sticks.push(createJoystick($('#look-stick'), (x, y) => { touchInput.lookX = x; touchInput.lookY = y; }));

window.addEventListener('resize', () => {
  if (app) app.resizeCanvas(window.innerWidth, window.innerHeight);
  resetControls();
});

// Expose a tiny debug surface for the smoke test.
window.__neonRoom = {
  get ready() { return started; },
  get playing() { return playing; },
  get npcCount() { return club ? club.npcs.length : 0; },
  get dancingCount() { return club ? club.npcs.filter(n => n.dance).length : 0; },
  get zone() { return camera ? zoneLabels[zoneForPosition(camera.getPosition().x, camera.getPosition().z)] : ''; },
  get stats() { return app && app.stats ? { fps: Math.round(app.stats.frame.fps * 10) / 10, ms: Math.round(app.stats.frame.ms * 100) / 100, drawCalls: app.stats.drawCalls.total, triangles: app.stats.frame.triangles } : null; },
  get engine() { return pc.version; },
  get audio() { return audio ? { enabled: audio.enabled, hasContext: !!audio.context, track: audio.track } : null; },
  get position() { const p = camera.getPosition(); return [Math.round(p.x * 100) / 100, Math.round(p.y * 100) / 100, Math.round(p.z * 100) / 100]; },
  get npcs() {
    return club ? club.npcs.map(n => {
      const p = n.getPosition();
      return { name: n.npcName, x: p.x, z: p.z, look: n.lookIndex, dance: !!n.dance };
    }) : [];
  },
  pose(x, z, yawDeg, pitchDeg) {
    if (!camera) return false;
    camera.setPosition(x, 1.66, z);
    yaw = (yawDeg || 0) * Math.PI / 180;
    pitch = (pitchDeg || 0) * Math.PI / 180;
    setCameraRotation();
    return true;
  },
  focus(index, distance) {
    if (!camera || !club || !club.npcs[index]) return false;
    const np = club.npcs[index].getPosition();
    const d = distance || 2.4;
    camera.setPosition(np.x, 1.62, np.z + d);
    yaw = Math.atan2(camera.getPosition().x - np.x, camera.getPosition().z - np.z);
    pitch = -.05;
    setCameraRotation();
    club.npcs[index].baseYaw = 0; // avatars face +z, the side the camera stands on
    return true;
  },
  act(type) { action(type); }
};
