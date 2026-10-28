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

  // Бесшовна текстура Midjourney як поле яскравості: середнє 0, розкид 1 (колір дає зона, текстура —
  // лише рельєф). Дані — textures-<зона>.js (tools/midjourney/textures.py): сірі байти в скрипті, бо
  // headless Chrome знімає кадр, не дочекавшись картинок, завантажених асинхронно.
  function loadTexture(data) {
    const bytes = atob(data.b64), n = data.w * data.h, v = new Float32Array(n);
    const cv = document.createElement("canvas");
    cv.width = data.w; cv.height = data.h;
    const c = cv.getContext("2d");
    let mean = 0;
    for (let i = 0; i < n; i++) { v[i] = bytes.charCodeAt(i); mean += v[i]; }
    mean /= n;
    let sd = 0;
    for (let i = 0; i < n; i++) sd += (v[i] - mean) * (v[i] - mean);
    sd = Math.sqrt(sd / n) || 1;
    for (let i = 0; i < n; i++) v[i] = Math.max(-3, Math.min(3, (v[i] - mean) / sd));
    // сірий варіант для накладання «soft-light» (уступи)
    const gray = c.createImageData(cv.width, cv.height);
    for (let i = 0; i < n; i++) {
      const g = 128 + v[i] * 38;
      gray.data[i * 4] = gray.data[i * 4 + 1] = gray.data[i * 4 + 2] = g; gray.data[i * 4 + 3] = 255;
    }
    c.putImageData(gray, 0, 0);
    return { w: cv.width, h: cv.height, v, canvas: cv };
  }

  function loadTextures(zoneName, names) {
    const data = (root.LevelTextureData || {})[zoneName], out = {};
    for (const key of names) out[key] = loadTexture(data[key]);
    return out;
  }

  // Яскравість текстури в точці світу; tile — скільки юнітів займає одна плитка.
  function texAt(t, wx, wy, tile) {
    const u = ((wx / tile) % 1 + 1) % 1 * t.w, v = ((-wy / tile) % 1 + 1) % 1 * t.h;
    const x0 = Math.floor(u), y0 = Math.floor(v), fx = u - x0, fy = v - y0;
    const x1 = (x0 + 1) % t.w, y1 = (y0 + 1) % t.h;
    const a = t.v[y0 * t.w + x0], b = t.v[y0 * t.w + x1], c = t.v[y1 * t.w + x0], d = t.v[y1 * t.w + x1];
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
    const tex = look.tex;
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
        // З текстурою освітлена смуга біля поверхні ширша (2026-10-28, «текстури ті самі»: рельєф жив
        // лише в тонкому краї, а маса була суцільно чорна); серцевина все одно гасне.
        const depth = look.tex ? smoothstep(0.62, 0.97, d) * 0.92 : smoothstep(0.5, 0.86, d);

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

        let shade = tone * seamDark * bevel;
        if (look.mass === "rock") {
          // Природна скеля (Ліс, Село): без цегли — великі м'які плями й пологі пласти, як мазки.
          const strata = Math.sin(wy * 1.7 + (N.fbm(wx * 0.3, wy * 0.3, seed + 41, 3) - 0.5) * 5) * 0.05;
          shade = 0.9 + (N.fbm(wx * 0.45, wy * 0.6, seed + 43, 4) - 0.5) * 0.32 + strata;
        } else if (look.mass === "blocks") {
          // Кладка (Гробниця, Столиця): великі блоки ледь читаються, шов — тонка тінь, без «плитки».
          shade = (0.93 + N.hash(col, row, seed + 11) * 0.1 + (N.fbm(wx * 0.6, wy * 0.6, seed + 13, 3) - 0.5) * 0.16)
            * (seam < 0.03 ? 0.78 : 1) * ((1 - (rowF - row)) < 0.05 ? 1.06 : 1);
        }
        if (tex) {
          // Рельєф з текстур Midjourney: у краю маси — кладка, у глибині — дикий камінь (дві плитки
          // різного розміру, змішані шумом, щоб повтор не читався), там, де стоять, — плити підлоги.
          // (2026-10-27, другий підхід: «виглядає сиро» — рельєф тихий, як мазки, а не фото; найсильніший
          // біля краю, де на камінь падає світло, у товщі майже згасає, як у Hollow Knight)
          const mix = N.fbm(wx * 0.11, wy * 0.11, seed + 31, 2);
          const rockDetail = texAt(tex.rock, wx, wy, 8) * mix + texAt(tex.rock, wx + 3.7, wy + 2.3, 11) * (1 - mix);
          let detail = texAt(tex.masonry, wx, wy, 4.5) * (1 - depth) + rockDetail * depth;
          detail = detail * (1 - guard) + texAt(tex.floor, wx, wy, 4) * guard;
          const strength = 0.25 * (1 - depth) + 0.08 * depth;
          shade = (1 + Math.max(-0.4, Math.min(0.32, detail * strength))) * (0.92 + (N.fbm(wx * 0.5, wy * 0.5, seed + 37, 3) - 0.5) * 0.2);
        }
        let r = band[0] * shade, g = band[1] * shade, b = band[2] * shade;
        const sink = depth;
        r = r + (core[0] * shade - r) * sink; g = g + (core[1] * shade - g) * sink; b = b + (core[2] * shade - b) * sink;

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
    cap(f, x0, x1, y, look, seed);
  }

  // Верх поверхні за характером зони (2026-10-28): плити підлоги, трава, тесаний карниз. Верх
  // малюнка — рівно по верху каменю: нічого не стирчить там, де колайдер каже «повітря», крім трави.
  function cap(f, x0, x1, y, look, seed) {
    const c = f.c, S = f.ppu, N2 = root.PaintNoise, ay = f.y(y);
    if (look.cap === "flagstones") {
      let x = x0, k = 0;
      while (x < x1 - 0.1) {
        const w = Math.min(x1 - x, 0.7 + N2.hash(Math.round(x * 9), Math.round(y * 9), seed + 51) * 0.7);
        const tone = 0.85 + N2.hash(k, Math.round(y * 5), seed + 53) * 0.3;
        const ax = f.x(x) + S * 0.03, bx = f.x(x + w) - S * 0.03, h = S * 0.26;
        const g = c.createLinearGradient(0, ay, 0, ay + h);
        g.addColorStop(0, rgba(look.lipLight, 0.55 * tone)); g.addColorStop(0.25, rgba(look.lip, 0.95)); g.addColorStop(1, rgba(look.lipDark, 0.95));
        c.fillStyle = g;
        c.beginPath();
        c.moveTo(ax, ay); c.lineTo(bx, ay); c.lineTo(bx, ay + h * 0.75);
        c.quadraticCurveTo(bx, ay + h, bx - S * 0.06, ay + h); c.lineTo(ax + S * 0.06, ay + h);
        c.quadraticCurveTo(ax, ay + h, ax, ay + h * 0.75); c.closePath(); c.fill();
        c.strokeStyle = "rgba(0,0,0,0.55)"; c.lineWidth = Math.max(1, S * 0.02); c.stroke();
        x += w; k++;
      }
      c.strokeStyle = rgba(look.lipLight, 0.75); c.lineWidth = Math.max(1.5, S * 0.035);
      c.beginPath(); c.moveTo(f.x(x0) + 1, ay + 1); c.lineTo(f.x(x1) - 1, ay + 1); c.stroke();
    } else if (look.cap === "grass") {
      const grass = look.grass || look.moss || "#4a5a3a";
      // темна смуга ґрунту під травою
      const g = c.createLinearGradient(0, ay, 0, ay + S * 0.3);
      g.addColorStop(0, rgba(grass, 0.9)); g.addColorStop(1, rgba(look.lipDark, 0));
      c.fillStyle = g; c.fillRect(f.x(x0), ay, f.x(x1) - f.x(x0), S * 0.3);
      // кущики травинок: густо там, де шум, рідко між ними
      c.lineCap = "round";
      for (let x = x0 + 0.05; x < x1 - 0.05; x += 0.07) {
        const dense = N2.fbm(x * 0.9, y, seed + 61, 2);
        if (N2.hash(Math.round(x * 41), Math.round(y * 7), seed + 63) > dense * 1.3) continue;
        const h = S * (0.08 + dense * 0.28) * (0.6 + N2.hash(Math.round(x * 37), 1, seed + 65) * 0.6);
        const lean = (N2.hash(Math.round(x * 29), 2, seed + 67) - 0.5) * S * 0.12;
        c.strokeStyle = rgba(N2.hash(Math.round(x * 31), 3, seed) > 0.7 ? (look.lipLight || grass) : grass, 0.95);
        c.lineWidth = Math.max(1, S * 0.03);
        c.beginPath(); c.moveTo(f.x(x), ay + S * 0.04); c.quadraticCurveTo(f.x(x) + lean * 0.3, ay - h * 0.6, f.x(x) + lean, ay - h); c.stroke();
      }
    } else if (look.cap === "trim") {
      // тесаний карниз: світла верхня смуга, тонка тінь, ряд зубців під нею
      const h = S * 0.18;
      c.fillStyle = rgba(look.lipLight, 0.35); c.fillRect(f.x(x0), ay, f.x(x1) - f.x(x0), h);
      c.fillStyle = "rgba(0,0,0,0.45)"; c.fillRect(f.x(x0), ay + h, f.x(x1) - f.x(x0), Math.max(1, S * 0.03));
      c.fillStyle = rgba(look.lip, 0.8);
      for (let x = x0 + 0.1; x < x1 - 0.2; x += 0.32) {
        c.fillRect(f.x(x), ay + h + S * 0.05, S * 0.16, S * 0.12);
      }
      c.strokeStyle = rgba(look.lipLight, 0.85); c.lineWidth = Math.max(1.5, S * 0.035);
      c.beginPath(); c.moveTo(f.x(x0) + 1, ay + 1); c.lineTo(f.x(x1) - 1, ay + 1); c.stroke();
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
          // Матеріал уступу за місцем (materials.js): полиця, гілка, балкон біля стіни; міст, настил,
          // колода чи плита у повітрі — на підвісах до першого каменю вгорі.
          const M = root.LevelMaterials, zoneName = look.style || "tomb", pal = M.WOOD[zoneName] || M.WOOD.tomb;
          const wallLeft = T.isRock(T.at(zone, gx0 - 1, gy)), wallRight = T.isRock(T.at(zone, gx1, gy));
          const kind = M.kindOf(zoneName, wallLeft || wallRight, gx1 - gx0, s2);
          const hangKind = look.hang === "roots" ? "root" : zoneName === "village" ? "rope" : "chain";
          const ceilingAt = cx => {
            let top = gy + 1;
            while (top < gy + 60 && !T.isRock(T.at(zone, cx, top))) top++;
            return top >= gy + 60 ? null : T.world(zone, cx, top)[1];
          };
          const hangers = () => [[wx0 + 0.3, ceilingAt(gx0)], [wx1 - 0.3, ceilingAt(gx1 - 1)]];
          const wallSide = wallLeft ? -1 : 1;
          if (kind === "shelf") M.shelf(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "branch") M.branch(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "balcony") M.balcony(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "bridge") {
            const hs = hangers();
            if (wx1 - wx0 > 7) { const mid = Math.round((gx0 + gx1) / 2); hs.push([T.world(zone, mid, gy)[0], ceilingAt(mid)]); }
            M.bridge(f, wx0, wx1, wyt, hs, pal, s2, hangKind);
          } else {
            hangers().forEach(([hx, up]) => { if (up !== null) M.rope(f, hx, up, wyt, pal, s2 + Math.round(hx * 7), hangKind); });
            if (kind === "log") M.log(f, wx0, wx1, wyt, pal, s2);
            else if (kind === "slab") { M.slab(f, wx0, wx1, wyt, look, s2); lip(f, wx0, wx1, wyt, look, s2); }
            else M.planks(f, wx0, wx1, wyt, pal, s2, 0.3);
          }
          start = -1;
        }
      }
    }
  }

  root.LevelTerrain = { paintChunk, loadTextures };
})(window);
