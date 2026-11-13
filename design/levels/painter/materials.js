// Матеріали уступів «=» (2026-10-28, користувач: «текстури виглядають дешево; не всюди камінь — мости,
// дерево»; решено Claude за постійною згодою). Уступ малюється тим, чим він є в цьому місці:
// біля стіни — полиця з підкосом (дерево) / гілка (Ліс) / балкон на консолі (Столиця); довгий вільний —
// міст із дощок на мотузках чи ланцюгах; короткий вільний — підвішений настил, колода або кам'яна плита.
// Форми чисті, з темним контуром і світлим верхом, як у намальованій грі, а не фото-фактура.
(function (root) {
  "use strict";
  const N = root.PaintNoise;

  function rgba(h, a) {
    const r = parseInt(h.slice(1, 3), 16), g = parseInt(h.slice(3, 5), 16), b = parseInt(h.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${a})`;
  }

  // Палітри дерева й заліза зон.
  const WOOD = {
    tomb: { wood: "#3a3029", light: "#6e5a46", dark: "#17110d", iron: "#2b3038", rope: "#2e333c" },
    forest: { wood: "#2f2a20", light: "#5e5a40", dark: "#121008", iron: "#1c201a", rope: "#1a2219", bark: "#2a2a1f", leaf: "#2f4a33" },
    village: { wood: "#4a3424", light: "#86643f", dark: "#1c120b", iron: "#2a2018", rope: "#5a4630" },
    capital: { wood: "#3a2c24", light: "#6c5444", dark: "#150f0c", iron: "#1b1615", rope: "#2a201c", stone: "#4a4442", stoneLight: "#8a8078", stoneDark: "#1c1918" }
  };

  // Яким бути уступу: що каже місце і зона.
  function kindOf(zoneName, attached, width, seed) {
    const r = N.hash(seed, 3, 17);
    // Камінь — брила (chunk): Гробниця, Столиця, Болота (style forest з look.hang roots, але свій kind нижче).
    if (zoneName === "tomb" || (zoneName === "capital" && !attached && width < 8)) return "chunk";
    if (attached) {
      if (zoneName === "forest") return "branch";
      // Гробниця — камінь, як її стіни (2026-11-04, «бездарно, як у Hollow Knight»): дерев'яна полиця була
      // чужим матеріалом і читалась заглушкою; виступ стіни з кантом — одне ціле з каменем.
      if (zoneName === "tomb") return "outcrop";
      if (zoneName === "capital") return r < 0.6 ? "balcony" : "shelf";
      return "shelf";
    }
    if (zoneName === "tomb") return "slab"; // і вільні — кам'яні плити на ланцюгах, не дерев'яний настил
    if (width >= 8) return "bridge";
    if (zoneName === "forest") return r < 0.55 ? "log" : "hanging";
    if (zoneName === "tomb") return r < 0.5 ? "slab" : "hanging";
    if (zoneName === "capital") return r < 0.5 ? "slab" : "hanging";
    return "hanging";
  }

  // Дошки настилу: верхня широка, під нею тонша, шви, цвяхи, рвані кінці; світла кромка зверху.
  function planks(f, x0, x1, top, pal, seed, thick) {
    const c = f.c, S = f.ppu, t = (thick || 0.3) * 1.45; // 2026-11-12: товщі — тонкі дошки читались заглушкою
    const y0 = f.y(top), h = S * t;
    // тінь під настилом
    c.fillStyle = "rgba(0,0,0,0.35)";
    c.fillRect(f.x(x0) + S * 0.05, y0 + h, f.x(x1) - f.x(x0) - S * 0.1, S * 0.06);
    // дошки: окремі, кожна своєї довжини й тону
    let x = x0;
    let k = 0;
    while (x < x1 - 0.05) {
      const len = Math.min(x1 - x, 0.9 + N.hash(seed, k, 5) * 1.1);
      const tone = N.hash(seed, k, 7);
      const ax = f.x(x) + (k ? S * 0.025 : 0), bx = f.x(x + len) - S * 0.025;
      const g = c.createLinearGradient(0, y0, 0, y0 + h);
      g.addColorStop(0, tone > 0.5 ? pal.light : pal.wood);
      g.addColorStop(0.35, pal.wood);
      g.addColorStop(1, pal.dark);
      c.fillStyle = g;
      c.beginPath();
      c.moveTo(ax, y0);
      c.lineTo(bx, y0);
      c.lineTo(bx - S * 0.02, y0 + h * (0.85 + N.hash(seed, k, 9) * 0.15));
      c.lineTo(ax + S * 0.015, y0 + h);
      c.closePath(); c.fill();
      // волокна
      c.strokeStyle = rgba(pal.dark, 0.5); c.lineWidth = Math.max(1, S * 0.015);
      for (let j = 1; j <= 2; j++) {
        const yy = y0 + h * (0.3 + j * 0.22 + (N.hash(seed, k, 11 + j) - 0.5) * 0.1);
        c.beginPath(); c.moveTo(ax + S * 0.08, yy); c.quadraticCurveTo((ax + bx) / 2, yy + S * 0.02, bx - S * 0.08, yy); c.stroke();
      }
      // цвяхи на кінцях
      c.fillStyle = rgba(pal.iron, 0.9);
      [ax + S * 0.09, bx - S * 0.09].forEach(nx => { c.beginPath(); c.arc(nx, y0 + h * 0.45, Math.max(1, S * 0.025), 0, Math.PI * 2); c.fill(); });
      x += len; k++;
    }
    // світла кромка й контур
    c.strokeStyle = rgba(pal.light, 0.85); c.lineWidth = Math.max(1.5, S * 0.03);
    c.beginPath(); c.moveTo(f.x(x0) + 1, y0 + 1); c.lineTo(f.x(x1) - 1, y0 + 1); c.stroke();
    c.strokeStyle = rgba(pal.dark, 0.9); c.lineWidth = Math.max(1, S * 0.02);
    c.strokeRect(f.x(x0), y0, f.x(x1) - f.x(x0), h);
  }

  function rope(f, x, yFrom, yTo, pal, seed, kind) {
    const c = f.c, S = f.ppu;
    if (kind === "chain") {
      c.strokeStyle = rgba(pal.iron, 0.95); c.lineWidth = S * 0.045;
      for (let yy = yTo; yy < yFrom; yy += 0.22) {
        c.beginPath(); c.ellipse(f.x(x), f.y(yy + 0.11), S * 0.05, S * 0.1, 0, 0, Math.PI * 2); c.stroke();
      }
      return;
    }
    c.strokeStyle = rgba(kind === "root" ? pal.bark || pal.rope : pal.rope, 0.95);
    c.lineWidth = S * (kind === "root" ? 0.09 : 0.045); c.lineCap = "round";
    c.beginPath(); c.moveTo(f.x(x), f.y(yFrom));
    for (let yy = yFrom; yy > yTo; yy -= 0.4) {
      const sway = kind === "root" ? (N.fbm(x, yy * 0.7, seed, 2) - 0.5) * 0.35 : 0;
      c.lineTo(f.x(x + sway), f.y(yy));
    }
    c.lineTo(f.x(x), f.y(yTo)); c.stroke();
    if (kind !== "root") {
      // вузол на дошці
      c.fillStyle = rgba(pal.rope, 1);
      c.beginPath(); c.ellipse(f.x(x), f.y(yTo) - S * 0.03, S * 0.06, S * 0.05, 0, 0, Math.PI * 2); c.fill();
    }
  }

  // Полиця біля стіни: дошки і підкіс від стіни під кутом.
  function shelf(f, x0, x1, top, wallSide, pal, seed) {
    const c = f.c, S = f.ppu;
    const wx = wallSide < 0 ? x0 : x1, reach = Math.min(1.4, (x1 - x0) * 0.6);
    // підкіс: брус від стіни нижче до середини полиці
    c.strokeStyle = pal.dark; c.lineWidth = S * 0.16; c.lineCap = "butt";
    c.beginPath(); c.moveTo(f.x(wx), f.y(top - reach)); c.lineTo(f.x(wx - wallSide * reach), f.y(top - 0.28)); c.stroke();
    c.strokeStyle = pal.wood; c.lineWidth = S * 0.1;
    c.beginPath(); c.moveTo(f.x(wx), f.y(top - reach)); c.lineTo(f.x(wx - wallSide * reach), f.y(top - 0.28)); c.stroke();
    // залізний кутник у стіні
    c.fillStyle = pal.iron;
    c.fillRect(f.x(wx) - (wallSide < 0 ? 0 : S * 0.12), f.y(top - reach) - S * 0.08, S * 0.12, S * 0.16);
    planks(f, x0, x1, top, pal, seed, 0.28);
  }

  // Міст: настил, під ним поперечини, з обох кінців і посередині — підвіс до склепіння.
  function bridge(f, x0, x1, top, ceilings, pal, seed, hangKind) {
    const c = f.c, S = f.ppu;
    // поперечини під настилом
    for (let x = x0 + 0.4; x < x1 - 0.2; x += 1.6) {
      c.fillStyle = pal.dark; c.fillRect(f.x(x), f.y(top - 0.42), S * 0.26, S * 0.3);
    }
    ceilings.forEach(([x, up]) => { if (up !== null) rope(f, x, up, top, pal, seed + Math.round(x * 7), hangKind); });
    planks(f, x0, x1, top, pal, seed, 0.3);
  }

  // Колода: заокруглена, кора смугами, торці з кільцями.
  function log(f, x0, x1, top, pal, seed) {
    const c = f.c, S = f.ppu, h = 0.66, y0 = f.y(top), y1 = f.y(top - h);
    const g = c.createLinearGradient(0, y0, 0, y1);
    g.addColorStop(0, pal.light); g.addColorStop(0.3, pal.bark || pal.wood); g.addColorStop(1, pal.dark);
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(f.x(x0) + S * 0.1, y0);
    c.lineTo(f.x(x1) - S * 0.1, y0);
    c.quadraticCurveTo(f.x(x1) + S * 0.04, (y0 + y1) / 2, f.x(x1) - S * 0.1, y1);
    c.lineTo(f.x(x0) + S * 0.1, y1);
    c.quadraticCurveTo(f.x(x0) - S * 0.04, (y0 + y1) / 2, f.x(x0) + S * 0.1, y0);
    c.fill();
    c.strokeStyle = rgba(pal.dark, 0.6); c.lineWidth = Math.max(1, S * 0.02);
    for (let x = x0 + 0.3; x < x1 - 0.3; x += 0.35 + N.hash(seed, Math.round(x * 10), 3) * 0.4) {
      c.beginPath(); c.moveTo(f.x(x), y0 + S * 0.06); c.quadraticCurveTo(f.x(x + 0.12), (y0 + y1) / 2, f.x(x + 0.05), y1 - S * 0.03); c.stroke();
    }
    // торці
    [x0, x1].forEach((ex, k) => {
      const cx = f.x(ex) + (k ? -S * 0.12 : S * 0.12), cy = (y0 + y1) / 2;
      c.fillStyle = pal.light; c.beginPath(); c.ellipse(cx, cy, S * 0.11, (y1 - y0) * 0.48, 0, 0, Math.PI * 2); c.fill();
      c.strokeStyle = rgba(pal.dark, 0.7); c.lineWidth = Math.max(1, S * 0.015);
      [0.65, 0.35].forEach(q => { c.beginPath(); c.ellipse(cx, cy, S * 0.11 * q, (y1 - y0) * 0.48 * q, 0, 0, Math.PI * 2); c.stroke(); });
    });
    // мох на колоді
    c.fillStyle = rgba(pal.leaf || "#2f4a33", 0.85);
    for (let x = x0 + 0.2; x < x1 - 0.2; x += 0.3) {
      const h2 = N.hash(Math.round(x * 13), seed, 5);
      if (h2 < 0.5) continue;
      c.beginPath(); c.ellipse(f.x(x), y0 + 1, S * (0.12 + h2 * 0.15), S * 0.05, 0, Math.PI, 0); c.fill();
    }
  }

  // Гілка з дерева, що росте зі стіни чи стовбура (2026-11-13, користувач: «наче дитина гралася геометричними
  // фігурами»; решено Claude за постійною згодою): не клин, а сук — майже рівної товщини, з плавним наростом біля
  // стовбура, вузлуватим низом, обламаним кінцем, з якого вгору йде тонка гілочка з листям, і пасмами моху знизу.
  function branch(f, x0, x1, top, wallSide, pal, seed) {
    const c = f.c, S = f.ppu, X = f.x, Y = f.y;
    const from = wallSide < 0 ? x0 - 0.3 : x1 + 0.3, to = wallSide < 0 ? x1 + 0.15 : x0 - 0.15;
    const len = Math.abs(to - from), dir = to > from ? 1 : -1;
    const at = u => from + (to - from) * u;
    // товщина вздовж суку: наріст біля стовбура, далі рівно, до кінця тоншає; вузли — шум
    const thick = u => 0.2 + 0.26 * (1 - u) + 0.55 * Math.pow(1 - u, 5) + (N.fbm(u * len * 1.7, top, seed, 2) - 0.5) * 0.12 * (1 - u * 0.5);
    const lift = u => -0.05 * Math.sin(u * Math.PI); // низ ледь вигнутий
    c.save();
    const g = c.createLinearGradient(0, Y(top), 0, Y(top - 0.75));
    g.addColorStop(0, pal.light); g.addColorStop(0.22, pal.bark); g.addColorStop(1, pal.dark);
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(X(from), Y(top));
    c.lineTo(X(to - dir * 0.12), Y(top));
    c.quadraticCurveTo(X(to + dir * 0.06), Y(top - 0.02), X(to + dir * 0.02), Y(top - 0.13)); // обламаний кінець
    c.lineTo(X(to - dir * 0.05), Y(top - thick(1)));
    for (let i = 19; i >= 0; i--) { const u = i / 20; c.lineTo(X(at(u)), Y(top - thick(u) + lift(u))); }
    c.closePath(); c.fill();
    c.strokeStyle = rgba(pal.dark, 0.85); c.lineWidth = Math.max(1, S * 0.025); c.stroke();
    // кора: довгі борозни вздовж суку, що сходяться до кінця
    c.strokeStyle = rgba(pal.dark, 0.5); c.lineWidth = Math.max(1, S * 0.018);
    for (let i = 1; i <= 3; i++) {
      const q = i / 4, u0 = N.hash(seed, i, 61) * 0.15, u1 = 0.6 + N.hash(seed, i, 63) * 0.35;
      c.beginPath();
      for (let k = 0; k <= 10; k++) {
        const u = u0 + (u1 - u0) * k / 10;
        c.lineTo(X(at(u)), Y(top - thick(u) * q + lift(u) * q + (N.hash(seed + i, k, 65) - 0.5) * 0.02));
      }
      c.stroke();
    }
    // сучок-око
    const ku = 0.25 + N.hash(seed, 2, 67) * 0.35;
    c.strokeStyle = rgba(pal.dark, 0.6); c.beginPath(); c.ellipse(X(at(ku)), Y(top - thick(ku) * 0.55), S * 0.07, S * 0.045, 0, 0, Math.PI * 2); c.stroke();
    // гілочки: одна з кінця вгору, одна-дві знизу; на кінцях листя купками
    const leafTuft = (x, y, n, k0) => {
      for (let j = 0; j < n; j++) {
        const lx = x + (N.hash(seed + k0, j, 71) - 0.5) * 0.55, ly = y + (N.hash(seed + k0, j, 73) - 0.4) * 0.4;
        c.fillStyle = rgba(pal.leaf, 0.55 + N.hash(seed + k0, j, 75) * 0.4);
        c.save(); c.translate(X(lx), Y(ly)); c.rotate((N.hash(seed + k0, j, 77) - 0.5) * 2.6);
        c.beginPath(); c.moveTo(-S * 0.13, 0); c.quadraticCurveTo(0, -S * 0.07, S * 0.13, 0); c.quadraticCurveTo(0, S * 0.07, -S * 0.13, 0); c.fill();
        c.restore();
      }
    };
    const twig = (x, y, a, l, w, k0) => {
      const mx = x + Math.cos(a) * l * 0.5 + dir * 0.08, my = y + Math.sin(a) * l * 0.55;
      const ex = x + Math.cos(a) * l, ey = y + Math.sin(a) * l;
      c.strokeStyle = pal.dark; c.lineWidth = Math.max(1, S * w); c.lineCap = "round";
      c.beginPath(); c.moveTo(X(x), Y(y)); c.quadraticCurveTo(X(mx), Y(my), X(ex), Y(ey)); c.stroke();
      leafTuft(ex, ey, 6, k0);
    };
    twig(to - dir * 0.08, top - 0.1, dir > 0 ? 0.5 : Math.PI - 0.5, 0.75 + N.hash(seed, 1, 81) * 0.4, 0.05, 11);
    const drops = 1 + (N.hash(seed, 3, 83) < 0.5 ? 1 : 0);
    for (let k = 0; k < drops; k++) {
      const u = 0.35 + k * 0.3 + N.hash(seed, k, 85) * 0.15;
      twig(at(u), top - thick(u) + 0.03, (dir > 0 ? -0.6 : Math.PI + 0.6) - (N.hash(seed, k, 87) - 0.5) * 0.5, 0.45 + N.hash(seed, k, 89) * 0.35, 0.04, 20 + k * 7);
    }
    // мох: подушка по верху (тут стоять) нерівними купинами і пасма, що звисають з краю
    c.fillStyle = rgba(pal.leaf, 0.92);
    c.beginPath(); c.moveTo(X(from), Y(top - 0.05));
    for (let k = 0; k <= 24; k++) {
      const u = k / 24;
      c.lineTo(X(at(u * 0.97)), Y(top + 0.015 + N.fbm(u * len * 3, 7, seed, 2) * 0.05));
    }
    for (let k = 24; k >= 0; k--) {
      const u = k / 24;
      c.lineTo(X(at(u * 0.97)), Y(top - 0.05 - N.fbm(u * len * 2.2, 3, seed, 2) * 0.12));
    }
    c.closePath(); c.fill();
    c.strokeStyle = rgba(pal.leaf, 0.7); c.lineWidth = Math.max(1, S * 0.022); c.lineCap = "round";
    for (let k = 0; k < len * 2.2; k++) {
      const u = N.hash(seed, k, 91); if (u < 0.12) continue;
      const hx = at(u), d = 0.12 + N.hash(seed, k, 93) * 0.3;
      c.beginPath(); c.moveTo(X(hx), Y(top - 0.06)); c.quadraticCurveTo(X(hx + 0.03), Y(top - 0.06 - d * 0.6), X(hx + (N.hash(seed, k, 95) - 0.5) * 0.08), Y(top - 0.06 - d)); c.stroke();
    }
    // світло зверху
    c.strokeStyle = "rgba(190,215,190,0.22)"; c.lineWidth = Math.max(1, S * 0.02);
    c.beginPath(); c.moveTo(X(at(0.05)), Y(top + 0.02)); c.lineTo(X(at(0.95)), Y(top + 0.02)); c.stroke();
    c.restore();
  }

  // Кам'яний балкон на фігурній консолі (Столиця).
  function balcony(f, x0, x1, top, wallSide, pal, seed) {
    const c = f.c, S = f.ppu;
    const y0 = f.y(top), h = S * 0.48;
    const g = c.createLinearGradient(0, y0, 0, y0 + h);
    g.addColorStop(0, pal.stoneLight); g.addColorStop(0.3, pal.stone); g.addColorStop(1, pal.stoneDark);
    c.fillStyle = g; c.fillRect(f.x(x0), y0, f.x(x1) - f.x(x0), h);
    c.strokeStyle = rgba(pal.stoneDark, 0.9); c.lineWidth = Math.max(1, S * 0.02);
    c.strokeRect(f.x(x0), y0, f.x(x1) - f.x(x0), h);
    c.beginPath(); c.moveTo(f.x(x0), y0 + h * 0.4); c.lineTo(f.x(x1), y0 + h * 0.4); c.stroke();
    // консолі — сходинки, що звужуються до стіни
    const wx = wallSide < 0 ? x0 : x1;
    for (let i = 0; i < 3; i++) {
      const w = 0.9 - i * 0.28, yy = top - 0.32 - i * 0.2;
      const ax = wallSide < 0 ? f.x(wx) : f.x(wx - w), bx = wallSide < 0 ? f.x(wx + w) : f.x(wx);
      c.fillStyle = i % 2 ? pal.stone : pal.stoneDark; c.fillRect(ax, f.y(yy), bx - ax, S * 0.2);
    }
    c.strokeStyle = rgba(pal.stoneLight, 0.8); c.lineWidth = Math.max(1.5, S * 0.03);
    c.beginPath(); c.moveTo(f.x(x0) + 1, y0 + 1); c.lineTo(f.x(x1) - 1, y0 + 1); c.stroke();
  }

  // Кам'яна плита на ланцюгах (Гробниця, Столиця): рівний верх, обламаний низ.
  function slab(f, x0, x1, top, look, seed) {
    const c = f.c;
    c.beginPath();
    c.moveTo(f.x(x0), f.y(top));
    c.lineTo(f.x(x1), f.y(top));
    for (let x = x1; x >= x0 - 0.001; x -= 0.12) {
      const u = (x - x0) / Math.max(0.01, x1 - x0);
      const sag = Math.sin(Math.PI * u) * 0.28 + (N.fbm(x * 2.4, top, seed, 3) - 0.5) * 0.18;
      c.lineTo(f.x(x), f.y(top - 0.3 - Math.max(0, sag)));
    }
    c.closePath();
    const g = c.createLinearGradient(0, f.y(top), 0, f.y(top - 0.7));
    g.addColorStop(0, look.stone); g.addColorStop(1, look.core);
    c.fillStyle = g; c.fill();
    c.strokeStyle = "rgba(0,0,0,0.6)"; c.lineWidth = Math.max(1, f.ppu * 0.02); c.stroke();
  }

  // Брила (2026-11-12, користувач: «елементи платформеру дуже прості й не красиві»; решено Claude за постійною
  // згодою): уступ — не тонка плитка, а шматок скелі чи кладки. Верх рівний (там коллайдер), низ — товща маса:
  // у вільної — кіль, найглибший посередині, у прирослої до стіни — клин, найтовщий біля стіни (консоль).
  // depth — товщина під верхом, юніти; її вирішує terrain.js з місця під уступом (прохід не закриває).
  // attach: -1 стіна ліворуч, 1 праворуч, 0 вільна.
  function chunk(f, x0, x1, top, depth, look, seed, attach) {
    const c = f.c, S = f.ppu, w = Math.max(0.2, x1 - x0);
    const ax0 = x0 - (attach < 0 ? 0.12 : 0), ax1 = x1 + (attach > 0 ? 0.12 : 0);
    const under = x => {
      const u = (x - x0) / w;
      let d;
      if (attach < 0) d = depth * (1 - 0.72 * Math.min(1, u) ** 0.8);
      else if (attach > 0) d = depth * (1 - 0.72 * Math.min(1, 1 - u) ** 0.8);
      else d = depth * (0.3 + 0.7 * Math.sin(Math.PI * Math.max(0, Math.min(1, u))) ** 0.7);
      d *= 0.82 + (N.fbm(x * 1.6, top * 0.7, seed, 3) - 0.5) * 0.5 + 0.18;
      // вільний кінець заокруглений: біля краю низ підходить до верху
      const fromFree = Math.min(attach < 0 ? 9 : x - x0, attach > 0 ? 9 : x1 - x);
      d = Math.min(d, 0.22 + fromFree * 1.4);
      return Math.max(0.2, d);
    };
    const pts = [];
    for (let x = ax1; x >= ax0 - 0.001; x -= 0.09) pts.push([x, top - under(Math.max(x0, Math.min(x1, x)))]);
    // гострі краплі-сталактити під масою
    const drips = [];
    for (let k = 0; k < w * 0.9; k++) {
      const h = N.hash(seed, k, 61);
      if (h < 0.55) continue;
      const x = x0 + 0.3 + N.hash(seed, k, 63) * Math.max(0.1, w - 0.6);
      drips.push([x, 0.12 + (h - 0.55) * 1.1, 0.08 + N.hash(seed, k, 65) * 0.1]);
    }
    const body = () => {
      c.beginPath();
      c.moveTo(f.x(ax0), f.y(top));
      c.lineTo(f.x(ax1), f.y(top));
      pts.forEach(([x, y]) => c.lineTo(f.x(x), f.y(y)));
      c.closePath();
    };
    // тінь-ореол під брилою: відокремлює її від тла
    c.save();
    c.shadowColor = "rgba(0,0,0,0.55)"; c.shadowBlur = S * 0.35; c.shadowOffsetY = S * 0.12;
    body();
    const g = c.createLinearGradient(0, f.y(top), 0, f.y(top - depth * 1.1));
    g.addColorStop(0, look.stone); g.addColorStop(0.55, look.core); g.addColorStop(1, look.core);
    c.fillStyle = g; c.fill();
    c.restore();
    // краплі
    c.fillStyle = look.core;
    drips.forEach(([x, len, half]) => {
      const y = top - under(x) + 0.02;
      c.beginPath(); c.moveTo(f.x(x - half), f.y(y)); c.quadraticCurveTo(f.x(x - half * 0.3), f.y(y - len * 0.6), f.x(x), f.y(y - len));
      c.quadraticCurveTo(f.x(x + half * 0.3), f.y(y - len * 0.6), f.x(x + half), f.y(y)); c.fill();
    });
    c.save(); body(); c.clip();
    // пласти: дві-три хвилясті тріщини вздовж маси
    c.strokeStyle = "rgba(0,0,0,0.45)"; c.lineWidth = Math.max(1, S * 0.025);
    for (let i = 1; i <= 3; i++) {
      const yy = top - depth * (0.22 + i * 0.2);
      c.beginPath();
      for (let x = ax0; x <= ax1 + 0.001; x += 0.15) c.lineTo(f.x(x), f.y(yy + (N.fbm(x * 1.3, i * 5, seed + 7, 2) - 0.5) * 0.25));
      c.stroke();
    }
    // вертикальні шви кладки біля верху (камінь тесаний, не глина)
    c.strokeStyle = "rgba(0,0,0,0.35)";
    for (let x = ax0 + 0.5 + N.hash(seed, 1, 71) * 0.6; x < ax1 - 0.3; x += 0.7 + N.hash(seed, Math.round(x * 10), 73) * 0.7) {
      c.beginPath(); c.moveTo(f.x(x), f.y(top - 0.12)); c.lineTo(f.x(x + 0.04), f.y(top - Math.min(depth * 0.5, 0.55))); c.stroke();
    }
    // відблиск світла на верхній третині й мох/сіль на краю
    const rim = c.createLinearGradient(0, f.y(top), 0, f.y(top - depth * 0.45));
    rim.addColorStop(0, rgba(look.rim || "#a9bdd8", 0.22)); rim.addColorStop(1, rgba(look.rim || "#a9bdd8", 0));
    c.fillStyle = rim; c.fillRect(f.x(ax0), f.y(top), f.x(ax1) - f.x(ax0), S * depth * 0.45);
    c.restore();
    body();
    c.strokeStyle = "rgba(0,0,0,0.7)"; c.lineWidth = Math.max(1, S * 0.03); c.stroke();
    // мох і коріння, що звисають з-під брили (Ліс, Болота)
    if (look.moss && look.hang === "roots") {
      c.strokeStyle = rgba(look.moss, 0.65); c.lineCap = "round";
      for (let k = 0; k < w * 1.2; k++) {
        const h = N.hash(seed, k, 81);
        if (h < 0.5) continue;
        const x = x0 + 0.2 + N.hash(seed, k, 83) * Math.max(0.1, w - 0.4), y = top - under(x);
        const len = 0.3 + (h - 0.5) * 2.2;
        c.lineWidth = Math.max(1, S * 0.035);
        c.beginPath(); c.moveTo(f.x(x), f.y(y + 0.05));
        c.quadraticCurveTo(f.x(x + (N.hash(seed, k, 85) - 0.5) * 0.4), f.y(y - len * 0.5), f.x(x + (N.hash(seed, k, 87) - 0.5) * 0.3), f.y(y - len));
        c.stroke();
      }
    }
  }

  // Опора драбини уступів (terrain.js, postsOf): стовп від каменю внизу до найвищого щабля.
  // timber — тесаний стовп із залізними скобами й підкосами під щаблями (Село); trunk — гнилий стовбур із мохом
  // (Болота); stone — колона з кладки з базою й капітеллю (Столиця).
  function post(f, x, yb, yt, kind, look, seed, grounded) {
    const c = f.c, S = f.ppu;
    const half = kind === "stone" ? 0.34 : kind === "trunk" ? 0.3 : 0.2;
    const pal = WOOD[kind === "stone" ? "capital" : kind === "trunk" ? "forest" : "village"];
    const sway = y => kind === "trunk" ? (N.fbm(x, y * 0.4, seed, 2) - 0.5) * 0.25 : 0;
    const body = () => {
      c.beginPath();
      const n = Math.max(2, Math.ceil((yt - yb) / 0.4));
      for (let i = 0; i <= n; i++) { const y = yb + (yt - yb) * i / n; c.lineTo(f.x(x - half * (kind === "trunk" ? 1 + 0.25 * (1 - i / n) : 1) + sway(y)), f.y(y)); }
      for (let i = n; i >= 0; i--) { const y = yb + (yt - yb) * i / n; c.lineTo(f.x(x + half * (kind === "trunk" ? 1 + 0.25 * (1 - i / n) : 1) + sway(y)), f.y(y)); }
      c.closePath();
    };
    const g = c.createLinearGradient(f.x(x - half), 0, f.x(x + half), 0);
    const dark = kind === "stone" ? pal.stoneDark : pal.dark, mid = kind === "stone" ? pal.stone : kind === "trunk" ? pal.bark : pal.wood,
      light = kind === "stone" ? pal.stoneLight : pal.light;
    g.addColorStop(0, dark); g.addColorStop(0.35, mid); g.addColorStop(0.62, light); g.addColorStop(1, dark);
    c.save();
    c.globalAlpha = 0.92;
    body(); c.fillStyle = g; c.fill();
    c.strokeStyle = "rgba(0,0,0,0.7)"; c.lineWidth = Math.max(1, S * 0.03); c.stroke();
    if (kind === "timber") {
      // волокна і скоби
      c.strokeStyle = rgba(pal.dark, 0.6); c.lineWidth = Math.max(1, S * 0.02);
      [-0.08, 0.06].forEach(o => { c.beginPath(); c.moveTo(f.x(x + o), f.y(yb)); c.lineTo(f.x(x + o), f.y(yt)); c.stroke(); });
      c.fillStyle = rgba(pal.iron, 0.95);
      for (let y = yb + 0.6; y < yt - 0.2; y += 1.6 + N.hash(seed, Math.round(y * 3), 5) * 0.8) c.fillRect(f.x(x - half - 0.02), f.y(y + 0.08), S * (half * 2 + 0.04), S * 0.12);
    } else if (kind === "stone") {
      // шви кладки, база й капітель
      c.strokeStyle = "rgba(0,0,0,0.4)"; c.lineWidth = Math.max(1, S * 0.02);
      for (let y = yb + 0.55; y < yt; y += 0.55) { c.beginPath(); c.moveTo(f.x(x - half), f.y(y)); c.lineTo(f.x(x + half), f.y(y)); c.stroke(); }
      c.fillStyle = pal.stoneDark;
      if (grounded) c.fillRect(f.x(x - half - 0.14), f.y(yb + 0.32), S * (half * 2 + 0.28), S * 0.32);
    } else {
      // кора й мох
      c.strokeStyle = rgba(pal.dark, 0.55); c.lineWidth = Math.max(1, S * 0.025);
      for (let k = -1; k <= 1; k++) { c.beginPath(); for (let y = yb; y <= yt; y += 0.3) c.lineTo(f.x(x + k * half * 0.45 + sway(y)), f.y(y)); c.stroke(); }
      if (look.moss) {
        c.fillStyle = rgba(look.moss, 0.55);
        for (let y = yb + 0.3; y < yt; y += 0.7) {
          const h = N.hash(seed, Math.round(y * 5), 9);
          if (h < 0.45) continue;
          c.beginPath(); c.ellipse(f.x(x - half * 0.6 + sway(y)), f.y(y), S * 0.12, S * (0.15 + h * 0.25), 0, 0, Math.PI * 2); c.fill();
        }
      }
    }
    c.restore();
  }

  // Хата під дахом-уступом (Село, 2026-11-13; користувач: «кімнати різного типу», решено Claude за постійною згодою):
  // широкий уступ над рівною землею на відкритій вулиці — це дах, під ним фасад: глина в дубовому каркасі, двері,
  // віконниці, подекуди вогник; під дахом — солом'яна стріха. Лише малюнок позаду героя: перед хатою ходять,
  // на дах застрибують знизу (уступ односторонній).
  function facade(f, x0, x1, yb, yt, look, seed, up) {
    const c = f.c, S = f.ppu, pal = WOOD.village, o = look.nearHouse || {};
    const l = x0 + 0.3, r = x1 - 0.3, w = r - l, h = yt - yb;
    const X = f.x, Y = f.y;
    c.save();
    // Верх хати над настилом (2026-11-13, «хата-коробка»): настил, яким ходять, — ґанок-піддашшя; за ним стіна
    // горища і крута солом'яна стріха з димарем. Далі від героя, тому темніше й без різких ліній.
    up = Math.min(up || 0, 7.5);
    if (up >= 1.6) {
      const roofH = Math.min(up * 0.62, 1.4 + w * 0.22, 3.6), wallH = Math.min(2.1, up - roofH - 0.1);
      const wy0 = yt, wy1 = yt + Math.max(0, wallH), ul = l + 0.15, ur = r - 0.15;
      if (wallH > 0.6) {
        const g2 = c.createLinearGradient(0, Y(wy1), 0, Y(wy0));
        g2.addColorStop(0, "#211811"); g2.addColorStop(1, "#2a2018");
        c.fillStyle = g2; c.fillRect(X(ul), Y(wy1), (ur - ul) * S, (wy1 - wy0) * S);
        const n2 = Math.max(2, Math.round((ur - ul) / 2.4));
        c.lineCap = "butt";
        for (let i = 0; i <= n2; i++) {
          const bx = ul + (ur - ul) * i / n2;
          c.strokeStyle = "#110b07"; c.lineWidth = S * 0.14; c.beginPath(); c.moveTo(X(bx), Y(wy0)); c.lineTo(X(bx), Y(wy1)); c.stroke();
          if (i < n2) {
            const cx = bx + (ur - ul) / n2 / 2, pick = N.hash(seed, i, 101);
            if (pick < 0.6) {
              const ww = 0.55, wh = Math.min(0.8, wallH * 0.5), wy = wy0 + wallH * 0.3, lit = N.hash(seed, i, 103) < 0.4;
              if (lit) {
                const gl = c.createRadialGradient(X(cx), Y(wy + wh / 2), 0, X(cx), Y(wy + wh / 2), S * 1.6);
                gl.addColorStop(0, rgba(o.windowLit || "#f4a852", 0.22)); gl.addColorStop(1, rgba(o.windowLit || "#f4a852", 0));
                c.fillStyle = gl; c.fillRect(X(cx - 1.6), Y(wy + wh / 2 + 1.6), S * 3.2, S * 3.2);
              }
              c.fillStyle = lit ? rgba(o.windowLit || "#f4a852", 0.85) : "#080504";
              c.beginPath(); c.moveTo(X(cx - ww / 2), Y(wy)); c.lineTo(X(cx - ww / 2), Y(wy + wh * 0.7));
              c.quadraticCurveTo(X(cx), Y(wy + wh * 1.15), X(cx + ww / 2), Y(wy + wh * 0.7)); c.lineTo(X(cx + ww / 2), Y(wy)); c.closePath(); c.fill();
              c.strokeStyle = "#0d0906"; c.lineWidth = S * 0.06; c.stroke();
              c.beginPath(); c.moveTo(X(cx), Y(wy)); c.lineTo(X(cx), Y(wy + wh * 0.92)); c.stroke();
            } else if (pick < 0.85) {
              c.strokeStyle = "#110b07"; c.lineWidth = S * 0.1; c.beginPath();
              c.moveTo(X(bx), Y(wy0)); c.lineTo(X(bx + (ur - ul) / n2), Y(wy1)); c.stroke();
            }
          }
        }
      }
      // стріха: два схили, що провисають, гребінь трохи збоку, звис за стіни; солома шарами
      const ov = 0.55, ex0 = x0 - ov, ex1 = x1 + ov, ry = wy1 - 0.12, ridgeX = (x0 + x1) / 2 + (N.hash(seed, 5, 105) - 0.5) * w * 0.12, ridgeY = ry + roofH;
      const slope = (t, left) => { // t 0 — звис, 1 — гребінь
        const ex = left ? ex0 : ex1, x = ex + (ridgeX - ex) * t, sag = Math.sin(t * Math.PI) * roofH * 0.09;
        return [x, ry + roofH * t - sag];
      };
      // димар за гребенем
      const chx = ridgeX + (N.hash(seed, 6, 107) < 0.5 ? -1 : 1) * w * 0.22, chTop = ridgeY + 0.25;
      if (up - (wallH > 0.6 ? wallH : 0) - roofH > 0.4) {
        c.fillStyle = "#1a1512"; c.fillRect(X(chx - 0.22), Y(chTop), 0.44 * S, roofH * 0.7 * S);
        c.fillStyle = "#26201b"; c.fillRect(X(chx - 0.28), Y(chTop + 0.1), 0.56 * S, 0.14 * S);
      }
      const rg = c.createLinearGradient(0, Y(ridgeY), 0, Y(ry));
      rg.addColorStop(0, "#3a2e1c"); rg.addColorStop(0.5, "#2a2014"); rg.addColorStop(1, "#18110a");
      c.fillStyle = rg;
      c.beginPath();
      for (let k = 0; k <= 12; k++) { const [x, y] = slope(k / 12, true); c.lineTo(X(x), Y(y)); }
      for (let k = 12; k >= 0; k--) { const [x, y] = slope(k / 12, false); c.lineTo(X(x), Y(y)); }
      // нерівний звис соломи понизу
      for (let x = ex1; x >= ex0 - 0.001; x -= 0.14) c.lineTo(X(x), Y(ry - 0.1 - N.hash(seed, Math.round(x * 17), 109) * 0.2));
      c.closePath(); c.fill();
      // шари соломи: ряди вздовж схилів і короткі штрихи вниз
      for (let row = 1; row <= 4; row++) {
        const t = row / 5;
        const [lx, ly] = slope(t, true), [rx, ryy] = slope(t, false);
        c.strokeStyle = "rgba(8,5,3,0.55)"; c.lineWidth = Math.max(1, S * 0.035);
        c.beginPath(); c.moveTo(X(lx + 0.1), Y(ly - 0.05)); c.quadraticCurveTo(X(ridgeX), Y(ly - roofH * 0.06 - 0.05), X(rx - 0.1), Y(ryy - 0.05)); c.stroke();
      }
      c.strokeStyle = rgba(o.roofLight || "#6a5634", 0.35); c.lineWidth = Math.max(1, S * 0.02);
      for (let k = 0; k < w * 9; k++) {
        const t = N.hash(seed, k, 111), left = N.hash(seed, k, 113) < 0.5, [sx, sy] = slope(t * 0.95, left);
        const inx = sx + (ridgeX - sx) * N.hash(seed, k, 115) * 0.9, d = 0.15 + N.hash(seed, k, 117) * 0.3;
        if (sy - d < ry) continue;
        c.beginPath(); c.moveTo(X(inx), Y(sy - 0.04)); c.lineTo(X(inx + (left ? -0.05 : 0.05)), Y(sy - 0.04 - d)); c.stroke();
      }
      // світла кромка схилів і гребінь зі снопів
      c.strokeStyle = rgba(o.roofLight || "#6a5634", 0.6); c.lineWidth = Math.max(1, S * 0.035);
      c.beginPath(); for (let k = 0; k <= 12; k++) { const [x, y] = slope(k / 12, true); c.lineTo(X(x), Y(y)); }
      for (let k = 12; k >= 0; k--) { const [x, y] = slope(k / 12, false); c.lineTo(X(x), Y(y)); } c.stroke();
      c.strokeStyle = "#120c07"; c.lineWidth = S * 0.07; c.lineCap = "round";
      for (let k = -1; k <= 1; k += 2) { c.beginPath(); c.moveTo(X(ridgeX - k * 0.3), Y(ridgeY - 0.28)); c.lineTo(X(ridgeX + k * 0.12), Y(ridgeY + 0.3)); c.stroke(); }
      c.lineCap = "butt";
    }
    // стіна
    const g = c.createLinearGradient(0, Y(yt), 0, Y(yb));
    g.addColorStop(0, "#3b2e23"); g.addColorStop(0.6, "#2c2119"); g.addColorStop(1, "#1a130e");
    c.fillStyle = g; c.fillRect(X(l), Y(yt), (r - l) * S, h * S);
    // плями глини
    for (let k = 0; k < w * h * 0.5; k++) {
      const px = l + N.hash(seed, k, 3) * w, py = yb + N.hash(seed, k, 5) * h;
      c.fillStyle = `rgba(${N.hash(seed, k, 7) < 0.5 ? "90,70,50" : "10,8,6"},0.10)`;
      c.beginPath(); c.ellipse(X(px), Y(py), S * (0.2 + N.hash(seed, k, 9) * 0.5), S * (0.12 + N.hash(seed, k, 11) * 0.3), 0, 0, Math.PI * 2); c.fill();
    }
    // каркас: стовпи, поверхові балки, укосини
    const storeys = Math.max(1, Math.round(h / 2.7)), bays = Math.max(2, Math.round(w / 2.1));
    const beam = (ax, ay, bx, by, t) => {
      c.strokeStyle = "#0d0906"; c.lineWidth = S * (t + 0.06); c.beginPath(); c.moveTo(X(ax), Y(ay)); c.lineTo(X(bx), Y(by)); c.stroke();
      c.strokeStyle = pal.wood; c.lineWidth = S * t; c.beginPath(); c.moveTo(X(ax), Y(ay)); c.lineTo(X(bx), Y(by)); c.stroke();
      c.strokeStyle = rgba(pal.light, 0.35); c.lineWidth = Math.max(1, S * 0.03); c.beginPath(); c.moveTo(X(ax) - S * t * 0.3, Y(ay)); c.lineTo(X(bx) - S * t * 0.3, Y(by)); c.stroke();
    };
    c.lineCap = "butt";
    const door = Math.floor(N.hash(seed, 1, 21) * bays);
    for (let s2 = 0; s2 < storeys; s2++) {
      const y0 = yb + h * s2 / storeys, y1 = yb + h * (s2 + 1) / storeys;
      for (let i = 0; i < bays; i++) {
        const bx0 = l + w * i / bays, bx1 = l + w * (i + 1) / bays, cx = (bx0 + bx1) / 2, pick = N.hash(seed + s2 * 13, i, 23);
        if (s2 === 0 && i === door) {
          // двері: темний отвір, одвірок, клямка
          const dw = Math.min(1.0, (bx1 - bx0) * 0.6), dh = Math.min(1.9, (y1 - y0) * 0.8);
          c.fillStyle = "#090605"; c.fillRect(X(cx - dw / 2), Y(yb + dh), dw * S, dh * S);
          c.strokeStyle = pal.wood; c.lineWidth = S * 0.1; c.strokeRect(X(cx - dw / 2), Y(yb + dh), dw * S, dh * S);
          c.fillStyle = rgba(pal.iron, 1); c.fillRect(X(cx + dw * 0.25), Y(yb + dh * 0.5), S * 0.08, S * 0.08);
        } else if (pick < 0.62) {
          // вікно: з вогником або з віконницями
          const ww = Math.min(0.8, (bx1 - bx0) * 0.45), wh = 0.9, wy = y0 + (y1 - y0) * 0.42;
          const lit = N.hash(seed + s2 * 7, i, 29) < (o.lit || 0.35);
          if (lit) {
            const gl = c.createRadialGradient(X(cx), Y(wy + wh / 2), 0, X(cx), Y(wy + wh / 2), S * 2.2);
            gl.addColorStop(0, rgba(o.windowLit || "#f4a852", 0.3)); gl.addColorStop(1, rgba(o.windowLit || "#f4a852", 0));
            c.fillStyle = gl; c.fillRect(X(cx - 2.2), Y(wy + wh / 2 + 2.2), S * 4.4, S * 4.4);
          }
          c.fillStyle = lit ? (o.windowLit || "#f4a852") : "#0a0706";
          c.fillRect(X(cx - ww / 2), Y(wy + wh), ww * S, wh * S);
          c.strokeStyle = "#0d0906"; c.lineWidth = S * 0.08; c.strokeRect(X(cx - ww / 2), Y(wy + wh), ww * S, wh * S);
          c.beginPath(); c.moveTo(X(cx), Y(wy + wh)); c.lineTo(X(cx), Y(wy)); c.moveTo(X(cx - ww / 2), Y(wy + wh / 2)); c.lineTo(X(cx + ww / 2), Y(wy + wh / 2)); c.stroke();
          if (!lit && N.hash(seed, i + s2 * 5, 31) < 0.5) { // причинена віконниця
            c.fillStyle = pal.dark; c.fillRect(X(cx - ww / 2), Y(wy + wh), ww * 0.55 * S, wh * S);
          }
        } else if (pick < 0.85) {
          const flip = pick < 0.74;
          beam(flip ? bx0 : bx1, y0, flip ? bx1 : bx0, y1, 0.1);
        }
      }
      beam(l, y0 === yb ? yb + 0.08 : y0, r, y0 === yb ? yb + 0.08 : y0, 0.16);
    }
    for (let i = 0; i <= bays; i++) beam(l + w * i / bays, yb, l + w * i / bays, yt, i === 0 || i === bays ? 0.2 : 0.13);
    // стріха під дахом: солома, що звисає
    c.fillStyle = "#231a11";
    c.beginPath(); c.moveTo(X(x0 - 0.35), Y(yt - 0.02));
    for (let x = x0 - 0.35; x <= x1 + 0.35 + 0.001; x += 0.12) c.lineTo(X(x), Y(yt - 0.34 - N.hash(seed, Math.round(x * 20), 41) * 0.28));
    c.lineTo(X(x1 + 0.35), Y(yt - 0.02)); c.closePath(); c.fill();
    c.strokeStyle = rgba(o.roofLight || "#6a5634", 0.55); c.lineWidth = Math.max(1, S * 0.025);
    for (let x = x0 - 0.3; x < x1 + 0.3; x += 0.09) {
      const d = 0.2 + N.hash(seed, Math.round(x * 30), 43) * 0.4;
      c.beginPath(); c.moveTo(X(x), Y(yt - 0.05)); c.lineTo(X(x + (N.hash(seed, Math.round(x * 30), 45) - 0.5) * 0.1), Y(yt - d)); c.stroke();
    }
    // земля під стіною темніша: хата стоїть, а не намальована
    const sh = c.createLinearGradient(0, Y(yb + 0.5), 0, Y(yb));
    sh.addColorStop(0, "rgba(0,0,0,0)"); sh.addColorStop(1, "rgba(0,0,0,0.5)");
    c.fillStyle = sh; c.fillRect(X(l), Y(yb + 0.5), w * S, 0.5 * S);
    c.restore();
  }

  // Аркада під мостом (Столиця, 2026-11-13, решено Claude за постійною згодою): широкий уступ над рівною землею —
  // карниз зруйнованої аркади: колони з базами й капітелями, між ними півкруглі арки з кладкою в пазухах.
  // Лише малюнок позаду героя: під аркадою ходять, на карниз застрибують знизу.
  // RUIN — палітра руїн абатства в Лісі (look.ruins): той самий камінь, холодний і замшілий.
  const RUIN = { stone: "#2c3832", stoneLight: "#5a6c60", stoneDark: "#111814" };
  function arcade(f, x0, x1, yb, yt, look, seed) {
    const c = f.c, S = f.ppu, pal = look.ruins ? RUIN : WOOD.capital, X = f.x, Y = f.y;
    const w = x1 - x0, h = yt - yb, n = Math.max(1, Math.round(w / 3.2)), span = w / n, colW = 0.5;
    const rise = Math.min(span * 0.5 - colW * 0.5, h * 0.45), spring = yt - 0.25 - rise;   // п'ята арки
    c.save();
    // пазухи арок: кладка від карниза до дуги
    const g = c.createLinearGradient(0, Y(yt), 0, Y(spring));
    g.addColorStop(0, pal.stone); g.addColorStop(1, pal.stoneDark);
    c.fillStyle = g;
    c.beginPath(); c.rect(X(x0), Y(yt), w * S, (yt - spring) * S);
    for (let i = 0; i < n; i++) {
      const cx = x0 + span * (i + 0.5), r = span * 0.5 - colW * 0.5;
      c.moveTo(X(cx + r), Y(spring));
      c.ellipse(X(cx), Y(spring), r * S, rise * S, 0, 0, Math.PI, true);
    }
    c.fill("evenodd");
    // шви кладки в пазухах і замковий камінь кожної арки
    c.strokeStyle = "rgba(0,0,0,0.35)"; c.lineWidth = Math.max(1, S * 0.02);
    for (let i = 0; i < n; i++) {
      const cx = x0 + span * (i + 0.5), r = span * 0.5 - colW * 0.5;
      c.beginPath(); c.ellipse(X(cx), Y(spring), r * S, rise * S, 0, Math.PI, 0); c.stroke();
      c.beginPath(); c.ellipse(X(cx), Y(spring), (r + 0.22) * S, (rise + 0.2) * S, 0, Math.PI, 0); c.stroke();
      c.fillStyle = pal.stoneLight; c.fillRect(X(cx - 0.12), Y(spring + rise + 0.22), S * 0.24, S * 0.26);
    }
    // колони: стовбур із канелюрами, база, капітель; одна з середніх — надломлена
    const broken = n > 2 ? 1 + Math.floor(N.hash(seed, 3, 51) * (n - 1)) : -1;
    for (let i = 0; i <= n; i++) {
      const cx = x0 + span * i + (i === 0 ? colW * 0.5 : i === n ? -colW * 0.5 : 0);
      const top = i === broken ? yb + h * (0.3 + N.hash(seed, i, 53) * 0.2) : spring;
      const cg = c.createLinearGradient(X(cx - colW / 2), 0, X(cx + colW / 2), 0);
      cg.addColorStop(0, pal.stoneDark); cg.addColorStop(0.4, pal.stone); cg.addColorStop(0.65, pal.stoneLight); cg.addColorStop(1, pal.stoneDark);
      c.fillStyle = cg; c.fillRect(X(cx - colW / 2), Y(top), colW * S, (top - yb) * S);
      c.strokeStyle = "rgba(0,0,0,0.3)"; c.lineWidth = Math.max(1, S * 0.02);
      [-0.12, 0.02, 0.14].forEach(o => { c.beginPath(); c.moveTo(X(cx + o), Y(top)); c.lineTo(X(cx + o), Y(yb + 0.3)); c.stroke(); });
      c.fillStyle = pal.stoneDark; c.fillRect(X(cx - colW / 2 - 0.14), Y(yb + 0.3), (colW + 0.28) * S, 0.3 * S);
      if (i === broken) {
        // злам: рваний верх
        c.fillStyle = pal.stone; c.beginPath(); c.moveTo(X(cx - colW / 2), Y(top));
        c.lineTo(X(cx - colW * 0.2), Y(top + 0.22)); c.lineTo(X(cx + colW * 0.1), Y(top + 0.05)); c.lineTo(X(cx + colW / 2), Y(top + 0.3)); c.lineTo(X(cx + colW / 2), Y(top)); c.closePath(); c.fill();
      } else {
        c.fillStyle = pal.stoneLight; c.fillRect(X(cx - colW / 2 - 0.16), Y(spring + 0.2), (colW + 0.32) * S, 0.2 * S);
        c.fillStyle = pal.stone; c.fillRect(X(cx - colW / 2 - 0.08), Y(spring), (colW + 0.16) * S, 0.12 * S);
      }
    }
    // Ліс: мох на карнизі й капітелях, плющ по колонах
    if (look.ruins && look.moss) {
      for (let i = 0; i <= n; i++) {
        const cx = x0 + span * i + (i === 0 ? colW * 0.5 : i === n ? -colW * 0.5 : 0);
        c.strokeStyle = rgba(look.moss, 0.7); c.lineWidth = Math.max(1, S * 0.05); c.lineCap = "round";
        c.beginPath(); c.moveTo(X(cx - 0.2), Y(spring + 0.1));
        for (let y = spring; y > yb + 0.4; y -= 0.35) c.lineTo(X(cx + Math.sin(y * 2.3 + i) * 0.22), Y(y));
        c.stroke();
        c.fillStyle = rgba(look.moss, 0.75);
        for (let k = 0; k < 5; k++) { const y = yb + 0.5 + N.hash(seed + i, k, 77) * (spring - yb - 0.5); c.beginPath(); c.ellipse(X(cx + Math.sin(y * 2.3 + i) * 0.22), Y(y), S * 0.13, S * 0.08, N.hash(seed, k, 79) * 3, 0, Math.PI * 2); c.fill(); }
      }
      c.fillStyle = rgba(look.moss, 0.6);
      for (let x = x0; x < x1; x += 0.35) { const hh = N.hash(seed, Math.round(x * 9), 81); if (hh > 0.4) { c.beginPath(); c.ellipse(X(x), Y(yt - 0.05), S * (0.2 + hh * 0.25), S * (0.1 + hh * 0.2), 0, 0, Math.PI); c.fill(); } }
    }
    // тінь під аркадою біля землі
    const sh = c.createLinearGradient(0, Y(yb + 0.4), 0, Y(yb));
    sh.addColorStop(0, "rgba(0,0,0,0)"); sh.addColorStop(1, "rgba(0,0,0,0.4)");
    c.fillStyle = sh; c.fillRect(X(x0), Y(yb + 0.4), w * S, 0.4 * S);
    c.restore();
  }

  // Карниз аркади: тесана плита з поясом і зубцями знизу — на ній стоять.
  function cornice(f, x0, x1, top, seed, look) {
    const c = f.c, S = f.ppu, pal = look && look.ruins ? RUIN : WOOD.capital, y0 = f.y(top), h = S * 0.5;
    const g = c.createLinearGradient(0, y0, 0, y0 + h);
    g.addColorStop(0, pal.stoneLight); g.addColorStop(0.3, pal.stone); g.addColorStop(1, pal.stoneDark);
    c.fillStyle = g; c.fillRect(f.x(x0 - 0.2), y0, f.x(x1 + 0.2) - f.x(x0 - 0.2), h);
    c.strokeStyle = "rgba(0,0,0,0.75)"; c.lineWidth = Math.max(1, S * 0.03); c.strokeRect(f.x(x0 - 0.2), y0, f.x(x1 + 0.2) - f.x(x0 - 0.2), h);
    c.strokeStyle = "rgba(0,0,0,0.4)"; c.beginPath(); c.moveTo(f.x(x0 - 0.2), y0 + h * 0.45); c.lineTo(f.x(x1 + 0.2), y0 + h * 0.45); c.stroke();
    c.fillStyle = pal.stoneDark;
    for (let x = x0; x < x1 - 0.1; x += 0.42) c.fillRect(f.x(x), y0 + h, S * 0.22, S * 0.14);
    c.strokeStyle = rgba(pal.stoneLight, 0.8); c.lineWidth = Math.max(1.5, S * 0.03);
    c.beginPath(); c.moveTo(f.x(x0 - 0.2) + 1, y0 + 1); c.lineTo(f.x(x1 + 0.2) - 1, y0 + 1); c.stroke();
  }

  // Кладка на палях (Болота, 2026-11-13, решено Claude за постійною згодою): широкий настил над рівною землею
  // стоїть на гнилих палях із хрестовинами й мотузяними в'язками, з мохом і корінням. Лише малюнок позаду героя.
  function stilts(f, x0, x1, yb, yt, look, seed) {
    const c = f.c, S = f.ppu, pal = WOOD.forest, X = f.x, Y = f.y;
    const w = x1 - x0, n = Math.max(1, Math.round(w / 2.4)), span = w / n;
    const lean = i => (N.hash(seed, i, 61) - 0.5) * 0.3;
    const px = i => x0 + 0.25 + (w - 0.5) * i / n;
    c.save(); c.lineCap = "round";
    // хрестовини між палями
    for (let i = 0; i < n; i++) {
      const a = px(i), b = px(i + 1), lo = yb + 0.3, hi = yt - 0.5;
      if (hi - lo < 0.8) continue;
      [[a + lean(i) * 0.2, lo, b + lean(i + 1) * 0.8, hi], [b + lean(i + 1) * 0.2, lo, a + lean(i) * 0.8, hi]].forEach(([ax, ay, bx, by], k) => {
        if (N.hash(seed, i * 2 + k, 63) < 0.25) return; // деякі відгнили
        c.strokeStyle = "#060805"; c.lineWidth = S * 0.16; c.beginPath(); c.moveTo(X(ax), Y(ay)); c.lineTo(X(bx), Y(by)); c.stroke();
        c.strokeStyle = pal.wood; c.lineWidth = S * 0.1; c.beginPath(); c.moveTo(X(ax), Y(ay)); c.lineTo(X(bx), Y(by)); c.stroke();
      });
    }
    // палі: товщі внизу, трохи криві
    for (let i = 0; i <= n; i++) {
      const bx = px(i), tx = bx + lean(i);
      c.strokeStyle = "#060805"; c.lineWidth = S * 0.34; c.beginPath(); c.moveTo(X(bx), Y(yb)); c.quadraticCurveTo(X(bx + lean(i) * 0.2), Y((yb + yt) / 2), X(tx), Y(yt)); c.stroke();
      c.strokeStyle = pal.bark; c.lineWidth = S * 0.26; c.beginPath(); c.moveTo(X(bx), Y(yb)); c.quadraticCurveTo(X(bx + lean(i) * 0.2), Y((yb + yt) / 2), X(tx), Y(yt)); c.stroke();
      c.strokeStyle = rgba(pal.light, 0.5); c.lineWidth = Math.max(1, S * 0.05); c.beginPath(); c.moveTo(X(bx - 0.06), Y(yb)); c.quadraticCurveTo(X(bx - 0.06 + lean(i) * 0.2), Y((yb + yt) / 2), X(tx - 0.06), Y(yt)); c.stroke();
      // в'язка мотузкою під настилом, мох біля води
      c.strokeStyle = "#3a3a24"; c.lineWidth = S * 0.07;
      [0.35, 0.47].forEach(d => { c.beginPath(); c.moveTo(X(tx - 0.17), Y(yt - d)); c.lineTo(X(tx + 0.17), Y(yt - d - 0.04)); c.stroke(); });
      if (look.moss) {
        c.fillStyle = rgba(look.moss, 0.7);
        c.beginPath(); c.ellipse(X(bx), Y(yb + 0.25), S * 0.26, S * 0.3, 0, 0, Math.PI * 2); c.fill();
        c.beginPath(); c.ellipse(X(bx - 0.08), Y(yb + 0.8 + N.hash(seed, i, 65) * 0.8), S * 0.12, S * 0.22, 0, 0, Math.PI * 2); c.fill();
      }
    }
    // поперечна балка під настилом і коріння, що звисає
    c.strokeStyle = "#060805"; c.lineWidth = S * 0.2; c.beginPath(); c.moveTo(X(x0 + 0.1), Y(yt - 0.14)); c.lineTo(X(x1 - 0.1), Y(yt - 0.14)); c.stroke();
    c.strokeStyle = pal.dark; c.lineWidth = S * 0.13; c.beginPath(); c.moveTo(X(x0 + 0.1), Y(yt - 0.14)); c.lineTo(X(x1 - 0.1), Y(yt - 0.14)); c.stroke();
    if (look.moss) {
      c.strokeStyle = rgba(look.moss, 0.6); c.lineWidth = Math.max(1, S * 0.035);
      for (let k = 0; k < w * 1.5; k++) {
        const hx = x0 + N.hash(seed, k, 67) * w, len = 0.3 + N.hash(seed, k, 69) * 1.1;
        c.beginPath(); c.moveTo(X(hx), Y(yt - 0.2)); c.quadraticCurveTo(X(hx + (N.hash(seed, k, 71) - 0.5) * 0.3), Y(yt - 0.2 - len * 0.5), X(hx + (N.hash(seed, k, 73) - 0.5) * 0.25), Y(yt - 0.2 - len)); c.stroke();
      }
    }
    c.restore();
  }

  root.LevelMaterials = { WOOD, kindOf, planks, rope, shelf, bridge, log, branch, balcony, slab, chunk, post, facade, arcade, cornice, stilts };
})(window);
