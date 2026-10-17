import { pc, rgb } from './rendering.js';

export function avatarUrl(index, detail, base = import.meta.url) {
  if (!Number.isInteger(index) || index < 0 || index > 9) throw new RangeError('Unknown outfit');
  return new URL(`./assets/avatars/look-${index}-${detail ? 'detail' : 'crowd'}.glb`, base).href;
}

export class AvatarLibrary {
  constructor(context) { this.context = context; this.cache = new Map(); }

  load(url) {
    if (!this.cache.has(url)) {
      this.cache.set(url, new Promise((resolve, reject) => {
        const asset = new pc.Asset(url.split('/').pop(), 'container', { url });
        asset.once('load', () => { this.finishMaterials(asset); resolve(asset); });
        asset.once('error', error => { this.cache.delete(url); reject(new Error(`Could not load outfit: ${error}`)); });
        this.context.app.assets.add(asset);
        this.context.app.assets.load(asset);
      }));
    }
    return this.cache.get(url);
  }

  finishMaterials(asset) {
    for (const entry of asset.resource.materials || []) {
      const mat = entry.resource;
      if (mat && ['cloth', 'inner'].includes(mat.name)) {
        mat.normalMap = this.fabricNormal();
        mat.normalMapTiling.set(5, 7);
        mat.bumpiness = .16;
        mat.update();
      }
    }
  }

  fabricNormal() {
    if (this.fabric) return this.fabric;
    const size = 128, values = new Uint8Array(size * size); let seed = 42;
    for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
      seed = (seed * 1664525 + 1013904223) >>> 0;
      values[y * size + x] = 137 + seed % 15 + (x % 3 === 0 ? 14 : 0) + (y % 3 === 0 ? 8 : 0);
    }
    const texture = new pc.Texture(this.context.app.graphicsDevice, { name: 'woven-fabric-normal', width: size, height: size, format: pc.PIXELFORMAT_RGBA8, mipmaps: true });
    const bytes = texture.lock();
    const at = (x, y) => values[((y + size) % size) * size + (x + size) % size];
    for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
      const p = (y * size + x) * 4;
      const nx = (at(x - 1, y) - at(x + 1, y)) / 128, ny = (at(x, y - 1) - at(x, y + 1)) / 128;
      const length = Math.hypot(nx, ny, 1);
      bytes[p] = Math.round((nx / length * .5 + .5) * 255);
      bytes[p + 1] = Math.round((ny / length * .5 + .5) * 255);
      bytes[p + 2] = Math.round((1 / length * .5 + .5) * 255); bytes[p + 3] = 255;
    }
    texture.unlock(); this.fabric = texture; return texture;
  }

  async avatar(index, detail = false) {
    const asset = await this.load(avatarUrl(index, detail));
    const entity = asset.resource.instantiateRenderEntity();
    return { entity, arms: [0, 1].map(i => entity.findByName(`arm-${i}`)), legs: [0, 1].map(i => entity.findByName(`leg-${i}`)) };
  }

  async hands(color) {
    const asset = await this.load(new URL('./assets/avatars/hands.glb', import.meta.url).href);
    const entity = asset.resource.instantiateRenderEntity();
    const parts = [0, 1].map(i => entity.findByName(`hand-${i}`));
    const materials = (asset.resource.materials || []).map(x => x.resource).filter(x => x?.name === 'bracelet');
    for (const m of materials) { m.diffuse = rgb(color); m.update(); }
    return { entity, parts, materials };
  }
}
