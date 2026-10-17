// NEON ROOM club built from PlayCanvas primitives.

import { npcPositions, npcNames, looks, dancingNpcCount } from './logic.js';
import { standard, unlit, neon, box, sphere, cylinder, cone, torus, plane, group, light, glowTexture, textureFromCanvas, textSign } from './primitives.js';
import { createAvatar } from './avatar.js';

export function buildClub(app) {
  const scene = {
    npcs: [],
    lasers: [],
    glows: [],
    tables: [],
    speakers: [],
    materials: {}
  };

  const m = scene.materials;
  m.floor = standard({ color: '#1c1529', metal: 0.8, roughness: 0.19 });
  m.wall = standard({ color: '#15111e', metal: 0.35, roughness: 0.4 });
  m.chrome = standard({ color: '#a3a6b7', metal: 0.92, roughness: 0.16 });
  m.black = standard({ color: '#0b0810', metal: 0.2, roughness: 0.4 });
  m.dark = standard({ color: '#281d35', metal: 0.8, roughness: 0.24 });
  m.seat = standard({ color: '#502462', metal: 0.4, roughness: 0.4 });
  m.cup = standard({ color: '#e294ff', metal: 0.35, roughness: 0.15 });
  m.floorLine = neon('#38214c');
  m.pinkLine = neon('#f23bb9');
  m.violetLine = neon('#9e51ff');
  m.cyanLine = neon('#60e5ff');
  m.acid = neon('#d9ff64');
  m.danceEdge = neon('#bd65ff');
  m.orb = neon('#b36bff');
  m.laserPink = unlit({ color: '#b05bff', opacity: 0.11, blend: pc.BLEND_ADDITIVE, depthWrite: false, twoSided: true });
  m.laserCyan = unlit({ color: '#55d9ff', opacity: 0.11, blend: pc.BLEND_ADDITIVE, depthWrite: false, twoSided: true });
  m.glowMap = glowTexture(app);

  // Shell.
  box(app.root, m.floor, [26, 0.2, 28], [0, -0.15, 0], null, 'floor');
  box(app.root, m.wall, [26, 7, 0.25], [0, 3.3, -14], null, 'wall-n');
  box(app.root, m.wall, [0.25, 7, 28], [-13, 3.3, 0], null, 'wall-w');
  box(app.root, m.wall, [0.25, 7, 28], [13, 3.3, 0], null, 'wall-e');
  box(app.root, m.wall, [26, 7, 0.25], [0, 3.3, 14], null, 'wall-s');
  box(app.root, m.black, [26, 0.2, 28], [0, 7, 0], null, 'ceiling');

  // Floor grid and wall neon.
  for (let z = -12; z <= 12; z += 3) {
    box(app.root, m.floorLine, [25, 0.018, 0.018], [0, 0.005, z], null, 'grid-z');
    for (const x of [-12.5, 12.5]) {
      box(app.root, x < 0 ? m.violetLine : m.pinkLine, [0.04, 5, 0.08], [x, 2.6, z], null, 'wall-strip');
      scene.glows.push(glow(app, x < 0 ? '#8e40ff' : '#f22fae', x, 2.4, z, 3, 0.22));
    }
  }
  for (let x = -10; x <= 10; x += 2.5) box(app.root, m.floorLine, [0.015, 0.018, 28], [x, 0.005, 0], null, 'grid-x');

  // DJ stage backdrop.
  for (let j = 0; j < 4; j++) {
    torus(app.root, neon(j % 2 ? '#a95aff' : '#f663ff'), [6.2 + j * 0.6, 6.2 + j * 0.6, 1], [0, 4.3 + j * 0.05, -10.8 - j * 0.25], [90, 0, 0], 'dj-ring');
    scene.glows.push(glow(app, '#b044ff', 0, 4, -11, 9, 0.16));
  }
  textSign(app, app.root, 'NEON ROOM', 7, '#e3c4ff', [0, 4.8, -13.6], [0, 0, 0]);
  textSign(app, app.root, 'FIND YOUR FREQUENCY', 4, '#d9ff64', [0, 3.8, -13.5], [0, 0, 0]);
  box(app.root, m.black, [8, 0.5, 4], [0, 0.15, -11], null, 'stage');
  box(app.root, m.chrome, [4.5, 1.2, 1.2], [0, 1, -10.6], null, 'booth');
  box(app.root, m.acid, [4.6, 0.04, 1.25], [0, 1.63, -10.6], null, 'booth-trim');
  for (const x of [-4.4, 4.4]) {
    box(app.root, m.black, [1.1, 3.2, 1], [x, 1.65, -11], null, 'speaker');
    for (const y of [0.7, 1.8, 2.7]) {
      torus(app.root, m.dark, [0.66, 0.66, 0.46], [x, y, -10.48], [90, 0, 0], 'speaker-cone');
    }
  }

  // Ceiling truss.
  for (let x = -6; x <= 6; x += 3) {
    box(app.root, m.chrome, [0.12, 0.12, 25], [x, 6.2, 0], null, 'truss');
    for (let z = -8; z <= 8; z += 4) {
      sphere(app.root, m.orb, [0.2, 0.2, 0.2], [x, 6.1, z], null, 'truss-orb');
      scene.glows.push(glow(app, '#b655ff', x, 6, z, 1.5, 0.3));
    }
  }

  // Bar.
  box(app.root, m.dark, [3.2, 1.05, 8], [-10, 0.5, 1], null, 'bar');
  box(app.root, m.chrome, [3.3, 0.1, 8.1], [-10, 1.08, 1], null, 'bar-top');
  box(app.root, m.cyanLine, [0.04, 0.07, 8.1], [-8.33, 0.9, 1], null, 'bar-trim');
  textSign(app, app.root, 'BAR / RECHARGE', 3, '#69eeff', [-12.8, 3.2, 1], [0, 90, 0]);
  for (let z = -2; z <= 4; z += 1.2) {
    cylinder(app.root, m.cup, [0.18, 0.26, 0.18], [-8.7, 1.27, z], null, 'cup');
    scene.glows.push(glow(app, '#d54fff', -8.7, 1.3, z, 0.6, 0.5));
  }

  // Lounge.
  for (const z of [-1, 4, 9]) {
    box(app.root, m.seat, [2.6, 0.55, 2.4], [10, 0.3, z], null, 'sofa');
    box(app.root, m.seat, [0.45, 1, 2.4], [11.25, 0.7, z], null, 'sofa-back');
    box(app.root, neon('#9d54ff'), [2.7, 0.035, 2.5], [10, 0.03, z], null, 'sofa-trim');
    cylinder(app.root, m.chrome, [1.3, 0.07, 1.3], [7.8, 0.65, z], null, 'table');
    box(app.root, m.chrome, [0.09, 0.65, 0.09], [7.8, 0.3, z], null, 'table-leg');
    scene.tables.push({ x: 7.8, z });
  }
  textSign(app, app.root, 'LOUNGE / SLOW DOWN', 4, '#dfa3ff', [12.8, 3.3, 5], [0, -90, 0]);

  // Dance floor.
  box(app.root, standard({ color: '#30243f', metal: 0.8, roughness: 0.22 }), [11, 0.025, 10], [0, 0.01, -2], null, 'dancefloor');
  for (const x of [-5.5, 5.5]) box(app.root, m.danceEdge, [0.045, 0.03, 10], [x, 0.04, -2], null, 'dance-edge');
  for (const z of [-7, 3]) box(app.root, m.danceEdge, [11, 0.03, 0.045], [0, 0.04, z], null, 'dance-edge');

  // DJ.
  const dj = createAvatar(2, { detail: false });
  dj.setPosition(0, 0.4, -11.8);
  app.root.addChild(dj);
  dj.arms.forEach((a) => a.setLocalEulerAngles(-63, 0, 0));
  scene.dj = dj;

  // NPC crowd.
  npcPositions.forEach(([x, z], i) => {
    const npc = createAvatar(i % looks.length, { detail: false });
    npc.setPosition(x, 0, z);
    const npcYaw = ((i * 0.9) % 6.28) * 180 / Math.PI;
    npc.setLocalEulerAngles(0, npcYaw, 0);
    npc.baseYaw = npcYaw;
    npc.npcName = npcNames[i];
    npc.phase = i * 2.1;
    npc.dance = i < dancingNpcCount;
    npc.reactUntil = 0;
    npc.baseY = 0;
    app.root.addChild(npc);
    scene.npcs.push(npc);
  });

  // Laser cones.
  for (let i = 0; i < 8; i++) {
    const g = group(app.root, [((i % 4) - 1.5) * 2.3, 5.9, -9], 'laser');
    cone(g, i % 2 ? m.laserPink : m.laserCyan, [1.3, 15, 1.3], [0, -7.5, 0], null, 'laser-cone');
    g.laserPhase = i;
    scene.lasers.push(g);
  }

  // Sparkles.
  const sparkMat = unlit({ color: '#cbb0ff', opacity: 0.5, blend: pc.BLEND_ADDITIVE, depthWrite: false });
  for (let i = 0; i < 90; i++) {
    const x = (Math.random() - 0.5) * 25;
    const y = Math.random() * 6;
    const z = (Math.random() - 0.5) * 26;
    sphere(app.root, sparkMat, [0.05, 0.05, 0.05], [x, y, z], null, 'spark');
  }

  // Lighting.
  app.scene.ambientLight = new pc.Color(0.09, 0.06, 0.14);
  light(app.root, { type: 'directional', color: '#b094e3', intensity: 0.9, rotation: [-45, 30, 0] });
  light(app.root, { type: 'omni', color: '#a53dff', intensity: 1.1, range: 20, position: [0, 5, -5] });
  light(app.root, { type: 'omni', color: '#61cfff', intensity: 0.9, range: 20, position: [-8, 3, 1] });
  light(app.root, { type: 'omni', color: '#ed54c6', intensity: 0.9, range: 20, position: [7, 3, 5] });
  light(app.root, { type: 'omni', color: '#b875ff', intensity: 0.8, range: 20, position: [0, 3, 9] });

  return scene;
}

function glow(app, color, x, y, z, size = 3, opacity = 0.45) {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = 64;
  const ctx = canvas.getContext('2d');
  const gradient = ctx.createRadialGradient(32, 32, 0, 32, 32, 32);
  gradient.addColorStop(0, '#fff');
  gradient.addColorStop(0.15, '#ffffffaa');
  gradient.addColorStop(0.4, '#ffffff22');
  gradient.addColorStop(1, '#ffffff00');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, 64, 64);
  const tex = textureFromCanvas(app, canvas);
  const mat = new pc.StandardMaterial();
  mat.diffuseMap = tex;
  mat.emissiveMap = tex;
  mat.emissive = new pc.Color().fromString(color);
  mat.useLighting = false;
  mat.opacity = opacity;
  mat.blendType = pc.BLEND_ADDITIVE;
  mat.depthWrite = false;
  mat.cull = pc.CULLFACE_NONE;
  mat.update();
  return plane(app.root, mat, [size, 1, size], [x, y, z], [90, 0, 0], 'glow');
}