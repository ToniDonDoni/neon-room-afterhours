#!/usr/bin/env python3
"""Measure the in-page frame rate of a built NEON ROOM site.

Works for both the PlayCanvas build (dist/) and the reference Three.js build
(dist/classic/): it serves a directory, runs the shared onboarding flow, then
counts requestAnimationFrame callbacks for a few seconds inside the club.

Usage:
    python3 scripts/pc-bench.py --dir dist --label playcanvas
    python3 scripts/pc-bench.py --dir dist/classic --label threejs
"""
import argparse
import functools
import glob
import http.server
import json
import os
import socketserver
import threading

from playwright.sync_api import sync_playwright


def find_chromium():
    if os.environ.get("PC_CHROMIUM"):
        return os.environ["PC_CHROMIUM"]
    for pattern in ("~/.cache/ms-playwright/chromium-*/chrome-linux*/chrome",
                    "~/.cache/ms-playwright/chromium-*/chrome-linux/chrome"):
        matches = sorted(glob.glob(os.path.expanduser(pattern)))
        if matches:
            return matches[-1]
    return None


class Quiet(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


def serve(directory):
    socketserver.TCPServer.allow_reuse_address = True
    httpd = socketserver.TCPServer(("127.0.0.1", 0), functools.partial(Quiet, directory=directory))
    threading.Thread(target=httpd.serve_forever, daemon=True).start()
    return httpd, httpd.server_address[1]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dir", required=True)
    parser.add_argument("--label", default="")
    parser.add_argument("--seconds", type=int, default=4)
    parser.add_argument("--out", default=None)
    args = parser.parse_args()

    httpd, port = serve(os.path.abspath(args.dir))
    with sync_playwright() as p:
        browser = p.chromium.launch(executable_path=find_chromium(), args=[
            "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
            "--no-sandbox", "--autoplay-policy=no-user-gesture-required",
        ])
        page = browser.new_page(viewport={"width": 1280, "height": 800})
        page.goto(f"http://127.0.0.1:{port}/index.html")
        page.wait_for_timeout(900)
        page.click("#start")
        page.fill("#nickname", "Bench")
        page.click("#next-name")
        page.wait_for_timeout(800)
        page.click("#next-outfit")
        page.wait_for_timeout(150)
        page.click("#enter-club")
        page.wait_for_timeout(250)
        page.click('[data-control="desktop"]')
        page.wait_for_timeout(150)
        page.click("#confirm-controls")
        page.wait_for_timeout(2500)
        canvas = page.evaluate("() => { const c = document.querySelector('#world canvas'); return c ? [c.width, c.height] : null; }")
        fps = page.evaluate("""() => new Promise(resolve => {
            let frames = 0; const start = performance.now();
            const tick = () => {
                frames += 1;
                if (performance.now() - start < %d * 1000) requestAnimationFrame(tick);
                else resolve(Math.round(frames / ((performance.now() - start) / 1000) * 10) / 10);
            };
            requestAnimationFrame(tick);
        })""" % args.seconds)
        stats = page.evaluate("() => (window.__neonRoom && window.__neonRoom.stats) || null")
        browser.close()
    httpd.shutdown()

    report = {"label": args.label or args.dir, "dir": os.path.abspath(args.dir), "canvas": canvas,
              "fps": fps, "seconds": args.seconds, "stats": stats}
    print(json.dumps(report, ensure_ascii=False))
    if args.out:
        with open(args.out, "w", encoding="utf-8") as fh:
            json.dump(report, fh, ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
