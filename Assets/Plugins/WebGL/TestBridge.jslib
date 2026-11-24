mergeInto(LibraryManager.library, {
  // MECH-009 test bridge: the game pushes a JSON state snapshot and the
  // harness reads window.__dwState. Presentation channel only; nothing in
  // the game reads it back.
  DwPushState: function (ptr) {
    try { window.__dwState = JSON.parse(UTF8ToString(ptr)); } catch (e) { }
  }
});
