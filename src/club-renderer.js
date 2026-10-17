import { pc, createContext, material, neon, box, sphere, cylinder, torus, light, text, glow, mesh, render, setQuality, rgb, degrees, unlit, floorReflection } from './rendering.js';
import { AvatarLibrary } from './avatars.js';
import { NPC_NAMES, NPC_POSITIONS, EXTRA_GUESTS, EXTRA_NAMES } from './club-layout.js';

// Club dimensions, furniture, outfits and choreography are carried over from main.
export class ClubRenderer {
  constructor(quality = 'high') {
    this.canvas = document.createElement('canvas');
    this.context = createContext(this.canvas, { fog: true, quality });
    this.library = new AvatarLibrary(this.context);
    this.position = new pc.Vec3(0, 1.66, 10);
    this.hands = { userData: {} }; this.lasers = [];
    this.player = new pc.Entity('player');
    this.context.app.root.addChild(this.player);
    this.context.camera.reparent(this.player);
    this.buildClub();
    this.npcs = [...NPC_POSITIONS, ...EXTRA_GUESTS].map(([x, z], i) => ({
      position: new pc.Vec3(x, 0, z), yaw: (i * .9) % 6.28,
      userData: { index: i % 10, name: [...NPC_NAMES,...EXTRA_NAMES][i], phase: i * 2.1, dance: i < 7 || i >= 12 },
      lookAt(tx, _y, tz) { this.yaw = Math.atan2(tx - this.position.x, tz - this.position.z); },
    }));
    // Sequential unique assets keep mobile decode/HTTP pressure bounded.
    this.ready = this.loadPeople();
  }
  setQuality(quality) { setQuality(this.context, quality); }
  async loadPeople() {
    const dj = await this.library.avatar(2, false);
    dj.entity.setLocalPosition(0, .4, -11.8);
    dj.arms.forEach(arm => arm?.setLocalEulerAngles(-1.1 * degrees, 0, 0));
    this.context.app.root.addChild(dj.entity);
    for (const n of this.npcs) {
      const model = await this.library.avatar(n.userData.index, false);
      n.entity = model.entity; n.userData.arms = model.arms; n.userData.legs = model.legs;
      n.entity.setLocalPosition(n.position); n.entity.setLocalEulerAngles(0, n.yaw * degrees, 0);
      this.context.app.root.addChild(n.entity);
    }
    this.batchPeople();
    this.handModel = await this.library.hands(this.handColor || '#a0a3ad');
    this.context.camera.addChild(this.handModel.entity);
    this.heartModel = await this.library.hands(this.handColor || '#a0a3ad', true);
    this.context.camera.addChild(this.heartModel.entity);this.heartModel.entity.enabled = false;
  }
  batchPeople() {
    const app = this.context.app;
    const group = app.batcher.addGroup('moving-guests', true, 32);
    for(const n of this.npcs) for(const part of n.entity.findComponents('render')) part.batchGroupId=group.id;
    app.root.syncHierarchy();app.batcher.generate([group.id]);
  }
  setHandColor(color) {
    this.handColor = color;
    [...(this.handModel?.materials || []), ...(this.heartModel?.materials || [])].forEach(m => { m.diffuse = rgb(color); m.update(); });
  }
  buildClub() {
    const c = this.context, root = c.app.root;
    const mats = new Map(), neons = new Map();
    const m = (color, metal = .1, rough = .4) => {
      const key = `${color}/${metal}/${rough}`;
      if (!mats.has(key)) mats.set(key, material(color, metal, rough));
      return mats.get(key);
    };
    const n = color => { if (!neons.has(color)) neons.set(color, neon(color)); return neons.get(color); };
    const b = (...args) => box(c, root, ...args);
    const floor = m('#110e18', .78, .12), wall = m('#15111e', .35, .4), chrome = m('#a3a6b7', .92, .16), black = m('#0b0810', .2, .4);
    b(26, .2, 28, 0, -.15, 0, floor);
    b(26, 7, .25, 0, 3.3, -14, wall); b(.25, 7, 28, -13, 3.3, 0, wall);
    b(.25, 7, 28, 13, 3.3, 0, wall); b(26, 7, .25, 0, 3.3, 14, wall); b(26, .2, 28, 0, 7, 0, black);
    for (let z = -12; z <= 12; z += 3) {
      b(25, .018, .018, 0, .005, z, m('#22182b', .45, .4));
      for (const x of [-12.5, 12.5]) {
        b(.04, 5, .08, x, 2.6, z, n(x < 0 ? '#20ccff' : '#ff29ac'));
        glow(c, root, x < 0 ? '#20bfff' : '#ff29ac', x, 2.4, z, 3, .22);
      }
    }
    for (let x = -10; x <= 10; x += 2.5) b(.015, .018, 28, x, .005, 0, m('#22182b', .45, .4));
    for (let j = 0; j < 4; j++) {
      torus(c, root, 3.1 + j * .3, .045, 0, 4.3 + j * .05, -10.8 - j * .25, n(j % 2 ? '#a95aff' : '#ff27a9'), false, 80);
      glow(c, root, '#ff2fae', 0, 4, -11, 7, .06);
    }
    text(c, root, 'NEON ROOM', 7, '#e3c4ff').setLocalPosition(0, 4.8, -13.6);
    text(c, root, 'FIND YOUR FREQUENCY', 4, '#d9ff64').setLocalPosition(0, 3.8, -13.5);
    b(8, .5, 4, 0, .15, -11, black); b(4.5, 1.2, 1.2, 0, 1, -10.6, chrome);
    b(4.6, .04, 1.25, 0, 1.63, -10.6, n('#d9ff64'));
    for (const x of [-4.4, 4.4]) {
      b(1.1, 3.2, 1, x, 1.65, -11, black);
      for (const y of [.7, 1.8, 2.7]) torus(c, root, .33, .045, x, y, -10.48, m('#4c3d5d', .6, .35), false, 28);
    }
    for (let x = -6; x <= 6; x += 3) {
      b(.12, .12, 25, x, 6.2, 0, chrome);
      for (let z = -8; z <= 8; z += 4) { sphere(c, root, .1, x, 6.1, z, n('#b36bff')); glow(c, root, '#ee55c8', x, 6, z, .55, .14); }
    }
    b(3.2, 1.05, 8, -10, .5, 1, m('#281d35', .8, .24)); b(3.3, .1, 8.1, -10, 1.08, 1, chrome);
    b(.04, .07, 8.1, -8.33, .9, 1, n('#60e5ff'));
    const bar = text(c, root, 'BAR / RECHARGE', 3, '#69eeff');
    // text() returns a plane rotated into XY; preserve that rotation while turning the wall sign.
    bar.setLocalPosition(-12.8, 3.2, 1); bar.setLocalEulerAngles(90, 90, 0);
    for (let z = -2; z <= 4; z += 1.2) {
      cylinder(c, root, .09, .26, -8.7, 1.27, z, m('#e294ff', .35, .15), 12);
      glow(c, root, '#d54fff', -8.7, 1.3, z, .6, .5);
    }
    for (const z of [-1, 4, 9]) {
      b(2.6, .55, 2.4, 10, .3, z, m('#502462', .4, .4)); b(.45, 1, 2.4, 11.25, .7, z, m('#502462', .4, .4));
      b(2.7, .035, 2.5, 10, .03, z, n('#9d54ff'));
      cylinder(c, root, .65, .07, 7.8, .65, z, chrome, 32); b(.09, .65, .09, 7.8, .3, z, chrome);
    }
    const lounge = text(c, root, 'LOUNGE / SLOW DOWN', 4, '#dfa3ff');
    lounge.setLocalPosition(12.8, 3.3, 5); lounge.setLocalEulerAngles(90, -90, 0);
    b(11, .025, 10, 0, .01, -2, m('#30243f', .8, .22));
    for (const x of [-5.5, 5.5]) b(.045, .03, 10, x, .04, -2, n('#bd65ff'));
    for (const z of [-7, 3]) b(11, .03, .045, 0, .04, z, n('#bd65ff'));
    for (let i = 0; i < 8; i++) {
      const pivot = new pc.Entity('laser'); root.addChild(pivot); pivot.setLocalPosition((i % 4 - 1.5) * 2.3, 5.9, -9);
      const mat = unlit(i % 2 ? '#b05bff' : '#55d9ff', .11, null, true);
      const cone = mesh(c, pivot, pc.createCone(c.app.graphicsDevice, { baseRadius: .65, peakRadius: 0, height: 15, capSegments: 12, heightSegments: 1 }), mat);
      cone.setLocalPosition(0, -7.5, 0); this.lasers.push(pivot);
    }
    for (const [color, x, y, z, power] of [['#ff2aaf', 0, 5, -5, 110], ['#61cfff', -8, 3, 1, 55], ['#ff247b', 7, 3, 5, 65], ['#b875ff', 0, 3, 9, 45]]) light(c, root, color, power * .065, x, y, z);
    // Layered ceiling rigs and ribbed reflective wall panels follow the original club reference.
    for (const z of [-5, 2, 9]) {
      torus(c, root, 2.7, .065, 0, 5.8, z, chrome, true, 72);
      torus(c, root, 2.63, .022, 0, 5.73, z, n('#ff249c'), true, 72);
      for (const x of [-2.7, 2.7]) b(.07, 1.1, .07, x, 6.35, z, chrome);
    }
    for (const side of [-1, 1]) for (let z = -11; z <= 11; z += 2.2) {
      b(.12, 4.8, 1.75, side * 12.78, 2.8, z, m('#221828', .85, .18));
      b(.18, .055, 1.9, side * 12.65, 4.8, z, n(side < 0 ? '#ff26ae' : '#14cfff'));
      for (let k = -2; k <= 2; k++) b(.2, .018, 1.55, side * 12.65, 2.6 + k * .46, z, chrome);
    }
    // Narrow magenta light rails above the bar, cyan on the far wall.
    b(.06, .06, 24, -8.2, 4.4, 0, n('#ff2da9'));
    b(.06, .06, 24, 8.2, 4.4, 0, n('#16bfff'));
    light(c, root, '#f0bfd5', .3, 0, 4, 5, { directional: true });
    // One static mesh for floating dust, rather than 180 separate renderers.
    const vertices = [], indices = [];
    let seed = 42; const random = () => ((seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296);
    for (let i = 0; i < 180; i++) {
      const x = (random() - .5) * 25, y = random() * 6, z = (random() - .5) * 26, k = i * 3;
      vertices.push(x - .012, y, z, x + .012, y, z, x, y + .025, z); indices.push(k, k + 1, k + 2);
    }
    const dust = new pc.Mesh(c.app.graphicsDevice); dust.setPositions(vertices); dust.setIndices(indices); dust.update();
    const dustMat = unlit('#cbb0ff', .5);
    mesh(c, root, dust, dustMat, 'dust');
    for(const x of [-11.5,-5,5,11.5]) for(const z of [-7,-1,5])floorReflection(c,root,x<0?'#ff28ac':'#20ccff',x,z,1.2,5);
    // Static architecture shares materials and batches; articulated people stay separate.
    const batch = c.app.batcher.addGroup('architecture', false, 32);
    for (const e of root.findComponents('render')) {
      if (e.entity.name === 'club-box' || e.entity.name === 'neon-ring' || e.entity.name === 'light-orb' || e.entity.name === 'floor-reflection') e.batchGroupId = batch.id;
    }
    c.app.root.syncHierarchy(); c.app.batcher.generate([batch.id]);
  }
  draw(width, height, { yaw, pitch, time: t, now, moving, dancing, lowMotion }) {
    this.player.setLocalPosition(this.position); this.player.setLocalEulerAngles(0, yaw * degrees, 0);
    this.context.camera.setLocalEulerAngles(pitch * degrees, 0, 0);
    if (this.handModel) {
      const heart = this.hands.userData.action === 'heart' && this.hands.userData.until > now && !!this.heartModel;
      this.handModel.entity.enabled = !heart;if(this.heartModel)this.heartModel.entity.enabled = heart;
      this.handModel.entity.setLocalPosition(0, moving && !lowMotion ? Math.sin(t * 9) * .014 : 0, 0);
      this.handModel.parts.forEach((h, i) => {
        if (!h) return;
        h.setLocalEulerAngles((this.hands.userData.until > now ? -.7 : dancing && !lowMotion ? Math.sin(t * 6 + i) * .25 : 0) * degrees, 0, (i ? 1 : -1) * .28 * degrees);
        const p = h.getLocalPosition(); h.setLocalPosition((i ? 1 : -1) * (this.hands.userData.action === 'heart' && this.hands.userData.until > now ? .12 : .31), p.y, p.z);
      });
    }
    for (const n of this.npcs) {
      const d = n.userData, dance = d.dance && !lowMotion;
      n.position.y = dance ? Math.abs(Math.sin(t * 3 + d.phase)) * .07 : 0;
      n.entity?.setLocalPosition(n.position);
      n.entity?.setLocalEulerAngles(0, n.yaw * degrees, (dance ? Math.sin(t * 3 + d.phase) * .045 : 0) * degrees);
      d.arms?.forEach((a, i) => a?.setLocalEulerAngles((d.reactUntil > now ? -2 : dance ? -.15 + Math.sin(t * 3 + d.phase + i) * .22 : 0) * degrees, 0, (dance ? Math.sin(t * 2 + d.phase + i) * .13 : 0) * degrees));
      d.legs?.forEach((leg, i) => leg?.setLocalEulerAngles((dance ? Math.sin(t * 3 + d.phase + i * Math.PI) * .055 : 0) * degrees, 0, 0));
    }
    this.lasers.forEach((laser, i) => laser.setLocalEulerAngles((.4 + (lowMotion ? 0 : Math.sin(t * .4 + i) * .3)) * degrees, 0, (lowMotion ? .15 : Math.sin(t * .3 + i) * .4) * degrees));
    render(this.context, width, height);
  }
}
