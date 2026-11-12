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

  // Гілка з дерева, що росте зі стіни: товща біля стіни, з листям на кінці.
  function branch(f, x0, x1, top, wallSide, pal, seed) {
    const c = f.c, S = f.ppu;
    const from = wallSide < 0 ? x0 - 0.3 : x1 + 0.3, to = wallSide < 0 ? x1 + 0.2 : x0 - 0.2;
    const g = c.createLinearGradient(0, f.y(top), 0, f.y(top - 0.5));
    g.addColorStop(0, pal.light); g.addColorStop(0.35, pal.bark); g.addColorStop(1, pal.dark);
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(f.x(from), f.y(top));
    c.lineTo(f.x(to), f.y(top));
    c.quadraticCurveTo(f.x(to), f.y(top - 0.26), f.x(to + wallSide * 0.3), f.y(top - 0.42));
    c.quadraticCurveTo(f.x((from + to) / 2), f.y(top - 0.62), f.x(from), f.y(top - 1.25));
    c.closePath(); c.fill();
    // мох по верху: тут стоять
    c.strokeStyle = rgba(pal.leaf, 0.9); c.lineWidth = Math.max(1.5, S * 0.05);
    c.beginPath(); c.moveTo(f.x(from), f.y(top) + 1); c.lineTo(f.x(to), f.y(top) + 1); c.stroke();
    c.fillStyle = rgba(pal.leaf, 0.85);
    for (let k = 0; k < Math.abs(to - from) * 3; k++) {
      const mx = from + (to - from) * N.hash(seed, k, 21);
      c.beginPath(); c.ellipse(f.x(mx), f.y(top - 0.03), S * (0.05 + N.hash(seed, k, 23) * 0.08), S * 0.05, 0, 0, Math.PI * 2); c.fill();
    }
    c.strokeStyle = rgba(pal.dark, 0.55); c.lineWidth = Math.max(1, S * 0.02);
    for (let i = 0; i < 3; i++) {
      const yy = top - 0.1 - i * 0.12;
      c.beginPath(); c.moveTo(f.x(from), f.y(yy - 0.1)); c.quadraticCurveTo(f.x((from + to) / 2), f.y(yy), f.x(to), f.y(top - 0.06 - i * 0.03)); c.stroke();
    }
    // листя на кінці
    c.fillStyle = rgba(pal.leaf, 0.95);
    for (let k = 0; k < 6; k++) {
      const lx = f.x(to + wallSide * (0.1 + N.hash(seed, k, 3) * 0.4)), ly = f.y(top - 0.05 - N.hash(seed, k, 5) * 0.35);
      c.save(); c.translate(lx, ly); c.rotate((N.hash(seed, k, 9) - 0.5) * 2.4);
      c.beginPath(); c.ellipse(0, 0, S * 0.16, S * 0.07, 0, 0, Math.PI * 2); c.fill(); c.restore();
    }
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

  root.LevelMaterials = { WOOD, kindOf, planks, rope, shelf, bridge, log, branch, balcony, slab, chunk };
})(window);
