# Specification 005: Port NEON ROOM to Unity

## 1. Problem and current state

NEON ROOM is a JavaScript browser game. Use the game on `main` as the baseline and create the Unity implementation branch from `main`.

## 2. Desired state

Recreate the game in Unity, preserving all current gameplay mechanics and matching the supplied [visual reference](../References/club-meeting-reference.png): a dark nightclub with pink/violet neon, cyan accents, reflective materials, detailed characters, first-person hands and a circular action menu.

## 3. Acceptance criteria

- [ ] The game is implemented in Unity `6000.3.6f1`, runs in the Editor and builds into a playable macOS application.
- [ ] All current gameplay mechanics from `main` are preserved, including wardrobe and location selection, movement, NPC interactions, gestures, dancing, drinks, music, recorded replies and settings.
- [ ] Actual Unity gameplay looks visually similar to `References/club-meeting-reference.png` in lighting, palette, materials, characters and interface. Verify this with rendered gameplay screenshots.

## 4. Analysis

- **Engine:** use the installed Unity `6000.3.6f1`, C# and URP. Keep gameplay logic separate from rendering and input.
- **Visuals:** combine baked lighting, emissive neon, restrained bloom, reflection probes and PBR materials. Use detailed character and clothing meshes, prefabs and animations to approach the reference.
- **Porting:** translate the current JavaScript gameplay into C#. Recreate the club as a Unity scene; export usable geometry from `main` where practical. Use the Input System for controls and uGUI/TextMeshPro for menus and the action wheel.
- **Audio:** use AudioSource/AudioMixer for music and NPC speech, preserving the existing tracks and reply rules. Port procedural music generation to C#; lower music volume during replies without restarting playback.
- **Future glasses:** keep camera and input replaceable so an XR rig, for example for XREAL, can be added later. Glasses support is outside this task.
- **Unity MCP:** propose [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) for scene, asset, script and test automation on Mac. It uses a Unity package and a Python/uv server; pin compatible versions and verify the connection before use. See the [installation guide](https://coplaydev.github.io/unity-mcp/getting-started/install).
