import assert from 'node:assert/strict';
import test from 'node:test';
import { readFile } from 'node:fs/promises';
import * as pc from 'playcanvas';
import { GlbParser } from 'playcanvas/build/playcanvas/src/framework/parsers/glb-parser.js';
import { GlbContainerResource } from 'playcanvas/build/playcanvas/src/framework/parsers/glb-container-resource.js';
import { createContext, render, setQuality } from '../src/rendering.js';
import { ClubRenderer } from '../src/club-renderer.js';
import { AvatarLibrary, avatarUrl } from '../src/avatars.js';
import { blocked, MAP_ZONES, NPC_POSITIONS } from '../src/club-layout.js';

class Canvas {
  constructor() { this.id = 'native-test'; this.width = 800; this.height = 600; this.clientWidth = 800; this.clientHeight = 600; this.style = {}; }
  addEventListener() {} removeEventListener() {}
  getContext() { const gradient = { addColorStop() {} }; return { createLinearGradient: () => gradient, createRadialGradient: () => gradient, fillRect() {}, fillText() {} }; }
  getBoundingClientRect() { return { width: this.width, height: this.height }; }
}
function nativeApp() {
  globalThis.HTMLCanvasElement = Canvas;
  globalThis.document = { createElement: () => new Canvas(), addEventListener() {}, removeEventListener() {}, hidden: false };
  globalThis.window = { innerWidth: 800, innerHeight: 600, devicePixelRatio: 1, addEventListener() {}, removeEventListener() {} };
  const app = new pc.Application(new Canvas(), { graphicsDevice: new pc.NullGraphicsDevice(new Canvas()) });
  const camera = new pc.Entity('camera'); camera.addComponent('camera'); app.root.addChild(camera);
  return { app, camera, meshCache: new Map(), glows: [] };
}
async function parseAsset(file, context) {
  const bytes = await readFile(new URL('../src/assets/avatars/' + file, import.meta.url));
  const asset = new pc.Asset(file, 'container', { url: file });context.app.assets.add(asset);
  const data = await new Promise((resolve, reject) => GlbParser.parse(file, '', bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength), context.app.graphicsDevice, context.app.assets, {}, [], (err, data) => err ? reject(err) : resolve(data)));
  asset.resource = new GlbContainerResource(data, asset, context.app.assets, GlbParser.createDefaultMaterial());
  return asset;
}

test('PlayCanvas parses every shipped GLB with articulated limbs and intact material surfaces', async () => {
  const c = nativeApp();
  try {
    const manifest = JSON.parse(await readFile(new URL('../src/assets/avatars/manifest.json', import.meta.url)));
    assert.equal(manifest.models.length, 20);
    let bytes = 0;
    for (const model of manifest.models) {
      const asset = await parseAsset(model.file, c); bytes += model.bytes;
      const entity = asset.resource.instantiateRenderEntity();c.app.root.addChild(entity);
      for (const name of ['arm-0', 'arm-1', 'leg-0', 'leg-1']) assert.ok(entity.findByName(name), `${model.file}: ${name}`);
      const materials = asset.resource.materials.map(x => x.resource);
      assert.ok(materials.some(x => ['cloth','leather','inner'].includes(x.name)));
      assert.ok(materials.some(x => x.name === 'skin'));
      assert.ok(entity.findComponents('render').length > 5, 'Head, body, hands and limbs stay separated');
      const meshes = entity.findComponents('render').flatMap(x => x.meshInstances);
      assert.ok(meshes.every(x => x.mesh.vertexBuffer.numVertices > 0));
      const y = entity.findByName('arm-0').getLocalPosition().y;
      assert.ok(y > 1.5 && y < 1.7, `Arm pivot remains at the shoulder, got ${y}`);
      entity.findByName('arm-0').setLocalEulerAngles(-110, 0, 0);
      entity.destroy();
    }
    assert.ok(bytes < 12 * 1048576, 'All 20 models remain below 12 MiB total');
    const handAsset = await parseAsset('hands.glb', c);
    const hands = handAsset.resource.instantiateRenderEntity();
    assert.ok(hands.findByName('hand-0'));assert.ok(hands.findByName('hand-1'));
    assert.ok(handAsset.resource.materials.some(x => x.resource.name === 'bracelet'));
    hands.destroy();
    const heart = (await parseAsset('hands-heart.glb',c)).resource.instantiateRenderEntity();
    assert.ok(heart.findByName('hand-0'));assert.ok(heart.findByName('hand-1'));heart.destroy();
  } finally { c.app.destroy(); }
});

