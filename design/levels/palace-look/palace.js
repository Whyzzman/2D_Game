// Палацові речі для макета поверх бібліотек Гробниці, Лісу, Села, Столиці й Боліт: балюстрада, ротонда,
// кипариси, квітучі дерева, підстрижений живопліт, люстри зі свічками, багряні гардини, дзеркала,
// примарні пари в танці. Плавні криві; кожна річ — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Балюстрада: поручень, під ним балясини-вазочки, через кілька — стовпчик (часом з урною).
  Frame.prototype.balustrade = function (x0, x1, y, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.fillStyle = o.stone;
    // цоколь і поручень
    c.fillRect(...this.px(x0, y + 0.18), (x1 - x0) * S, 0.18 * S);
    c.beginPath();
    c.moveTo(...this.px(x0, y + h)); c.lineTo(...this.px(x1, y + h));
    c.quadraticCurveTo(...this.px(x1 + 0.1, y + h - 0.12), ...this.px(x1, y + h - 0.22));
    c.lineTo(...this.px(x0, y + h - 0.22)); c.closePath(); c.fill();
    // балясини: вузька шийка, кругле черевце — крива, не прямокутник
    let k = 0;
    for (let x = x0 + 0.3; x < x1 - 0.2; x += 0.42) {
      k++;
      const bx = x, top = y + h - 0.22, bot = y + 0.18, mid = bot + (top - bot) * 0.38;
      if (k % 9 === 0) {
        // стовпчик
        c.fillRect(...this.px(bx - 0.18, top + 0.1), 0.36 * S, (top - bot + 0.1) * S);
        if (r() < 0.5) {
          c.beginPath(); c.ellipse(...this.px(bx, top + 0.42), 0.2 * S, 0.24 * S, 0, 0, Math.PI * 2); c.fill();
          c.fillRect(...this.px(bx - 0.05, top + 0.75), 0.1 * S, 0.14 * S);
        }
        continue;
      }
      c.beginPath();
      c.moveTo(...this.px(bx - 0.05, top));
      c.bezierCurveTo(...this.px(bx - 0.05, mid + 0.3), ...this.px(bx - 0.15, mid + 0.1), ...this.px(bx - 0.13, mid));
      c.bezierCurveTo(...this.px(bx - 0.12, bot + 0.1), ...this.px(bx - 0.08, bot), ...this.px(bx - 0.1, bot));
      c.lineTo(...this.px(bx + 0.1, bot));
      c.bezierCurveTo(...this.px(bx + 0.08, bot), ...this.px(bx + 0.12, bot + 0.1), ...this.px(bx + 0.13, mid));
      c.bezierCurveTo(...this.px(bx + 0.15, mid + 0.1), ...this.px(bx + 0.05, mid + 0.3), ...this.px(bx + 0.05, top));
      c.closePath(); c.fill();
    }
    // місячний відблиск на поручні
    c.strokeStyle = rgba(o.light, 0.55); c.lineWidth = S * 0.035;
    c.beginPath(); c.moveTo(...this.px(x0, y + h - 0.02)); c.lineTo(...this.px(x1, y + h - 0.02)); c.stroke();
    c.restore();
  };

  // Кипарис: високе полум'я темряви, край розмитий шумом — не конус.
  Frame.prototype.cypress = function (x, ground, h, w, color, seed) {
    const c = this.c;
    c.fillStyle = color;
    c.beginPath();
    c.moveTo(...this.px(x - w * 0.12, ground));
    const n = 28;
    for (let i = 0; i <= n; i++) {
      const t = i / n, y = ground + h * t;
      const prof = Math.sin(Math.pow(t, 0.75) * Math.PI) * (1 - t * 0.35);
      const wob = (fbm(x * 3, y * 1.4, seed, 3) - 0.5) * w * 0.35;
      c.lineTo(...this.px(x - w * 0.5 * prof - wob, y));
    }
    for (let i = n; i >= 0; i--) {
      const t = i / n, y = ground + h * t;
      const prof = Math.sin(Math.pow(t, 0.75) * Math.PI) * (1 - t * 0.35);
      const wob = (fbm(x * 3 + 9, y * 1.4, seed, 3) - 0.5) * w * 0.35;
      c.lineTo(...this.px(x + w * 0.5 * prof + wob, y));
    }
    c.closePath(); c.fill();
  };

  // Квітуче дерево місячного саду: тонкий кривий стовбур, крона з білих цяток, що ледь світяться.
  Frame.prototype.blossomTree = function (x, ground, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const lean = (r() - 0.5) * 1.2;
    c.save();
    c.strokeStyle = o.bark; c.lineCap = "round";
    c.lineWidth = S * 0.22;
    c.beginPath(); c.moveTo(...this.px(x, ground));
    c.bezierCurveTo(...this.px(x + lean * 0.2, ground + h * 0.3), ...this.px(x - lean * 0.4, ground + h * 0.5), ...this.px(x + lean, ground + h * 0.62));
    c.stroke();
    const tops = [];
    for (let i = 0; i < 5; i++) {
      const a = Math.PI / 2 + (r() - 0.5) * 2.2, len = h * (0.18 + r() * 0.22);
      const sx = x + lean, sy = ground + h * 0.62;
      const ex = sx + Math.cos(a) * len, ey = sy + Math.sin(a) * len;
      c.lineWidth = S * (0.06 + r() * 0.06);
      c.beginPath(); c.moveTo(...this.px(sx, sy));
      c.quadraticCurveTo(...this.px((sx + ex) / 2 + (r() - 0.5), (sy + ey) / 2 + 0.3), ...this.px(ex, ey)); c.stroke();
      tops.push([ex, ey]);
    }
    c.restore();
    // крона: м'які купи цвіту довкола кінців гілок
    tops.forEach(([tx, ty], i) => {
      this.glow(tx, ty, 1.6, o.glow, 0.18);
      for (let k = 0; k < 70; k++) {
        const a = r() * Math.PI * 2, d = Math.pow(r(), 0.6) * (0.9 + r() * 0.8);
        const [px, py] = this.px(tx + Math.cos(a) * d * 1.3, ty + Math.sin(a) * d * 0.8);
        c.fillStyle = rgba(r() < 0.75 ? o.petal : o.petalShade, 0.35 + r() * 0.55);
        c.beginPath(); c.arc(px, py, S * (0.03 + r() * 0.06), 0, Math.PI * 2); c.fill();
      }
      void i;
    });
    // пелюстки, що падають
    for (let k = 0; k < 18; k++) {
      const [px, py] = this.px(x + lean + (r() - 0.5) * 4, ground + r() * h * 0.7);
      c.fillStyle = rgba(o.petal, 0.4 + r() * 0.4);
      c.beginPath(); c.ellipse(px, py, S * 0.04, S * 0.02, r() * 3, 0, Math.PI * 2); c.fill();
    }
  };

  // Підстрижений живопліт: округлий верх, листяний край із шуму, місячний відблиск на маківці.
  Frame.prototype.hedge = function (x0, x1, ground, h, o, seed) {
    const c = this.c, S = this.S;
    c.fillStyle = o.leaf;
    c.beginPath(); c.moveTo(...this.px(x0, ground));
    for (let x = x0; x <= x1; x += 0.08) {
      const t = (x - x0) / (x1 - x0);
      const round = Math.min(1, Math.min(t, 1 - t) * (x1 - x0) / 0.6);
      const y = ground + h * Math.sqrt(Math.max(0, round)) + (fbm(x * 4, ground, seed, 3) - 0.5) * 0.22;
      c.lineTo(...this.px(x, y));
    }
    c.lineTo(...this.px(x1, ground)); c.closePath(); c.fill();
    // листя-цятки і відблиск зверху
    const r = rng(seed + 1);
    for (let i = 0; i < (x1 - x0) * 18; i++) {
      const x = x0 + 0.15 + r() * (x1 - x0 - 0.3), y = ground + r() * h * 0.95;
      c.fillStyle = rgba(r() < 0.5 ? o.leafLight : o.leafDark, 0.35);
      c.beginPath(); c.arc(...this.px(x, y), S * (0.04 + r() * 0.05), 0, Math.PI * 2); c.fill();
    }
    c.strokeStyle = rgba(o.moon, 0.4); c.lineWidth = S * 0.05;
    c.beginPath(); c.moveTo(...this.px(x0 + 0.4, ground + h - 0.02)); c.lineTo(...this.px(x1 - 0.4, ground + h - 0.02)); c.stroke();
  };

  // Ротонда: щаблі, колони, карниз, купол-півсфера з ліхтариком; усередині — біла статуя в місячному світлі.
  Frame.prototype.rotunda = function (x, ground, w, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const colH = h * 0.6, domeH = h * 0.3;
    // світло всередині
    this.glow(x, ground + colH * 0.5, w * 0.8, o.inner, 0.35);
    c.fillStyle = rgba(o.inner, 0.12);
    c.fillRect(...this.px(x - w * 0.42, ground + colH), w * 0.84 * S, colH * S);
    // статуя всередині: жінка в покривалі, руки складені
    c.fillStyle = o.statue;
    c.beginPath();
    c.moveTo(...this.px(x - 0.45, ground + 0.5));
    c.bezierCurveTo(...this.px(x - 0.55, ground + 1.6), ...this.px(x - 0.3, ground + 2.6), ...this.px(x - 0.2, ground + 3.0));
    c.quadraticCurveTo(...this.px(x, ground + 3.55), ...this.px(x + 0.22, ground + 3.0));
    c.bezierCurveTo(...this.px(x + 0.32, ground + 2.5), ...this.px(x + 0.55, ground + 1.5), ...this.px(x + 0.45, ground + 0.5));
    c.closePath(); c.fill();
    c.fillRect(...this.px(x - 0.6, ground + 0.5), 1.2 * S, 0.5 * S);
    // щаблі
    c.fillStyle = o.stone;
    [[0.62, 0], [0.56, 0.18], [0.5, 0.34]].forEach(([k, dy]) => {
      c.beginPath();
      c.moveTo(...this.px(x - w * k, ground + dy)); c.lineTo(...this.px(x + w * k, ground + dy));
      c.lineTo(...this.px(x + w * k - 0.1, ground + dy + 0.18)); c.lineTo(...this.px(x - w * k + 0.1, ground + dy + 0.18));
      c.closePath(); c.fill();
    });
    // колони з канелюрами
    const cols = 4;
    for (let i = 0; i < cols; i++) {
      const cx = x - w * 0.42 + (w * 0.84) * i / (cols - 1);
      c.fillStyle = o.stone;
      c.fillRect(...this.px(cx - 0.17, ground + colH + 0.5), 0.34 * S, colH * S);
      c.fillStyle = rgba(o.light, 0.35);
      c.fillRect(...this.px(cx - 0.1, ground + colH + 0.5), 0.05 * S, colH * S);
      c.fillStyle = o.stone;
      c.fillRect(...this.px(cx - 0.26, ground + colH + 0.62), 0.52 * S, 0.14 * S);
    }
    // карниз
    c.fillStyle = o.stone;
    c.beginPath();
    c.moveTo(...this.px(x - w * 0.55, ground + colH + 0.5)); c.lineTo(...this.px(x + w * 0.55, ground + colH + 0.5));
    c.lineTo(...this.px(x + w * 0.5, ground + colH + 0.95)); c.lineTo(...this.px(x - w * 0.5, ground + colH + 0.95));
    c.closePath(); c.fill();
    // купол
    const dy0 = ground + colH + 0.95;
    c.beginPath();
    c.moveTo(...this.px(x - w * 0.48, dy0));
    c.bezierCurveTo(...this.px(x - w * 0.48, dy0 + domeH * 0.9), ...this.px(x - w * 0.15, dy0 + domeH * 1.05), ...this.px(x, dy0 + domeH * 1.05));
    c.bezierCurveTo(...this.px(x + w * 0.15, dy0 + domeH * 1.05), ...this.px(x + w * 0.48, dy0 + domeH * 0.9), ...this.px(x + w * 0.48, dy0));
    c.closePath(); c.fill();
    // ребра купола — місячні
    c.strokeStyle = rgba(o.light, 0.3); c.lineWidth = S * 0.04;
    for (let k = -2; k <= 2; k++) {
      c.beginPath(); c.moveTo(...this.px(x + k * w * 0.18, dy0));
      c.quadraticCurveTo(...this.px(x + k * w * 0.12, dy0 + domeH * 0.8), ...this.px(x, dy0 + domeH * 1.05)); c.stroke();
    }
    // ліхтарик і шпиль
    c.fillStyle = o.stone;
    c.fillRect(...this.px(x - 0.25, dy0 + domeH * 1.05 + 0.6), 0.5 * S, 0.6 * S);
    c.beginPath(); c.moveTo(...this.px(x - 0.3, dy0 + domeH * 1.05 + 0.6));
    c.quadraticCurveTo(...this.px(x, dy0 + domeH * 1.05 + 1.3), ...this.px(x + 0.3, dy0 + domeH * 1.05 + 0.6)); c.fill();
    this.glow(x, dy0 + domeH * 1.05 + 0.3, 0.8, o.inner, 0.5);
    // плющ, що звисає з карниза
    c.strokeStyle = rgba(o.vine, 0.85); c.lineCap = "round";
    for (let i = 0; i < 26; i++) {
      const vx = x - w * 0.52 + r() * w * 1.04, len = 0.3 + r() * 1.6;
      c.lineWidth = S * (0.03 + r() * 0.04);
      c.beginPath(); c.moveTo(...this.px(vx, ground + colH + 0.55));
      c.quadraticCurveTo(...this.px(vx + (r() - 0.5) * 0.4, ground + colH + 0.55 - len * 0.5), ...this.px(vx + (r() - 0.5) * 0.3, ground + colH + 0.55 - len));
      c.stroke();
      if (r() < 0.5) {
        c.fillStyle = rgba(o.flower, 0.8);
        c.beginPath(); c.arc(...this.px(vx, ground + colH + 0.55 - len * 0.7), S * 0.05, 0, Math.PI * 2); c.fill();
      }
    }
  };

  // Люстра: ланцюг, золоті ріжки по дузі, свічки з м'яким світлом, кришталеві краплі. tilt — гойдається.
  Frame.prototype.chandelier = function (x, y, w, o, seed, tilt) {
    const c = this.c, S = this.S, r = rng(seed);
    this.chain(x, y + 0.4, 18.5, o.chain, 1);
    c.save();
    const [px, py] = this.px(x, y);
    c.translate(px, py); c.rotate(tilt || 0); c.translate(-px, -py);
    this.glow(x, y + 0.2, w * 1.9, o.glow, 0.42);
    c.strokeStyle = o.gold; c.lineCap = "round";
    // центральна вісь з кулею
    c.lineWidth = S * 0.1;
    c.beginPath(); c.moveTo(...this.px(x, y + 0.6)); c.lineTo(...this.px(x, y - 0.9)); c.stroke();
    c.fillStyle = o.gold;
    c.beginPath(); c.ellipse(...this.px(x, y - 0.3), 0.26 * S, 0.34 * S, 0, 0, Math.PI * 2); c.fill();
    // два яруси ріжків
    [[w * 0.5, 0, 6], [w * 0.3, 0.55, 4]].forEach(([rad, dy, n]) => {
      for (let i = 0; i < n; i++) {
        const t = (i + 0.5) / n, ax = x - rad + rad * 2 * t;
        const sag = Math.sin(t * Math.PI) * 0.35;
        c.lineWidth = S * 0.06;
        c.beginPath(); c.moveTo(...this.px(x, y - 0.2 + dy));
        c.quadraticCurveTo(...this.px((x + ax) / 2, y - 0.75 + dy - sag), ...this.px(ax, y - 0.15 + dy)); c.stroke();
        // свічка
        c.fillStyle = o.wax; c.fillRect(...this.px(ax - 0.04, y + 0.15 + dy), 0.08 * S, 0.3 * S);
        c.fillStyle = o.flame;
        c.beginPath(); c.ellipse(...this.px(ax, y + 0.27 + dy), S * 0.045, S * 0.1, 0, 0, Math.PI * 2); c.fill();
        this.glow(ax, y + 0.27 + dy, 0.7, o.flame, 0.55);
        // кришталева крапля
        if (r() < 0.8) {
          c.fillStyle = rgba(o.crystal, 0.75);
          c.beginPath(); c.ellipse(...this.px(ax, y - 0.45 + dy), S * 0.04, S * 0.08, 0, 0, Math.PI * 2); c.fill();
        }
      }
    });
    // кільце для гака знизу — холодне світло
    if (o.ring) {
      this.glow(x, y - 1.25, 0.9, o.ring, 0.45);
      c.strokeStyle = o.ring; c.lineWidth = S * 0.08;
      c.beginPath(); c.arc(...this.px(x, y - 1.25), S * 0.26, 0, Math.PI * 2); c.stroke();
      c.lineWidth = S * 0.05; c.strokeStyle = o.gold;
      c.beginPath(); c.moveTo(...this.px(x, y - 0.9)); c.lineTo(...this.px(x, y - 1.0)); c.stroke();
    }
    c.restore();
  };

  // Гардина: важке багряне полотно, підв'язане шнуром; складки — смуги світла й тіні.
  Frame.prototype.curtain = function (x, top, len, w, o, seed, side) {
    const c = this.c, S = this.S, r = rng(seed), dir = side === "right" ? -1 : 1;
    const tie = top - len * (0.55 + r() * 0.1);
    const outer = x, inner = x + w * dir;
    const pts = [];
    c.save();
    c.beginPath();
    c.moveTo(...this.px(outer, top));
    c.lineTo(...this.px(inner, top));
    // внутрішній край стягується до шнура і знову розходиться донизу
    c.bezierCurveTo(...this.px(inner, top - len * 0.25), ...this.px(outer + w * 0.35 * dir, tie + 0.6), ...this.px(outer + w * 0.28 * dir, tie));
    c.bezierCurveTo(...this.px(outer + w * 0.4 * dir, tie - 0.8), ...this.px(outer + w * 0.62 * dir, top - len + 0.5), ...this.px(outer + w * 0.58 * dir, top - len));
    c.lineTo(...this.px(outer, top - len));
    c.closePath();
    const g = c.createLinearGradient(...this.px(outer, 0), ...this.px(inner, 0));
    g.addColorStop(0, o.dark); g.addColorStop(0.5, o.cloth); g.addColorStop(1, o.dark);
    c.fillStyle = g; c.fill();
    c.clip();
    // складки
    for (let k = 0; k < 7; k++) {
      const fx = outer + w * dir * (0.08 + k * 0.12 + (r() - 0.5) * 0.04);
      c.strokeStyle = rgba(k % 2 ? o.light : "#000000", k % 2 ? 0.22 : 0.35); c.lineWidth = S * (0.08 + r() * 0.08);
      c.beginPath(); c.moveTo(...this.px(fx, top));
      c.bezierCurveTo(...this.px(fx, top - len * 0.3), ...this.px(outer + (fx - outer) * 0.35, tie + 0.4), ...this.px(outer + (fx - outer) * 0.32, tie));
      c.bezierCurveTo(...this.px(outer + (fx - outer) * 0.4, tie - 1), ...this.px(fx * 0.5 + (outer + w * 0.6 * dir) * 0.5, top - len + 0.5), ...this.px(fx * 0.4 + (outer + w * 0.62 * dir) * 0.6, top - len));
      c.stroke();
      pts.push(fx);
    }
    c.restore();
    // шнур із китицею
    c.strokeStyle = o.gold; c.lineWidth = S * 0.07;
    c.beginPath(); c.moveTo(...this.px(outer, tie + 0.15)); c.quadraticCurveTo(...this.px(outer + w * 0.2 * dir, tie - 0.15), ...this.px(outer + w * 0.32 * dir, tie + 0.05)); c.stroke();
    c.fillStyle = o.gold;
    c.beginPath(); c.moveTo(...this.px(outer + w * 0.3 * dir, tie)); c.lineTo(...this.px(outer + w * 0.25 * dir, tie - 0.7)); c.lineTo(...this.px(outer + w * 0.38 * dir, tie - 0.7)); c.closePath(); c.fill();
    // карниз
    c.fillStyle = o.gold; c.fillRect(...this.px(Math.min(outer, inner) - 0.2, top + 0.12), (w + 0.4) * S, 0.14 * S);
  };

  // Дзеркало в золоченій рамі зі стрільчастим верхом; у склі — блідий відблиск і чужа постать.
  Frame.prototype.mirror = function (x, ground, w, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.beginPath(); this.pointedArch(x, w, ground, h); c.closePath();
    const g = c.createLinearGradient(...this.px(x - w / 2, ground + h), ...this.px(x + w / 2, ground));
    g.addColorStop(0, rgba(o.glass, 0.55)); g.addColorStop(0.5, rgba(o.glassDark, 0.85)); g.addColorStop(1, rgba(o.glass, 0.45));
    c.fillStyle = g; c.fill();
    c.clip();
    // постать у дзеркалі — того, кого в залі немає
    c.fillStyle = rgba(o.ghost, 0.32);
    c.beginPath();
    c.moveTo(...this.px(x - 0.5, ground));
    c.bezierCurveTo(...this.px(x - 0.6, ground + h * 0.3), ...this.px(x - 0.3, ground + h * 0.5), ...this.px(x - 0.18, ground + h * 0.58));
    c.quadraticCurveTo(...this.px(x, ground + h * 0.7), ...this.px(x + 0.18, ground + h * 0.58));
    c.bezierCurveTo(...this.px(x + 0.3, ground + h * 0.5), ...this.px(x + 0.7, ground + h * 0.25), ...this.px(x + 0.8, ground));
    c.closePath(); c.fill();
    // косі смуги відблиску
    for (let k = 0; k < 3; k++) {
      const sx = x - w / 2 + r() * w;
      c.strokeStyle = rgba(o.glint, 0.18 + r() * 0.12); c.lineWidth = S * (0.12 + r() * 0.2);
      c.beginPath(); c.moveTo(...this.px(sx, ground)); c.lineTo(...this.px(sx + 1.2, ground + h)); c.stroke();
    }
    c.restore();
    c.strokeStyle = o.frame; c.lineWidth = S * 0.18;
    c.beginPath(); this.pointedArch(x, w, ground, h); c.stroke();
    c.lineWidth = S * 0.05; c.strokeStyle = rgba(o.glint, 0.5);
    c.beginPath(); this.pointedArch(x, w - 0.25, ground + 0.1, h - 0.2); c.stroke();
    // корона-завиток на вершині
    c.fillStyle = o.frame;
    c.beginPath(); c.ellipse(...this.px(x, ground + h + 0.2), 0.3 * S, 0.18 * S, 0, 0, Math.PI * 2); c.fill();
    this.glow(x, ground + h * 0.5, w * 1.1, o.glass, 0.12);
  };

  // Примарна пара в танці: два напівпрозорі силуети, сукня кружляє, край тане в туман.
  Frame.prototype.ghostDancers = function (x, ground, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), spin = (r() - 0.5) * 0.6;
    c.save();
    c.globalCompositeOperation = "screen";
    this.glow(x, ground + h * 0.5, h * 0.9, o.glow, 0.18);
    const g = c.createLinearGradient(...this.px(0, ground + h), ...this.px(0, ground));
    g.addColorStop(0, rgba(o.ghost, 0.42)); g.addColorStop(1, rgba(o.ghost, 0));
    c.fillStyle = g;
    // дама: широка сукня-дзвін
    c.beginPath();
    c.moveTo(...this.px(x - 0.12, ground + h * 0.95));
    c.quadraticCurveTo(...this.px(x + 0.05, ground + h * 1.05), ...this.px(x + 0.15, ground + h * 0.92));
    c.bezierCurveTo(...this.px(x + 0.25, ground + h * 0.6), ...this.px(x + 0.9 + spin, ground + h * 0.25), ...this.px(x + 1.1 + spin, ground));
    c.quadraticCurveTo(...this.px(x + 0.2, ground - 0.15), ...this.px(x - 0.8 + spin, ground + 0.05));
    c.bezierCurveTo(...this.px(x - 0.5, ground + h * 0.3), ...this.px(x - 0.25, ground + h * 0.6), ...this.px(x - 0.12, ground + h * 0.95));
    c.closePath(); c.fill();
    // кавалер: вужчий, рука на її талії
    const kx = x - 0.55;
    c.beginPath();
    c.moveTo(...this.px(kx - 0.1, ground + h * 1.05));
    c.quadraticCurveTo(...this.px(kx + 0.05, ground + h * 1.15), ...this.px(kx + 0.16, ground + h * 1.02));
    c.lineTo(...this.px(kx + 0.3, ground + h * 0.7));
    c.quadraticCurveTo(...this.px(x - 0.1, ground + h * 0.66), ...this.px(x + 0.05, ground + h * 0.6));
    c.lineTo(...this.px(kx + 0.25, ground + h * 0.55));
    c.lineTo(...this.px(kx + 0.22, ground));
    c.lineTo(...this.px(kx - 0.22, ground));
    c.lineTo(...this.px(kx - 0.25, ground + h * 0.7));
    c.closePath(); c.fill();
    c.restore();
  };

  // Мармурові жилки поверх маси: тонкі криві світлішого каменю.
  Frame.prototype.marbleVeins = function (x0, x1, y0, y1, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 0.6; i++) {
      let x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0);
      c.strokeStyle = rgba(color, 0.08 + r() * 0.12); c.lineWidth = S * (0.015 + r() * 0.03);
      c.beginPath(); c.moveTo(...this.px(x, y));
      for (let k = 0; k < 6; k++) {
        x += 0.3 + r() * 0.6; y += (fbm(x, y, seed, 2) - 0.5) * 0.9;
        if (y < y0 || y > y1) break;
        c.lineTo(...this.px(x, y));
      }
      c.stroke();
    }
    c.restore();
  };
})();
