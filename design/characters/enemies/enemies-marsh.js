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
  const LEECH = {
    w: 140, h: 110, eyes: "#e8ff8a",
    draw(g, p) {
      const c = { body: "#3a2a26", ring: "#5a443a", belly: "#7a5a46", wing: "rgba(170,200,140,0.45)", vein: "rgba(60,80,40,0.7)" };
      g.save(); g.translate(0, -45); g.rotate(R(p.lean));
      const wing = (sign) => {
        g.save(); g.translate(0, -8); g.rotate(R(-90 + p.wingUp * 70 * sign + p.flap * 35));
        g.fillStyle = c.wing; g.strokeStyle = c.vein; g.lineWidth = 1.5;
        g.beginPath(); g.ellipse(18 + 10 * p.spread, 0, 22 + 10 * p.spread, 8, 0, 0, PI2); g.fill(); g.stroke();
        g.beginPath(); g.moveTo(0, 0); g.lineTo(38, -2); g.stroke();
        g.restore();
      };
      wing(-1);
      // тіло з п'яти кілець, від хвоста до голови, хвилясте
      for (let k = 0; k < 5; k++) {
        const x = -30 + k * 13, y = S(p.wave * PI2 + k * 0.9 + (p.flap || 0)) * 3, r = 9 + Math.min(k, 4 - k) * 2.5;
        g.fillStyle = INK; g.beginPath(); g.ellipse(x, y, r + 2, r * 0.8 + 2, 0, 0, PI2); g.fill();
        g.fillStyle = k % 2 ? c.ring : c.body; g.beginPath(); g.ellipse(x, y, r, r * 0.8, 0, 0, PI2); g.fill();
        g.fillStyle = c.belly; g.fillRect(x - r * 0.6, y + r * 0.3, r * 1.2, 2);
      }
      // голова-присосок: кругла паща із зубцями
      g.save(); g.translate(28, 0); g.rotate(R(p.head));
      g.fillStyle = INK; g.beginPath(); g.arc(0, 0, 11, 0, PI2); g.fill();
      g.fillStyle = c.ring; g.beginPath(); g.arc(0, 0, 9, 0, PI2); g.fill();
      g.fillStyle = "#1a0e0c"; g.beginPath(); g.arc(5, 0, 4 + (p.jaw || 0) * 2, 0, PI2); g.fill();
      for (let i = 0; i < 6; i++) { const a = i * PI2 / 6; stroke(g, 1.2, "#d8c8a8", [[5 + Math.cos(a) * 4, Math.sin(a) * 4], [5 + Math.cos(a) * 6, Math.sin(a) * 6]]); }
      eye(g, -2, -5, 1.8, p.eyes, this.eyes);
      g.restore();
      wing(1);
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
