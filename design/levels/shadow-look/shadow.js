// Речі Царства Тіні для макета поверх бібліотек Гробниці, Лісу, Села, Столиці й Обсерваторії (звідти —
// зорі, туманність, кристали, острови): чорне сонце з короною, тіньові платформи (видно лише з Місячним
// оком — контур і мерехтіння), око-портал, пасма тіні з безодні, зруйнований міст з арками, шпиль-руїна,
// уламки, що висять у порожнечі. Плавні криві; кожна річ — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Чорне сонце: диск порожнечі, корона рваними м'якими пелюстками, тонке світле кільце по краю.
  Frame.prototype.eclipse = function (x, y, rad, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y);
    this.glow(x, y, rad * 6, o.halo, 0.28);
    this.glow(x, y, rad * 2.6, o.corona, 0.45);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 70; i++) {
      const a = r() * Math.PI * 2, len = rad * (0.4 + r() * r() * 1.8), w = 0.04 + r() * 0.12;
      const sx = px + Math.cos(a) * rad * S, sy = py + Math.sin(a) * rad * S;
      const ex = px + Math.cos(a + (r() - 0.5) * 0.25) * (rad + len) * S, ey = py + Math.sin(a + (r() - 0.5) * 0.25) * (rad + len) * S;
      const g = c.createLinearGradient(sx, sy, ex, ey);
      g.addColorStop(0, rgba(i % 3 ? o.corona : o.flare, 0.55)); g.addColorStop(1, rgba(o.corona, 0));
      c.strokeStyle = g; c.lineWidth = S * w; c.lineCap = "round";
      c.beginPath(); c.moveTo(sx, sy);
      c.quadraticCurveTo(px + Math.cos(a + 0.12) * (rad + len * 0.5) * S, py + Math.sin(a + 0.12) * (rad + len * 0.5) * S, ex, ey); c.stroke();
    }
    c.restore();
    c.fillStyle = o.disc;
    c.beginPath(); c.arc(px, py, rad * S, 0, Math.PI * 2); c.fill();
    c.strokeStyle = rgba(o.rim, 0.9); c.lineWidth = S * 0.06;
    c.beginPath(); c.arc(px, py, rad * S * 1.01, 0, Math.PI * 2); c.stroke();
  };

  // Серп затемнення: тонке кільце світла, з одного боку товще — місяць, що ховається за тінню.
  Frame.prototype.crescentEclipse = function (x, y, rad, o) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    this.glow(x, y, rad * 3.5, o.halo, 0.3);
    c.save();
    c.fillStyle = rgba(o.light, 0.95);
    c.beginPath(); c.arc(px, py, rad * S, 0, Math.PI * 2); c.fill();
    c.globalCompositeOperation = "destination-out";
    c.beginPath(); c.arc(px - rad * S * 0.16, py - rad * S * 0.06, rad * S * 0.97, 0, Math.PI * 2); c.fill();
    c.restore();
    c.fillStyle = o.disc;
    c.beginPath(); c.arc(px - rad * S * 0.16, py - rad * S * 0.06, rad * S * 0.97, 0, Math.PI * 2); c.fill();
    this.glow(x + rad * 0.9, y - rad * 0.2, rad * 0.8, o.light, 0.35);
  };

  // Порожнеча знизу: темрява густішає донизу, у ній — повільні іскри й далекі уламки.
  Frame.prototype.voidBelow = function (top, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [, t] = this.px(0, top);
    const g = c.createLinearGradient(0, t, 0, c.canvas.height);
    g.addColorStop(0, rgba(o.deep, 0)); g.addColorStop(0.6, rgba(o.deep, 0.75)); g.addColorStop(1, rgba(o.deep, 1));
    c.fillStyle = g; c.fillRect(0, t, c.canvas.width, c.canvas.height - t);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 60; i++) {
      const [px, py] = this.px(r() * 32, -1 + r() * (top + 1));
      c.fillStyle = rgba(o.spark, 0.15 + r() * 0.5);
      c.beginPath(); c.arc(px, py, S * (0.01 + r() * 0.03), 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };

  // Пасма тіні: м'які хвилясті язики, що піднімаються з безодні й тануть угорі.
  Frame.prototype.tendrils = function (x0, x1, y, len, color, alpha, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let x = x0; x < x1; x += 0.4 + r() * 1.1) {
      const l = len * (0.4 + r() * 0.8), sway = (r() - 0.5) * 2.4, w = 0.08 + r() * 0.22;
      const [sx, sy] = this.px(x, y), [ex, ey] = this.px(x + sway, y + l);
      const g = c.createLinearGradient(sx, sy, ex, ey);
      g.addColorStop(0, rgba(color, alpha)); g.addColorStop(1, rgba(color, 0));
      c.strokeStyle = g; c.lineWidth = S * w;
      c.beginPath(); c.moveTo(sx, sy);
      c.bezierCurveTo(...this.px(x - sway * 0.6, y + l * 0.35), ...this.px(x + sway * 1.2, y + l * 0.65), ex, ey); c.stroke();
    }
    c.restore();
  };

  // Тіньова платформа: є, але її не видно без Місячного ока — лише світлий контур, мерехтіння й тінь під ним.
  Frame.prototype.shadePlatform = function (x0, x1, y, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), w = x1 - x0;
    const pts = [[x0, y], [x1, y]];
    for (let t = 1; t >= 0; t -= 0.1) {
      pts.push([x0 + w * t, y - 0.25 - Math.sin(Math.PI * t) * (0.5 + fbm(t * 4, y, seed, 2) * 0.5)]);
    }
    c.save();
    this.path(pts);
    c.fillStyle = rgba(o.fill, 0.28); c.fill();
    c.setLineDash([S * 0.18, S * 0.1]);
    c.strokeStyle = rgba(o.edge, 0.55); c.lineWidth = S * 0.03; c.stroke();
    c.setLineDash([]);
    c.restore();
    // світла поверхня, по якій стоять, і мерехтливі іскри над нею
    c.save(); c.globalCompositeOperation = "screen";
    const [l, t] = this.px(x0, y), [rr] = this.px(x1, y);
    const g = c.createLinearGradient(0, t - S * 0.15, 0, t + S * 0.25);
    g.addColorStop(0, rgba(o.edge, 0)); g.addColorStop(0.4, rgba(o.edge, 0.85)); g.addColorStop(1, rgba(o.edge, 0));
    c.fillStyle = g; c.fillRect(l, t - S * 0.15, rr - l, S * 0.4);
    for (let i = 0; i < w * 4; i++) {
      const [px, py] = this.px(x0 + r() * w, y + r() * 0.8);
      c.fillStyle = rgba(o.edge, 0.3 + r() * 0.5);
      c.beginPath(); c.arc(px, py, S * (0.012 + r() * 0.025), 0, Math.PI * 2); c.fill();
    }
    c.restore();
    this.glow((x0 + x1) / 2, y, w * 0.6, o.edge, 0.12);
  };

  // Око-портал: мигдалеподібна рамка з каменю, всередині — райдужка, що світиться, промені навколо.
  Frame.prototype.eyePortal = function (x, y, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y);
    const w = h * 0.42;
    this.glow(x, y, h * 1.4, o.glow, 0.35);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 18; i++) {
      const a = i / 18 * Math.PI * 2 + r() * 0.2, len = h * (0.65 + r() * 0.5);
      c.strokeStyle = rgba(o.glow, 0.18 + r() * 0.2); c.lineWidth = S * 0.03;
      c.beginPath(); c.moveTo(px + Math.cos(a) * w * 1.1 * S, py + Math.sin(a) * h * 0.6 * S);
      c.lineTo(px + Math.cos(a) * len * 0.9 * S, py + Math.sin(a) * len * S); c.stroke();
    }
    c.restore();
    // мигдаль: дві дуги
    const almond = (k) => {
      c.beginPath();
      c.moveTo(px, py - h * 0.5 * S * k);
      c.quadraticCurveTo(px + w * 1.3 * S * k, py, px, py + h * 0.5 * S * k);
      c.quadraticCurveTo(px - w * 1.3 * S * k, py, px, py - h * 0.5 * S * k);
      c.closePath();
    };
    c.fillStyle = o.stone; almond(1.18); c.fill();
    const g = c.createRadialGradient(px, py, 0, px, py, h * 0.5 * S);
    g.addColorStop(0, o.core); g.addColorStop(0.45, o.iris); g.addColorStop(1, o.dark);
    c.fillStyle = g; almond(1); c.fill();
    // зіниця — вертикальна щілина
    c.fillStyle = o.pupil;
    c.beginPath(); c.ellipse(px, py, w * 0.18 * S, h * 0.3 * S, 0, 0, Math.PI * 2); c.fill();
    this.glow(x, y, 0.4, o.core, 0.8);
  };

  // Зруйнований міст: настил (по ньому ходять — окремою масою), під ним арки, деякі прольоти обвалені.
  Frame.prototype.archBridge = function (x0, x1, deck, depth, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.fillStyle = o.stone;
    c.beginPath(); c.moveTo(...this.px(x0, deck));
    c.lineTo(...this.px(x1, deck));
    const span = 3.2 + r() * 0.8;
    let x = x1;
    c.lineTo(...this.px(x1, deck - depth));
    while (x > x0) {
      const nx = Math.max(x0, x - span), pier = 0.5;
      c.lineTo(...this.px(x - pier, deck - depth));
      c.quadraticCurveTo(...this.px((x + nx) / 2, deck - 0.6 - r() * 0.3), ...this.px(nx + pier, deck - depth));
      x = nx;
    }
    c.lineTo(...this.px(x0, deck - depth));
    c.closePath(); c.fill();
    // опори йдуть у порожнечу й обламуються
    for (let px = x0 + 0.1; px < x1; px += span) {
      const len = 1.5 + r() * 4;
      c.beginPath(); c.moveTo(...this.px(px - 0.45, deck - depth)); c.lineTo(...this.px(px + 0.55, deck - depth));
      c.quadraticCurveTo(...this.px(px + 0.4, deck - depth - len * 0.6), ...this.px(px + 0.1 + (r() - 0.5) * 0.4, deck - depth - len));
      c.quadraticCurveTo(...this.px(px - 0.3, deck - depth - len * 0.5), ...this.px(px - 0.45, deck - depth)); c.fill();
    }
  };

  // Готична руїна, що висить у порожнечі: стрілчасті прорізи, шпилі, рваний низ, як у острова.
  Frame.prototype.floatingRuin = function (x, base, w, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.fillStyle = o.stone;
    c.beginPath(); c.moveTo(...this.px(x - w / 2, base));
    // дах: шпилі різної висоти, деякі обламані
    for (let t = 0; t <= 1.0001; t += 1 / 9) {
      const tx = x - w / 2 + w * t, top = base + h * (0.55 + 0.45 * Math.sin(Math.PI * t)) * (0.75 + r() * 0.35);
      c.lineTo(...this.px(tx - w / 30, base + h * 0.5));
      c.quadraticCurveTo(...this.px(tx - w / 60, top * 0.98), ...this.px(tx, top));
      c.quadraticCurveTo(...this.px(tx + w / 60, top * 0.98), ...this.px(tx + w / 30, base + h * 0.5));
    }
    c.lineTo(...this.px(x + w / 2, base));
    // рваний низ
    for (let t = 1; t >= 0; t -= 0.05) {
      const d = h * 0.7 * Math.pow(Math.sin(Math.PI * t), 1.4) * (0.6 + fbm(t * 7, base, seed, 2) * 0.8);
      c.lineTo(...this.px(x - w / 2 + w * t, base - d));
    }
    c.closePath(); c.fill();
    // вікна зі світлом порожнечі
    c.globalCompositeOperation = "source-atop";
    for (let i = 0; i < 4; i++) {
      const wx = x - w * 0.32 + i * w * 0.21;
      c.fillStyle = rgba(o.window, 0.75);
      c.beginPath(); this.pointedArch(wx, w * 0.07, base + h * 0.12, h * 0.42 * (0.8 + r() * 0.3)); c.closePath(); c.fill();
    }
    c.restore();
  };
  // Обламана колона: м'яко звужується, легкий нахил, рваний злам угорі; без склепіння.
  Frame.prototype.brokenColumn = function (x, base, h, w, color, light, seed) {
    const c = this.c, S = this.S, r = rng(seed), lean = (r() - 0.5) * 0.5, top = base + h;
    c.fillStyle = color;
    c.beginPath();
    c.moveTo(...this.px(x - w * 0.75, base));
    c.quadraticCurveTo(...this.px(x - w * 0.5, base + 0.4), ...this.px(x - w * 0.5, base + 0.9));
    c.bezierCurveTo(...this.px(x - w * 0.42 + lean * 0.3, base + h * 0.4), ...this.px(x - w * 0.45 + lean * 0.7, base + h * 0.8), ...this.px(x - w * 0.42 + lean, top));
    // злам: кілька м'яких зубців
    const n = 4;
    for (let i = 1; i <= n; i++) {
      const t = i / n;
      c.lineTo(...this.px(x - w * 0.42 + lean + w * 0.84 * t, top + (i % 2 ? 0.25 + r() * 0.6 : -0.1 - r() * 0.4)));
    }
    c.bezierCurveTo(...this.px(x + w * 0.45 + lean * 0.7, base + h * 0.8), ...this.px(x + w * 0.42 + lean * 0.3, base + h * 0.4), ...this.px(x + w * 0.5, base + 0.9));
    c.quadraticCurveTo(...this.px(x + w * 0.5, base + 0.4), ...this.px(x + w * 0.75, base));
    c.closePath(); c.fill();
    // світла смуга з боку затемнення — об'єм
    c.strokeStyle = rgba(light, 0.35); c.lineWidth = S * w * 0.12; c.lineCap = "round";
    c.beginPath(); c.moveTo(...this.px(x + w * 0.28, base + 1)); c.quadraticCurveTo(...this.px(x + w * 0.3 + lean * 0.5, base + h * 0.5), ...this.px(x + w * 0.26 + lean, top - 0.4)); c.stroke();
  };

  // Статуя без обличчя: каптур, складки плаща, руки складені; на місці лиця — порожнеча.
  Frame.prototype.hooded = function (x, ground, h, color, light) {
    const c = this.c, S = this.S, P = (dx, dy) => this.px(x + dx * h, ground + dy * h);
    c.fillStyle = color;
    c.beginPath(); c.moveTo(...P(-0.3, 0));
    c.bezierCurveTo(...P(-0.26, 0.35), ...P(-0.2, 0.7), ...P(-0.16, 0.82));
    c.bezierCurveTo(...P(-0.16, 0.98), ...P(-0.04, 1.06), ...P(0.02, 1.04));
    c.bezierCurveTo(...P(0.14, 1.0), ...P(0.18, 0.9), ...P(0.15, 0.8));
    c.bezierCurveTo(...P(0.2, 0.6), ...P(0.27, 0.3), ...P(0.32, 0));
    c.closePath(); c.fill();
    c.fillStyle = "#020104";
    c.beginPath(); c.ellipse(...P(0.0, 0.88), h * 0.07 * S, h * 0.09 * S, 0, 0, Math.PI * 2); c.fill();
    c.strokeStyle = rgba(light, 0.4); c.lineWidth = S * 0.04;
    [-0.1, 0.05, 0.16].forEach(d => { c.beginPath(); c.moveTo(...P(d, 0.62)); c.quadraticCurveTo(...P(d * 1.3, 0.3), ...P(d * 1.6, 0.02)); c.stroke(); });
  };
})();
