/** Recorded NPC replies, started synchronously by the player's interaction. */
export function createNpcInteraction({ voice, range = 3.5, onSpeakingChange = () => {}, onError = () => {} }) {
  let speaking = false;
  let generation = 0;
  voice.loop = false;

  function setVolume(value) {
    voice.volume = Math.max(0, Math.min(1, Number(value) || 0));
  }

  function stop() {
    generation++;
    voice.pause();
    try { voice.currentTime = 0; } catch { /* Metadata may not have loaded yet. */ }
    if (speaking) {
      speaking = false;
      onSpeakingChange(false);
    }
  }

  function fail(error, attempt = generation) {
    if (attempt !== generation || !speaking) return;
    stop();
    onError(error);
  }

  voice.addEventListener('ended', stop);
  voice.addEventListener('error', () => fail(voice.error));

  return {
    interact({ npc, d } = {}, volume = 1) {
      if (!npc || !Number.isFinite(d) || d > range) return 'out-of-range';
      if (speaking) return 'busy';
      const attempt = ++generation;
      speaking = true;
      setVolume(volume);
      onSpeakingChange(true);
      try {
        voice.currentTime = 0;
        // Keep play() in the tap/click/key event to satisfy mobile media policies.
        Promise.resolve(voice.play()).catch(error => fail(error, attempt));
      } catch (error) {
        fail(error, attempt);
        return 'unavailable';
      }
      return 'started';
    },
    stop,
    setVolume,
    get speaking() { return speaking; },
  };
}
