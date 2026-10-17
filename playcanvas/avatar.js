// PlayCanvas fashion avatar: ten looks rebuilt from primitives.
// Silhouette families and the acid/violet/ash palette follow src/fashion.js.

import { looks, lookStyles } from './logic.js';
import { hex, standard, neon, box, sphere, cylinder, cone, capsule, torus, group } from './primitives.js';

function outfitMaterials(index, style, female, detail) {
  return {
    skin: standard({ color: female ? '#bf968c' : '#c3a294', roughness: 0.62, metal: 0.04 }),
    hair: standard({ color: index === 0 ? '#e6e0ef' : style === 2 ? '#b4adb3' : style === 5 ? '#817b83' : '#16131c', roughness: 0.53, metal: 0.08 }),
    cloth: standard({ color: looks[index][1], roughness: style === 0 ? 0.35 : 0.77, metal: style === 0 ? 0.28 : 0.04 }),
    inner: standard({ color: style === 4 ? '#302033' : '#18171f', roughness: 0.78, metal: 0.04 }),
    leather: standard({ color: '#242029', roughness: 0.3, metal: 0.24 }),
    rubber: standard({ color: '#16151c', roughness: 0.7, metal: 0.05 }),
    metal: standard({ color: '#a0a3ad', roughness: 0.25, metal: 0.82 }),
    accent: neon(style === 3 ? (index === 8 ? '#b98be1' : '#b4c75c') : '#d9ff64', 0.9),
    trim: neon(index === 8 ? '#b98be1' : index % 2 ? '#61cfff' : '#f368ff', 0.85)
  };
}

