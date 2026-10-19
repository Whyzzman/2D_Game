// Лісові речі для макета поверх бібліотеки Гробниці (../tomb-look/paint.js): місяць, мертві дуби,
// руїна абатства без даху, мох на кантах, коріння, гриби. Усе — плавні криві, без повторів: кожне
// дерево й корінь ростуть зі свого seed.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Місяць за туманом: диск і широке гало.
  Frame.prototype.moon = function (x, y, r, color) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    this.glow(x, y, r * 9, color, 0.22);
    this.glow(x, y, r * 3, color, 0.35);
    c.save();
    c.fillStyle = rgba(color, 0.92);
    c.beginPath(); c.arc(px, py, r * S, 0, Math.PI * 2); c.fill();
    c.globalCompositeOperation = "multiply";
    const rr = rng(77);
    for (let i = 0; i < 6; i++) {
      c.fillStyle = "rgba(170,180,175,0.35)";
      c.beginPath();
      c.arc(px + (rr() - 0.5) * r * S * 1.1, py + (rr() - 0.5) * r * S * 1.1, r * S * (0.12 + rr() * 0.18), 0, Math.PI * 2);
      c.fill();
    }
    c.restore();
  };

  // Гілка: товщина спадає до кінця, напрям гнеться шумом, від неї — менші гілки.
  function branch(f, x, y, angle, len, width, seed, depth, color) {
    const c = f.c, S = f.S, r = rng(seed);
    let cx = x, cy = y, a = angle;
    const steps = Math.max(3, Math.round(len / 0.35));
    const pts = [[cx, cy]];
    for (let i = 1; i <= steps; i++) {
      a += (fbm(cx * 0.8, cy * 0.8, seed, 2) - 0.5) * 0.5;
      cx += Math.cos(a) * len / steps; cy += Math.sin(a) * len / steps;
      pts.push([cx, cy]);
    }
    c.strokeStyle = color; c.lineCap = "round"; c.lineJoin = "round";
    for (let i = 1; i < pts.length; i++) {
      const t = i / pts.length;
      c.lineWidth = Math.max(0.6, width * (1 - t * 0.85) * S);
      const [ax, ay] = f.px(pts[i - 1][0], pts[i - 1][1]), [bx, by] = f.px(pts[i][0], pts[i][1]);
      c.beginPath(); c.moveTo(ax, ay); c.lineTo(bx, by); c.stroke();
    }
    if (depth <= 0) return;
    const forks = 2 + Math.floor(r() * 2);
    for (let k = 0; k < forks; k++) {
      const at = pts[Math.floor(pts.length * (0.35 + r() * 0.55))];
      const side = r() < 0.5 ? -1 : 1;
      branch(f, at[0], at[1], angle + side * (0.4 + r() * 0.7), len * (0.4 + r() * 0.3),
        width * 0.55, seed * 7 + k + 1, depth - 1, color);
    }
  }

  // Мертвий дуб: стовбур, що розширюється до коріння, і крона з голих гілок.
  Frame.prototype.deadOak = function (x, ground, height, width, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const lean = (r() - 0.5) * 0.6;
    c.save();
    c.fillStyle = color;
    c.beginPath();
    const [l0x, l0y] = this.px(x - width * 1.6, ground - 0.2);
    c.moveTo(l0x, l0y);
    c.quadraticCurveTo(...this.px(x - width * 0.55, ground + 0.4), ...this.px(x - width * 0.5 + lean, ground + height * 0.55));
    c.lineTo(...this.px(x - width * 0.32 + lean * 1.4, ground + height));
    c.lineTo(...this.px(x + width * 0.32 + lean * 1.4, ground + height));
    c.lineTo(...this.px(x + width * 0.5 + lean, ground + height * 0.55));
    c.quadraticCurveTo(...this.px(x + width * 0.55, ground + 0.4), ...this.px(x + width * 1.7, ground - 0.2));
    c.closePath(); c.fill();
    const top = [x + lean * 1.4, ground + height];
    const n = 3 + Math.floor(r() * 3);
    for (let i = 0; i < n; i++) {
      const a = Math.PI / 2 + (r() - 0.5) * 1.9;
      branch(this, top[0], top[1] - r() * height * 0.25, a, height * (0.3 + r() * 0.35), width * 0.45,
        seed * 13 + i, 2, color);
    }
    c.restore();
  };

  // Ряд дерев одного плану, кожне своє.
  Frame.prototype.treeRow = function (o) {
    const r = rng(o.seed);
    for (let x = o.from; x < o.to; x += o.gap * (0.6 + r() * 0.8)) {
      this.deadOak(x, o.ground, o.height * (0.75 + r() * 0.5), o.width * (0.7 + r() * 0.6), o.color, o.seed * 31 + Math.round(x * 10));
    }
  };

  // Руїна абатства: стрілчасті вікна без скла, небо крізь кістяк, верх обламаний.
  Frame.prototype.abbey = function (x0, x1, base, top, wall, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.beginPath();
    c.moveTo(...this.px(x0, base));
    let y = top;
    for (let x = x0; x <= x1; x += 0.4) {
      y = top - Math.pow(fbm(x * 0.5, 0, seed, 3), 2) * 4.5 - (x > (x0 + x1) / 2 ? (x - (x0 + x1) / 2) * 0.35 : 0);
      c.lineTo(...this.px(x, y));
    }
    c.lineTo(...this.px(x1, base));
    c.closePath();
    c.fillStyle = wall; c.fill();
    c.globalCompositeOperation = "destination-out";
    const span = (x1 - x0) / 4;
    for (let i = 0; i < 4; i++) {
      const cx = x0 + span * (i + 0.5);
      c.beginPath();
      this.pointedArch(cx, span * 0.55, base + 1.2, (top - base) * (0.55 + r() * 0.1));
      c.fill();
    }
    c.restore();
  };

  // Мох на канті: м'які зелені подушки, що звисають за край.
  Frame.prototype.moss = function (x0, x1, y, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    for (let x = x0; x < x1; x += 0.25 + r() * 0.5) {
      if (r() < 0.3) continue;
      const [px, py] = this.px(x, y);
      const w = S * (0.25 + r() * 0.5), h = S * (0.08 + r() * 0.1);
      c.fillStyle = rgba(color, 0.65 + r() * 0.3);
      c.beginPath(); c.ellipse(px, py, w, h, 0, Math.PI, 0); c.fill();
      if (r() < 0.35) {
        const drop = S * (0.2 + r() * 0.6);
        c.strokeStyle = rgba(color, 0.5); c.lineWidth = S * 0.035; c.lineCap = "round";
        c.beginPath(); c.moveTo(px, py + S * 0.05);
        c.quadraticCurveTo(px + S * 0.08, py + drop * 0.6, px - S * 0.02, py + drop); c.stroke();
      }
    }
    c.restore();
  };

  // Корінь: товста крива від точки в точку, з відгалуженнями.
  Frame.prototype.root = function (pts, width, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.strokeStyle = color; c.lineCap = "round";
    c.lineWidth = width * S;
    c.beginPath();
    c.moveTo(...this.px(pts[0][0], pts[0][1]));
    for (let i = 1; i < pts.length - 1; i++) {
      const mx = (pts[i][0] + pts[i + 1][0]) / 2, my = (pts[i][1] + pts[i + 1][1]) / 2;
      c.quadraticCurveTo(...this.px(pts[i][0], pts[i][1]), ...this.px(mx, my));
    }
    const last = pts[pts.length - 1];
    c.lineTo(...this.px(last[0], last[1]));
    c.stroke();
    for (let i = 0; i < pts.length - 1; i++) {
      if (r() < 0.5) continue;
      const p = pts[i];
      branch(this, p[0], p[1], -Math.PI / 2 + (r() - 0.5) * 1.6, 0.8 + r() * 1.6, width * 0.4, seed * 5 + i, 1, color);
    }
    c.restore();
  };

  // Гриб, що світиться.
  Frame.prototype.fungus = function (x, y, size, color, glow) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    this.glow(x, y + size * 0.3, size * 4, glow, 0.45);
    c.save();
    c.fillStyle = color;
    c.fillRect(px - S * size * 0.08, py - S * size * 0.5, S * size * 0.16, S * size * 0.5);
    c.beginPath(); c.ellipse(px, py - S * size * 0.5, S * size * 0.4, S * size * 0.22, 0, Math.PI, 0); c.fill();
    c.restore();
  };
})();
