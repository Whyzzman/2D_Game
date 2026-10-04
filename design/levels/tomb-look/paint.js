// Макет нового вигляду Гробниці (пілот переробки зон, 2026-10-04).
// Малює кадр гри (32 × 18 юнітів, як камера) тими прийомами, що підуть у генератор:
// органічні маси каменю з ерозією по краю, світлий кант там, де стоять, п'ять планів глибини,
// промені, пил, туман і темна рамка переднього плану.
(function () {
  "use strict";

  const W = 32, H = 18;

  // ---------- шум ----------
  function hash(x, y, seed) {
    let h = (x * 374761393 + y * 668265263 + seed * 2246822519) | 0;
    h = Math.imul(h ^ (h >>> 13), 1274126177);
    return ((h ^ (h >>> 16)) >>> 0) / 4294967295;
  }
  const smooth = t => t * t * (3 - 2 * t);
  function noise2(x, y, seed) {
    const xi = Math.floor(x), yi = Math.floor(y), xf = smooth(x - xi), yf = smooth(y - yi);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed);
    const c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * xf + (c - a) * yf + (a - b - c + d) * xf * yf;
  }
  function fbm(x, y, seed, octaves) {
    let sum = 0, amp = 0.5, f = 1, norm = 0;
    for (let i = 0; i < (octaves || 4); i++) {
      sum += amp * noise2(x * f, y * f, seed + i * 17);
      norm += amp; amp *= 0.5; f *= 2.03;
    }
    return sum / norm;
  }
  function rng(seed) {
    let s = seed >>> 0;
    return () => { s = (Math.imul(s ^ (s >>> 15), 2246822519) + 0x9e3779b9) >>> 0; return s / 4294967295; };
  }

  // ---------- кадр ----------
  function Frame(canvas) {
    this.c = canvas.getContext("2d");
    this.S = canvas.width / W;
    this.px = (x, y) => [x * this.S, canvas.height - y * this.S];
  }

  // Контур маси: точки в юнітах; ребро, позначене як top, лишається рівним (по ньому ходять),
  // інші ребра розмиваються шумом — камінь, що осипався.
  function erode(points, tops, seed, amount) {
    const out = [];
    const n = points.length;
    for (let i = 0; i < n; i++) {
      const a = points[i], b = points[(i + 1) % n];
      const len = Math.hypot(b[0] - a[0], b[1] - a[1]);
      const steps = Math.max(1, Math.ceil(len / 0.08));
      const nx = -(b[1] - a[1]) / (len || 1), ny = (b[0] - a[0]) / (len || 1);
      const flat = tops.indexOf(i) >= 0;
      for (let s = 0; s < steps; s++) {
        const t = s / steps;
        const x = a[0] + (b[0] - a[0]) * t, y = a[1] + (b[1] - a[1]) * t;
        // Біля кутів ерозія затухає, щоб кут лишився кутом.
        const corner = Math.min(1, Math.min(t, 1 - t) * len / 0.6);
        const amp = flat ? 0.025 : amount * corner;
        const d = (fbm(x * 1.7, y * 1.7, seed, 4) - 0.5) * 2 * amp
          + (fbm(x * 6, y * 6, seed + 5, 2) - 0.5) * amp * 0.35;
        out.push([x + nx * d, y + ny * d]);
      }
    }
    return out;
  }

  Frame.prototype.path = function (pts) {
    const c = this.c;
    c.beginPath();
    pts.forEach((p, i) => { const [x, y] = this.px(p[0], p[1]); i ? c.lineTo(x, y) : c.moveTo(x, y); });
    c.closePath();
  };

  // Кладка всередині маси: великі камені різної ширини, шви темніші, верх каменю трохи світліший.
  Frame.prototype.masonry = function (box, tone, seed, course) {
    const c = this.c, r = rng(seed), S = this.S;
    for (let y = Math.floor(box[1]); y < box[3] + 1; y += course) {
      let x = box[0] - r() * 1.5;
      const row = Math.round(y / course);
      while (x < box[2] + 1) {
        const w = 1.1 + r() * 1.6;
        const [x0, y0] = this.px(x, y + course);
        const shade = (r() - 0.5) * 0.06 + (fbm(x * 0.3, y * 0.3, seed, 3) - 0.5) * 0.14;
        c.fillStyle = shadeColor(tone, shade);
        roundRect(c, x0 + 1.5, y0 + 1.5, w * S - 3, course * S - 3, S * 0.12);
        c.fill();
        // світліший верхній край кожного каменю — об'єм без жорстких ліній
        c.fillStyle = "rgba(200,210,230,0.035)";
        c.fillRect(x0 + 3, y0 + 2, w * S - 6, S * 0.08);
        x += w;
      }
      void row;
    }
  };

  function roundRect(c, x, y, w, h, r) {
    c.beginPath();
    c.moveTo(x + r, y); c.lineTo(x + w - r, y); c.quadraticCurveTo(x + w, y, x + w, y + r);
    c.lineTo(x + w, y + h - r); c.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    c.lineTo(x + r, y + h); c.quadraticCurveTo(x, y + h, x, y + h - r);
    c.lineTo(x, y + r); c.quadraticCurveTo(x, y, x + r, y); c.closePath();
  }

  function hex(h) { return [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)]; }
  function shadeColor(h, d) {
    const [r, g, b] = hex(h);
    const f = v => Math.max(0, Math.min(255, Math.round(v * (1 + d) + d * 18)));
    return `rgb(${f(r)},${f(g)},${f(b)})`;
  }
  function rgba(h, a) { const [r, g, b] = hex(h); return `rgba(${r},${g},${b},${a})`; }

  // Маса ігрової площини: заливка, кладка, внутрішня тінь углиб, кант на поверхнях, де стоять.
  Frame.prototype.mass = function (spec, look) {
    const c = this.c, S = this.S;
    const pts = erode(spec.points, spec.tops || [], spec.seed || 1, spec.erosion == null ? 0.22 : spec.erosion);
    const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]);
    const box = [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];

    c.save();
    this.path(pts);
    c.fillStyle = look.core;
    c.fill();
    c.clip();
    // Край маси світліший, серцевина темна: обвідна форма читається без лінії.
    this.path(pts);
    c.lineJoin = "round";
    c.lineWidth = S * 0.9;
    c.strokeStyle = look.stone;
    c.filter = `blur(${S * 0.3}px)`;
    c.stroke();
    c.filter = "none";
    // Кладка проступає лише біля краю, де на неї падає світло.
    c.globalAlpha = 0.55;
    this.masonry(box, look.stone, spec.seed || 1, spec.course || 1.0);
    c.globalAlpha = 1;
    c.globalCompositeOperation = "destination-in";
    this.path(pts);
    c.lineWidth = S * 1.3;
    c.strokeStyle = "#000";
    c.filter = `blur(${S * 0.4}px)`;
    c.stroke();
    c.filter = "none";
    c.restore();

    // Серцевину домальовуємо поверх стертої кладки.
    c.save();
    this.path(pts);
    c.clip();
    c.globalCompositeOperation = "destination-over";
    c.fillStyle = look.core;
    c.fillRect(0, 0, c.canvas.width, c.canvas.height);
    c.restore();

    // Відблиск на гранях, повернутих до світла (вгору і трохи вбік).
    c.save();
    c.lineCap = "round";
    for (let i = 0; i < pts.length; i++) {
      const a = pts[i], b = pts[(i + 1) % pts.length];
      const nx = -(b[1] - a[1]), ny = (b[0] - a[0]);
      const len = Math.hypot(nx, ny) || 1;
      const facing = (ny / len) * 0.9 + (nx / len) * 0.25;
      if (facing <= 0.15) continue;
      c.strokeStyle = rgba(look.rim, 0.42 * Math.min(1, facing));
      c.lineWidth = S * 0.05;
      const [ax, ay] = this.px(a[0], a[1]), [bx, by] = this.px(b[0], b[1]);
      c.beginPath(); c.moveTo(ax, ay); c.lineTo(bx, by); c.stroke();
    }
    c.restore();

    // Кант: світліша кладка зверху, з ледь відбитим пилом — «тут можна стояти».
    (spec.tops || []).forEach(i => {
      const a = spec.points[i], b = spec.points[(i + 1) % spec.points.length];
      const x0 = Math.min(a[0], b[0]), x1 = Math.max(a[0], b[0]), y = a[1];
      this.ledgeLip(x0, x1, y, look, spec.seed || 1);
    });
  };

  Frame.prototype.ledgeLip = function (x0, x1, y, look, seed) {
    const c = this.c, S = this.S, r = rng(seed * 31 + Math.round(x0 * 10));
    const [ax, ay] = this.px(x0, y), [bx] = this.px(x1, y);
    // плита канта
    const g = c.createLinearGradient(0, ay, 0, ay + S * 0.32);
    g.addColorStop(0, look.lip);
    g.addColorStop(1, rgba(look.lipDark, 0.0));
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(ax, ay);
    for (let x = x0; x <= x1; x += 0.1) {
      const [px] = this.px(x, y);
      c.lineTo(px, ay + S * (0.22 + (fbm(x * 2, y, seed, 2) - 0.5) * 0.12));
    }
    c.lineTo(bx, ay);
    c.closePath();
    c.fill();
    // світла лінія самої поверхні
    c.strokeStyle = rgba(look.lipLight, 0.85);
    c.lineWidth = S * 0.045;
    c.beginPath(); c.moveTo(ax, ay + 1); c.lineTo(bx, ay + 1); c.stroke();
    // пил і дрібні уламки на краю
    for (let x = x0 + 0.2; x < x1 - 0.2; x += 0.3 + r() * 0.9) {
      if (r() < 0.45) {
        const [px, py] = this.px(x, y);
        c.fillStyle = rgba(look.lipDark, 0.9);
        c.beginPath(); c.ellipse(px, py - S * 0.04, S * (0.06 + r() * 0.1), S * (0.04 + r() * 0.04), 0, Math.PI, 0); c.fill();
      }
    }
  };

  // ---------- фонові плани ----------
  Frame.prototype.sky = function (top, bottom) {
    const c = this.c, g = c.createLinearGradient(0, 0, 0, c.canvas.height);
    g.addColorStop(0, top); g.addColorStop(1, bottom);
    c.fillStyle = g; c.fillRect(0, 0, c.canvas.width, c.canvas.height);
  };

  // Стрілчаста арка (силует): x — центр, w — ширина прольоту, y0 — низ, h — висота до вершини.
  Frame.prototype.pointedArch = function (x, w, y0, h) {
    const c = this.c;
    const [lx, ly] = this.px(x - w / 2, y0), [rx] = this.px(x + w / 2, y0);
    const [, sy] = this.px(x, y0 + h - w * 0.75), [tx, ty] = this.px(x, y0 + h);
    c.moveTo(lx, ly); c.lineTo(lx, sy);
    c.quadraticCurveTo(lx, ty + (sy - ty) * 0.15, tx, ty);
    c.quadraticCurveTo(rx, ty + (sy - ty) * 0.15, rx, sy);
    c.lineTo(rx, ly);
  };

  // Ряд колон з арками — середній чи ближній план; break — обвалені прольоти.
  Frame.prototype.arcade = function (o) {
    const c = this.c, S = this.S, r = rng(o.seed);
    c.save();
    c.fillStyle = o.wall;
    c.fillRect(0, this.px(0, o.top)[1], c.canvas.width, this.px(0, o.base)[1] - this.px(0, o.top)[1]);
    for (let x = o.offset; x < W + o.span; x += o.span) {
      const broken = r() < (o.broken || 0);
      c.beginPath();
      this.pointedArch(x, o.span * 0.62, o.base, o.archH * (broken ? 0.7 + r() * 0.2 : 1));
      c.fillStyle = o.hole;
      c.fill();
      // глибина прорізу — світліше до низу (туман за аркою)
      const [, top] = this.px(0, o.base + o.archH), [, bot] = this.px(0, o.base);
      const g = c.createLinearGradient(0, top, 0, bot);
      g.addColorStop(0, "rgba(0,0,0,0)"); g.addColorStop(1, o.mist);
      c.fillStyle = g; c.fill();
      // колона між арками: пучкова, з капітеллю
      const cx = x + o.span / 2;
      const [px0, py0] = this.px(cx - o.col / 2, o.top), [, pyb] = this.px(0, o.base);
      c.fillStyle = o.column;
      c.fillRect(px0, py0, o.col * S, pyb - py0);
      c.fillStyle = o.columnLight;
      c.fillRect(px0 + o.col * S * 0.18, py0, o.col * S * 0.12, pyb - py0);
      c.fillRect(px0 - S * 0.12, this.px(0, o.base + o.archH * 0.62)[1], o.col * S + S * 0.24, S * 0.22);
    }
    c.restore();
  };

  // Далекий план: величезні ребра склепінь і вікна-розетки в тумані.
  Frame.prototype.farNave = function (o) {
    const c = this.c, S = this.S;
    c.save();
    for (let x = o.offset; x < W + 12; x += o.span) {
      c.beginPath();
      this.pointedArch(x, o.span * 0.8, o.base, o.h);
      c.lineWidth = S * 0.7;
      c.strokeStyle = o.rib;
      c.stroke();
      // високе вузьке вікно з тьмяним світлом
      c.beginPath();
      this.pointedArch(x, o.span * 0.18, o.base + o.h * 0.35, o.h * 0.42);
      c.fillStyle = o.window;
      c.fill();
    }
    c.restore();
  };

  Frame.prototype.fog = function (y0, y1, color, alpha) {
    const c = this.c, [, a] = this.px(0, y1), [, b] = this.px(0, y0);
    const g = c.createLinearGradient(0, a, 0, b);
    g.addColorStop(0, rgba(color, 0)); g.addColorStop(0.6, rgba(color, alpha)); g.addColorStop(1, rgba(color, alpha * 0.6));
    c.fillStyle = g; c.fillRect(0, a, c.canvas.width, b - a);
    // клапті, що рухаються
    const r = rng(Math.round(y0 * 100));
    for (let i = 0; i < 9; i++) {
      const x = r() * W, y = y0 + r() * (y1 - y0) * 0.6;
      const [px, py] = this.px(x, y);
      const rad = this.S * (2 + r() * 4);
      const gg = c.createRadialGradient(px, py, 0, px, py, rad);
      gg.addColorStop(0, rgba(color, alpha * 0.7)); gg.addColorStop(1, rgba(color, 0));
      c.fillStyle = gg;
      c.beginPath(); c.ellipse(px, py, rad * 1.8, rad * 0.45, 0, 0, Math.PI * 2); c.fill();
    }
  };

  // Промінь крізь пролом склепіння: м'який конус, пил у ньому.
  Frame.prototype.shaft = function (x, y, angle, width, length, color, alpha) {
    const c = this.c, S = this.S;
    const [px, py] = this.px(x, y);
    c.save();
    c.globalCompositeOperation = "screen";
    c.translate(px, py); c.rotate(angle);
    const g = c.createLinearGradient(0, 0, 0, length * S);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(0.7, rgba(color, alpha * 0.35)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g;
    c.filter = `blur(${S * 0.35}px)`;
    c.beginPath();
    c.moveTo(-width * S * 0.35, 0); c.lineTo(width * S * 0.35, 0);
    c.lineTo(width * S * 0.9, length * S); c.lineTo(-width * S * 0.9, length * S);
    c.closePath(); c.fill();
    c.filter = "none";
    const r = rng(Math.round(x * 97));
    for (let i = 0; i < 40; i++) {
      const t = r(), off = (r() - 0.5) * width * (0.5 + t * 0.9);
      c.fillStyle = rgba(color, 0.25 + r() * 0.5);
      c.beginPath(); c.arc(off * S, t * length * S, S * (0.015 + r() * 0.03), 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };

  Frame.prototype.glow = function (x, y, radius, color, alpha) {
    const c = this.c, [px, py] = this.px(x, y), rad = radius * this.S;
    c.save();
    c.globalCompositeOperation = "screen";
    const g = c.createRadialGradient(px, py, 0, px, py, rad);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(0.35, rgba(color, alpha * 0.4)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g; c.fillRect(px - rad, py - rad, rad * 2, rad * 2);
    c.restore();
  };

  // ---------- речі ----------
  Frame.prototype.altar = function (x, y) {
    const c = this.c, S = this.S;
    this.glow(x, y + 1.2, 5.5, "#F0B441", 0.42);
    const [px, py] = this.px(x, y);
    // кругла приземлена маса — єдина тепла форма серед стрілчастої
    c.fillStyle = "#3a3128";
    c.beginPath(); c.ellipse(px, py, S * 1.1, S * 0.28, 0, Math.PI, 0); c.fill();
    c.fillStyle = "#4a3e31";
    roundRect(c, px - S * 0.7, py - S * 1.0, S * 1.4, S * 0.85, S * 0.2); c.fill();
    c.fillStyle = "#5e4c37";
    c.beginPath(); c.ellipse(px, py - S * 1.0, S * 0.85, S * 0.2, 0, 0, Math.PI * 2); c.fill();
    // чаша з вогнем
    c.fillStyle = "#8A7434";
    c.beginPath(); c.ellipse(px, py - S * 1.18, S * 0.42, S * 0.16, 0, 0, Math.PI); c.fill();
    this.glow(x, y + 1.35, 1.0, "#FFE2A8", 0.9);
    // свічки
    [[-1.3, 0.35], [-1.05, 0.5], [1.15, 0.42], [1.38, 0.3]].forEach(([dx, h]) => {
      const [cx, cy] = this.px(x + dx, y);
      c.fillStyle = "#d8cbb4"; c.fillRect(cx - S * 0.04, cy - S * h, S * 0.08, S * h);
      this.glow(x + dx, y + h + 0.06, 0.45, "#FFC66B", 0.8);
    });
  };

  Frame.prototype.hero = function (x, y, facing) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save();
    c.translate(px, py); c.scale(facing || 1, 1);
    // контрове світло — холодний кант
    c.shadowColor = "rgba(150,180,220,0.55)"; c.shadowBlur = S * 0.25;
    c.fillStyle = "#1c2230";
    c.beginPath();
    c.moveTo(-S * 0.18, -S * 1.72);
    c.quadraticCurveTo(S * 0.05, -S * 2.15, S * 0.22, -S * 1.6);
    c.lineTo(S * 0.42, -S * 0.25); c.quadraticCurveTo(S * 0.2, 0, S * 0.05, -S * 0.05);
    c.lineTo(-S * 0.1, 0); c.quadraticCurveTo(-S * 0.45, -S * 0.1, -S * 0.42, -S * 0.3);
    c.closePath(); c.fill();
    c.shadowBlur = 0;
    c.fillStyle = "#7f93ad";
    c.beginPath(); c.ellipse(S * 0.08, -S * 1.55, S * 0.06, S * 0.03, 0, 0, Math.PI * 2); c.fill();
    c.strokeStyle = "#b8c4d6"; c.lineWidth = S * 0.05;
    c.beginPath(); c.moveTo(S * 0.25, -S * 1.0); c.lineTo(S * 1.15, -S * 0.55); c.stroke();
    c.restore();
  };

  Frame.prototype.chain = function (x, y0, y1, color, scale) {
    const c = this.c, S = this.S * (scale || 1);
    c.save(); c.strokeStyle = color; c.lineWidth = S * 0.05;
    for (let y = y0; y < y1; y += 0.22 * (scale || 1)) {
      const [px, py] = this.px(x, y);
      c.beginPath(); c.ellipse(px, py, S * 0.06, S * 0.12, 0, 0, Math.PI * 2); c.stroke();
    }
    c.restore();
  };

  Frame.prototype.banner = function (x, top, len, cloth, gold) {
    const c = this.c, S = this.S, [px, py] = this.px(x, top);
    c.fillStyle = "#2a2520"; c.fillRect(px - S * 0.55, py - S * 0.06, S * 1.1, S * 0.12);
    c.fillStyle = cloth;
    c.beginPath(); c.moveTo(px - S * 0.45, py);
    c.lineTo(px + S * 0.45, py); c.lineTo(px + S * 0.42, py + len * S);
    c.lineTo(px, py + (len - 0.45) * S); c.lineTo(px - S * 0.4, py + len * S); c.closePath(); c.fill();
    // вицвіла корона-полум'я
    c.fillStyle = gold;
    c.beginPath(); c.moveTo(px, py + S * 0.7); c.lineTo(px - S * 0.2, py + S * 1.15);
    c.lineTo(px + S * 0.2, py + S * 1.15); c.closePath(); c.fill();
    // рвані краї
    c.fillStyle = "rgba(0,0,0,0.35)";
    c.fillRect(px + S * 0.2, py + S * 0.2, S * 0.25, len * S * 0.8);
  };

  Frame.prototype.statue = function (x, y, h, color, light) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.fillStyle = color;
    // постамент
    c.fillRect(px - S * 0.6, py - S * 0.6, S * 1.2, S * 0.6);
    // лицар-ефігія: капюшон, плащ, меч уперед
    c.beginPath();
    c.moveTo(px - S * 0.45, py - S * 0.6);
    c.lineTo(px - S * 0.35, py - S * (h - 0.9));
    c.quadraticCurveTo(px, py - S * (h + 0.1), px + S * 0.35, py - S * (h - 0.9));
    c.lineTo(px + S * 0.45, py - S * 0.6); c.closePath(); c.fill();
    c.fillStyle = light;
    c.fillRect(px - S * 0.03, py - S * (h - 0.6), S * 0.06, S * (h - 1.4));
  };

  Frame.prototype.rubble = function (x0, x1, y, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    for (let x = x0; x < x1; x += 0.15 + r() * 0.35) {
      const [px, py] = this.px(x, y);
      const w = S * (0.12 + r() * 0.35), h = S * (0.08 + r() * 0.25);
      c.fillStyle = shadeColor(color, (r() - 0.5) * 0.3);
      c.beginPath();
      c.moveTo(px - w, py); c.lineTo(px - w * 0.6, py - h); c.lineTo(px + w * 0.4, py - h * (0.8 + r() * 0.4)); c.lineTo(px + w, py);
      c.closePath(); c.fill();
    }
  };

  // Передній план: лише силует, майже чорний, з м'яким розмиттям глибини.
  Frame.prototype.foreground = function (draw) {
    const c = this.c;
    c.save();
    c.filter = `blur(${this.S * 0.06}px)`;
    c.fillStyle = "#07080b";
    draw(c, this);
    c.restore();
  };

  Frame.prototype.vignette = function (strength) {
    const c = this.c, w = c.canvas.width, h = c.canvas.height;
    const g = c.createRadialGradient(w / 2, h * 0.55, h * 0.35, w / 2, h * 0.55, h * 0.95);
    g.addColorStop(0, "rgba(0,0,0,0)"); g.addColorStop(1, `rgba(0,0,0,${strength})`);
    c.fillStyle = g; c.fillRect(0, 0, w, h);
  };

  Frame.prototype.dust = function (count, color, seed) {
    const c = this.c, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < count; i++) {
      const [px, py] = this.px(r() * W, r() * H);
      c.fillStyle = rgba(color, 0.15 + r() * 0.45);
      c.beginPath(); c.arc(px, py, this.S * (0.012 + r() * 0.035), 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };

  window.TombLook = { Frame, W, H, rgba, rng, fbm };
})();
