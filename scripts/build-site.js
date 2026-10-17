import { cp, mkdir, rm } from 'node:fs/promises';

// Experimental PlayCanvas branch: the ported game is the site root and the
// original Three.js build stays available under /classic/ for comparison.
const playcanvasSource = new URL('../playcanvas/', import.meta.url);
const classicSource = new URL('../src/', import.meta.url);
const output = new URL('../dist/', import.meta.url);
await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
await cp(playcanvasSource, output, { recursive: true });
await cp(classicSource, new URL('classic/', output), { recursive: true });
console.log('Built static site: playcanvas/ -> dist/ (root), src/ -> dist/classic/');
