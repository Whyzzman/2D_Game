// Герой «Тіні Забутого Королівства» (2026-10-28, просьба користувача «зміни візуал персонажа, придумай героя»;
// образ вибрав Claude за постійною згодою — концепт design/characters/hero/concept_hero_a_3.png): маленький
// мандрівник у гострому каптурі, з темряви якого світять дві бліді очі; довгий сіро-блакитний шарф — друга
// лінія руху; плащ-дзвін до колін; короткий сталевий меч; фіолетова «Тінь» на лівій руці.
// Кожен стан анімації — ключові пози, між ними кадри інтерполюються. Кадр 256 × 224, стопи в (128, 200),
// герой дивиться праворуч (гра дзеркалить сама). Чисті форми з чорнильним контуром, як намальована гра.
(function (root) {
  "use strict";

  const C = {
    ink: "#07080c",
    cloak: "#262c40", cloakLight: "#3a4462", cloakDark: "#151926", lining: "#4d5f7a",
    scarf: "#6a83a3", scarfLight: "#9cb2cc", scarfDark: "#3e4f68",
    void: "#05060a", eye: "#e9f1ff", eyeGlow: "rgba(200,220,255,0.55)",
    boot: "#16161e", bootLight: "#2b2b38", leg: "#1d1f2b",
    blade: "#c8d1db", bladeEdge: "#f1f5fa", guard: "#7a6448", grip: "#3a2e24",
    shadow: "#9a6bff", shadowSoft: "rgba(154,107,255,0.35)", slash: "rgba(225,235,255,0.85)"
  };

  // ---------- пози ----------
  // Кути в градусах. Нога: hip — від вертикалі вниз (+ вперед), knee — згин (+ назад). Рука: від вертикалі
  // вниз (+ вперед). Меч — абсолютний кут леза (0 — вперед, -90 — вгору, 90 — вниз).
  const BASE = {
    x: 0, y: 0, lean: 0, sy: 1, sx: 1,
    hipF: 4, kneeF: 6, hipB: -4, kneeB: 6,
    arm: 20, sword: 55, armB: -10,
    flare: 0.15, scarf: 165, scarfWave: 0, scarfLen: 1, hood: 0, eyes: 1, tip: 0, hem: 0,
    glow: 0, slash: 0, slashFrom: -60, slashTo: 60, streak: 0, alpha: 1, sword2: 0
  };

  function pose(p) { return Object.assign({}, BASE, p); }

  function lerp(a, b, t) {
    const o = {};
    for (const k in a) o[k] = typeof a[k] === "number" ? a[k] + (b[k] - a[k]) * t : (t < 0.5 ? a[k] : b[k]);
    return o;
  }

  // Ключі розкладено по кадрах: [кадр, поза]; між ключами — плавно (ease in-out).
  function track(frames, keys) {
    // Ключі, розставлені під меншу кількість кадрів, розтягуються на всі кадри стану.
    const last = keys[keys.length - 1][0];
    if (last > 0 && last < frames - 1) keys = keys.map(([k, p]) => [k * (frames - 1) / last, p]);
    const out = [];
    for (let f = 0; f < frames; f++) {
      let i = 0;
      while (i < keys.length - 1 && keys[i + 1][0] <= f) i++;
      const [fa, pa] = keys[i], [fb, pb] = keys[Math.min(i + 1, keys.length - 1)];
      const t = fb === fa ? 0 : (f - fa) / (fb - fa);
      out.push(lerp(pose(pa), pose(pb), t * t * (3 - 2 * t)));
    }
    return out;
  }

  // Цикл: кадр f з n — фаза 0..1.
  function cycle(n, fn) { const out = []; for (let f = 0; f < n; f++) out.push(pose(fn(f / n, f))); return out; }

  const S = Math.sin, PI2 = Math.PI * 2;

  // Удар збоку: замах назад, удар (кадр контакту з дугою), проведення. contact — номер кадру удару.
  function swing(n, contact, from, to, heavy) {
    const wind = pose({ tip: 12, hem: 0.3, lean: -8, arm: -60, sword: -120 + (heavy ? -20 : 0), hipF: 12, kneeF: 20, hipB: -14, kneeB: 10, y: 3, flare: 0.35, scarf: 150 });
    const hit = pose({ tip: -22, hem: 0.7, sy: 0.94, sx: 1.07, lean: 21, arm: 86, sword: to, hipF: 42, kneeF: 30, hipB: -32, kneeB: 6, x: 13, y: 3, flare: 0.8, scarfLen: 1.3, scarf: 195, slash: 1, slashFrom: from, slashTo: to });
    const follow = pose({ tip: -8, hem: 0.9, lean: 13, arm: 100, sword: to + 30, hipF: 30, kneeF: 22, hipB: -24, kneeB: 10, x: 11, y: 2, flare: 0.55, scarf: 185, slash: 0.25, slashFrom: from, slashTo: to });
    // глибший замах за мить до удару (передчуття) і перехльост після — тіло наздоганяє меч
    const deep = Object.assign({}, wind, { lean: -12, arm: -75, sword: wind.sword - 20, y: 5, sy: 0.94, sx: 1.04, tip: 18 });
    const over = Object.assign({}, follow, { lean: 25, x: 16, sword: to + 45, arm: 112, tip: -26, sy: 1.03, hipF: 40, kneeF: 28, flare: 0.75 });
    const keys = [[0, pose({ lean: 2 })], [Math.max(1, contact - 2), wind], [contact - 1, deep], [contact, hit],
      [contact + 1, over], [Math.min(n - 2, contact + 2), follow], [n - 1, pose({ lean: 3, arm: 40, sword: 60, tip: 4 })]];
    return track(n, keys.filter((k, i) => i === 0 || k[0] > keys[i - 1][0]));
  }

  // Біг (2026-11-12, «приємні анімації»): довгий крок довгих ніг із фазою польоту, тіло легко пливе над кроком
  // (найвище — у польоті, найнижче — на контакті), плащ і шарф летять назад, каптур відстає.
  const RUN = n => cycle(n, (t) => {
    const a = t * PI2, up = Math.abs(S(a)), contact = Math.max(0, Math.cos(a * 2));
    return {
      lean: 10 + up * 3, y: -up * 9 + 4 + contact * 2, sy: 1 + (up - 0.5) * 0.07, sx: 1 - (up - 0.5) * 0.04,
      tip: -20 - up * 10, hem: t * 2, hood: -up * 3 + 1,
      hipF: 38 * S(a), kneeF: 14 + 52 * Math.max(0, -S(a + 0.9)),
      hipB: -38 * S(a), kneeB: 14 + 52 * Math.max(0, S(a + 0.9)),
      arm: -24 * S(a) + 30, sword: 150 - 12 * S(a), armB: 40 * S(a) - 6,
      flare: 0.85 + 0.15 * S(a * 2), scarf: 192 + 6 * S(a * 2), scarfWave: t, scarfLen: 1.35
    };
  });

  const STATES = {
    // 2026-11-13, «анімації»: стійка дихає помітно — плечі й каптур ходять, плащ і шарф колише вітер, меч гойдається
    Idle: n => cycle(n, t => ({ y: S(t * PI2) * 2.6, sy: 1 + S(t * PI2) * 0.034, sx: 1 - S(t * PI2) * 0.02, lean: S(t * PI2 - 1.2) * 1.6, arm: 18 + S(t * PI2 - 0.5) * 7, sword: 52 + S(t * PI2 - 0.9) * 6, armB: -10 - S(t * PI2) * 6, scarf: 162 + S(t * PI2 - 0.6) * 18, scarfWave: t, scarfLen: 1.1 + S(t * PI2) * 0.06, flare: 0.2 + S(t * PI2 - 1) * 0.12, tip: S(t * PI2 - 0.8) * 13, hem: t, hood: S(t * PI2 - 0.4) * 3.5, hipF: 5 + S(t * PI2) * 2, kneeF: 7 + S(t * PI2) * 4, kneeB: 7 + S(t * PI2) * 4 })),
    IdleGlance: n => track(n, [[0, {}], [3, { hood: -10, lean: -3 }], [6, { hood: 8, lean: 2 }], [9, {}]]),
    IdleVariant: n => track(n, [[0, {}], [3, { armB: 60, hood: 6, y: 1 }], [6, { armB: 70, hood: -4, scarf: 140, scarfWave: 0.5 }], [9, {}]]),
    Run: RUN,
    Turn: n => track(n, [[0, { lean: -10, sx: 0.9, scarf: 120 }], [1, { lean: 4, sx: 1, scarf: 175 }]]),
    JumpStart: n => track(n, [[0, { y: 9, sy: 0.86, sx: 1.06, hipF: 34, kneeF: 58, hipB: -14, kneeB: 54, lean: 8, tip: 12 }], [2, { y: -4, sy: 1.12, sx: 0.93, hipF: 6, kneeF: 4, hipB: -12, kneeB: 30, arm: -20, sword: 165, flare: 0.45, tip: 24, scarf: 140 }]]),
    JumpRise: n => cycle(n, t => ({ sy: 1.07, sx: 0.95, tip: 24, hem: t, hipF: 26, kneeF: 62, hipB: -16, kneeB: 28, arm: -14, sword: 162, armB: -40, flare: 0.35 + t * 0.15, scarf: 145, scarfWave: t, scarfLen: 1.25 })),
    Fall: n => cycle(n, t => ({ tip: -26, hem: t * 2, hipF: 16, kneeF: 22, hipB: -14, kneeB: 34, arm: 50, sword: 115, armB: -55, flare: 0.95 + 0.12 * S(t * PI2), scarf: 228, scarfWave: t, scarfLen: 1.35, y: -2, sy: 1.03, sx: 0.97 })),
    Land: n => track(n, [[0, { y: 14, sy: 0.84, sx: 1.07, hipF: 40, kneeF: 72, hipB: -26, kneeB: 64, flare: 0.9, tip: 26, arm: 40 }], [1, { y: 2, sy: 1.03, sx: 0.98, tip: -10, flare: 0.4, hipF: 12, kneeF: 18, hipB: -8, kneeB: 16 }], [2, {}]]),
    HardLand: n => track(n, [[0, { y: 12, sy: 0.75, hipF: 40, kneeF: 70, hipB: -30, kneeB: 60, arm: 60, sword: 100, flare: 0.8, lean: 20 }], [2, { y: 10, sy: 0.8, lean: 18, hipF: 38, kneeF: 65, hipB: -28, kneeB: 55 }], [4, {}]]),
    Dash: n => track(n, [[0, { tip: -24, hem: 0.5, lean: 30, x: -6, streak: 0.4, arm: 60, sword: 175, flare: 0.9, scarf: 185, scarfLen: 1.4 }], [2, { lean: 34, x: 4, streak: 1, hipF: 50, kneeF: 30, hipB: -45, kneeB: 20, flare: 1, scarfLen: 1.5 }], [4, { lean: 16, x: 2, streak: 0.3, flare: 0.5 }]]),
    AirDash: n => track(n, [[0, { lean: 26, x: -6, streak: 0.4, arm: 60, sword: 175, flare: 0.9, scarf: 180, scarfLen: 1.4, hipF: 30, kneeF: 50, hipB: -10, kneeB: 60 }], [2, { lean: 30, x: 4, streak: 1, hipF: 40, kneeF: 60, hipB: 0, kneeB: 70, flare: 1, scarfLen: 1.5 }], [4, { lean: 12, x: 2, streak: 0.3, flare: 0.6 }]]),
    WallSlide: n => cycle(n, t => ({ lean: -8, x: 6, hipF: 30, kneeF: 50, hipB: 10, kneeB: 40, arm: 140, armB: 150, sword: 100, flare: 0.3, scarf: 250, scarfWave: t, y: S(t * PI2) })),
    WallJump: n => track(n, [[0, { lean: -20, x: 4, hipF: 40, kneeF: 60, hipB: 20, kneeB: 50, arm: -40, sword: 170, flare: 0.5, scarf: 260 }], [1, { lean: 10, sy: 1.08, hipF: 10, kneeF: 20, hipB: -30, kneeB: 20, flare: 0.4, scarf: 220 }]]),
    WallRun: n => cycle(n, t => ({ lean: -20, x: 6, y: -S(t * PI2) * 3, hipF: 60 + 20 * S(t * PI2), kneeF: 60, hipB: 20 - 20 * S(t * PI2), kneeB: 60, arm: 150, sword: -60, flare: 0.4, scarf: 270, scarfWave: t })),
    Attack1: n => swing(n, Math.round(n * 0.43), -80, 50, false),
    Attack2: n => swing(n, Math.round(n * 0.43), 70, -50, false),
    Attack3: n => swing(n, Math.round(n * 0.44), -110, 80, false),
    Heavy: n => swing(n, Math.round(n * 0.58), -150, 90, true),
    AttackUp: n => track(n, [[0, {}], [1, { arm: 40, sword: 30, y: 2, hipF: 10, kneeF: 15 }], [2, { arm: 170, sword: -90, slash: 1, slashFrom: 10, slashTo: -170, flare: 0.4, scarf: 200, sy: 1.06 }], [3, { arm: 175, sword: -100, slash: 0.3, slashFrom: 10, slashTo: -170 }], [5, { arm: 40, sword: 60 }]]),
    AttackDown: n => track(n, [[0, { hipF: 20, kneeF: 30, hipB: -6, kneeB: 30 }], [1, { arm: 120, sword: -60, flare: 0.5, scarf: 240 }], [2, { arm: 10, sword: 95, slash: 1, slashFrom: -20, slashTo: 170, hipF: 30, kneeF: 50, hipB: -10, kneeB: 50, flare: 0.7, scarf: 260 }], [3, { arm: 5, sword: 100, slash: 0.3, slashFrom: -20, slashTo: 170 }], [5, { arm: 30, sword: 80 }]]),
    Block: n => cycle(n, t => ({ lean: -4, arm: 70, sword: -70, hipF: 18, kneeF: 18, hipB: -20, kneeB: 12, flare: 0.25, y: 2 + S(t * PI2) * 0.5 })),
    BlockImpact: n => track(n, [[0, { lean: -14, x: -5, arm: 60, sword: -60, hipF: 20, kneeF: 25, hipB: -26, kneeB: 18, flare: 0.5, scarf: 140 }], [1, { lean: -6, arm: 70, sword: -70, hipF: 18, kneeF: 18, hipB: -20, kneeB: 12 }]]),
    Parry: n => track(n, [[0, { arm: 60, sword: -70, lean: -4 }], [1, { arm: 95, sword: -20, lean: 8, slash: 1, slashFrom: -80, slashTo: -10, glow: 0.4 }], [2, { arm: 70, sword: 20, lean: 4 }]]),
    GuardBroken: n => track(n, [[0, { lean: -24, x: -8, arm: -40, sword: -150, armB: -60, flare: 0.8, scarf: 130, hood: -10 }], [2, { lean: -16, x: -6, arm: -10, sword: 160, hood: -6 }]]),
    Hurt: n => track(n, [[0, { lean: -30, x: -11, y: -5, sy: 1.06, sx: 0.95, arm: -50, sword: 165, armB: -80, hipF: 30, kneeF: 40, hipB: 12, kneeB: 46, flare: 1, scarf: 110, scarfLen: 1.3, hood: -16, tip: 26, eyes: 0.3 }], [1, { lean: -12, x: -4, y: 4, sy: 0.95, hipF: 16, kneeF: 30, hipB: -10, kneeB: 26, hood: -6, eyes: 0.8, flare: 0.5 }]]),
    Stunned: n => cycle(n, t => ({ lean: -6 + S(t * PI2) * 8, hood: S(t * PI2) * 10, arm: 30, sword: 80, eyes: 0.4, scarf: 150, scarfWave: t })),
    Death: n => track(n, [[0, { lean: -20, hood: -10, eyes: 0.5 }], [2, { lean: 30, y: 14, sy: 0.85, hipF: 60, kneeF: 90, hipB: 20, kneeB: 90, arm: 60, sword: 120, eyes: 0.2, flare: 0.9 }], [5, { lean: 70, y: 30, sy: 0.8, hipF: 80, kneeF: 100, hipB: 40, kneeB: 100, arm: 90, sword: 0, eyes: 0, flare: 1, alpha: 0.85 }]]),
    CastStart: n => cycle(n, t => ({ lean: -4, armB: 80, arm: 10, sword: 80, glow: 0.6 + 0.4 * S(t * PI2), flare: 0.3, scarf: 150, scarfWave: t })),
    CastRelease: n => track(n, [[0, { armB: 80, glow: 1, lean: -6 }], [1, { armB: 100, glow: 1.4, lean: 10, x: 4, flare: 0.6, scarf: 195 }], [2, { armB: 60, glow: 0.3, lean: 4 }]]),
    GroundBreaker: n => track(n, [[0, {}], [3, { y: -20, arm: 160, sword: -80, lean: -10, flare: 0.4, glow: 0.6, scarf: 260 }], [5, { y: 8, sy: 0.85, arm: 20, sword: 95, lean: 25, hipF: 40, kneeF: 70, hipB: -30, kneeB: 60, slash: 1, slashFrom: -60, slashTo: 100, glow: 1, flare: 0.9 }], [9, { glow: 0 }]]),
    DashStrike: n => track(n, [[0, { lean: 20, arm: -40, sword: -140, streak: 0.3 }], [3, { lean: 34, x: 8, arm: 90, sword: 5, streak: 1, slash: 1, slashFrom: -30, slashTo: 20, flare: 1, scarfLen: 1.5 }], [6, { lean: 10, arm: 80, sword: 30 }]]),
    ShadowDashStart: n => track(n, [[0, { lean: 20, glow: 0.4, streak: 0.3 }], [2, { lean: 32, glow: 1, streak: 0.8, alpha: 0.75, flare: 1 }]]),
    ShadowDashLoop: n => cycle(n, t => ({ lean: 34, glow: 1, streak: 1, alpha: 0.6, flare: 1, hipF: 50, kneeF: 30, hipB: -45, kneeB: 20, scarfLen: 1.5, scarfWave: t })),
    ShadowDashEnd: n => track(n, [[0, { lean: 30, glow: 1, alpha: 0.7, streak: 0.6 }], [2, { lean: 8, glow: 0, alpha: 1 }]]),
    ShadowStrike: n => track(n, [[0, { glow: 0.6, arm: -50, sword: -130 }], [2, { glow: 1.2, arm: 90, sword: 40, lean: 16, x: 6, slash: 1, slashFrom: -100, slashTo: 60 }], [4, { glow: 0.3, arm: 60, sword: 70 }]]),
    WeaponSwitch: n => track(n, [[0, { arm: 20, sword: 75 }], [1, { arm: -30, sword: -100, armB: 30 }], [2, { arm: 20, sword: 75 }]]),
    ActiveSkill: n => track(n, [[0, { glow: 0.5, arm: -60, sword: -120 }], [2, { glow: 1.2, arm: 90, sword: 0, lean: 18, x: 8, slash: 1, slashFrom: -120, slashTo: 40, streak: 0.6 }], [3, { glow: 0.4, arm: 70, sword: 40 }]]),
    Heal: n => track(n, [[0, {}], [2, { y: 8, hipF: 40, kneeF: 80, hipB: -10, kneeB: 90, armB: 50, arm: 40, sword: 120, glow: 0.8, hood: 10, eyes: 0.3 }], [4, { y: 8, hipF: 40, kneeF: 80, hipB: -10, kneeB: 90, armB: 50, glow: 1.2, hood: 12, eyes: 0.2 }], [5, { glow: 0.2 }]])
  };

  // ---------- малювання ----------
  function px(c) { return c; }

  function limb(g, x, y, ang1, len1, ang2, len2, w, color) {
    // від точки (x, y) вниз під кутом ang1 (0 — вниз, + вперед), потім ang2 згин
    const r1 = ang1 * Math.PI / 180, r2 = (ang1 - ang2) * Math.PI / 180;
    const kx = x + Math.sin(r1) * len1, ky = y + Math.cos(r1) * len1;
    const fx = kx + Math.sin(r2) * len2, fy = ky + Math.cos(r2) * len2;
    // звужені сегменти з круглим суглобом (2026-11-04, «на рівні Hollow Knight»): стегно повніше, до стопи тоншає
    g.fillStyle = C.ink;
    taper(g, x, y, kx, ky, w * 1.25 + 4, w * 0.95 + 4); taper(g, kx, ky, fx, fy, w * 0.95 + 4, w * 0.7 + 4);
    g.fillStyle = color;
    taper(g, x, y, kx, ky, w * 1.25, w * 0.95); taper(g, kx, ky, fx, fy, w * 0.95, w * 0.7);
    return [fx, fy, r2];
  }

  function taper(g, ax, ay, bx, by, wa, wb) {
    const len = Math.hypot(bx - ax, by - ay) || 1, nx = -(by - ay) / len, ny = (bx - ax) / len;
    const ra = wa / 2, rb = wb / 2, mx = (ax + bx) / 2, my = (ay + by) / 2, bulge = (ra + rb) * 0.55;
    g.beginPath();
    g.moveTo(ax + nx * ra, ay + ny * ra);
    g.quadraticCurveTo(mx + nx * bulge, my + ny * bulge, bx + nx * rb, by + ny * rb);
    g.lineTo(bx - nx * rb, by - ny * rb);
    g.quadraticCurveTo(mx - nx * bulge, my - ny * bulge, ax - nx * ra, ay - ny * ra);
    g.closePath(); g.fill();
    g.beginPath(); g.arc(ax, ay, ra, 0, Math.PI * 2); g.fill();
    g.beginPath(); g.arc(bx, by, rb, 0, Math.PI * 2); g.fill();
  }

  function boot(g, x, y, ang) {
    g.save(); g.translate(x, y); g.rotate(-ang * 0.3);
    g.fillStyle = C.ink; g.beginPath(); g.ellipse(4, 1, 10, 6, 0, 0, Math.PI * 2); g.fill();
    g.fillStyle = C.boot; g.beginPath(); g.ellipse(4, 0, 8, 4.5, 0, 0, Math.PI * 2); g.fill();
    g.fillStyle = C.bootLight; g.beginPath(); g.ellipse(6, -1.5, 4, 1.6, 0, 0, Math.PI * 2); g.fill();
    g.restore();
  }

  function scarfTail(g, nx, ny, p, frame) {
    const len = 58 * p.scarfLen, base = p.scarf * Math.PI / 180;
    const pts = [];
    for (let i = 0; i <= 10; i++) {
      const t = i / 10;
      const wave = Math.sin(t * 5 - (p.scarfWave + frame * 0.13) * Math.PI * 2) * 7 * t;
      const a = base + Math.sin(t * 2.2) * 0.18;
      pts.push([nx + Math.cos(a) * len * t - Math.sin(a) * wave, ny + Math.sin(a) * len * t + Math.cos(a) * wave + t * t * 16]);
    }
    const width = t => 7 * (1 - t * 0.45);
    const left = [], right = [];
    for (let i = 0; i < pts.length; i++) {
      const [x0, y0] = pts[Math.max(0, i - 1)], [x1, y1] = pts[Math.min(pts.length - 1, i + 1)];
      const dx = x1 - x0, dy = y1 - y0, l = Math.hypot(dx, dy) || 1, w = width(i / 10);
      left.push([pts[i][0] - dy / l * w, pts[i][1] + dx / l * w]);
      right.push([pts[i][0] + dy / l * w, pts[i][1] - dx / l * w]);
    }
    const path = () => {
      g.beginPath(); g.moveTo(...left[0]);
      left.forEach(q => g.lineTo(...q));
      // рваний кінець
      const e = pts[pts.length - 1];
      g.lineTo(e[0] + 4, e[1] + 6); g.lineTo(e[0] - 2, e[1] + 2); g.lineTo(e[0] + 1, e[1] + 9);
      right.slice().reverse().forEach(q => g.lineTo(...q));
      g.closePath();
    };
    g.lineJoin = "round";
    g.strokeStyle = C.ink; g.lineWidth = 4; path(); g.stroke();
    g.fillStyle = C.scarf; path(); g.fill();
    g.strokeStyle = C.scarfDark; g.lineWidth = 2;
    g.beginPath(); right.forEach((q, i) => i ? g.lineTo(...q) : g.moveTo(...q)); g.stroke();
    g.strokeStyle = C.scarfLight; g.lineWidth = 1.5;
    g.beginPath(); left.slice(0, 6).forEach((q, i) => i ? g.lineTo(...q) : g.moveTo(...q)); g.stroke();
  }

  function cloak(g, p) {
    // дзвін від плечей до колін; flare розкидає поділ назад
    const hemY = -30 + p.flare * 6, back = -30 - p.flare * 22, front = 22 + p.flare * 4;
    const path = () => {
      g.beginPath();
      g.moveTo(-14, -88);
      g.quadraticCurveTo(-26 - p.flare * 10, -50, back, hemY - p.flare * 10);
      // рваний поділ
      const n = 7;
      for (let i = 1; i <= n; i++) {
        const t = i / n, x = back + (front - back) * t;
        const dip = (i % 2 ? 7 : 0) + Math.sin(i * 1.7 + p.hem * Math.PI * 2) * 4;
        g.lineTo(x, hemY - p.flare * 10 * (1 - t) + dip);
      }
      g.quadraticCurveTo(24, -48, 14, -88);
      g.closePath();
    };
    g.lineJoin = "round";
    g.strokeStyle = C.ink; g.lineWidth = 5; path(); g.stroke();
    const grad = g.createLinearGradient(-20, -90, 20, -20);
    grad.addColorStop(0, C.cloakLight); grad.addColorStop(0.45, C.cloak); grad.addColorStop(1, C.cloakDark);
    g.fillStyle = grad; path(); g.fill();
    // складки
    g.strokeStyle = C.cloakDark; g.lineWidth = 2;
    [[-6, -80, -14, hemY], [6, -78, 8, hemY + 2], [-14, -70, back + 10, hemY - 4]].forEach(([a, b, c2, d]) => {
      g.beginPath(); g.moveTo(a, b); g.quadraticCurveTo((a + c2) / 2 + 3, (b + d) / 2, c2, d); g.stroke();
    });
    // світла кромка спереду
    g.strokeStyle = C.cloakLight; g.lineWidth = 2;
    g.beginPath(); g.moveTo(13, -84); g.quadraticCurveTo(22, -50, front - 2, hemY - 1); g.stroke();
  }

  function hood(g, p) {
    g.save(); g.translate(0, -106); g.rotate(p.hood * Math.PI / 180); g.scale(1.22, 1.22);
    const path = () => {
      g.beginPath();
      g.moveTo(-20, 14);
      g.quadraticCurveTo(-28, -8, -14, -24);
      // гострий кінчик, відкинутий назад
      // кінчик гнеться за рухом (tip, градуси): відстає в бігу, падає назад у стрибку
      const tb = p.tip * Math.PI / 180, tx = -30 * Math.cos(tb) + 54 * Math.sin(tb) * 0.35, ty = -54 + 30 * Math.sin(tb) * 0.6;
      g.quadraticCurveTo(-12 + p.tip * 0.15, -40, tx, ty);
      g.quadraticCurveTo(-4, -46, 10, -26);
      g.quadraticCurveTo(26, -14, 22, 8);
      g.quadraticCurveTo(10, 18, -20, 14);
      g.closePath();
    };
    g.lineJoin = "round";
    g.strokeStyle = C.ink; g.lineWidth = 5; path(); g.stroke();
    const grad = g.createLinearGradient(-20, -50, 20, 14);
    grad.addColorStop(0, C.cloakLight); grad.addColorStop(0.5, C.cloak); grad.addColorStop(1, C.cloakDark);
    g.fillStyle = grad; path(); g.fill();
    // отвір каптура — темрява обличчя
    g.fillStyle = C.void;
    g.beginPath(); g.ellipse(9, -2, 11, 12, -0.15, 0, Math.PI * 2); g.fill();
    g.strokeStyle = C.cloakLight; g.lineWidth = 1.5;
    g.beginPath(); g.ellipse(9, -2, 11.5, 12.5, -0.15, -1.9, 0.6); g.stroke();
    // очі
    if (p.eyes > 0.05) {
      const h = 4.2 * p.eyes;
      [[6, -3], [14, -3.6]].forEach(([ex, ey]) => {
        g.fillStyle = C.eyeGlow; g.beginPath(); g.ellipse(ex, ey, 4.5, h + 2.5, 0, 0, Math.PI * 2); g.fill();
        g.fillStyle = C.eye; g.beginPath(); g.ellipse(ex, ey, 2.1, h, 0, 0, Math.PI * 2); g.fill();
      });
    }
    g.restore();
  }

  function scarfWrap(g) {
    const path = () => {
      g.beginPath();
      g.moveTo(-18, -92); g.quadraticCurveTo(0, -84, 18, -94);
      g.lineTo(19, -84); g.quadraticCurveTo(0, -74, -19, -82); g.closePath();
    };
    g.strokeStyle = C.ink; g.lineWidth = 4; path(); g.stroke();
    g.fillStyle = C.scarf; path(); g.fill();
    g.strokeStyle = C.scarfLight; g.lineWidth = 1.5;
    g.beginPath(); g.moveTo(-14, -89); g.quadraticCurveTo(0, -82, 15, -91); g.stroke();
    g.strokeStyle = C.scarfDark; g.lineWidth = 1.5;
    g.beginPath(); g.moveTo(-16, -84); g.quadraticCurveTo(0, -77, 16, -86); g.stroke();
  }

  function sword(g, hx, hy, ang, big) {
    const r = ang * Math.PI / 180, L = big ? 92 : 66, W = big ? 6 : 4;
    g.save(); g.translate(hx, hy); g.rotate(r);
    // клинок
    g.fillStyle = C.ink; g.beginPath(); g.moveTo(6, -W - 2); g.lineTo(L, -2); g.lineTo(L + 8, 0); g.lineTo(L, 2); g.lineTo(6, W + 2); g.closePath(); g.fill();
    g.fillStyle = C.blade; g.beginPath(); g.moveTo(8, -W); g.lineTo(L, -1); g.lineTo(L + 6, 0); g.lineTo(L, 1); g.lineTo(8, W); g.closePath(); g.fill();
    g.strokeStyle = C.bladeEdge; g.lineWidth = 1.2; g.beginPath(); g.moveTo(9, -W + 1); g.lineTo(L, -1); g.stroke();
    // гарда й руків'я
    g.fillStyle = C.ink; g.fillRect(3, -9, 6, 18);
    g.fillStyle = C.guard; g.fillRect(4, -8, 4, 16);
    g.fillStyle = C.grip; g.fillRect(-8, -2.5, 12, 5);
    g.fillStyle = C.guard; g.beginPath(); g.arc(-9, 0, 3, 0, Math.PI * 2); g.fill();
    g.restore();
  }

  function slashArc(g, cx, cy, from, to, a) {
    if (a <= 0.01) return;
    const r = 92, f = from * Math.PI / 180, t = to * Math.PI / 180, ccw = to < from;
    g.save();
    g.globalAlpha = a;
    const grad = g.createRadialGradient(cx, cy, r * 0.55, cx, cy, r * 1.1);
    grad.addColorStop(0, "rgba(225,235,255,0)"); grad.addColorStop(0.7, C.slash); grad.addColorStop(1, "rgba(225,235,255,0)");
    g.fillStyle = grad;
    g.beginPath(); g.arc(cx, cy, r * 1.05, f, t, ccw); g.arc(cx, cy, r * 0.6, t, f, !ccw); g.closePath(); g.fill();
    g.strokeStyle = "rgba(255,255,255,0.9)"; g.lineWidth = 2.5;
    g.beginPath(); g.arc(cx, cy, r * 0.98, f, t, ccw); g.stroke();
    // на кадрі удару — суцільний серп сліду (як у Hollow Knight): тонкий на кінцях, товстий посередині
    if (a >= 0.9) {
      g.globalAlpha = 1; g.fillStyle = "#eef3ff";
      const n = 24, outer = [], inner = [];
      for (let i = 0; i <= n; i++) {
        const k = i / n, ang = f + (t - f) * k, w = Math.sin(Math.PI * k);
        outer.push([cx + Math.cos(ang) * r * 1.02, cy + Math.sin(ang) * r * 1.02]);
        inner.push([cx + Math.cos(ang) * r * (1.02 - 0.3 * w), cy + Math.sin(ang) * r * (1.02 - 0.3 * w)]);
      }
      g.beginPath(); g.moveTo(...outer[0]); outer.forEach(q => g.lineTo(...q)); inner.reverse().forEach(q => g.lineTo(...q)); g.closePath(); g.fill();
    }
    g.restore();
  }

  function draw(g, p, frame, big) {
    g.clearRect(0, 0, 256, 224);
    g.save();
    g.translate(128 + p.x, 219 + p.y);
    g.globalAlpha = p.alpha;
    // прожилки руху (ривок)
    if (p.streak > 0.01) {
      g.strokeStyle = `rgba(200,215,240,${0.35 * p.streak})`; g.lineCap = "round";
      for (let i = 0; i < 5; i++) {
        g.lineWidth = 3 - i * 0.4;
        const y = -30 - i * 16, l = 40 + i * 9;
        g.beginPath(); g.moveTo(-30 - l * p.streak, y); g.lineTo(-24, y); g.stroke();
      }
    }
    // «Тінь» навколо
    if (p.glow > 0.01) {
      const grad = g.createRadialGradient(0, -60, 10, 0, -60, 90);
      grad.addColorStop(0, `rgba(154,107,255,${0.35 * Math.min(1, p.glow)})`); grad.addColorStop(1, "rgba(154,107,255,0)");
      g.fillStyle = grad; g.fillRect(-120, -190, 240, 200);
    }
    g.scale(p.sx, p.sy);
    g.rotate(p.lean * Math.PI / 180 * 0.5);
    // шарф позаду
    scarfTail(g, -10, -88, p, frame);
    // задня нога
    const hipY = -36;
    const [bfx, bfy, br] = limb(g, -4, hipY, p.hipB, 19, p.kneeB, 17, 7, C.leg);
    boot(g, bfx, bfy, br);
    // задня рука (з «Тінню»)
    const rb = p.armB * Math.PI / 180;
    const [ax, ay] = limb(g, -8, -84, p.armB, 15, -10, 13, 6, C.cloakDark);
    if (p.glow > 0.05 || true) {
      const gl = 0.35 + 0.65 * Math.min(1, p.glow);
      const gg = g.createRadialGradient(ax, ay, 1, ax, ay, 9 + 8 * p.glow);
      gg.addColorStop(0, `rgba(190,160,255,${gl})`); gg.addColorStop(1, "rgba(154,107,255,0)");
      g.fillStyle = gg; g.beginPath(); g.arc(ax, ay, 10 + 8 * p.glow, 0, Math.PI * 2); g.fill();
    }
    void rb;
    // плащ
    cloak(g, p);
    // передня нога
    const [ffx, ffy, fr] = limb(g, 4, hipY, p.hipF, 19, p.kneeF, 17, 7, C.leg);
    boot(g, ffx, ffy, fr);
    // дуга удару — під рукою з мечем
    slashArc(g, 8, -70, p.slashFrom, p.slashTo, p.slash);
    // рука з мечем
    const [hx, hy] = limb(g, 8, -84, p.arm, 15, -18, 13, 6, C.cloak);
    sword(g, hx, hy, p.sword, big);
    g.fillStyle = C.ink; g.beginPath(); g.arc(hx, hy, 5, 0, Math.PI * 2); g.fill();
    g.fillStyle = C.cloakDark; g.beginPath(); g.arc(hx, hy, 3.6, 0, Math.PI * 2); g.fill();
    // голова
    hood(g, p);
    scarfWrap(g);
    g.restore();
  }

  // ---------- лялька з мальованих частин (2026-11-05, користувач: «персонаж має виглядати як картинка Midjourney») ----------
  // Частини вирізає tools/hero/puppet.py з аркуша Midjourney (parts/source.png); скелет і пози — ті самі, що вище:
  // тіло (каптур, плащ, шарф) — на стегнах, ноги — стегно й гомілка з колінами, рукавиця з мечем — на кінці руки.
  const PARTS = {}, K = 0.27;               // масштаб частин: каптур на 178 px над стопами
  // Стрункий силует (2026-11-12, користувач: «розміри персонажа дуже товсті, треба елегантніший, вільніший»; решено
  // Claude за постійною згодою): тіло вужче (KX), каптур менший (KY), ноги довші й тонші — зріст той самий.
  const KX = 0.205, KY = 0.236, HIP = -58, SHOULDER = -100, THIGH = 30, SHIN = 28, LEG_W = 6.2, ARM1 = 19, ARM2 = 17;
  const SWORD_ANGLE = 138.3;                 // кут клинка в аркуші (від руків'я до вістря), градуси
  function loadParts(base) {
    return fetch(base + "parts.json").then(r => r.json()).then(meta => Promise.all(Object.keys(meta).map(name => new Promise(res => {
      const img = new Image(); img.onload = () => { PARTS[name] = Object.assign({ img }, meta[name]); res(); }; img.src = base + name + ".png";
    }))));
  }
  function piece(g, name, x, y, deg, k) {
    const q = PARTS[name]; if (!q) return;
    g.save(); g.translate(x, y); g.rotate(deg * Math.PI / 180); g.scale(k || K, k || K);
    g.drawImage(q.img, -q.px, -q.py); g.restore();
  }
  // нога лялькою: стегно від стегна (hip — від вертикалі, + вперед), гомілка від коліна (knee — згин назад)
  // Ноги — темні щільні силуети з чоботами, як у Hollow Knight: мальовані ноги Midjourney в кадрі виходили
  // тоненькими й світлими (контур героя робив їх плямою біля стоп).
  function puppetLeg(g, side, x, y, hip, knee) {
    const [fx, fy, fr] = limb(g, x, y, hip, THIGH, knee, SHIN, LEG_W, side === "back" ? "#0d1119" : "#151b27");
    boot(g, fx, fy, fr);
  }
  function sleeve(g, x, y, ang1, len1, ang2, len2) {
    const r1 = ang1 * Math.PI / 180, r2 = (ang1 - ang2) * Math.PI / 180;
    const ex = x + Math.sin(r1) * len1, ey = y + Math.cos(r1) * len1;
    const hx = ex + Math.sin(r2) * len2, hy = ey + Math.cos(r2) * len2;
    g.fillStyle = C.ink;
    taper(g, x, y, ex, ey, 15, 11); taper(g, ex, ey, hx, hy, 11, 8);
    g.fillStyle = C.cloak;
    taper(g, x, y, ex, ey, 11, 8); taper(g, ex, ey, hx, hy, 8, 5);
    // складка й кромка світла по зовнішньому боці
    g.strokeStyle = C.cloakLight; g.lineWidth = 1.6; g.lineCap = "round";
    const nx = Math.cos(r1), ny = -Math.sin(r1);
    g.beginPath(); g.moveTo(x + nx * 3.5, y + ny * 3.5); g.quadraticCurveTo(ex + nx * 3.2, ey + ny * 3.2, hx + Math.cos(r2) * 2, hy - Math.sin(r2) * 2); g.stroke();
    // розтруб рукава біля рукавиці
    g.fillStyle = C.ink; g.beginPath(); g.ellipse(hx, hy, 6, 4.5, -r2, 0, Math.PI * 2); g.fill();
    g.fillStyle = C.cloakDark; g.beginPath(); g.ellipse(hx, hy, 4.6, 3.3, -r2, 0, Math.PI * 2); g.fill();
    return [hx, hy];
  }
  const smooth01 = (a, b, x) => { const t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
  // Пікселі мальованого тіла (від півоту на стегнах) — у позі p.
  function bodyMap(p) {
    const q = PARTS.body, W = q.w, H = q.h, Rad = Math.PI / 180;
    const top = -q.py * KY, bottom = (H - q.py) * KY, span = bottom - top;
    const neck = [0, top + span * 0.28];
    return (sx, sy) => {
      let x = (sx - q.px) * KX, y = (sy - q.py) * KY;
      const t = (bottom - y) / span;
      if (t < 0.42) {
        const w = Math.pow((0.42 - t) / 0.42, 1.5);
        const step = ((p.hipF || 0) - (p.hipB || 0)) / 80 * 0.08 * W * KX;
        // і відстає від нахилу: тканина не встигає за тілом
        const lag = -(p.lean || 0) * 0.004 * W * KX;
        // поділ вільніший: летить назад сильніше й ширше розвівається
        x += (step + lag + Math.sin(Math.PI * 2 * (p.hem || 0) + sx / W * 5) * 0.045 * W * KX - (p.flare || 0) * 0.16 * W * KX) * w;
        y += (p.flare || 0) * -0.05 * H * KY * w * w;
      }
      if (t > 0.7) {
        const r = Mesh.rot([x, y], neck, ((p.tip || 0) * 0.5 + (p.hood || 0) * 0.4) * Rad * smooth01(0.7, 1, t));
        x = r[0]; y = r[1];
      }
      return [x, y];
    };
  }

  function drawPuppet(g, p, frame, big) {
    g.clearRect(0, 0, 256, 224);
    g.save();
    g.translate(128 + p.x, 219 + p.y);
    g.globalAlpha = p.alpha;
    if (p.streak > 0.01) {
      g.strokeStyle = `rgba(200,215,240,${0.35 * p.streak})`; g.lineCap = "round";
      for (let i = 0; i < 5; i++) {
        g.lineWidth = 3 - i * 0.4;
        const y = -30 - i * 16, l = 40 + i * 9;
        g.beginPath(); g.moveTo(-30 - l * p.streak, y); g.lineTo(-24, y); g.stroke();
      }
    }
    if (p.glow > 0.01) {
      const grad = g.createRadialGradient(0, -80, 10, 0, -80, 100);
      grad.addColorStop(0, `rgba(154,107,255,${0.35 * Math.min(1, p.glow)})`); grad.addColorStop(1, "rgba(154,107,255,0)");
      g.fillStyle = grad; g.fillRect(-120, -200, 240, 210);
    }
    g.scale(p.sx, p.sy);
    g.rotate(p.lean * Math.PI / 180 * 0.5);
    const hipY = HIP;
    puppetLeg(g, "back", -3, hipY, p.hipB, p.kneeB);
    // задня рука з «Тінню»: світло на кулаці, сама рука під плащем
    const rb = p.armB * Math.PI / 180, ax = -6 + Math.sin(rb) * (ARM1 + ARM2), ay = SHOULDER + Math.cos(rb) * (ARM1 + ARM2);
    const gl = 0.35 + 0.65 * Math.min(1, p.glow);
    const gg = g.createRadialGradient(ax, ay, 1, ax, ay, 9 + 8 * p.glow);
    gg.addColorStop(0, `rgba(190,160,255,${gl})`); gg.addColorStop(1, "rgba(154,107,255,0)");
    g.fillStyle = gg; g.beginPath(); g.arc(ax, ay, 10 + 8 * p.glow, 0, Math.PI * 2); g.fill();
    // тіло — жива сітка (design/characters/_shared/mesh.js): поділ гойдається з кроком і відлітає назад у русі
    // (flare), вістря каптура відстає в бігу й падає назад у стрибку (tip), голова хитається (hood)
    g.save(); g.translate(0, hipY);
    const q = PARTS.body, map = bodyMap(p);
    if (!q.mesh) q.mesh = Mesh.prescale(q.img, q.w * KX * 1.5, q.h * KY * 1.5);
    Mesh.draw(g, q.mesh, 8, 12, (u, v) => map(u * q.w, v * q.h));
    // очі: заплющуються (eyes < 1) і спалахують перед ударом (eyes > 1)
    const eyes = [map(q.px + 38, q.py - 370), map(q.px + 95, q.py - 372)];
    if (p.eyes < 0.95) eyes.forEach(([ex, ey]) => { g.fillStyle = "#020204"; g.beginPath(); g.ellipse(ex, ey, 3.2, 4.6 * (1 - p.eyes) + 0.4, 0, 0, Math.PI * 2); g.fill(); });
    if (p.eyes > 1.02 || p.glow > 0.3) eyes.forEach(([ex, ey]) => {
      const k = Math.max(p.eyes - 1, p.glow * 0.5), e = g.createRadialGradient(ex, ey, 0, ex, ey, 9);
      e.addColorStop(0, `rgba(235,242,255,${Math.min(0.9, k)})`); e.addColorStop(1, "rgba(235,242,255,0)");
      g.fillStyle = e; g.beginPath(); g.arc(ex, ey, 9, 0, Math.PI * 2); g.fill();
    });
    g.restore();
    puppetLeg(g, "front", 3, hipY, p.hipF, p.kneeF);
    slashArc(g, 8, SHOULDER + 14, p.slashFrom, p.slashTo, p.slash);
    // рука з мечем: рукав у темряві плаща, рукавиця й мальований меч на кінці
    // рукав плаща (2026-11-12): ширший біля плеча, звужується до рукавиці, з холодною кромкою зверху — не паличка
    const [hx, hy] = sleeve(g, 6, SHOULDER, p.arm, ARM1, -18, ARM2);
    piece(g, "sword", hx, hy, p.sword - SWORD_ANGLE, K * (big ? 1.25 : 0.92));
    piece(g, "glove", hx, hy, p.sword * 0.3 - 20, K * 0.8);
    g.restore();
  }

  root.Hero = { STATES, draw: (g, p, frame, big) => (PARTS.body ? drawPuppet : draw)(g, p, frame, big), track, pose, loadParts };
})(window);
