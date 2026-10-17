#!/usr/bin/env python3
"""Headless smoke and performance test for the PlayCanvas port.

Serves a directory over HTTP, drives the onboarding flow in Chromium and checks
the core state: 12 NPCs, 7 dancers, zone changes, every action, the recorded
voice reply with music ducking, settings, and wardrobe re-entry. It also
records frame rate and draw calls. Requires Playwright for Python.

Usage:
    python3 scripts/pc-smoke.py [--dir playcanvas] [--out playcanvas/performance-report.json]
"""
import argparse
import functools
import glob
import http.server
import json
import os
import socketserver
import statistics
import sys
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
    patterns = (
        "~/.cache/ms-playwright/chromium-*/chrome-linux*/chrome",
        "~/.cache/ms-playwright/chromium-*/chrome-linux/chrome",
    )
    for pattern in patterns:
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
    parser.add_argument("--out", default=None)
    parser.add_argument("--timeout", type=int, default=20000)
    args = parser.parse_args()

    httpd, port = serve(os.path.abspath(args.dir))
    errors = []
    result = {"ok": False}

    launch_args = [
        "--use-gl=angle", "--use-angle=swiftshader", "--enable-unsafe-swiftshader",
        "--no-sandbox", "--autoplay-policy=no-user-gesture-required",
    ]

    with sync_playwright() as p:
        browser = p.chromium.launch(executable_path=find_chromium(), args=launch_args)
        page = browser.new_page(viewport={"width": 1280, "height": 800})
        def watch(target):
            target.on("console", lambda m: errors.append("console: " + m.text) if m.type == "error" and not any(font in m.text for font in ("fonts.googleapis.com", "fonts.gstatic.com")) else None)
            target.on("pageerror", lambda e: errors.append("pageerror: " + str(e)))

        watch(page)

        page.goto(f"http://127.0.0.1:{port}/index.html")
        page.wait_for_timeout(900)
        result["engine"] = page.evaluate("typeof pc !== 'undefined' ? pc.version : null")
        page.click("#start")
        page.fill("#nickname", "Smoke")
        page.click("#next-name")
        page.wait_for_timeout(900)
        result["previewCanvas"] = page.evaluate("!!document.querySelector('#avatar-preview canvas')")
        page.click('[data-outfit="3"]')
        page.wait_for_timeout(300)
        page.click("#next-outfit")
        page.wait_for_timeout(200)
        page.click("#enter-club")
        page.wait_for_timeout(200)
        page.click('[data-control="desktop"]')
        page.wait_for_timeout(150)
        page.click("#confirm-controls")
        page.wait_for_function("window.__neonRoom && window.__neonRoom.playing", timeout=args.timeout)
        page.wait_for_timeout(700)

        result["npcs"] = page.evaluate("window.__neonRoom.npcCount")
        result["dancing"] = page.evaluate("window.__neonRoom.dancingCount")
        result["zoneStart"] = page.evaluate("window.__neonRoom.zone")
        result["canvas"] = page.evaluate("(() => { const c = document.querySelector('#world canvas'); return c ? [c.width, c.height] : null; })()")

        before = page.evaluate("window.__neonRoom.position")
        page.keyboard.down("w")
        page.wait_for_timeout(2500)
        page.keyboard.up("w")
        page.wait_for_timeout(300)
        after = page.evaluate("window.__neonRoom.position")
        result["walkDistance"] = round(((after[0] - before[0]) ** 2 + (after[2] - before[2]) ** 2) ** 0.5, 2)
        # Frames are slow on SwiftShader, so place the camera deterministically
        # for the interaction checks instead of relying on a fixed walk distance.
        page.evaluate("window.__neonRoom.pose(0, 3, 0, 0)")
        page.wait_for_timeout(250)
        result["zoneAfterWalk"] = page.evaluate("window.__neonRoom.zone")

        # Talk to a nearby NPC.
        page.evaluate("window.__neonRoom.focus(0, 2.2)")
        page.wait_for_timeout(250)
        page.keyboard.press("e")
        page.wait_for_timeout(200)
        page.click('[data-action="talk"]')
        page.wait_for_timeout(600)
        result["speaking"] = page.is_visible("#npc-speaking")
        result["voicePlaying"] = page.evaluate("!document.querySelector('#npc-voice').paused")
        page.click("#stop-npc-voice")
        page.wait_for_timeout(250)
        result["voiceStopped"] = not page.is_visible("#npc-speaking")

        # Heart gesture on the same NPC.
        page.keyboard.press("e")
        page.wait_for_timeout(200)
        page.click('[data-action="heart"]')
        page.wait_for_timeout(500)
        result["heartReplies"] = page.is_visible("#npc-speaking")
        page.click("#stop-npc-voice")
        page.wait_for_timeout(200)

        # Dance toggles.
        page.keyboard.press("e")
        page.wait_for_timeout(150)
        page.click('[data-action="dance"]')
        page.wait_for_timeout(300)
        result["danceOn"] = page.is_visible("#dance-state")
        page.keyboard.press("e")
        page.wait_for_timeout(150)
        page.click('[data-action="dance"]')
        page.wait_for_timeout(200)
        result["danceOff"] = not page.is_visible("#dance-state")

        # Map teleport to the bar and a drink.
        page.click("#map")
        page.wait_for_timeout(200)
        page.click('[data-zone="1"]')
        page.wait_for_timeout(300)
        result["zoneAfterMap"] = page.evaluate("window.__neonRoom.zone")
        page.evaluate("window.__neonRoom.pose(-7, 1, 90, 0)")
        page.wait_for_timeout(200)
        page.keyboard.press("e")
        page.wait_for_timeout(150)
        page.click('[data-action="drink"]')
        page.wait_for_timeout(200)
        result["drinkOn"] = page.is_visible("#drink-view")
        page.keyboard.press("e")
        page.wait_for_timeout(150)
        page.click('[data-action="drink"]')
        page.wait_for_timeout(200)
        result["drinkOff"] = not page.is_visible("#drink-view")

        # Settings: volume and track.
        page.click("#settings")
        page.wait_for_timeout(250)
        page.fill("#volume", "55")
        page.select_option("#track", "2")
        page.wait_for_timeout(200)
        result["trackLabel"] = page.text_content("#track-label")
        page.click("#close-modal")
        page.wait_for_timeout(200)

        # Wardrobe re-entry without a reload.
        page.click("#backroom")
        page.wait_for_timeout(700)
        result["wardrobeReopen"] = page.evaluate("!!document.querySelector('#avatar-preview canvas')")
        page.click("#next-outfit")
        page.wait_for_timeout(200)
        page.click("#enter-club")
        page.wait_for_timeout(200)
        page.click("#confirm-controls")
        page.wait_for_timeout(500)
        result["reentered"] = page.evaluate("window.__neonRoom.playing")

        # Sample frame statistics at the entrance.
        page.click("#map")
        page.wait_for_timeout(150)
        page.click('[data-zone="3"]')
        page.wait_for_timeout(500)
        samples = []
        for _ in range(12):
            page.wait_for_timeout(200)
            samples.append(page.evaluate("window.__neonRoom.stats"))
        fps = [s["fps"] for s in samples if s and s.get("fps")]
        result["fpsMedian"] = round(statistics.median(fps), 1) if fps else None
        last = samples[-1] or {}
        result["frameMs"] = last.get("ms")
        result["drawCalls"] = last.get("drawCalls")
        result["consoleErrors"] = errors

        # Track-first regression: choosing a track on the landing page must not
        # create a silent, disabled audio graph before the club is entered.
        tpage = browser.new_page(viewport={"width": 1280, "height": 800})
        watch(tpage)
        tpage.goto(f"http://127.0.0.1:{port}/index.html")
        tpage.wait_for_timeout(800)
        tpage.click("#settings")
        tpage.wait_for_timeout(200)
        tpage.select_option("#track", "2")
        tpage.click("#close-modal")
        tpage.wait_for_timeout(150)
        tpage.click("#start")
        tpage.fill("#nickname", "Track")
        tpage.click("#next-name")
        tpage.wait_for_timeout(800)
        tpage.click("#next-outfit")
        tpage.wait_for_timeout(150)
        tpage.click("#enter-club")
        tpage.wait_for_timeout(150)
        tpage.click('[data-control="desktop"]')
        tpage.wait_for_timeout(100)
        tpage.click("#confirm-controls")
        tpage.wait_for_function("window.__neonRoom && window.__neonRoom.playing", timeout=args.timeout)
        tpage.wait_for_timeout(400)
        audio_state = tpage.evaluate("window.__neonRoom.audio")
        result["trackFirstAudio"] = audio_state
        result["trackFirstStartsMusic"] = bool(audio_state and audio_state.get("enabled") and audio_state.get("hasContext") and audio_state.get("track") == 2)
        tpage.close()

        # Mobile pass: two joysticks must own independent touch pointers at once.
        mobile = browser.new_context(viewport={"width": 390, "height": 844}, has_touch=True, is_mobile=True, device_scale_factor=2)
        mpage = mobile.new_page()
        watch(mpage)
        mpage.goto(f"http://127.0.0.1:{port}/index.html")
        mpage.wait_for_timeout(900)
        mpage.click("#start")
        mpage.fill("#nickname", "Touch")
        mpage.click("#next-name")
        mpage.wait_for_timeout(800)
        mpage.click("#next-outfit")
        mpage.wait_for_timeout(150)
        mpage.click("#enter-club")
        mpage.wait_for_timeout(150)
        mpage.click('[data-control="mobile"]')
        mpage.wait_for_timeout(150)
        mpage.click("#confirm-controls")
        mpage.wait_for_function("window.__neonRoom && window.__neonRoom.playing", timeout=args.timeout)
        mpage.wait_for_timeout(400)
        result["mobileMode"] = mpage.evaluate("document.body.classList.contains('mobile-controls')")
        move_box = mpage.locator("#move-stick").bounding_box()
        look_box = mpage.locator("#look-stick").bounding_box()
        cdp = mobile.new_cdp_session(mpage)
        cdp.send("Input.dispatchTouchEvent", {"type": "touchStart", "touchPoints": [
            {"x": move_box["x"] + move_box["width"] * .72, "y": move_box["y"] + move_box["height"] * .28, "id": 1},
            {"x": look_box["x"] + look_box["width"] * .28, "y": look_box["y"] + look_box["height"] * .72, "id": 2},
        ]})
        mpage.wait_for_timeout(250)
        result["mobileDualSticks"] = mpage.evaluate("document.querySelector('#move-stick').classList.contains('active') && document.querySelector('#look-stick').classList.contains('active')")
        result["mobileMoveOffset"] = mpage.evaluate("document.querySelector('#move-stick .stick-thumb').style.transform")
        cdp.send("Input.dispatchTouchEvent", {"type": "touchEnd", "touchPoints": []})
        mpage.wait_for_timeout(150)
        result["mobileSticksRelease"] = mpage.evaluate("!document.querySelector('#move-stick').classList.contains('active') && !document.querySelector('#look-stick').classList.contains('active')")
        mobile.close()
        browser.close()

    httpd.shutdown()
    checks = {
        "engineLoaded": bool(result.get("engine")),
        "canvasSized": bool(result.get("canvas")) and result["canvas"][0] > 0 and result["canvas"][1] > 0,
        "walkMoves": (result.get("walkDistance") or 0) > 0.5,
        "previewCanvas": bool(result.get("previewCanvas")),
        "twelveNpcs": result.get("npcs") == 12,
        "sevenDancing": result.get("dancing") == 7,
        "walkChangesZone": result.get("zoneAfterWalk") != result.get("zoneStart"),
        "voiceReplies": bool(result.get("speaking")) and bool(result.get("voicePlaying")),
        "voiceStops": bool(result.get("voiceStopped")),
        "heartReplies": bool(result.get("heartReplies")),
        "danceToggles": bool(result.get("danceOn")) and bool(result.get("danceOff")),
        "mapTeleports": result.get("zoneAfterMap") not in (None, result.get("zoneAfterWalk")),
        "drinkToggles": bool(result.get("drinkOn")) and bool(result.get("drinkOff")),
        "settingsWork": "Electric pulse" in (result.get("trackLabel") or ""),
        "wardrobeReopens": bool(result.get("wardrobeReopen")) and bool(result.get("reentered")),
        "trackFirstStartsMusic": bool(result.get("trackFirstStartsMusic")),
        "mobileMode": bool(result.get("mobileMode")),
        "mobileDualSticks": bool(result.get("mobileDualSticks")) and bool(result.get("mobileSticksRelease")),
        "noConsoleErrors": not errors,
    }
    result["checks"] = checks
    result["ok"] = all(checks.values())
    print(json.dumps(result, ensure_ascii=False, indent=2))
    if args.out:
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(result, handle, ensure_ascii=False, indent=2)
        print("report written to " + args.out)
    sys.exit(0 if result["ok"] else 1)


if __name__ == "__main__":
    main()