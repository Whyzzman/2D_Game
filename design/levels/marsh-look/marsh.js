// Болотні речі для макета поверх бібліотек Гробниці, Лісу, Села й Столиці: ядовита вода, що світиться,
// мох-борода, палі, клітка на ланцюгу, очерет, бульбашки, похилий корабель, надгробки в воді.
// Плавні криві; кожна річ — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Ядовита вода: поверхня світиться зеленим (небезпека читається світлом), глибина чорна,
  // по поверхні — плівка, відблиски й бульбашки. Світло з води підсвічує все над нею.
  Frame.prototype.toxicWater = function (x0, x1, surface, floor, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const [l, t] = this.px(x0, surface), [rr, b] = this.px(x1, floor);
    this.glow((x0 + x1) / 2, surface + 0.4, (x1 - x0) * 0.55, o.glow, 0.28);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, rgba(o.surface, 0.92)); g.addColorStop(0.18, rgba(o.water, 0.95)); g.addColorStop(1, rgba(o.deep, 1));
    c.fillStyle = g; c.fillRect(l, t, rr - l, b - t);
    // плівка: м'які плями світлішого зеленого, що пливуть по поверхні
    c.save();
    c.beginPath(); c.rect(l, t - S * 0.1, rr - l, S * 0.9); c.clip();
    for (let x = x0; x < x1; x += 0.6 + r() * 0.9) {
      const [px, py] = this.px(x, surface - 0.12 - r() * 0.3);
      const gg = c.createRadialGradient(px, py, 0, px, py, S * (0.6 + r() * 0.9));
      gg.addColorStop(0, rgba(o.film, 0.35 + r() * 0.2)); gg.addColorStop(1, rgba(o.film, 0));
      c.fillStyle = gg; c.beginPath(); c.ellipse(px, py, S * 1.6, S * 0.25, 0, 0, Math.PI * 2); c.fill();
    }
    c.restore();
    // відблиски
    c.strokeStyle = rgba(o.glint, 0.6); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 1.6; i++) {
      const x = x0 + r() * (x1 - x0), y = surface - r() * r() * 1.0, len = 0.2 + r() * 0.8;
      c.lineWidth = S * (0.02 + r() * 0.025);
      c.globalAlpha = 1 - (surface - y);
      c.beginPath(); c.moveTo(...this.px(x, y)); c.lineTo(...this.px(x + len, y)); c.stroke();
    }
    c.globalAlpha = 1;
    // бульбашки на поверхні
    for (let i = 0; i < (x1 - x0) * 0.8; i++) {
      const x = x0 + r() * (x1 - x0), rad = 0.05 + r() * 0.12;
      const [px, py] = this.px(x, surface + rad * 0.6);
      c.strokeStyle = rgba(o.glint, 0.55); c.lineWidth = S * 0.02;
      c.beginPath(); c.arc(px, py, rad * S, Math.PI, 0); c.stroke();
    }
    c.strokeStyle = rgba(o.glint, 0.7); c.lineWidth = S * 0.035;
    c.beginPath(); c.moveTo(l, t); c.lineTo(rr, t); c.stroke();
  };

  // Мох-борода: пасма, що звисають з гілки чи балки, хитаються від шуму.
  Frame.prototype.beardMoss = function (x0, x1, y, len, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let x = x0; x < x1; x += 0.12 + r() * 0.3) {
      if (r() < 0.35) continue;
      const l = len * (0.3 + r() * 0.9), sway = (fbm(x, y, seed, 2) - 0.5) * 0.8;
      c.strokeStyle = rgba(color, 0.35 + r() * 0.45); c.lineWidth = S * (0.03 + r() * 0.05);
      c.beginPath(); c.moveTo(...this.px(x, y));
      c.bezierCurveTo(...this.px(x + sway * 0.3, y - l * 0.35), ...this.px(x + sway, y - l * 0.7), ...this.px(x + sway * 0.6, y - l));
      c.stroke();
    }
    c.restore();
  };

  // Палі: криві чорні стовпи з водяною лінією й слизом.
  Frame.prototype.stilts = function (xs, top, bottom, color, slime, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    xs.forEach((x, i) => {
      const lean = (r() - 0.5) * 0.25, w = 0.18 + r() * 0.12;
      c.fillStyle = color;
      c.beginPath();
      c.moveTo(...this.px(x - w, top)); c.lineTo(...this.px(x + w, top));
      c.quadraticCurveTo(...this.px(x + w + lean * 0.5, (top + bottom) / 2), ...this.px(x + w * 0.8 + lean, bottom));
      c.lineTo(...this.px(x - w * 0.8 + lean, bottom));
      c.quadraticCurveTo(...this.px(x - w + lean * 0.5, (top + bottom) / 2), ...this.px(x - w, top));
      c.closePath(); c.fill();
      // слиз біля води
      c.fillStyle = rgba(slime, 0.5);
      c.fillRect(...this.px(x - w * 0.9 + lean, bottom + 0.9), w * 1.8 * S, 0.5 * S);
    });
  };

  // Хата на палях: дощаті стіни, провисла солом'яна стріха, вікно з болотним вогником.
  Frame.prototype.stiltHut = function (x, floor, w, h, o, seed) {
    this.stilts([x + 0.4, x + w * 0.5, x + w - 0.4], floor, floor - 4, o.beam, o.slime, seed);
    this.house(x, floor, w, h, o, seed);
  };

  // Клітка на ланцюгу: ґратчастий купол, у ній — щось, що колись було людиною.
  Frame.prototype.cage = function (x, y, h, o) {
    const c = this.c, S = this.S;
    this.chain(x, y + h, y + h + 4, o.chain, 0.8);
    c.strokeStyle = o.iron; c.lineWidth = S * 0.06;
    for (let i = -3; i <= 3; i++) {
      const dx = i * 0.22;
      c.beginPath(); c.moveTo(...this.px(x + dx, y));
      c.quadraticCurveTo(...this.px(x + dx * 1.2, y + h * 0.8), ...this.px(x, y + h));
      c.stroke();
    }
    c.beginPath(); c.ellipse(...this.px(x, y), 0.75 * S, 0.12 * S, 0, 0, Math.PI * 2); c.stroke();
    c.beginPath(); c.ellipse(...this.px(x, y + h * 0.5), 0.75 * S, 0.12 * S, 0, 0, Math.PI * 2); c.stroke();
    c.fillStyle = o.bones;
    c.beginPath(); c.ellipse(...this.px(x - 0.1, y + 0.35), 0.18 * S, 0.2 * S, 0, 0, Math.PI * 2); c.fill();
    c.fillRect(...this.px(x - 0.3, y + 0.15), 0.6 * S, 0.08 * S);
  };

  // Очерет і рогіз біля води.
  Frame.prototype.reeds = function (x0, x1, y, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let x = x0; x < x1; x += 0.08 + r() * 0.25) {
      const h = 0.6 + r() * 1.6, bend = (r() - 0.5) * 0.6;
      c.strokeStyle = rgba(color, 0.6 + r() * 0.4); c.lineWidth = S * (0.025 + r() * 0.03);
      c.beginPath(); c.moveTo(...this.px(x, y)); c.quadraticCurveTo(...this.px(x + bend * 0.3, y + h * 0.6), ...this.px(x + bend, y + h)); c.stroke();
      if (r() < 0.2) {
        c.fillStyle = color; c.beginPath();
        c.ellipse(...this.px(x + bend * 0.85, y + h * 0.88), S * 0.05, S * 0.16, -bend * 0.4, 0, Math.PI * 2); c.fill();
      }
    }
    c.restore();
  };

  // Похилий корабель: корпус кривою, ребра шпангоутів у пробоїні, зламана щогла.
  Frame.prototype.wreck = function (x, water, len, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.fillStyle = o.hull;
    c.beginPath();
    c.moveTo(...this.px(x, water + 2.6));
    c.quadraticCurveTo(...this.px(x + len * 0.15, water - 1.4), ...this.px(x + len * 0.55, water - 1.2));
    c.quadraticCurveTo(...this.px(x + len * 0.9, water - 0.6), ...this.px(x + len, water + 1.8));
    c.lineTo(...this.px(x + len * 0.96, water + 2.2));
    c.lineTo(...this.px(x + 0.4, water + 3.0));
    c.closePath(); c.fill();
    // пробоїна з ребрами
    c.globalCompositeOperation = "destination-out";
    c.beginPath(); c.ellipse(...this.px(x + len * 0.35, water + 0.6), len * 0.11 * S, 0.9 * S, 0.1, 0, Math.PI * 2); c.fill();
    c.globalCompositeOperation = "source-over";
    c.strokeStyle = o.hull; c.lineWidth = S * 0.12;
    for (let k = -2; k <= 2; k++) {
      const bx = x + len * 0.35 + k * 0.35;
      c.beginPath(); c.moveTo(...this.px(bx, water - 0.2)); c.quadraticCurveTo(...this.px(bx + 0.15, water + 0.6), ...this.px(bx, water + 1.5)); c.stroke();
    }
    // планки обшивки
    c.strokeStyle = rgba(o.plank, 0.5); c.lineWidth = S * 0.03;
    for (let k = 0; k < 5; k++) {
      c.beginPath(); c.moveTo(...this.px(x + 0.4, water + 2.6 - k * 0.6));
      c.quadraticCurveTo(...this.px(x + len * 0.5, water - 0.8 - k * 0.1 + k * 0.55), ...this.px(x + len * 0.97, water + 2.0 - k * 0.4)); c.stroke();
    }
    // зламана щогла з реєю, на ній — мох
    c.strokeStyle = o.hull; c.lineWidth = S * 0.22;
    const mx = x + len * 0.62;
    c.beginPath(); c.moveTo(...this.px(mx, water + 2.4)); c.quadraticCurveTo(...this.px(mx + 0.4, water + 6), ...this.px(mx + 1.2, water + 9.2)); c.stroke();
    c.lineWidth = S * 0.12;
    c.beginPath(); c.moveTo(...this.px(mx - 1.6, water + 7.6)); c.lineTo(...this.px(mx + 2.6, water + 8.4)); c.stroke();
    c.restore();
    this.beardMoss(mx - 1.4, mx + 2.4, water + 7.9, 1.4, o.moss, seed + 3);
  };

  // Надгробок, наполовину в воді, похилий.
  Frame.prototype.gravestone = function (x, y, h, tilt, color) {
    const c = this.c, S = this.S;
    c.save();
    const [px, py] = this.px(x, y);
    c.translate(px, py); c.rotate(tilt);
    c.fillStyle = color;
    c.beginPath(); c.moveTo(-0.35 * S, 0); c.lineTo(-0.35 * S, -h * 0.7 * S);
    c.quadraticCurveTo(0, -h * 1.1 * S, 0.35 * S, -h * 0.7 * S); c.lineTo(0.35 * S, 0); c.closePath(); c.fill();
    c.restore();
  };

  // Болотні вогники: м'які зелені кульки світла над водою.
  Frame.prototype.wisps = function (count, x0, x1, y0, y1, color, seed) {
    const r = rng(seed);
    for (let i = 0; i < count; i++) {
      const x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0);
      this.glow(x, y, 0.5 + r() * 0.6, color, 0.5);
      this.glow(x, y, 0.12, "#f2ffd0", 0.9);
    }
  };
})();
