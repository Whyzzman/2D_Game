// Сільські речі для макета поверх бібліотеки Гробниці й Лісу: хати з провислою соломою, дзвіниця,
// колодязь, дим, вітраж, свічки, балки. Плавні криві; кожна хата — зі свого seed.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Хата: стіна з глини в дерев'яному каркасі, солом'яний дах провисає, вікна — темні або з вогником.
  Frame.prototype.house = function (x, ground, w, h, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const roofH = h * (0.7 + r() * 0.3), sag = 0.3 + r() * 0.5, broken = r() < (o.broken || 0);
    // стіна
    c.fillStyle = o.wall;
    c.fillRect(...this.px(x, ground + h), w * S, h * S);
    // каркас
    c.strokeStyle = o.beam; c.lineWidth = S * 0.14;
    const [l, t] = this.px(x, ground + h), [rr, b] = this.px(x + w, ground);
    c.strokeRect(l, t, rr - l, b - t);
    c.beginPath(); c.moveTo(l, t); c.lineTo(rr, b); c.moveTo(...this.px(x + w / 2, ground + h)); c.lineTo(...this.px(x + w / 2, ground)); c.stroke();
    // вікна
    const n = Math.max(1, Math.round(w / 2.4));
    for (let i = 0; i < n; i++) {
      const wx = x + w * (i + 0.5) / n - 0.35, wy = ground + h * 0.45;
      const lit = r() < (o.lit || 0);
      c.fillStyle = lit ? o.windowLit : o.window;
      c.fillRect(...this.px(wx, wy + 0.8), 0.7 * S, 0.8 * S);
      if (lit) this.glow(wx + 0.35, wy + 0.4, 1.6, o.windowLit, 0.35);
    }
    // дах: провислий, з краєм, що звисає; у зруйнованої хати — дірка з кроквами
    c.fillStyle = o.roof;
    c.beginPath();
    c.moveTo(...this.px(x - 0.5, ground + h - 0.1));
    c.quadraticCurveTo(...this.px(x + w * 0.3, ground + h + roofH * 0.55 - sag), ...this.px(x + w / 2, ground + h + roofH));
    c.quadraticCurveTo(...this.px(x + w * 0.7, ground + h + roofH * 0.55 - sag), ...this.px(x + w + 0.5, ground + h - 0.1));
    c.closePath(); c.fill();
    // пасма соломи
    c.strokeStyle = rgba(o.roofLight, 0.45); c.lineWidth = S * 0.03;
    for (let i = 0; i < w * 5; i++) {
      const sx = x - 0.4 + r() * (w + 0.8);
      const [px, py] = this.px(sx, ground + h - 0.05);
      c.beginPath(); c.moveTo(px, py); c.lineTo(px + (r() - 0.5) * S * 0.2, py + S * (0.15 + r() * 0.3)); c.stroke();
    }
    if (broken) {
      // провалена стріха: голі крокви стирчать над западиною гребеня, солома обвисла
      const hx = x + w * (0.35 + r() * 0.3), hw = w * (0.2 + r() * 0.1);
      c.strokeStyle = o.beam; c.lineCap = "round"; c.lineWidth = S * 0.09;
      for (let k = -2; k <= 2; k++) {
        const bx = hx + k * hw * 0.4;
        c.beginPath(); c.moveTo(...this.px(bx, ground + h + roofH * 0.45));
        c.quadraticCurveTo(...this.px(bx + 0.1, ground + h + roofH * 0.8), ...this.px(bx + k * 0.12, ground + h + roofH * (0.95 + r() * 0.2)));
        c.stroke();
      }
      c.strokeStyle = rgba(o.roofLight, 0.35); c.lineWidth = S * 0.03;
      for (let k = 0; k < 14; k++) {
        const [px, py] = this.px(hx - hw + r() * hw * 2, ground + h + roofH * (0.45 + r() * 0.1));
        c.beginPath(); c.moveTo(px, py); c.quadraticCurveTo(px + (r() - 0.5) * S * 0.2, py + S * 0.2, px + (r() - 0.5) * S * 0.3, py + S * (0.3 + r() * 0.4)); c.stroke();
      }
    }
  };

  // Дзвіниця: кам'яна вежа, відкритий ярус з дзвоном і гостра покрівля.
  Frame.prototype.belfry = function (x, ground, w, h, o) {
    const c = this.c, S = this.S;
    c.fillStyle = o.stone;
    c.fillRect(...this.px(x - w / 2, ground + h), w * S, h * S);
    const top = ground + h;
    c.fillRect(...this.px(x - w / 2 - 0.3, top + 0.4), (w + 0.6) * S, 0.4 * S);
    c.fillRect(...this.px(x - w / 2, top + 3.4), 0.5 * S, 3 * S);
    c.fillRect(...this.px(x + w / 2 - 0.5, top + 3.4), 0.5 * S, 3 * S);
    c.beginPath();
    c.moveTo(...this.px(x - w / 2 - 0.5, top + 3.4)); c.lineTo(...this.px(x, top + 7.5)); c.lineTo(...this.px(x + w / 2 + 0.5, top + 3.4));
    c.closePath(); c.fill();
    // дзвін — бронза, ледь жевріє
    this.glow(x, top + 1.8, 2.4, o.bellGlow, 0.35);
    c.fillStyle = o.bell;
    c.beginPath();
    c.moveTo(...this.px(x - 0.5, top + 2.6));
    c.quadraticCurveTo(...this.px(x - 0.55, top + 1.2), ...this.px(x - 0.9, top + 0.9));
    c.lineTo(...this.px(x + 0.9, top + 0.9));
    c.quadraticCurveTo(...this.px(x + 0.55, top + 1.2), ...this.px(x + 0.5, top + 2.6));
    c.closePath(); c.fill();
    // вузькі вікна стовбура вежі
    c.fillStyle = o.slit;
    for (let y = ground + 2; y < top - 1.5; y += 3) c.fillRect(...this.px(x - 0.15, y + 1.2), 0.3 * S, 1.2 * S);
  };

  // Дим, що піднімається знизу (зі Столиці під Селом).
  Frame.prototype.smoke = function (x0, x1, y0, y1, color, alpha, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    for (let i = 0; i < 26; i++) {
      const x = x0 + r() * (x1 - x0), y = y0 + r() * (y1 - y0);
      const [px, py] = this.px(x + fbm(x, y, seed, 2) * 2, y);
      const rad = S * (1.5 + r() * 3.5);
      const g = c.createRadialGradient(px, py, 0, px, py, rad);
      g.addColorStop(0, rgba(color, alpha * (0.4 + r() * 0.6))); g.addColorStop(1, rgba(color, 0));
      c.fillStyle = g; c.beginPath(); c.ellipse(px, py, rad * 1.4, rad, 0, 0, Math.PI * 2); c.fill();
    }
  };

  // Колодязь: кам'яне кільце, дашок на двох стовпах, коловорот.
  Frame.prototype.well = function (x, ground, o) {
    const c = this.c, S = this.S;
    c.fillStyle = o.stone;
    c.beginPath(); c.moveTo(...this.px(x - 1.1, ground)); c.lineTo(...this.px(x - 1.0, ground + 1.0));
    c.quadraticCurveTo(...this.px(x, ground + 1.2), ...this.px(x + 1.0, ground + 1.0)); c.lineTo(...this.px(x + 1.1, ground)); c.closePath(); c.fill();
    c.fillStyle = o.beam;
    c.fillRect(...this.px(x - 0.95, ground + 3), 0.16 * S, 2 * S);
    c.fillRect(...this.px(x + 0.8, ground + 3), 0.16 * S, 2 * S);
    c.beginPath(); c.moveTo(...this.px(x - 1.4, ground + 2.9)); c.lineTo(...this.px(x, ground + 3.8)); c.lineTo(...this.px(x + 1.4, ground + 2.9)); c.closePath();
    c.fillStyle = o.roof; c.fill();
    c.strokeStyle = o.beam; c.lineWidth = S * 0.08;
    c.beginPath(); c.moveTo(...this.px(x - 0.9, ground + 2.3)); c.lineTo(...this.px(x + 0.9, ground + 2.3)); c.stroke();
    c.lineWidth = S * 0.025; c.beginPath(); c.moveTo(...this.px(x, ground + 2.3)); c.lineTo(...this.px(x, ground + 1.3)); c.stroke();
  };

  // Вітраж: стрілчасте вікно, скло — нерівні уламки в олов'яних жилках, частина вибита; світло м'яке.
  Frame.prototype.stainedWindow = function (x, y, w, h, colors, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save();
    c.beginPath(); this.pointedArch(x, w, y, h); c.clip();
    c.fillStyle = "#120d0b"; c.fillRect(...this.px(x - w, y + h), w * 2 * S, h * S);
    // уламки — опуклі багатокутники навколо випадкових центрів, кожен трохи розмитий
    c.filter = `blur(${S * 0.04}px)`;
    for (let i = 0; i < 46; i++) {
      if (r() < 0.22) continue; // вибите скло
      const cx = x + (r() - 0.5) * w, cy = y + r() * h, rad = 0.35 + r() * 0.45;
      const pts = 5 + Math.floor(r() * 3);
      c.beginPath();
      for (let k = 0; k < pts; k++) {
        const a = k / pts * Math.PI * 2 + r() * 0.5, d = rad * (0.7 + r() * 0.4);
        const [px, py] = this.px(cx + Math.cos(a) * d, cy + Math.sin(a) * d);
        k ? c.lineTo(px, py) : c.moveTo(px, py);
      }
      c.closePath();
      c.fillStyle = rgba(colors[Math.floor(r() * colors.length)], 0.5 + r() * 0.35); c.fill();
      c.strokeStyle = "rgba(18,13,11,0.85)"; c.lineWidth = S * 0.05; c.stroke();
    }
    c.filter = "none";
    c.restore();
    this.glow(x, y + h * 0.5, w * 1.3, colors[1], 0.25);
    c.strokeStyle = "#120d0b"; c.lineWidth = S * 0.14;
    c.beginPath(); this.pointedArch(x, w, y, h); c.stroke();
  };

  Frame.prototype.candle = function (x, y, h, color) {
    const c = this.c, S = this.S;
    this.glow(x, y + h + 0.15, 1.8, color, 0.45);
    c.fillStyle = "#cbbca0"; c.fillRect(...this.px(x - 0.06, y + h), 0.12 * S, h * S);
    c.fillStyle = color;
    c.beginPath(); c.ellipse(...this.px(x, y + h + 0.12), S * 0.05, S * 0.12, 0, 0, Math.PI * 2); c.fill();
  };
})();
