import test from 'node:test';
import assert from 'node:assert/strict';
import { createJoystick } from '../src/controls.js';

function makePad() {
  const handlers = new Map();
  const captures = new Set();
  return {
    thumb: { style: {} },
    classList: { add() {}, remove() {} },
    querySelector() { return this.thumb; },
    getBoundingClientRect() { return { left: 0, top: 0, width: 124, height: 124 }; },
    setPointerCapture(id) { captures.add(id); },
    hasPointerCapture(id) { return captures.has(id); },
    releasePointerCapture(id) { captures.delete(id); },
    addEventListener(type, fn) { handlers.set(type, fn); },
    fire(type, pointerId, x = 62, y = 62) {
      handlers.get(type)({ pointerId, clientX: x, clientY: y, button: 0, preventDefault() {} });
    }
  };
}

test('two sticks can move and look at the same time, with independent pointer ownership', () => {
  const movePad = makePad();
  const lookPad = makePad();
  let move = [0, 0];
  let look = [0, 0];
  const moveStick = createJoystick(movePad, (...value) => { move = value; });
  const lookStick = createJoystick(lookPad, (...value) => { look = value; });

  movePad.fire('pointerdown', 1, 62, 20);
  lookPad.fire('pointerdown', 2, 110, 62);
  assert.ok(move[1] < -0.99);
  assert.ok(look[0] > 0.99);

  movePad.fire('pointerup', 2);
  assert.ok(move[1] < -0.99, 'another pointer must not release this stick');
  lookPad.fire('pointerup', 2);
  assert.deepEqual(look, [0, 0]);
  assert.ok(move[1] < -0.99, 'releasing look must not stop movement');
});

test('stick applies a dead zone, caps diagonals and releases on cancel', () => {
  const pad = makePad();
  let value = [0, 0];
  const stick = createJoystick(pad, (...next) => { value = next; });

  pad.fire('pointerdown', 1, 63, 63);
  assert.deepEqual(value, [0, 0]);
  pad.fire('pointermove', 1, 0, 0);
  assert.ok(Math.hypot(...value) <= 1.00001);
  pad.fire('pointercancel', 1);
  assert.deepEqual(value, [0, 0]);
  stick.reset();
});
