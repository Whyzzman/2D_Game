// Речі Обсерваторії для макета поверх бібліотек Гробниці, Лісу, Села й Столиці: зоряне небо й туманності,
// летючі острови з кришталем знизу, оррері, полиці з книгами, телескоп, купол із ребрами, лінзи й зоряні
// містки, сузір'я. Плавні криві; кожен острів, полиця й туманність — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Зоряне небо: дрібні зорі різної яскравості й кілька великих із м'якими променями-хрестиками.
  Frame.prototype.stars = function (count, y0, y1, color, seed, big) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < count; i++) {
      const x = r() * 32, y = y0 + r() * (y1 - y0), a = 0.15 + r() * r() * 0.85;
      const [px, py] = this.px(x, y);
      c.fillStyle = rgba(color, a);
      c.beginPath(); c.arc(px, py, S * (0.008 + r() * r() * 0.035), 0, Math.PI * 2); c.fill();
    }
    c.restore();
    for (let i = 0; i < (big || 0); i++) {
      const x = 1 + r() * 30, y = y0 + r() * (y1 - y0), s = 0.25 + r() * 0.45;
      this.glow(x, y, s * 2.4, color, 0.35);
      const [px, py] = this.px(x, y);
      c.save(); c.globalCompositeOperation = "screen";
      const g = c.createRadialGradient(px, py, 0, px, py, s * S);
      g.addColorStop(0, rgba(color, 0.9)); g.addColorStop(1, rgba(color, 0));
      c.strokeStyle = g; c.lineWidth = S * 0.025; c.lineCap = "round";
      c.beginPath(); c.moveTo(px - s * S, py); c.lineTo(px + s * S, py); c.moveTo(px, py - s * S); c.lineTo(px, py + s * S); c.stroke();
      c.restore();
    }
  };

  // Туманність: витягнуті м'які плями двох-трьох кольорів, що течуть смугою під кутом.
  Frame.prototype.nebula = function (x, y, len, thick, angle, colors, alpha, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 26; i++) {
      const t = r() - 0.5, off = (fbm(t * 3, i, seed, 2) - 0.5) * thick * 2;
      const cx = x + Math.cos(angle) * t * len - Math.sin(angle) * off;
      const cy = y + Math.sin(angle) * t * len + Math.cos(angle) * off;
      const [px, py] = this.px(cx, cy), rad = S * thick * (0.4 + r() * 0.8);
      const g = c.createRadialGradient(px, py, 0, px, py, rad);
      const col = colors[i % colors.length];
      g.addColorStop(0, rgba(col, alpha * (0.35 + r() * 0.65))); g.addColorStop(1, rgba(col, 0));
      c.fillStyle = g;
      c.beginPath(); c.ellipse(px, py, rad * 1.9, rad * 0.8, -angle, 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };

  // Кристал: видовжений, з однією гранню-відблиском і світлом на кінці. angle 0 — вістрям униз.
  Frame.prototype.crystal = function (x, y, len, w, angle, o) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save(); c.translate(px, py); c.rotate(angle);
    const g = c.createLinearGradient(0, 0, 0, len * S);
    g.addColorStop(0, o.dark); g.addColorStop(0.65, o.mid); g.addColorStop(1, o.light);
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(-w * S / 2, 0); c.lineTo(w * S / 2, 0);
    c.quadraticCurveTo(w * S * 0.45, len * S * 0.5, w * S * 0.22, len * S * 0.8);
    c.lineTo(0, len * S); c.lineTo(-w * S * 0.3, len * S * 0.72);
    c.quadraticCurveTo(-w * S * 0.5, len * S * 0.4, -w * S / 2, 0);
    c.closePath(); c.fill();
    c.strokeStyle = rgba(o.light, 0.45); c.lineWidth = S * 0.02;
    c.beginPath(); c.moveTo(w * S * 0.08, 0); c.quadraticCurveTo(w * S * 0.12, len * S * 0.5, 0, len * S); c.stroke();
    c.restore();
    const tip = [x + Math.sin(-angle) * len, y - Math.cos(angle) * len];
    this.glow(tip[0], tip[1], w * 1.6 + 0.3, o.glow, 0.45);
  };

  // Летючий острів: рівний верх (по ньому стоять), низ звужується м'якими зубцями, з нього звисає кришталь.
  Frame.prototype.island = function (x0, x1, y, depth, look, cryst, seed) {
    const r = rng(seed), w = x1 - x0, pts = [[x0, y], [x1, y]];
    // низ — м'яка цибулина з кілем трохи збоку від середини; край острова закруглений, не прямий
    const keel = 0.35 + r() * 0.3, n = 16;
    const under = t => {
      const bulb = Math.pow(Math.sin(Math.PI * t), 0.75);
      const spike = Math.exp(-Math.pow((t - keel) / 0.16, 2));
      return depth * (0.45 * bulb + 0.55 * spike) * (0.8 + fbm(t * 5, y, seed, 2) * 0.4);
    };
    for (let i = 1; i < n; i++) {
      const t = 1 - i / n;
      pts.push([x0 + w * t, y - 0.25 - under(t)]);
    }
    if (cryst) {
      for (let i = 0; i < 2 + w * 0.7; i++) {
        const t = 0.15 + r() * 0.7;
        this.crystal(x0 + w * t, y - 0.1 - under(t) * 0.85, 0.5 + r() * 1.5, 0.16 + r() * 0.24, (r() - 0.5) * 0.6, cryst);
      }
    }
    this.mass({ points: pts, tops: [0], seed, erosion: 0.3 }, look);
  };

  // Далекий острів — лише силует із туманним краєм і вогником на вершині.
  Frame.prototype.farIsland = function (x, y, w, depth, color, seed, light) {
    const c = this.c, r = rng(seed);
    c.fillStyle = color;
    c.beginPath(); c.moveTo(...this.px(x - w / 2, y));
    for (let t = 0; t <= 1.0001; t += 0.05) {
      const top = y + Math.sin(Math.PI * t) * (0.4 + r() * 0.3) * w * 0.12;
      c.lineTo(...this.px(x - w / 2 + w * t, top));
    }
    for (let t = 1; t >= 0; t -= 0.05) {
      const d = depth * Math.pow(Math.sin(Math.PI * t), 1.6) * (0.7 + fbm(t * 6, y, seed, 2) * 0.6);
      c.lineTo(...this.px(x - w / 2 + w * t, y - d));
    }
    c.closePath(); c.fill();
    if (light) { this.glow(x + (r() - 0.5) * w * 0.5, y + 0.6, 1.2, light, 0.45); }
  };

  // Оррері: латунні орбіти під різними нахилами, планети на них, сонце в центрі, тримач зі стелі.
  Frame.prototype.orrery = function (x, y, rad, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y);
    this.glow(x, y, rad * 1.8, o.glow, 0.3);
    c.save();
    c.strokeStyle = o.brass; c.lineWidth = S * 0.07;
    c.beginPath(); c.moveTo(px, py); c.lineTo(...this.px(x, 18.5)); c.stroke();
    for (let i = 0; i < 5; i++) {
      const rx = rad * (0.32 + i * 0.17), ry = rx * (0.18 + r() * 0.2), tilt = (r() - 0.5) * 0.6;
      c.strokeStyle = rgba(o.brass, 0.85 - i * 0.12); c.lineWidth = S * (0.05 - i * 0.006);
      c.beginPath(); c.ellipse(px, py, rx * S, ry * S, tilt, 0, Math.PI * 2); c.stroke();
      const a = r() * Math.PI * 2, ct = Math.cos(tilt), st = Math.sin(tilt);
      const ex = Math.cos(a) * rx * S, ey = Math.sin(a) * ry * S;
      const bx = px + ex * ct - ey * st, by = py + ex * st + ey * ct;
      c.strokeStyle = rgba(o.brass, 0.5); c.lineWidth = S * 0.02;
      c.beginPath(); c.moveTo(px, py); c.lineTo(bx, by); c.stroke();
      const pr = S * (0.1 + r() * 0.14);
      const g = c.createRadialGradient(bx - pr * 0.3, by - pr * 0.3, 0, bx, by, pr);
      g.addColorStop(0, o.planets[i % o.planets.length]); g.addColorStop(1, "#0c0a18");
      c.fillStyle = g; c.beginPath(); c.arc(bx, by, pr, 0, Math.PI * 2); c.fill();
    }
    c.fillStyle = o.sun; c.beginPath(); c.arc(px, py, S * rad * 0.13, 0, Math.PI * 2); c.fill();
    c.restore();
    this.glow(x, y, rad * 0.55, o.sun, 0.75);
  };

  // Книжкові полиці: стіна полиць під стрілчастим верхом; корінці різної висоти, похилені, прогалини.
  Frame.prototype.shelves = function (x0, x1, base, top, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.beginPath(); this.pointedArch((x0 + x1) / 2, x1 - x0, base, top - base); c.closePath(); c.clip();
    c.fillStyle = o.wood; c.fillRect(...this.px(x0, top), (x1 - x0) * S, (top - base) * S);
    for (let y = base + 0.3; y < top - 0.6; y += 1.0 + r() * 0.25) {
      let x = x0 + 0.05;
      while (x < x1 - 0.1) {
        if (r() < 0.07) { x += 0.3 + r() * 0.6; continue; }
        const bw = 0.07 + r() * 0.11, bh = 0.5 + r() * 0.35, lean = r() < 0.07 ? (r() - 0.5) * 0.5 : 0;
        const [bx, by] = this.px(x, y + 0.06);
        c.save(); c.translate(bx, by); c.rotate(lean);
        c.fillStyle = shade(o.books[Math.floor(r() * o.books.length)], (fbm(x, y, seed, 2) - 0.5) * 0.6);
        c.fillRect(0, -bh * S, bw * S, bh * S);
        if (r() < 0.3) { c.fillStyle = rgba(o.gilt, 0.55); c.fillRect(0, -bh * S * 0.8, bw * S, S * 0.025); }
        c.restore();
        x += bw + 0.012;
      }
      c.fillStyle = o.board; c.fillRect(...this.px(x0, y + 0.06), (x1 - x0) * S, 0.09 * S);
    }
    // тінь угорі, щоб полиці йшли в темряву склепіння
    const [, t] = this.px(0, top), [, b] = this.px(0, base);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, "rgba(4,3,12,0.9)"); g.addColorStop(0.5, "rgba(4,3,12,0.25)"); g.addColorStop(1, "rgba(4,3,12,0.55)");
    c.fillStyle = g; c.fillRect(...this.px(x0, top), (x1 - x0) * S, (top - base) * S);
    c.restore();
  };

  function shade(hex, d) {
    const n = [1, 3, 5].map(i => parseInt(hex.slice(i, i + 2), 16));
    return "rgb(" + n.map(v => Math.max(0, Math.min(255, Math.round(v * (1 + d))))).join(",") + ")";
  }

  // Вікно-зореглядне: стрілчасте, без скла, за ним — туманність і зорі; рама з тонким ажуром.
  Frame.prototype.starWindow = function (x, y, w, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.beginPath(); this.pointedArch(x, w, y, h); c.closePath(); c.clip();
    const [, t] = this.px(0, y + h), [, b] = this.px(0, y);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, o.skyTop); g.addColorStop(1, o.skyBottom);
    c.fillStyle = g; c.fillRect(...this.px(x - w, y + h), w * 2 * S, h * S);
    this.nebula(x, y + h * 0.55, h * 1.1, w * 0.35, 1.1 + r() * 0.5, o.nebula, 0.5, seed + 1);
    for (let i = 0; i < 40 * w; i++) {
      const [px, py] = this.px(x + (r() - 0.5) * w, y + r() * h);
      c.fillStyle = rgba(o.star, 0.3 + r() * 0.7);
      c.beginPath(); c.arc(px, py, S * (0.01 + r() * r() * 0.04), 0, Math.PI * 2); c.fill();
    }
    c.restore();
    this.glow(x, y + h * 0.4, w * 1.4, o.glow, 0.22);
    c.strokeStyle = o.frame; c.lineWidth = S * 0.16;
    c.beginPath(); this.pointedArch(x, w, y, h); c.stroke();
    c.lineWidth = S * 0.06;
    c.beginPath(); c.moveTo(...this.px(x, y)); c.lineTo(...this.px(x, y + h - w * 0.4)); c.stroke();
    c.beginPath(); c.arc(...this.px(x, y + h - w * 0.55), w * S * 0.2, 0, Math.PI * 2); c.stroke();
  };

  // Сузір'я: зорі, з'єднані тонкими золотими лініями, — на склепінні, на стіні чи в небі.
  Frame.prototype.constellation = function (pts, color, alpha) {
    const c = this.c, S = this.S;
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = rgba(color, alpha * 0.55); c.lineWidth = S * 0.025; c.setLineDash([S * 0.12, S * 0.08]);
    c.beginPath(); pts.forEach((p, i) => { const [px, py] = this.px(p[0], p[1]); i ? c.lineTo(px, py) : c.moveTo(px, py); }); c.stroke();
    c.setLineDash([]);
    c.restore();
    pts.forEach(p => { this.glow(p[0], p[1], 0.45, color, alpha); this.glow(p[0], p[1], 0.1, "#fff6dc", alpha); });
  };

  // Купол: напівсфера-силует із ребрами, щілиною для телескопа і світлом у щілині.
  Frame.prototype.dome = function (x, base, rad, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, base);
    // барабан під куполом — м'яко звужується донизу
    c.fillStyle = o.body;
    c.beginPath(); c.moveTo(...this.px(x - rad * 1.06, base)); c.lineTo(...this.px(x + rad * 1.06, base));
    c.quadraticCurveTo(...this.px(x + rad * 0.95, base - rad * 0.3), ...this.px(x + rad * 0.8, base - rad * 0.55));
    c.lineTo(...this.px(x - rad * 0.8, base - rad * 0.55));
    c.quadraticCurveTo(...this.px(x - rad * 0.95, base - rad * 0.3), ...this.px(x - rad * 1.06, base)); c.closePath(); c.fill();
    const g = c.createRadialGradient(px - rad * S * 0.35, py - rad * S * 0.7, 0, px, py, rad * S * 1.1);
    g.addColorStop(0, o.light); g.addColorStop(1, o.body);
    c.fillStyle = g;
    c.beginPath(); c.ellipse(px, py, rad * S, rad * 0.92 * S, 0, Math.PI, 0); c.fill();
    c.save();
    c.beginPath(); c.ellipse(px, py, rad * S, rad * 0.92 * S, 0, Math.PI, 0); c.clip();
    c.strokeStyle = rgba(o.rib, 0.35); c.lineWidth = S * 0.04;
    for (let k = -3; k <= 3; k++) {
      c.beginPath(); c.ellipse(px, py, Math.abs(k) / 3.5 * rad * S + 1, rad * 0.92 * S, 0, Math.PI, 0); c.stroke();
    }
    // щілина з зоряним світлом
    const sw = rad * 0.16, tilt = (r() - 0.5) * 0.2;
    const [sx0, sy0] = this.px(x, base + rad), [, sy1] = this.px(x, base + rad * 0.15);
    const sg = c.createLinearGradient(0, sy0, 0, sy1);
    sg.addColorStop(0, rgba(o.slit, 0.15)); sg.addColorStop(1, rgba(o.slit, 0.75));
    c.fillStyle = sg;
    c.beginPath(); c.moveTo(...this.px(x - sw / 2 + tilt, base + rad * 0.15)); c.lineTo(...this.px(x - sw / 4, base + rad));
    c.lineTo(...this.px(x + sw / 4, base + rad)); c.lineTo(...this.px(x + sw / 2 + tilt, base + rad * 0.15)); c.closePath(); c.fill();
    void sx0;
    c.restore();
    this.glow(x, base + rad * 0.6, rad * 0.5, o.slit, 0.4);
    // підвіконня-галерея і вогники на ній
    c.fillStyle = o.body;
    for (let i = 0; i < 7; i++) {
      const wx = x - rad + (i + 0.5) * rad * 2 / 7;
      if (r() < 0.45) this.glow(wx, base + 0.4, 0.6, o.lamp, 0.55);
    }
  };

  // Телескоп: довга латунна труба на поворотній основі, націлена в щілину; кільця на трубі, лінза світиться.
  Frame.prototype.telescope = function (x, ground, len, angle, o) {
    const c = this.c, S = this.S, [px, py] = this.px(x, ground + 1.8);
    c.save();
    c.fillStyle = o.iron;
    c.beginPath(); c.moveTo(...this.px(x - 1.2, ground)); c.quadraticCurveTo(...this.px(x, ground + 0.6), ...this.px(x + 1.2, ground));
    c.closePath(); c.fill();
    c.fillRect(...this.px(x - 0.2, ground + 1.9), 0.4 * S, 1.9 * S);
    c.translate(px, py); c.rotate(-angle);
    const g = c.createLinearGradient(0, -S * 0.5, 0, S * 0.5);
    g.addColorStop(0, o.brassLight); g.addColorStop(0.45, o.brass); g.addColorStop(1, o.iron);
    c.fillStyle = g;
    c.beginPath();
    c.moveTo(-len * 0.3 * S, -0.28 * S); c.lineTo(len * 0.7 * S, -0.5 * S);
    c.quadraticCurveTo(len * 0.72 * S, 0, len * 0.7 * S, 0.5 * S);
    c.lineTo(-len * 0.3 * S, 0.28 * S); c.closePath(); c.fill();
    c.strokeStyle = o.iron; c.lineWidth = S * 0.06;
    [0.05, 0.3, 0.55].forEach(t => {
      const xx = (-0.3 + t) * len * S, hh = (0.28 + t * 0.22) * S;
      c.beginPath(); c.moveTo(xx, -hh - 0.05 * S); c.lineTo(xx, hh + 0.05 * S); c.stroke();
    });
    c.restore();
    const tip = [x + Math.cos(angle) * len * 0.7, ground + 1.8 + Math.sin(angle) * len * 0.7];
    this.glow(tip[0], tip[1], 1.2, o.lens, 0.6);
  };

  // Лінза на підставці: латунне кільце з кристалом; світиться, коли міст увімкнено.
  Frame.prototype.lens = function (x, ground, o, lit) {
    const c = this.c, S = this.S;
    c.fillStyle = o.brass;
    c.beginPath(); c.moveTo(...this.px(x - 0.35, ground)); c.lineTo(...this.px(x - 0.08, ground + 1.1));
    c.lineTo(...this.px(x + 0.08, ground + 1.1)); c.lineTo(...this.px(x + 0.35, ground)); c.closePath(); c.fill();
    if (lit) this.glow(x, ground + 1.55, 1.6, o.light, 0.6);
    c.strokeStyle = o.brass; c.lineWidth = S * 0.08;
    c.beginPath(); c.arc(...this.px(x, ground + 1.55), S * 0.42, 0, Math.PI * 2); c.stroke();
    const [px, py] = this.px(x, ground + 1.55);
    const g = c.createRadialGradient(px - S * 0.1, py - S * 0.1, 0, px, py, S * 0.38);
    g.addColorStop(0, lit ? "#ffffff" : o.dim); g.addColorStop(1, rgba(lit ? o.light : o.dim, 0.3));
    c.fillStyle = g; c.beginPath(); c.arc(px, py, S * 0.36, 0, Math.PI * 2); c.fill();
  };

  // Зоряний місток: смуга світла від лінзи, по якій можна пройти; руни біжать уздовж.
  Frame.prototype.lightBridge = function (x0, x1, y, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const [l, t] = this.px(x0, y), [rr] = this.px(x1, y);
    c.save(); c.globalCompositeOperation = "screen";
    const g = c.createLinearGradient(0, t - S * 0.25, 0, t + S * 0.7);
    g.addColorStop(0, rgba(o.edge, 0)); g.addColorStop(0.3, rgba(o.edge, 0.95)); g.addColorStop(0.45, rgba(o.core, 0.55)); g.addColorStop(1, rgba(o.core, 0));
    c.fillStyle = g;
    c.beginPath(); c.moveTo(l, t - S * 0.25); c.lineTo(rr, t - S * 0.25); c.lineTo(rr - S * 0.3, t + S * 0.7); c.lineTo(l + S * 0.3, t + S * 0.7); c.closePath(); c.fill();
    c.strokeStyle = rgba(o.edge, 0.7); c.lineWidth = S * 0.025;
    for (let x = x0 + 0.4; x < x1 - 0.4; x += 0.5 + r() * 0.6) {
      const [px, py] = this.px(x, y - 0.25);
      c.beginPath(); c.moveTo(px - S * 0.06, py - S * 0.08); c.lineTo(px + S * 0.06, py + S * 0.08);
      c.moveTo(px + S * 0.06, py - S * 0.08); c.quadraticCurveTo(px, py, px - S * 0.03, py + S * 0.08); c.stroke();
    }
    c.restore();
    this.glow((x0 + x1) / 2, y + 0.2, (x1 - x0) * 0.25, o.core, 0.12);
  };

  // Орбіта: пунктирний еліпс — шлях острова, що пливе; позначки там, де він буде.
  Frame.prototype.orbit = function (x, y, rx, ry, tilt, color, alpha) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = rgba(color, alpha); c.lineWidth = S * 0.03; c.setLineDash([S * 0.25, S * 0.18]);
    c.beginPath(); c.ellipse(px, py, rx * S, ry * S, tilt, 0, Math.PI * 2); c.stroke();
    c.restore();
  };

  // Падаюча зоря: м'який хвіст, що тоншає.
  Frame.prototype.meteor = function (x, y, len, angle, color) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save(); c.globalCompositeOperation = "screen";
    const ex = px - Math.cos(angle) * len * S, ey = py + Math.sin(angle) * len * S;
    const g = c.createLinearGradient(px, py, ex, ey);
    g.addColorStop(0, rgba(color, 0.95)); g.addColorStop(1, rgba(color, 0));
    c.strokeStyle = g; c.lineCap = "round"; c.lineWidth = S * 0.07;
    c.beginPath(); c.moveTo(px, py); c.lineTo(ex, ey); c.stroke();
    c.restore();
    this.glow(x, y, 0.6, color, 0.8);
  };

  // Летючий фоліант: розкрита книга в повітрі, сторінки світяться, літери злітають.
  Frame.prototype.tome = function (x, y, size, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y), s = size * S;
    this.glow(x, y + 0.2, size * 2.2, o.glow, 0.4);
    c.save(); c.translate(px, py);
    c.fillStyle = o.cover;
    c.beginPath(); c.moveTo(0, 0); c.quadraticCurveTo(-s * 0.6, -s * 0.35, -s * 1.1, -s * 0.15); c.lineTo(-s * 1.0, s * 0.18);
    c.quadraticCurveTo(-s * 0.55, s * 0.05, 0, s * 0.25); c.quadraticCurveTo(s * 0.55, s * 0.05, s * 1.0, s * 0.18);
    c.lineTo(s * 1.1, -s * 0.15); c.quadraticCurveTo(s * 0.6, -s * 0.35, 0, 0); c.fill();
    c.fillStyle = o.page;
    c.beginPath(); c.moveTo(0, -s * 0.02); c.quadraticCurveTo(-s * 0.55, -s * 0.3, -s * 0.95, -s * 0.12); c.quadraticCurveTo(-s * 0.5, -s * 0.05, 0, s * 0.12);
    c.quadraticCurveTo(s * 0.5, -s * 0.05, s * 0.95, -s * 0.12); c.quadraticCurveTo(s * 0.55, -s * 0.3, 0, -s * 0.02); c.fill();
    c.restore();
    for (let i = 0; i < 7; i++) {
      const lx = x + (r() - 0.5) * size * 1.6, ly = y + 0.3 + r() * size * 1.4;
      this.glow(lx, ly, 0.12 + r() * 0.1, o.glow, 0.8);
    }
  };
})();
