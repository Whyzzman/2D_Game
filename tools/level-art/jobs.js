// Список робіт painter для зони: шматки каменю 16 × 16 юнітів (сітка від 0) у межах зони
// і два шари тла на кімнату. Запуск: node tools/level-art/jobs.js <зона>
const fs = require("fs");
const path = require("path");
const T = require("../../design/levels/painter/level-text.js");
const ROOT = path.resolve(__dirname, "..", "..");
const zoneName = process.argv[2] || "tomb";
const zone = T.parse(fs.readFileSync(path.join(ROOT, "Assets/Game/Levels", zoneName + ".txt"), "utf8"));
const CHUNK = 16, TERRAIN_PPU = 48, ROOM_PPU = 32, MARGIN = 3;
const b = zone.bounds, c = zone.cell;
const x0 = zone.origin[0] + b.minX * c, x1 = zone.origin[0] + b.maxX * c;
const y0 = zone.origin[1] + b.minY * c, y1 = zone.origin[1] + b.maxY * c;
for (let ix = Math.floor(x0 / CHUNK); ix * CHUNK < x1; ix++) {
  for (let iy = Math.floor(y0 / CHUNK); iy * CHUNK < y1; iy++) {
    const px = CHUNK * TERRAIN_PPU;
    console.log(`terrain t_${ix}_${iy} ${px} ${px} what=terrain&x=${ix * CHUNK}&y=${iy * CHUNK}&size=${CHUNK}&ppu=${TERRAIN_PPU}`);
  }
}
// Стіни тайників: окремий шматок каменю поверх отвору (у камені площини там повітря), з запасом
// BREAK_PAD юніта довкола — той самий шум, тож шов не видно; розбита стіна лишає отвір.
const BREAK_PAD = 0.5;
for (const r of zone.rooms) {
  for (let y = 0; y < r.h; y++) for (let x = 0; x < r.w; x++) {
    const k = r.keys[r.rows[y][x]];
    if (!k || k[0] !== "breakable") continue;
    const cx = r.x + x, cy = r.y + y, h = parseInt(k[2], 10);
    const w = Math.round((2 * c + 2 * BREAK_PAD) * TERRAIN_PPU), hh = Math.round((h * c + 2 * BREAK_PAD) * TERRAIN_PPU);
    console.log(`breakable b_${cx}_${cy} ${w} ${hh} what=breakable&cx=${cx}&cy=${cy}&h=${h}&pad=${BREAK_PAD}&ppu=${TERRAIN_PPU}`);
  }
}
// Передній план (front.js) — усі чотири зони (Ліс, Село й Столиця — 2026-11-04).
const FRONT_ZONES = ["tomb", "forest", "village", "capital", "marsh"], FRONT_MARGIN = 2;
for (const r of zone.rooms) {
  if (FRONT_ZONES.indexOf(zoneName) >= 0) {
    const w = Math.round((r.w * c + 2 * FRONT_MARGIN) * ROOM_PPU), h = Math.round((r.h * c + 2 * FRONT_MARGIN) * ROOM_PPU);
    console.log(`room ${r.id}_front ${w} ${h} what=room&id=${r.id}&layer=front&ppu=${ROOM_PPU}`);
  }
  for (const [layer, m] of [["mid", MARGIN], ["near", 0]]) {
    const w = Math.round((r.w * c + 2 * m) * ROOM_PPU), h = Math.round((r.h * c + 2 * m) * ROOM_PPU);
    console.log(`room ${r.id}_${layer} ${w} ${h} what=room&id=${r.id}&layer=${layer}&ppu=${ROOM_PPU}`);
  }
}
