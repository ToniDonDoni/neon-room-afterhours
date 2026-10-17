# WebGL Manual Smoke Test

Run this against the built Unity WebGL player served over HTTP. Record the browser, build output path, date, result, and screenshots for the wardrobe, club, and action wheel.

## Launch and visual review

1. Build the enabled game scene with the active WebGL Build Profile and serve the output over HTTP.
2. Load the player in a current desktop browser. Confirm the game loads without a Unity error screen and shows the wardrobe.
3. Capture the wardrobe, club, and action wheel at a usable desktop resolution. Compare each capture directly with the matching image described in `references/REFERENCES.md`.
4. Enter the club, move with WASD, open the action wheel with E, and confirm the scene remains responsive.

## Hidden-tab NPC reply

1. Enter the club and start the club music using the visible music control.
2. Move within 3.5 Unity units of an NPC and choose Talk from the visible action wheel.
3. Confirm the recorded reply starts, the same music track continues at reduced volume, and the speaking indicator is visible.
4. Switch to another browser tab for at least one second, then return to the game tab.
5. Confirm the reply has stopped, its speaking indicator is gone, and the music has returned to the selected volume without restarting the track.

Pass only when all steps succeed. Save the three reference captures and a short recording or annotated evidence of the tab-hide/recovery check with the WebGL test report.
