// Pure, engine-independent logic shared by the PlayCanvas port and its tests.

export const looks = [
  ['Draped Noir', '#24202b', 'Leather · asymmetric skirt'],
  ['Shadow Tailor', '#29262d', 'Deconstruction · wide trousers'],
  ['Ritual', '#24252b', 'High collar · platforms'],
  ['Acid Nomad', '#30312c', 'Oversized nylon · acid green'],
  ['Violet Veil', '#58304e', 'Violet · layering'],
  ['Night Mass', '#24222c', 'Long coat · chains'],
  ['Silver Noir', '#525260', 'Draped Noir · silver leather'],
  ['Ash Ritual', '#706a70', 'Ritual · ash cotton'],
  ['Violet Nomad', '#49334e', 'Acid Nomad · dark plum'],
  ['Midnight Tailor', '#252b39', 'Shadow Tailor · ink']
];

export const lookNotes = [
  'Asymmetric leather jacket, open midriff, wrap skirt and high platforms.',
  'Oversized shoulders, a layered shirt, wide trousers and heavy boots.',
  'Sculptural collar, elongated vest, rings and high boots.',
  'Oversized bomber, hood, cargo and acid drawcords.',
  'Fine violet knit, layered skirt, high boots and cuffs.',
  'Architectural coat, crossed straps, loose trousers and chains.',
  'Silver take on the asymmetric leather look.',
  'Ash take on the elongated silhouette with a high collar.',
  'Deep-violet take on rave cargo with metal hardware.',
  'Ink take on the deconstructed tailoring.'
];

// Silhouette family per look, mirroring the Three.js avatar builder.
export const lookStyles = [0, 1, 2, 3, 4, 5, 0, 2, 3, 1];

export const tracks = [
  { label: 'Acid afterglow · 124 BPM', bpm: 124, notes: [55, 55, 82.41, 65.41, 55, 110, 73.42, 65.41] },
  { label: 'Violet hours · 118 BPM', bpm: 118, notes: [49, 73.42, 58.27, 49, 98, 73.42, 65.41, 58.27] },
  { label: 'Electric pulse · 132 BPM', bpm: 132, notes: [65.41, 65.41, 98, 87.31, 65.41, 130.81, 98, 77.78] }
];

export const npcNames = ['Luna', 'Kira', 'Max', 'Void', 'Nika', 'Echo', 'Nova', 'Ash', 'Pixel', 'Iris', 'Zen', 'Mika'];
export const npcPositions = [[-3, -3], [2, -4], [0, -1], [3, 1], [-2, 2], [4, -6], [-4, -6], [-6, 4], [-6, -1], [7, 5], [7, 9], [3, 6]];
export const dancingNpcCount = 7;

export const zoneLabels = {
  entry: 'ENTRANCE GALLERY',
  bar: 'BAR / RECHARGE',
  lounge: 'LOUNGE / SLOW DOWN',
  stage: 'DJ STAGE',
  dancefloor: 'DANCEFLOOR'
};

export function zoneForPosition(x, z) {
  if (z > 7) return 'entry';
  if (x < -5.5) return 'bar';
  if (x > 5.5) return 'lounge';
  if (z < -6) return 'stage';
  return 'dancefloor';
}

// Same solid rectangles as the Three.js prototype.
export function isBlocked(x, z) {
  if (Math.abs(x) > 12.1 || Math.abs(z) > 13.1) return true;
  if (x < -7.95 && x > -12 && z > -3.55 && z < 5.55) return true;
  if (Math.abs(x) < 4.5 && z < -8.5) return true;
  if (Math.abs(x) > 3.5 && Math.abs(x) < 5.4 && z < -10) return true;
  for (const cz of [-1, 4, 9]) {
    if (x > 8.25 && x < 11.8 && Math.abs(z - cz) < 1.7) return true;
    if (Math.hypot(x - 7.8, z - cz) < 1) return true;
  }
  return false;
}

export function clampOutfit(value) {
  const n = Number(value);
  return Math.max(0, Math.min(looks.length - 1, Number.isFinite(n) ? Math.trunc(n) : 0));
}

export const defaultProfile = { name: '', outfit: 5, volume: 0.35 };

export function sanitizeProfile(raw) {
  const profile = { ...defaultProfile };
  if (raw && typeof raw === 'object') Object.assign(profile, raw);
  profile.outfit = clampOutfit(profile.outfit);
  const volume = Number(profile.volume);
  profile.volume = Number.isFinite(volume) ? Math.max(0, Math.min(1, volume)) : defaultProfile.volume;
  if (!['mobile', 'desktop'].includes(profile.controls)) delete profile.controls;
  return profile;
}

export function defaultControlMode(profile, coarsePointer) {
  return profile.controls || (coarsePointer ? 'mobile' : 'desktop');
}

// Nearest NPC by 3D distance, matching the prototype's behaviour.
export function nearestNpc(position, npcs) {
  let best = null;
  let distance = Infinity;
  for (const npc of npcs) {
    const dx = npc.x - position.x;
    const dy = (npc.y || 0) - position.y;
    const dz = npc.z - position.z;
    const d = Math.sqrt(dx * dx + dy * dy + dz * dz);
    if (d < distance) {
      distance = d;
      best = npc;
    }
  }
  return { npc: best, d: distance };
}

export const mapTargets = {
  0: { x: 0, y: 1.66, z: 4, yaw: 0 },
  1: { x: -6, y: 1.66, z: 1, yaw: Math.PI / 2 },
  2: { x: 6.2, y: 1.66, z: 5, yaw: -Math.PI / 2 },
  3: { x: 0, y: 1.66, z: 10, yaw: 0 }
};

export function canTakeDrink(x, z) {
  return !(x > -6.5 || z < -4 || z > 6);
}
