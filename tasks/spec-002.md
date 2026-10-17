# Specification 002: Recorded voice replies during NPC interactions

## 1. Problem and current state

NPCs currently answer social gestures visually. The user supplied a short video containing a woman's voice and requested that recording as the NPC reply while the existing club music continues.

## 2. Desired state

Players approach an NPC and choose **Talk** or a social gesture. The extracted recording plays once as a foreground reply. The current procedural club track continues at a reduced volume during the reply, then returns to the player's chosen level. The same recording is used for all NPCs in this prototype.

## 3. Acceptance criteria

> As a player on a phone or computer, I want to approach a character, open Actions and select Talk or a greeting, then hear the recorded voice over the club music so that the interaction feels like a conversation.

- [x] The action menu contains a Talk button on phone and desktop layouts.
- [x] Talk, heart, wave and high-five interactions within 3.5 scene units start the supplied voice recording.
- [x] Approaching alone, distant NPCs, an empty scene, dancing and drinks do not start the voice.
- [x] Playback is requested directly within the player's interaction event.
- [x] The recording plays once; repeated inputs during a reply cannot overlap or restart it.
- [x] The existing music track and beat clock continue; only its gain is temporarily reduced.
- [x] Speech respects the volume setting. Completion, playback failure and manual stopping restore the music gain.
- [x] Opening a menu, leaving the club or hiding the tab stops the reply.
- [x] The recording is bundled with relative paths for both hosting targets, without a Drive dependency at runtime.
- [x] Automated tests exercise the actual HTML action buttons and app handlers, including voice playback and music restoration.

## 4. Analysis

Use the actual extracted voice, rather than synthesized speech or the one-minute music-and-voice mix. Bundling a short MP3 avoids external authentication and duplicate background music. An HTML media element supports playback from taps, clicks and keyboard-triggered UI actions; browser playback failures must be handled without leaving music ducked.

This is a prerecorded local NPC response, not live voice chat, speech recognition or generated conversation. The original club synthesizer, track selection and music playback state remain unchanged.

## 5. Proposed solution

- Store the cleaned 6.4-second recording at `src/assets/audio/npc-voice.mp3`.
- Use `src/npc-interaction.js` for distance validation, single-reply playback, cancellation and error handling.
- Invoke it from the existing social action handler for Talk, heart, wave and high-five.
- Reduce the music gain to 35% of its selected level while speech is active; restore it when speech finishes or stops.
- Show a speaking indicator with a Stop button. Use the existing volume setting for speech as well as music.
- Run the Node test suite and the normal static build. Tests dispatch the shipped action buttons with a media adapter; asset decoding is verified separately. Real-device autoplay and listening checks remain part of device QA.

## 6. ToDo

- [ ] Validate audibility and media policies on reference iOS and Android devices.
- [ ] Add distinct recordings and dialogue content per character in a later task.
- [ ] Consider subtitles after the dialogue transcript is confirmed.
