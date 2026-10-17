#!/usr/bin/env python3
"""Capture comparison screenshots of the PlayCanvas port.

Serves the built site, drives the full onboarding, then poses the camera inside
the club and saves 14 1400x800 JPEGs plus two reference comparison sheets to
playcanvas/screenshots/. The meeting shots reproduce the composition of
playcanvas/reference/club-meeting-reference.webp: a close NPC, the action wheel
on the right and the first-person heart gesture.

Usage:
    python3 scripts/pc-shots.py [--dir dist] [--out playcanvas/screenshots]
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


def which(name):
    for folder in os.environ.get("PATH", "").split(os.pathsep):
        candidate = os.path.join(folder, name)
        if os.path.isfile(candidate) and os.access(candidate, os.X_OK):
            return candidate
    return None


def find_chromium():
    if os.environ.get("PC_CHROMIUM"):
        return os.environ["PC_CHROMIUM"]
    for pattern in ("~/.cache/ms-playwright/chromium-*/chrome-linux*/chrome",
                    "~/.cache/ms-playwright/chromium-*/chrome-linux/chrome"):
        matches = sorted(glob.glob(os.path.expanduser(pattern)))
        if matches:
            return matches[-1]
    for name in ("chromium", "chromium-browser", "google-chrome"):
        found = which(name)
        if found:
            return found
    return None


class Quiet(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass


def serve(directory):
    socketserver.TCPServer.allow_reuse_address = True
    httpd = socketserver.TCPServer(("127.0.0.1", 0), functools.partial(Quiet, directory=directory))
    port = httpd.server_address[1]
    threading.Thread(target=httpd.serve_forever, daemon=True).start()
    return httpd, port


def main():
    parser = argparse.ArgumentParser()
    root = os.path.join(os.path.dirname(__file__), "..")
    default_dir = os.path.join(root, "dist") if os.path.isdir(os.path.join(root, "dist")) else os.path.join(root, "playcanvas")
    parser.add_argument("--dir", default=default_dir)
    parser.add_argument("--out", default=os.path.join(root, "playcanvas", "screenshots"))
    parser.add_argument("--timeout", type=int, default=20000)
    args = parser.parse_args()

    out_dir = os.path.abspath(args.out)
    os.makedirs(out_dir, exist_ok=True)
    httpd, port = serve(os.path.abspath(args.dir))
    shots = []

    launch_args = [
        "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
        "--no-sandbox", "--autoplay-policy=no-user-gesture-required",
    ]

    with sync_playwright() as p:
        browser = p.chromium.launch(executable_path=find_chromium(), args=launch_args)
        page = browser.new_page(viewport={"width": 1400, "height": 800})
        errors = []
        page.on("console", lambda m: errors.append("console: " + m.text) if m.type == "error" else None)
        page.on("pageerror", lambda e: errors.append("pageerror: " + str(e)))

        def shot(name, note):
            page.wait_for_timeout(320)
            path = os.path.join(out_dir, name)
            page.screenshot(path=path, type="jpeg", quality=88)
            shots.append({"file": name, "note": note,
                          "zone": page.evaluate("window.__neonRoom ? window.__neonRoom.zone : ''"),
                          "npcs": page.evaluate("window.__neonRoom ? window.__neonRoom.npcCount : 0")})
            print("shot", name, "-", note)

        page.goto(f"http://127.0.0.1:{port}/index.html?shots=1")
        page.wait_for_timeout(900)
        shot("00-landing.jpg", "landing hero before onboarding")

        page.click("#start")
        page.fill("#nickname", "Neon")
        page.click("#next-name")
        page.wait_for_timeout(900)
        page.click('[data-outfit="5"]')
        page.wait_for_timeout(400)
        shot("01-wardrobe.jpg", "wardrobe with live avatar preview")
        page.click("#next-outfit")
        page.wait_for_timeout(200)
        page.click("#enter-club")
        page.wait_for_timeout(200)
        page.click('[data-control="desktop"]')
        page.wait_for_timeout(150)
        page.click("#confirm-controls")
        try:
            page.wait_for_function("window.__neonRoom && window.__neonRoom.playing", timeout=args.timeout)
        except Exception:
            print("ERRORS:", errors)
            raise
        page.wait_for_timeout(900)
        print("canvas", page.evaluate("(() => { const c = document.querySelector('#world canvas'); return { w: c && c.width, h: c && c.height, stats: window.__neonRoom.stats }; })()"))

        page.evaluate("window.__neonRoom.pose(0, 10, 0, -3)")
        shot("10-entrance.jpg", "entrance gallery looking into the club")
        page.evaluate("window.__neonRoom.pose(0, 3.4, 0, -3)")
        shot("20-dancefloor.jpg", "dancefloor towards the DJ stage")
        page.evaluate("window.__neonRoom.pose(-2.2, 1.6, -12, -2)")
        shot("21-crowd.jpg", "dancing crowd close up")
        page.evaluate("window.__neonRoom.pose(-5.6, 1, 90, -2)")
        shot("30-bar.jpg", "bar and recharge counter")
        page.evaluate("window.__neonRoom.act('drink')")
        shot("31-drink.jpg", "neon tonic in hand at the bar")
        page.evaluate("window.__neonRoom.pose(0, -4.2, 0, 3)")
        shot("40-stage.jpg", "DJ stage and rings")
        page.evaluate("window.__neonRoom.pose(4.6, 4, -90, -2)")
        shot("41-lounge.jpg", "lounge booths")

        # Meeting shots: NPC face to face with the action wheel and heart gesture.
        page.evaluate("window.__neonRoom.focus(0, 1.9)")
        page.wait_for_timeout(500)
        page.click("#actions-toggle")
        shot("50-meeting-wheel.jpg", "NPC meeting with the action wheel open")
        page.click('[data-action="heart"]')
        page.wait_for_timeout(150)
        page.click("#actions-toggle")
        page.wait_for_timeout(220)
        page.screenshot(path=os.path.join(out_dir, "51-meeting-heart.jpg"), type="jpeg", quality=88)
        shots.append({"file": "51-meeting-heart.jpg", "note": "heart gesture and NPC reply with wheel open",
                      "zone": page.evaluate("window.__neonRoom.zone"), "npcs": page.evaluate("window.__neonRoom.npcCount")})
        print("shot 51-meeting-heart.jpg - heart gesture with wheel open")
        page.click("#stop-npc-voice")

        page.evaluate("window.__neonRoom.focus(4, 1.8)")
        page.wait_for_timeout(300)
        page.evaluate("window.__neonRoom.act('wave'); document.querySelector('#action-wheel').hidden = false")
        page.screenshot(path=os.path.join(out_dir, "52-meeting-wave.jpg"), type="jpeg", quality=88)
        shots.append({"file": "52-meeting-wave.jpg", "note": "wave gesture close up",
                      "zone": page.evaluate("window.__neonRoom.zone"), "npcs": page.evaluate("window.__neonRoom.npcCount")})
        print("shot 52-meeting-wave.jpg - wave gesture close up")
        page.click("#stop-npc-voice")

        page.click("#people")
        shot("60-people.jpg", "people list modal")
        page.click("#close-modal")
        page.click("#map")
        shot("61-map.jpg", "club map modal")
        page.click("#close-modal")

        browser.close()

    httpd.shutdown()
    manifest = {"viewport": "1400x800", "engine": "PlayCanvas 2.7.4", "shots": shots}
    with open(os.path.join(out_dir, "manifest.json"), "w", encoding="utf-8") as fh:
        json.dump(manifest, fh, ensure_ascii=False, indent=2)
    print("wrote", len(shots), "shots to", out_dir)
    build_comparison(out_dir)


def build_comparison(out_dir):
    """Stack the reference mood image above the matching gameplay shots."""
    reference = os.path.join(out_dir, "..", "reference", "club-meeting-reference.webp")
    if not os.path.isfile(reference):
        print("reference image not found, skipping comparison sheets")
        return
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        print("Pillow not installed, skipping comparison sheets")
        return

    def load(path):
        return Image.open(path).convert("RGB")

    def sheet(pairs, name, width=1180, label_h=44):
        rows = [(label, img.resize((width, int(img.height * width / img.width)), Image.LANCZOS))
                for label, img in pairs]
        total_h = sum(label_h + img.height for _, img in rows) + 24 * (len(rows) - 1)
        canvas = Image.new("RGB", (width, total_h), (10, 8, 16))
        draw = ImageDraw.Draw(canvas)
        try:
            font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
        except Exception:
            font = ImageFont.load_default()
        y = 0
        for label, img in rows:
            draw.text((14, y + 10), label, fill=(240, 230, 255), font=font)
            y += label_h
            canvas.paste(img, (0, y))
            y += img.height + 24
        canvas.save(os.path.join(out_dir, name), quality=88)
        print("wrote", name, canvas.size)

    reference_img = load(reference)
    sheet([
        ("REFERENCE - club meeting mood (from the brief)", reference_img),
        ("PLAYCANVAS PORT - NPC encounter, heart gesture, action wheel",
         load(os.path.join(out_dir, "51-meeting-heart.jpg")))
    ], "comparison-meeting.jpg")
    sheet([
        ("REFERENCE - neon club mood (from the brief)", reference_img),
        ("PLAYCANVAS PORT - dancefloor view", load(os.path.join(out_dir, "21-crowd.jpg")))
    ], "comparison-club.jpg")


if __name__ == "__main__":
    main()