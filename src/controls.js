// Independent pointer ownership allows movement and looking at the same time.
export function createJoystick(element, onChange) {
  const thumb = element.querySelector('.stick-thumb');
  let pointerId = null;
  function reset() {
    const previous = pointerId;
    pointerId = null;
    thumb.style.transform = 'translate(0px, 0px)';
    element.classList.remove('active');
    onChange(0, 0);
    if (previous !== null && element.hasPointerCapture(previous)) element.releasePointerCapture(previous);
  }
  function update(event) {
    const rect = element.getBoundingClientRect();
    const radius = rect.width * .32;
    let x = (event.clientX - rect.left - rect.width / 2) / radius;
    let y = (event.clientY - rect.top - rect.height / 2) / radius;
    const magnitude = Math.hypot(x, y);
    if (magnitude > 1) { x /= magnitude; y /= magnitude; }
    thumb.style.transform = `translate(${x * radius}px, ${y * radius}px)`;
    const strength = Math.min(1, magnitude);
    const scale = strength <= .12 ? 0 : (strength - .12) / (.88 * strength);
    onChange(x * scale, y * scale);
  }
  element.addEventListener('pointerdown', event => {
    if (pointerId !== null || event.button > 0) return;
    event.preventDefault();
    pointerId = event.pointerId;
    element.setPointerCapture(pointerId);
    element.classList.add('active');
    update(event);
  });
  element.addEventListener('pointermove', event => {
    if (event.pointerId !== pointerId) return;
    event.preventDefault();
    update(event);
  });
  for (const type of ['pointerup', 'pointercancel', 'lostpointercapture']) {
    element.addEventListener(type, event => { if (event.pointerId === pointerId) reset(); });
  }
  return { reset };
}
