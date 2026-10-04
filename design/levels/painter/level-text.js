// Читає текст зони (Assets/Game/Levels/<зона>.txt, формат — gamedesign/LEVEL_TOOL.md) для painter:
// клітинки, кімнати, межі. Працює і в браузері (window.LevelText), і в node (module.exports).
(function (root) {
  "use strict";

  function parse(text) {
    const zone = { cell: 0.5, origin: [0, 0], rooms: [], solid: new Map() };
    const lines = text.split(/\r?\n/);
    let room = null, grid = null;
    for (const raw of lines) {
      const line = raw.replace(/\s+#.*$/, "");
      if (grid) {
        if (line.trim() === "end") {
          room.h = grid.length;
          room.w = grid[0].length;
          room.rows = grid.reverse(); // rows[y] — знизу вгору
          zone.rooms.push(room);
          grid = null; room = null;
        } else {
          grid.push(raw.trimEnd());
        }
        continue;
      }
      const words = line.trim().split(/\s+/);
      if (words[0] === "cell") zone.cell = parseFloat(words[1]);
      else if (words[0] === "origin") zone.origin = [parseFloat(words[1]), parseFloat(words[2])];
      else if (words[0] === "room") {
        const at = words.indexOf("at");
        const name = line.match(/«(.*)»/);
        room = { id: words[1], name: name ? name[1] : words[1], x: parseInt(words[at + 1], 10),
          y: parseInt(words[at + 2], 10), flags: [], keys: {} };
      } else if (words[0] === "flags" && room) room.flags = words.slice(1);
      else if (words[0] === "key" && room) room.keys[words[1]] = words.slice(2);
      else if (words[0] === "grid" && room) grid = [];
    }

    // Клітинки: усе поза кімнатами — камінь.
    zone.cells = new Map();
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (const r of zone.rooms) {
      for (let y = 0; y < r.h; y++) {
        const row = r.rows[y];
        for (let x = 0; x < r.w; x++) zone.cells.set(key(r.x + x, r.y + y), row[x] || "#");
      }
      minX = Math.min(minX, r.x); minY = Math.min(minY, r.y);
      maxX = Math.max(maxX, r.x + r.w); maxY = Math.max(maxY, r.y + r.h);
    }
    zone.bounds = { minX, minY, maxX, maxY }; // у клітинках
    return zone;
  }

  function key(x, y) { return x * 100003 + y; }

  function at(zone, x, y) {
    const c = zone.cells.get(key(x, y));
    return c === undefined ? "#" : c;
  }

  // Камінь, по якому ходять і об який б'ються. Уступи «=» — окремо, шипи — повітря (свої об'єкти).
  function isRock(c) { return c === "#" || c === "%"; }
  function isLedge(c) { return c === "="; }
  function isAir(c) { return !isRock(c) && !isLedge(c); }

  function roomOf(zone, x, y) {
    for (const r of zone.rooms) {
      if (x >= r.x && x < r.x + r.w && y >= r.y && y < r.y + r.h) return r;
    }
    return null;
  }

  // Світові координати.
  function world(zone, cx, cy) { return [zone.origin[0] + cx * zone.cell, zone.origin[1] + cy * zone.cell]; }

  const api = { parse, at, isRock, isLedge, isAir, roomOf, world };
  if (typeof module !== "undefined" && module.exports) module.exports = api;
  else root.LevelText = api;
})(typeof window !== "undefined" ? window : this);
