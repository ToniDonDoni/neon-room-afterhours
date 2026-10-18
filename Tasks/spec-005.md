# Specification 005: English-language game interface

## 1. Problem and current state

The public PlayCanvas game mixes Russian and English across the landing page, onboarding, outfit descriptions, controls, settings, HUD, NPC panels and notifications.

## 2. Desired state

Use natural English for every application-authored visible string and accessible label. Keep gameplay, profile persistence, models, music and the supplied prerecorded voice unchanged. The previous stable Site remains preserved; update the separate public PlayCanvas Site.

## 3. Acceptance criteria

> As a player, I want to choose my nickname, inspect all ten looks, select phone or keyboard/mouse controls, enter the club and understand every menu, action, prompt and notification in English.

- [x] Translate the landing page, document title/description and navigation; set the document language to English.
- [x] Translate nickname, wardrobe, location and control-selection screens, including all outfit descriptions and loading/error states.
- [x] Translate settings, map zones, NPC panel/status, radial actions, movement/look instructions and action notifications.
- [x] Translate placeholders and accessible names for buttons, joysticks and voice controls.
- [x] Keep offline authoring metadata and the shipped avatar manifest in English so rebuilding models does not restore Russian descriptions.
- [x] Update browser selectors and regression assertions for English copy.
- [x] Validate desktop and phone layouts and publish the English version at the existing public PlayCanvas URL.

## 4. Analysis

The game has no locale selector and the user requests a complete switch to English. Replace application copy directly rather than adding an unused multilingual framework. Preserve internal action keys, storage keys and asset paths. Player-entered names are user content and may use any script; the original prerecorded voice is audio, not interface text.

## 5. Proposed solution

Translate `src/index.html`, dynamic copy in `src/app.js`, and outfit descriptions in browser, manifest and offline authoring metadata. Keep the club's existing English 3D signs. Add an automated check for untranslated Cyrillic in shipped text sources and check rendered text/accessibility labels in Playwright. Reuse existing desktop/mobile flows.

## 6. ToDo

- [x] Complete final browser verification and public deployment.

Verification: 18 automated tests and static build passed. Desktop and 390×844 mobile Playwright flows passed with English visible copy/accessibility assertions, no JavaScript errors, and independent simultaneous touch control. The landing artwork is also copied to the static output; its existing image contains no written labels.