test('native club scene constructs, batches, and advances gesture and dance poses', () => {
  const c = nativeApp();
  try {
    const world = Object.create(ClubRenderer.prototype);
    Object.assign(world, { context: c, lasers: [], position: new pc.Vec3(0, 1.66, 10), player: new pc.Entity('player'), hands: { userData: { action: 'heart', until: 2000 } } });
    c.app.root.addChild(world.player);c.camera.reparent(world.player);
    world.buildClub();
    assert.equal(c.app.root.findComponents('light').length, 5);
    assert.equal(world.lasers.length, 8);assert.ok(c.glows.length > 30);
    const entity = new pc.Entity('Luna'), arms = [new pc.Entity(), new pc.Entity()], legs = [new pc.Entity(), new pc.Entity()];
    arms.concat(legs).forEach(x => entity.addChild(x));c.app.root.addChild(entity);
    world.npcs = [{ entity, position: new pc.Vec3(-3,0,-3), yaw: .5, userData: { dance: true, phase: 0, reactUntil: 2000, arms, legs } }];
    const handRoot = new pc.Entity('hands'), parts = [new pc.Entity(),new pc.Entity()];parts.forEach(x=>handRoot.addChild(x));c.camera.addChild(handRoot);
    world.handModel={entity:handRoot,parts};
    // Execute actual native frame submission against PlayCanvas's no-GPU device.
    world.draw(800,600,{yaw:.5,pitch:.2,time:1,now:1000,moving:true,dancing:false,lowMotion:false});
    assert.ok(Math.abs(parts[0].getLocalPosition().x + .12) < .001);
    assert.ok(Math.abs(arms[0].getLocalEulerAngles().x + 2 * 180 / Math.PI) < .01);
    assert.ok(entity.getLocalPosition().y > 0);
    world.draw(390,844,{yaw:0,pitch:0,time:2,now:3000,moving:false,dancing:false,lowMotion:true});
    assert.equal(entity.getLocalPosition().y, 0);
    assert.equal(legs[0].getLocalEulerAngles().x, 0);
    assert.ok(Math.abs(parts[0].getLocalPosition().x + .31) < .001);
  } finally { c.app.destroy(); }
});

test('same club collision boundaries keep map destinations reachable', () => {
  for (const [x,z] of MAP_ZONES) assert.equal(blocked(x,z),false);
  for (const [x,z] of NPC_POSITIONS) assert.ok(Array.from({length:8},(_,i)=>i*Math.PI/4).some(a=>!blocked(x+Math.cos(a)*2,z+Math.sin(a)*2)), 'Every NPC has an approach within interaction range');
  for (const [x,z] of [[13,0],[-13,0],[0,14],[0,-14],[-10,1],[0,-11],[4.7,-11],[10,4],[7.8,9]]) assert.equal(blocked(x,z),true);
  assert.equal(blocked(0,0),false);
});

test('asset URLs resolve beneath both root hosting and GitHub Pages subpath', () => {
  for(const base of ['https://example.test/app.js','https://example.test/neon-room-afterhours/app.js']) {
    const url=avatarUrl(9,true,base);
    assert.equal(url,new URL('./assets/avatars/look-9-detail.glb',base).href);
  }
  assert.throws(()=>avatarUrl(10,true),RangeError);
});

test('native render context initializes reflections, bloom and quality changes', () => {
  const old = nativeApp();old.app.destroy();
  globalThis.requestAnimationFrame = () => 0;globalThis.cancelAnimationFrame = () => {};
  const canvas = new Canvas();
  const c = createContext(canvas,{fog:true,graphicsDevice:new pc.NullGraphicsDevice(canvas)});
  try {
    assert.equal(c.app.scene.fog.type,pc.FOG_EXP2);
    assert.ok(c.app.scene.envAtlas);
    render(c,390,844);setQuality(c,'low');render(c,844,390);
    assert.equal(c.app.graphicsDevice.maxPixelRatio,1);
  } finally { c.app.destroy(); }
});

test('animated guest batching reduces draw groups while retaining articulated nodes', async () => {
  const c=nativeApp();
  try{
    const asset=await parseAsset('look-4-crowd.glb',c);
    const entities=[asset.resource.instantiateRenderEntity(),asset.resource.instantiateRenderEntity()];
    entities.forEach((e,i)=>{e.setLocalPosition(i*2,0,0);c.app.root.addChild(e)});
    const before=entities.flatMap(e=>e.findComponents('render')).reduce((n,r)=>n+r.meshInstances.length,0);
    const world=Object.create(ClubRenderer.prototype);world.context=c;world.npcs=entities.map(entity=>({entity}));world.batchPeople();
    const group=c.app.batcher.getGroupByName('moving-guests');const batches=c.app.batcher.getBatches(group.id);
    assert.ok(batches.length>0&&batches.length<before,`${before} draws reduced to ${batches.length} groups`);
    entities[0].findByName('arm-0').setLocalEulerAngles(-100,0,0);
    c.app.root.syncHierarchy();c.app.batcher.updateAll();
    assert.ok(entities[0].findByName('leg-1'));
  }finally{c.app.destroy()}
});
