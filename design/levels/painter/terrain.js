// Камінь ігрової площини одного шматка зони (painter рівнів, 2026-10-04; погоджений напрям —
// design/levels/tomb-look). Форма — з клітинок тексту: м'яке поле, розмите й зсунуте шумом, тож
// стіни й стелі виходять органічними; там, де стоять (верх каменю під повітрям, уступи «=»), край
// рівно по клітинці — малюнок не бреше про колайдер. Серцевина темна, край світліший з кладкою,
// грані до світла — холодний відблиск, верх — світлий кант.
(function (root) {
  "use strict";
  const T = root.LevelText, N = root.PaintNoise;

  function hexRgb(h) { return [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)]; }
  function rgba(h, a) { const [r, g, b] = hexRgb(h); return `rgba(${r},${g},${b},${a})`; }
  const smoothstep = (a, b, x) => { const t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

  // Розмиття Гаусса окремо по осях; values — Float32Array w*h.
  function blur(values, w, h, sigma) {
    const radius = Math.ceil(sigma * 3), kernel = [];
    let sum = 0;
    for (let i = -radius; i <= radius; i++) { const k = Math.exp(-(i * i) / (2 * sigma * sigma)); kernel.push(k); sum += k; }
    for (let i = 0; i < kernel.length; i++) kernel[i] /= sum;
    const tmp = new Float32Array(w * h), out = new Float32Array(w * h);
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      let s = 0;
      for (let i = -radius; i <= radius; i++) { const xx = Math.min(w - 1, Math.max(0, x + i)); s += values[y * w + xx] * kernel[i + radius]; }
      tmp[y * w + x] = s;
    }
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      let s = 0;
      for (let i = -radius; i <= radius; i++) { const yy = Math.min(h - 1, Math.max(0, y + i)); s += tmp[yy * w + x] * kernel[i + radius]; }
      out[y * w + x] = s;
    }
    return out;
  }

  function sample(arr, w, h, u, v) {
    // u, v — у координатах центрів клітинок масиву
    const x0 = Math.max(0, Math.min(w - 2, Math.floor(u))), y0 = Math.max(0, Math.min(h - 2, Math.floor(v)));
    const fx = Math.max(0, Math.min(1, u - x0)), fy = Math.max(0, Math.min(1, v - y0));
    const a = arr[y0 * w + x0], b = arr[y0 * w + x0 + 1], c = arr[(y0 + 1) * w + x0], d = arr[(y0 + 1) * w + x0 + 1];
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy;
  }

  /**
   * Малює шматок [wx0, wy0] … [wx0 + sx, wy0 + sy] у світових юнітах на canvas (ppu пікселів на юніт).
   * look — кольори зони.
   */
  function paintChunk(canvas, zone, wx0, wy0, sx, sy, ppu, look, seed) {
    const c = canvas.getContext("2d");
    const W = canvas.width, H = canvas.height, cell = zone.cell;
    const M = 12; // запас клітинок довкола для розмиття
    const cx0 = Math.floor((wx0 - zone.origin[0]) / cell) - M, cy0 = Math.floor((wy0 - zone.origin[1]) / cell) - M;
    const gw = Math.ceil(sx / cell) + 2 * M + 1, gh = Math.ceil(sy / cell) + 2 * M + 1;
    const rock = new Float32Array(gw * gh), air = new Uint8Array(gw * gh);
    for (let y = 0; y < gh; y++) for (let x = 0; x < gw; x++) {
      const ch = T.at(zone, cx0 + x, cy0 + y);
      rock[y * gw + x] = T.isRock(ch) ? 1 : 0;
      air[y * gw + x] = T.isAir(ch) ? 1 : 0;
    }
    const soft = blur(rock, gw, gh, 0.85), deep = blur(rock, gw, gh, 3.2);
    const rockAt = (x, y) => (x < 0 || y < 0 || x >= gw || y >= gh) ? 1 : rock[y * gw + x];
    const airAt = (x, y) => (x < 0 || y < 0 || x >= gw || y >= gh) ? 0 : air[y * gw + x];

    const img = c.createImageData(W, H), px = img.data;
    const core = hexRgb(look.core), band = hexRgb(look.stone), rim = hexRgb(look.rim);
    const amp = 0.42; // ерозія в клітинках
    for (let j = 0; j < H; j++) {
      const wy = wy0 + sy * (1 - (j + 0.5) / H);
      for (let i = 0; i < W; i++) {
        const wx = wx0 + sx * (i + 0.5) / W;
        const fx = (wx - zone.origin[0]) / cell - cx0, fy = (wy - zone.origin[1]) / cell - cy0;
        const ix = Math.floor(fx), iy = Math.floor(fy);
        // Поза межами зони — сусідня зона: її не зафарбовувати.
        const gcx = (wx - zone.origin[0]) / cell, gcy = (wy - zone.origin[1]) / cell;
        if (gcx < zone.bounds.minX || gcx >= zone.bounds.maxX || gcy < zone.bounds.minY || gcy >= zone.bounds.maxY) continue;

        // Біля поверхні, де стоять, форма — рівно по клітинці.
        let guard = 0;
        for (let k = -2; k <= 1; k++) {
          const yy = iy + k;
          if (rockAt(ix, yy) > 0.5 && airAt(ix, yy + 1)) {
            const d = Math.abs(fy - (yy + 1));
            guard = Math.max(guard, 1 - smoothstep(0.5, 1.7, d));
          }
        }
        const free = 1 - guard;
        const nx = (N.fbm(wx * 0.9, wy * 0.9, seed, 4) - 0.5) * 2, ny = (N.fbm(wx * 0.9, wy * 0.9, seed + 7, 4) - 0.5) * 2;
        const su = fx - 0.5 + nx * amp * free, sv = fy - 0.5 + ny * amp * free;
        let s = sample(soft, gw, gh, su, sv);
        s += (N.fbm(wx * 4.5, wy * 4.5, seed + 3, 2) - 0.5) * 0.16 * free;
        const sharp = rockAt(ix, iy);
        s = s * free + sharp * guard;
        if (s <= 0.47) continue;
        const alpha = Math.min(1, (s - 0.47) / 0.06);

        // Глибина маси: край світліший, серцевина темна.
        const d = sample(deep, gw, gh, fx - 0.5, fy - 0.5);
        const depth = smoothstep(0.5, 0.86, d);

        // Кладка: ряди ~0,95 юніта, камені різної ширини, шви темні.
        const rowF = (wy + N.hash(Math.floor(wy / 7), 0, seed) * 0.4) / 0.95, row = Math.floor(rowF);
        const off = N.hash(row, 1, seed) * 3;
        const bw = 1.3 + N.hash(row, 2, seed) * 1.2;
        const colF = (wx + off) / bw, col = Math.floor(colF);
        const seamY = Math.min(rowF - row, 1 - (rowF - row)) * 0.95, seamX = Math.min(colF - col, 1 - (colF - col)) * bw;
        const seam = Math.min(seamY, seamX);
        const tone = 0.88 + N.hash(col, row, seed + 11) * 0.22 + (N.fbm(wx * 2.2, wy * 2.2, seed + 13, 3) - 0.5) * 0.28;
        const seamDark = seam < 0.04 ? 0.62 : seam < 0.08 ? 0.85 : 1;
        // світліший верхній край кожного каменю
        const bevel = (1 - (rowF - row)) < 0.08 ? 1.12 : 1;

        let r = band[0] * tone * seamDark * bevel, g = band[1] * tone * seamDark * bevel, b = band[2] * tone * seamDark * bevel;
        r = r + (core[0] - r) * depth; g = g + (core[1] - g) * depth; b = b + (core[2] - b) * depth;

        // Відблиск на гранях, повернутих угору й трохи вбік.
        const e = 0.6;
        const gx = sample(soft, gw, gh, su + e, sv) - sample(soft, gw, gh, su - e, sv);
        const gy = sample(soft, gw, gh, su, sv + e) - sample(soft, gw, gh, su, sv - e);
        const glen = Math.hypot(gx, gy);
        if (glen > 0.02) {
          const ox = -gx / glen, oy = -gy / glen; // назовні з каменю
          const facing = Math.max(0, oy * 0.9 + ox * 0.25);
          const near = smoothstep(0.78, 0.5, s);
          const lit = facing * near * 0.55;
          r += (rim[0] - r) * lit; g += (rim[1] - g) * lit; b += (rim[2] - b) * lit;
          // знизу — темніше (тінь під навісом)
          const under = Math.max(0, -oy) * near * 0.35;
          r *= 1 - under; g *= 1 - under; b *= 1 - under;
        }

        const o = (j * W + i) * 4;
        px[o] = r; px[o + 1] = g; px[o + 2] = b; px[o + 3] = 255 * alpha;
      }
    }
    c.putImageData(img, 0, 0);

    const frame = {
      c, ppu,
      x: wx => (wx - wx0) * ppu,
      y: wy => H - (wy - wy0) * ppu
    };
    drawTops(frame, zone, cx0, cy0, gw, gh, look, seed);
    drawLedges(frame, zone, cx0, cy0, gw, gh, look, seed);
  }

  // Кант на верху каменю під повітрям: світла лінія, тепліша плита й пил — «тут можна стояти».
  function drawTops(f, zone, cx0, cy0, gw, gh, look, seed) {
    for (let y = 0; y < gh; y++) {
      let start = -1;
      for (let x = 0; x <= gw; x++) {
        const here = x < gw && T.isRock(T.at(zone, cx0 + x, cy0 + y)) && T.isAir(T.at(zone, cx0 + x, cy0 + y + 1));
        if (here && start < 0) start = x;
        if (!here && start >= 0) {
          if (x - start >= 2) {
            const [wx0, wy] = T.world(zone, cx0 + start, cy0 + y + 1), [wx1] = T.world(zone, cx0 + x, 0);
            lip(f, wx0, wx1, wy, look, seed);
          }
          start = -1;
        }
      }
    }
  }

  function lip(f, x0, x1, y, look, seed) {
    const c = f.c, S = f.ppu, N2 = root.PaintNoise;
    const ax = f.x(x0), bx = f.x(x1), ay = f.y(y);
    const g = c.createLinearGradient(0, ay, 0, ay + S * 0.34);
    g.addColorStop(0, rgba(look.lip, 0.9)); g.addColorStop(1, rgba(look.lipDark, 0));
    c.fillStyle = g;
    c.beginPath(); c.moveTo(ax, ay);
    for (let x = x0; x <= x1 + 0.001; x += 0.1) c.lineTo(f.x(x), ay + S * (0.2 + (N2.fbm(x * 2, y, seed + 21, 2) - 0.5) * 0.14));
    c.lineTo(bx, ay); c.closePath(); c.fill();
    c.strokeStyle = rgba(look.lipLight, 0.8);
    c.lineWidth = Math.max(1.5, S * 0.045);
    c.beginPath(); c.moveTo(ax + 1, ay + 1); c.lineTo(bx - 1, ay + 1); c.stroke();
    // мох (Ліс): м'які подушки на канті, подекуди звисає за край
    if (look.moss) {
      for (let x = x0 + 0.1; x < x1 - 0.1; x += 0.3) {
        const h = N2.hash(Math.round(x * 17), Math.round(y * 17), seed + 29);
        if (h < 0.35) continue;
        c.fillStyle = rgba(look.moss, 0.6 + h * 0.35);
        c.beginPath(); c.ellipse(f.x(x), ay + 1, S * (0.2 + h * 0.35), S * (0.06 + h * 0.08), 0, Math.PI, 0); c.fill();
        if (h > 0.85) {
          const drop = S * (0.2 + (h - 0.85) * 4);
          c.strokeStyle = rgba(look.moss, 0.5); c.lineWidth = Math.max(1, S * 0.035); c.lineCap = "round";
          c.beginPath(); c.moveTo(f.x(x), ay + S * 0.05); c.quadraticCurveTo(f.x(x + 0.08), ay + drop * 0.6, f.x(x - 0.02), ay + drop); c.stroke();
        }
      }
    }
    // пил і дрібні уламки
    for (let x = x0 + 0.15; x < x1 - 0.15; x += 0.25) {
      const h = N2.hash(Math.round(x * 20), Math.round(y * 20), seed + 23);
      if (h > 0.62) {
        c.fillStyle = rgba(look.lipDark, 0.95);
        c.beginPath(); c.ellipse(f.x(x), ay - S * 0.03, S * (0.05 + h * 0.08), S * (0.03 + h * 0.035), 0, Math.PI, 0); c.fill();
      }
    }
  }

  // Уступи «=»: плита з рівним верхом і обламаним низом; вільна — на двох ланцюгах до склепіння.
  function drawLedges(f, zone, cx0, cy0, gw, gh, look, seed) {
    const c = f.c, S = f.ppu, N2 = root.PaintNoise;
    for (let y = 0; y < gh; y++) {
      let start = -1;
      for (let x = 0; x <= gw; x++) {
        const here = x < gw && T.isLedge(T.at(zone, cx0 + x, cy0 + y));
        if (here && start < 0) start = x;
        if (!here && start >= 0) {
          const gx0 = cx0 + start, gx1 = cx0 + x, gy = cy0 + y;
          const [wx0, wyb] = T.world(zone, gx0, gy), [wx1, wyt] = T.world(zone, gx1, gy + 1);
          const s2 = seed + gx0 * 7 + gy * 13;
          // ланцюги — лише в повітрі, до першого каменю вгорі
          const freeLeft = !T.isRock(T.at(zone, gx0 - 1, gy)), freeRight = !T.isRock(T.at(zone, gx1, gy));
          if (freeLeft && freeRight) {
            [gx0, gx1 - 1].forEach((cx, k) => {
              let top = gy + 1;
              while (top < gy + 60 && !T.isRock(T.at(zone, cx, top))) top++;
              const [hx, hy] = T.world(zone, cx, top);
              const chainX = hx + (k === 0 ? 0.3 : 0.2);
              c.strokeStyle = rgba(look.chain, 0.9); c.lineWidth = S * 0.045;
              if (look.hang === "roots") {
                // Ліс: уступ тримають корені, що звисають зі стелі, — хвиляста лінія, не ланцюг
                c.lineWidth = S * 0.09; c.lineCap = "round";
                c.beginPath(); c.moveTo(f.x(chainX), f.y(hy));
                for (let yy = hy; yy > wyt; yy -= 0.4) c.lineTo(f.x(chainX + (N2.fbm(chainX, yy * 0.7, s2, 2) - 0.5) * 0.35), f.y(yy));
                c.lineTo(f.x(chainX), f.y(wyt)); c.stroke();
                return;
              }
              for (let yy = wyt; yy < hy; yy += 0.22) {
                c.beginPath(); c.ellipse(f.x(chainX), f.y(yy + 0.11), S * 0.05, S * 0.1, 0, 0, Math.PI * 2); c.stroke();
              }
            });
          }
          // плита
          c.beginPath();
          c.moveTo(f.x(wx0), f.y(wyt));
          c.lineTo(f.x(wx1), f.y(wyt));
          const bottom = [];
          for (let x = wx1; x >= wx0 - 0.001; x -= 0.12) {
            const u = (x - wx0) / Math.max(0.01, wx1 - wx0);
            const sag = Math.sin(Math.PI * u) * 0.32 + (N2.fbm(x * 2.4, wyb, s2, 3) - 0.5) * 0.3;
            bottom.push([x, wyb - Math.max(0, sag)]);
          }
          bottom.forEach(([x, yy]) => c.lineTo(f.x(x), f.y(yy)));
          c.closePath();
          const g = c.createLinearGradient(0, f.y(wyt), 0, f.y(wyb - 0.4));
          g.addColorStop(0, look.stone); g.addColorStop(1, look.core);
          c.fillStyle = g; c.fill();
          c.save(); c.clip();
          for (let x = wx0 + 0.6 + N2.hash(gx0, gy, s2) * 0.8; x < wx1 - 0.3; x += 1.1 + N2.hash(Math.round(x * 10), gy, s2) * 0.9) {
            c.strokeStyle = "rgba(0,0,0,0.45)"; c.lineWidth = Math.max(1, S * 0.03);
            c.beginPath(); c.moveTo(f.x(x), f.y(wyt)); c.lineTo(f.x(x - 0.05), f.y(wyb - 0.4)); c.stroke();
          }
          c.restore();
          lip(f, wx0, wx1, wyt, look, s2);
          start = -1;
        }
      }
    }
  }

  root.LevelTerrain = { paintChunk };
})(window);
