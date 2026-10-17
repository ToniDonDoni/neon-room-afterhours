import { cp, mkdir, rm, writeFile, readFile } from 'node:fs/promises';
import { build } from 'esbuild';
const output = new URL('../dist/', import.meta.url);
await rm(output, { recursive: true, force: true });
await mkdir(output, { recursive: true });
for (const file of ['index.html', 'style.css', 'THIRD_PARTY_LICENSES.txt', 'assets']) {
  await cp(new URL('../src/' + file, import.meta.url), new URL(file, output), { recursive: true });
}
const result = await build({ entryPoints: ['src/app.js'], outfile: 'dist/app.js', bundle: true, plugins: [{ name: 'browser-worker-target', setup(build) {
  // PlayCanvas also supports Node workers. This static browser build uses the Web Worker branch.
  build.onLoad({ filter: /[\\/]playcanvas[\\/].*-worker\.js$/ }, async ({ path }) => ({ contents: (await readFile(path, 'utf8')).replace('typeof self !== \"undefined\" && self || require(\"node:worker_threads\").parentPort', 'self'), loader: 'js' }));
} }], format: 'esm', target: ['es2022'], minify: true, legalComments: 'eof', metafile: true, logOverride: { 'import-is-undefined': 'error' } });
if (Object.keys(result.metafile.inputs).some(path => /(^|\/)three(\.|\/)/.test(path))) throw new Error('Three.js must not enter the browser bundle');
await writeFile(new URL('../build-meta.json', import.meta.url), JSON.stringify(result.metafile, null, 2));
console.log('Built PlayCanvas static application → dist/ (no Three.js runtime)');
