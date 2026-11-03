// Вороги й бос Лісу (2026-11-03, просьба користувача: «другий рівень так само — персонажі через Midjourney»;
// образи — концепти design/characters/enemies/concept_forest_*.png і concept_boss_warden.png, обрані Claude за
// постійною згодою). Той самий підхід, що й у Гробниці (enemies.js): кадри малює код за концептом, ключі пози —
// частки анімації. Удари стоять там, де їх ставлять заглушки PlaceholderEnemyArt.Forest (контракт той самий):
// укус гончака 0,34…0,6, стрибок 0,40…0,56; постріл лучника 0,74, штурхан 0,28…0,62; закляття шамана 0,72 / 0,8;
// укол списника 0,42…0,68; змах Вартового 0,32…0,68, підсічка 0,36…0,66, корінь-постріл 0,76, удар у землю 0,64.
(function (root) {
  "use strict";
  const E = root.Enemies, K = E.kit;
  const { S, PI2, R, INK, track, cycle, stroke, limb, shape, eye, slashArc, rags } = K;
  const C = Math.cos;
  const GLOW = "#9be36a", EYE = "#d4ffb8";

  // кошлата смуга моху/шерсті по ламаній: шипи назовні, хвилюються з wave
  function shag(g, pts, len, color, p, n) {
    const out = [];
    for (let i = 0; i < pts.length - 1; i++) {
      const [ax, ay] = pts[i], [bx, by] = pts[i + 1];
      const nx = -(by - ay), ny = bx - ax, l = Math.hypot(nx, ny) || 1;
      for (let k = 0; k < n; k++) {
        const t = k / n, x = ax + (bx - ax) * t, y = ay + (by - ay) * t;
        const d = (k % 2 ? len : len * 0.35) * (0.7 + 0.3 * S(i * 3 + k * 1.7 + p.wave * PI2));
        out.push([x + nx / l * d, y + ny / l * d]);
      }
    }
    out.push(pts[pts.length - 1]);
    for (let i = pts.length - 1; i >= 0; i--) out.push(pts[i]);
    shape(g, color, out, 3);
  }
  function glowBall(g, x, y, r, a) {
    if (a <= 0.02) return;
    const gl = g.createRadialGradient(x, y, 0, x, y, r);
    gl.addColorStop(0, `rgba(190,255,140,${0.9 * Math.min(1, a)})`); gl.addColorStop(0.35, `rgba(120,220,90,${0.5 * Math.min(1, a)})`); gl.addColorStop(1, "rgba(120,220,90,0)");
    g.fillStyle = gl; g.beginPath(); g.arc(x, y, r, 0, PI2); g.fill();
  }

  // ---------- Гончак: вовк, порослий корою й мохом, колючий хребет ----------
  const HOUND = {
    w: 200, h: 120, eyes: EYE,
    draw(g, p) {
      const c = { fur: "#26372b", furDark: "#141f17", moss: "#3e5c3c", thorn: "#8a8f6a", bark: "#2e2a20" };
      const lift = p.spine * 6;
      const sh = [22, -56 + lift * 0.3], hp = [-46, -54 - lift];
      // далекі лапи (темніші)
      limb(g, hp[0] + 4, hp[1] + 4, -p.hipB * 0.8 - 8, 24, -p.kneeB * 0.8 - 20, 26, 7, c.furDark, 5);
      limb(g, sh[0] - 4, sh[1] + 6, -p.hipF * 0.8 + 6, 24, p.kneeF * 0.8 + 10, 26, 7, c.furDark, 5);
      // хвіст
      const ta = R(200 + p.tail * 30 + S(p.wave * PI2) * 8);
      stroke(g, 8, c.fur, [[hp[0] - 4, hp[1] - 4], [hp[0] - 4 + C(ta) * 22, hp[1] - 4 + S(ta) * 22 - 6], [hp[0] - 4 + C(ta) * 40, hp[1] + S(ta) * 34 - 4]]);
      // тулуб: глибока грудь, вигнута спина, підтягнутий живіт — кривими, кошлатий край
      const top = -74 - lift * 0.6;
      g.beginPath();
      g.moveTo(hp[0] - 10, hp[1] + 4);
      g.quadraticCurveTo(hp[0] - 12, hp[1] - 16, hp[0] + 14, top + 4);
      g.quadraticCurveTo(4, top - 6, sh[0] - 4, sh[1] - 18);
      g.quadraticCurveTo(sh[0] + 18, sh[1] - 14, sh[0] + 14, sh[1] + 6);
      g.quadraticCurveTo(sh[0] + 6, sh[1] + 24, sh[0] - 14, sh[1] + 20);
      g.quadraticCurveTo(-6, -42 - lift * 0.3, hp[0] + 10, hp[1] + 14);
      g.quadraticCurveTo(hp[0] - 2, hp[1] + 14, hp[0] - 10, hp[1] + 4);
      g.closePath();
      g.lineJoin = 'round'; g.strokeStyle = INK; g.lineWidth = 4; g.stroke(); g.fillStyle = c.fur; g.fill();
      // мох пасмами з боків і грудей
      g.fillStyle = c.moss;
      for (let i = 0; i < 9; i++) {
        const x = hp[0] + 8 + i * 9, y = -60 - lift * 0.4 + S(i * 1.3) * 4;
        g.beginPath(); g.moveTo(x - 5, y - 8); g.quadraticCurveTo(x + 6, y - 2, x + 2 + S(p.wave * PI2 + i) * 2, y + 8 + (i % 3) * 3); g.quadraticCurveTo(x - 2, y, x - 5, y - 8); g.fill();
      }
      // колючий хребет: шипи-терни назад, від загривка до крижів
      for (let i = 0; i < 7; i++) {
        const t = i / 6, x = hp[0] + 10 + (sh[0] - hp[0] - 10) * t, y = top + 2 + Math.abs(t - 0.45) * 14 - (i % 2) * 2;
        const L = 12 + (1 - Math.abs(t - 0.5) * 2) * 8 + p.spine * 6;
        shape(g, i % 2 ? c.thorn : c.furDark, [[x - 4, y + 3], [x + 4, y + 3], [x - L * 0.55, y - L]], 2.5);
      }
      // ближні лапи
      limb(g, hp[0], hp[1] + 6, -p.hipB, 24, -p.kneeB - 20, 26, 12, c.fur, 7);
      limb(g, sh[0], sh[1] + 8, -p.hipF + 6, 24, p.kneeF + 10, 26, 12, c.fur, 7);
      // голова: клин черепа, довга звужена морда, вуха назад, щелепа
      g.save(); g.translate(sh[0] + 8, sh[1] - 10); g.rotate(R(p.head + 6));
      const jaw = p.jaw * 26;
      g.save(); g.translate(8, 4); g.rotate(R(jaw));
      shape(g, c.furDark, [[-4, 0], [30, 2], [32, 6], [0, 9]], 3);
      g.fillStyle = '#c9c8b0'; for (let i = 0; i < 4; i++) { g.beginPath(); g.moveTo(10 + i * 6, 1); g.lineTo(13 + i * 6, -3); g.lineTo(16 + i * 6, 1); g.fill(); }
      g.restore();
      g.beginPath(); g.moveTo(-8, 8); g.quadraticCurveTo(-10, -12, 8, -14); g.quadraticCurveTo(22, -12, 40, 0); g.quadraticCurveTo(42, 5, 36, 6); g.quadraticCurveTo(14, 8, -8, 8); g.closePath();
      g.strokeStyle = INK; g.lineWidth = 4; g.stroke(); g.fillStyle = c.fur; g.fill();
      shape(g, c.furDark, [[0, -12], [-14, -24], [10, -14]], 3); // вухо
      g.fillStyle = INK; g.beginPath(); g.arc(39, 1, 2.5, 0, PI2); g.fill();
      eye(g, 18, -6, 2.6, p.eyes, this.eyes);
      g.restore();
    },
    states: {}
  };
  (function () {
    const gait = (t, k, run) => {
      const a = S(t * PI2), b = S(t * PI2 + (run ? 0.6 : Math.PI));
      return { hipF: k * a, kneeF: 10 + k * 0.6 * Math.max(0, -a), hipB: k * b, kneeB: 10 + k * 0.6 * Math.max(0, b) };
    };
    Object.assign(HOUND.states, {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.2, head: S(t * PI2) * 3, tail: S(t * PI2) * 0.3, wave: t })),
      Patrol: n => cycle(n, t => Object.assign({ y: -Math.abs(S(t * PI2 * 2)) * 2, head: 4, wave: t }, gait(t, 26, false))),
      Alert: n => track(n, [[0, {}], [0.5, { head: -18, spine: 0.6, tail: 0.8, eyes: 1.5 }], [1, { head: 8, spine: 1, y: 4, jaw: 0.3 }]]),
      Chase: n => cycle(n, t => Object.assign({ y: -Math.abs(S(t * PI2)) * 6, lean: S(t * PI2) * 6, head: 8, spine: 0.6, wave: t, tail: -0.4 }, gait(t, 40, true))),
      // укус: присід, ривок уперед 0,34…0,6, щелепа клацає на 0,5
      AttackLight: n => track(n, [[0, {}], [0.3, { x: -8, y: 4, head: -10, jaw: 0.8, spine: 0.8, hipF: -20, hipB: 20, eyes: 1.5 }], [0.48, { x: 18, head: 10, jaw: 1, hipF: 40, kneeF: 10, hipB: -30 }], [0.56, { x: 20, head: 14, jaw: 0, hipF: 34, hipB: -24 }], [1, {}]]),
      // стрибок: присід до 0,38, злет 0,40…0,56, приземлення 0,74
      AttackHeavy: n => track(n, [[0, {}], [0.38, { x: -10, y: 10, sy: 0.88, head: -14, spine: 1, jaw: 0.5, hipF: -30, kneeF: 50, hipB: 30, kneeB: 50, eyes: 1.7 }], [0.56, { x: 26, y: -26, lean: -18, head: 6, jaw: 1, hipF: 70, kneeF: 0, hipB: -70, kneeB: 10, spine: 0.4 }], [0.74, { x: 30, y: 2, lean: 10, jaw: 0, hipF: 20, kneeF: 30, hipB: -10, kneeB: 30 }], [1, { x: 0 }]]),
      Hurt: n => track(n, [[0, { x: -8, lean: 10, head: -20, eyes: 0.4, jaw: 0.5 }], [1, {}]]),
      Stunned: n => cycle(n, t => ({ y: 4, head: 14 + S(t * PI2) * 10, lean: S(t * PI2) * 5, eyes: 0.4, tail: -0.6, wave: t })),
      Death: n => track(n, [[0, { head: -20, jaw: 0.6, eyes: 0.6 }], [0.5, { y: 10, sy: 0.8, head: 20, hipF: 50, hipB: -50, eyes: 0.2 }], [1, { y: 24, sy: 0.55, sx: 1.05, head: 30, hipF: 80, hipB: -80, kneeF: 40, kneeB: 40, eyes: 0, alpha: 0.9 }]])
    });
  })();

  // ---------- Лісовий лучник: моховий плащ-копиця, два ока з каптура, лук із гілки ----------
  const ARCHER = {
    w: 150, h: 176, hip: 52, thigh: 26, shin: 26, eyes: EYE,
    draw(g, p) {
      const c = { moss: "#3d5a35", mossDark: "#243620", leaf: "#58763f", leg: "#1b2016", bow: "#4a3d2a", string: "#cfd8c0" };
      const hip = -this.hip;
      const back = limb(g, -4, hip, p.hipB, this.thigh, p.kneeB, this.shin, 7, c.leg);
      K.boot(g, back.end, c.leg);
      const fr = limb(g, 4, hip, p.hipF, this.thigh, p.kneeF, this.shin, 7, c.leg);
      K.boot(g, fr.end, c.leg);
      // колчан зі стрілами з пір'ям
      g.save(); g.translate(-16, hip - 52); g.rotate(R(-24));
      shape(g, "#2f2a1e", [[-5, 0], [5, 0], [6, 30], [-6, 30]], 3);
      for (let i = -1; i <= 1; i++) { stroke(g, 1.5, "#2a2318", [[i * 3, 0], [i * 4, -14]]); shape(g, "#9aa58a", [[i * 4 - 2, -14], [i * 4 + 2, -14], [i * 4, -20]], 1.5); }
      g.restore();
      // плащ: копиця моху до колін, кошлатий низ
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      const pts = [[-22, 20], [-26, -20], [-18, -52], [-4, -66], [14, -60], [24, -30], [24, 18]];
      shape(g, c.moss, pts, 4);
      shag(g, [[24, 18], [0, 24], [-22, 20]], 10, c.mossDark, p, 5);
      g.fillStyle = c.leaf;
      for (let i = 0; i < 9; i++) { const x = -18 + (i * 37) % 40, y = -46 + (i * 23) % 60; g.beginPath(); g.ellipse(x, y, 5, 2.5, R(i * 40), 0, PI2); g.fill(); }
      g.restore();
      // рука з луком
      const shoulder = [6 + p.lean * 0.4, hip - 44];
      const bowArm = limb(g, shoulder[0], shoulder[1], p.arm, 20, p.elbow, 18, 6, c.mossDark);
      const pull = 6 + p.draw * 24, hand = [bowArm.end[0] - pull, bowArm.end[1] + 2];
      stroke(g, 6, c.mossDark, [[shoulder[0] - 6, shoulder[1] + 4], [hand[0] + 4, hand[1] + 8], hand]);
      const bx = bowArm.end[0], by = bowArm.end[1], ba = R(p.weapon);
      const rot = (x, y) => [bx + x * C(ba) - y * S(ba), by + x * S(ba) + y * C(ba)];
      g.save(); g.translate(bx, by); g.rotate(ba);
      // лук-гілка: крива, з сучками
      for (const [w, col] of [[8, INK], [4.5, c.bow]]) { g.strokeStyle = col; g.lineWidth = w; g.beginPath(); g.moveTo(-50, -2); g.bezierCurveTo(-24, 20, 18, 22, 50, -6); g.stroke(); }
      stroke(g, 2, c.bow, [[-30, 9], [-36, 18]]); stroke(g, 2, c.bow, [[34, 6], [42, 14]]);
      g.restore();
      const tips = [rot(-50, -2), rot(50, -6)];
      g.strokeStyle = c.string; g.lineWidth = 1.2; g.beginPath(); g.moveTo(...tips[0]); g.lineTo(...hand); g.lineTo(...tips[1]); g.stroke();
      if (p.arrow > 0.5) stroke(g, 1.8, "#1c1712", [hand, [bx + 22, by]]);
      // каптур: тьма і два ока
      g.save(); g.translate(6 + p.lean * 0.5, hip - 62); g.rotate(R(p.head));
      shape(g, c.moss, [[-18, 14], [-20, -4], [-6, -22], [-10, -32], [8, -22], [18, -6], [16, 12]], 4);
      g.fillStyle = "#050705"; g.beginPath(); g.ellipse(4, -2, 10, 11, 0, 0, PI2); g.fill();
      eye(g, 1, -3, 2.4, p.eyes, this.eyes); eye(g, 9, -3.5, 2.2, p.eyes, this.eyes);
      g.restore();
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.2, arm: 40, elbow: -10, weapon: 90, head: S(t * PI2) * 4, wave: t })),
      Patrol: n => cycle(n, t => ({ lean: 4, y: -Math.abs(S(t * PI2)) * 3, hipF: 22 * S(t * PI2), kneeF: 10 + 18 * Math.max(0, -S(t * PI2)), hipB: -22 * S(t * PI2), kneeB: 10 + 18 * Math.max(0, S(t * PI2)), arm: 30, weapon: 100, wave: t })),
      Chase: n => cycle(n, t => ({ lean: 14, y: -Math.abs(S(t * PI2)) * 5, hipF: 32 * S(t * PI2), kneeF: 16 + 26 * Math.max(0, -S(t * PI2)), hipB: -32 * S(t * PI2), kneeB: 16 + 26 * Math.max(0, S(t * PI2)), arm: 50, weapon: 110, wave: t })),
      Alert: n => track(n, [[0, {}], [0.5, { head: -14, y: -3, eyes: 1.5 }], [1, { arm: 80, weapon: 90 }]]),
      // довгий натяг до 0,72 — і постріл на 0,74
      AttackLight: n => track(n, [[0, { arm: 40, weapon: 90 }], [0.22, { arm: 84, elbow: 0, weapon: 90, lean: -4 }], [0.72, { arm: 88, weapon: 90, draw: 1, lean: -8, eyes: 1.6, hipF: 16, hipB: -16 }], [0.76, { arm: 88, weapon: 90, draw: 0, arrow: 0, lean: 4, x: -3 }], [1, { arm: 50, weapon: 95, arrow: 0 }]]),
      // штурхан ногою, щоб відкупити відстань: 0,28…0,62
      ShieldBash: n => track(n, [[0, {}], [0.28, { lean: -10, x: -4, hipF: -10, kneeF: 60, arm: 30 }], [0.5, { lean: 14, x: 10, hipF: 80, kneeF: 0, hipB: -10, arm: 60, weapon: 120 }], [0.62, { lean: 10, x: 10, hipF: 60, kneeF: 10 }], [1, {}]]),
      Hurt: n => track(n, [[0, { lean: -20, x: -6, head: -16, eyes: 0.4, wave: 0.5 }], [1, {}]]),
      Stunned: n => cycle(n, t => ({ lean: S(t * PI2) * 10, head: S(t * PI2) * 14, eyes: 0.4, arm: 20, weapon: 120, wave: t })),
      Death: n => track(n, [[0, { lean: -10, eyes: 0.6 }], [0.5, { y: 14, sy: 0.82, hipF: 60, kneeF: 100, hipB: 20, kneeB: 100, lean: 20, arm: 10, weapon: 160, eyes: 0.2 }], [1, { y: 30, sy: 0.6, sx: 1.1, lean: 60, hipF: 80, kneeF: 120, hipB: 40, kneeB: 120, arm: 0, weapon: 175, eyes: 0, alpha: 0.85 }]])
    }
  };

  // ---------- Шаман коріння: череп оленя, мохова ряса, низ — клубок коріння, посох із зеленим зерном ----------
  const SHAMAN = {
    w: 156, h: 178, eyes: EYE,
    draw(g, p) {
      const c = { robe: "#34492f", robeDark: "#1f2c1c", root: "#3b3224", rootDark: "#211b13", bone: "#d9d2bc", boneDark: "#8f8770", staff: "#4a3c28" };
      // коріння замість ніг: розповзається по землі, ворушиться
      for (let i = 0; i < 7; i++) {
        const sx = -16 + i * 5, a = (i - 3) * 0.5 + S(p.wave * PI2 + i) * 0.15 + p.roots * (i - 3) * 0.12;
        const ex = sx + S(a) * 34 + (i - 3) * 6, ey = -2 + Math.abs(i - 3) * 0.6;
        stroke(g, 6 - Math.abs(i - 3) * 0.6, i % 2 ? c.root : c.rootDark, [[sx, -40], [sx + S(a) * 14, -18], [ex, ey]]);
      }
      g.save(); g.translate(0, -40); g.rotate(R(p.lean * 0.4));
      // ряса: від коренів до плечей, згорблена
      shape(g, c.robe, [[-20, 6], [-24, -36], [-12, -70], [10, -74], [22, -54], [22, -20], [18, 8]], 4);
      shag(g, [[18, 8], [0, 12], [-20, 6]], 9, c.robeDark, p, 5);
      // кістяні оберіжки
      for (let i = 0; i < 4; i++) { const x = -12 + i * 8; stroke(g, 1.2, c.boneDark, [[x, -54], [x + 1, -40 - (i % 2) * 5]]); g.fillStyle = c.bone; g.fillRect(x - 2, -40 - (i % 2) * 5, 4, 6); }
      // задня рука
      limb(g, -8, -64, p.armB, 20, p.elbowB, 18, 6, c.robeDark);
      g.restore();
      // голова: череп оленя з рогами
      const hx = 10 + p.lean * 0.5, hy = -118;
      g.save(); g.translate(hx, hy); g.rotate(R(p.head));
      for (const s of [-1, 1]) {
        const col = s < 0 ? c.boneDark : c.bone;
        stroke(g, 3, col, [[s * 4, -10], [s * 14, -26], [s * 18, -44]]);
        stroke(g, 2.4, col, [[s * 14, -26], [s * 26, -32]]); stroke(g, 2.4, col, [[s * 16, -36], [s * 8, -46]]); stroke(g, 2, col, [[s * 18, -44], [s * 28, -50]]);
      }
      shape(g, c.bone, [[-10, -12], [12, -12], [14, 0], [8, 14], [2, 22], [-4, 14], [-10, 0]], 3);
      g.fillStyle = "#070806"; g.beginPath(); g.ellipse(-3, -2, 3.5, 4.5, 0, 0, PI2); g.ellipse(7, -2, 3.5, 4.5, 0, 0, PI2); g.fill();
      eye(g, -3, -2, 1.8, p.eyes, this.eyes); eye(g, 7, -2, 1.8, p.eyes, this.eyes);
      g.restore();
      // передня рука з посохом; зерно на верхівці світиться (glow)
      const arm = limb(g, 12 + p.lean * 0.4, -100, p.arm, 22, p.elbow, 20, 6, c.robe);
      g.save(); g.translate(arm.end[0], arm.end[1]); g.rotate(R(p.weapon - 90));
      stroke(g, 4, c.staff, [[0, 40], [2, 0], [-2, -50], [4, -66]]);
      stroke(g, 2, c.staff, [[4, -66], [-6, -74], [0, -82]]); stroke(g, 2, c.staff, [[4, -66], [12, -74], [8, -82]]);
      glowBall(g, 2, -76, 16 + p.glow * 22, 0.5 + p.glow);
      g.fillStyle = "#c8ff9a"; g.beginPath(); g.arc(2, -76, 4 + p.glow * 1.5, 0, PI2); g.fill();
      g.restore();
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.5, lean: 6, arm: 30, elbow: -30, weapon: 96, head: S(t * PI2) * 4, wave: t, glow: 0.2 + S(t * PI2) * 0.1 })),
      Patrol: n => cycle(n, t => ({ lean: 8, y: -Math.abs(S(t * PI2)) * 2, x: S(t * PI2) * 2, arm: 26, elbow: -30, weapon: 100, wave: t * 2, roots: S(t * PI2), glow: 0.2 })),
      Chase: n => cycle(n, t => ({ lean: 14, y: -Math.abs(S(t * PI2)) * 3, arm: 40, elbow: -20, weapon: 110, wave: t * 3, roots: S(t * PI2) * 1.4, glow: 0.3 })),
      Alert: n => track(n, [[0, { glow: 0.2 }], [0.5, { head: -14, eyes: 1.6, glow: 0.6 }], [1, { arm: 50, weapon: 80, glow: 0.4 }]]),
      // збирає до 0,7 (посох угору, зерно розгоряється), поштовх 0,72…0,92
      AttackLight: n => track(n, [[0, { arm: 30, weapon: 96 }], [0.7, { arm: 130, elbow: 20, weapon: 70, lean: -10, head: -10, glow: 1.4, eyes: 1.6 }], [0.82, { arm: 70, elbow: 0, weapon: 30, lean: 16, x: 6, glow: 0.8 }], [1, { arm: 40, weapon: 90, glow: 0.3 }]]),
      // на землю: довше, нижче, посох б'є в корені на 0,8…1
      AttackHeavy: n => track(n, [[0, { arm: 30, weapon: 96 }], [0.78, { arm: 160, elbow: 30, weapon: 100, lean: -14, y: -4, glow: 1.6, eyes: 1.8, roots: -1 }], [0.88, { arm: 40, elbow: -10, weapon: 170, lean: 22, y: 4, glow: 1, roots: 1.5 }], [1, { arm: 30, weapon: 120, glow: 0.3, roots: 0.5 }]]),
      Hurt: n => track(n, [[0, { lean: -22, x: -6, head: -18, eyes: 0.4, glow: 0 }], [1, { glow: 0.2 }]]),
      Stunned: n => cycle(n, t => ({ lean: S(t * PI2) * 10, head: S(t * PI2) * 16, eyes: 0.4, arm: 10, weapon: 130, wave: t, glow: 0.05 })),
      Death: n => track(n, [[0, { lean: -12, eyes: 0.6, glow: 0.6 }], [0.5, { y: 16, sy: 0.8, lean: 20, arm: 0, weapon: 150, eyes: 0.2, glow: 0.2, roots: 1 }], [1, { y: 40, sy: 0.55, sx: 1.15, lean: 40, arm: -20, weapon: 175, eyes: 0, glow: 0, roots: 2, alpha: 0.85 }]])
    }
  };

  // ---------- Королівський списник: лицар у мохнатій табарді, вузька щілина шолома, довгий спис ----------
  const SPEAR = {
    w: 214, h: 178, hip: 66, thigh: 32, shin: 32, eyes: EYE,
    draw(g, p) {
      const c = { armor: "#3c4040", armorLight: "#5c6260", armorDark: "#202423", tabard: "#2f4a30", tabardDark: "#1d2e1e", hood: "#263826", shaft: "#4a3b2a", tip: "#a9aca2" };
      const hip = -this.hip;
      // плащ-каптур позаду
      g.save(); g.translate(-8, hip - 54); rags(g, -12, 0, 10, 20, 70, p, c.hood, 7); g.restore();
      const back = limb(g, -5, hip, p.hipB, this.thigh, p.kneeB, this.shin, 12, c.armorDark);
      K.boot(g, back.end, c.armorDark);
      // задня рука тримає спис позаду
      const ba = limb(g, -8, hip - 48, p.armB, 22, p.elbowB, 20, 9, c.armorDark);
      // тулуб: латні груди, табарда з мохом
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      shape(g, c.armor, [[-16, 4], [-20, -40], [-10, -56], [14, -56], [22, -40], [16, 4]], 4);
      g.fillStyle = c.armorLight; g.fillRect(-14, -52, 28, 5);
      rags(g, -12, -46, 12, 24, 56, p, c.tabard, 6);
      g.fillStyle = c.tabardDark; g.fillRect(-2, -44, 4, 34);
      g.restore();
      const fr = limb(g, 5, hip, p.hipF, this.thigh, p.kneeF, this.shin, 12, c.armor);
      K.boot(g, fr.end, c.armorDark);
      // шолом під каптуром
      g.save(); g.translate(8 + p.lean * 0.5, hip - 70); g.rotate(R(p.head));
      shape(g, c.hood, [[-16, 14], [-18, -8], [-6, -24], [10, -22], [18, -8], [16, 12]], 4);
      shape(g, c.armor, [[-8, 10], [-9, -10], [12, -12], [14, 8]], 3);
      g.fillStyle = "#050605"; g.fillRect(-2, -5, 16, 4);
      eye(g, 6, -3, 1.8, p.eyes, this.eyes); eye(g, 11, -3, 1.6, p.eyes, this.eyes);
      g.restore();
      // передня рука і спис уздовж кута weapon через обидві руки
      const arm = limb(g, 10, hip - 50, p.arm, 22, p.elbow, 20, 9, c.armor);
      const a = R(p.weapon), hx = arm.end[0], hy = arm.end[1];
      const back0 = [hx - C(a) * 70, hy - S(a) * 70], tip = [hx + C(a) * 78, hy + S(a) * 78];
      stroke(g, 4, c.shaft, [back0, tip]);
      g.save(); g.translate(...tip); g.rotate(a);
      shape(g, c.tip, [[0, -5], [22, 0], [0, 5]], 3); g.fillStyle = c.tabard; g.fillRect(-6, -4, 6, 8);
      g.restore();
      stroke(g, 6, c.armorDark, [ba.end, [hx - C(a) * 30, hy - S(a) * 30]]);
      slashArc(g, 10, hip - 46, 80, p.slashFrom, p.slashTo, p.slash);
    },
    states: {
      Idle: n => cycle(n, t => ({ y: S(t * PI2) * 1.2, arm: 30, elbow: -20, weapon: -54, armB: 10, head: S(t * PI2) * 3, wave: t })),
      Patrol: n => cycle(n, t => ({ lean: 4, y: -Math.abs(S(t * PI2)) * 3, hipF: 22 * S(t * PI2), kneeF: 10 + 18 * Math.max(0, -S(t * PI2)), hipB: -22 * S(t * PI2), kneeB: 10 + 18 * Math.max(0, S(t * PI2)), arm: 30, elbow: -20, weapon: -52, wave: t, flare: 0.3 })),
      Chase: n => cycle(n, t => ({ lean: 14, y: -Math.abs(S(t * PI2)) * 5, hipF: 32 * S(t * PI2), kneeF: 16 + 26 * Math.max(0, -S(t * PI2)), hipB: -32 * S(t * PI2), kneeB: 16 + 26 * Math.max(0, S(t * PI2)), arm: 60, elbow: -10, weapon: -10, armB: 30, wave: t, flare: 0.6 })),
      Alert: n => track(n, [[0, {}], [0.5, { head: -12, eyes: 1.5, y: -2 }], [1, { arm: 60, weapon: -5, armB: 30 }]]),
      // укол: спис відтягнуто й вирівняно, вихід уздовж лінії 0,42…0,68
      AttackLight: n => track(n, [[0, { arm: 30, weapon: -70 }], [0.38, { arm: 50, elbow: -50, weapon: -4, armB: -20, lean: -10, x: -8, hipF: 10, kneeF: 20, eyes: 1.5 }], [0.55, { arm: 88, elbow: 0, weapon: 0, armB: 60, lean: 18, x: 14, hipF: 34, kneeF: 16, hipB: -28 }], [0.68, { arm: 86, weapon: 2, armB: 56, lean: 16, x: 14 }], [1, { arm: 30, weapon: -70 }]]),
      // широкий мах древком згори: 0,4…0,62
      AttackHeavy: n => track(n, [[0, {}], [0.4, { arm: -120, elbow: -20, weapon: -110, armB: -100, lean: -12, y: -2, eyes: 1.6 }], [0.55, { arm: 70, elbow: 0, weapon: 30, armB: 40, lean: 22, x: 10, slash: 1, slashFrom: -110, slashTo: 40, hipF: 30, hipB: -26 }], [0.66, { arm: 80, weapon: 40, lean: 18, x: 10, slash: 0.3, slashFrom: -110, slashTo: 40 }], [1, {}]]),
      // укол із випадом: тіло йде за вістрям
      ShieldBash: n => track(n, [[0, { arm: 30, weapon: -70 }], [0.38, { arm: 50, elbow: -50, weapon: -4, armB: -20, lean: -14, x: -10, y: 4, hipF: 20, kneeF: 40, eyes: 1.6 }], [0.56, { arm: 90, elbow: 0, weapon: 4, armB: 64, lean: 28, x: 30, hipF: 50, kneeF: 10, hipB: -40, kneeB: 20 }], [0.7, { arm: 88, weapon: 6, armB: 60, lean: 22, x: 30 }], [1, { arm: 30, weapon: -70 }]]),
      Hurt: n => track(n, [[0, { lean: -20, x: -6, head: -16, eyes: 0.4, flare: 0.8, weapon: -90 }], [1, { weapon: -70 }]]),
      Stunned: n => cycle(n, t => ({ lean: S(t * PI2) * 8, head: S(t * PI2) * 14, eyes: 0.4, arm: 10, weapon: 80, wave: t })),
      Death: n => track(n, [[0, { lean: -12, eyes: 0.6, weapon: -70 }], [0.45, { y: 14, sy: 0.84, hipF: 60, kneeF: 100, hipB: 20, kneeB: 100, lean: 24, arm: 60, weapon: 60, eyes: 0.2 }], [1, { y: 34, sy: 0.66, lean: 70, hipF: 80, kneeF: 120, hipB: 40, kneeB: 120, arm: 90, weapon: 175, eyes: 0, alpha: 0.85 }]])
    }
  };

  // ---------- Порожній Вартовий: дерево-лицар, порожні груди сяють, корона з гілок, глефа з кореня ----------
  const WARDEN = {
    w: 268, h: 260, hip: 86, thigh: 42, shin: 44, eyes: EYE,
    draw(g, p) {
      const c = { bark: "#3a3326", barkLight: "#5a5038", barkDark: "#1c1811", moss: "#3c5a36", plate: "#454b46", plateDark: "#262a27", haft: "#4b3d2a", blade: "#b9b49a" };
      const hip = -this.hip;
      // корені, що волочаться від ніг
      const roots = (at, s) => { for (let i = 0; i < 4; i++) { const a = (i - 1.5) * 0.4 + S(p.wave * PI2 + i) * 0.12; stroke(g, 4 - i * 0.5, c.barkDark, [[at[0], at[1] - 4], [at[0] + S(a) * 20 * s - 8, at[1] - 1], [at[0] + S(a) * 34 * s - 18, at[1] + 1]]); } };
      const back = limb(g, -12, hip, p.hipB, this.thigh, p.kneeB, this.shin, 28, c.barkDark, 22);
      roots(back.end, 1);
      // задня рука — тримає глефу (замахи нею)
      const sa = limb(g, -26, hip - 84, p.armB, 38, p.elbowB, 34, 20, c.barkDark, 16);
      g.save(); g.translate(sa.end[0], sa.end[1]); g.rotate(R(p.weapon));
      stroke(g, 7, c.haft, [[-40, 0], [118, 0]]);
      for (let i = 0; i < 5; i++) stroke(g, 2, c.barkDark, [[-30 + i * 28, -4], [-20 + i * 28, 4]]); // корінь обвиває древко
      shape(g, c.blade, [[112, -6], [150, -16], [180, -4], [158, 6], [112, 8]], 4);
      g.fillStyle = "rgba(60,90,50,0.6)"; g.fillRect(118, -4, 30, 4);
      g.restore();
      // тулуб: стовбур із корою, наплічник-пластина, порожнина в грудях
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.35));
      shape(g, c.bark, [[-38, 8], [-52, -60], [-36, -96], [34, -98], [54, -64], [40, 8]], 5);
      g.strokeStyle = "rgba(0,0,0,0.4)"; g.lineWidth = 2;
      for (let i = -3; i <= 3; i++) { g.beginPath(); g.moveTo(i * 9, 6); g.quadraticCurveTo(i * 11 + 3, -46, i * 8, -92); g.stroke(); }
      shape(g, c.plate, [[-46, -84], [-28, -104], [8, -106], [6, -86], [-24, -78]], 4);
      // порожнина
      g.fillStyle = "#060805"; g.beginPath(); g.ellipse(4, -56, 14, 20, 0.1, 0, PI2); g.fill();
      glowBall(g, 4, -56, 26 + p.glow * 30, 0.35 + p.glow * 0.8);
      for (let i = 0; i < 4; i++) stroke(g, 2, c.barkDark, [[-8 + i * 6, -76], [-6 + i * 6 + S(i) * 3, -36]]); // ребра-корені
      g.fillStyle = c.moss;
      for (let i = 0; i < 6; i++) { const x = -30 + i * 12; g.beginPath(); g.moveTo(x, -96 + (i % 2) * 4); g.lineTo(x + 10, -96); g.lineTo(x + 4, -78 + (i % 3) * 4 + S(p.wave * PI2 + i) * 2); g.closePath(); g.fill(); }
      g.restore();
      const fr = limb(g, 10, hip, p.hipF, this.thigh, p.kneeF, this.shin, 30, c.bark, 24);
      roots(fr.end, 1);
      // голова-пень: щілина очей, корона з гілок
      g.save(); g.translate(6 + p.lean * 0.5, hip - 100); g.rotate(R(p.head));
      shape(g, c.bark, [[-22, 18], [-24, -18], [-10, -30], [16, -30], [24, -16], [22, 18]], 5);
      g.fillStyle = "#050605"; g.fillRect(-4, -10, 22, 7);
      eye(g, 4, -7, 3, p.eyes, this.eyes); eye(g, 13, -7, 2.6, p.eyes, this.eyes);
      const br = (x, y, a, l, w) => stroke(g, w, c.barkDark, [[x, y], [x + S(R(a)) * l, y - C(R(a)) * l]]);
      br(-12, -26, -30, 24, 4); br(-20, -44, -60, 14, 2.5); br(-4, -28, -8, 28, 4); br(0, -50, 30, 14, 2.5); br(10, -28, 24, 28, 4); br(20, -44, 60, 14, 2.5); br(16, -24, 50, 22, 3);
      g.restore();
      // передня рука: вільна, світиться долонею (постріл коренем, отруйне поле)
      const fa = limb(g, 26, hip - 86, p.arm, 34, p.elbow, 30, 19, c.bark, 15);
      shape(g, c.barkDark, [[fa.end[0] - 6, fa.end[1] - 4], [fa.end[0] + 10, fa.end[1] - 2], [fa.end[0] + 14, fa.end[1] + 10], [fa.end[0] - 2, fa.end[1] + 10]], 3);
      glowBall(g, fa.end[0] + 4, fa.end[1] + 4, 10 + p.shield * 30, p.shield);
      slashArc(g, -10, hip - 74, 150, p.slashFrom, p.slashTo, p.slash);
    },
    states: {}
  };
  // У Вартового «shield» — сяйво долоні (постріл коренем, отруйне поле), «glow» — сяйво грудей (фаза 2).
  (function () {
    const swing = (contact, from, to, heavy) => [[0, {}], [contact - 0.16, { armB: -150, elbowB: -30, weapon: -55, lean: -12, eyes: 1.3 }], [contact - 0.04, { armB: -160, weapon: -66, lean: -16, y: heavy ? -6 : 0, eyes: 1.7 }],
      [contact, { armB: 60, elbowB: 0, weapon: heavy ? 100 : 60, lean: 24, x: 18, slash: 1, slashFrom: from, slashTo: to, hipF: 30, kneeF: 24, hipB: -28 }], [contact + 0.16, { armB: 70, weapon: heavy ? 110 : 80, lean: 20, x: 18, slash: 0.3, slashFrom: from, slashTo: to }], [1, {}]];
    const idle = gl => n => cycle(n, t => ({ y: S(t * PI2) * 2, lean: 2, armB: 14, elbowB: -10, weapon: 62, arm: 10, head: S(t * PI2) * 2, wave: t, glow: gl }));
    const walk = gl => n => cycle(n, t => ({ lean: 6, y: -Math.abs(S(t * PI2)) * 4, hipF: 20 * S(t * PI2), kneeF: 10 + 16 * Math.max(0, -S(t * PI2)), hipB: -20 * S(t * PI2), kneeB: 10 + 16 * Math.max(0, S(t * PI2)), armB: 14, elbowB: -10, weapon: 58, arm: 10 - 10 * S(t * PI2), wave: t * 2, glow: gl }));
    const withGlow = (keys, gl) => keys.map(([u, k]) => [u, Object.assign({ glow: gl }, k)]);
    Object.assign(WARDEN.states, {
      // спить деревом: зігнутий, корені в землі
      Dormant: n => cycle(n, t => ({ lean: 16, head: 26, eyes: 0, y: 8, hipF: 30, kneeF: 60, hipB: -10, kneeB: 70, armB: 40, weapon: 95, arm: 50, elbow: 40, glow: 0, wave: t * 0.3 })),
      Intro: n => track(n, [[0, { lean: 16, head: 26, eyes: 0, y: 8, hipF: 30, kneeF: 60, hipB: -10, kneeB: 70, armB: 40, weapon: 95, arm: 50, elbow: 40 }], [0.5, { lean: 4, head: 4, eyes: 0.8, y: 2, glow: 0.4 }], [0.8, { lean: -8, head: -12, eyes: 1.7, armB: -120, weapon: -70, y: -6, glow: 0.8 }], [1, {}]]),
      IdleP1: idle(0), WalkP1: walk(0),
      SlashP1: n => track(n, swing(0.5, -120, 60, false)),
      // низько й широко — уздовж підлоги
      Sweep: n => track(n, [[0, {}], [0.34, { armB: -40, elbowB: -40, weapon: 170, lean: -14, y: 8, hipF: 30, kneeF: 50, eyes: 1.5 }], [0.5, { armB: 90, elbowB: 0, weapon: 10, lean: 26, x: 18, y: 10, slash: 1, slashFrom: 150, slashTo: 30, hipF: 40, kneeF: 50, hipB: -30 }], [0.66, { armB: 100, weapon: 20, lean: 20, x: 18, y: 8, slash: 0.3, slashFrom: 150, slashTo: 30 }], [1, {}]]),
      // глефа відтягнута й вирівняна — і ривок
      ChargeStart: n => track(n, [[0, {}], [0.7, { armB: 20, elbowB: -60, weapon: -6, lean: -14, y: 6, hipF: 30, kneeF: 50, hipB: -30, kneeB: 40, eyes: 1.7 }], [1, { armB: 70, weapon: 0, lean: 22 }]]),
      Charge: n => cycle(n, t => ({ armB: 74, weapon: 0, lean: 28, y: -Math.abs(S(t * PI2)) * 6, hipF: 40 * S(t * PI2), kneeF: 20 + 30 * Math.max(0, -S(t * PI2)), hipB: -40 * S(t * PI2), kneeB: 20 + 30 * Math.max(0, S(t * PI2)), wave: t * 2 })),
      ChargeRecover: n => track(n, [[0, { armB: 74, weapon: 0, lean: 30, x: 10 }], [0.4, { lean: -8, y: 4, hipF: 26, kneeF: 40, weapon: 60 }], [1, {}]]),
      // долоня вгору, сяйво росте до 0,74, кидок на 0,76
      RootShot: n => track(n, [[0, {}], [0.74, { arm: -150, elbow: -30, shield: 1.2, lean: -10, head: -10, eyes: 1.6 }], [0.8, { arm: 90, elbow: 0, shield: 0.3, lean: 18, x: 8 }], [1, { shield: 0 }]]),
      Stunned: n => cycle(n, t => ({ lean: -6 + S(t * PI2) * 8, head: S(t * PI2) * 12, eyes: 0.4, armB: 30, weapon: 120, wave: t })),
      Recoil: n => track(n, [[0, { lean: -18, x: -10, head: -14, eyes: 0.6 }], [1, {}]]),
      PhaseTransition: n => track(n, [[0, {}], [0.3, { lean: 20, y: 10, hipF: 40, kneeF: 70, hipB: -10, kneeB: 80, head: 20, eyes: 0.3, glow: 0.4 }], [0.7, { lean: -10, y: -4, head: -16, eyes: 2, glow: 1.5, armB: -140, weapon: -60, arm: -120 }], [1, { glow: 1 }]]),
      IdleP2: idle(1), MoveP2: walk(1),
      SlashP2: n => track(n, withGlow(swing(0.46, -130, 70, true), 1)),
      ComboP2: n => track(n, withGlow(swing(0.5, -150, 100, true), 1.2)),
      Combo1: n => track(n, swing(0.5, -110, 50, false)),
      Combo2: n => track(n, swing(0.5, 80, -60, false)),
      Combo3: n => track(n, swing(0.52, -150, 100, true)),
      // обидві руки вгору до 0,62, древко в землю 0,64…0,86
      RootSlam: n => track(n, [[0, { glow: 1 }], [0.62, { armB: -170, elbowB: -10, weapon: -80, arm: -160, lean: -10, y: -6, eyes: 1.8, glow: 1.4 }], [0.76, { armB: 40, elbowB: 10, weapon: 100, arm: 50, lean: 26, y: 10, sy: 0.92, hipF: 40, kneeF: 60, hipB: -30, slash: 1, slashFrom: -100, slashTo: 95, glow: 1.6 }], [0.86, { armB: 50, weapon: 100, lean: 24, y: 10, sy: 0.92, glow: 1.2 }], [1, { glow: 1 }]]),
      // відкрита долоня над місцем калюжі майже секунду
      PoisonFieldCast: n => track(n, [[0, { glow: 1 }], [0.8, { arm: 60, elbow: 40, shield: 1.4, lean: 14, head: 18, eyes: 1.5, glow: 1.3 }], [1, { arm: 30, shield: 0.4, glow: 1 }]]),
      JumpStart: n => track(n, [[0, { glow: 1 }], [0.7, { y: 12, sy: 0.86, hipF: 40, kneeF: 70, hipB: -30, kneeB: 60, lean: 10, armB: -150, weapon: -80, glow: 1 }], [1, { y: -20, sy: 1.06, glow: 1 }]]),
      Jump: n => cycle(n, t => ({ y: -28, hipF: 30, kneeF: 50, hipB: -10, kneeB: 60, armB: -160, weapon: -90, glow: 1, wave: t })),
      Slam: n => track(n, [[0, { y: -10, armB: -160, weapon: -90, glow: 1 }], [1, { y: 14, sy: 0.84, lean: 26, hipF: 50, kneeF: 80, hipB: -30, kneeB: 70, armB: 60, weapon: 95, slash: 1, slashFrom: -100, slashTo: 95, glow: 1.4 }]]),
      SlamRecover: n => track(n, [[0, { y: 14, sy: 0.84, lean: 26, hipF: 50, kneeF: 80, hipB: -30, kneeB: 70, armB: 60, weapon: 95, glow: 1 }], [1, { glow: 1 }]]),
      // падає деревом і гасне
      Death: n => track(n, [[0, { glow: 1, head: -10 }], [0.35, { lean: 20, y: 14, hipF: 60, kneeF: 100, hipB: 10, kneeB: 100, head: 24, eyes: 0.4, glow: 1.8, weapon: 140 }], [1, { lean: 70, y: 40, sy: 0.8, hipF: 80, kneeF: 110, hipB: 40, kneeB: 110, head: 40, eyes: 0, glow: 0, weapon: 175, alpha: 0.9 }]])
    });
  })();

  Object.assign(E.ACTORS, { CorruptedHound: HOUND, ForestArcher: ARCHER, RootShaman: SHAMAN, RoyalSpearman: SPEAR, HollowWarden: WARDEN });
})(window);
