// Реквізит на підлогах (2026-10-28, користувач: «кардинально інакше, красивіше, менш сиро»; рішено Claude):
// порожня кромка читалась лінією. По верху кожної підлоги — мох і травинки в тоні кімнати, уламки, кістки
// й черепи, свічки з теплими ореолами; густо там, де шум, рідко між ними, і ніколи біля обриву. Усе низьке
// (до пів юніта), темний силует зі світлом кімнати на гранях — фон бою, а не перешкода.
(function (root) {
  "use strict";
  const N = root.PaintNoise;

  function rgba(h, a) {
    const r = parseInt(h.slice(1, 3), 16), g = parseInt(h.slice(3, 5), 16), b = parseInt(h.slice(5, 7), 16);
    return `rgba(${r},${g},${b},${a})`;
  }

  function moss(f, x, y, look, h) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    c.fillStyle = rgba(look.moss || look.lip, 0.9);
    for (let k = 0; k < 3; k++) {
      c.beginPath(); c.ellipse(f.x(x + (k - 1) * 0.14), ay + 1, S * (0.16 + h * 0.12), S * (0.06 + h * 0.05), 0, Math.PI, 0); c.fill();
    }
    c.strokeStyle = rgba(look.lipLight, 0.8); c.lineWidth = Math.max(1, S * 0.02); c.lineCap = "round";
    for (let k = 0; k < 5; k++) {
      const bx = f.x(x + (k - 2) * 0.07), lean = (k - 2) * S * 0.03, hh = S * (0.12 + h * 0.2) * (0.6 + (k % 2) * 0.5);
      c.beginPath(); c.moveTo(bx, ay); c.quadraticCurveTo(bx + lean * 0.4, ay - hh * 0.6, bx + lean, ay - hh); c.stroke();
    }
  }

  function rubble(f, x, y, look, h, seed) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    for (let k = 0; k < 3; k++) {
      const w = S * (0.08 + N.hash(seed, k, 3) * 0.12), hh = w * (0.6 + N.hash(seed, k, 5) * 0.4);
      const cx = f.x(x + (k - 1) * 0.16 + (N.hash(seed, k, 7) - 0.5) * 0.08);
      c.fillStyle = rgba(look.shadowTint || look.lipDark, 1);
      c.beginPath(); c.ellipse(cx, ay - hh * 0.5, w, hh, (N.hash(seed, k, 9) - 0.5) * 0.6, 0, Math.PI * 2); c.fill();
      c.fillStyle = rgba(look.lipLight, 0.55);
      c.beginPath(); c.ellipse(cx - w * 0.25, ay - hh * 0.8, w * 0.5, hh * 0.3, -0.3, 0, Math.PI * 2); c.fill();
    }
  }

  function skull(f, cx, ay, s, look) {
    const c = f.c;
    c.fillStyle = rgba(look.lipLight, 0.9);
    c.beginPath(); c.ellipse(cx, ay - s * 0.55, s * 0.5, s * 0.45, 0, 0, Math.PI * 2); c.fill();
    c.fillRect(cx - s * 0.28, ay - s * 0.3, s * 0.56, s * 0.3);
    c.fillStyle = rgba(look.shadowTint || "#000000", 1);
    c.beginPath(); c.ellipse(cx - s * 0.18, ay - s * 0.55, s * 0.12, s * 0.13, 0, 0, Math.PI * 2); c.fill();
    c.beginPath(); c.ellipse(cx + s * 0.18, ay - s * 0.55, s * 0.12, s * 0.13, 0, 0, Math.PI * 2); c.fill();
    c.fillRect(cx - s * 0.03, ay - s * 0.42, s * 0.06, s * 0.1);
  }

  function bones(f, x, y, look, h, seed) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    c.strokeStyle = rgba(look.lipLight, 0.75); c.lineWidth = Math.max(1.5, S * 0.035); c.lineCap = "round";
    for (let k = 0; k < 3; k++) {
      const a = (N.hash(seed, k, 11) - 0.5) * 0.8, len = S * (0.2 + N.hash(seed, k, 13) * 0.15), cx = f.x(x + (k - 1) * 0.15);
      c.beginPath(); c.moveTo(cx - Math.cos(a) * len / 2, ay - S * 0.03 - Math.sin(a) * len / 2);
      c.lineTo(cx + Math.cos(a) * len / 2, ay - S * 0.03 + Math.sin(a) * len / 2); c.stroke();
    }
    skull(f, f.x(x + 0.28), ay, S * (0.3 + h * 0.1), look);
    if (h > 0.6) skull(f, f.x(x - 0.12), ay - S * 0.16, S * 0.24, look);
  }

  function candles(f, x, y, look, h, seed) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    const n = 1 + Math.floor(N.hash(seed, 1, 17) * 3);
    for (let k = 0; k < n; k++) {
      const cx = f.x(x + (k - (n - 1) / 2) * 0.13), hh = S * (0.2 + N.hash(seed, k, 19) * 0.25), w = S * 0.065;
      // ореол
      const g = c.createRadialGradient(cx, ay - hh - S * 0.05, 1, cx, ay - hh - S * 0.05, S * 0.7);
      g.addColorStop(0, "rgba(255,200,120,0.32)"); g.addColorStop(1, "rgba(255,170,90,0)");
      c.fillStyle = g; c.beginPath(); c.arc(cx, ay - hh, S * 0.7, 0, Math.PI * 2); c.fill();
      // віск і потік
      c.fillStyle = "#cdbfa2"; c.fillRect(cx - w, ay - hh, w * 2, hh);
      c.fillStyle = "#a8987a"; c.fillRect(cx - w, ay - hh * 0.4, w * 0.7, hh * 0.4);
      // полум'я
      c.fillStyle = "#ffe2a0";
      c.beginPath(); c.moveTo(cx, ay - hh - S * 0.13); c.quadraticCurveTo(cx + S * 0.045, ay - hh - S * 0.04, cx, ay - hh);
      c.quadraticCurveTo(cx - S * 0.045, ay - hh - S * 0.04, cx, ay - hh - S * 0.13); c.fill();
    }
  }

  // Лісові (2026-11-03, решено Claude): папороть віялом листочків і бліді гриби з холодним ореолом.
  function fern(f, x, y, look, h, seed) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    const n = 4 + Math.floor(h * 3);
    c.strokeStyle = rgba(look.moss || look.lip, 0.95); c.fillStyle = rgba(look.moss || look.lip, 0.95);
    c.lineWidth = Math.max(1, S * 0.025); c.lineCap = "round";
    for (let k = 0; k < n; k++) {
      const a = -Math.PI / 2 + (k - (n - 1) / 2) * 0.38 + (N.hash(seed, k, 3) - 0.5) * 0.2;
      const len = S * (0.55 + h * 0.45) * (0.7 + N.hash(seed, k, 5) * 0.4);
      const bx = f.x(x), ex = bx + Math.cos(a) * len, ey = ay + Math.sin(a) * len;
      const mx = bx + Math.cos(a) * len * 0.5 + (a < -Math.PI / 2 ? -1 : 1) * S * 0.05, my = ay + Math.sin(a) * len * 0.6;
      c.beginPath(); c.moveTo(bx, ay); c.quadraticCurveTo(mx, my, ex, ey + S * 0.06); c.stroke();
      for (let j = 1; j < 6; j++) {
        const t = j / 6, px = bx + (ex - bx) * t, py = ay + (ey + S * 0.06 - ay) * t - Math.sin(t * Math.PI) * S * 0.04, l = S * 0.09 * (1 - t * 0.6);
        c.beginPath(); c.ellipse(px, py, l, l * 0.35, a + 1.2, 0, Math.PI * 2); c.fill();
        c.beginPath(); c.ellipse(px, py, l, l * 0.35, a - 1.2, 0, Math.PI * 2); c.fill();
      }
    }
  }

  function mushrooms(f, x, y, look, h, seed) {
    const c = f.c, S = f.ppu, ay = f.y(y);
    const n = 1 + Math.floor(N.hash(seed, 2, 29) * 3);
    for (let k = 0; k < n; k++) {
      const cx = f.x(x + (k - (n - 1) / 2) * 0.16), hh = S * (0.18 + N.hash(seed, k, 31) * 0.26), w = S * (0.12 + N.hash(seed, k, 33) * 0.1);
      const g = c.createRadialGradient(cx, ay - hh, 1, cx, ay - hh, S * 0.55);
      g.addColorStop(0, rgba(look.fungusGlow || "#6fd0c4", 0.28)); g.addColorStop(1, rgba(look.fungusGlow || "#6fd0c4", 0));
      c.fillStyle = g; c.beginPath(); c.arc(cx, ay - hh, S * 0.55, 0, Math.PI * 2); c.fill();
      c.fillStyle = rgba(look.lipLight, 0.8); c.fillRect(cx - w * 0.18, ay - hh, w * 0.36, hh);
      c.fillStyle = rgba(look.fungus || "#a6dccd", 0.95);
      c.beginPath(); c.ellipse(cx, ay - hh, w, w * 0.55, 0, Math.PI, 0); c.fill();
      c.fillStyle = rgba(look.shadowTint || "#000000", 0.35);
      c.fillRect(cx - w, ay - hh, w * 2, Math.max(1, S * 0.02));
    }
  }

  const SETS = {
    tomb: [["moss", 4], ["rubble", 3], ["bones", 2], ["candles", 1.2]],
    forest: [["moss", 5], ["fern", 3], ["mushrooms", 1.5], ["rubble", 1.5]],
    village: [["moss", 4], ["rubble", 3]],
    capital: [["rubble", 4], ["candles", 1], ["moss", 1]]
  };

  function pick(set, r) {
    let total = 0;
    for (const [, w] of set) total += w;
    let x = r * total;
    for (const [k, w] of set) { x -= w; if (x <= 0) return k; }
    return set[0][0];
  }

  function dress(f, zone, x0, x1, y, look, seed) {
    const set = SETS[look.style || "tomb"];
    if (!set || x1 - x0 < 1.2) return;
    for (let x = x0 + 0.5; x < x1 - 0.5; x += 0.55 + N.hash(Math.round(x * 7), Math.round(y * 3), seed) * 1.6) {
      const dense = N.fbm(x * 0.35, y * 0.2, seed + 3, 2);
      if (dense < 0.47) continue;
      const h = N.hash(Math.round(x * 11), Math.round(y * 5), seed + 5);
      const what = pick(set, N.hash(Math.round(x * 13), Math.round(y * 7), seed + 7));
      if (what === "moss") moss(f, x, y, look, h);
      else if (what === "rubble") rubble(f, x, y, look, h, seed + Math.round(x * 17));
      else if (what === "fern") fern(f, x, y, look, h, seed + Math.round(x * 29));
      else if (what === "mushrooms") mushrooms(f, x, y, look, h, seed + Math.round(x * 31));
      else if (what === "bones") bones(f, x, y, look, h, seed + Math.round(x * 19));
      else candles(f, x, y, look, h, seed + Math.round(x * 23));
    }
  }

  root.LevelProps = { dress };
})(window);
