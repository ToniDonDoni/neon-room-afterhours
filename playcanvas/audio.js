// Procedural tech-house synthesizer, carried over from the Three.js prototype.
// Engine-independent: it only uses the Web Audio API.

import { tracks } from './logic.js';

const MUSIC_LEVEL = 0.45;
const DUCK_LEVEL = 0.35;

export function createClubAudio(trackList = tracks) {
  const state = {
    ctx: null,
    master: null,
    noise: null,
    scheduler: null,
    enabled: false,
    track: 0,
    volume: 0.35,
    ducked: false,
    silent: false,
    next: 0,
    beat: 0
  };

  function tone(freq, time, dur, type, vol) {
    const ctx = state.ctx;
    const o = ctx.createOscillator();
    const g = ctx.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, time);
    g.gain.setValueAtTime(vol, time);
    g.gain.exponentialRampToValueAtTime(0.0001, time + dur);
    o.connect(g);
    g.connect(state.master);
    o.start(time);
    o.stop(time + dur);
  }

  function beat(t, n) {
    const ctx = state.ctx;
    const notes = trackList[state.track].notes;
    if (n % 4 === 0) {
      const o = ctx.createOscillator();
      const g = ctx.createGain();
      o.frequency.setValueAtTime(145, t);
      o.frequency.exponentialRampToValueAtTime(43, t + 0.16);
      g.gain.setValueAtTime(0.75, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.28);
      o.connect(g);
      g.connect(state.master);
      o.start(t);
      o.stop(t + 0.3);
    }
    if (n % 2 === 0) {
      const s = ctx.createBufferSource();
      const f = ctx.createBiquadFilter();
      const g = ctx.createGain();
      s.buffer = state.noise;
      f.type = 'highpass';
      f.frequency.value = 7000;
      g.gain.setValueAtTime(n % 4 === 2 ? 0.12 : 0.05, t);
      g.gain.exponentialRampToValueAtTime(0.001, t + 0.07);
      s.connect(f);
      f.connect(g);
      g.connect(state.master);
      s.start(t);
      s.stop(t + 0.08);
    }
    tone(notes[Math.floor(n / 2) % 8], t, 0.12, 'triangle', 0.22);
    if (n % 8 === 4) tone(440 * 2 ** ((n % 32) / 12), t, 0.09, 'sine', 0.035);
  }

  function targetGain() {
    if (!state.enabled || state.silent) return 0;
    return state.volume * MUSIC_LEVEL * (state.ducked ? DUCK_LEVEL : 1);
  }

  function apply() {
    if (!state.ctx) return;
    state.master.gain.setTargetAtTime(targetGain(), state.ctx.currentTime, 0.15);
  }

  function tick() {
    if (!state.ctx || !state.enabled || document.hidden) return;
    const now = state.ctx.currentTime;
    if (state.next < now) state.next = now + 0.04;
    while (state.next < now + 0.15) {
      beat(state.next, state.beat++);
      state.next += 60 / trackList[state.track].bpm / 4;
    }
  }

  function ensure() {
    if (state.ctx) return;
    const Ctx = window.AudioContext || window.webkitAudioContext;
    const ctx = new Ctx();
    const master = ctx.createGain();
    master.gain.value = 0;
    master.connect(ctx.destination);
    const noise = ctx.createBuffer(1, ctx.sampleRate * 0.15, ctx.sampleRate);
    const data = noise.getChannelData(0);
    for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
    state.ctx = ctx;
    state.master = master;
    state.noise = noise;
    state.scheduler = setInterval(tick, 30);
  }

  function resume() {
    if (!state.ctx) return;
    const p = state.ctx.resume();
    if (p && p.catch) p.catch(() => {});
  }

  function toggle() {
    ensure();
    resume();
    state.enabled = !state.enabled;
    apply();
    return state.enabled;
  }

  function setVolume(value) {
    state.volume = Math.max(0, Math.min(1, Number(value) || 0));
    apply();
  }

  function setTrack(index) {
    state.track = Math.max(0, Math.min(trackList.length - 1, Number(index) || 0));
    return trackList[state.track].label;
  }

  function setDucked(value) {
    state.ducked = !!value;
    apply();
  }

  function setSilent(value) {
    state.silent = !!value;
    apply();
  }

  function suspend() {
    if (state.ctx && state.ctx.suspend) state.ctx.suspend().catch(() => {});
  }

  return {
    toggle, resume, setVolume, setTrack, setDucked, setSilent, suspend, apply,
    get enabled() { return state.enabled; },
    get track() { return state.track; },
    get context() { return state.ctx; },
    get trackLabel() { return trackList[state.track].label; }
  };
}

export { MUSIC_LEVEL, DUCK_LEVEL };