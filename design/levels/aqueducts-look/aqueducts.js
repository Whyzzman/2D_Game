// Речі Акведуків для макета поверх бібліотек Гробниці, Лісу, Села, Столиці й Боліт: ряди арок акведука,
// водяні колеса, зелені вентилі шлюзів, бірюзові відблиски води на камені, водомети-левові голови,
// течія на поверхні, статуя Владаря з тризубом на троні. Плавні криві; кожна річ — зі свого seed.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Ряд арок акведука: суцільна маса з напівкруглими прорізами (evenodd — крізь арки видно туман),
  // верх із жолобом, частина прольотів обвалена — з них цівками стікає вода.
  Frame.prototype.aqueductRow = function (o) {
    const c = this.c, S = this.S, r = rng(o.seed);
    const spans = [];
    for (let x = o.from; x < o.to; x += o.span) spans.push({ x, broken: r() < (o.broken || 0), drop: 0.6 + r() * 1.4 });
    c.save();
    c.beginPath();
    // верхній край: рівний жолоб, у обвалених прольотах — м'який вируб
    c.moveTo(...this.px(o.from, o.base));
    c.lineTo(...this.px(o.from, o.top));
    spans.forEach(sp => {
      const x0 = sp.x, x1 = sp.x + o.span;
      if (sp.broken) {
        c.lineTo(...this.px(x0 + o.span * 0.18, o.top));
        c.bezierCurveTo(...this.px(x0 + o.span * 0.3, o.top - sp.drop), ...this.px(x0 + o.span * 0.7, o.top - sp.drop),
          ...this.px(x0 + o.span * 0.82, o.top));
      }
      c.lineTo(...this.px(x1, o.top + (fbm(x1 * 0.4, 1, o.seed, 2) - 0.5) * 0.15));
    });
    c.lineTo(...this.px(o.to + o.span, o.base));
    c.closePath();
    // прорізи: нижній ярус — великі арки, верхній — малі
    (o.tiers || [{ base: o.base, spring: o.base + (o.top - o.base) * 0.55, ratio: 0.72 }]).forEach(t => {
      spans.forEach(sp => {
        const cx = sp.x + o.span / 2, half = o.span * t.ratio / 2;
        c.moveTo(...this.px(cx - half, t.base));
        c.lineTo(...this.px(cx - half, t.spring));
        // напівкругле склепіння з кривих
        c.bezierCurveTo(...this.px(cx - half, t.spring + half * 0.55), ...this.px(cx - half * 0.55, t.spring + half),
          ...this.px(cx, t.spring + half));
        c.bezierCurveTo(...this.px(cx + half * 0.55, t.spring + half), ...this.px(cx + half, t.spring + half * 0.55),
          ...this.px(cx + half, t.spring));
        c.lineTo(...this.px(cx + half, t.base));
        c.closePath();
      });
    });
    c.fillStyle = o.color;
    c.fill("evenodd");
    // світлий кант жолоба
    c.strokeStyle = rgba(o.rim, 0.35); c.lineWidth = S * 0.05;
    c.beginPath(); c.moveTo(...this.px(o.from, o.top)); c.lineTo(...this.px(o.to + o.span, o.top)); c.stroke();
    c.restore();
    // вода з обвалених прольотів і з жолоба
    if (o.spill) spans.forEach(sp => {
      if (!sp.broken) return;
      const x = sp.x + o.span / 2;
      this.thinFall(x, o.top - sp.drop, o.spill.to, 0.18 + r() * 0.2, o.spill, sp.x * 13 + o.seed);
    });
  };

  // Тонка цівка води: кілька м'яких смуг, що розходяться донизу, і бризки.
  Frame.prototype.thinFall = function (x, top, bottom, w, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen"; c.lineCap = "round";
    for (let i = 0; i < 5; i++) {
      const dx = (r() - 0.5) * w, a = o.alpha * (0.4 + r() * 0.6);
      const [, gt] = this.px(0, top), [, gb] = this.px(0, bottom);
      const g = c.createLinearGradient(0, gt, 0, gb);
      g.addColorStop(0, rgba(o.water, a)); g.addColorStop(0.7, rgba(o.water, a * 0.6)); g.addColorStop(1, rgba(o.water, 0));
      c.strokeStyle = g; c.lineWidth = S * (0.02 + r() * 0.045);
      const wob = (r() - 0.5) * 0.25;
      c.beginPath(); c.moveTo(...this.px(x + dx * 0.3, top));
      c.bezierCurveTo(...this.px(x + dx * 0.5 + wob, top - (top - bottom) * 0.3), ...this.px(x + dx - wob, top - (top - bottom) * 0.7),
        ...this.px(x + dx * 1.6 + wob * 0.5, bottom));
      c.stroke();
    }
    c.restore();
    this.glow(x, bottom + 0.1, 0.7 + w * 2, o.water, o.alpha * 0.6);
  };

  // Водяне колесо: обід, спиці, лопаті-ковші; нижня частина ховається у воду (малюється до води).
  Frame.prototype.waterWheel = function (x, y, rad, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y), turn = r() * Math.PI;
    c.save();
    c.strokeStyle = o.wood; c.lineCap = "round";
    // два ободи
    c.lineWidth = S * 0.22;
    c.beginPath(); c.arc(px, py, rad * S, 0, Math.PI * 2); c.stroke();
    c.lineWidth = S * 0.12;
    c.beginPath(); c.arc(px, py, rad * S * 0.78, 0, Math.PI * 2); c.stroke();
    // спиці
    const n = o.spokes || 8;
    c.lineWidth = S * 0.13;
    for (let i = 0; i < n; i++) {
      const a = turn + i / n * Math.PI * 2;
      c.beginPath(); c.moveTo(px, py); c.lineTo(px + Math.cos(a) * rad * S * 0.98, py + Math.sin(a) * rad * S * 0.98); c.stroke();
    }
    // лопаті — дощечки по ободу, з них капає
    c.fillStyle = o.wood;
    const m = o.paddles || 16;
    for (let i = 0; i < m; i++) {
      const a = turn + i / m * Math.PI * 2, bx = px + Math.cos(a) * rad * S, by = py + Math.sin(a) * rad * S;
      c.save(); c.translate(bx, by); c.rotate(a);
      c.beginPath(); c.moveTo(0, -S * 0.05); c.quadraticCurveTo(S * 0.45, -S * 0.12, S * 0.55, S * 0.05);
      c.lineTo(0, S * 0.08); c.closePath(); c.fill();
      c.restore();
    }
    // маточина з відблиском
    c.fillStyle = o.hub;
    c.beginPath(); c.arc(px, py, S * rad * 0.16, 0, Math.PI * 2); c.fill();
    c.strokeStyle = rgba(o.rim, 0.5); c.lineWidth = S * 0.04;
    c.beginPath(); c.arc(px, py, rad * S * 1.02, Math.PI * 1.1, Math.PI * 1.55); c.stroke();
    c.restore();
    // цівки з верхніх лопатей праворуч — колесо крутиться
    for (let i = 0; i < 4; i++) {
      const a = -0.2 - i * 0.28 - r() * 0.1;
      const bx = x + Math.cos(a) * rad, by = y - Math.sin(a) * rad;
      if (by > y) this.thinFall(bx + 0.3, by, y - rad * 0.4, 0.12, { water: o.drip, alpha: 0.45 }, seed + i * 7);
    }
  };

  // Вентиль шлюзу: зелене колесо на стіні — його видно здалеку, світиться, як на карті.
  Frame.prototype.valve = function (x, y, rad, o) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    this.glow(x, y, rad * 4.2, o.glow, 0.35);
    c.save();
    // труба, на якій сидить вентиль
    c.fillStyle = o.pipe;
    c.fillRect(px - S * rad * 0.22, py, S * rad * 0.44, S * rad * 2.2);
    c.beginPath(); c.ellipse(px, py + S * rad * 2.2, S * rad * 0.5, S * rad * 0.18, 0, 0, Math.PI * 2); c.fill();
    c.strokeStyle = o.ring; c.lineWidth = S * rad * 0.2;
    c.beginPath(); c.arc(px, py, rad * S, 0, Math.PI * 2); c.stroke();
    c.lineWidth = S * rad * 0.12;
    for (let i = 0; i < 5; i++) {
      const a = 0.3 + i / 5 * Math.PI * 2;
      c.beginPath(); c.moveTo(px, py); c.lineTo(px + Math.cos(a) * rad * S, py + Math.sin(a) * rad * S); c.stroke();
    }
    c.fillStyle = o.ring;
    c.beginPath(); c.arc(px, py, rad * S * 0.22, 0, Math.PI * 2); c.fill();
    c.restore();
    this.glow(x, y, rad * 1.2, o.core, 0.5);
  };

  // Бірюзові відблиски води на камені: хвилясті світлі нитки, що повторюють рух поверхні.
  Frame.prototype.caustics = function (x0, x1, y0, y1, color, alpha, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen"; c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * (y1 - y0) * 0.35; i++) {
      const x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0), len = 0.4 + r() * 1.2;
      const k = 1 - (y - y0) / (y1 - y0);
      c.strokeStyle = rgba(color, alpha * k * (0.4 + r() * 0.6)); c.lineWidth = S * (0.02 + r() * 0.03);
      c.beginPath(); c.moveTo(...this.px(x, y));
      c.bezierCurveTo(...this.px(x + len * 0.3, y + 0.15), ...this.px(x + len * 0.6, y - 0.15), ...this.px(x + len, y + (r() - 0.5) * 0.1));
      c.stroke();
    }
    c.restore();
  };

  // Течія: видовжені світлі мазки на поверхні в один бік і складки води за ними.
  Frame.prototype.current = function (x0, x1, surface, dir, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 2.2; i++) {
      const x = x0 + r() * (x1 - x0), y = surface - 0.05 - r() * r() * 0.9, len = (0.6 + r() * 1.4) * dir;
      c.strokeStyle = rgba(color, (0.25 + r() * 0.45) * (1 - (surface - y)));
      c.lineWidth = S * (0.025 + r() * 0.035);
      c.beginPath(); c.moveTo(...this.px(x, y)); c.quadraticCurveTo(...this.px(x + len * 0.5, y + 0.06), ...this.px(x + len, y)); c.stroke();
    }
    c.restore();
  };

  // Водомет: кам'яна голова лева в стіні й дуга струменя, що б'є у воду.
  Frame.prototype.spout = function (x, y, dir, reach, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y);
    c.save();
    c.translate(px, py); c.scale(dir, 1);
    c.fillStyle = o.stone;
    c.beginPath();
    c.moveTo(-S * 0.5, -S * 0.7);
    c.bezierCurveTo(S * 0.1, -S * 0.95, S * 0.55, -S * 0.5, S * 0.55, -S * 0.05);
    c.bezierCurveTo(S * 0.55, S * 0.4, S * 0.15, S * 0.7, -S * 0.5, S * 0.7);
    c.closePath(); c.fill();
    // грива — м'які пасма
    c.strokeStyle = o.mane; c.lineWidth = S * 0.08; c.lineCap = "round";
    for (let i = 0; i < 6; i++) {
      const a = -1.2 + i * 0.48;
      c.beginPath(); c.moveTo(-S * 0.1 + Math.cos(a) * S * 0.35, Math.sin(a) * S * 0.45);
      c.quadraticCurveTo(-S * 0.45 + Math.cos(a) * S * 0.5, Math.sin(a) * S * 0.7, -S * 0.55, Math.sin(a) * S * 0.75); c.stroke();
    }
    // паща
    c.fillStyle = "#050607";
    c.beginPath(); c.ellipse(S * 0.42, S * 0.12, S * 0.14, S * 0.1, 0, 0, Math.PI * 2); c.fill();
    c.restore();
    // струмінь: щільна дуга й бризки там, де вдаряє
    const sx = x + dir * 0.5, sy = y - 0.12, ex = x + dir * reach, ey = o.to;
    c.save(); c.globalCompositeOperation = "screen"; c.lineCap = "round";
    for (let i = 0; i < 6; i++) {
      c.strokeStyle = rgba(o.water, 0.25 + r() * 0.35); c.lineWidth = S * (0.05 + r() * 0.09);
      c.beginPath(); c.moveTo(...this.px(sx, sy + (r() - 0.5) * 0.08));
      c.bezierCurveTo(...this.px(sx + dir * reach * 0.45, sy + 0.5 + r() * 0.2), ...this.px(ex - dir * 0.3, sy - (sy - ey) * 0.4),
        ...this.px(ex + (r() - 0.5) * 0.4, ey));
      c.stroke();
    }
    c.restore();
    this.glow(ex, ey + 0.1, 1.2, o.water, 0.35);
  };

  // Колона, що стоїть у воді: стовбур із м'якими боками, капітель, мокра смуга й водорості біля води.
  Frame.prototype.wetColumn = function (x, base, top, w, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.fillStyle = o.stone;
    c.beginPath();
    c.moveTo(...this.px(x - w / 2, base));
    c.bezierCurveTo(...this.px(x - w / 2 - 0.05, (base + top) / 2), ...this.px(x - w / 2 + 0.05, top - 1), ...this.px(x - w / 2 - 0.25, top));
    c.lineTo(...this.px(x + w / 2 + 0.25, top));
    c.bezierCurveTo(...this.px(x + w / 2 - 0.05, top - 1), ...this.px(x + w / 2 + 0.05, (base + top) / 2), ...this.px(x + w / 2, base));
    c.closePath(); c.fill();
    c.fillStyle = rgba(o.light, 0.12);
    c.fillRect(...this.px(x - w * 0.3, top - 0.3), w * 0.12 * S, (top - base - 0.3) * S);
    // мокра темна смуга й водорості над водою
    const g = c.createLinearGradient(0, this.px(0, o.water + 1.2)[1], 0, this.px(0, o.water)[1]);
    g.addColorStop(0, "rgba(0,0,0,0)"); g.addColorStop(1, rgba(o.wet, 0.8));
    c.fillStyle = g; c.fillRect(...this.px(x - w / 2, o.water + 1.2), w * S, 1.2 * S);
    c.strokeStyle = rgba(o.weed, 0.7); c.lineCap = "round";
    for (let i = 0; i < 7; i++) {
      const sx = x - w / 2 + r() * w;
      c.lineWidth = S * (0.03 + r() * 0.04);
      c.beginPath(); c.moveTo(...this.px(sx, o.water + 0.9 + r() * 0.3));
      c.quadraticCurveTo(...this.px(sx + (r() - 0.5) * 0.3, o.water + 0.4), ...this.px(sx + (r() - 0.5) * 0.2, o.water - 0.1)); c.stroke();
    }
  };

  // Статуя Владаря Шлюзів на троні: висока спинка-вежа, сидяча постать у плащі, корона-хвилі,
  // тризуб у правиці; з тріщин очей стікає світла вода. Силует — вертикальний клин.
  Frame.prototype.lordStatue = function (x, ground, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const u = h / 10;
    // спинка трону — стрілчаста, з різьбленими хвилями
    c.fillStyle = o.throne;
    c.beginPath();
    c.moveTo(...this.px(x - 2.2 * u, ground));
    c.lineTo(...this.px(x - 2.0 * u, ground + 7.2 * u));
    c.bezierCurveTo(...this.px(x - 1.8 * u, ground + 9.4 * u), ...this.px(x - 0.5 * u, ground + 10.4 * u), ...this.px(x, ground + 11 * u));
    c.bezierCurveTo(...this.px(x + 0.5 * u, ground + 10.4 * u), ...this.px(x + 1.8 * u, ground + 9.4 * u), ...this.px(x + 2.0 * u, ground + 7.2 * u));
    c.lineTo(...this.px(x + 2.2 * u, ground));
    c.closePath(); c.fill();
    c.strokeStyle = rgba(o.rim, 0.25); c.lineWidth = S * 0.05;
    for (let k = 0; k < 4; k++) {
      const yy = ground + (6.2 + k * 1.0) * u;
      c.beginPath(); c.moveTo(...this.px(x - 1.5 * u, yy));
      c.bezierCurveTo(...this.px(x - 0.8 * u, yy + 0.35 * u), ...this.px(x - 0.3 * u, yy - 0.35 * u), ...this.px(x, yy));
      c.bezierCurveTo(...this.px(x + 0.3 * u, yy + 0.35 * u), ...this.px(x + 0.8 * u, yy - 0.35 * u), ...this.px(x + 1.5 * u, yy)); c.stroke();
    }
    // постать: коліна, плащ, плечі, голова
    c.fillStyle = o.stone;
    c.beginPath();
    c.moveTo(...this.px(x - 1.9 * u, ground));
    c.bezierCurveTo(...this.px(x - 2.1 * u, ground + 2.4 * u), ...this.px(x - 1.6 * u, ground + 3.6 * u), ...this.px(x - 1.5 * u, ground + 3.8 * u));
    c.lineTo(...this.px(x - 1.3 * u, ground + 6.3 * u));
    c.bezierCurveTo(...this.px(x - 1.1 * u, ground + 6.9 * u), ...this.px(x - 0.5 * u, ground + 7.0 * u), ...this.px(x - 0.38 * u, ground + 7.1 * u));
    // голова з короною-хвилями
    c.bezierCurveTo(...this.px(x - 0.5 * u, ground + 7.9 * u), ...this.px(x - 0.4 * u, ground + 8.5 * u), ...this.px(x - 0.5 * u, ground + 8.8 * u));
    c.quadraticCurveTo(...this.px(x - 0.3 * u, ground + 8.6 * u), ...this.px(x - 0.2 * u, ground + 9.1 * u));
    c.quadraticCurveTo(...this.px(x - 0.05 * u, ground + 8.7 * u), ...this.px(x + 0.1 * u, ground + 9.25 * u));
    c.quadraticCurveTo(...this.px(x + 0.25 * u, ground + 8.7 * u), ...this.px(x + 0.45 * u, ground + 9.0 * u));
    c.quadraticCurveTo(...this.px(x + 0.45 * u, ground + 8.4 * u), ...this.px(x + 0.42 * u, ground + 7.9 * u));
    c.bezierCurveTo(...this.px(x + 0.42 * u, ground + 7.4 * u), ...this.px(x + 0.4 * u, ground + 7.1 * u), ...this.px(x + 0.38 * u, ground + 7.1 * u));
    c.bezierCurveTo(...this.px(x + 0.6 * u, ground + 7.0 * u), ...this.px(x + 1.2 * u, ground + 6.9 * u), ...this.px(x + 1.35 * u, ground + 6.2 * u));
    c.lineTo(...this.px(x + 1.55 * u, ground + 3.8 * u));
    c.bezierCurveTo(...this.px(x + 1.7 * u, ground + 3.6 * u), ...this.px(x + 2.15 * u, ground + 2.4 * u), ...this.px(x + 1.95 * u, ground));
    c.closePath(); c.fill();
    // світло з тріщин очей і тріщина по грудях
    this.glow(x - 0.15 * u, ground + 7.75 * u, 0.6 * u, o.eye, 0.85);
    this.glow(x + 0.18 * u, ground + 7.75 * u, 0.6 * u, o.eye, 0.85);
    c.strokeStyle = rgba(o.eye, 0.7); c.lineWidth = S * 0.04; c.lineCap = "round";
    c.beginPath(); c.moveTo(...this.px(x + 0.1 * u, ground + 6.8 * u));
    c.lineTo(...this.px(x - 0.15 * u, ground + 5.8 * u)); c.lineTo(...this.px(x + 0.2 * u, ground + 5.0 * u)); c.lineTo(...this.px(x - 0.05 * u, ground + 3.9 * u)); c.stroke();
    // сльози-цівки з очей
    [-0.15, 0.18].forEach((dx, i) => this.thinFall(x + dx * u, ground + 7.6 * u, ground + 3.8 * u, 0.06, { water: o.eye, alpha: 0.5 }, seed + i));
    // тризуб у правиці: держак від підлоги, три зубці з вістрями
    const tx = x + 2.6 * u;
    c.strokeStyle = o.bronze; c.lineWidth = S * 0.16 * u;
    c.beginPath(); c.moveTo(...this.px(tx, ground)); c.lineTo(...this.px(tx, ground + 9.2 * u)); c.stroke();
    c.lineWidth = S * 0.12 * u;
    c.beginPath();
    c.moveTo(...this.px(tx - 0.8 * u, ground + 10.6 * u));
    c.bezierCurveTo(...this.px(tx - 0.8 * u, ground + 9.4 * u), ...this.px(tx - 0.5 * u, ground + 9.1 * u), ...this.px(tx, ground + 9.1 * u));
    c.bezierCurveTo(...this.px(tx + 0.5 * u, ground + 9.1 * u), ...this.px(tx + 0.8 * u, ground + 9.4 * u), ...this.px(tx + 0.8 * u, ground + 10.6 * u));
    c.moveTo(...this.px(tx, ground + 9.1 * u)); c.lineTo(...this.px(tx, ground + 11.0 * u));
    c.stroke();
    c.fillStyle = o.bronze;
    [-0.8, 0, 0.8].forEach(dx => {
      const top = ground + (dx === 0 ? 11.0 : 10.6) * u;
      c.beginPath(); c.moveTo(...this.px(tx + dx * u - 0.18 * u, top - 0.05 * u));
      c.lineTo(...this.px(tx + dx * u, top + 0.55 * u)); c.lineTo(...this.px(tx + dx * u + 0.18 * u, top - 0.05 * u)); c.closePath(); c.fill();
    });
    // рука, що тримає держак
    c.fillStyle = o.stone;
    c.beginPath(); c.ellipse(...this.px(tx - 0.05 * u, ground + 4.6 * u), 0.42 * u * S, 0.32 * u * S, 0, 0, Math.PI * 2); c.fill();
    c.beginPath();
    c.moveTo(...this.px(x + 1.3 * u, ground + 6.0 * u)); c.quadraticCurveTo(...this.px(x + 2.4 * u, ground + 5.8 * u), ...this.px(tx - 0.2 * u, ground + 4.9 * u));
    c.lineTo(...this.px(tx - 0.2 * u, ground + 4.3 * u)); c.quadraticCurveTo(...this.px(x + 2.0 * u, ground + 4.9 * u), ...this.px(x + 1.4 * u, ground + 5.0 * u));
    c.closePath(); c.fill();
    this.glow(tx, ground + 10.4 * u, 1.6 * u, o.eye, 0.3);
    void r;
  };

  // Мокрі пасма водоростей, що звисають з краю склепіння.
  Frame.prototype.weedCurtain = function (x0, x1, y, len, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let x = x0; x < x1; x += 0.1 + r() * 0.35) {
      if (r() < 0.4) continue;
      const l = len * (0.25 + r() * 0.9), sway = (fbm(x, y, seed, 2) - 0.5) * 0.5;
      c.strokeStyle = rgba(color, 0.4 + r() * 0.4); c.lineWidth = S * (0.025 + r() * 0.04);
      c.beginPath(); c.moveTo(...this.px(x, y));
      c.bezierCurveTo(...this.px(x + sway * 0.2, y - l * 0.4), ...this.px(x + sway, y - l * 0.7), ...this.px(x + sway * 0.7, y - l)); c.stroke();
      if (r() < 0.3) this.glow(x + sway * 0.7, y - l, 0.12, "#bff4ee", 0.6);   // крапля на кінці
    }
    c.restore();
  };

  // Краплі, що летять униз: короткі світлі риски.
  Frame.prototype.drips = function (count, x0, x1, y0, y1, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen"; c.lineCap = "round";
    for (let i = 0; i < count; i++) {
      const x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0), len = 0.12 + r() * 0.25;
      c.strokeStyle = rgba(color, 0.3 + r() * 0.5); c.lineWidth = S * (0.015 + r() * 0.02);
      c.beginPath(); c.moveTo(...this.px(x, y)); c.lineTo(...this.px(x, y - len)); c.stroke();
    }
    c.restore();
  };
})();
