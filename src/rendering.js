import * as pc from 'playcanvas';

export { pc };
export const degrees = 180 / Math.PI;
export const rgb = hex => new pc.Color().fromString(hex);

export function material(color, metalness = .1, roughness = .4) {
  const m = new pc.StandardMaterial();
  m.diffuse = rgb(color);
  m.useMetalness = true;
  m.metalness = metalness;
  m.useMetalnessSpecularColor = true;
  m.gloss = 1 - roughness;
  m.cull = pc.CULLFACE_NONE;
  m.update();
  return m;
}

export function neon(color, strength = 2) {
  const m = new pc.StandardMaterial();
  m.diffuse.set(0, 0, 0);
  m.emissive = rgb(color);
  m.emissiveIntensity = strength;
  m.useLighting = false;
  m.cull = pc.CULLFACE_NONE;
  m.update();
  return m;
}

export function unlit(color = '#ffffff', opacity = 1, texture = null, additive = false) {
  const m = new pc.StandardMaterial();
  m.useLighting = false; m.useSkybox = false;
  m.diffuse.set(0, 0, 0); m.emissive = rgb(color);
  m.emissiveMap = texture; m.opacityMap = texture; m.opacityMapChannel = 'a';
  m.opacity = opacity; m.blendType = additive ? pc.BLEND_ADDITIVEALPHA : pc.BLEND_NORMAL;
  m.depthWrite = false; m.cull = pc.CULLFACE_NONE; m.update(); return m;
}

export function createContext(canvas, { alpha = false, quality = 'high', fog = false, graphicsDevice } = {}) {
  const app = new pc.Application(canvas, {
    graphicsDevice,
    graphicsDeviceOptions: { alpha, antialias: quality === 'high', powerPreference: 'high-performance', preserveDrawingBuffer: false },
  });
  app.autoRender = false;
  app.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(globalThis.devicePixelRatio || 1, 1.5) : 1;
  app.setCanvasResolution(pc.RESOLUTION_AUTO);
  app.setCanvasFillMode(pc.FILLMODE_NONE);
  app.scene.ambientLight = rgb(fog ? '#2b1e31' : '#35313b');
  app.scene.exposure = fog ? .9 : 1;
  app.scene.skyboxIntensity = fog ? .35 : .55;
  if (fog) {
    app.scene.fog.type = pc.FOG_EXP2;
    app.scene.fog.color = rgb('#180c29');
    app.scene.fog.density = .034;
  }
  app.scene.lighting.cells = new pc.Vec3(12, 6, 12);
  app.scene.lighting.maxLightsPerCell = 16;
  const camera = new pc.Entity('view-camera');
  camera.addComponent('camera', { fov: fog ? 70 : 32, nearClip: .05, farClip: fog ? 70 : 30, clearColor: alpha ? new pc.Color(0, 0, 0, 0) : rgb('#10091c') });
  camera.camera.toneMapping = pc.TONEMAP_ACES;
  app.root.addChild(camera);
  const frame = new pc.CameraFrame(app, camera.camera);
  frame.rendering.toneMapping = pc.TONEMAP_ACES;
  frame.bloom.intensity = alpha ? .035 : .085;
  frame.bloom.threshold = 1;
  frame.bloom.blurLevel = 6;
  frame.update();
  const context = { app, camera, frame, canvas, alpha, width: 0, height: 0, meshCache: new Map(), glows: [] };
  setEnvironment(context);
  app.start();
  return context;
}

