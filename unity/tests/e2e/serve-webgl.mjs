import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';

const root = path.resolve(process.env.UNITY_WEBGL_BUILD_PATH || new URL('../../build/webgl', import.meta.url).pathname);
const mimeTypes = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.json': 'application/json',
  '.css': 'text/css; charset=utf-8',
  '.png': 'image/png',
  '.ico': 'image/x-icon'
};

try {
  await stat(path.join(root, 'index.html'));
} catch {
  throw new Error(`Unity WebGL build is missing from ${root}. Build the WebGL player and set UNITY_WEBGL_BUILD_PATH.`);
}

createServer(async (request, response) => {
  try {
    const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    const requested = pathname === '/' ? '/index.html' : pathname;
    const filePath = path.resolve(root, `.${requested}`);
    if (!filePath.startsWith(`${root}${path.sep}`)) {
      response.writeHead(403).end('Forbidden');
      return;
    }

    const data = await readFile(filePath);
    const compressed = filePath.endsWith('.gz');
    const uncompressedExtension = compressed ? path.extname(filePath.slice(0, -3)) : path.extname(filePath);
    response.writeHead(200, {
      'Content-Type': mimeTypes[uncompressedExtension] || 'application/octet-stream',
      ...(compressed ? { 'Content-Encoding': 'gzip' } : {}),
      'Cache-Control': 'no-store'
    });
    response.end(data);
  } catch {
    response.writeHead(404).end('Not found');
  }
}).listen(4173, '127.0.0.1', () => console.log(`Serving Unity WebGL build from ${root} at http://127.0.0.1:4173`));
