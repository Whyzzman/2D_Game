// Печерні речі для макета поверх бібліотек Гробниці, Лісу, Села й Столиці: кристали з гранями й світлом
// усередині, друзи, сталактити й сталагміти, дерев'яні риштування шахти з драбинами, лампа шахтаря, рейки,
// іскри світла. Плавні криві; кожен кристал і кожна балка — зі свого seed, нічого не повторюється.
(function () {
  "use strict";
  const { Frame, rgba, rng, fbm } = window.TombLook;

  // Один кристал: витягнута призма з гострим кінцем, світла й темна грані, світло всередині, кант по ребру.
  Frame.prototype.crystal = function (x, y, len, w, angle, o, glow) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save();
    c.translate(px, py); c.rotate(-angle);
    const L = len * S, W = w * S;
    // тіло: дві грані
    const g1 = c.createLinearGradient(0, 0, 0, -L);
    g1.addColorStop(0, rgba(o.dark, 0.95)); g1.addColorStop(0.6, rgba(o.mid, 0.9)); g1.addColorStop(1, rgba(o.light, 0.95));
    c.fillStyle = g1;
    c.beginPath(); c.moveTo(-W / 2, 0); c.lineTo(-W / 2, -L * 0.78); c.lineTo(0, -L); c.lineTo(0, 0); c.closePath(); c.fill();
    const g2 = c.createLinearGradient(0, 0, 0, -L);
    g2.addColorStop(0, rgba(o.dark, 1)); g2.addColorStop(1, rgba(o.mid, 0.95));
    c.fillStyle = g2;
    c.beginPath(); c.moveTo(0, 0); c.lineTo(0, -L); c.lineTo(W / 2, -L * 0.78); c.lineTo(W / 2, 0); c.closePath(); c.fill();
    // світла жилка по середньому ребру й по краю
    c.strokeStyle = rgba(o.edge, 0.8); c.lineWidth = Math.max(1, S * 0.025);
    c.beginPath(); c.moveTo(0, -L * 0.05); c.lineTo(0, -L); c.lineTo(-W / 2, -L * 0.78); c.stroke();
    // внутрішнє світло — розмита пляма на третині висоти
    if (glow) {
      c.globalCompositeOperation = "screen";
      const gg = c.createRadialGradient(0, -L * 0.62, 0, 0, -L * 0.62, W * 1.1);
      gg.addColorStop(0, rgba(o.edge, 0.55)); gg.addColorStop(1, rgba(o.edge, 0));
      c.fillStyle = gg; c.fillRect(-W * 1.2, -L * 1.2, W * 2.4, L * 1.3);
    }
    c.restore();
  };

  // Друза: віяло кристалів різної довжини й нахилу з одного кореня; найбільші — посередині.
  Frame.prototype.crystalCluster = function (x, y, size, o, seed, opts) {
    const r = rng(seed), n = 4 + Math.floor(r() * 5), base = (opts && opts.angle) || 0;
    this.glow(x, y + size * 0.45, size * 1.6, o.glow, (opts && opts.glowAlpha) || 0.4);
    const items = [];
    for (let i = 0; i < n; i++) {
      const t = (i / (n - 1 || 1)) - 0.5;
      items.push({ a: base + t * 1.5 + (r() - 0.5) * 0.35, len: size * (1 - Math.abs(t) * 1.1) * (0.6 + r() * 0.5), w: size * (0.14 + r() * 0.12),
        dx: t * size * 0.35 + (r() - 0.5) * 0.15 });
    }
    items.sort((a, b) => a.len - b.len);
    items.forEach((it, i) => this.crystal(x + it.dx, y, Math.max(0.3, it.len), it.w, it.a, o, i >= items.length - 2));
  };

  // Сталактити зі стелі: звужені криві язики, на кінці деяких — крапля світла.
  Frame.prototype.stalactites = function (x0, x1, ceil, maxLen, color, seed, drip) {
    const c = this.c, S = this.S, r = rng(seed);
    c.fillStyle = color;
    for (let x = x0; x < x1; x += 0.25 + r() * 0.9) {
      const len = maxLen * (0.2 + Math.pow(r(), 1.6) * 0.8), w = 0.2 + r() * 0.5 + len * 0.08, bend = (r() - 0.5) * 0.3;
      c.beginPath();
      c.moveTo(...this.px(x - w / 2, ceil + 0.3));
      c.quadraticCurveTo(...this.px(x - w * 0.35, ceil - len * 0.5), ...this.px(x + bend, ceil - len));
      c.quadraticCurveTo(...this.px(x + w * 0.35, ceil - len * 0.5), ...this.px(x + w / 2, ceil + 0.3));
      c.closePath(); c.fill();
      if (drip && r() < 0.25) this.glow(x + bend, ceil - len - 0.15, 0.25, drip, 0.7);
    }
  };

  // Сталагміти з підлоги: такі самі, але вгору й ширші в основі.
  Frame.prototype.stalagmites = function (x0, x1, ground, maxLen, color, seed) {
    const c = this.c, r = rng(seed);
    c.fillStyle = color;
    for (let x = x0; x < x1; x += 0.6 + r() * 1.6) {
      const len = maxLen * (0.25 + Math.pow(r(), 1.4) * 0.75), w = 0.5 + r() * 0.8 + len * 0.15, bend = (r() - 0.5) * 0.4;
      c.beginPath();
      c.moveTo(...this.px(x - w / 2, ground - 0.3));
      c.quadraticCurveTo(...this.px(x - w * 0.3, ground + len * 0.55), ...this.px(x + bend, ground + len));
      c.quadraticCurveTo(...this.px(x + w * 0.3, ground + len * 0.55), ...this.px(x + w / 2, ground - 0.3));
      c.closePath(); c.fill();
    }
  };

  // Риштування шахти: криві стояки, дощаті помости на рівнях, хрести-розпірки, драбина між помостами, мотузки.
  Frame.prototype.scaffold = function (x0, x1, bottom, levels, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    const posts = [];
    for (let x = x0; x <= x1 + 0.01; x += (x1 - x0) / Math.max(1, Math.round((x1 - x0) / 2.6))) posts.push(x + (r() - 0.5) * 0.25);
    const top = Math.max(...levels) + 0.6;
    c.save(); c.lineCap = "round";
    // розпірки між стояками — по прольоту між рівнями
    c.strokeStyle = o.brace; c.lineWidth = S * 0.08;
    const ys = [bottom].concat(levels);
    for (let i = 0; i < posts.length - 1; i++) {
      for (let k = 0; k < ys.length - 1; k++) {
        if (r() < 0.35) continue;
        c.beginPath(); c.moveTo(...this.px(posts[i], ys[k])); c.lineTo(...this.px(posts[i + 1], ys[k + 1] - 0.15)); c.stroke();
        if (r() < 0.5) { c.beginPath(); c.moveTo(...this.px(posts[i + 1], ys[k])); c.lineTo(...this.px(posts[i], ys[k + 1] - 0.15)); c.stroke(); }
      }
    }
    // стояки: ледь криві колоди
    posts.forEach(x => {
      const lean = (r() - 0.5) * 0.2;
      c.strokeStyle = o.post; c.lineWidth = S * (0.2 + r() * 0.06);
      c.beginPath(); c.moveTo(...this.px(x, bottom)); c.quadraticCurveTo(...this.px(x + lean, (bottom + top) / 2), ...this.px(x + lean * 0.5, top)); c.stroke();
      c.strokeStyle = rgba(o.light, 0.35); c.lineWidth = S * 0.04;
      c.beginPath(); c.moveTo(...this.px(x - 0.07, bottom)); c.quadraticCurveTo(...this.px(x + lean - 0.07, (bottom + top) / 2), ...this.px(x + lean * 0.5 - 0.07, top)); c.stroke();
    });
    // помости: дошки з проміжками, кінці трохи нерівні
    levels.forEach((y, k) => {
      for (let x = x0 - 0.3; x < x1 + 0.3; x += 0.42) {
        if (r() < 0.06) continue; // вибита дошка
        c.fillStyle = r() < 0.5 ? o.plank : o.post;
        c.fillRect(...this.px(x, y), 0.38 * S, 0.2 * S);
      }
      c.fillStyle = rgba(o.light, 0.55);
      c.fillRect(...this.px(x0 - 0.3, y + 0.02), (x1 - x0 + 0.6) * S, 0.035 * S);
      // драбина з цього помосту на наступний
      if (k < levels.length - 1) {
        const lx = x0 + (k % 2 ? (x1 - x0) * 0.75 : (x1 - x0) * 0.2), ny = levels[k + 1];
        c.strokeStyle = o.post; c.lineWidth = S * 0.07;
        [-0.25, 0.25].forEach(d => { c.beginPath(); c.moveTo(...this.px(lx + d, y)); c.lineTo(...this.px(lx + d + 0.1, ny)); c.stroke(); });
        c.lineWidth = S * 0.05;
        for (let yy = y + 0.35; yy < ny; yy += 0.4) { c.beginPath(); c.moveTo(...this.px(lx - 0.25 + (yy - y) * 0.03, yy)); c.lineTo(...this.px(lx + 0.25 + (yy - y) * 0.03, yy)); c.stroke(); }
      }
    });
    // мотузки, що звисають
    c.strokeStyle = rgba(o.rope, 0.7); c.lineWidth = S * 0.025;
    for (let i = 0; i < 3; i++) {
      const x = x0 + r() * (x1 - x0), y = levels[Math.floor(r() * levels.length)];
      c.beginPath(); c.moveTo(...this.px(x, y)); c.quadraticCurveTo(...this.px(x + 0.2, y - 0.8), ...this.px(x - 0.05, y - 1.2 - r() * 1.4)); c.stroke();
    }
    c.restore();
  };

  // Лампа шахтаря на гаку: скляний ковпак, усередині — кришталеве світло замість вогню.
  Frame.prototype.minerLamp = function (x, y, o) {
    const c = this.c, S = this.S;
    c.strokeStyle = o.iron; c.lineWidth = S * 0.04;
    c.beginPath(); c.moveTo(...this.px(x, y + 0.9)); c.lineTo(...this.px(x, y + 0.35)); c.stroke();
    this.glow(x, y, 2.2, o.light, 0.4);
    c.fillStyle = o.iron;
    c.beginPath(); c.moveTo(...this.px(x - 0.18, y + 0.32)); c.lineTo(...this.px(x + 0.18, y + 0.32)); c.lineTo(...this.px(x + 0.1, y + 0.4)); c.lineTo(...this.px(x - 0.1, y + 0.4)); c.closePath(); c.fill();
    c.fillStyle = rgba(o.light, 0.85);
    c.beginPath(); c.ellipse(...this.px(x, y + 0.08), S * 0.15, S * 0.24, 0, 0, Math.PI * 2); c.fill();
    c.fillStyle = o.iron; c.fillRect(...this.px(x - 0.18, y - 0.14), 0.36 * S, 0.08 * S);
    this.glow(x, y + 0.08, 0.35, "#ffffff", 0.8);
  };

  // Рейки вагонеток: дві лінії й шпали, з легким прогином.
  Frame.prototype.rails = function (x0, x1, y, o, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    for (let x = x0; x < x1; x += 0.55) {
      c.fillStyle = o.tie;
      c.fillRect(...this.px(x, y + 0.08), 0.32 * S, 0.1 * S);
    }
    c.strokeStyle = o.rail; c.lineWidth = S * 0.05;
    [0.12, 0.2].forEach(d => {
      c.beginPath();
      for (let x = x0; x <= x1; x += 0.3) c.lineTo(...this.px(x, y + d + (fbm(x * 0.3, d * 10, seed, 2) - 0.5) * 0.06));
      c.stroke();
    });
    void r;
  };

  // Вагонетка, що лишилась на рейках: коробка зі скосами, колеса, у ній — уламки кристала.
  Frame.prototype.cart = function (x, y, o, seed) {
    const c = this.c, S = this.S;
    c.fillStyle = o.body;
    c.beginPath(); c.moveTo(...this.px(x - 0.9, y + 1.0)); c.lineTo(...this.px(x + 0.9, y + 1.0)); c.lineTo(...this.px(x + 0.7, y + 0.3)); c.lineTo(...this.px(x - 0.7, y + 0.3)); c.closePath(); c.fill();
    c.strokeStyle = rgba(o.light, 0.4); c.lineWidth = S * 0.04;
    c.beginPath(); c.moveTo(...this.px(x - 0.9, y + 1.0)); c.lineTo(...this.px(x + 0.9, y + 1.0)); c.stroke();
    c.fillStyle = o.wheel;
    [-0.45, 0.45].forEach(d => { c.beginPath(); c.arc(...this.px(x + d, y + 0.28), S * 0.18, 0, Math.PI * 2); c.fill(); });
    this.crystalCluster(x + 0.1, y + 0.95, 0.9, o.crystal, seed, { glowAlpha: 0.25 });
  };

  // Іскри світла: чотирипроменеві зірочки там, де світло ловить грань.
  Frame.prototype.sparkles = function (count, x0, x1, y0, y1, color, seed) {
    const c = this.c, S = this.S, r = rng(seed);
    c.save(); c.globalCompositeOperation = "screen"; c.strokeStyle = rgba(color, 0.8); c.lineCap = "round";
    for (let i = 0; i < count; i++) {
      const [px, py] = this.px(x0 + r() * (x1 - x0), y0 + r() * (y1 - y0)), l = S * (0.06 + r() * 0.12);
      c.lineWidth = S * 0.018;
      c.beginPath(); c.moveTo(px - l, py); c.lineTo(px + l, py); c.moveTo(px, py - l); c.lineTo(px, py + l); c.stroke();
      c.fillStyle = rgba(color, 0.9); c.beginPath(); c.arc(px, py, S * 0.02, 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };
})();