function setEnvironment({ app }) {
  const faces = Array.from({ length: 6 }, (_, i) => {
    const c = document.createElement('canvas'); c.width = c.height = 64;
    const x = c.getContext('2d'), g = x.createLinearGradient(0, 0, 64, 64);
    for (const [at, color] of [[0, '#0e0924'], [.3, i % 2 ? '#ae4ad8' : '#31b2dc'], [.5, '#7b5078'], [.6, '#292339'], [1, '#100c17']]) g.addColorStop(at, color);
    x.fillStyle = g; x.fillRect(0, 0, 64, 64);
    x.fillStyle = '#f7e8ff'; x.fillRect(10, 0, 2, 64); x.fillRect(40, 0, 2, 64);
    return c;
  });
  const cube = new pc.Texture(app.graphicsDevice, { name: 'neon-reflections', cubemap: true, width: 64, height: 64, mipmaps: true, format: pc.PIXELFORMAT_RGBA8 });
  cube.setSource(faces);
  const source = pc.EnvLighting.generateLightingSource(cube, { size: 64 });
  app.scene.envAtlas = pc.EnvLighting.generateAtlas(source, { size: 256, numReflectionSamples: 64, numAmbientSamples: 64 });
  cube.destroy(); source.destroy();
}

export function setQuality(context, quality) {
  context.app.graphicsDevice.maxPixelRatio = quality === 'high' ? Math.min(globalThis.devicePixelRatio || 1, 1.5) : 1;
  context.frame.bloom.intensity = context.alpha ? .035 : quality === 'high' ? .085 : .05;
  context.app.resizeCanvas(context.width || 1, context.height || 1);
  context.frame.update();
}

export function render(context, width, height) {
  width = Math.max(1, Math.round(width)); height = Math.max(1, Math.round(height));
  if (context.width !== width || context.height !== height) {
    context.width = width; context.height = height;
    context.app.resizeCanvas(width, height);
  }
  for (const glow of context.glows) glow.lookAt(context.camera.getPosition());
  context.app.render();
}

function meshFor(context, key, create) {
  if (!context.meshCache.has(key)) context.meshCache.set(key, create());
  return context.meshCache.get(key);
}

export function mesh(context, parent, geometry, mat, name = '') {
  const entity = new pc.Entity(name);
  const instance = new pc.MeshInstance(geometry, mat);
  entity.addComponent('render', { meshInstances: [instance], castShadows: false, receiveShadows: true });
  parent.addChild(entity);
  return entity;
}

export function box(c, parent, w, h, d, x, y, z, mat) {
  const geometry = meshFor(c, 'box', () => pc.createBox(c.app.graphicsDevice));
  const e = mesh(c, parent, geometry, mat, 'club-box');
  e.setLocalScale(w, h, d); e.setLocalPosition(x, y, z); return e;
}
export function sphere(c, parent, r, x, y, z, mat) {
  const geometry = meshFor(c, 'sphere', () => pc.createSphere(c.app.graphicsDevice, { radius: 1, latitudeBands: 12, longitudeBands: 16 }));
  const e = mesh(c, parent, geometry, mat, 'light-orb'); e.setLocalScale(r, r, r); e.setLocalPosition(x, y, z); return e;
}
export function cylinder(c, parent, radius, height, x, y, z, mat, segments = 24) {
  const geometry = meshFor(c, `cylinder-${segments}`, () => pc.createCylinder(c.app.graphicsDevice, { radius: 1, height: 1, capSegments: segments, heightSegments: 1 }));
  const e = mesh(c, parent, geometry, mat); e.setLocalScale(radius, height, radius); e.setLocalPosition(x, y, z); return e;
}
export function torus(c, parent, radius, tube, x, y, z, mat, flat = false, segments = 64) {
  const key = `torus-${radius}-${tube}-${segments}`;
  const geometry = meshFor(c, key, () => pc.createTorus(c.app.graphicsDevice, { ringRadius: radius, tubeRadius: tube, segments, sides: 8 }));
  const e = mesh(c, parent, geometry, mat, 'neon-ring'); e.setLocalPosition(x, y, z); if (!flat) e.setLocalEulerAngles(90, 0, 0); return e;
}

export function light(c, parent, color, intensity, x, y, z, { directional = false, shadows = false } = {}) {
  const e = new pc.Entity('club-light');
  e.addComponent('light', { type: directional ? 'directional' : 'omni', color: rgb(color), intensity, range: 20, falloffMode: pc.LIGHTFALLOFF_INVERSESQUARED, castShadows: shadows, shadowDistance: 20, shadowResolution: 512 });
  e.setLocalPosition(x, y, z); if (directional) e.setLocalEulerAngles(42, -35, 0); parent.addChild(e); return e;
}

