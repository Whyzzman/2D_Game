// Речі Столиці для макета поверх бібліотек Гробниці, Лісу й Села: згорілі шпилі палацу, кам'яний ангел,
// жаровні, водоспад, темна вода й синя печать. Плавні криві; кожен шпиль і кожна вежа — зі свого seed.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Шпиль: стрункий конус із м'якими боками, обгорілий — верхівка зламана й обвуглена у частини.
  function spire(f, x, base, w, h, color, r) {
    const c = f.c, broken = r() < 0.45, top = base + h * (broken ? 0.72 + r() * 0.12 : 1);
    const lean = (r() - 0.5) * 0.6;
    c.fillStyle = color;
    c.beginPath();
    c.moveTo(...f.px(x - w / 2, base));
    c.bezierCurveTo(...f.px(x - w / 2, base + h * 0.45), ...f.px(x - w * 0.12, base + h * 0.75), ...f.px(x + lean - (broken ? w * 0.18 : 0), top));
    if (broken) {
      // рвана верхівка: кілька зубців
      c.lineTo(...f.px(x + lean - w * 0.05, top - 0.5 - r() * 0.4));
      c.lineTo(...f.px(x + lean + w * 0.08, top + 0.2));
      c.lineTo(...f.px(x + lean + w * 0.16, top - 0.35));
    }
    c.bezierCurveTo(...f.px(x + w * 0.12, base + h * 0.75), ...f.px(x + w / 2, base + h * 0.45), ...f.px(x + w / 2, base));
    c.closePath(); c.fill();
  }

  // Силует палацу: стіни з ярусами веж і шпилів; ширина й ритм — із seed, нічого не повторюється.
  Frame.prototype.palace = function (x0, x1, base, height, color, seed, windows) {
    const c = this.c, S = this.S, r = rng(seed);
    c.fillStyle = color;
    // тіло палацу: м'яка хвиля дахів
    c.beginPath(); c.moveTo(...this.px(x0, base));
    for (let x = x0; x <= x1; x += 0.5) {
      const y = base + height * (0.32 + 0.12 * fbm(x * 0.4, 1, seed, 2));
      c.lineTo(...this.px(x, y));
    }
    c.lineTo(...this.px(x1, base)); c.closePath(); c.fill();
    // вежі з шпилями
    for (let x = x0 + r() * 2; x < x1; x += 2.2 + r() * 3.4) {
      const tw = 0.9 + r() * 1.1, th = height * (0.45 + r() * 0.35);
      c.fillStyle = color;
      c.fillRect(...this.px(x - tw / 2, base + th), tw * S, th * S);
      spire(this, x, base + th - 0.1, tw * 1.25, height * (0.25 + r() * 0.45), color, r);
      if (windows && r() < 0.35) {
        const wy = base + th * (0.55 + r() * 0.3);
        c.fillStyle = windows;
        c.beginPath(); this.pointedArch(x, tw * 0.28, wy, 0.7); c.fill();
        this.glow(x, wy + 0.3, 1.4, windows, 0.3);
      }
    }
  };

  // Кам'яний ангел на постаменті: склав руки, одне крило ціле, друге зламане; лице опущене.
  Frame.prototype.angel = function (x, ground, h, o) {
    const c = this.c, S = this.S, P = (dx, dy) => this.px(x + dx * h, ground + dy * h);
    c.fillStyle = o.stone;
    // постамент
    c.beginPath(); c.moveTo(...P(-0.32, 0)); c.lineTo(...P(-0.28, 0.08)); c.lineTo(...P(-0.22, 0.1)); c.lineTo(...P(-0.2, 0.26));
    c.lineTo(...P(0.2, 0.26)); c.lineTo(...P(0.22, 0.1)); c.lineTo(...P(0.28, 0.08)); c.lineTo(...P(0.32, 0)); c.closePath(); c.fill();
    // ціле крило (ліве): дуга до плеча, нижній край — пера, що звисають гребінцем
    c.beginPath(); c.moveTo(...P(-0.05, 0.64));
    c.bezierCurveTo(...P(-0.2, 0.78), ...P(-0.34, 0.96), ...P(-0.3, 1.14));
    c.bezierCurveTo(...P(-0.4, 1.0), ...P(-0.46, 0.8), ...P(-0.44, 0.58));
    [[-0.4, 0.5], [-0.35, 0.42], [-0.29, 0.36], [-0.22, 0.34]].forEach(([fx, fy], i) => {
      c.quadraticCurveTo(...P(fx + 0.02, fy + 0.08), ...P(fx + 0.035, fy + 0.02 + i * 0.01));
      c.lineTo(...P(fx + 0.02, fy));
    });
    c.quadraticCurveTo(...P(-0.12, 0.46), ...P(-0.06, 0.52)); c.closePath(); c.fill();
    // зламане крило (праве): пеньок із рваним краєм
    c.beginPath(); c.moveTo(...P(0.05, 0.64));
    c.bezierCurveTo(...P(0.16, 0.72), ...P(0.24, 0.8), ...P(0.25, 0.88));
    c.lineTo(...P(0.22, 0.82)); c.lineTo(...P(0.24, 0.76)); c.lineTo(...P(0.19, 0.72)); c.lineTo(...P(0.2, 0.64));
    c.quadraticCurveTo(...P(0.12, 0.54), ...P(0.06, 0.52)); c.closePath(); c.fill();
    // тіло в довгій сукні
    c.beginPath(); c.moveTo(...P(-0.15, 0.26));
    c.bezierCurveTo(...P(-0.12, 0.45), ...P(-0.1, 0.6), ...P(-0.08, 0.7));
    c.quadraticCurveTo(...P(0, 0.74), ...P(0.08, 0.7));
    c.bezierCurveTo(...P(0.1, 0.6), ...P(0.12, 0.45), ...P(0.15, 0.26)); c.closePath(); c.fill();
    // голова, схилена
    c.beginPath(); c.ellipse(...P(0.015, 0.78), h * S * 0.045, h * S * 0.055, 0.25, 0, Math.PI * 2); c.fill();
    // світло з лівого краю — холодний кант на камені
    c.strokeStyle = rgba(o.light, 0.55); c.lineWidth = S * 0.06; c.lineCap = "round";
    c.beginPath(); c.moveTo(...P(-0.08, 0.7)); c.bezierCurveTo(...P(-0.1, 0.6), ...P(-0.12, 0.45), ...P(-0.15, 0.27)); c.stroke();
    c.beginPath(); c.moveTo(...P(-0.3, 1.14)); c.bezierCurveTo(...P(-0.4, 1.0), ...P(-0.46, 0.8), ...P(-0.44, 0.58)); c.stroke();
    // складки сукні
    c.strokeStyle = rgba("#000000", 0.35); c.lineWidth = S * 0.035;
    [-0.05, 0.03, 0.09].forEach((dx, i) => {
      c.beginPath(); c.moveTo(...P(dx * 0.7, 0.62)); c.quadraticCurveTo(...P(dx + 0.02 * i, 0.45), ...P(dx * 1.4, 0.27)); c.stroke();
    });
  };

  // Жаровня на ніжці: чаша, жар і язики полум'я; єдине тепле світло поруч.
  Frame.prototype.brazier = function (x, ground, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    this.glow(x, ground + h + 0.5, 4.2, o.ember, 0.4);
    this.glow(x, ground + h + 0.4, 1.4, o.flame, 0.55);
    c.fillStyle = o.iron;
    c.beginPath(); c.moveTo(...this.px(x - 0.5, ground)); c.lineTo(...this.px(x - 0.1, ground + h * 0.6));
    c.lineTo(...this.px(x + 0.1, ground + h * 0.6)); c.lineTo(...this.px(x + 0.5, ground)); c.closePath(); c.fill();
    c.fillRect(...this.px(x - 0.08, ground + h), 0.16 * S, h * 0.4 * S);
    c.beginPath(); c.moveTo(...this.px(x - 0.6, ground + h + 0.05));
    c.quadraticCurveTo(...this.px(x, ground + h - 0.45), ...this.px(x + 0.6, ground + h + 0.05)); c.closePath(); c.fill();
    // полум'я — кілька м'яких язиків
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 6; i++) {
      const fx = x + (r() - 0.5) * 0.7, fh = 0.4 + r() * 0.6;
      const [bx, by] = this.px(fx, ground + h), [tx, ty] = this.px(fx + (r() - 0.5) * 0.2, ground + h + fh);
      const g = c.createLinearGradient(bx, by, tx, ty);
      g.addColorStop(0, rgba(o.flame, 0.9)); g.addColorStop(1, rgba(o.ember, 0));
      c.fillStyle = g;
      c.beginPath(); c.moveTo(bx - S * 0.14, by);
      c.quadraticCurveTo(bx - S * 0.12, by - (by - ty) * 0.6, tx, ty);
      c.quadraticCurveTo(bx + S * 0.12, by - (by - ty) * 0.6, bx + S * 0.14, by); c.fill();
    }
    c.restore();
    // іскри вгору
    c.fillStyle = rgba(o.flame, 0.8);
    for (let i = 0; i < 8; i++) {
      c.beginPath(); c.arc(...this.px(x + (r() - 0.5) * 1.4, ground + h + 0.8 + r() * 2.6), S * (0.015 + r() * 0.025), 0, Math.PI * 2); c.fill();
    }
  };

  // Зруйнована стіна з арками: верх не рівний — обвалені зубці, проломи; низ — тінь.
  Frame.prototype.ruinedWall = function (x0, x1, base, top, color, seed) {
    const c = this.c;
    c.fillStyle = color;
    c.beginPath(); c.moveTo(...this.px(x0, base));
    for (let x = x0; x <= x1; x += 0.25) {
      const n = fbm(x * 0.35, 3, seed, 3), drop = Math.max(0, fbm(x * 0.12, 7, seed + 1, 2)) * 6;
      c.lineTo(...this.px(x, top + n * 1.4 - drop));
    }
    c.lineTo(...this.px(x1, base)); c.closePath(); c.fill();
  };

  // Відблиск світла на воді: розмита смуга під джерелом.
  Frame.prototype.reflection = function (x, surface, w, depth, color, alpha) {
    const c = this.c, S = this.S, [px, py] = this.px(x, surface);
    c.save(); c.globalCompositeOperation = "screen";
    const g = c.createLinearGradient(0, py, 0, py + depth * S);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g;
    for (let i = 0; i < 7; i++) {
      const yy = py + depth * S * i / 7, ww = w * S * (1 - i / 9) * (0.6 + 0.4 * Math.sin(i * 2.3));
      c.fillRect(px - ww / 2, yy, ww, Math.max(1, S * 0.08));
    }
    c.restore();
  };

  // Попіл, що падає: сірі пластівці, не пил.
  Frame.prototype.ash = function (count, color, seed) {
    const c = this.c, S = this.S, r = rng(seed), W = c.canvas.width / S, H = c.canvas.height / S;
    for (let i = 0; i < count; i++) {
      const [px, py] = this.px(r() * W, r() * H);
      c.fillStyle = rgba(color, 0.2 + r() * 0.5);
      c.beginPath(); c.ellipse(px, py, S * (0.03 + r() * 0.04), S * (0.015 + r() * 0.02), r() * 3, 0, Math.PI * 2); c.fill();
    }
  };

  // Водоспад із пролому: смуги води з м'якими краями, піна внизу.
  Frame.prototype.waterfall = function (x, top, bottom, w, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const [l, t] = this.px(x - w / 2, top), [, b] = this.px(x, bottom);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, rgba(o.water, 0.55)); g.addColorStop(1, rgba(o.water, 0.85));
    c.save(); c.filter = `blur(${S * 0.08}px)`;
    c.fillStyle = g;
    c.beginPath(); c.moveTo(l, t);
    c.bezierCurveTo(l - S * 0.1, t + (b - t) * 0.4, l - S * 0.35, b - (b - t) * 0.2, l - S * 0.45, b);
    c.lineTo(l + w * S + S * 0.45, b);
    c.bezierCurveTo(l + w * S + S * 0.35, b - (b - t) * 0.2, l + w * S + S * 0.1, t + (b - t) * 0.4, l + w * S, t);
    c.closePath(); c.fill();
    c.strokeStyle = rgba(o.foam, 0.35); c.lineCap = "round";
    for (let i = 0; i < w * 9; i++) {
      const sx = x - w / 2 + r() * w, len = 1.5 + r() * 4, sy = top - r() * (top - bottom - len);
      c.lineWidth = S * (0.03 + r() * 0.05);
      c.beginPath(); c.moveTo(...this.px(sx, sy)); c.lineTo(...this.px(sx + (sx - x) * 0.05, sy - len)); c.stroke();
    }
    c.restore();
    // піна й бризки біля води
    for (let i = 0; i < 18; i++) {
      const [px, py] = this.px(x + (r() - 0.5) * (w + 1.6), bottom + r() * 0.8);
      const rad = S * (0.3 + r() * 0.6), gg = c.createRadialGradient(px, py, 0, px, py, rad);
      gg.addColorStop(0, rgba(o.foam, 0.35)); gg.addColorStop(1, rgba(o.foam, 0));
      c.fillStyle = gg; c.beginPath(); c.arc(px, py, rad, 0, Math.PI * 2); c.fill();
    }
  };

  // Темна вода: поверхня з відблисками, глибина темніє донизу; colour відбитого світла — з o.glint.
  Frame.prototype.water = function (x0, x1, surface, floor, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const [l, t] = this.px(x0, surface), [rr, b] = this.px(x1, floor);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, rgba(o.water, 0.8)); g.addColorStop(1, rgba(o.deep, 0.95));
    c.fillStyle = g; c.fillRect(l, t, rr - l, b - t);
    c.strokeStyle = rgba(o.glint, 0.5); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 2; i++) {
      const x = x0 + r() * (x1 - x0), y = surface - r() * r() * 1.4, len = 0.3 + r() * 1.1;
      c.lineWidth = S * (0.02 + r() * 0.03);
      c.globalAlpha = 1 - (surface - y) / 1.6;
      c.beginPath(); c.moveTo(...this.px(x, y)); c.lineTo(...this.px(x + len, y)); c.stroke();
    }
    c.globalAlpha = 1;
    c.strokeStyle = rgba(o.glint, 0.6); c.lineWidth = S * 0.04;
    c.beginPath(); c.moveTo(l, t); c.lineTo(rr, t); c.stroke();
  };

  // Печать: синє коло з рунами на стіні, світиться холодним.
  Frame.prototype.seal = function (x, y, rad, o, seed) {
    const c = this.c, S = this.S, r = rng(seed), [px, py] = this.px(x, y);
    this.glow(x, y, rad * 3.2, o.glow, 0.35);
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = rgba(o.line, 0.9); c.lineWidth = S * 0.07;
    c.beginPath(); c.arc(px, py, rad * S, 0, Math.PI * 2); c.stroke();
    c.lineWidth = S * 0.04;
    c.beginPath(); c.arc(px, py, rad * S * 0.78, 0, Math.PI * 2); c.stroke();
    // руни між колами
    for (let i = 0; i < 12; i++) {
      const a = i / 12 * Math.PI * 2, rr = rad * S * 0.89;
      const cx = px + Math.cos(a) * rr, cy = py + Math.sin(a) * rr;
      c.beginPath(); c.moveTo(cx - S * 0.05, cy - S * 0.07); c.lineTo(cx + (r() - 0.5) * S * 0.1, cy + S * 0.07);
      c.moveTo(cx + S * 0.05, cy - S * 0.05); c.quadraticCurveTo(cx, cy, cx - S * 0.03, cy + S * 0.05); c.stroke();
    }
    // знак у центрі — корона-полум'я, як на стягах
    c.lineWidth = S * 0.06;
    c.beginPath(); c.moveTo(px - rad * S * 0.35, py + rad * S * 0.25);
    c.quadraticCurveTo(px - rad * S * 0.3, py - rad * S * 0.2, px - rad * S * 0.15, py - rad * S * 0.4);
    c.quadraticCurveTo(px - rad * S * 0.05, py - rad * S * 0.1, px, py - rad * S * 0.5);
    c.quadraticCurveTo(px + rad * S * 0.05, py - rad * S * 0.1, px + rad * S * 0.15, py - rad * S * 0.4);
    c.quadraticCurveTo(px + rad * S * 0.3, py - rad * S * 0.2, px + rad * S * 0.35, py + rad * S * 0.25);
    c.closePath(); c.stroke();
    c.restore();
  };

  // Довгий стяг, що висить зі стіни: багряне полотно з обпаленим рваним низом.
  Frame.prototype.longBanner = function (x, top, len, w, cloth, gold, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.fillStyle = "#1c1818"; c.fillRect(...this.px(x - w / 2 - 0.15, top + 0.08), (w + 0.3) * S, 0.16 * S);
    c.fillStyle = cloth;
    c.beginPath(); c.moveTo(...this.px(x - w / 2, top));
    c.bezierCurveTo(...this.px(x - w / 2 - 0.05, top - len * 0.4), ...this.px(x - w / 2 + 0.08, top - len * 0.7), ...this.px(x - w / 2 + 0.05, top - len));
    for (let k = 1; k <= 6; k++) c.lineTo(...this.px(x - w / 2 + w * k / 6, top - len + (k % 2 ? 0.35 + r() * 0.4 : r() * 0.2)));
    c.bezierCurveTo(...this.px(x + w / 2 + 0.08, top - len * 0.7), ...this.px(x + w / 2 - 0.05, top - len * 0.4), ...this.px(x + w / 2, top));
    c.closePath(); c.fill();
    // складки й вицвіла корона-полум'я
    c.strokeStyle = rgba("#000000", 0.3); c.lineWidth = S * 0.06;
    [-0.25, 0.2].forEach(d => { c.beginPath(); c.moveTo(...this.px(x + d * w, top - 0.2)); c.quadraticCurveTo(...this.px(x + d * w * 1.3, top - len * 0.5), ...this.px(x + d * w, top - len + 0.4)); c.stroke(); });
    c.fillStyle = rgba(gold, 0.8);
    c.beginPath(); c.moveTo(...this.px(x - w * 0.22, top - len * 0.32));
    c.lineTo(...this.px(x - w * 0.12, top - len * 0.18)); c.lineTo(...this.px(x, top - len * 0.26)); c.lineTo(...this.px(x + w * 0.12, top - len * 0.18));
    c.lineTo(...this.px(x + w * 0.22, top - len * 0.32)); c.closePath(); c.fill();
  };
})();
