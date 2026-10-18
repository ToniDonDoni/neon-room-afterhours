import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import test from 'node:test';
import { looks, lookNotes } from '../src/fashion.js';

test('all shipped UI copy and regenerated outfit metadata are English', async () => {
  async function inspect(dir) {
    for (const file of await readdir(dir, { withFileTypes: true })) {
      const path = `${dir}/${file.name}`;
      if (file.isDirectory()) await inspect(path);
      else if (/\.(html|js|json|css)$/.test(path)) assert.doesNotMatch(await readFile(path, 'utf8'), /\p{Script=Cyrillic}/u, `Untranslated copy in ${path}`);
    }
  }
  await inspect('src');
  const html = await readFile('src/index.html', 'utf8');
  assert.match(html, /<html lang="en">/);
  for (const label of ['Talk', 'Heart', 'Wave', 'High five', 'Dance', 'Drink']) assert.ok(html.includes(`<span>${label}</span>`), label);
  const metadata = JSON.parse(await readFile('src/assets/avatars/manifest.json', 'utf8'));
  assert.deepEqual(metadata.looks, looks);assert.deepEqual(metadata.lookNotes, lookNotes);
  assert.equal(looks.length, 10);assert.equal(lookNotes.length, 10);
  assert.doesNotMatch(await readFile('scripts/avatar-source/fashion.js', 'utf8'), /\p{Script=Cyrillic}/u, 'Offline model generation must keep English outfit descriptions');
});
