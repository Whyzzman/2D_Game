// Тло кімнат Села для painter (переробка зон; погоджений макет — design/levels/village-look, 2026-10-19).
// Два шари, як у Гробниці й Лісі:
//  mid  — далеке: присмерк із жаром над Столицею, дальні хати, дзвіниця, дим; у кам'яних — нава церкви
//         або склепіння погреба; у дерев'яних — стіна з колод у темряві;
//  near — ближнє: хати по краях вулиці з вогником у вікні, промені, пил; у церкві — вітраж і свічки;
//         у погребах і коморах — балки, бочки, павутина світла.
// Кожна кімната — зі свого seed: жодна хата не повторюється.
(function (root) {
  "use strict";
  const N = root.PaintNoise;
  const B = root.LevelBackdrop;

  function kind(room, look) {
    if (room.flags.some(f => f.startsWith("boss="))) return "bell";
    if (room.flags.indexOf("secret") >= 0) return "secret";
    if (look.open.indexOf(room.id) >= 0) return "open";
    if (room.id === look.church) return "church";
    if (look.stone.indexOf(room.id) >= 0) return "stone";
    return "timber";
  }

  // Хата: глина в каркасі, провисла солома, вікна темні або з вогником; у частини — голі крокви.
  function house(f, x, ground, w, h, o, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    const roofH = h * (0.7 + r() * 0.3), sag = 0.3 + r() * 0.5;
    c.fillStyle = o.wall; c.fillRect(...f.px(x, ground + h), w * S, h * S);
    c.strokeStyle = o.beam; c.lineWidth = S * 0.14;
    const [l, t] = f.px(x, ground + h), [rr, b] = f.px(x + w, ground);
    c.strokeRect(l, t, rr - l, b - t);
    c.beginPath(); c.moveTo(l, t); c.lineTo(rr, b); c.stroke();
    const n = Math.max(1, Math.round(w / 2.4));
    for (let i = 0; i < n; i++) {
      const wx = x + w * (i + 0.5) / n - 0.35, wy = ground + h * 0.45, lit = r() < o.lit;
      c.fillStyle = lit ? o.windowLit : o.window;
      c.fillRect(...f.px(wx, wy + 0.8), 0.7 * S, 0.8 * S);
      if (lit) f.glow(wx + 0.35, wy + 0.4, 1.6, o.windowLit, 0.32);
    }
    c.fillStyle = o.roof;
    c.beginPath();
    c.moveTo(...f.px(x - 0.5, ground + h - 0.1));
    c.quadraticCurveTo(...f.px(x + w * 0.3, ground + h + roofH * 0.55 - sag), ...f.px(x + w / 2, ground + h + roofH));
    c.quadraticCurveTo(...f.px(x + w * 0.7, ground + h + roofH * 0.55 - sag), ...f.px(x + w + 0.5, ground + h - 0.1));
    c.closePath(); c.fill();
    c.strokeStyle = B.rgba(o.roofLight, 0.4); c.lineWidth = Math.max(1, S * 0.03);
    for (let i = 0; i < w * 4; i++) {
      const [px, py] = f.px(x - 0.4 + r() * (w + 0.8), ground + h - 0.05);
      c.beginPath(); c.moveTo(px, py); c.lineTo(px + (r() - 0.5) * S * 0.2, py + S * (0.15 + r() * 0.3)); c.stroke();
    }
    if (r() < o.broken) {
      const hx = x + w * (0.35 + r() * 0.3), hw = w * 0.2;
      c.strokeStyle = o.beam; c.lineCap = "round"; c.lineWidth = S * 0.09;
      for (let k = -2; k <= 2; k++) {
        const bx = hx + k * hw * 0.4;
        c.beginPath(); c.moveTo(...f.px(bx, ground + h + roofH * 0.45));
        c.quadraticCurveTo(...f.px(bx + 0.1, ground + h + roofH * 0.8), ...f.px(bx + k * 0.12, ground + h + roofH * (0.95 + r() * 0.2)));
        c.stroke();
      }
    }
  }

  function houseRow(f, ground, o, seed, scale) {
    const r = N.rng(seed);
    for (let x = f.X0 - 2 + r() * 2; x < f.X0 + f.W + 2; x += (3.2 + r() * 2.4) * scale) {
      house(f, x, ground, (2.4 + r() * 1.6) * scale, (2 + r() * 1.3) * scale, o, seed * 17 + Math.round(x * 10));
    }
  }

  function belfry(f, x, ground, w, h, look) {
    const c = f.c, S = f.S, top = ground + h;
    c.fillStyle = look.towerStone;
    c.fillRect(...f.px(x - w / 2, top), w * S, h * S);
    c.fillRect(...f.px(x - w / 2 - 0.3, top + 0.4), (w + 0.6) * S, 0.4 * S);
    c.fillRect(...f.px(x - w / 2, top + 3.4), 0.5 * S, 3 * S);
    c.fillRect(...f.px(x + w / 2 - 0.5, top + 3.4), 0.5 * S, 3 * S);
    c.beginPath(); c.moveTo(...f.px(x - w / 2 - 0.5, top + 3.4)); c.lineTo(...f.px(x, top + 7.5)); c.lineTo(...f.px(x + w / 2 + 0.5, top + 3.4));
    c.closePath(); c.fill();
    f.glow(x, top + 1.8, 2.4, look.bellGlow, 0.32);
    c.fillStyle = look.bell;
    c.beginPath(); c.moveTo(...f.px(x - 0.5, top + 2.6));
    c.quadraticCurveTo(...f.px(x - 0.55, top + 1.2), ...f.px(x - 0.9, top + 0.9)); c.lineTo(...f.px(x + 0.9, top + 0.9));
    c.quadraticCurveTo(...f.px(x + 0.55, top + 1.2), ...f.px(x + 0.5, top + 2.6)); c.closePath(); c.fill();
  }

  function smoke(f, y0, y1, color, alpha, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    for (let i = 0; i < f.W; i++) {
      const x = f.X0 + r() * f.W, y = y0 + r() * (y1 - y0);
      const [px, py] = f.px(x + N.fbm(x, y, seed, 2) * 2, y), rad = S * (1.5 + r() * 3.5);
      const g = c.createRadialGradient(px, py, 0, px, py, rad);
      g.addColorStop(0, B.rgba(color, alpha * (0.4 + r() * 0.6))); g.addColorStop(1, B.rgba(color, 0));
      c.fillStyle = g; c.beginPath(); c.ellipse(px, py, rad * 1.4, rad, 0, 0, Math.PI * 2); c.fill();
    }
  }

  function stainedWindow(f, x, y, w, h, colors, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    c.save();
    c.beginPath(); f.pointedArch(x, w, y, h); c.clip();
    c.fillStyle = "#120d0b"; c.fillRect(...f.px(x - w, y + h), w * 2 * S, h * S);
    c.filter = `blur(${S * 0.04}px)`;
    for (let i = 0; i < w * h * 1.4; i++) {
      if (r() < 0.22) continue;
      const cx = x + (r() - 0.5) * w, cy = y + r() * h, rad = 0.35 + r() * 0.45, pts = 5 + Math.floor(r() * 3);
      c.beginPath();
      for (let k = 0; k < pts; k++) {
        const a = k / pts * Math.PI * 2 + r() * 0.5, d = rad * (0.7 + r() * 0.4);
        const [px, py] = f.px(cx + Math.cos(a) * d, cy + Math.sin(a) * d);
        k ? c.lineTo(px, py) : c.moveTo(px, py);
      }
      c.closePath();
      c.fillStyle = B.rgba(colors[Math.floor(r() * colors.length)], 0.5 + r() * 0.35); c.fill();
      c.strokeStyle = "rgba(18,13,11,0.85)"; c.lineWidth = S * 0.05; c.stroke();
    }
    c.filter = "none";
    c.restore();
    f.glow(x, y + h * 0.5, w * 1.3, colors[1], 0.22);
    c.strokeStyle = "#120d0b"; c.lineWidth = S * 0.14; c.beginPath(); f.pointedArch(x, w, y, h); c.stroke();
  }

  function candle(f, x, y, h, color) {
    const c = f.c, S = f.S;
    f.glow(x, y + h + 0.15, 1.8, color, 0.42);
    c.fillStyle = "#cbbca0"; c.fillRect(...f.px(x - 0.06, y + h), 0.12 * S, h * S);
    c.fillStyle = color; c.beginPath(); c.ellipse(...f.px(x, y + h + 0.12), S * 0.05, S * 0.12, 0, 0, Math.PI * 2); c.fill();
  }

  // Колоди стіни: горизонтальні, з темними швами, ледь різні тоном.
  function logWall(f, rect, color, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    for (let y = rect.y - 1; y < rect.y + rect.h + 1; y += 0.55) {
      c.fillStyle = B.shade(color, (r() - 0.5) * 0.25);
      c.fillRect(...f.px(f.X0, y + 0.55), f.W * S, 0.55 * S);
      c.fillStyle = "rgba(0,0,0,0.5)"; c.fillRect(...f.px(f.X0, y + 0.04), f.W * S, Math.max(1, 0.05 * S));
    }
  }

  function beams(f, rect, top, color, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    c.fillStyle = color;
    for (let x = rect.x + 1 + r() * 2; x < rect.x + rect.w - 1; x += 3 + r() * 2) {
      c.fillRect(...f.px(x - 0.2, top), 0.4 * S, rect.h * S);
    }
    c.fillRect(...f.px(rect.x, top - 0.4), rect.w * S, 0.45 * S);
  }

  function barrels(f, x0, x1, floor, color, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    for (let x = x0; x < x1; x += 1.4 + r() * 2.5) {
      if (r() < 0.4) continue;
      const h = 0.9 + r() * 0.3, w = 0.7;
      c.fillStyle = B.shade(color, (r() - 0.5) * 0.3);
      c.beginPath(); c.ellipse(...f.px(x, floor + h / 2), w * S / 2, h * S / 2, 0, 0, Math.PI * 2); c.fill();
      c.strokeStyle = "rgba(0,0,0,0.5)"; c.lineWidth = S * 0.04;
      [0.25, 0.75].forEach(t => { c.beginPath(); c.moveTo(...f.px(x - w * 0.48, floor + h * t)); c.lineTo(...f.px(x + w * 0.48, floor + h * t)); c.stroke(); });
    }
  }

  // ---------- шари ----------

  function paintMid(f, zone, room, look, rect, seed) {
    const k = kind(room, look), r = N.rng(seed);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "open" || k === "bell") {
      f.sky(look.skyTop, look.skyBottom);
      f.glow(rect.x + rect.w / 2, rect.y - 4, Math.max(18, rect.w * 0.7), look.ember, 0.32);
      if (k === "bell" || r() < 0.4) belfry(f, rect.x + rect.w * (0.2 + r() * 0.6), floor - 0.5, 2.6, k === "bell" ? 4 : 8 + r() * 3, look);
      houseRow(f, floor - 0.3, look.farHouse, seed, 0.9);
      smoke(f, rect.y - 3, floor + 5, look.smoke, 0.45, seed + 1);
      f.fog(floor - 1, floor + 6, look.fog, 0.35, seed + 2);
      return;
    }
    if (k === "church") {
      f.sky(look.innerTop, look.innerBottom);
      f.arcade({ top: top + 2, base: floor - 0.4, span: 6.6 + r() * 1.4, archH: Math.min(10, (top - floor) * 0.6), col: 1.0, offset: r() * 3,
        seed: seed + 3, broken: 0.2, cut: false, wall: look.nave, hole: look.naveHole, mist: B.rgba(look.warmFog, 0.45),
        column: look.naveColumn, columnLight: look.naveColumnLight });
      f.fog(floor - 1, floor + 8, look.warmFog, 0.35, seed + 4);
      return;
    }
    if (k === "stone" || k === "secret") {
      f.sky(look.innerTop, look.innerBottom);
      f.farNave({ offset: r() * 4, span: 5 + r() * 2, base: floor - 1, h: Math.min(rect.h + 2, 9), rib: look.vault, window: look.innerBottom });
      f.fog(floor - 1, top, look.warmFog, 0.25, seed + 5);
      return;
    }
    f.sky(look.innerTop, look.innerBottom);
    logWall(f, { x: f.X0, y: f.Y0, w: f.W, h: f.H }, look.logFar, seed + 6);
    f.fog(floor - 1, floor + 5, look.warmFog, 0.3, seed + 7);
  }

  function paintNear(f, zone, room, look, rect, seed) {
    const k = kind(room, look), r = N.rng(seed + 100);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "open" || k === "bell") {
      // хати по краях вулиці, середина вільна — крізь неї видно далечінь
      if (k === "open") {
        [rect.x - 1 + r() * 2, rect.x + rect.w - 5 - r() * 2].forEach((x, i) =>
          house(f, x, floor - 0.2, 4.6 + r() * 1.4, 3.2 + r() * 0.8, look.nearHouse, seed + 20 + i));
      }
      const shafts = rect.w > 24 ? 2 : 1;
      for (let i = 0; i < shafts; i++) f.beam(rect.x + rect.w * ((i + 0.3 + r() * 0.4) / shafts), top + 1, -0.2 - r() * 0.2, 1.6, top - floor + 3, look.beam, 0.14, seed + i);
      f.dust(rect.w * rect.h / 6, look.dust, seed + 21);
      return;
    }
    if (k === "church") {
      const cx = rect.x + rect.w * (0.4 + r() * 0.2);
      stainedWindow(f, cx, floor + 3.5, Math.min(4.2, rect.w * 0.15), Math.min(9, (top - floor) - 4.5), look.glass, seed + 22);
      f.beam(cx, floor + 3.5 + Math.min(9, top - floor - 4.5), 0.12, 2.4, top - floor, look.warmBeam, 0.2, seed + 23);
      [-1.6, -1.0, 1.0, 1.6, 0].forEach((dx, i) => candle(f, cx + dx, floor, 0.4 + (i % 2) * 0.2, look.flame));
      f.dust(rect.w * rect.h / 5, look.dust, seed + 24);
      return;
    }
    if (k === "stone" || k === "secret") {
      barrels(f, rect.x + 1.5, rect.x + rect.w - 1.5, floor, look.barrel, seed + 25);
      if (k === "secret") f.glow(rect.x + rect.w / 2, floor + 1, 3.5, look.secretGlow, 0.2);
      else candle(f, rect.x + rect.w * (0.3 + r() * 0.4), floor, 0.35, look.flame);
      f.dust(rect.w * rect.h / 6, look.dust, seed + 26);
      return;
    }
    beams(f, rect, top, look.beamWood, seed + 27);
    barrels(f, rect.x + 1.5, rect.x + rect.w - 1.5, floor, look.barrel, seed + 28);
    f.beam(rect.x + rect.w * (0.3 + r() * 0.4), top, (r() - 0.5) * 0.3, 0.8, top - floor + 1, look.warmBeam, 0.14, seed + 29);
    f.dust(rect.w * rect.h / 6, look.dust, seed + 30);
  }

  root.LevelBackdropVillage = { paintMid, paintNear, kind };
})(window);
