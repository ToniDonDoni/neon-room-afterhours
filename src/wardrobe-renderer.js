import { pc, createContext, material, neon, cylinder, torus, light, render, degrees } from './rendering.js';
import { AvatarLibrary } from './avatars.js';

export class WardrobeRenderer {
  constructor() {
    this.canvas = document.createElement('canvas');
    this.context = createContext(this.canvas, { alpha: true, quality: 'high' });
    this.library = new AvatarLibrary(this.context);
    this.distance = 4.8; this.targetY = 1.06; this.angle = 0; this.request = 0;
    const c = this.context, root = c.app.root;
    light(c, root, '#e1edff', .8, -2, 3, 4, { directional: true });
    light(c, root, '#b58de8', .65, -2, 2, -1);
    light(c, root, '#c6f7ff', 1.1, 2, 3, 2);
    cylinder(c, root, .7, .08, 0, 0, 0, material('#382f4c', .85, .18), 48);
    torus(c, root, .65, .012, 0, .05, 0, neon('#d9ff64'), true);
    this.positionCamera();
  }
  async setOutfit(index) {
    const request = ++this.request;
    const model = await this.library.avatar(index, true);
    if (request !== this.request) { model.entity.destroy(); return; }
    this.model?.destroy(); this.model = model.entity;
    this.context.app.root.addChild(this.model);
  }
  positionCamera() {
    const camera = this.context.camera;
    camera.setLocalPosition(0, this.targetY + .06, this.distance);
    camera.lookAt(0, this.targetY, 0);
  }
  frame(view) {
    this.targetY = view === 'face' ? 1.91 : view === 'fabric' ? 1.15 : 1.06;
    this.distance = view === 'face' ? .72 : view === 'fabric' ? 2.25 : 4.8;
    this.positionCamera();
  }
  zoom(multiplier) { this.distance = Math.max(.55, Math.min(5.5, this.distance * multiplier)); this.positionCamera(); }
  draw(width, height, time, lowMotion) {
    this.model?.setLocalEulerAngles(0, (this.angle + (lowMotion ? 0 : Math.sin(time * .35) * .15)) * degrees, 0);
    render(this.context, width, height);
  }
}
