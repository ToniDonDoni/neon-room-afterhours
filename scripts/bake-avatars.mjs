import { mkdir, writeFile, readFile, rename } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { GLTFExporter } from 'three/addons/exporters/GLTFExporter.js';
import { createFashionAvatar, createFirstPersonHands, looks, lookNotes } from './avatar-source/fashion.js';

// Node's Blob API supplies the same bytes that the browser exporter writes.
globalThis.FileReader = class {
  readAsArrayBuffer(blob) { blob.arrayBuffer().then(result => { this.result = result; this.onloadend?.(); }); }
  readAsDataURL(blob) { blob.arrayBuffer().then(result => { this.result = `data:${blob.type};base64,${Buffer.from(result).toString('base64')}`; this.onloadend?.(); }); }
};
const out = new URL('../src/assets/avatars/', import.meta.url);
await mkdir(out, { recursive: true });
const exporter = new GLTFExporter();
const records = [];
async function save(root, file, names = []) {
  root.name = file.replace('.glb', '');
  for (const [node, name] of names) node.name = name;
  root.traverse(node => { node.userData = {}; });
  root.updateMatrixWorld(true);
  const bytes = await exporter.parseAsync(root, { binary: true, trs: true });
  await writeFile(new URL(file, out), Buffer.from(bytes));
  root.traverse(node => node.geometry?.dispose());
  const packed = new URL(file + '.packed.glb', out);
  execFileSync(process.execPath, ['node_modules/gltfpack/cli.js', '-i', fileURLToPath(new URL(file, out)), '-o', fileURLToPath(packed), '-kn', '-km'], { stdio: 'pipe' });
  await rename(packed, new URL(file, out));
  return (await readFile(new URL(file, out))).byteLength;
}
for (let index = 0; index < looks.length; index++) {
  for (const detail of [true, false]) {
    const root = createFashionAvatar(index, { detail });
    const names = [...root.userData.arms.map((node, i) => [node, `arm-${i}`]), ...root.userData.legs.map((node, i) => [node, `leg-${i}`])];
    const materialNames = ['skin', 'lip', 'shadow', 'eye', 'iris', 'pupil', 'hair', 'strand', 'cloth', 'inner', 'leather', 'rubber', 'metal', 'seam', 'accent'];
    const materials = [];
    root.traverse(node => { if (node.material && !materials.includes(node.material)) materials.push(node.material); });
    // Material names are assigned by the source generator when available.
    materials.forEach((material, i) => { if (!material.name) material.name = `surface-${i}`; });
    const file = `look-${index}-${detail ? 'detail' : 'crowd'}.glb`;
    const bytes = await save(root, file, names);
    records.push({ index, detail, file, bytes });
  }
}
const hands = createFirstPersonHands('#a0a3ad');
hands.userData.braceletMaterials.forEach(m => m.name = 'bracelet');
await save(hands, 'hands.glb', hands.children.map((node, i) => [node, `hand-${i}`]));
await writeFile(new URL('manifest.json', out), JSON.stringify({ looks, lookNotes, models: records }, null, 2));
console.log(`Baked ${records.length} avatars and first-person hands; ${(records.reduce((s, x) => s + x.bytes, 0) / 1048576).toFixed(1)} MiB`);
