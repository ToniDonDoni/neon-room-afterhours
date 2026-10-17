import { readdir } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
async function check(dir) {
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const file = `${dir}/${entry.name}`;
    if (entry.isDirectory()) await check(file);
    else if (/\.(m?js)$/.test(file)) execFileSync(process.execPath, ['--check', file], { stdio: 'inherit' });
  }
}
await check('src');await check('scripts');await check('tests');
