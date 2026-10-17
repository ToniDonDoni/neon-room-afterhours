// Small helpers over the PlayCanvas scene graph shared by scene and avatar builders.

export function hex(value) {
  return new pc.Color().fromString(value);
}

export function unlit({ color = '#ffffff', opacity = 1, blend = pc.BLEND_NORMAL, depthWrite = true, twoSided = false } = {}) {
  const m = new pc.StandardMaterial();
  m.diffuse = hex(color);
  m.useLighting = false;
  m.useMetalness = false;
  m.opacity = opacity;
  m.blendType = blend;
  m.depthWrite = depthWrite;
  m.cull = twoSided ? pc.CULLFACE_NONE : pc.CULLFACE_BACK;
  m.update();
  return m;
}

export function standard({
  color = '#ffffff',
  metal = 0.1,
  roughness = 0.4,
  emissive = '#000000',
  emissiveIntensity = 1,
  opacity = 1,
  blend = pc.BLEND_NORMAL,
  depthWrite = true,
  twoSided = false
} = {}) {
  const m = new pc.StandardMaterial();
  m.diffuse = hex(color);
  m.useMetalness = true;
  m.metalness = metal;
  m.gloss = Math.max(0, Math.min(1, 1 - roughness));
  m.emissive = hex(emissive);
  m.emissiveIntensity = emissiveIntensity;
  m.opacity = opacity;
  m.blendType = blend;
  m.depthWrite = depthWrite;
  m.cull = twoSided ? pc.CULLFACE_NONE : pc.CULLFACE_BACK;
  m.update();
  return m;
}

export function neon(color, opacity = 1) {
  return unlit({ color, opacity, blend: opacity < 1 ? pc.BLEND_ADDITIVE : pc.BLEND_NORMAL, depthWrite: opacity >= 1 });
}

function place(entity, scale, position, rotation) {
  if (scale) entity.setLocalScale(scale[0], scale[1], scale[2]);
  if (position) entity.setLocalPosition(position[0], position[1], position[2]);
  if (rotation) entity.setLocalEulerAngles(rotation[0], rotation[1], rotation[2]);
  return entity;
}

export function primitive(parent, type, material, scale, position, rotation, name) {
  const e = new pc.Entity(name || type);
  e.addComponent('render', { type, material, castShadows: false, receiveShadows: false });
  place(e, scale, position, rotation);
  parent.addChild(e);
  return e;
}

export const box = (p, m, s, pos, rot, name) => primitive(p, 'box', m, s, pos, rot, name);
export const sphere = (p, m, s, pos, rot, name) => primitive(p, 'sphere', m, s, pos, rot, name);
export const cylinder = (p, m, s, pos, rot, name) => primitive(p, 'cylinder', m, s, pos, rot, name);
export const cone = (p, m, s, pos, rot, name) => primitive(p, 'cone', m, s, pos, rot, name);
export const capsule = (p, m, s, pos, rot, name) => primitive(p, 'capsule', m, s, pos, rot, name);
export const torus = (p, m, s, pos, rot, name) => primitive(p, 'torus', m, s, pos, rot, name);
export const plane = (p, m, s, pos, rot, name) => primitive(p, 'plane', m, s, pos, rot, name);

export function group(parent, position, name) {
  const e = new pc.Entity(name || 'group');
  if (position) e.setLocalPosition(position[0], position[1], position[2]);
  parent.addChild(e);
  return e;
}

export function light(parent, { type = 'omni', color = '#ffffff', intensity = 1, range = 10, position = [0, 0, 0], rotation } = {}) {
  const e = new pc.Entity('light');
  e.addComponent('light', { type, color: hex(color), intensity, range, castShadows: false });
  if (position) e.setLocalPosition(position[0], position[1], position[2]);
  if (rotation) e.setLocalEulerAngles(rotation[0], rotation[1], rotation[2]);
  parent.addChild(e);
  return e;
}

export function textureFromCanvas(app, canvas) {
  const tex = new pc.Texture(app.graphicsDevice, { mipmaps: false });
  tex.setSource(canvas);
  return tex;
}

export function glowTexture(app) {
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
  return textureFromCanvas(app, canvas);
}

export function signMaterial(app, text, color = '#ffffff', width = 1024, font = 'bold 100px Arial') {
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = Math.round(width / 6.4);
  const ctx = canvas.getContext('2d');
  ctx.font = font;
  ctx.fillStyle = color;
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(text, canvas.width / 2, canvas.height / 2 + 4);
  const tex = textureFromCanvas(app, canvas);
  const m = new pc.StandardMaterial();
  m.diffuse = new pc.Color(1, 1, 1);
  m.diffuseMap = tex;
  m.emissiveMap = tex;
  m.emissive = new pc.Color(1, 1, 1);
  m.emissiveIntensity = 1;
  m.useLighting = false;
  m.useMetalness = false;
  m.opacity = 1;
  m.blendType = pc.BLEND_NORMAL;
  m.depthWrite = false;
  m.update();
  return m;
}

export function textSign(app, parent, text, size, color, position, rotation) {
  // A thin box keeps the canvas text upright on any wall without Euler-order surprises.
  return box(parent, signMaterial(app, text, color), [size, size / 6.4, 0.02], position, rotation, 'sign');
}