export function createAvatar(index = 0, { detail = true } = {}) {
  index = ((index % looks.length) + looks.length) % looks.length;
  const style = lookStyles[index];
  const female = [0, 3, 4].includes(style);
  const m = outfitMaterials(index, style, female, detail);
  const root = new pc.Entity('avatar-' + index);

  const widePants = style === 1;
  const platform = style === 0 || style === 2;
  const tallCollar = style === 2 || style === 5;
  const hood = style === 3;
  const longCoat = style === 5;
  const skirt = style === 0 || style === 4;

  // Hips and torso.
  capsule(root, m.cloth, [0.36, 0.3, 0.3], [0, 1.33, 0], null, 'torso');
  cylinder(root, m.inner, [0.31, 0.16, 0.27], [0, 1.07, 0], null, 'hips');

  // Legs pivot at the hips so dancing can rotate them.
  const legs = [];
  for (const sign of [-1, 1]) {
    const leg = group(root, [sign * 0.095, 1.06, 0], 'leg');
    const thigh = widePants ? 0.21 : 0.16;
    capsule(leg, m.cloth, [thigh, 0.5, thigh], [0, -0.5, 0], null, 'leg-limb');
    if (platform) cylinder(leg, m.rubber, [0.24, 0.09, 0.3], [0, -0.99, 0.02], null, 'platform');
    box(leg, m.rubber, [0.19, 0.09, 0.3], [0, -0.95, 0.03], null, 'boot');
    box(leg, m.trim, [0.19, 0.012, 0.012], [0, -1.0, 0.18], null, 'boot-trim');
    legs.push(leg);
  }

  // Skirt and long coat.
  if (skirt) {
    cylinder(root, m.cloth, [0.44, 0.4, 0.4], [0, 0.94, 0], null, 'skirt');
    cylinder(root, m.inner, [0.36, 0.34, 0.34], [0, 0.8, 0], null, 'skirt-layer');
  }
  if (longCoat) {
    box(root, m.leather, [0.42, 0.72, 0.28], [0, 0.95, -0.02], null, 'coat');
    box(root, m.inner, [0.3, 0.68, 0.2], [0, 0.92, 0.07], null, 'coat-front');
  }

  // Shoulders and arms.
  const arms = [];
  for (const sign of [-1, 1]) {
    const shoulder = group(root, [sign * 0.23, 1.57, 0], 'arm');
    sphere(shoulder, m.cloth, [0.2, 0.16, 0.19], [0, 0, 0], null, 'shoulder');
    capsule(shoulder, m.cloth, [0.16, 0.31, 0.16], [0, -0.32, 0], null, 'sleeve');
    sphere(shoulder, m.skin, [0.11, 0.12, 0.1], [0, -0.64, 0], null, 'hand');
    if (style === 0 || style === 4) torus(shoulder, m.metal, [0.14, 1, 0.14], [0, -0.62, 0], [90, 0, 0], 'bracelet');
    if (style === 2) torus(shoulder, m.accent, [0.12, 1, 0.12], [0, -0.58, 0], [90, 0, 0], 'ring');
    arms.push(shoulder);
  }

  // Collar, hood and chains.
  if (tallCollar) cylinder(root, style === 5 ? m.metal : m.cloth, [0.3, 0.24, 0.3], [0, 1.66, 0], null, 'collar');
  if (hood) sphere(root, m.cloth, [0.3, 0.28, 0.32], [0, 1.86, -0.05], null, 'hood');
  if (style === 5) {
    for (let i = 0; i < 3; i++) cylinder(root, m.metal, [0.012, 0.3, 0.012], [0.1 - i * 0.02, 1.45, 0.16], [12, 0, 0], 'chain');
  }

  // Neck and head.
  cylinder(root, m.skin, [0.1, 0.16, 0.1], [0, 1.69, 0], null, 'neck');
  sphere(root, m.skin, [0.22, 0.24, 0.23], [0, 1.92, 0], null, 'head');
  sphere(root, m.hair, [0.235, 0.2, 0.24], [0, 1.99, -0.01], null, 'scalp');
  if (female) {
    box(root, m.hair, [0.26, 0.34, 0.12], [0, 1.75, -0.11], null, 'long-hair');
    sphere(root, m.hair, [0.12, female && style !== 3 ? 0.4 : 0.24, 0.12], [-0.12, 1.78, -0.05], null, 'lock');
    sphere(root, m.hair, [0.12, female && style !== 3 ? 0.4 : 0.24, 0.12], [0.12, 1.78, -0.05], null, 'lock');
  } else {
    box(root, m.hair, [0.18, 0.12, 0.2], [0, 1.82, -0.08], null, 'crop');
  }

  if (detail) {
    for (const sign of [-1, 1]) sphere(root, m.inner, [0.026, 0.03, 0.02], [sign * 0.055, 1.93, 0.1], null, 'eye');
  }
  // Night-club shades on the leather silhouettes, as in the mood reference.
  if (style === 0 || style === 4) box(root, m.inner, [0.22, 0.06, 0.05], [0, 1.95, 0.1], null, 'shades');

  // Recycled fabric accent strip per look.
  box(root, m.trim, [0.34, 0.014, 0.014], [0, 1.52, 0.16], null, 'trim');
  if (style === 3) {
    box(root, m.rubber, [0.12, 0.12, 0.08], [0.16, 1.05, 0.14], null, 'cargo');
    box(root, m.rubber, [0.12, 0.12, 0.08], [-0.16, 1.05, 0.14], null, 'cargo');
    box(root, m.accent, [0.04, 0.3, 0.04], [0.2, 1.3, 0.05], null, 'cord');
  }

  root.arms = arms;
  root.legs = legs;
  root.lookIndex = index;
  root.detail = detail;
  return root;
}

export function createFirstPersonHands(color) {
  const root = new pc.Entity('hands');
  const skin = standard({ color: '#c3a294', roughness: 0.62, metal: 0.04 });
  const cuff = standard({ color: '#242029', roughness: 0.3, metal: 0.24 });
  const bracelet = neon(color || '#d9ff64', 0.9);
  const hands = [];
  for (const sign of [-1, 1]) {
    const hand = group(root, [sign * 0.31, -0.3, -0.55], 'hand-group');
    hand.baseX = sign * 0.31;
    hand.baseY = -0.3;
    hand.baseZ = -0.55;
    capsule(hand, skin, [0.09, 0.16, 0.09], [0, -0.08, 0], null, 'forearm');
    sphere(hand, skin, [0.12, 0.1, 0.14], [0, -0.02, 0.02], null, 'palm');
    cylinder(hand, cuff, [0.1, 0.07, 0.1], [0, -0.18, 0], null, 'cuff');
    torus(hand, bracelet, [0.12, 1, 0.12], [0, -0.2, 0], [90, 0, 0], 'bracelet');
    hands.push(hand);
  }
  root.hands = hands;
  root.braceletMaterials = [bracelet];
  root.action = null;
  root.until = 0;
  return root;
}