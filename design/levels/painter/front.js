// Передній план кімнати (front, 2026-10-27; рішення користувача: «вайб, атмосфера» як у Hollow Knight,
// не копіюючи): темні силуети, що звисають зі стелі кадру — ліани з листям, ланцюги, коріння,
// сталактити, — і кілька розмитих світлих плям-боке. Шар стоїть перед грою й зсувається швидше за
// камеру (RoomParallax з від'ємним коефіцієнтом), тому дає глибину. Лише вгорі кімнати: силует ніколи не
// лягає на лінію бою (VISUAL_BIBLE §14). Кожна кімната — свій seed, жодного повтору.
(function (root) {
  "use strict";
  const N = root.PaintNoise;
  const MARGIN = 2;

  function rgba(h, a) {
    const r = parseInt(h.slice(1, 3), 16), g = parseInt(h.slice(3, 5), 16), b = parseInt(h.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${a})`;
  }

  // Що звисає в кімнаті цього характеру, з якою вагою.
  const KINDS = {
    hall: { vine: 3, chain: 2, root: 1, drip: 0 },
    arena: { vine: 1, chain: 4, root: 0, drip: 0 },
    shaft: { vine: 1, chain: 3, root: 2, drip: 1 },
    cave: { vine: 2, chain: 0, root: 3, drip: 3 },
    secret: { vine: 1, chain: 0, root: 2, drip: 2 }
  };

  function pick(weights, r) {
    let total = 0;
    for (const k in weights) total += weights[k];
    let x = r() * total;
    for (const k in weights) { x -= weights[k]; if (x <= 0) return k; }
    return "vine";
  }

  // Ліана: гнучкий стебель, що звужується донизу, з листям по обидва боки й кетягом на кінці.
  function vine(f, x, top, len, r, color) {
    const c = f.c, S = f.S, sway = (r() - 0.5) * 1.2, steps = Math.max(6, Math.round(len * 6));
    const pts = [];
    for (let i = 0; i <= steps; i++) {
      const t = i / steps;
      pts.push([x + Math.sin(t * Math.PI * (0.8 + r() * 0.2)) * sway * t + (r() - 0.5) * 0.05, top - t * len]);
    }
    c.strokeStyle = color; c.lineCap = "round";
    for (let i = 0; i < steps; i++) {
      const [ax, ay] = f.px(...pts[i]), [bx, by] = f.px(...pts[i + 1]);
      c.lineWidth = Math.max(1.5, S * (0.13 * (1 - i / steps) + 0.035));
      c.beginPath(); c.moveTo(ax, ay); c.lineTo(bx, by); c.stroke();
    }
    c.fillStyle = color;
    for (let i = 1; i < steps; i += 1 + Math.floor(r() * 1.6)) {
      const [px, py] = f.px(...pts[i]), side = i % 2 ? 1 : -1, size = S * (0.2 + r() * 0.16) * (1 - i / steps * 0.45);
      c.save(); c.translate(px, py); c.rotate(side * (0.5 + r() * 0.6));
      c.beginPath(); c.moveTo(0, 0);
      c.quadraticCurveTo(side * size * 0.9, -size * 0.5, side * size * 2, size * 0.15);
      c.quadraticCurveTo(side * size * 0.9, size * 0.45, 0, 0); c.fill();
      c.restore();
    }
    // кінчик — два-три видовжені листки, що звисають (не кулі: вони читалися як чорні ягоди)
    const [ex, ey] = f.px(...pts[steps]);
    for (let k = 0; k < 3; k++) {
      const a = (k - 1) * 0.5 + (r() - 0.5) * 0.3, size = S * (0.22 + r() * 0.12);
      c.save(); c.translate(ex, ey); c.rotate(a);
      c.beginPath(); c.moveTo(0, 0);
      c.quadraticCurveTo(size * 0.35, size * 0.9, 0, size * 2.1);
      c.quadraticCurveTo(-size * 0.35, size * 0.9, 0, 0); c.fill();
      c.restore();
    }
  }

  // Ланцюг: ланки по черзі анфас і в профіль, наприкінці — гак або обірвана ланка.
  function chain(f, x, top, len, r, color) {
    const c = f.c, S = f.S, step = 0.26;
    c.strokeStyle = color; c.fillStyle = color;
    let y = top, k = 0;
    for (; y > top - len; y -= step, k++) {
      const [px, py] = f.px(x + Math.sin(k * 0.3) * 0.02, y);
      c.lineWidth = Math.max(2, S * 0.08);
      c.beginPath();
      if (k % 2) c.ellipse(px, py, S * 0.04, S * 0.14, 0, 0, Math.PI * 2);
      else c.ellipse(px, py, S * 0.1, S * 0.15, 0, 0, Math.PI * 2);
      c.stroke();
    }
    if (r() < 0.5) {
      const [px, py] = f.px(x, y);
      c.lineWidth = Math.max(2, S * 0.08);
      c.beginPath(); c.arc(px + S * 0.12, py, S * 0.14, Math.PI * 0.1, Math.PI * 1.1); c.stroke();
    }
  }

  // Корінь: товстий у стелі, гілкується й скручується донизу.
  function rootStrand(f, x, top, len, r, color, depth) {
    const c = f.c, S = f.S, steps = Math.max(5, Math.round(len * 5));
    let px = x, py = top, angle = -Math.PI / 2 + (r() - 0.5) * 0.5;
    c.strokeStyle = color; c.lineCap = "round";
    for (let i = 0; i < steps; i++) {
      const t = i / steps, seg = len / steps;
      angle += (r() - 0.5) * 0.6;
      angle = Math.max(-Math.PI * 0.85, Math.min(-Math.PI * 0.15, angle));
      const nx = px + Math.cos(angle) * seg, ny = py + Math.sin(angle) * seg;
      const [ax, ay] = f.px(px, py), [bx, by] = f.px(nx, ny);
      c.lineWidth = Math.max(1.5, S * (0.24 - depth * 0.07) * (1 - t * 0.75));
      c.beginPath(); c.moveTo(ax, ay); c.lineTo(bx, by); c.stroke();
      if (depth < 2 && r() < 0.18) rootStrand(f, nx, ny, len * (1 - t) * 0.6, r, color, depth + 1);
      px = nx; py = ny;
    }
  }

  // Сталактит: звислий камінь з опуклими боками, що плавно сходяться в краплю, трохи вигнутий
  // (не трикутник — прямі боки читалися як вирізаний папір).
  function drip(f, x, top, len, r, color) {
    const c = f.c, w = 0.18 + r() * 0.3, bend = (r() - 0.5) * 0.35;
    const P = (dx, t) => f.px(x + dx + bend * t * t, top - len * t);
    c.fillStyle = color;
    c.beginPath();
    c.moveTo(...P(-w, -0.1));
    c.bezierCurveTo(...P(-w * 1.05, 0.35), ...P(-w * 0.45, 0.75), ...P(0, 1));
    c.bezierCurveTo(...P(w * 0.45, 0.75), ...P(w * 1.05, 0.35), ...P(w, -0.1));
    c.closePath(); c.fill();
    // крапля на кінчику
    const [dx, dy] = P(0, 1.08);
    c.beginPath(); c.ellipse(dx, dy, f.S * 0.035, f.S * 0.055, 0, 0, Math.PI * 2); c.fill();
  }

  // Розмита світла пляма: вогник, що пролітає ближче до ока, ніж фокус.
  function bokeh(f, x, y, radius, color, alpha) {
    const c = f.c, [px, py] = f.px(x, y), R = radius * f.S;
    const g = c.createRadialGradient(px, py, 0, px, py, R);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(0.55, rgba(color, alpha * 0.55)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g; c.beginPath(); c.arc(px, py, R, 0, Math.PI * 2); c.fill();
  }

  function paintFront(canvas, zone, room, ppu, look) {
    const B = root.LevelBackdrop, rect = B.roomRect(zone, room), seed = N.seedOf(room.id + "/front");
    canvas.width = Math.round((rect.w + 2 * MARGIN) * ppu);
    canvas.height = Math.round((rect.h + 2 * MARGIN) * ppu);
    const f = new B.Frame(canvas, rect.x - MARGIN, rect.y - MARGIN, ppu);
    const r = N.rng(seed), kind = B.kindOf(room, look), weights = KINDS[kind] || KINDS.hall;
    const color = look.front || "#05060a";
    // Стеля над кожним стовпчиком кімнати: найнижча клітинка верхнього каменю. Там, де стелі немає
    // (шахта відкрита вгору) або стовпчик увесь камінь, нічого не звисає.
    const cell = zone.cell, ceilings = [];
    for (let x = 0; x < room.w; x++) {
      let y = room.h - 1;
      if (root.LevelText.isAir(room.rows[y][x])) { ceilings.push(null); continue; }
      while (y >= 0 && !root.LevelText.isAir(room.rows[y][x])) y--;
      ceilings.push(y < 0 ? null : zone.origin[1] + (room.y + y + 1) * cell);
    }
    const ceilingAt = wx => {
      const i = Math.floor((wx - rect.x) / cell);
      return i < 0 || i >= ceilings.length ? null : ceilings[i];
    };
    // Довжина — до чверті кімнати, не більше 4 юнітів: силует тримається над лінією бою.
    const maxLen = Math.min(4, Math.max(1.2, rect.h * 0.25));
    const c = f.c;
    c.save();
    c.globalAlpha = 0.9;
    // передній план поза фокусом: ледь розмитий, як у HK (різкий чорний читався аплікацією)
    c.filter = `blur(${Math.max(1, ppu * 0.035).toFixed(1)}px)`;
    // Кетяги: густо там, де шум високий, і порожньо між ними — як справжні зарості, не гребінка.
    for (let x = rect.x + 0.3; x < rect.x + rect.w - 0.3; x += 0.28 + r() * 0.4) {
      const density = N.fbm(x * 0.16, 1.7, seed, 3), ceiling = ceilingAt(x);
      if (density < 0.5 || ceiling === null) continue;
      const what = pick(weights, r);
      const len = maxLen * (0.3 + r() * 0.7) * (0.55 + density * 0.7), from = ceiling + 0.15;
      if (what === "vine") vine(f, x, from, len, r, color);
      else if (what === "chain") chain(f, x, from, len, r, color);
      else if (what === "root") rootStrand(f, x, from, len, r, color, 0);
      else drip(f, x, from, Math.min(len, 2.2), r, color);
      // під стелею — плаский наплив моху, з якого все звисає (притиснутий до каменю, не кулька)
      if (r() < 0.35) {
        const [mx, my] = f.px(x, ceiling + 0.05);
        c.fillStyle = color;
        c.beginPath(); c.ellipse(mx, my, f.S * (0.22 + r() * 0.2), f.S * (0.04 + r() * 0.03), 0, 0, Math.PI * 2); c.fill();
      }
    }
    c.restore();
    // Боке: кілька теплих чи холодних вогників упереді, м'яко розмитих.
    const glow = kind === "arena" ? (look.arenaGlow || "#c9d6ff") : kind === "secret" ? (look.secretGlow || "#d9b45a")
      : kind === "cave" ? (look.fungus || "#6fd0c4") : (look.dust || "#c9d6e8");
    const count = 2 + Math.floor(r() * 4) + Math.floor(rect.w / 18);
    for (let i = 0; i < count; i++) {
      bokeh(f, rect.x + r() * rect.w, rect.y + rect.h * (0.35 + r() * 0.6), 0.35 + r() * 0.9, glow, 0.05 + r() * 0.09);
    }
  }

  root.LevelFront = { paintFront, MARGIN };
})(window);
