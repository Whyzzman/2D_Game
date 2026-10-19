// Соборні речі для макета поверх бібліотек Гробниці, Лісу, Села й Столиці: розетка з вітражем, пучкові колони
// з ребрами склепінь, люстра зі свічками, свята-статуя з німбом, дзвін на ланцюгу, балюстрада хорів,
// товща води з каустикою й променями. Плавні криві; кожна річ — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Розетка: кам'яне коло, пелюстки ажуру, скло — теплі й бірюзові уламки, частина вибита.
  Frame.prototype.roseWindow = function (x, y, r, o, seed) {
    const c = this.c, S = this.S, rr = rng(seed), [px, py] = this.px(x, y), R = r * S;
    this.glow(x, y, r * 3.2, o.glow, 0.32);
    c.save();
    // скло: уламки в колі
    c.beginPath(); c.arc(px, py, R * 0.9, 0, Math.PI * 2); c.clip();
    c.fillStyle = o.dark; c.fillRect(px - R, py - R, R * 2, R * 2);
    c.filter = `blur(${S * 0.03}px)`;
    for (let i = 0; i < 320; i++) {
      if (rr() < 0.14) continue; // вибите скло
      const a = rr() * Math.PI * 2, d = Math.sqrt(rr()) * R * 0.9, cx = px + Math.cos(a) * d, cy = py + Math.sin(a) * d;
      const rad = S * (0.1 + rr() * 0.16), n = 5 + Math.floor(rr() * 3);
      c.beginPath();
      for (let k = 0; k < n; k++) {
        const b = k / n * Math.PI * 2 + rr() * 0.4, dd = rad * (0.7 + rr() * 0.4);
        k ? c.lineTo(cx + Math.cos(b) * dd, cy + Math.sin(b) * dd) : c.moveTo(cx + Math.cos(b) * dd, cy + Math.sin(b) * dd);
      }
      c.closePath();
      // ближче до центру — тепле, до краю — бірюзове
      const warm = d / (R * 0.9) < 0.45 ? 0.75 : 0.25;
      c.fillStyle = rgba(rr() < warm ? o.warm[Math.floor(rr() * o.warm.length)] : o.cold[Math.floor(rr() * o.cold.length)], 0.35 + rr() * 0.35);
      c.fill();
    }
    c.filter = "none";
    c.restore();
    // ажур: обід, пелюстки, спиці, середнє коло — кам'яні жилки
    c.save();
    c.strokeStyle = o.stone; c.lineCap = "round";
    c.lineWidth = R * 0.12;
    c.beginPath(); c.arc(px, py, R * 0.95, 0, Math.PI * 2); c.stroke();
    const petals = o.petals || 12;
    c.lineWidth = R * 0.035;
    for (let k = 0; k < petals; k++) {
      const a = k / petals * Math.PI * 2, a2 = (k + 0.5) / petals * Math.PI * 2;
      c.beginPath(); c.moveTo(px + Math.cos(a) * R * 0.28, py + Math.sin(a) * R * 0.28); c.lineTo(px + Math.cos(a) * R * 0.9, py + Math.sin(a) * R * 0.9); c.stroke();
      // пелюстка — стрілчаста арка між спицями
      c.beginPath();
      c.moveTo(px + Math.cos(a) * R * 0.5, py + Math.sin(a) * R * 0.5);
      c.quadraticCurveTo(px + Math.cos(a2) * R * 0.98, py + Math.sin(a2) * R * 0.98,
        px + Math.cos(a + Math.PI * 2 / petals) * R * 0.5, py + Math.sin(a + Math.PI * 2 / petals) * R * 0.5);
      c.stroke();
      c.beginPath(); c.arc(px + Math.cos(a2) * R * 0.74, py + Math.sin(a2) * R * 0.74, R * 0.07, 0, Math.PI * 2); c.stroke();
    }
    c.lineWidth = R * 0.05;
    c.beginPath(); c.arc(px, py, R * 0.28, 0, Math.PI * 2); c.stroke();
    c.beginPath(); c.arc(px, py, R * 0.5, 0, Math.PI * 2); c.lineWidth = R * 0.025; c.stroke();
    // золотий відблиск на ажурі зверху-ліворуч
    c.strokeStyle = rgba(o.gold, 0.55); c.lineWidth = R * 0.02;
    c.beginPath(); c.arc(px, py, R * 0.99, Math.PI * 1.05, Math.PI * 1.6); c.stroke();
    c.restore();
    this.glow(x, y, r * 0.5, o.core, 0.55);
  };

  // Пучкова колона: стовбур зі світлими жилками, капітель, від неї — ребра склепіння в обидва боки.
  Frame.prototype.pillar = function (x, base, top, w, o, ribs) {
    const c = this.c, S = this.S;
    const [l, t] = this.px(x - w / 2, top), [, b] = this.px(x, base);
    const g = c.createLinearGradient(l, 0, l + w * S, 0);
    g.addColorStop(0, o.light); g.addColorStop(0.3, o.stone); g.addColorStop(1, o.dark);
    c.fillStyle = g; c.fillRect(l, t, w * S, b - t);
    c.strokeStyle = rgba("#000000", 0.35); c.lineWidth = S * 0.04;
    [0.25, 0.5, 0.75].forEach(k => { c.beginPath(); c.moveTo(l + w * S * k, t); c.lineTo(l + w * S * k, b); c.stroke(); });
    // база й капітель — м'які потовщення
    c.fillStyle = o.stone;
    c.beginPath(); c.ellipse(...this.px(x, base + 0.3), w * S * 0.75, S * 0.35, 0, Math.PI, 0); c.fill();
    c.fillRect(...this.px(x - w * 0.7, top + 0.1), w * 1.4 * S, 0.45 * S);
    if (!ribs) return;
    // ребра: криві до сусідніх колон, сходяться стрілкою
    c.strokeStyle = o.stone; c.lineCap = "round";
    ribs.forEach(([tx, peak]) => {
      const mx = (x + tx) / 2;
      c.lineWidth = S * w * 0.42;
      c.beginPath(); c.moveTo(...this.px(x, top));
      c.bezierCurveTo(...this.px(x, top + (peak - top) * 0.75), ...this.px(mx - (tx - x) * 0.18, peak), ...this.px(mx, peak));
      c.stroke();
      c.strokeStyle = rgba(o.light, 0.5); c.lineWidth = S * 0.05;
      c.beginPath(); c.moveTo(...this.px(x - w * 0.15, top));
      c.bezierCurveTo(...this.px(x - w * 0.15, top + (peak - top) * 0.75), ...this.px(mx - (tx - x) * 0.18, peak + 0.15), ...this.px(mx, peak + 0.15));
      c.stroke();
      c.strokeStyle = o.stone;
    });
  };

  // Балюстрада хорів: поруччя з маленькими стрілчастими арками.
  Frame.prototype.balustrade = function (x0, x1, y, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.fillStyle = o.stone;
    c.fillRect(...this.px(x0, y + h), (x1 - x0) * S, 0.18 * S);
    for (let x = x0 + 0.25; x < x1 - 0.2; x += 0.55) {
      if (r() < 0.12) continue; // відбитий стовпчик
      const [bx, by] = this.px(x, y);
      c.beginPath();
      c.moveTo(bx - S * 0.07, by);
      c.quadraticCurveTo(bx - S * 0.16, by - h * S * 0.5, bx - S * 0.06, by - h * S);
      c.lineTo(bx + S * 0.06, by - h * S);
      c.quadraticCurveTo(bx + S * 0.16, by - h * S * 0.5, bx + S * 0.07, by);
      c.closePath(); c.fill();
    }
    c.fillStyle = rgba(o.light, 0.5);
    c.fillRect(...this.px(x0, y + h + 0.02), (x1 - x0) * S, 0.04 * S);
    c.restore();
  };

  // Люстра: ланцюг, обід, свічки на ньому; може висіти похило й наполовину під водою.
  Frame.prototype.chandelier = function (x, y, w, o, seed, tilt) {
    const c = this.c, S = this.S, r = rng(seed);
    this.chain(x, y + 0.3, 19, o.chain, 0.9);
    c.save();
    c.translate(...this.px(x, y)); c.rotate(tilt || 0);
    c.strokeStyle = o.iron; c.lineWidth = S * 0.09;
    c.beginPath(); c.ellipse(0, 0, w * S / 2, S * 0.22, 0, 0, Math.PI * 2); c.stroke();
    c.lineWidth = S * 0.05;
    for (let k = -2; k <= 2; k++) { c.beginPath(); c.moveTo(0, -S * 0.3); c.quadraticCurveTo(k * w * S * 0.12, S * 0.25, k * w * S * 0.2, S * 0.05); c.stroke(); }
    c.restore();
    if (!o.lit) return;
    for (let k = 0; k < 7; k++) {
      if (r() < 0.3) continue;
      const a = k / 7 * Math.PI * 2, cx = x + Math.cos(a) * w * 0.5, cy = y + Math.sin(a) * 0.22 + Math.sin(tilt || 0) * (cx - x);
      this.candle(cx, cy, 0.3, o.flame);
    }
  };

  // Дзвін: бронзове тіло з розширеним краєм, вухо, язик; ледь жевріє.
  Frame.prototype.bell = function (x, y, h, o) {
    const c = this.c, S = this.S, P = (dx, dy) => this.px(x + dx * h, y + dy * h);
    this.glow(x, y + h * 0.5, h * 1.8, o.glow, 0.25);
    c.fillStyle = o.bronze;
    c.beginPath(); c.moveTo(...P(-0.18, 1));
    c.bezierCurveTo(...P(-0.32, 0.95), ...P(-0.3, 0.5), ...P(-0.36, 0.22));
    c.quadraticCurveTo(...P(-0.5, 0.05), ...P(-0.56, 0));
    c.lineTo(...P(0.56, 0));
    c.quadraticCurveTo(...P(0.5, 0.05), ...P(0.36, 0.22));
    c.bezierCurveTo(...P(0.3, 0.5), ...P(0.32, 0.95), ...P(0.18, 1));
    c.closePath(); c.fill();
    c.strokeStyle = rgba(o.light, 0.5); c.lineWidth = S * 0.05;
    c.beginPath(); c.moveTo(...P(-0.2, 0.92)); c.bezierCurveTo(...P(-0.28, 0.7), ...P(-0.27, 0.4), ...P(-0.36, 0.18)); c.stroke();
    c.beginPath(); c.ellipse(...P(0, 0.08), h * S * 0.4, h * S * 0.035, 0, 0, Math.PI * 2); c.stroke();
    c.fillStyle = o.bronze; c.beginPath(); c.arc(...P(0, -0.06), h * S * 0.07, 0, Math.PI * 2); c.fill();
    this.chain(x, y + h, y + h + 6, o.chain, 0.9);
  };

  // Свята-статуя: довгий каптур, складені руки з розірваними чотками, гладке обличчя з тріщиною світла,
  // німб променями; низ одягу тече водою.
  Frame.prototype.saint = function (x, ground, h, o) {
    const c = this.c, S = this.S, P = (dx, dy) => this.px(x + dx * h, ground + dy * h);
    // німб — тонкі промені
    const [hx, hy] = P(0, 0.86);
    this.glow(x, ground + h * 0.86, h * 0.75, o.halo, 0.3);
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = rgba(o.halo, 0.75); c.lineWidth = S * 0.035;
    c.beginPath(); c.arc(hx, hy, h * S * 0.17, 0, Math.PI * 2); c.stroke();
    for (let k = 0; k < 28; k++) {
      const a = k / 28 * Math.PI * 2, l = h * S * (k % 2 ? 0.26 : 0.36);
      c.beginPath(); c.moveTo(hx + Math.cos(a) * h * S * 0.19, hy + Math.sin(a) * h * S * 0.19); c.lineTo(hx + Math.cos(a) * l, hy + Math.sin(a) * l); c.stroke();
    }
    c.restore();
    c.fillStyle = o.stone;
    // тіло в каптурі: високий стрілчастий силует, рваний текучий низ
    c.beginPath(); c.moveTo(...P(0, 1.0));
    c.bezierCurveTo(...P(0.1, 0.98), ...P(0.13, 0.86), ...P(0.12, 0.74));
    c.bezierCurveTo(...P(0.17, 0.56), ...P(0.19, 0.3), ...P(0.16, 0.1));
    for (let k = 0; k <= 8; k++) c.lineTo(...P(0.16 - k * 0.04, (k % 2 ? 0.0 : 0.08) - (k % 3) * 0.025));
    c.bezierCurveTo(...P(-0.19, 0.3), ...P(-0.17, 0.56), ...P(-0.12, 0.74));
    c.bezierCurveTo(...P(-0.13, 0.86), ...P(-0.1, 0.98), ...P(0, 1.0));
    c.closePath(); c.fill();
    // проріз каптура — стрілчаста тінь, у ній гладкий камінь обличчя з тріщиною світла
    c.fillStyle = o.face;
    c.beginPath(); c.moveTo(...P(0, 0.93));
    c.quadraticCurveTo(...P(0.07, 0.86), ...P(0.06, 0.76)); c.quadraticCurveTo(...P(0, 0.72), ...P(-0.06, 0.76));
    c.quadraticCurveTo(...P(-0.07, 0.86), ...P(0, 0.93)); c.closePath(); c.fill();
    c.strokeStyle = o.crack; c.lineWidth = S * 0.035; c.lineCap = "round";
    c.beginPath(); c.moveTo(...P(0.005, 0.89)); c.lineTo(...P(-0.01, 0.85)); c.lineTo(...P(0.012, 0.81)); c.lineTo(...P(0, 0.77)); c.stroke();
    this.glow(x, ground + h * 0.83, h * 0.14, o.crack, 0.55);
    // складки одягу — довгі вертикальні тіні
    c.strokeStyle = rgba("#000000", 0.35); c.lineWidth = S * 0.04;
    [-0.09, -0.03, 0.04, 0.1].forEach((dx, i) => {
      c.beginPath(); c.moveTo(...P(dx * 0.6, 0.68)); c.bezierCurveTo(...P(dx * 0.9, 0.5), ...P(dx * 1.1 + (i % 2 ? 0.01 : -0.01), 0.3), ...P(dx * 1.3, 0.08)); c.stroke();
    });
    // складені руки під грудьми й розірвані чотки, що звисають
    c.fillStyle = o.stone;
    c.beginPath(); c.ellipse(...P(0, 0.6), h * S * 0.05, h * S * 0.035, 0, 0, Math.PI * 2); c.fill();
    c.fillStyle = rgba(o.light, 0.8);
    for (let k = 0; k < 6; k++) { c.beginPath(); c.arc(...P(0.015 + Math.sin(k * 0.9) * 0.012, 0.55 - k * 0.032), S * 0.035, 0, Math.PI * 2); c.fill(); }
    // кант світла по краю каптура
    c.strokeStyle = rgba(o.light, 0.45); c.lineWidth = S * 0.05;
    c.beginPath(); c.moveTo(...P(-0.12, 0.74)); c.bezierCurveTo(...P(-0.17, 0.56), ...P(-0.19, 0.3), ...P(-0.16, 0.1)); c.stroke();
    c.beginPath(); c.moveTo(...P(-0.1, 0.97)); c.bezierCurveTo(...P(-0.12, 0.9), ...P(-0.13, 0.82), ...P(-0.12, 0.74)); c.stroke();
    // вода стікає з одягу
    c.strokeStyle = rgba(o.water, 0.5); c.lineWidth = S * 0.025;
    for (let k = 0; k < 9; k++) {
      const dx = -0.15 + k * 0.037;
      c.beginPath(); c.moveTo(...P(dx, 0.08)); c.quadraticCurveTo(...P(dx + 0.01, -0.04), ...P(dx - 0.01, -0.1 - (k % 3) * 0.04)); c.stroke();
    }
  };

  // Товща води: усе, що намальовано раніше нижче поверхні, тоне в бірюзі; каустика по дну, промені від поверхні,
  // зважений пил і бульбашки. Поверхня — світла жилка з відблисками.
  Frame.prototype.underwater = function (x0, x1, surface, floor, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const [l, t] = this.px(x0, surface), [rr, b] = this.px(x1, floor);
    c.save();
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, rgba(o.top, 0.5)); g.addColorStop(0.5, rgba(o.mid, 0.5)); g.addColorStop(1, rgba(o.deep, 0.72));
    c.fillStyle = g; c.fillRect(l, t, rr - l, b - t);
    c.beginPath(); c.rect(l, t, rr - l, b - t); c.clip();
    // промені від поверхні — косі, м'які
    c.globalCompositeOperation = "screen";
    for (let i = 0; i < 7; i++) {
      const x = x0 + r() * (x1 - x0), w = 0.6 + r() * 1.6;
      const gg = c.createLinearGradient(0, t, 0, b);
      gg.addColorStop(0, rgba(o.ray, 0.22 + r() * 0.12)); gg.addColorStop(1, rgba(o.ray, 0));
      c.fillStyle = gg; c.filter = `blur(${S * 0.3}px)`;
      c.beginPath(); c.moveTo(...this.px(x, surface)); c.lineTo(...this.px(x + w, surface));
      c.lineTo(...this.px(x + w + 2.5, floor)); c.lineTo(...this.px(x + 1.2, floor)); c.closePath(); c.fill();
    }
    c.filter = "none";
    // каустика — сітка світлих кривих біля поверхні і на дні
    c.strokeStyle = rgba(o.caustic, 0.22); c.lineWidth = S * 0.025;
    for (let band = 0; band < 2; band++) {
      const y0 = band ? floor + 0.2 : surface - 1.4, y1 = band ? floor + 1.6 : surface - 0.1;
      for (let i = 0; i < (x1 - x0) * 2.2; i++) {
        const x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0), len = 0.3 + r() * 0.7;
        c.beginPath(); c.moveTo(...this.px(x, y));
        c.quadraticCurveTo(...this.px(x + len * 0.5, y + (r() - 0.5) * 0.4), ...this.px(x + len, y + (r() - 0.5) * 0.2)); c.stroke();
      }
    }
    // зважений пил і бульбашки
    for (let i = 0; i < (x1 - x0) * 3; i++) {
      const [px, py] = this.px(x0 + r() * (x1 - x0), floor + r() * (surface - floor));
      c.fillStyle = rgba(o.caustic, 0.15 + r() * 0.35);
      c.beginPath(); c.arc(px, py, S * (0.01 + r() * 0.03), 0, Math.PI * 2); c.fill();
    }
    c.strokeStyle = rgba(o.caustic, 0.45); c.lineWidth = S * 0.018;
    for (let i = 0; i < 16; i++) {
      const bx = x0 + r() * (x1 - x0), by = floor + 1 + r() * (surface - floor - 1.5);
      for (let k = 0; k < 4; k++) { c.beginPath(); c.arc(...this.px(bx + Math.sin(k + bx) * 0.08, by + k * 0.35), S * (0.04 + k * 0.012), 0, Math.PI * 2); c.stroke(); }
    }
    c.restore();
    // поверхня
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = rgba(o.glint, 0.75); c.lineWidth = S * 0.04;
    c.beginPath(); c.moveTo(l, t);
    for (let x = x0; x <= x1; x += 0.2) c.lineTo(...this.px(x, surface + (fbm(x * 1.3, 1, seed, 2) - 0.5) * 0.08));
    c.stroke();
    c.strokeStyle = rgba(o.glint, 0.45); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 1.4; i++) {
      const x = x0 + r() * (x1 - x0), y = surface + 0.04 + r() * 0.25, len = 0.2 + r() * 0.8;
      c.lineWidth = S * (0.015 + r() * 0.02);
      c.beginPath(); c.moveTo(...this.px(x, y)); c.lineTo(...this.px(x + len, y)); c.stroke();
    }
    c.restore();
  };

  // Ряд хористів-статуй під водою: каптури в ряд, кожен трохи інший.
  Frame.prototype.choirRow = function (x0, x1, ground, h, color, seed) {
    const c = this.c, r = rng(seed);
    c.fillStyle = color;
    for (let x = x0; x < x1; x += h * (0.55 + r() * 0.35)) {
      const hh = h * (0.8 + r() * 0.35), lean = (r() - 0.5) * 0.25, P = (dx, dy) => this.px(x + dx * hh + lean * dy * hh, ground + dy * hh);
      c.beginPath(); c.moveTo(...P(-0.2, 0));
      c.bezierCurveTo(...P(-0.24, 0.4), ...P(-0.16, 0.72), ...P(-0.1, 0.84));
      c.quadraticCurveTo(...P(0, 1.02), ...P(0.1, 0.84));
      c.bezierCurveTo(...P(0.16, 0.72), ...P(0.24, 0.4), ...P(0.2, 0));
      c.closePath(); c.fill();
    }
  };
})();
