// Вороги й боси Села й Столиці (2026-11-04, користувач: «роби всю гру поетапно»; решено Claude за постійною згодою).
// Кожен — новий вигляд на ріґу й рухах, що вже є: таблиці кадрів у PlaceholderEnemyArt ті самі (селянин — списника,
// пес — гончака, арбалетник — лучника, жрець — шамана, горгулья й опудало — списника з нерухомим спокоєм; боси —
// Стража), тож удари падають на ті самі частки, що й у заглушок. Ворон — свій маленький ріґ птаха.
(function (root) {
  "use strict";
  const E = root.Enemies, K = E.kit;
  const { S, PI2, R, INK, track, cycle, stroke, limb, shape, eye, slashArc, rags, boot } = K;
  const C = Math.cos;
  const A = E.ACTORS;

  function glow(g, x, y, r, rgb, a) {
    if (a <= 0.02) return;
    const gl = g.createRadialGradient(x, y, 0, x, y, r);
    gl.addColorStop(0, `rgba(${rgb},${0.85 * Math.min(1, a)})`); gl.addColorStop(1, `rgba(${rgb},0)`);
    g.fillStyle = gl; g.beginPath(); g.arc(x, y, r, 0, PI2); g.fill();
  }

  // Довга зброя через обидві руки під кутом weapon (як спис списника): що на кінці — tip(g)
  function polearm(g, hx, hy, ang, back, front, shaft, tip) {
    const a = R(ang), b0 = [hx - C(a) * back, hy - S(a) * back], t = [hx + C(a) * front, hy + S(a) * front];
    stroke(g, 4, shaft, [b0, t]);
    g.save(); g.translate(...t); g.rotate(a); tip(g); g.restore();
    return t;
  }

  // ---------- Одержимий селянин: вила, солом'яний капелюх, латана свитка, жар в очах ----------
  const VILLAGER = {
    w: 214, h: 178, hip: 64, thigh: 31, shin: 31, eyes: "#ffb070",
    draw(g, p) {
      const c = { tunic: "#5a4630", tunicDark: "#3a2c1e", patch: "#6e5a3c", skin: "#8a7a66", pants: "#2c2620", hat: "#7a6a40", hatDark: "#4a3e24", shaft: "#4e3e2a", iron: "#8a8a80" };
      const hip = -this.hip;
      const back = limb(g, -5, hip, p.hipB, this.thigh, p.kneeB, this.shin, 11, c.pants);
      boot(g, back.end, "#1e1a16");
      const ba = limb(g, -8, hip - 46, p.armB, 21, p.elbowB, 19, 8, c.skin);
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      shape(g, c.tunic, [[-16, 6], [-19, -38], [-9, -54], [13, -54], [20, -38], [17, 6], [6, 10], [-6, 8]], 4);
      shape(g, c.patch, [[-12, -30], [-4, -32], [-3, -22], [-11, -20]], 2);
      g.fillStyle = c.tunicDark; g.fillRect(-17, -14, 36, 4); // мотузяний пояс
      g.restore();
      const fr = limb(g, 5, hip, p.hipF, this.thigh, p.kneeF, this.shin, 11, c.pants);
      boot(g, fr.end, "#1e1a16");
      // голова: запале обличчя під широким брилем, дим з-під нього, коли одержимість сильніша
      g.save(); g.translate(8 + p.lean * 0.5, hip - 68); g.rotate(R(p.head));
      shape(g, c.skin, [[-9, 12], [-10, -8], [0, -14], [11, -9], [12, 6], [4, 13]], 3);
      g.fillStyle = "rgba(0,0,0,0.45)"; g.fillRect(-4, -6, 16, 6);
      eye(g, 3, -3, 2, p.eyes, this.eyes); eye(g, 9, -3.5, 1.8, p.eyes, this.eyes);
      shape(g, c.hatDark, [[-26, -10], [28, -12], [24, -6], [-22, -4]], 3);
      shape(g, c.hat, [[-12, -10], [-10, -24], [10, -26], [14, -12]], 3);
      glow(g, 6, -30, 14 + p.eyes * 6, "255,140,60", (p.eyes - 0.8) * 0.5);
      g.restore();
      const arm = limb(g, 10, hip - 48, p.arm, 21, p.elbow, 19, 8, c.skin);
      polearm(g, arm.end[0], arm.end[1], p.weapon, 64, 72, c.shaft, (gg) => {
        // вила: перекладина й три загнуті зуби
        stroke(gg, 3, c.iron, [[0, -10], [0, 10]]);
        for (const dy of [-9, 0, 9]) stroke(gg, 2.5, c.iron, [[0, dy], [16, dy * 0.8], [24, dy * 0.6 - 2]]);
      });
      stroke(g, 6, c.skin, [ba.end, [arm.end[0] - C(R(p.weapon)) * 28, arm.end[1] - S(R(p.weapon)) * 28]]);
      slashArc(g, 10, hip - 46, 80, p.slashFrom, p.slashTo, p.slash);
    },
    states: A.RoyalSpearman.states
  };

  // ---------- Опудало: мішок-голова із зашитим ротом, солома, коса ----------
  const statueIdle = (pose) => n => cycle(n, () => pose);
  const SCARECROW = {
    w: 214, h: 178, hip: 66, thigh: 32, shin: 32, eyes: "#ffcf6a",
    draw(g, p) {
      const c = { coat: "#3e3426", coatDark: "#241e16", sack: "#a08a62", sackDark: "#6e5c3e", straw: "#c9aa5a", leg: "#5a4a32", shaft: "#4a3a26", blade: "#9a9a90" };
      const hip = -this.hip;
      const back = limb(g, -5, hip, p.hipB, this.thigh, p.kneeB, this.shin, 8, c.leg);
      stroke(g, 2, c.straw, [back.end, [back.end[0] - 6, back.end[1] + 2]]);
      const ba = limb(g, -8, hip - 48, p.armB, 22, p.elbowB, 20, 7, c.coatDark);
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      rags(g, -18, -54, 18, 30, 66, p, c.coat, 8);
      for (let i = 0; i < 6; i++) stroke(g, 1.5, c.straw, [[-14 + i * 6, -50], [-16 + i * 6 + S(p.wave * PI2 + i) * 3, -62]]);
      g.fillStyle = c.coatDark; g.fillRect(-2, -50, 4, 40);
      g.restore();
      const fr = limb(g, 5, hip, p.hipF, this.thigh, p.kneeF, this.shin, 8, c.leg);
      stroke(g, 2, c.straw, [fr.end, [fr.end[0] + 6, fr.end[1] + 2]]);
      g.save(); g.translate(8 + p.lean * 0.5, hip - 72); g.rotate(R(p.head));
      shape(g, c.sack, [[-14, 12], [-16, -10], [-6, -20], [10, -20], [16, -8], [14, 12], [0, 16]], 4);
      stroke(g, 1.5, c.sackDark, [[-8, 6], [-4, 8], [0, 6], [4, 8], [8, 6]]);
      stroke(g, 2, c.sackDark, [[-6, -12], [-2, -8]]); stroke(g, 2, c.sackDark, [[-2, -12], [-6, -8]]);
      eye(g, 7, -9, 2.4, p.eyes, this.eyes);
      shape(g, c.coatDark, [[-22, -16], [22, -18], [12, -24], [-12, -24]], 3); // обвислий бриль
      g.restore();
      const arm = limb(g, 10, hip - 50, p.arm, 22, p.elbow, 20, 7, c.coatDark);
      polearm(g, arm.end[0], arm.end[1], p.weapon, 60, 70, c.shaft, (gg) => {
        // коса: лезо вниз від верхівки, загнуте назад
        shape(gg, c.blade, [[0, -3], [6, -2], [4, 30], [-10, 52], [-6, 30], [-2, 4]], 3);
      });
      stroke(g, 5, c.coatDark, [ba.end, [arm.end[0] - C(R(p.weapon)) * 28, arm.end[1] - S(R(p.weapon)) * 28]]);
      slashArc(g, 10, hip - 46, 86, p.slashFrom, p.slashTo, p.slash);
    },
    states: Object.assign({}, A.RoyalSpearman.states, {
      // спить на жердині: руки розкинуті, голова на грудях
      Idle: statueIdle({ arm: 90, elbow: 0, armB: -80, elbowB: 0, weapon: 100, head: 24, eyes: 0.15, lean: 0 })
    })
  };

  // ---------- Кам'яна горгулья: роги, складені крила, пазурі; у спокої — статуя ----------
  const GARGOYLE = {
    w: 220, h: 186, hip: 64, thigh: 30, shin: 30, eyes: "#9ad0ff",
    draw(g, p) {
      const c = { stone: "#4a4644", stoneLight: "#7a746e", stoneDark: "#26231f", wing: "#363230", claw: "#c8c0b0" };
      const hip = -this.hip;
      // крила позаду: розкриваються, коли прокинулась (eyes)
      const open = Math.min(1, Math.max(0, (p.eyes - 0.3) * 0.8));
      g.save(); g.translate(-10, hip - 52);
      shape(g, c.wing, [[0, 0], [-30 - open * 30, -30 - open * 20], [-46 - open * 30, 10], [-30, 30], [-16, 22]], 4);
      g.restore();
      const back = limb(g, -6, hip, p.hipB, this.thigh, p.kneeB, this.shin, 15, c.stoneDark);
      boot(g, back.end, c.stoneDark);
      const ba = limb(g, -10, hip - 46, p.armB, 22, p.elbowB, 20, 11, c.stoneDark);
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      shape(g, c.stone, [[-22, 6], [-26, -36], [-12, -58], [16, -58], [26, -36], [20, 6]], 5);
      g.strokeStyle = "rgba(0,0,0,0.35)"; g.lineWidth = 2;
      g.beginPath(); g.moveTo(-14, -40); g.lineTo(-4, -30); g.lineTo(-10, -18); g.stroke();
      g.fillStyle = c.stoneLight; g.fillRect(-18, -54, 30, 5);
      g.restore();
      const fr = limb(g, 6, hip, p.hipF, this.thigh, p.kneeF, this.shin, 15, c.stone);
      boot(g, fr.end, c.stoneDark);
      g.save(); g.translate(10 + p.lean * 0.5, hip - 70); g.rotate(R(p.head));
      shape(g, c.stone, [[-14, 12], [-16, -10], [-4, -18], [14, -14], [20, 0], [14, 12]], 4);
      shape(g, c.stoneDark, [[-10, -12], [-22, -32], [-4, -16]], 3); shape(g, c.stoneDark, [[6, -16], [10, -36], [14, -14]], 3);
      g.fillStyle = "#050505"; g.fillRect(2, -6, 16, 5);
      eye(g, 6, -4, 2.2, p.eyes, this.eyes); eye(g, 13, -4, 2, p.eyes, this.eyes);
      shape(g, c.stoneDark, [[8, 6], [22, 4], [16, 12]], 2); // пащека
      g.restore();
      const arm = limb(g, 12, hip - 48, p.arm, 22, p.elbow, 20, 11, c.stone);
      // пазурі там, де в списника спис
      g.save(); g.translate(arm.end[0], arm.end[1]); g.rotate(R(p.weapon));
      for (const dy of [-6, 0, 6]) shape(g, c.claw, [[0, dy - 3], [20, dy - 1], [24, dy + 3], [0, dy + 3]], 2);
      g.restore();
      slashArc(g, 10, hip - 46, 70, p.slashFrom, p.slashTo, p.slash);
    },
    states: Object.assign({}, A.RoyalSpearman.states, {
      // статуя на постаменті: присіла, голова донизу, очі згаслі — жодного руху
      Idle: statueIdle({ lean: 26, y: 8, hipF: 40, kneeF: 70, hipB: -10, kneeB: 80, arm: 40, elbow: 60, weapon: 90, head: 22, eyes: 0 })
    })
  };

  // ---------- Королівський арбалетник: шолом-капелюх, багряна табарда, арбалет ----------
  const CROSSBOW = {
    w: 160, h: 176, hip: 62, thigh: 30, shin: 30, eyes: "#ffd27a",
    draw(g, p) {
      const c = { armor: "#4a4442", armorLight: "#7a726c", armorDark: "#242120", tabard: "#7a1c1c", tabardDark: "#4a1010", wood: "#4e3a26", iron: "#9a948a" };
      const hip = -this.hip;
      const back = limb(g, -4, hip, p.hipB, this.thigh, p.kneeB, this.shin, 11, c.armorDark);
      boot(g, back.end, c.armorDark);
      g.save(); g.translate(0, hip); g.rotate(R(p.lean * 0.4));
      shape(g, c.armor, [[-15, 4], [-18, -38], [-9, -52], [13, -52], [19, -38], [15, 4]], 4);
      rags(g, -11, -44, 11, 22, 52, p, c.tabard, 6);
      g.fillStyle = "#d9b45a"; g.beginPath(); g.moveTo(-5, -34); g.lineTo(0, -40); g.lineTo(5, -34); g.lineTo(0, -28); g.closePath(); g.fill();
      g.restore();
      const fr = limb(g, 4, hip, p.hipF, this.thigh, p.kneeF, this.shin, 11, c.armor);
      boot(g, fr.end, c.armorDark);
      g.save(); g.translate(7 + p.lean * 0.5, hip - 64); g.rotate(R(p.head));
      shape(g, c.armor, [[-10, 12], [-10, -6], [12, -6], [12, 12]], 3);
      g.fillStyle = "#050505"; g.fillRect(-2, -1, 14, 4);
      eye(g, 3, 1, 1.8, p.eyes, this.eyes); eye(g, 9, 1, 1.6, p.eyes, this.eyes);
      shape(g, c.armorLight, [[-20, -4], [22, -4], [14, -10], [8, -20], [-6, -20], [-12, -10]], 3); // капелін
      g.restore();
      // арбалет: ложе вздовж руки, плечі дуги поперек, тятива відтягнута (draw)
      const shoulder = [4 + p.lean * 0.4, hip - 46];
      const arm = limb(g, shoulder[0], shoulder[1], p.arm, 20, p.elbow, 18, 8, c.armor);
      const a = R(p.weapon - 90);
      g.save(); g.translate(arm.end[0], arm.end[1]); g.rotate(a);
      shape(g, c.wood, [[-26, -4], [24, -3], [24, 3], [-26, 5]], 3);
      for (const s of [-1, 1]) stroke(g, 3, c.iron, [[18, 0], [14, s * 22], [8, s * 26]]);
      const pull = 18 - p.draw * 22;
      g.strokeStyle = "#d8d0c0"; g.lineWidth = 1.2; g.beginPath(); g.moveTo(8, -26); g.lineTo(pull, 0); g.lineTo(8, 26); g.stroke();
      if (p.arrow > 0.5) stroke(g, 2, "#2a2018", [[pull, 0], [34, 0]]);
      g.restore();
      limb(g, shoulder[0] - 6, shoulder[1] + 2, p.arm + 10, 18, p.elbow + 20, 16, 7, c.armorDark);
    },
    states: A.ForestArcher.states
  };

  // ---------- Палацовий жрець: бліда ряса до землі, золота маска, позолочений посох із сонцем ----------
  const PRIEST = {
    w: 156, h: 178, eyes: "#ffe6a0",
    draw(g, p) {
      const c = { robe: "#bab2a2", robeDark: "#7a7266", trim: "#b8923a", mask: "#d9b45a", staff: "#8a6a2a" };
      g.save(); g.translate(0, -2); g.rotate(R(p.lean * 0.3));
      // ряса до самої землі, поділ хвилюється
      const hem = [];
      for (let i = 0; i <= 8; i++) hem.push([-24 + i * 6 + S(p.wave * PI2 + i) * 2, -(i % 2) * 4 + p.roots * 2]);
      shape(g, c.robe, [[-14, -112], [16, -112], [22, -60], [26, -2], ...hem.reverse(), [-24, -2], [-20, -60]], 4);
      stroke(g, 3, c.trim, [[0, -104], [2, -60], [4, -4]]);
      g.fillStyle = "rgba(0,0,0,0.18)"; g.fillRect(-20, -60, 10, 56);
      limb(g, -8, -100, p.armB || -10, 20, p.elbowB || -10, 18, 7, c.robeDark);
      g.restore();
      g.save(); g.translate(8 + p.lean * 0.5, -122); g.rotate(R(p.head));
      shape(g, c.robe, [[-16, 16], [-18, -6], [-4, -24], [14, -18], [18, 0], [14, 16]], 4);
      g.fillStyle = "#08070a"; g.beginPath(); g.ellipse(5, -1, 9, 10, 0, 0, PI2); g.fill();
      shape(g, c.mask, [[-2, -8], [12, -8], [12, 4], [5, 10], [-2, 4]], 2);
      eye(g, 2, -3, 1.6, p.eyes, this.eyes); eye(g, 9, -3, 1.6, p.eyes, this.eyes);
      g.restore();
      const arm = limb(g, 12 + p.lean * 0.4, -100, p.arm, 22, p.elbow, 20, 7, c.robe);
      g.save(); g.translate(arm.end[0], arm.end[1]); g.rotate(R(p.weapon - 90));
      stroke(g, 4, c.staff, [[0, 40], [2, 0], [0, -62]]);
      glow(g, 0, -74, 18 + p.glow * 24, "255,214,120", 0.45 + p.glow);
      shape(g, c.trim, [[-10, -74], [0, -86], [10, -74], [0, -62]], 2);
      g.fillStyle = "#fff0b0"; g.beginPath(); g.arc(0, -74, 4 + p.glow * 1.5, 0, PI2); g.fill();
      g.restore();
    },
    states: A.RootShaman.states
  };

  // ---------- Попелястий ворон: птах завбільшки з пса; крила назад-угору — і пікірування ----------
  // 2026-11-06 (вирішено Claude за постійною згодою): не мультяшний овал із сірим крилом, а рваний силует із пер —
  // попіл на кінчиках, кістяний дзьоб-маска, червоне око; рухи й таблиця кадрів ті самі.
  const RAVEN = {
    w: 140, h: 110, eyes: "#ff6a4a",
    draw(g, p) {
      const c = { far: "#0d0b0b", near: "#1c1817", ash: "rgba(150,142,132,0.85)", bone: "#c4b8a2", boneDark: "#6a6050" };
      g.save(); g.translate(0, -45); g.rotate(R(p.lean));
      // перо вздовж +x: довгий лист із рваним кінцем; попіл — світліший кінчик
      const feather = (len, wid, col, ash) => {
        g.beginPath(); g.moveTo(0, -wid * 0.35);
        g.quadraticCurveTo(len * 0.55, -wid * 1.05, len, -wid * 0.12);
        g.quadraticCurveTo(len * 0.93, wid * 0.05, len * 0.97, wid * 0.3);
        g.quadraticCurveTo(len * 0.5, wid * 1.0, 0, wid * 0.35); g.closePath();
        g.fillStyle = col; g.fill();
        if (ash) { g.fillStyle = ash; g.beginPath(); g.ellipse(len * 0.84, wid * 0.05, len * 0.12, wid * 0.32, 0, 0, PI2); g.fill(); }
      };
      const wing = (sign, col, ash) => {
        g.save(); g.translate(4, -6); g.rotate(R(-90 + p.wingUp * 80 * sign + p.flap * 40));
        // шість махових віялом (розкриваються зі spread), над ними три покривні
        for (let k = 5; k >= 0; k--) {
          g.save(); g.rotate(R((k - 2.5) * (4 + 7 * p.spread)));
          feather(26 + k * 3.5 + 12 * p.spread, 5.5, col, ash && k % 2 === 0 ? ash : null);
          g.restore();
        }
        for (let k = 0; k < 3; k++) { g.save(); g.rotate(R((k - 1) * 14)); feather(17, 7.5, col, null); g.restore(); }
        g.restore();
      };
      wing(-1, c.far, null);
      // хвіст — віяло з п'яти пер, гойдається з помахом
      for (let k = 0; k < 5; k++) {
        g.save(); g.translate(-15, 3); g.rotate(R(180 + (k - 2) * 9 + S(p.wave * PI2 + k) * 3 - p.flap * 6));
        feather(28 + (k % 2) * 6, 5, c.near, k === 1 || k === 3 ? c.ash : null);
        g.restore();
      }
      // тулуб — рвана крапля з пір'я: спина ловить світло, черево в тіні
      const body = [];
      for (let i = 0; i < 28; i++) {
        const t = i / 28 * PI2, rag = i % 2 ? 1.1 : 0.96;
        body.push([Math.cos(t) * 24 * rag - (Math.cos(t) < -0.6 ? 4 : 0), Math.sin(t) * 13.5 * rag]);
      }
      const tone = g.createLinearGradient(0, -14, 0, 14);
      tone.addColorStop(0, "#2e2826"); tone.addColorStop(0.45, "#181414"); tone.addColorStop(1, "#090808");
      g.beginPath(); body.forEach((q, i) => i ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1])); g.closePath();
      g.lineJoin = "round"; g.strokeStyle = INK; g.lineWidth = 2.5; g.stroke(); g.fillStyle = tone; g.fill();
      // голова з комірцем пір'я, дзьоб-маска з кістки
      g.save(); g.translate(20, -8); g.rotate(R(p.head));
      const ruff = [];
      for (let i = 0; i < 18; i++) { const t = i / 18 * PI2, r = (i % 2 ? 12.5 : 10) * (Math.cos(t) < 0 ? 1.15 : 1); ruff.push([Math.cos(t) * r, Math.sin(t) * r * 0.92]); }
      const head = g.createLinearGradient(0, -12, 0, 12);
      head.addColorStop(0, "#2a2422"); head.addColorStop(1, "#0b0909");
      g.beginPath(); ruff.forEach((q, i) => i ? g.lineTo(q[0], q[1]) : g.moveTo(q[0], q[1])); g.closePath();
      g.strokeStyle = INK; g.lineWidth = 2.5; g.stroke(); g.fillStyle = head; g.fill();
      const jaw = p.jaw || 0, beak = g.createLinearGradient(4, -6, 26, 4);
      beak.addColorStop(0, c.bone); beak.addColorStop(1, c.boneDark);
      g.beginPath(); g.moveTo(3, -6); g.quadraticCurveTo(16, -7.5, 27, 0.5 + jaw * 1.5); g.quadraticCurveTo(16, 1 + jaw * 3, 4, 2 + jaw * 2);
      g.quadraticCurveTo(16, 3 + jaw * 4, 22, 3.5 + jaw * 5); g.quadraticCurveTo(12, 6 + jaw * 3, 3, 4.5); g.closePath();
      g.strokeStyle = INK; g.lineWidth = 2; g.stroke(); g.fillStyle = beak; g.fill();
      eye(g, 3.5, -2.5, 2.2, p.eyes, this.eyes);
      g.restore();
      wing(1, c.near, c.ash);
      if (p.legs > 0) {
        stroke(g, 1.6, "#2a2422", [[-2, 11], [-4, 21], [-1, 23]]); stroke(g, 1.6, "#2a2422", [[6, 11], [7, 21], [10, 23]]);
      }
      g.restore();
    },
    states: {}
  };
  (function () {
    const P = (o) => Object.assign({ wingUp: 0, flap: 0, spread: 0.6, legs: 0 }, o);
    const fly = (speed) => n => cycle(n, t => P({ y: S(t * PI2) * 3, flap: S(t * PI2 * speed), wingUp: 0.4 + S(t * PI2 * speed) * 0.5, spread: 1, head: S(t * PI2) * 4 }));
    Object.assign(RAVEN.states, {
      Idle: n => cycle(n, t => P({ legs: 1, y: 24 + Math.abs(S(t * PI2)) * -2, head: S(t * PI2) * 8, wingUp: 0.05, spread: 0.3 })),
      Patrol: fly(1), Chase: fly(2),
      Alert: n => track(n, [[0, P({ legs: 1, y: 24 })], [1, P({ wingUp: 0.9, spread: 1, head: -16, eyes: 1.6, y: -4 })]]),
      // крила назад і вгору до 0,55 — і склались для пікірування
      AttackLight: n => track(n, [[0, P({ wingUp: 0.5, spread: 1 })], [0.55, P({ wingUp: 1.1, spread: 1, lean: -18, x: -8, head: -10, eyes: 1.8, jaw: 1 })],
        [0.68, P({ wingUp: -0.2, spread: 0.2, lean: 34, x: 22, y: 10, jaw: 1 })], [0.85, P({ wingUp: -0.1, spread: 0.3, lean: 30, x: 24, y: 12 })], [1, P({ wingUp: 0.5, spread: 1 })]]),
      Hurt: n => track(n, [[0, P({ lean: -30, x: -8, eyes: 0.4, wingUp: 1 })], [1, P({ wingUp: 0.5 })]]),
      Stunned: n => cycle(n, t => P({ lean: S(t * PI2) * 30, wingUp: 0.2, flap: S(t * PI2 * 3) * 0.3, eyes: 0.4, y: 6 })),
      Death: n => track(n, [[0, P({ lean: -20, eyes: 0.6, wingUp: 1 })], [0.5, P({ lean: 90, y: 20, wingUp: 0.3, eyes: 0.2 })], [1, P({ lean: 170, y: 44, wingUp: 0, spread: 0.2, eyes: 0, alpha: 0.8 })]])
    });
  })();

  // ---------- Пес на ланцюгу: гончак у буро-сірій шерсті з нашийником ----------
  const WATCHDOG = Object.assign({}, A.CorruptedHound, {
    eyes: "#ffb070", collar: true,
    look: { fur: "#4a3a2c", furDark: "#221a14", moss: "#5a4a38", thorn: "#2a221a", bark: "#3a2c20" }
  });

  // ---------- Боси на ріґу Стража ----------
  const G = A.GuardianOfTheTomb;
  const BELLRINGER = Object.assign({}, G, {
    w: 300, h: 300, eyes: "#ffcf8a", headgear: "hood", weapon: "hammer", back: "bell", noShield: true,
    look: { armor: "#3a2e24", armorLight: "#5a4836", armorDark: "#1e1610", cape: "#4a3020", capeDark: "#2a1a10", crown: "#8a6a3a", blade: "#7a5a30", bell: "#6e522c", crack: "#f0a050" }
  });
  const COMMANDER = Object.assign({}, G, {
    w: 320, h: 300, eyes: "#ff9a5a", headgear: "horns", weapon: "greatsword", noShield: true,
    look: { armor: "#3a3836", armorLight: "#6a6460", armorDark: "#1a1918", cape: "#7a1c18", capeDark: "#4a1010", crown: "#5a5450", blade: "#c0b8b0", crack: "#ff7a3a" }
  });
  const KING = Object.assign({}, G, {
    w: 300, h: 312, eyes: "#ffe0a0", headgear: "crown", weapon: "sword", noShield: true,
    look: { armor: "#3a3050", armorLight: "#6a5a8a", armorDark: "#1c1828", cape: "#4a2a6a", capeDark: "#2a1640", crown: "#e0b84e", blade: "#d0c8c0", crack: "#ffd27a" }
  });

  Object.assign(A, { PossessedVillager: VILLAGER, Scarecrow: SCARECROW, StoneGargoyle: GARGOYLE, RoyalCrossbowman: CROSSBOW,
    PalacePriest: PRIEST, AshRaven: RAVEN, Watchdog: WATCHDOG, Bellringer: BELLRINGER, AshCommander: COMMANDER, OathBoundKing: KING });
})(window);
