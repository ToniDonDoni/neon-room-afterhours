export const NPC_POSITIONS = [[-3,-3],[2,-4],[0,-1],[3,1],[-2,2],[4,-6],[-4,-6],[-6,4],[-6,-1],[7,5],[7,9],[3,6]];
export const NPC_NAMES = ['Luna','Kira','Max','Void','Nika','Echo','Nova','Ash','Pixel','Iris','Zen','Mika'];
export const MAP_ZONES = [[0,4],[-6,1],[6.2,5],[0,10]];
export function blocked(x, z) {
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
export function nearestNpc(npcs, position) {
  let npc = null, d = Infinity;
  for (const candidate of npcs) {
    const p = candidate.position;
    const distance = Math.hypot(p.x-position.x, p.y-position.y, p.z-position.z);
    if (distance < d) { npc = candidate; d = distance; }
  }
  return { npc, d };
}
