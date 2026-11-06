// Вороги й бос Чумних Боліт (п'ятий рівень, 2026-11-05; решено Claude за постійною згодою). Як і в Селі й Столиці —
// новий вигляд на ріґах і рухах, що вже є: трупоїд — гончака, чумний лікар — шамана, п'явка — ворона, Олень — Стража.
(function (root) {
  "use strict";
  const E = root.Enemies, K = E.kit;
  const { S, PI2, R, INK, track, cycle, stroke, limb, shape, eye } = K;
  const A = E.ACTORS;

  function glow(g, x, y, r, rgb, a) {
    if (a <= 0.02) return;
    const gl = g.createRadialGradient(x, y, 0, x, y, r);
    gl.addColorStop(0, `rgba(${rgb},${0.85 * Math.min(1, a)})`); gl.addColorStop(1, `rgba(${rgb},0)`);
    g.fillStyle = gl; g.beginPath(); g.arc(x, y, r, 0, PI2); g.fill();
  }

  // ---------- Болотний трупоїд: гончак, утоплений — сіро-зелена шкіра, водорості, зелений жар очей ----------
  const EATER = Object.assign({}, A.CorruptedHound, {
    eyes: "#b6ff5a",
    look: { fur: "#3e4a38", furDark: "#1c2418", moss: "#5a7a3a", thorn: "#26301e", bark: "#2e3826" }
  });

  // ---------- Чумний лікар: чорний плащ до землі, крислатий капелюх, дзьоб-маска, склянка з отрутою ----------
  const DOCTOR = {
    w: 156, h: 178, eyes: "#c8ff7a",
    draw(g, p) {
      const c = { coat: "#242226", coatDark: "#121114", belt: "#4a3a28", mask: "#b0a68e", maskDark: "#6e6656", hat: "#1a181c", glass: "120,230,80" };
      g.save(); g.translate(0, -2); g.rotate(R(p.lean * 0.3));
      const hem = [];
      for (let i = 0; i <= 8; i++) hem.push([-26 + i * 6.5 + S(p.wave * PI2 + i) * 2.5, -(i % 2) * 5 + p.roots * 2]);
      shape(g, c.coat, [[-14, -112], [16, -112], [24, -60], [28, -2], ...hem.reverse(), [-26, -2], [-22, -60]], 4);
      g.fillStyle = c.belt; g.fillRect(-20, -66, 42, 5);
      g.fillStyle = "rgba(255,255,255,0.06)"; g.fillRect(8, -108, 8, 100);
      // склянки на поясі
      for (const x of [-12, -4]) { glow(g, x, -58, 7, c.glass, 0.5); g.fillStyle = "#6a9a3a"; g.fillRect(x - 2, -62, 4, 8); }
      limb(g, -8, -100, p.armB || -10, 20, p.elbowB || -10, 18, 7, c.coatDark);
      g.restore();
      // голова: дзьоб уперед, круглі скляні очі, широкий капелюх
      g.save(); g.translate(8 + p.lean * 0.5, -122); g.rotate(R(p.head));
      shape(g, c.coatDark, [[-14, 16], [-16, -6], [-4, -18], [12, -14], [16, 4], [12, 16]], 4);
      shape(g, c.mask, [[2, -8], [40, 8], [36, 12], [4, 8]], 3);
      stroke(g, 1.5, c.maskDark, [[8, 2], [34, 9]]);
      g.fillStyle = INK; g.beginPath(); g.arc(4, -6, 5, 0, PI2); g.fill();
      eye(g, 4, -6, 3, p.eyes, this.eyes);
      shape(g, c.hat, [[-30, -14], [32, -18], [26, -10], [-24, -8]], 3);
      shape(g, c.hat, [[-12, -14], [-10, -36], [12, -38], [14, -16]], 3);
      g.restore();
      // рука з тростиною; на замаху — склянка, що світиться
      const arm = limb(g, 12 + p.lean * 0.4, -100, p.arm, 22, p.elbow, 20, 7, c.coat);
      g.save(); g.translate(arm.end[0], arm.end[1]); g.rotate(R(p.weapon - 90));
      stroke(g, 3.5, "#3a2e22", [[0, 46], [1, 0], [0, -40]]);
      shape(g, "#8a7a5a", [[-5, -40], [5, -40], [6, -46], [-6, -46]], 2);
      glow(g, 10, -6, 12 + p.glow * 22, c.glass, 0.35 + p.glow);
      g.fillStyle = "#7acb3a"; g.beginPath(); g.ellipse(10, -6, 5 + p.glow * 1.5, 7 + p.glow * 1.5, 0, 0, PI2); g.fill();
      g.restore();
    },
    states: A.RootShaman.states
  };

  // ---------- Болотна п'явка: товсте кільчасте тіло, перетинчасті крила, кругла паща ----------
  // 2026-11-06 (вирішено Claude за постійною згодою): не мультяшні кільця двох кольорів, а слизька темна трубка з
  // глянцем по спині й ледь видними перетяжками, крила — прозорі перетинки з прожилками, паща — коло блідих зубів.
  const LEECH = {
    w: 140, h: 110, eyes: "#e8ff8a",
    draw(g, p) {
      g.save(); g.translate(0, -45); g.rotate(R(p.lean));
      const wing = (sign, alpha) => {
        g.save(); g.translate(2, -9); g.rotate(R(-90 + p.wingUp * 70 * sign + p.flap * 35));
        const len = 30 + 12 * p.spread;
        g.globalAlpha = alpha;
        g.fillStyle = "#b8cf9a";
        g.beginPath(); g.moveTo(0, -2); g.quadraticCurveTo(len * 0.6, -11, len, -3); g.quadraticCurveTo(len * 0.7, 6, 0, 3); g.closePath(); g.fill();
        g.globalAlpha = Math.min(1, alpha * 2.2);
        g.strokeStyle = "#1c2616"; g.lineWidth = 1.1; g.lineCap = "round";
        [[-4, 0.95], [-1, 0.75], [2, 0.55]].forEach(([dy, k]) => {
          g.beginPath(); g.moveTo(0, 0); g.quadraticCurveTo(len * k * 0.5, dy - 3, len * k, dy * 0.6); g.stroke();
        });
        g.restore();
      };
      wing(-1, 0.22);
      // хребет від хвоста до голови, хвилястий; товщина — тонкий хвіст, товста середина, шия
      const spine = [];
      for (let k = 0; k <= 12; k++) {
        const t = k / 12, x = -36 + t * 62, y = S(p.wave * PI2 + t * 5.5 + (p.flap || 0)) * 3.2;
        spine.push([x, y, 2.2 + 11 * Math.pow(Math.sin(Math.PI * (0.03 + t * 0.89)), 0.75)]);
      }
      const top = spine.map(([x, y, r]) => [x, y - r]), bottom = spine.map(([x, y, r]) => [x, y + r * 0.82]).reverse();
      const skin = g.createLinearGradient(0, -14, 0, 12);
      skin.addColorStop(0, "#3c4a30"); skin.addColorStop(0.3, "#1a2214"); skin.addColorStop(0.75, "#0d110a"); skin.addColorStop(1, "#241f16");
      g.beginPath(); top.concat(bottom).forEach((q, i) => i ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1])); g.closePath();
      g.lineJoin = "round"; g.strokeStyle = INK; g.lineWidth = 2.5; g.stroke(); g.fillStyle = skin; g.fill();
      // перетяжки кілець — темні дуги впоперек, і глянець по спині
      g.strokeStyle = "rgba(4,6,3,0.55)"; g.lineWidth = 1.4;
      for (let k = 2; k <= 10; k += 1) {
        const [x, y, r] = spine[k];
        g.beginPath(); g.moveTo(x - 1, y - r * 0.92); g.quadraticCurveTo(x + 2.2, y, x - 0.5, y + r * 0.76); g.stroke();
      }
      g.strokeStyle = "rgba(170,200,130,0.38)"; g.lineWidth = 1.8; g.lineCap = "round";
      g.beginPath(); spine.slice(2, 11).forEach(([x, y, r], i) => i ? g.lineTo(x, y - r * 0.62) : g.moveTo(x, y - r * 0.62)); g.stroke();
      // паща-присосок: темне коло, всередині кільце блідих зубів; відкривається з jaw
      const [hx, hy, hr] = spine[12];
      g.save(); g.translate(hx + 3, hy); g.rotate(R(p.head));
      const jaw = p.jaw || 0, mouth = 5 + jaw * 2.5;
      g.fillStyle = "#1a1f14"; g.strokeStyle = INK; g.lineWidth = 2;
      g.beginPath(); g.ellipse(2, 0, 6, hr * 0.95, 0, 0, PI2); g.stroke(); g.fill();
      g.fillStyle = "#070806"; g.beginPath(); g.ellipse(4, 0, mouth * 0.55, mouth, 0, 0, PI2); g.fill();
      g.fillStyle = "#d6cbb0";
      for (let i = 0; i < 9; i++) {
        const a = i / 9 * PI2, ax = 4 + Math.cos(a) * mouth * 0.55, ay = Math.sin(a) * mouth;
        g.beginPath(); g.moveTo(ax, ay); g.lineTo(4 + Math.cos(a + 0.18) * mouth * 0.25, Math.sin(a + 0.18) * mouth * 0.45);
        g.lineTo(4 + Math.cos(a + 0.36) * mouth * 0.55, Math.sin(a + 0.36) * mouth); g.closePath(); g.fill();
      }
      eye(g, -5, -hr * 0.62, 1.7, p.eyes, this.eyes); eye(g, -9, -hr * 0.5, 1.3, p.eyes, this.eyes);
      g.restore();
      wing(1, 0.34);
      g.restore();
    },
    states: A.AshRaven.states
  };

  // ---------- Чумний Олень: ріґ Стража, оленячий череп і роги, гнилий плащ, посох-гак ----------
  const STAG = Object.assign({}, A.GuardianOfTheTomb, {
    w: 300, h: 312, eyes: "#b6ff5a", headgear: "antlers", weapon: "crook", noShield: true, smoke: "150,230,80",
    look: { armor: "#3a3a2c", armorLight: "#5a5a44", armorDark: "#1a1c14", cape: "#2a3a1e", capeDark: "#16200e", crown: "#cfc4a6", blade: "#d8ccb0", crack: "#b6ff5a" }
  });

  Object.assign(A, { MarshCorpseEater: EATER, PlagueDoctor: DOCTOR, MarshLeech: LEECH, PlagueStag: STAG });
})(window);
