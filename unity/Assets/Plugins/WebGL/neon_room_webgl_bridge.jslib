mergeInto(LibraryManager.library, {
  NeonRoomReportState: function (keyPointer, valuePointer) {
    var key = UTF8ToString(keyPointer);
    var value = UTF8ToString(valuePointer);
    var state = document.getElementById('neon-room-state');

    if (!state) {
      state = document.createElement('div');
      state.id = 'neon-room-state';
      state.setAttribute('role', 'status');
      state.setAttribute('aria-live', 'polite');
      state.style.position = 'fixed';
      state.style.left = '0';
      state.style.top = '0';
      state.style.width = '1px';
      state.style.height = '1px';
      state.style.overflow = 'hidden';
      state.style.clipPath = 'inset(50%)';
      state.style.whiteSpace = 'nowrap';
      document.body.appendChild(state);

      var announcement = document.createElement('div');
      announcement.id = 'neon-room-announcement';
      state.appendChild(announcement);

      var copy = document.createElement('div');
      copy.id = 'neon-room-ui-copy';
      state.appendChild(copy);
    }

    state.setAttribute('data-' + key, value);
    if (key !== 'ui-copy') {
      document.getElementById('neon-room-announcement').textContent = key + ': ' + value;
    }
    if (key === 'ui-copy') {
      document.getElementById('neon-room-ui-copy').textContent = value;
    }
    state.setAttribute('data-revision', String((Number(state.getAttribute('data-revision')) || 0) + 1));
  }
});