function canvasTexture(c, canvas) {
  const texture = new pc.Texture(c.app.graphicsDevice, { width: canvas.width, height: canvas.height, mipmaps: true, format: pc.PIXELFORMAT_SRGBA8 });
  texture.setSource(canvas); return texture;
}
function quad(c, parent, width, height, mat) {
  const geometry = meshFor(c, 'quad', () => pc.createPlane(c.app.graphicsDevice, { halfExtents: new pc.Vec2(.5, .5) }));
  const e = mesh(c, parent, geometry, mat);
  e.setLocalScale(width, 1, height); e.setLocalEulerAngles(90, 0, 0); return e;
}

export function text(c, parent, label, size, color = '#fff') {
  const canvas = document.createElement('canvas'); canvas.width = 1024; canvas.height = 160;
  const x = canvas.getContext('2d'); x.font = 'bold 100px Arial'; x.fillStyle = color; x.textAlign = 'center'; x.fillText(label, 512, 115, 960);
  const mat = unlit('#ffffff', 1, canvasTexture(c, canvas));
  return quad(c, parent, size, size / 6.4, mat);
}

export function glow(c, parent, color, x, y, z, size = 3, opacity = .45) {
  if (!c.glowMap) {
    const canvas = document.createElement('canvas'); canvas.width = canvas.height = 64;
    const x = canvas.getContext('2d'), g = x.createRadialGradient(32, 32, 0, 32, 32, 32);
    for (const [at, color] of [[0, '#fff'], [.15, '#ffffffaa'], [.4, '#ffffff22'], [1, '#ffffff00']]) g.addColorStop(at, color);
    x.fillStyle = g; x.fillRect(0, 0, 64, 64); c.glowMap = canvasTexture(c, canvas);
  }
  const mat = unlit(color, opacity, c.glowMap, true);
  // Billboard pivot keeps the plane's local XZ orientation and rectangular scale intact.
  const pivot = new pc.Entity('glow'); parent.addChild(pivot); pivot.setLocalPosition(x, y, z);
  quad(c, pivot, size, size, mat); c.glows.push(pivot); return pivot;
}

// Authored light streaks provide inexpensive floor sheen without an extra reflected scene pass.
export function floorReflection(c, parent, color, x, z, width = 1, length = 4) {
  if (!c.reflectionTexture) {
    const canvas = document.createElement('canvas');canvas.width=64;canvas.height=256;
    const ctx=canvas.getContext('2d');
    for(let y=0;y<256;y++){
      const spread=5+y*.045, drift=Math.sin(y*.43)*2+Math.sin(y*1.7)*1.2;
      const g=ctx.createLinearGradient(0,0,64,0),a=(1-y/256)**1.6;
      g.addColorStop(0,'#fff0');g.addColorStop(Math.max(.01,(32-spread+drift)/64),'#fff0');g.addColorStop(.5+drift/64,`rgba(255,255,255,${a*.6})`);g.addColorStop(Math.min(.99,(32+spread+drift)/64),'#fff0');g.addColorStop(1,'#fff0');
      ctx.fillStyle=g;ctx.fillRect(0,y,64,1);
    }
    c.reflectionTexture=canvasTexture(c,canvas);c.reflectionMats=new Map();
  }
  if(!c.reflectionMats.has(color))c.reflectionMats.set(color,unlit(color,.32,c.reflectionTexture,true));
  const geometry=meshFor(c,'reflection-plane',()=>pc.createPlane(c.app.graphicsDevice,{halfExtents:new pc.Vec2(.5,.5)}));
  const e=mesh(c,parent,geometry,c.reflectionMats.get(color),'floor-reflection');e.setLocalScale(width,1,length);e.setLocalPosition(x,.027,z);return e;
}
