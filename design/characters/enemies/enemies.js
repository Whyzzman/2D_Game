// Вороги й бос Гробниці (2026-11-02, просьба користувача: «анімації, моделі, боси через Midjourney»;
// образи — концепти Midjourney design/characters/enemies/concept_*.png, обрані Claude за постійною згодою).
// Midjourney не тримає одного персонажа від кадру до кадру, тож кадри малює код за концептом: спільний
// скелет, на ньому — вигляд кожного (каптур, кольчуга, щит, корона), пози станів ключами з проміжними
// кадрами. Стопи — на нижньому краї кадру (півот спрайта — низ кадру), обличчям праворуч.
(function (root) {
  "use strict";
  const S = Math.sin, PI2 = Math.PI * 2, R = d => d * Math.PI / 180;
  const INK = "#07080c";

  const BASE = {
    x: 0, y: 0, lean: 0, sy: 1, sx: 1, head: 0,
    hipF: 6, kneeF: 8, hipB: -6, kneeB: 8,
    arm: 20, elbow: -15, armB: -10, elbowB: -10, weapon: 70,
    flare: 0.2, wave: 0, eyes: 1, glow: 0, slash: 0, slashFrom: -60, slashTo: 60,
    shield: 0, draw: 0, arrow: 1, alpha: 1, fall: 0,
    jaw: 0, tail: 0, spine: 0, roots: 0
  };
  const pose = p => Object.assign({}, BASE, p);
  function lerp(a, b, t) {
    const o = {};
    for (const k in a) o[k] = typeof a[k] === "number" ? a[k] + (b[k] - a[k]) * t : (t < 0.5 ? a[k] : b[k]);
    return o;
  }
  // Ключі — частки анімації (0…1): кадри розтягуються на будь-яку кількість.
  function track(n, keys) {
    const out = [];
    for (let f = 0; f < n; f++) {
      const u = n === 1 ? 0 : f / (n - 1);
      let i = 0;
      while (i < keys.length - 1 && keys[i + 1][0] <= u) i++;
      const [ua, pa] = keys[i], [ub, pb] = keys[Math.min(i + 1, keys.length - 1)];
      const t = ub === ua ? 0 : Math.min(1, (u - ua) / (ub - ua));
      out.push(lerp(pose(pa), pose(pb), t * t * (3 - 2 * t)));
    }
    return out;
  }
  const cycle = (n, fn) => { const o = []; for (let f = 0; f < n; f++) o.push(pose(fn(f / n))); return o; };

  // ---------- малювання: спільне ----------
  function stroke(g, w, color, pts) {
    g.lineCap = "round"; g.lineJoin = "round";
    g.strokeStyle = INK; g.lineWidth = w + 4; g.beginPath(); pts.forEach((p, i) => i ? g.lineTo(...p) : g.moveTo(...p)); g.stroke();
    g.strokeStyle = color; g.lineWidth = w; g.beginPath(); pts.forEach((p, i) => i ? g.lineTo(...p) : g.moveTo(...p)); g.stroke();
  }
  // кінцівка вниз від (x, y): a1 — від вертикалі (+ вперед), a2 — згин
  // Кінцівка — не палиця однієї товщини (2026-11-04, «на рівні Hollow Knight»): два звужені сегменти з
  // округлим суглобом, товстіші біля тіла; контур — той самий чорнильний, світло — з tools/characters/shade.py.
  function taper(g, a, b, wa, wb, color) {
    const dx = b[0] - a[0], dy = b[1] - a[1], len = Math.hypot(dx, dy) || 1, nx = -dy / len, ny = dx / len;
    // тіло сегмента — опуклий чотирикутник, кінці — повні круги (дуги з напрямками лишали дірки в суглобах)
    const body = (grow) => {
      const ra = wa / 2 + grow, rb = wb / 2 + grow, mx = (a[0] + b[0]) / 2, my = (a[1] + b[1]) / 2, bulge = (ra + rb) * 0.55;
      g.beginPath();
      g.moveTo(a[0] + nx * ra, a[1] + ny * ra);
      g.quadraticCurveTo(mx + nx * bulge, my + ny * bulge, b[0] + nx * rb, b[1] + ny * rb);
      g.lineTo(b[0] - nx * rb, b[1] - ny * rb);
      g.quadraticCurveTo(mx - nx * bulge, my - ny * bulge, a[0] - nx * ra, a[1] - ny * ra);
      g.closePath(); g.fill();
      g.beginPath(); g.arc(a[0], a[1], ra, 0, PI2); g.fill();
      g.beginPath(); g.arc(b[0], b[1], rb, 0, PI2); g.fill();
    };
    g.fillStyle = INK; body(2);
    g.fillStyle = color; body(0);
  }
  function limb(g, x, y, a1, l1, a2, l2, w, color, w2) {
    const r1 = R(a1), r2 = R(a1 - a2);
    const k = [x + S(r1) * l1, y + Math.cos(r1) * l1], e = [k[0] + S(r2) * l2, k[1] + Math.cos(r2) * l2];
    const lower = w2 || w;
    taper(g, [x, y], k, w * 1.2, w * 0.85, color);
    taper(g, k, e, lower * 0.95, lower * 0.62, color);
    return { knee: k, end: e, dir: r2 };
  }
  function shape(g, fill, pts, outline) {
    g.beginPath(); pts.forEach((p, i) => i ? g.lineTo(...p) : g.moveTo(...p)); g.closePath();
    g.lineJoin = "round"; g.strokeStyle = INK; g.lineWidth = outline || 4; g.stroke(); g.fillStyle = fill; g.fill();
  }
  function eye(g, x, y, r, open, color) {
    if (open < 0.05) return;
    const gl = g.createRadialGradient(x, y, 0, x, y, r * 3);
    gl.addColorStop(0, color + "aa"); gl.addColorStop(1, color + "00");
    g.fillStyle = gl; g.beginPath(); g.arc(x, y, r * 3, 0, PI2); g.fill();
    // ядро ока — його колір (видно, що це око, а не відблиск сталі), у центрі білий відблиск;
    // перед ударом (open > 1) відблиск розгоряється — замах читається по очах
    // перед ударом — відблиск, а не білий диск на пів обличчя (2026-11-06): широкий м'який ореол кольору ока, невелике
    // біле ядро й два тонкі промені навхрест; замах читається так само ясно (вирішено Claude за постійною згодою)
    const over = Math.max(0, open - 1);
    if (over > 0.05) {
      const halo = g.createRadialGradient(x, y, 0, x, y, r * (3 + over * 6));
      halo.addColorStop(0, color + "cc"); halo.addColorStop(0.35, color + "55"); halo.addColorStop(1, color + "00");
      g.fillStyle = halo; g.beginPath(); g.arc(x, y, r * (3 + over * 6), 0, PI2); g.fill();
    }
    g.fillStyle = color; g.beginPath(); g.ellipse(x, y, r * 1.15, r * 1.15 * Math.min(1, open), 0, 0, PI2); g.fill();
    const core = r * (0.35 + over * 0.8);
    g.fillStyle = "#fffaf0"; g.beginPath(); g.ellipse(x, y, core, core * Math.min(1, open), 0, 0, PI2); g.fill();
    if (over > 0.05) {
      const len = r * (2 + over * 7), wid = r * 0.22;
      g.fillStyle = "rgba(255,250,240,0.9)";
      g.beginPath(); g.moveTo(x - len, y); g.lineTo(x, y - wid); g.lineTo(x + len, y); g.lineTo(x, y + wid); g.closePath(); g.fill();
      g.beginPath(); g.moveTo(x, y - len * 0.6); g.lineTo(x + wid, y); g.lineTo(x, y + len * 0.6); g.lineTo(x - wid, y); g.closePath(); g.fill();
    }
  }
  function blade(g, hx, hy, ang, len, wid, colors, broken) {
    g.save(); g.translate(hx, hy); g.rotate(R(ang));
    const L = broken ? len * 0.7 : len;
    shape(g, colors.blade, broken ? [[6, -wid], [L, -wid * 0.6], [L - 6, wid * 0.3], [L + 2, wid], [6, wid]] : [[6, -wid], [L, -2], [L + 8, 0], [L, 2], [6, wid]], 3);
    g.fillStyle = INK; g.fillRect(2, -wid - 5, 6, wid * 2 + 10);
    g.fillStyle = colors.guard; g.fillRect(3, -wid - 4, 4, wid * 2 + 8);
    g.fillStyle = colors.grip; g.fillRect(-9, -2.5, 12, 5);
    g.restore();
  }
  function slashArc(g, cx, cy, r, from, to, a) {
    if (a <= 0.01) return;
    g.save(); g.globalAlpha = a;
    const f = R(from), t = R(to), ccw = to < from;
    const gr = g.createRadialGradient(cx, cy, r * 0.55, cx, cy, r * 1.1);
    gr.addColorStop(0, "rgba(220,200,255,0)"); gr.addColorStop(0.7, "rgba(225,210,255,0.8)"); gr.addColorStop(1, "rgba(220,200,255,0)");
    g.fillStyle = gr; g.beginPath(); g.arc(cx, cy, r * 1.05, f, t, ccw); g.arc(cx, cy, r * 0.6, t, f, !ccw); g.closePath(); g.fill();
    g.restore();
  }
  function rags(g, x0, y0, x1, w, len, p, color, n) {
    // рване полотнище від (x0..x1, y0) вниз, хвилюється й відлітає назад з flare
    const pts = [[x0, y0], [x1, y0]];
    for (let i = 0; i <= n; i++) {
      const t = i / n, x = x1 + (x0 - x1) * t - p.flare * 14 * t, dip = (i % 2 ? len : len * 0.72) + S(i * 1.9 + p.wave * PI2) * 5;
      pts.push([x - p.flare * 8, y0 + dip * (1 - p.flare * 0.25)]);
    }
    shape(g, color, pts, 4);
  }

  // ---------- персонажі ----------
  const ZOMBIE = {
    w: 140, h: 176, hip: 62, thigh: 31, shin: 31, eyes: "#b98cff",
    colors: { blade: "#9a8f80", guard: "#5d4a36", grip: "#3a2e24" },
    draw(g, p, f) {
      const c = { skin: "#7f8a74", skinDark: "#56604f", mail: "#6e6f74", tabard: "#3d5a7a", tabardDark: "#283c52", pants: "#2c2a30", helm: "#7a7c82" };
      const hip = -this.hip;
      const back = limb(g, -4, hip, p.hipB, this.thigh, p.kneeB, this.shin, 10, c.pants);
      boot(g, back.end, "#2a2422");
      // задня рука — звисає мертво
      limb(g, -10, hip - 46, p.armB, 22, p.elbowB, 20, 8, c.skinDark);
      // тулуб: кольчуга, згорблені плечі, рвана табарда
      g.save(); g.translate(0, hip); g.rotate(R(14 + p.lean * 0.4));
      shape(g, c.mail, [[-14, 2], [-18, -38], [-6, -52], [16, -50], [20, -34], [14, 2]]);
      g.strokeStyle = "rgba(0,0,0,0.35)"; g.lineWidth = 1;
      for (let y = -46; y < 0; y += 5) { g.beginPath(); g.moveTo(-16, y); g.lineTo(16, y + 1); g.stroke(); }
      rags(g, -12, -44, 12, 24, 52, p, c.tabard, 6);
      g.fillStyle = c.tabardDark; g.fillRect(-2, -42, 4, 30);
      g.restore();
      const fr = limb(g, 4, hip, p.hipF, this.thigh, p.kneeF, this.shin, 10, c.pants);
      boot(g, fr.end, "#2a2422");
      // голова: висуплена вперед, пом'ятий капелюх-шолом, очі
      const hx = 18 + p.lean * 0.25, hy = hip - 58;
      g.save(); g.translate(hx, hy); g.rotate(R(p.head + 10));
      shape(g, c.skin, [[-10, 2], [-11, -12], [-2, -18], [10, -14], [12, -2], [6, 8], [-6, 8]]);
      shape(g, c.helm, [[-18, -10], [18, -12], [12, -16], [6, -24], [-8, -24], [-13, -16]]);
      g.fillStyle = "rgba(0,0,0,0.55)"; g.fillRect(-9, -10, 20, 8);
      eye(g, 2, -6, 2.4, p.eyes, this.eyes); eye(g, 9, -6.5, 2, p.eyes, this.eyes);
      g.restore();
      // передня рука зі зламаним мечем
      slashArc(g, 14, hip - 40, 62, p.slashFrom, p.slashTo, p.slash);
      const arm = limb(g, 10, hip - 48, p.arm, 22, p.elbow, 20, 8, c.skin);
      blade(g, arm.end[0], arm.end[1], p.weapon, 58, 4, this.colors, true);
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.5, lean: 6 + S(t * PI2) * 2, head: S(t * PI2 + 1) * 5, arm: 10, weapon: 85, armB: 4, wave: t })),
      Patrol: n => cycle(n, t => ({ lean: 10, y: -Math.abs(S(t * PI2)) * 3, hipF: 22 * S(t * PI2), kneeF: 12 + 18 * Math.max(0, -S(t * PI2)), hipB: -22 * S(t * PI2), kneeB: 12 + 18 * Math.max(0, S(t * PI2)), arm: 8 - 10 * S(t * PI2), weapon: 95, armB: 10 * S(t * PI2), head: S(t * PI2 * 2) * 4, wave: t, flare: 0.3 })),
      Chase: n => cycle(n, t => ({ lean: 20, y: -Math.abs(S(t * PI2)) * 5, hipF: 32 * S(t * PI2), kneeF: 18 + 26 * Math.max(0, -S(t * PI2)), hipB: -32 * S(t * PI2), kneeB: 18 + 26 * Math.max(0, S(t * PI2)), arm: 40, weapon: 150, armB: -30 + 20 * S(t * PI2), wave: t, flare: 0.6 })),
      Alert: n => track(n, [[0, {}], [0.4, { head: -18, lean: -6, eyes: 1.4, y: -3 }], [1, { head: -6, lean: 4, arm: 40, weapon: 140 }]]),
      AttackLight: n => track(n, [[0, { arm: 20, weapon: 90 }], [0.3, { lean: -8, arm: -70, elbow: -40, weapon: -130, hipF: 10, kneeF: 20, flare: 0.4, eyes: 1.3 }], [0.42, { lean: -14, arm: -85, weapon: -150, y: 3, eyes: 1.6 }], [0.47, { lean: 22, x: 8, arm: 85, elbow: 0, weapon: 40, slash: 1, slashFrom: -100, slashTo: 50, hipF: 30, kneeF: 18, hipB: -26, flare: 0.8 }], [0.58, { lean: 20, x: 9, arm: 95, weapon: 70, slash: 0.3, slashFrom: -100, slashTo: 50 }], [1, { lean: 8, arm: 20, weapon: 90 }]]),
      Hurt: n => track(n, [[0, { lean: -24, x: -6, head: -18, eyes: 0.4, arm: -30, weapon: 160, flare: 0.8 }], [1, { lean: 4 }]]),
      Stunned: n => cycle(n, t => ({ lean: 4 + S(t * PI2) * 10, head: S(t * PI2) * 16, eyes: 0.4, arm: 30, weapon: 100, wave: t })),
      Death: n => track(n, [[0, { lean: -20, head: -15, eyes: 0.6 }], [0.4, { lean: 30, y: 10, hipF: 50, kneeF: 80, hipB: 10, kneeB: 80, arm: 70, weapon: 150, eyes: 0.2 }], [1, { lean: 80, y: 28, sy: 0.85, hipF: 80, kneeF: 100, hipB: 40, kneeB: 100, arm: 90, weapon: 175, eyes: 0, alpha: 0.9 }]])
    }
  };

  function boot(g, at, color) {
    g.fillStyle = INK; g.beginPath(); g.ellipse(at[0] + 4, at[1] + 1, 11, 6, 0, 0, PI2); g.fill();
    g.fillStyle = color; g.beginPath(); g.ellipse(at[0] + 4, at[1], 9, 4.5, 0, 0, PI2); g.fill();
  }

  const ARCHER = {
    w: 150, h: 176, hip: 70, thigh: 35, shin: 34, eyes: "#b98cff",
    draw(g, p) {
      const c = { bone: "#cdc4ad", boneDark: "#8f8672", rag: "#2b2638", ragLight: "#463d58", bow: "#4a3a2c", string: "#d8d0c0" };
      const hip = -this.hip;
      // плащ-лахміття позаду
      g.save(); g.translate(-4, hip - 52);
      rags(g, -14, 0, 8, 22, 70, p, c.rag, 8);
      g.restore();
      const back = limb(g, -3, hip, p.hipB, this.thigh, p.kneeB, this.shin, 4, c.bone);
      boot(g, back.end, c.boneDark);
      // колчан
      g.save(); g.translate(-12, hip - 50); g.rotate(R(-20));
      shape(g, "#3a2c22", [[-5, 0], [5, 0], [6, 34], [-6, 34]], 3);
      g.strokeStyle = INK; g.lineWidth = 2; for (let i = -1; i <= 1; i++) { g.beginPath(); g.moveTo(i * 3, 0); g.lineTo(i * 4, -12); g.stroke(); }
      g.restore();
      // ребра й хребет
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.5));
      stroke(g, 4, c.bone, [[0, 0], [2, -50]]);
      for (let i = 0; i < 4; i++) stroke(g, 2.5, c.bone, [[2, -22 - i * 7], [11, -18 - i * 7]]);
      shape(g, c.bone, [[-8, 0], [8, 0], [5, 6], [-5, 6]], 3);
      g.restore();
      const fr = limb(g, 3, hip, p.hipF, this.thigh, p.kneeF, this.shin, 4, c.bone);
      boot(g, fr.end, c.boneDark);
      // задня рука тягне тятиву
      const shoulder = [4 + p.lean * 0.4, hip - 50];
      const bowArm = limb(g, shoulder[0], shoulder[1], p.arm, 22, p.elbow, 20, 3.5, c.bone);
      const pull = 8 + p.draw * 26;
      const hand = [bowArm.end[0] - pull, bowArm.end[1] + 2];
      stroke(g, 3.5, c.bone, [[shoulder[0] - 4, shoulder[1] + 2], [hand[0] + 4, hand[1] + 10], hand]);
      // лук: дуга перед рукою, тятива до руки, що тягне
      const bx = bowArm.end[0], by = bowArm.end[1];
      // лук стоїть дугою перед рукою (weapon 90 — вертикально), тятива — від кінців до руки, що тягне
      const ba = R(p.weapon), rot = (x, y) => [bx + x * Math.cos(ba) - y * Math.sin(ba), by + x * Math.sin(ba) + y * Math.cos(ba)];
      g.save(); g.translate(bx, by); g.rotate(ba);
      g.strokeStyle = INK; g.lineWidth = 7; g.beginPath(); g.moveTo(-46, -4); g.quadraticCurveTo(0, 22, 46, -4); g.stroke();
      g.strokeStyle = c.bow; g.lineWidth = 4; g.beginPath(); g.moveTo(-46, -4); g.quadraticCurveTo(0, 22, 46, -4); g.stroke();
      g.restore();
      const tips = [rot(-46, -4), rot(46, -4)];
      g.strokeStyle = c.string; g.lineWidth = 1.2; g.beginPath(); g.moveTo(...tips[0]); g.lineTo(...hand); g.lineTo(...tips[1]); g.stroke();
      if (p.arrow > 0.5) stroke(g, 1.8, "#1c1712", [hand, [bx + 20, by]]);
      // голова: череп у каптурі
      g.save(); g.translate(8 + p.lean * 0.5, hip - 64); g.rotate(R(p.head));
      shape(g, c.rag, [[-16, 12], [-18, -6], [-8, -22], [-20, -30], [4, -24], [14, -10], [14, 8], [2, 14]]);
      g.fillStyle = "#08080b"; g.beginPath(); g.ellipse(4, -2, 9, 10, 0, 0, PI2); g.fill();
      g.fillStyle = c.boneDark; g.beginPath(); g.ellipse(5, 2, 6, 7, 0, 0, PI2); g.fill();
      eye(g, 3, -1, 2, p.eyes, this.eyes); eye(g, 9, -1.5, 1.7, p.eyes, this.eyes);
      g.restore();
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.2, arm: 40, elbow: -10, weapon: 90, head: S(t * PI2) * 4, wave: t, draw: 0 })),
      Patrol: n => cycle(n, t => ({ lean: 4, y: -Math.abs(S(t * PI2)) * 3, hipF: 24 * S(t * PI2), kneeF: 10 + 20 * Math.max(0, -S(t * PI2)), hipB: -24 * S(t * PI2), kneeB: 10 + 20 * Math.max(0, S(t * PI2)), arm: 30, weapon: 100, wave: t, flare: 0.3 })),
      Chase: n => cycle(n, t => ({ lean: 14, y: -Math.abs(S(t * PI2)) * 5, hipF: 34 * S(t * PI2), kneeF: 16 + 28 * Math.max(0, -S(t * PI2)), hipB: -34 * S(t * PI2), kneeB: 16 + 28 * Math.max(0, S(t * PI2)), arm: 50, weapon: 110, wave: t, flare: 0.6 })),
      Alert: n => track(n, [[0, {}], [0.5, { head: -14, y: -3, eyes: 1.4 }], [1, { arm: 80, weapon: 90 }]]),
      // довгий натяг і короткий постріл — у цьому весь сигнал
      AttackLight: n => track(n, [[0, { arm: 40, weapon: 90 }], [0.25, { arm: 85, elbow: 0, weapon: 90, lean: -4 }], [0.68, { arm: 88, weapon: 90, draw: 1, lean: -8, eyes: 1.5, hipF: 16, hipB: -16 }], [0.72, { arm: 88, weapon: 90, draw: 0, arrow: 0, lean: 4, x: -3 }], [1, { arm: 50, weapon: 95, arrow: 0 }]]),
      ShieldBash: n => track(n, [[0, {}], [0.3, { arm: -40, weapon: 40, lean: -10 }], [0.45, { arm: 100, weapon: 150, lean: 18, x: 8, slash: 1, slashFrom: -60, slashTo: 60 }], [1, {}]]),
      Hurt: n => track(n, [[0, { lean: -20, x: -6, head: -16, eyes: 0.4, flare: 0.8 }], [1, {}]]),
      Stunned: n => cycle(n, t => ({ lean: S(t * PI2) * 10, head: S(t * PI2) * 14, eyes: 0.4, arm: 20, weapon: 120, wave: t })),
      Death: n => track(n, [[0, { lean: -10, eyes: 0.6 }], [0.5, { y: 18, sy: 0.8, hipF: 60, kneeF: 100, hipB: 20, kneeB: 100, lean: 20, arm: 10, weapon: 160, eyes: 0.2 }], [1, { y: 36, sy: 0.7, lean: 70, hipF: 80, kneeF: 120, hipB: 40, kneeB: 120, arm: 0, weapon: 175, eyes: 0, alpha: 0.85 }]])
    }
  };

  const GUARD = {
    w: 168, h: 176, hip: 64, thigh: 30, shin: 30, eyes: "#c79bff",
    draw(g, p) {
      const c = { armor: "#4a4658", armorLight: "#6a6580", armorDark: "#2a2734", shield: "#5b5568", emblem: "#cfc3a4", crack: "#b07cff", mace: "#3c3a44" };
      const hip = -this.hip;
      const back = limb(g, -6, hip, p.hipB, this.thigh, p.kneeB, this.shin, 14, c.armorDark);
      boot(g, back.end, "#1e1c26");
      // задня рука з булавою
      const ma = limb(g, -14, hip - 50, p.armB, 22, p.elbowB, 20, 11, c.armorDark);
      g.save(); g.translate(ma.end[0], ma.end[1]); g.rotate(R(p.weapon - 90));
      stroke(g, 4, "#4a3a2c", [[0, 0], [0, -34]]);
      g.fillStyle = INK; g.beginPath(); g.arc(0, -40, 11, 0, PI2); g.fill();
      g.fillStyle = c.mace; g.beginPath(); g.arc(0, -40, 9, 0, PI2); g.fill();
      for (let i = 0; i < 6; i++) { const a = i * PI2 / 6; shape(g, c.mace, [[S(a) * 8, -40 + Math.cos(a) * 8], [S(a) * 14, -40 + Math.cos(a) * 14], [S(a + 0.3) * 8, -40 + Math.cos(a + 0.3) * 8]], 2); }
      g.restore();
      // тулуб: брила обладунку з тріщинами, що світять
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      shape(g, c.armor, [[-20, 4], [-24, -40], [-12, -56], [16, -56], [26, -40], [20, 4]]);
      g.fillStyle = c.armorLight; g.fillRect(-18, -50, 36, 6);
      g.strokeStyle = c.crack; g.lineWidth = 2; g.shadowColor = c.crack; g.shadowBlur = 6 + p.glow * 8;
      g.beginPath(); g.moveTo(-6, -40); g.lineTo(0, -30); g.lineTo(-4, -20); g.lineTo(4, -10); g.stroke();
      g.shadowBlur = 0;
      g.restore();
      const fr = limb(g, 6, hip, p.hipF, this.thigh, p.kneeF, this.shin, 14, c.armor);
      boot(g, fr.end, "#1e1c26");
      // високий шолом із гребенем
      g.save(); g.translate(4 + p.lean * 0.3, hip - 66); g.rotate(R(p.head));
      shape(g, c.armor, [[-13, 12], [-14, -30], [-6, -40], [8, -40], [14, -30], [13, 12]]);
      shape(g, c.armorDark, [[-3, -40], [3, -40], [2, -52], [-2, -52]], 3);
      g.fillStyle = "#08080b"; g.fillRect(-2, -12, 15, 5);
      eye(g, 4, -9.5, 2, p.eyes, this.eyes); eye(g, 10, -9.5, 1.8, p.eyes, this.eyes);
      g.restore();
      // баштовий щит попереду: піднятий — вище й ближче
      const sx = 24 + p.shield * 6 + (p.x > 0 ? p.x * 0.3 : 0), sy = hip - 18 - p.shield * 16;
      g.save(); g.translate(sx, sy); g.rotate(R(p.lean * 0.3 - 4 + p.shield * 4));
      shape(g, c.shield, [[-14, -42], [14, -46], [16, 30], [0, 40], [-16, 30]], 5);
      g.strokeStyle = c.emblem; g.lineWidth = 3;
      g.beginPath(); g.moveTo(-8, -6); g.lineTo(8, 20); g.moveTo(8, -6); g.lineTo(-8, 20); g.stroke();
      g.fillStyle = c.emblem; g.beginPath(); g.moveTo(-7, -20); g.lineTo(-4, -28); g.lineTo(0, -22); g.lineTo(4, -28); g.lineTo(7, -20); g.closePath(); g.fill();
      g.restore();
      slashArc(g, 0, hip - 46, 64, p.slashFrom, p.slashTo, p.slash);
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.2, weapon: 110, armB: -20, head: S(t * PI2) * 2 })),
      Patrol: n => cycle(n, t => ({ lean: 4, y: -Math.abs(S(t * PI2)) * 2, hipF: 18 * S(t * PI2), kneeF: 8 + 14 * Math.max(0, -S(t * PI2)), hipB: -18 * S(t * PI2), kneeB: 8 + 14 * Math.max(0, S(t * PI2)), weapon: 120, armB: -16 })),
      Chase: n => cycle(n, t => ({ lean: 12, y: -Math.abs(S(t * PI2)) * 4, hipF: 26 * S(t * PI2), kneeF: 12 + 20 * Math.max(0, -S(t * PI2)), hipB: -26 * S(t * PI2), kneeB: 12 + 20 * Math.max(0, S(t * PI2)), weapon: 140, armB: -30, shield: 0.4 })),
      Alert: n => track(n, [[0, {}], [0.5, { head: -10, eyes: 1.4, shield: 0.5, glow: 0.6 }], [1, { shield: 0.3 }]]),
      GuardRaise: n => track(n, [[0, {}], [1, { shield: 1, lean: -4, hipF: 20, kneeF: 20, hipB: -22, kneeB: 14, y: 3 }]]),
      GuardIdle: n => cycle(n, t => ({ shield: 1, lean: -4, hipF: 20, kneeF: 20, hipB: -22, kneeB: 14, y: 3 + S(t * PI2) })),
      GuardImpact: n => track(n, [[0, { shield: 1, lean: -14, x: -8, y: 3, glow: 1 }], [1, { shield: 1, lean: -4, y: 3 }]]),
      GuardBreak: n => track(n, [[0, { shield: 1, lean: -18, x: -8 }], [0.3, { shield: -0.4, lean: -26, x: -10, head: -14, eyes: 0.4, armB: -60 }], [1, { shield: 0, lean: -8, eyes: 0.6 }]]),
      AttackLight: n => track(n, [[0, {}], [0.38, { armB: -150, elbowB: -40, weapon: -60, lean: -8, eyes: 1.3, glow: 0.4 }], [0.45, { armB: -160, weapon: -80, lean: -12, eyes: 1.6 }], [0.5, { armB: 70, elbowB: 0, weapon: 120, lean: 18, x: 8, slash: 1, slashFrom: -110, slashTo: 40, hipF: 28, hipB: -24 }], [0.62, { armB: 80, weapon: 140, lean: 16, x: 8, slash: 0.3, slashFrom: -110, slashTo: 40 }], [1, {}]]),
      ShieldBash: n => track(n, [[0, {}], [0.32, { shield: 0.8, lean: -10, x: -4, glow: 0.4 }], [0.4, { shield: 1, lean: 20, x: 14, hipF: 30, hipB: -28, glow: 1 }], [0.55, { shield: 0.8, lean: 14, x: 12 }], [1, {}]]),
      AttackHeavy: n => track(n, [[0, {}], [0.35, { armB: -170, elbowB: -20, weapon: -90, lean: -14, y: -4, eyes: 1.4, glow: 0.8 }], [0.44, { armB: -175, weapon: -95, lean: -16, y: -6, eyes: 1.8, glow: 1 }], [0.48, { armB: 40, elbowB: 0, weapon: 175, lean: 28, x: 12, y: 4, slash: 1, slashFrom: -130, slashTo: 90, hipF: 34, kneeF: 30, hipB: -30, glow: 1 }], [0.62, { armB: 50, weapon: 175, lean: 26, x: 12, y: 4, slash: 0.3, slashFrom: -130, slashTo: 90 }], [1, {}]]),
      Hurt: n => track(n, [[0, { lean: -18, x: -6, head: -12, eyes: 0.5, glow: 0.8 }], [1, {}]]),
      Stunned: n => cycle(n, t => ({ lean: S(t * PI2) * 8, head: S(t * PI2) * 12, eyes: 0.4, weapon: 150, shield: -0.3 })),
      Death: n => track(n, [[0, { lean: -14, glow: 1 }], [0.4, { y: 10, sy: 0.9, hipF: 50, kneeF: 80, hipB: 10, kneeB: 80, lean: 20, shield: -0.5, eyes: 0.3, glow: 0.5 }], [1, { y: 30, sy: 0.8, lean: 75, hipF: 80, kneeF: 100, hipB: 40, kneeB: 100, shield: -1, weapon: 175, eyes: 0, glow: 0, alpha: 0.9 }]])
    }
  };

  // Вартовий Гробниці: монументальний лицар з короною на шоломі, багряний плащ, щит із короною й полум'ям,
  // величезний меч; у другій фазі з тріснутих грудей валить фіолетовий дим (glow).
  const GUARDIAN = {
    w: 272, h: 300, hip: 110, thigh: 52, shin: 52, eyes: "#ffb27a",
    draw(g, p) {
      // Вигляд підміняють боси на цьому ж ріґу (enemies-vc.js): look — кольори, headgear, weapon, shield, back.
      const c = Object.assign({ armor: "#3a3340", armorLight: "#5c5266", armorDark: "#1e1a22", cape: "#6e1e22", capeDark: "#40121a", crown: "#c9a24e", shield: "#3a2c2c", emblem: "#e2843c", blade: "#a8a2a0", crack: "#b07cff" }, this.look);
      const hip = -this.hip;
      if (this.back === "bell") {
        // дзвін на спині Дзвонаря
        g.save(); g.translate(-38, hip - 96); g.rotate(R(-8 + p.lean * 0.2));
        shape(g, c.bell || "#6e522c", [[-30, 40], [-24, -20], [-12, -40], [12, -40], [24, -20], [30, 40]], 5);
        g.fillStyle = "rgba(255,210,140,0.25)"; g.fillRect(-20, -30, 8, 60);
        g.restore();
      }
      // плащ позаду
      g.save(); g.translate(-10, hip - 92);
      rags(g, -34, 0, 22, 50, 150, p, c.cape, 9);
      g.restore();
      const back = limb(g, -10, hip, p.hipB, this.thigh, p.kneeB, this.shin, 24, c.armorDark);
      boot(g, back.end, c.armorDark); boot(g, [back.end[0] + 8, back.end[1]], c.armorDark);
      // меч у задній руці (замахи — нею)
      const sa = limb(g, -18, hip - 90, p.armB, 40, p.elbowB, 36, 18, c.armorDark);
      g.save(); g.translate(sa.end[0], sa.end[1]); g.rotate(R(p.weapon));
      if (this.weapon === "hammer") {
        // молот дзвонаря: довге древко, важкий бронзовий обух
        g.fillStyle = INK; g.fillRect(-20, -6, 130, 12); g.fillStyle = "#3a2a20"; g.fillRect(-18, -4, 126, 8);
        shape(g, c.blade, [[96, -30], [132, -30], [138, -20], [138, 20], [132, 30], [96, 30], [90, 20], [90, -20]], 5);
        g.fillStyle = "rgba(255,220,160,0.25)"; g.fillRect(100, -24, 6, 48);
      } else if (this.weapon === "crook") {
        // посох-гак Оленя: криве древко, на кінці — загнутий кістяний ріг
        g.fillStyle = INK; g.fillRect(-24, -6, 140, 12); g.fillStyle = "#3a3224"; g.fillRect(-22, -4, 136, 8);
        // загнутий ріг: товстий біля древка, тоншає до вістря, що дивиться назад
        shape(g, c.blade, [[104, -7], [124, -16], [142, -12], [152, 2], [150, 20], [140, 34], [142, 18], [138, 4], [126, -2], [106, 6]], 4);
        stroke(g, 1.5, "rgba(0,0,0,0.3)", [[118, -6], [136, -6], [144, 6]]);
      } else {
        const L = this.weapon === "greatsword" ? 1.3 : 1;
        shape(g, c.blade, [[10, -9 * L], [120 * L, -5 * L], [134 * L, 0], [120 * L, 5 * L], [10, 9 * L]], 4);
        g.fillStyle = INK; g.fillRect(2, -20, 10, 40); g.fillStyle = c.crown; g.fillRect(4, -18, 6, 36);
        g.fillStyle = "#3a2a20"; g.fillRect(-18, -5, 22, 10);
      }
      g.restore();
      // тулуб: важкий трапецієподібний обладунок, тріщина на грудях
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.35));
      shape(g, c.armor, [[-34, 6], [-46, -74], [-26, -100], [30, -100], [48, -74], [34, 6]], 5);
      g.fillStyle = c.armorLight; g.fillRect(-36, -92, 72, 10);
      g.strokeStyle = c.crack; g.lineWidth = 3 + p.glow * 2; g.shadowColor = c.crack; g.shadowBlur = 10 + p.glow * 20;
      g.beginPath(); g.moveTo(-6, -84); g.lineTo(4, -66); g.lineTo(-4, -50); g.lineTo(8, -36); g.stroke();
      g.shadowBlur = 0;
      if (p.glow > 0.05) {
        const sm = g.createRadialGradient(0, -70, 4, 0, -110, 90);
        const smoke = this.smoke || "170,110,255";
        sm.addColorStop(0, `rgba(${smoke},${0.45 * Math.min(1, p.glow)})`); sm.addColorStop(1, `rgba(${smoke},0)`);
        g.fillStyle = sm; g.beginPath(); g.ellipse(-10, -120, 60, 80, -0.3, 0, PI2); g.fill();
      }
      g.restore();
      const fr = limb(g, 10, hip, p.hipF, this.thigh, p.kneeF, this.shin, 24, c.armor);
      boot(g, fr.end, c.armorDark); boot(g, [fr.end[0] + 8, fr.end[1]], c.armorDark);
      // шолом із короною
      g.save(); g.translate(6 + p.lean * 0.5, hip - 120); g.rotate(R(p.head));
      shape(g, c.armor, [[-20, 18], [-22, -26], [-10, -36], [14, -36], [22, -24], [20, 18]], 5);
      g.fillStyle = "#06060a"; g.fillRect(-4, -12, 24, 6);
      eye(g, 6, -9, 3, p.eyes, this.eyes); eye(g, 15, -9, 2.6, p.eyes, this.eyes);
      if (this.headgear === "horns") {
        shape(g, c.crown, [[-18, -30], [-34, -44], [-38, -64], [-26, -46], [-12, -36]], 3);
        shape(g, c.crown, [[16, -32], [30, -48], [30, -68], [22, -48], [8, -36]], 3);
      } else if (this.headgear === "antlers") {
        // Чумний Олень: оленячий череп замість шолома, роги ширші за плечі, мох звисає з відростків
        shape(g, c.crown, [[-14, 20], [-18, -20], [-6, -34], [14, -32], [26, -10], [34, 6], [22, 14], [8, 22]], 4);
        g.fillStyle = "#06060a"; g.beginPath(); g.ellipse(2, -12, 6, 7, 0, 0, PI2); g.fill(); g.beginPath(); g.ellipse(14, -10, 5, 6, 0, 0, PI2); g.fill();
        eye(g, 2, -12, 2.6, p.eyes, this.eyes); eye(g, 14, -10, 2.2, p.eyes, this.eyes);
        const tine = (x0, y0, pts) => stroke(g, 5, c.crown, [[x0, y0], ...pts]);
        tine(-8, -30, [[-26, -44], [-50, -50], [-72, -44]]); tine(-26, -44, [[-34, -62], [-30, -74]]); tine(-50, -50, [[-62, -66]]);
        tine(8, -32, [[22, -46], [44, -54], [66, -50]]); tine(22, -46, [[20, -64], [28, -76]]); tine(44, -54, [[56, -70]]);
        stroke(g, 2, c.cape, [[-50, -50], [-52, -36 + S(p.wave * PI2) * 3]]); stroke(g, 2, c.cape, [[44, -54], [46, -40 + S(p.wave * PI2 + 1) * 3]]);
      } else if (this.headgear === "hood") {
        shape(g, c.cape, [[-26, 22], [-28, -24], [-10, -46], [18, -44], [28, -22], [26, 20], [14, 8], [-12, 10]], 4);
        g.fillStyle = "#06060a"; g.beginPath(); g.ellipse(6, -8, 13, 14, 0, 0, PI2); g.fill();
        eye(g, 2, -9, 3, p.eyes, this.eyes); eye(g, 12, -9, 2.6, p.eyes, this.eyes);
      } else {
        shape(g, c.crown, [[-18, -34], [18, -34], [18, -44], [12, -38], [8, -52], [2, -40], [-4, -54], [-8, -40], [-14, -50], [-18, -42]], 3);
      }
      g.restore();
      if (this.noShield) { slashArc(g, -6, hip - 80, 140, p.slashFrom, p.slashTo, p.slash); return; }
      // щит попереду
      const shx = 46 + p.shield * 10 + Math.max(0, p.x) * 0.3, shy = hip - 44 - p.shield * 20;
      g.save(); g.translate(shx, shy); g.rotate(R(p.lean * 0.3 - 6 + p.shield * 6));
      shape(g, c.shield, [[-30, -62], [30, -66], [32, 40], [0, 70], [-32, 40]], 6);
      g.strokeStyle = c.crown; g.lineWidth = 3; g.strokeRect(-24, -56, 48, 90);
      g.fillStyle = c.emblem;
      g.beginPath(); g.moveTo(0, -16); g.quadraticCurveTo(16, 10, 2, 30); g.quadraticCurveTo(8, 12, -2, 4); g.quadraticCurveTo(-14, 16, -6, 30); g.quadraticCurveTo(-18, 6, 0, -16); g.fill();
      g.fillStyle = c.crown; g.beginPath(); g.moveTo(-12, -30); g.lineTo(-8, -44); g.lineTo(-2, -34); g.lineTo(4, -46); g.lineTo(10, -34); g.lineTo(14, -44); g.lineTo(16, -30); g.closePath(); g.fill();
      g.restore();
      slashArc(g, -6, hip - 80, 140, p.slashFrom, p.slashTo, p.slash);
    },
    states: {}
  };
  // Стани боса (BossAnimationState): ключі на частки, удар — там, де його ставить атака боса.
  (function () {
    const swing = (contact, from, to, heavy) => [[0, {}], [contact - 0.12, { armB: -150, elbowB: -30, weapon: -55, lean: -10, eyes: 1.3 }], [contact - 0.03, { armB: -160, weapon: -65, lean: -14, y: heavy ? -6 : 0, eyes: 1.7 }],
      [contact, { armB: 60, elbowB: 0, weapon: heavy ? 100 : 60, lean: 24, x: 16, slash: 1, slashFrom: from, slashTo: to, hipF: 30, kneeF: 24, hipB: -28 }], [contact + 0.12, { armB: 70, weapon: heavy ? 110 : 80, lean: 20, x: 16, slash: 0.3, slashFrom: from, slashTo: to }], [1, {}]];
    const idle = (g) => n => cycle(n, t => ({ y: S(t * PI2) * 2, lean: 2, armB: -10, weapon: 100, head: S(t * PI2) * 2, wave: t, glow: g }));
    const walk = (g) => n => cycle(n, t => ({ lean: 6, y: -Math.abs(S(t * PI2)) * 4, hipF: 20 * S(t * PI2), kneeF: 10 + 16 * Math.max(0, -S(t * PI2)), hipB: -20 * S(t * PI2), kneeB: 10 + 16 * Math.max(0, S(t * PI2)), armB: -6, weapon: 110, wave: t, flare: 0.4, glow: g }));
    Object.assign(GUARDIAN.states, {
      Dormant: n => cycle(n, t => ({ lean: 14, head: 22, eyes: 0.05, y: 6, hipF: 30, kneeF: 60, hipB: -10, kneeB: 70, armB: 40, weapon: 95, shield: -0.2 })),
      Intro: n => track(n, [[0, { lean: 14, head: 22, eyes: 0, y: 6, hipF: 30, kneeF: 60, hipB: -10, kneeB: 70, armB: 40, weapon: 95 }], [0.4, { lean: 6, head: 6, eyes: 0.6, y: 2 }], [0.7, { lean: -6, head: -10, eyes: 1.6, armB: -120, weapon: -70, y: -6 }], [1, {}]]),
      IdleP1: idle(0), WalkP1: walk(0),
      SlashP1: n => track(n, swing(0.45, -120, 60, false)),
      ShieldBash: n => track(n, [[0, {}], [0.3, { shield: 0.7, lean: -10, x: -6 }], [0.4, { shield: 1, lean: 22, x: 20, hipF: 34, hipB: -30 }], [0.55, { shield: 0.8, lean: 16, x: 18 }], [1, {}]]),
      ChargeStart: n => track(n, [[0, {}], [0.7, { lean: -12, y: 6, hipF: 30, kneeF: 50, hipB: -30, kneeB: 40, shield: 1, eyes: 1.6 }], [1, { lean: 20, shield: 1 }]]),
      Charge: n => cycle(n, t => ({ lean: 28, shield: 1, y: -Math.abs(S(t * PI2)) * 6, hipF: 40 * S(t * PI2), kneeF: 20 + 30 * Math.max(0, -S(t * PI2)), hipB: -40 * S(t * PI2), kneeB: 20 + 30 * Math.max(0, S(t * PI2)), flare: 1, wave: t })),
      ChargeRecover: n => track(n, [[0, { lean: 30, shield: 1, x: 8 }], [0.4, { lean: -8, y: 4, hipF: 26, kneeF: 40 }], [1, {}]]),
      Combo1: n => track(n, swing(0.45, -110, 50, false)),
      Combo2: n => track(n, swing(0.45, 80, -60, false)),
      Combo3: n => track(n, swing(0.5, -150, 100, true)),
      Stunned: n => cycle(n, t => ({ lean: -6 + S(t * PI2) * 8, head: S(t * PI2) * 12, eyes: 0.4, armB: 30, weapon: 120, shield: -0.3 })),
      Recoil: n => track(n, [[0, { lean: -18, x: -10, head: -14, eyes: 0.6 }], [1, {}]]),
      PhaseTransition: n => track(n, [[0, {}], [0.3, { lean: 20, y: 10, hipF: 40, kneeF: 70, hipB: -10, kneeB: 80, head: 20, eyes: 0.3, glow: 0.4 }], [0.7, { lean: -10, y: -4, head: -16, eyes: 2, glow: 1.4, armB: -140, weapon: -60 }], [1, { glow: 1 }]]),
      IdleP2: idle(1), MoveP2: walk(1),
      SlashP2: n => track(n, swing(0.42, -130, 70, true).map(([u, k]) => [u, Object.assign({ glow: 1 }, k)])),
      ComboP2: n => track(n, swing(0.5, -150, 100, true).map(([u, k]) => [u, Object.assign({ glow: 1.2 }, k)])),
      JumpStart: n => track(n, [[0, { glow: 1 }], [0.7, { y: 12, sy: 0.86, hipF: 40, kneeF: 70, hipB: -30, kneeB: 60, lean: 10, armB: -150, weapon: -80, glow: 1 }], [1, { y: -20, sy: 1.08, glow: 1 }]]),
      Jump: n => cycle(n, t => ({ y: -30, hipF: 30, kneeF: 50, hipB: -10, kneeB: 60, armB: -160, weapon: -90, flare: 0.9, glow: 1, wave: t })),
      Slam: n => track(n, [[0, { y: -10, armB: -160, weapon: -90, glow: 1 }], [1, { y: 14, sy: 0.84, lean: 26, hipF: 50, kneeF: 80, hipB: -30, kneeB: 70, armB: 60, weapon: 95, slash: 1, slashFrom: -100, slashTo: 95, glow: 1.4 }]]),
      SlamRecover: n => track(n, [[0, { y: 14, sy: 0.84, lean: 26, hipF: 50, kneeF: 80, hipB: -30, kneeB: 70, armB: 60, weapon: 95, glow: 1 }], [1, { glow: 1 }]]),
      Death: n => track(n, [[0, { glow: 1, head: -10 }], [0.35, { lean: 20, y: 14, hipF: 60, kneeF: 100, hipB: 10, kneeB: 100, head: 24, eyes: 0.4, glow: 1.6, weapon: 140, shield: -0.6 }], [1, { lean: 70, y: 40, sy: 0.8, hipF: 80, kneeF: 110, hipB: 40, kneeB: 110, head: 40, eyes: 0, glow: 0, weapon: 175, shield: -1, alpha: 0.9 }]])
    });
  })();

  const ACTORS = { ZombieSoldier: ZOMBIE, BoneArcher: ARCHER, ShieldGuard: GUARD, GuardianOfTheTomb: GUARDIAN };

  function renderVector(g, actor, p, frame) {
    g.clearRect(0, 0, actor.w, actor.h);
    g.save();
    g.translate(actor.w / 2 - 6 + p.x, actor.h - 2 + p.y);
    g.globalAlpha = p.alpha;
    g.scale(p.sx, p.sy);
    g.rotate(R(p.lean * 0.15));
    actor.draw(g, p, frame);
    g.restore();
  }

  // ---------- лялька з мальованого Midjourney (2026-11-05, користувач: «усі моделі на рівні героя з Midjourney,
  // як у Hollow Knight: згладжені, темні, у плащах»; решено Claude за постійною згодою) ----------
  // tools/enemies/puppet.py ріже parts/<Actor>/source.png на тіло й зброю; пози ті самі, що вище: тіло стоїть на
  // стопах і хилиться (lean), присідає (sy, y), поділ гойдається з кроком (hipF/hipB), зброя — у руці (arm) під кутом
  // weapon, очі спалахують (eyes) перед ударом. Удари падають на ті самі кадри: таблиці станів не змінюються.
  const PUPPETS = {};
  function loadPuppet(name, base) {
    return fetch(base + "parts.json").then(r => r.ok ? r.json() : null).then(meta => {
      if (!meta) return;
      const pz = { meta, img: {} };
      return Promise.all(["body", "weapon", "glow"].filter(k => k === "glow" || meta[k]).map(k => new Promise(res => {
        const im = new Image(); im.onload = () => { pz.img[k] = im; res(); }; im.onerror = res; im.src = base + k + ".png";
      }))).then(() => { if (pz.img.body) PUPPETS[name] = pz; });
    }).catch(() => {});
  }
  // Звірі на чотирьох лапах: їхній малюнок гнеться інакше, ніж фігура в плащі.
  const QUADS = new Set(["CorruptedHound", "Watchdog", "MarshCorpseEater"]);
  // Летючі (ворон, п'явка): малюнок висить у повітрі, махає тим, що над тулубом, і хвилюється вздовж.
  const FLYERS = new Set(["AshRaven", "MarshLeech"]);
  const smooth01 = (a, b, x) => { const t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

  // Де стоїть вершина малюнка (u, v; v = 0 — верх) у позі p: стопи на місці, усе інше — за позою.
  function deformRobed(p, bw, bh, u, v) {
    const t = 1 - v, HEM = 0.32, HEAD = 0.8;
    let q = [(u - 0.5) * bw, -t * bh];
    const stepSwing = ((p.hipF || 0) - (p.hipB || 0)) / 90;
    if (t < HEM) {
      // поділ: гойдається з кроком, хвилюється, відлітає назад у русі (flare) і відстає від нахилу (інерція тканини)
      const w = Math.pow((HEM - t) / HEM, 1.5);
      const step = stepSwing * 0.07 * bw;
      const lag = -(p.lean || 0) * 0.0035 * bw;
      q[0] += (step + lag + Math.sin(PI2 * (p.wave || 0) + u * 4) * 0.022 * bw - (p.flare || 0) * 0.05 * bw) * w;
    }
    // дихання: на вдиху (присід пози спокою) груди трохи ширшають
    const breath = Math.max(0, p.y || 0) / 1.5;
    if (breath > 0) q[0] *= 1 + 0.025 * breath * smooth01(0.4, 0.6, t) * smooth01(0.9, 0.7, t);
    if (t > HEAD) {
      // голова кивне окремо від корпуса
      q = Mesh.rot(q, [0, -bh * HEAD], R(p.head || 0) * 0.8 * smooth01(HEAD, 1, t));
    }
    // корпус гнеться від стоп: верх нахиляється повністю, низ — ледь; з кожним кроком ледь хитається
    return Mesh.rot(q, [0, 0], (R(p.lean || 0) * 0.55 + R(stepSwing * 3)) * Math.pow(t, 1.2));
  }

  function deformQuad(p, bw, bh, u, v) {
    const t = 1 - v, LEG = 0.45;
    let q = [(u - 0.5) * bw, -t * bh];
    if (t < LEG) {
      // лапи ходять навколо плечей і стегон: передні — hipF, задні — hipB, між ними плавно
      const f = smooth01(0.4, 0.6, u);
      const pivot = [(-0.3 + 0.58 * f) * bw, -bh * LEG];
      const hip = R((p.hipB || 0) * (1 - f) + (p.hipF || 0) * f) * 0.6;
      q = Mesh.rot(q, pivot, -hip * Math.min(1, (LEG - t) / LEG * 1.3));
    }
    // хребет вигинається
    q[1] -= (p.spine || 0) * 0.06 * bh * Math.sin(Math.PI * u) * smooth01(0.2, LEG, t);
    if (u > 0.7) q = Mesh.rot(q, [bw * 0.2, -bh * 0.62], R(p.head || 0) * 0.7 * smooth01(0.7, 0.95, u) * smooth01(0.25, 0.5, t));
    if (u < 0.18) q = Mesh.rot(q, [-bw * 0.33, -bh * 0.62], -(p.tail || 0) * 0.45 * smooth01(0.18, 0.02, u) * smooth01(0.3, 0.5, t));
    return Mesh.rot(q, [0, 0], R(p.lean || 0) * 0.45);
  }

  function deformFlyer(p, bw, bh, u, v) {
    let q = [(u - 0.5) * bw, (v - 0.5) * bh];
    // помах: усе, що вище середини тулуба (крила), стискається й розтягується по вертикалі, край — найбільше
    const beat = ((p.wingUp || 0) * 2 - 1) * 0.3 + (p.flap || 0) * 0.3;
    const above = smooth01(0.52, 0.12, v);
    q[1] += -(v - 0.5) * bh * beat * above * -1 - beat * bh * 0.08 * above;
    // тіло хвилюється від хвоста до голови; голова кивне
    q[1] += Math.sin(PI2 * (p.wave || 0) + u * 5) * bh * 0.025 * smooth01(0.3, 0.6, v);
    if (u > 0.72) q = Mesh.rot(q, [bw * 0.22, 0], R(p.head || 0) * 0.6 * smooth01(0.72, 0.95, u));
    return Mesh.rot(q, [0, 0], R(p.lean || 0) * 0.7);
  }

  function drawPuppet(g, actor, pz, p, frame) {
    const m = pz.meta;
    // масштаб: висота старої фігури (і не ширша за кадр)
    const k = Math.min(m.height / m.body.h, (actor.w * 0.98) / m.body.w);
    const bw = m.body.w * k, bh = m.body.h * k;
    if (!pz.cache) {
      // чисто зменшений малюнок, у півтора раза більший за кадр: згин і поворот беруть пікселі з запасом
      pz.cache = { body: Mesh.prescale(pz.img.body, bw * 1.5, bh * 1.5) };
      if (pz.img.glow) pz.cache.glow = Mesh.prescale(pz.img.glow, bw * 1.5, bh * 1.5);
      if (pz.img.weapon) {
        const wk = Math.min((bh * (actor.weaponLen || 0.75)) / m.weapon.w, 1);
        pz.cache.wk = wk;
        pz.cache.weapon = Mesh.prescale(pz.img.weapon, m.weapon.w * wk * 1.5, m.weapon.h * wk * 1.5);
      }
    }
    const quad = QUADS.has(actor.__name), flyer = FLYERS.has(actor.__name);
    const map = (u, v) => (flyer ? deformFlyer : quad ? deformQuad : deformRobed)(p, bw, bh, u, v);
    const COLS = 8, ROWS = 12;
    g.clearRect(0, 0, actor.w, actor.h);
    g.save();
    // стопи на землі: присід векторної пози (y донизу — коліна гнуться) для мальованої фігури — стиск тіла донизу,
    // а не з'їзд за край кадру; підскок угору (y < 0) лишається підскоком
    const crouch = Math.max(0, p.y || 0);
    if (flyer) {
      // у повітрі: середина малюнка там, де була середина старої фігури
      // …але не нижче землі: малюнок із розкритими крилами вищий за стару фігуру, що сиділа
      g.translate(m.centre + p.x, Math.min((m.middle || actor.h * 0.55) + (p.y || 0), actor.h - 2 - bh * 0.5));
      g.globalAlpha = p.alpha;
      g.scale(p.sx, p.sy);
    } else {
      g.translate(m.centre + p.x, actor.h - 2 + Math.min(0, p.y || 0));
      g.globalAlpha = p.alpha;
      g.scale(p.sx * (1 + crouch / bh * 0.35), p.sy * (1 - crouch / bh * 0.9));
    }
    // фаза-2 і магія: світло з-за спини
    if (p.glow > 0.02) {
      // the whole circle of the gradient, ending at the feet: a narrower fill showed the light as a box with straight
      // sides once the frame had room round it (2026-11-06)
      const c = map(0.5, 0.45), r = bh * 0.6;
      const gl = g.createRadialGradient(c[0], c[1], 4, c[0], c[1], r);
      gl.addColorStop(0, `rgba(${actor.glowRgb || "170,110,255"},${0.4 * Math.min(1, p.glow)})`); gl.addColorStop(1, "rgba(0,0,0,0)");
      g.fillStyle = gl; g.fillRect(c[0] - r, c[1] - r, r * 2, r * 2);
    }
    // рука під плащем: плече біля переднього краю грудей іде за згином тіла, кулак ходить невеликим колом
    const sh = map(0.66, 0.42), armLen = bh * 0.16, ra = R(p.arm || 0);
    const hand = [sh[0] + Math.sin(ra) * armLen, sh[1] + Math.cos(ra) * armLen];
    const weapon = pz.cache.weapon;
    const drawWeapon = () => {
      const ww = weapon.width / 1.5, wh = weapon.height / 1.5;
      g.save(); g.translate(hand[0], hand[1]); g.rotate(R(p.weapon));
      g.drawImage(weapon, -ww * 0.2, -wh / 2, ww, wh);
      g.restore();
    };
    // у спокої зброя тримається збоку, за тілом; у замаху — попереду
    const front = (p.arm || 0) > 20 || p.slash > 0.05;
    if (weapon && !front) drawWeapon();
    Mesh.draw(g, pz.cache.body, COLS, ROWS, map);
    // світло фігури (очі, маска, вогник): завжди тихо світить, перед ударом спалахує, у смерті гасне
    if (pz.cache.glow) {
      const lit = Math.max(0, Math.min(1, p.eyes)) * 0.6 + Math.max(0, p.eyes - 1) * 1.8;
      g.save(); g.globalCompositeOperation = "lighter";
      for (let a = lit; a > 0.01; a -= 1) { g.globalAlpha = p.alpha * Math.min(1, a); Mesh.draw(g, pz.cache.glow, COLS, ROWS, map, 0.4); }
      g.restore();
    }
    if (weapon && front) drawWeapon();
    // очі — поверх зброї (піднята булава не ховає замах); у розмірі, що читається в кадрі; перед ударом спалахують
    (m.eyes || []).forEach(([ex, ey, er, col]) => {
      const r = Math.max(2.6, er * k * 0.8), at = map(ex / m.body.w, ey / m.body.h);
      if (p.eyes > 0.05) eye(g, at[0], at[1], r, p.eyes, col || actor.eyes || "#ffd27a");
    });
    slashArc(g, sh[0], sh[1] + bh * 0.1, bh * 0.55, p.slashFrom, p.slashTo, p.slash);
    g.restore();
  }

  function render(g, actor, p, frame) {
    const pz = actor.__name && PUPPETS[actor.__name];
    if (pz) { drawPuppet(g, actor, pz, p, frame); return; }
    renderVector(g, actor, p, frame);
  }

  root.Enemies = { ACTORS, render, loadPuppet: (name, base) => { ACTORS[name].__name = name; return loadPuppet(name, base); }, kit: { S, PI2, R, INK, pose, track, cycle, stroke, limb, shape, eye, blade, slashArc, rags, boot } };
})(window);
