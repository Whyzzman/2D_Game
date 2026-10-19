// Тло кімнат Лісу для painter (п. 4 переробки зон; погоджений макет — design/levels/forest-look,
// 2026-10-19). Ті самі два шари, що в Гробниці:
//  mid  — далеке: небо з місяцем, ряди мертвих дубів у тумані, подекуди руїна абатства; під землею —
//         стіна з переплетеного коріння в зеленкуватому тумані;
//  near — ближнє: стовбури по краях кімнати, промені місяця, пил; у закритих — хащі й руїни; під
//         землею — коріння зі стелі, брили, гриби, отруйне світло.
// Кожна кімната росте зі свого seed — жодне дерево не повторюється.
(function (root) {
  "use strict";
  const N = root.PaintNoise;
  const B = root.LevelBackdrop;

  function kind(zone, room, look, rect) {
    if (room.flags.some(f => f.startsWith("boss="))) return "arena";
    if (room.flags.indexOf("secret") >= 0) return "secret";
    if (rect.y + rect.h <= look.groundLine || look.underground.indexOf(room.id) >= 0) return "cave";
    if (look.enclosed.indexOf(room.id) >= 0) return "thicket";
    return "open";
  }

  // ---------- рослини й руїни ----------

  // Гілка: товщина спадає, напрям гнеться шумом, від неї — менші гілки.
  function branch(f, x, y, angle, len, width, seed, depth, color) {
    const c = f.c, S = f.S, r = N.rng(seed);
    let cx = x, cy = y, a = angle;
    const steps = Math.max(3, Math.round(len / 0.35));
    const pts = [[cx, cy]];
    for (let i = 1; i <= steps; i++) {
      a += (N.fbm(cx * 0.8, cy * 0.8, seed, 2) - 0.5) * 0.5;
      cx += Math.cos(a) * len / steps; cy += Math.sin(a) * len / steps;
      pts.push([cx, cy]);
    }
    c.strokeStyle = color; c.lineCap = "round"; c.lineJoin = "round";
    for (let i = 1; i < pts.length; i++) {
      c.lineWidth = Math.max(0.8, width * (1 - (i / pts.length) * 0.85) * S);
      c.beginPath(); c.moveTo(...f.px(...pts[i - 1])); c.lineTo(...f.px(...pts[i])); c.stroke();
    }
    if (depth <= 0) return;
    const forks = 2 + Math.floor(r() * 2);
    for (let k = 0; k < forks; k++) {
      const at = pts[Math.floor(pts.length * (0.35 + r() * 0.55))];
      branch(f, at[0], at[1], angle + (r() < 0.5 ? -1 : 1) * (0.4 + r() * 0.7), len * (0.4 + r() * 0.3),
        width * 0.55, seed * 7 + k + 1, depth - 1, color);
    }
  }

  function deadOak(f, x, ground, height, width, color, seed) {
    const c = f.c, r = N.rng(seed), lean = (r() - 0.5) * 0.6;
    c.fillStyle = color;
    c.beginPath();
    c.moveTo(...f.px(x - width * 1.6, ground - 0.3));
    c.quadraticCurveTo(...f.px(x - width * 0.55, ground + 0.4), ...f.px(x - width * 0.5 + lean, ground + height * 0.55));
    c.lineTo(...f.px(x - width * 0.32 + lean * 1.4, ground + height));
    c.lineTo(...f.px(x + width * 0.32 + lean * 1.4, ground + height));
    c.lineTo(...f.px(x + width * 0.5 + lean, ground + height * 0.55));
    c.quadraticCurveTo(...f.px(x + width * 0.55, ground + 0.4), ...f.px(x + width * 1.7, ground - 0.3));
    c.closePath(); c.fill();
    const n = 3 + Math.floor(r() * 3);
    for (let i = 0; i < n; i++) {
      branch(f, x + lean * 1.4, ground + height - r() * height * 0.25, Math.PI / 2 + (r() - 0.5) * 1.9,
        height * (0.3 + r() * 0.35), width * 0.45, seed * 13 + i, 2, color);
    }
  }

  function treeRow(f, o) {
    const r = N.rng(o.seed);
    for (let x = f.X0 - 2 + r() * o.gap; x < f.X0 + f.W + 2; x += o.gap * (0.6 + r() * 0.8)) {
      deadOak(f, x, o.ground, o.height * (0.75 + r() * 0.5), o.width * (0.7 + r() * 0.6), o.color,
        o.seed * 31 + Math.round(x * 10));
    }
  }

  // Корінь: товста крива по точках, з відгалуженнями.
  function rootLine(f, pts, width, color, seed) {
    const c = f.c, r = N.rng(seed);
    c.strokeStyle = color; c.lineCap = "round"; c.lineWidth = width * f.S;
    c.beginPath(); c.moveTo(...f.px(...pts[0]));
    for (let i = 1; i < pts.length - 1; i++) {
      const mx = (pts[i][0] + pts[i + 1][0]) / 2, my = (pts[i][1] + pts[i + 1][1]) / 2;
      c.quadraticCurveTo(...f.px(...pts[i]), ...f.px(mx, my));
    }
    c.lineTo(...f.px(...pts[pts.length - 1])); c.stroke();
    for (let i = 0; i < pts.length - 1; i++) {
      if (r() < 0.5) continue;
      branch(f, pts[i][0], pts[i][1], -Math.PI / 2 + (r() - 0.5) * 1.6, 0.8 + r() * 1.6, width * 0.4, seed * 5 + i, 1, color);
    }
  }

  // Корені, що звисають зі стелі кімнати.
  function hangingRoots(f, rect, top, color, seed, density) {
    const r = N.rng(seed);
    for (let i = 0; i < rect.w * density; i++) {
      const x = rect.x + r() * rect.w, len = 1 + r() * Math.min(5, rect.h * 0.35);
      const pts = [[x, top + 0.5]];
      for (let y = top; y > top - len; y -= 0.8) pts.push([x + (r() - 0.5) * 0.7, y]);
      rootLine(f, pts, 0.06 + r() * 0.14, color, seed * 3 + i);
    }
  }

  // Руїна абатства без даху: стрілчасті вікна вирізані, крізь них — небо (шар прозорий).
  function abbey(f, x0, x1, base, top, wall, seed) {
    const c = f.c, r = N.rng(seed);
    c.save();
    c.beginPath(); c.moveTo(...f.px(x0, base));
    for (let x = x0; x <= x1; x += 0.4) {
      const y = top - Math.pow(N.fbm(x * 0.5, 0, seed, 3), 2) * 4.5 - (x > (x0 + x1) / 2 ? (x - (x0 + x1) / 2) * 0.35 : 0);
      c.lineTo(...f.px(x, y));
    }
    c.lineTo(...f.px(x1, base)); c.closePath();
    c.fillStyle = wall; c.fill();
    c.globalCompositeOperation = "destination-out";
    const windows = Math.max(2, Math.round((x1 - x0) / 3));
    const span = (x1 - x0) / windows;
    for (let i = 0; i < windows; i++) {
      c.beginPath(); f.pointedArch(x0 + span * (i + 0.5), span * 0.55, base + 1.2, (top - base) * (0.5 + r() * 0.12)); c.fill();
    }
    c.restore();
  }

  function moon(f, x, y, rad, color) {
    const c = f.c, [px, py] = f.px(x, y);
    f.glow(x, y, rad * 9, color, 0.2);
    f.glow(x, y, rad * 3, color, 0.32);
    c.fillStyle = B.rgba(color, 0.92);
    c.beginPath(); c.arc(px, py, rad * f.S, 0, Math.PI * 2); c.fill();
  }

  function fungus(f, x, y, size, look) {
    const c = f.c, S = f.S, [px, py] = f.px(x, y);
    f.glow(x, y + size * 0.3, size * 3.5, look.fungusGlow, 0.4);
    c.fillStyle = look.fungus;
    c.fillRect(px - S * size * 0.08, py - S * size * 0.5, S * size * 0.16, S * size * 0.5);
    c.beginPath(); c.ellipse(px, py - S * size * 0.5, S * size * 0.4, S * size * 0.22, 0, Math.PI, 0); c.fill();
  }

  // ---------- шари ----------

  function paintMid(f, zone, room, look, rect, seed) {
    const k = kind(zone, room, look, rect), r = N.rng(seed);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "cave" || k === "secret") {
      f.sky(look.caveTop, look.caveBottom);
      for (let i = 0; i < rect.w / 2.5; i++) {
        const x = f.X0 + r() * f.W;
        rootLine(f, [[x, top + 4], [x + (r() - 0.5) * 3, top - rect.h * 0.3], [x + (r() - 0.5) * 4, floor + rect.h * 0.2],
          [x + (r() - 0.5) * 3, rect.y - 3]], 0.35 + r() * 0.4, look.caveRoot, seed + i);
      }
      f.fog(rect.y - 3, top, look.caveFog, 0.45, seed);
      return;
    }
    if (k === "thicket") {
      f.sky(look.thicketTop, look.thicketBottom);
      treeRow(f, { ground: floor - 1, height: Math.max(10, rect.h + 3), width: 0.45, gap: 2.2, color: look.farTree, seed });
      f.fog(floor - 1, top, look.fog, 0.5, seed + 1);
      treeRow(f, { ground: floor - 1, height: Math.max(10, rect.h + 4), width: 0.7, gap: 3.4, color: look.midTree, seed: seed + 2 });
      f.fog(floor - 1, floor + 5, look.fog, 0.35, seed + 3);
      return;
    }
    // відкрито — небо
    f.sky(look.skyTop, look.skyBottom);
    const arena = k === "arena";
    if (arena || r() < 0.3) moon(f, rect.x + rect.w * (0.25 + r() * 0.5), Math.min(top + 1, floor + 14 + r() * 3), arena ? 1.6 : 1, look.moon);
    treeRow(f, { ground: floor - 0.5, height: 9 + r() * 3, width: 0.32, gap: 3, color: look.farTree, seed });
    if (!arena && r() < 0.45) {
      const w = Math.min(rect.w * 0.5, 9 + r() * 6), x0 = rect.x + r() * Math.max(1, rect.w - w);
      abbey(f, x0, x0 + w, floor - 0.5, floor + 9 + r() * 3, look.ruin, seed + 4);
    }
    f.fog(floor - 1, floor + 8, look.fog, 0.55, seed + 5);
    treeRow(f, { ground: floor - 1, height: 11 + r() * 3, width: 0.6, gap: 4.4, color: look.midTree, seed: seed + 6 });
    f.fog(floor - 1, floor + 4, look.fogLow, 0.4, seed + 7);
  }

  function paintNear(f, zone, room, look, rect, seed) {
    const k = kind(zone, room, look, rect), r = N.rng(seed + 100);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "cave" || k === "secret") {
      hangingRoots(f, rect, top, look.nearRoot, seed + 11, k === "secret" ? 0.5 : 0.3);
      f.rocks(floor - 0.3, 0.7 + r() * 0.8, look.caveRock, seed + 12, false);
      const fungi = Math.max(2, Math.round(rect.w / 6));
      for (let i = 0; i < fungi; i++) fungus(f, rect.x + 1.5 + r() * (rect.w - 3), floor + r() * 0.2, 0.45 + r() * 0.35, look);
      if (k === "secret") f.glow(rect.x + rect.w / 2, floor + 1, 3.5, look.secretGlow, 0.2);
      else if (look.poison.indexOf(room.id) >= 0) f.glow(rect.x + rect.w / 2, floor, Math.min(10, rect.w * 0.4), look.poisonGlow, 0.24);
      if (rect.h > 9 && k !== "secret") f.beam(rect.x + rect.w * (0.3 + r() * 0.4), top - 1, (r() - 0.5) * 0.2, 1.2, rect.h * 0.9, look.beam, 0.14, seed);
      f.dust(rect.w * rect.h / 6, look.caveDust, seed + 13);
      return;
    }
    if (k === "thicket") {
      // хащі: стовбури по краях, корені зі стелі, подекуди обламана аркада руїни
      [rect.x + 0.6 + r() * 1.2, rect.x + rect.w - 0.6 - r() * 1.2].forEach((x, i) =>
        deadOak(f, x, floor - 0.6, rect.h + 2, 0.9 + r() * 0.4, look.nearTree, seed + 20 + i));
      hangingRoots(f, rect, top, look.nearRoot, seed + 21, 0.18);
      if (r() < 0.5 && rect.w > 12) {
        abbey(f, rect.x + rect.w * 0.2, rect.x + rect.w * (0.55 + r() * 0.25), floor - 0.6, Math.min(top - 1, floor + 8), look.nearRuin, seed + 22);
      }
      f.beam(rect.x + rect.w * (0.35 + r() * 0.3), top, (r() - 0.5) * 0.3, 1.2, top - floor + 2, look.beam, 0.14, seed + 23);
      f.dust(rect.w * rect.h / 6, look.dust, seed + 24);
      return;
    }
    // відкрито: стовбури на краях кадру, ближній ряд рідкий — крізь нього видно далечінь
    const trunks = Math.max(1, Math.round(rect.w / 14));
    for (let i = 0; i < trunks; i++) {
      const x = rect.x + rect.w * ((i + 0.15 + r() * 0.7) / trunks);
      deadOak(f, x, floor - 0.6, Math.max(9, top - floor + 1), 0.8 + r() * 0.5, look.nearTree, seed + 30 + i);
    }
    const beams = k === "arena" ? 3 : rect.w > 22 ? 2 : 1;
    for (let i = 0; i < beams; i++) {
      f.beam(rect.x + rect.w * ((i + 0.3 + r() * 0.4) / beams), top + 1, 0.15 + r() * 0.2, 1.4 + r() * 1.2, top - floor + 3,
        look.beam, 0.16 + r() * 0.08, seed + i);
    }
    f.dust(rect.w * rect.h / 6, look.dust, seed + 31);
  }

  root.LevelBackdropForest = { paintMid, paintNear, kind };
})(window);
