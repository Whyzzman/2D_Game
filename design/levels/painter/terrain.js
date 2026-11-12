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
    let core = hexRgb(look.core), band = hexRgb(look.stone), rim = hexRgb(look.rim);
    // Камінь у гамі своєї кімнати (2026-10-28, «кардинально інакше, красивіше»): тінь, тон і світло з фону
    // Midjourney (palette-<зона>.js), між кімнатами — плавно (розмита сітка кольорів по клітинках).
    const pal = paletteGrid(zone, cx0, cy0, gw, gh, look);
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
        s += (N.fbm(wx * 4.5, wy * 4.5, seed + 3, 2) - 0.5) * 0.09 * free;
        const sharp = rockAt(ix, iy);
        s = s * free + sharp * guard;
        if (s <= 0.47) {
          // Тінь довкола каменю на фоні (2026-11-07, «фон, який насправді стінка»): м'який темний ореол
          // відриває перший план від фону Midjourney; над підлогою, де стоять, його немає.
          const halo = smoothstep(0.26, 0.47, s) * free;
          if (halo > 0.01) {
            const o = (j * W + i) * 4;
            px[o] = 4; px[o + 1] = 6; px[o + 2] = 8; px[o + 3] = 255 * halo * 0.5;
          }
          continue;
        }
        // Заокруглені кути: верх, що закінчується обривом убік, закруглюється (2026-10-28, «менше прямих
        // кутів»). Верх лишається рівним — по ньому ходять.
        if (sharp > 0.5 && airAt(ix, iy + 1)) {
          const lx = fx - ix, ly = fy - iy, R = 0.42;
          const openL = airAt(ix - 1, iy) && airAt(ix - 1, iy + 1), openR = airAt(ix + 1, iy) && airAt(ix + 1, iy + 1);
          if ((openL && lx < R && ly > 1 - R && Math.hypot(R - lx, ly - (1 - R)) > R)
            || (openR && lx > 1 - R && ly > 1 - R && Math.hypot(lx - (1 - R), ly - (1 - R)) > R)) continue;
        }
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
          // Бутова кладка: камені неправильної форми (комірки Вороного по зсунутій сітці), без прямих рядів.
          const vs = 1.4, gx = Math.floor(wx / vs), gy = Math.floor(wy / vs);
          let d1 = 9, d2 = 9, id = 0;
          for (let oy = -1; oy <= 1; oy++) for (let ox = -1; ox <= 1; ox++) {
            const cxv = gx + ox, cyv = gy + oy;
            const px2 = (cxv + 0.15 + N.hash(cxv, cyv, seed + 71) * 0.7) * vs, py2 = (cyv + 0.15 + N.hash(cxv, cyv, seed + 73) * 0.7) * vs;
            const dd = Math.hypot(wx - px2, (wy - py2) * 1.25);
            if (dd < d1) { d2 = d1; d1 = dd; id = cxv * 31 + cyv; } else if (dd < d2) d2 = dd;
          }
          const edge = d2 - d1;
          shade = (0.9 + N.hash(id, 7, seed + 75) * 0.16 + (N.fbm(wx * 0.6, wy * 0.6, seed + 13, 3) - 0.5) * 0.14)
            * (1 - (edge < 0.05 ? 0.28 : edge < 0.1 ? 0.12 : 0) * (1 - depth * 0.85)) * (1 + Math.max(0, 0.25 - d1) * 0.2);
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
        if (pal) {
          const u = fx - 0.5, v = fy - 0.5, at = k => sample(pal[k], gw, gh, u, v);
          band = [at(3) * 0.62 + at(0) * 0.38, at(4) * 0.62 + at(1) * 0.38, at(5) * 0.62 + at(2) * 0.38].map(c => c * 0.62);
          core = [at(0) * 0.32, at(1) * 0.32, at(2) * 0.32];
          rim = [at(6), at(7), at(8)];
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
          // Бічні грані теж ловлять світло (2026-11-07, користувач: «посеред блоками фон, який насправді не фон
          // а стінка»; решено Claude): вузька стіна з одним верхнім відблиском губилася у фоні Midjourney того ж
          // тону. Тепер кожна відкрита грань має світлий край — сильніший угорі, слабший з боків, — і тонкий
          // контур по самій межі, тож усе, у що впираєшся, читається як камінь на першому плані.
          const facing = Math.max(0, oy * 0.9 + Math.abs(ox) * 0.55);
          const near = smoothstep(0.78, 0.5, s);
          const lit = facing * near * 0.6;
          r += (rim[0] - r) * lit; g += (rim[1] - g) * lit; b += (rim[2] - b) * lit;
          // контур: світла нитка по самій межі каменю, з будь-якого боку
          const line = smoothstep(0.6, 0.5, s) * (0.22 + 0.2 * Math.max(0, oy));
          r += (rim[0] - r) * line; g += (rim[1] - g) * line; b += (rim[2] - b) * line;
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
    drawInside(frame, zone, cx0, cy0, gw, gh, look, seed, wx0, wy0, sx, sy);
    drawTops(frame, zone, cx0, cy0, gw, gh, look, seed);
    if (look.style === "forest") trunksOf(zone).forEach(t => drawTrunk(frame, zone, t, seed));
    drawLedges(frame, zone, cx0, cy0, gw, gh, look, seed);
    drawSpikes(frame, zone, cx0, cy0, gw, gh, look, seed);
  }

  // Шипи «^» (2026-11-04, користувач: «шипи виглядають некрасиво… по-ігровому, по-мультяшному»; решено Claude):
  // гра їх не малює (Spikes._painted), painter — тут, у стилі рівня. Гробниця й решта — криві кістяні ікла на
  // купці уламків, Ліс — терен: скручена лоза з загнутими колючками. Темний контур, світлі кінчики — читається
  // «не ступай» здалеку, як у Hollow Knight. Висота — уся стопка «^» над підлогою.
  function drawSpikes(f, zone, cx0, cy0, gw, gh, look, seed) {
    const c = f.c, S = f.ppu, cell = zone.cell, forest = look.style === "forest";
    for (let y = 0; y < gh; y++) {
      let start = -1;
      for (let x = 0; x <= gw; x++) {
        const gx = cx0 + x, gy = cy0 + y;
        const here = x < gw && T.at(zone, gx, gy) === "^" && T.at(zone, gx, gy - 1) !== "^";
        if (here && start < 0) start = x;
        if (!here && start >= 0) {
          const g0 = cx0 + start, g1 = cx0 + x;
          let stack = 1;
          while (T.at(zone, g0, gy + stack) === "^") stack++;
          const [wx0, wy] = T.world(zone, g0, gy), [wx1] = T.world(zone, g1, gy);
          const lk = lookAt(zone, g0, gy, look);
          spikeRow(f, wx0, wx1, wy, stack * cell * 0.85 + 0.3, lk, seed + g0 * 7 + gy * 13, forest);
          start = -1;
        }
      }
    }
  }

  function spikeRow(f, x0, x1, floor, height, lk, seed, forest) {
    const c = f.c, S = f.ppu;
    const ink = "#07080b";
    const body = forest ? "#4a5a32" : (lk.lip || "#6a5a4c");
    const tip = forest ? "#f0eebc" : "#fbf0d4";
    // основа: купка уламків / скручена лоза — щоб шипи росли з чогось
    c.fillStyle = ink;
    c.beginPath(); c.moveTo(f.x(x0 - 0.05), f.y(floor));
    for (let k = 0; k <= 10; k++) {
      const t = k / 10, x = x0 + (x1 - x0) * t;
      c.lineTo(f.x(x), f.y(floor + 0.1 + N.hash(seed, k, 3) * 0.12));
    }
    c.lineTo(f.x(x1 + 0.05), f.y(floor)); c.closePath(); c.fill();
    const n = Math.max(2, Math.round((x1 - x0) / 0.36));
    const order = [];
    for (let k = 0; k < n; k++) order.push(k);
    // задні ряди — нижчі й темніші, передні — вищі
    for (const layer of [0, 1]) {
      for (const k of order) {
        const t = (k + 0.5 + (N.hash(seed, k, 5 + layer) - 0.5) * 0.5) / n;
        const bx = x0 + (x1 - x0) * t;
        const h = height * (layer ? 0.75 + N.hash(seed, k, 7) * 0.3 : 0.5 + N.hash(seed, k, 9) * 0.25);
        const w = 0.17 + N.hash(seed, k, 11) * 0.07;
        const lean = (N.hash(seed, k, 13) - 0.5) * 0.25 + (forest ? (k % 2 ? 0.12 : -0.12) : 0);
        const tx = bx + lean * h, ty = floor + h;
        const bend = forest ? 0.12 : 0.05;
        // контур
        const shape = (grow) => {
          c.beginPath();
          c.moveTo(f.x(bx - w - grow), f.y(floor + 0.05));
          c.quadraticCurveTo(f.x(bx - w * 0.4 - grow + bend), f.y(floor + h * 0.55), f.x(tx), f.y(ty + grow));
          c.quadraticCurveTo(f.x(bx + w * 0.5 + grow + bend), f.y(floor + h * 0.45), f.x(bx + w + grow), f.y(floor + 0.05));
          c.closePath();
        };
        shape(0.035); c.fillStyle = ink; c.fill();
        const g = c.createLinearGradient(0, f.y(floor), 0, f.y(ty));
        g.addColorStop(0, layer ? body : ink); g.addColorStop(0.55, layer ? body : body); g.addColorStop(1, tip);
        shape(0); c.fillStyle = g; c.fill();
        if (layer) {
          // відблиск уздовж лівого боку — об'єм, як у мальованого
          c.strokeStyle = rgba(tip, 0.45); c.lineWidth = Math.max(1, S * 0.02);
          c.beginPath(); c.moveTo(f.x(bx - w * 0.45), f.y(floor + h * 0.2));
          c.quadraticCurveTo(f.x(bx - w * 0.2 + bend), f.y(floor + h * 0.6), f.x(tx - 0.01), f.y(ty - 0.04)); c.stroke();
          if (forest && N.hash(seed, k, 17) < 0.5) {
            // бічна колючка на терні
            const my = floor + h * 0.45, mx = bx + lean * h * 0.45, side = k % 2 ? 1 : -1;
            c.fillStyle = ink;
            c.beginPath(); c.moveTo(f.x(mx), f.y(my + 0.04)); c.lineTo(f.x(mx + side * 0.16), f.y(my + 0.14)); c.lineTo(f.x(mx), f.y(my - 0.04)); c.closePath(); c.fill();
          }
        }
      }
    }
  }

  // У товщі каменю (2026-11-04, «щоб гра виглядала дуже дорого»; решено Claude): мазки пензля в тоні кімнати,
  // щоб маса мала фактуру, як намальоване тло; у Гробниці — вмуровані черепи, кістки й тріщини, у Лісі —
  // коріння крізь скелю й камінці. Малюється лише поверх каменю (source-atop) і лише вглибині, далі від
  // країв, де стоять, — читається як фон стіни, не як щось, на що можна стати.
  function drawInside(f, zone, cx0, cy0, gw, gh, look, seed, wx0, wy0, sx, sy) {
    const c = f.c, S = f.ppu;
    const solidAround = (wx, wy, r) => {
      const [gx, gy] = [Math.floor((wx - zone.origin[0]) / zone.cell), Math.floor((wy - zone.origin[1]) / zone.cell)];
      for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++) if (!T.isRock(T.at(zone, gx + dx, gy + dy))) return false;
      return true;
    };
    // лише в смузі біля краю, яку видно з кімнати: не ближче 1 юніта до повітря й не далі 3,5
    const nearAir = (wx, wy, r) => !solidAround(wx, wy, r);
    c.save();
    c.globalCompositeOperation = "source-atop";
    const step = 1.9;
    for (let gy = Math.floor(wy0 / step); gy * step < wy0 + sy; gy++) for (let gx = Math.floor(wx0 / step); gx * step < wx0 + sx; gx++) {
      const h = N.hash(gx, gy, seed + 121);
      const wx = (gx + 0.2 + N.hash(gx, gy, seed + 123) * 0.6) * step, wy = (gy + 0.2 + N.hash(gx, gy, seed + 125) * 0.6) * step;
      if (!solidAround(wx, wy, 2) || !nearAir(wx, wy, 7)) continue;
      const lk = lookAt(zone, Math.floor((wx - zone.origin[0]) / zone.cell), Math.floor((wy - zone.origin[1]) / zone.cell), look);
      const pale = rgba(lk.lipLight || look.rim, 0.14), dark = "rgba(0,0,0,0.32)";
      if (look.mass === "blocks") {
        if (h < 0.1) embeddedSkull(f, wx, wy, 0.32 + N.hash(gx, gy, seed + 127) * 0.2, pale, dark);
        else if (h < 0.22) embeddedBones(f, wx, wy, pale, seed + gx * 7 + gy);
        else if (h < 0.4) crack(f, wx, wy, dark, seed + gx * 13 + gy);
      } else {
        if (h < 0.3) rootThrough(f, wx, wy, lk, seed + gx * 11 + gy);
        else if (h < 0.45) pebbles(f, wx, wy, pale, seed + gx * 17 + gy);
      }
    }
    c.restore();
  }

  function embeddedSkull(f, x, y, s, pale, dark) {
    const c = f.c, S = f.ppu;
    c.fillStyle = pale;
    c.beginPath(); c.ellipse(f.x(x), f.y(y), S * s * 0.5, S * s * 0.44, 0, 0, Math.PI * 2); c.fill();
    c.fillRect(f.x(x - s * 0.26), f.y(y - s * 0.2), S * s * 0.52, S * s * 0.3);
    c.fillStyle = dark;
    c.beginPath(); c.ellipse(f.x(x - s * 0.18), f.y(y), S * s * 0.11, S * s * 0.12, 0, 0, Math.PI * 2); c.fill();
    c.beginPath(); c.ellipse(f.x(x + s * 0.18), f.y(y), S * s * 0.11, S * s * 0.12, 0, 0, Math.PI * 2); c.fill();
  }

  function embeddedBones(f, x, y, pale, seed) {
    const c = f.c, S = f.ppu;
    c.strokeStyle = pale; c.lineCap = "round"; c.lineWidth = S * 0.07;
    for (let k = 0; k < 3; k++) {
      const a = N.hash(seed, k, 3) * Math.PI, l = 0.35 + N.hash(seed, k, 5) * 0.35, ox = (N.hash(seed, k, 7) - 0.5) * 0.6;
      c.beginPath(); c.moveTo(f.x(x + ox - Math.cos(a) * l / 2), f.y(y - Math.sin(a) * l / 2));
      c.lineTo(f.x(x + ox + Math.cos(a) * l / 2), f.y(y + Math.sin(a) * l / 2)); c.stroke();
    }
  }

  function crack(f, x, y, dark, seed) {
    const c = f.c, S = f.ppu;
    c.strokeStyle = dark; c.lineWidth = Math.max(1, S * 0.025); c.lineCap = "round";
    let px = x, py = y;
    c.beginPath(); c.moveTo(f.x(px), f.y(py));
    for (let k = 0; k < 5; k++) {
      px += (N.hash(seed, k, 3) - 0.5) * 0.5; py -= 0.15 + N.hash(seed, k, 5) * 0.3;
      c.lineTo(f.x(px), f.y(py));
    }
    c.stroke();
  }

  function rootThrough(f, x, y, lk, seed) {
    const c = f.c, S = f.ppu;
    // корінь: товстий біля початку, звужується, з тінню під собою й світлом по верху; одна-дві гілочки
    const a = (N.hash(seed, 1, 3) - 0.5) * 1.4 - Math.PI / 2, len = 1.4 + N.hash(seed, 1, 5) * 1.6;
    const pts = [];
    for (let k = 0; k <= 12; k++) {
      const t = k / 12, bend = Math.sin(t * Math.PI) * (N.hash(seed, 1, 7) - 0.5) * 1.1;
      pts.push([x + Math.cos(a) * len * t - Math.sin(a) * bend, y + Math.sin(a) * len * t + Math.cos(a) * bend, 0.32 * (1 - t * 0.85)]);
    }
    const ribbon = (o, k, col) => {
      c.fillStyle = col; c.beginPath();
      pts.forEach(([px, py, w], i) => i ? c.lineTo(f.x(px + o - w * k), f.y(py - o)) : c.moveTo(f.x(px + o - w * k), f.y(py - o)));
      for (let i = pts.length - 1; i >= 0; i--) { const [px, py, w] = pts[i]; c.lineTo(f.x(px + o + w * k), f.y(py - o)); }
      c.closePath(); c.fill();
    };
    ribbon(0.07, 0.6, "rgba(0,0,0,0.4)");                // its shadow in the stone
    ribbon(0, 0.5, rgba(lk.lipDark || "#2a3a31", 0.55));  // the root, a shade lighter than the stone
    ribbon(-0.03, 0.18, rgba(lk.lip || "#4e6656", 0.2));  // the light along it, dim
  }

  function pebbles(f, x, y, pale, seed) {
    const c = f.c, S = f.ppu;
    c.fillStyle = pale;
    for (let k = 0; k < 4; k++) {
      const ox = (N.hash(seed, k, 3) - 0.5) * 0.9, oy = (N.hash(seed, k, 5) - 0.5) * 0.6, r = 0.08 + N.hash(seed, k, 7) * 0.12;
      c.beginPath(); c.ellipse(f.x(x + ox), f.y(y + oy), S * r, S * r * 0.7, N.hash(seed, k, 9) * 3, 0, Math.PI * 2); c.fill();
    }
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
            // обрив з боку — кут заокруглений, як і камінь під ним
            const air = (cx, cy) => T.isAir(T.at(zone, cx, cy));
            const openL = air(cx0 + start - 1, cy0 + y) && air(cx0 + start - 1, cy0 + y + 1);
            const openR = air(cx0 + x, cy0 + y) && air(cx0 + x, cy0 + y + 1);
            const c = f.c;
            c.save();
            c.beginPath();
            const ax = f.x(wx0), bx = f.x(wx1), ay = f.y(wy), by = ay + f.ppu * 1.2;
            const rl = openL ? f.ppu * zone.cell * 0.84 : 0, rr = openR ? f.ppu * zone.cell * 0.84 : 0;
            c.moveTo(ax, by); c.lineTo(ax, ay + rl); c.quadraticCurveTo(ax, ay, ax + rl, ay);
            c.lineTo(bx - rr, ay); c.quadraticCurveTo(bx, ay, bx, ay + rr); c.lineTo(bx, by); c.closePath();
            c.rect(ax - 50, ay - f.ppu * 0.6, bx - ax + 100, f.ppu * 0.6); // трава над краєм не обрізається
            c.clip();
            lip(f, wx0, wx1, wy, lookAt(zone, cx0 + Math.round((start + x) / 2), cy0 + y, look), seed);
            c.restore();
            if (root.LevelProps) root.LevelProps.dress(f, zone, wx0, wx1, wy, lookAt(zone, cx0 + Math.round((start + x) / 2), cy0 + y, look), seed + y * 31 + start);
          }
          start = -1;
        }
      }
    }
  }

  // Кольори кімнат по клітинках шматка, розмиті, — 9 каналів: тінь, тон, світло.
  function paletteGrid(zone, cx0, cy0, gw, gh, look) {
    const all = root.LevelPalettes && root.LevelPalettes[look.style || "tomb"];
    if (!all || !look.roomPalette) return null;
    const ch = [];
    for (let k = 0; k < 9; k++) ch.push(new Float32Array(gw * gh));
    const fallback = { dark: hexRgb(look.core).map(v => v * 2.5), mid: hexRgb(look.stone).map(v => v * 1.6), light: hexRgb(look.rim) };
    for (let y = 0; y < gh; y++) for (let x = 0; x < gw; x++) {
      const room = T.roomOf(zone, cx0 + x, cy0 + y);
      const p = (room && all[room.id]) || fallback;
      const v = [...p.dark, ...p.mid, ...p.light];
      for (let k = 0; k < 9; k++) ch[k][y * gw + x] = v[k];
    }
    return ch.map(a => blur(a, gw, gh, 3));
  }

  // Вигляд для кромки й реквізиту там, де вони стоять: кольори кромки — з палітри кімнати.
  function lookAt(zone, cx, cy, look) {
    const all = root.LevelPalettes && root.LevelPalettes[look.style || "tomb"];
    const room = all && look.roomPalette ? T.roomOf(zone, cx, cy) : null;
    const p = room && all[room.id];
    if (!p) return look;
    const hex = v => "#" + v.map(c => Math.max(0, Math.min(255, Math.round(c))).toString(16).padStart(2, "0")).join("");
    const mix = (a, b, t) => a.map((c, i) => c + (b[i] - c) * t);
    return Object.assign({}, look, {
      lip: hex(mix(p.dark, p.mid, 0.7)), lipDark: hex(p.dark.map(c => c * 0.6)), lipLight: hex(mix(p.mid, p.light, 0.75)),
      moss: hex(mix(p.mid, p.light, 0.3)), grass: hex(mix(p.mid, p.light, 0.3)), glowTint: hex(p.light), shadowTint: hex(p.dark)
    });
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
        const ax = f.x(x) + S * 0.03, bx = f.x(x + w) - S * 0.03, h = S * (0.18 + N2.hash(Math.round(x * 13), Math.round(y * 3), seed + 57) * 0.18);
        const g = c.createLinearGradient(0, ay, 0, ay + h);
        g.addColorStop(0, rgba(look.lipLight, 0.55 * tone)); g.addColorStop(0.25, rgba(look.lip, 0.95)); g.addColorStop(1, rgba(look.lipDark, 0.95));
        c.fillStyle = g;
        c.beginPath();
        // плита з округлим, трохи обламаним низом: верх рівний (по ньому ходять), низ — ні
        c.moveTo(ax, ay); c.lineTo(bx, ay); c.lineTo(bx, ay + h * 0.45);
        c.quadraticCurveTo(bx, ay + h, bx - S * 0.16, ay + h * (0.92 + N2.hash(k, 3, seed + 59) * 0.12));
        c.quadraticCurveTo((ax + bx) / 2, ay + h * (1.08 + N2.hash(k, 5, seed + 61) * 0.15), ax + S * 0.16, ay + h);
        c.quadraticCurveTo(ax, ay + h, ax, ay + h * 0.45); c.closePath(); c.fill();
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

  // Дерева Лісу (2026-11-03, решено Claude): вільні уступи двома стовпчиками через щілину у 2 клітинки
  // (tools/level-drafts/trees.py) — гілки одного стовбура. Стовбур у щілині, позаду героя (колайдера нема),
  // від землі під нижньою гілкою до склепіння або, під небом, трохи вище верхньої — там крона розходиться.
  const trunkCache = new WeakMap();
  function trunksOf(zone) {
    if (trunkCache.has(zone)) return trunkCache.get(zone);
    const free = [];
    for (const r of zone.rooms) {
      for (let y = 0; y < r.h; y++) {
        const row = r.rows[y];
        for (let x = 0; x < r.w; x++) {
          if (row[x] !== "=" || (x > 0 && row[x - 1] === "=")) continue;
          let e = x;
          while (e + 1 < r.w && row[e + 1] === "=") e++;
          const gx0 = r.x + x, gx1 = r.x + e, gy = r.y + y;
          const touches = T.isRock(T.at(zone, gx0 - 1, gy)) || T.isRock(T.at(zone, gx1 + 1, gy));
          let above = false;
          for (let k = gx0; k <= gx1; k++) if (T.isRock(T.at(zone, k, gy + 1))) above = true;
          if (!touches && !above) free.push({ x0: gx0, x1: gx1, y: gy });
        }
      }
    }
    // гілка ліворуч закінчується на x, праворуч починається на x+3 — між ними стовбур x+1…x+2
    const cols = new Map();
    for (const a of free) for (const b of free) {
      if (b.x0 === a.x1 + 3 && Math.abs(a.y - b.y) <= 7) {
        const k = a.x1 + 1;
        if (!cols.has(k)) cols.set(k, []);
        cols.get(k).push(a, b);
      }
    }
    const trunks = [];
    cols.forEach((ls, x) => {
      const ys = ls.map(l => l.y);
      let lo = Math.min(...ys), hi = Math.max(...ys);
      let base = lo;
      while (base > lo - 60 && !T.isRock(T.at(zone, x, base - 1))) base--;
      let top = hi + 1, roof = false;
      while (top < hi + 24) { if (T.isRock(T.at(zone, x, top))) { roof = true; break; } top++; }
      if (!roof) top = hi + 5;
      trunks.push({ x, base, top, roof, forks: ls });
    });
    trunkCache.set(zone, trunks);
    return trunks;
  }

  function drawTrunk(f, zone, t, seed) {
    const c = f.c, S = f.ppu, pal = root.LevelMaterials.WOOD.forest;
    const [cxw, by] = T.world(zone, t.x + 1, t.base), ty = T.world(zone, 0, t.top)[1];
    const sway = y => (N.hash(seed + t.x, Math.floor(y), 31) - 0.5) * 0.06 + Math.sin(y * 0.35 + t.x) * 0.12;
    const half = y => 0.62 + 0.4 * Math.max(0, 1 - (y - by) / 2.5) ** 2; // корені розходяться внизу
    c.save();
    const g = c.createLinearGradient(f.x(cxw - 0.6), 0, f.x(cxw + 0.6), 0);
    g.addColorStop(0, pal.dark); g.addColorStop(0.35, pal.bark); g.addColorStop(0.6, pal.light); g.addColorStop(1, pal.dark);
    c.fillStyle = g;
    c.beginPath();
    const steps = Math.max(2, Math.ceil((ty - by) / 0.5));
    for (let i = 0; i <= steps; i++) { const y = by + (ty - by) * i / steps; c.lineTo(f.x(cxw - half(y) + sway(y)), f.y(y)); }
    for (let i = steps; i >= 0; i--) { const y = by + (ty - by) * i / steps; c.lineTo(f.x(cxw + half(y) + sway(y)), f.y(y)); }
    c.closePath(); c.fill();
    // кора: поздовжні борозни
    c.strokeStyle = rgba(pal.dark, 0.6); c.lineWidth = Math.max(1, S * 0.03);
    for (let k = -2; k <= 2; k++) {
      c.beginPath();
      for (let i = 0; i <= steps; i++) {
        const y = by + (ty - by) * i / steps, w = half(y) * 0.8;
        c.lineTo(f.x(cxw + k * w / 2.5 + sway(y) + (N.hash(seed, k, i) - 0.5) * 0.05), f.y(y));
      }
      c.stroke();
    }
    // мох плямами з тіньового боку
    c.fillStyle = rgba(pal.leaf, 0.5);
    for (let i = 0; i < (ty - by) * 1.5; i++) {
      const y = by + N.hash(seed + t.x, i, 51) * (ty - by), w = half(y);
      c.beginPath(); c.ellipse(f.x(cxw - w * 0.55 + sway(y)), f.y(y), S * w * 0.35, S * (0.2 + N.hash(seed, i, 53) * 0.4), 0, 0, Math.PI * 2); c.fill();
    }
    // без склепіння — стовбур тоншає в розвилку, далі крона в тумані
    if (!t.roof) {
      // розвилка: кілька гілок угору й убік, тоншають і розгалужуються; листя — дрібні купки на кінцях
      const twig = (x, y, a, len, w, depth, k) => {
        const ex = x + Math.cos(a) * len, ey = y + Math.sin(a) * len;
        c.strokeStyle = pal.dark; c.lineWidth = Math.max(1, S * w); c.lineCap = "round";
        c.beginPath(); c.moveTo(f.x(x), f.y(y)); c.quadraticCurveTo(f.x((x + ex) / 2 + Math.sin(a) * 0.15), f.y((y + ey) / 2), f.x(ex), f.y(ey)); c.stroke();
        if (depth > 0) {
          const spread = 0.35 + N.hash(seed + t.x, k, 47) * 0.3;
          twig(ex, ey, a - spread, len * 0.68, w * 0.62, depth - 1, k * 2 + 1);
          twig(ex, ey, a + spread, len * 0.62, w * 0.62, depth - 1, k * 2 + 2);
        } else {
          c.fillStyle = rgba(pal.leaf, 0.8);
          for (let j = 0; j < 7; j++) {
            const lx = ex + (N.hash(seed + k, j, 51) - 0.5) * 0.7, ly = ey + (N.hash(seed + k, j, 53) - 0.3) * 0.5;
            c.beginPath(); c.ellipse(f.x(lx), f.y(ly), S * 0.13, S * 0.07, N.hash(seed + k, j, 55) * 3, 0, Math.PI * 2); c.fill();
          }
        }
      };
      const top = ty - 0.2;
      [2.3, 1.6, 0.85].forEach((a, i) => twig(cxw + sway(top), top, a + (N.hash(seed + t.x, i, 45) - 0.5) * 0.3, 1.6, 0.16, 3, i + 1));
      [0.5, 2.65].forEach((a, i) => twig(cxw + sway(top - 1.5), top - 1.5, a, 1.1, 0.1, 2, i + 7));
    }
    c.restore();
  }

  // Опори драбин (2026-11-12, «елементи платформеру прості й некрасиві»; решено Claude за постійною згодою):
  // вільні уступи, що стоять один над одним (перекриття ≥ 2 клітинки, 3–8 рядів між ними), — щаблі однієї будови.
  // Під такою драбиною — опора від найнижчого щабля до каменю внизу (якщо близько) і до найвищого: у Селі дерев'яний
  // стовп риштовання, у Болотах гнилий стовбур, у Столиці кам'яний стовп. Лише малюнок — коллайдерів не додає.
  const postCache = new WeakMap();
  function postsOf(zone) {
    if (postCache.has(zone)) return postCache.get(zone);
    const free = [];
    for (const r of zone.rooms) {
      for (let y = 0; y < r.h; y++) {
        const row = r.rows[y];
        for (let x = 0; x < r.w; x++) {
          if (row[x] !== "=" || (x > 0 && row[x - 1] === "=")) continue;
          let e = x;
          while (e + 1 < r.w && row[e + 1] === "=") e++;
          const gx0 = r.x + x, gx1 = r.x + e, gy = r.y + y;
          const touches = T.isRock(T.at(zone, gx0 - 1, gy)) || T.isRock(T.at(zone, gx1 + 1, gy));
          if (!touches) free.push({ x0: gx0, x1: gx1, y: gy, group: -1 });
        }
      }
    }
    const stacked = (a, b) => Math.min(a.x1, b.x1) - Math.max(a.x0, b.x0) >= 1 && Math.abs(a.y - b.y) >= 3 && Math.abs(a.y - b.y) <= 8;
    const posts = [];
    free.forEach((l, i) => {
      if (l.group >= 0) return;
      const members = [l]; l.group = i;
      for (let k = 0; k < members.length; k++) free.forEach(o => { if (o.group < 0 && stacked(members[k], o)) { o.group = i; members.push(o); } });
      if (members.length < 2) return;
      const mid = members.reduce((s, m) => s + (m.x0 + m.x1 + 1) / 2, 0) / members.length;
      const px = Math.round(mid * 2) / 2;
      const low = Math.min(...members.map(m => m.y)), high = Math.max(...members.map(m => m.y));
      let base = low;
      while (base > low - 10 && !T.isRock(T.at(zone, Math.floor(px), base - 1))) base--;
      const grounded = T.isRock(T.at(zone, Math.floor(px), base - 1));
      posts.push({ x: px, bottom: grounded ? base : low - 1, top: high + 1, grounded });
    });
    postCache.set(zone, posts);
    return posts;
  }

  function drawPosts(f, zone, look, seed) {
    const zoneName = look.style || "tomb", M = root.LevelMaterials;
    const kind = zoneName === "village" ? "timber" : zoneName === "capital" ? "stone" : look.hang === "roots" && zoneName === "forest" && look.groundLine > 0 ? "trunk" : null;
    if (!kind || !M.post) return;
    postsOf(zone).forEach((p, i) => {
      const wx = zone.origin[0] + p.x * zone.cell;
      const yb = zone.origin[1] + p.bottom * zone.cell, yt = zone.origin[1] + p.top * zone.cell;
      M.post(f, wx, yb, yt, kind, look, seed + i * 31, p.grounded);
    });
  }

  // Уступи «=»: плита з рівним верхом і обламаним низом; вільна — на двох ланцюгах до склепіння.
  function drawLedges(f, zone, cx0, cy0, gw, gh, look, seed) {
    const c = f.c, S = f.ppu, N2 = root.PaintNoise;
    drawPosts(f, zone, look, seed);
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
          // гілка дерева росте від стовбура (trunksOf)
          const tree = zoneName === "forest" && trunksOf(zone).find(t => (t.x === gx1 || t.x + 2 === gx0) && gy >= t.base && gy < t.top);
          const kind = tree ? "branch" : M.kindOf(zoneName, wallLeft || wallRight, gx1 - gx0, s2);
          const hangKind = look.hang === "roots" ? "root" : zoneName === "village" ? "rope" : "chain";
          const ceilingAt = cx => {
            let top = gy + 1;
            while (top < gy + 60 && !T.isRock(T.at(zone, cx, top))) top++;
            return top >= gy + 60 ? null : T.world(zone, cx, top)[1];
          };
          const hangers = () => [[wx0 + 0.3, ceilingAt(gx0)], [wx1 - 0.3, ceilingAt(gx1 - 1)]];
          const wallSide = tree ? (tree.x === gx1 ? 1 : -1) : wallLeft ? -1 : 1;
          if (kind === "chunk") {
            // товщина брили — з місця під нею: найменший вільний стовпчик повітря під уступом
            let free = 40;
            for (let k = gx0; k < gx1; k++) {
              let d = 0;
              while (d < 40 && !T.isRock(T.at(zone, k, gy - 1 - d)) && !T.isLedge(T.at(zone, k, gy - 1 - d))) d++;
              free = Math.min(free, d);
            }
            const room = free * zone.cell, attach = wallLeft ? -1 : wallRight ? 1 : 0;
            const most = attach ? 1.9 : 1.35;
            const depth = Math.max(0.42, Math.min(most, room * 0.45, room - 1.9));
            const lk = lookAt(zone, gx0, gy, look), tinted = Object.assign({}, lk, { stone: lk.lip, core: lk.shadowTint || lk.core });
            // вільна брила в Гробниці висить на двох ланцюгах до склепіння, якщо воно не надто високо
            if (!attach) hangers().forEach(([hx, up]) => { if (up !== null && up - wyt < 12) M.rope(f, hx, up, wyt - depth * 0.3, pal, s2 + Math.round(hx * 7), hangKind); });
            M.chunk(f, wx0, wx1, wyt, depth, tinted, s2, attach);
            lip(f, wx0, wx1, wyt, lk, s2);
          } else if (kind === "outcrop") {
            // виступ стіни: та сама плита, що й підвісна, у кольорах кімнати, без ланцюгів — росте з каменю
            const lk = lookAt(zone, gx0, gy, look), tinted = Object.assign({}, lk, { stone: lk.lip, core: lk.shadowTint || lk.core });
            M.slab(f, wx0 - (wallLeft ? 0.25 : 0), wx1 + (wallRight ? 0.25 : 0), wyt, tinted, s2);
            lip(f, wx0, wx1, wyt, lk, s2);
          } else if (kind === "shelf") M.shelf(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "branch") M.branch(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "balcony") M.balcony(f, wx0, wx1, wyt, wallSide, pal, s2);
          else if (kind === "bridge") {
            const hs = hangers();
            if (wx1 - wx0 > 7) { const mid = Math.round((gx0 + gx1) / 2); hs.push([T.world(zone, mid, gy)[0], ceilingAt(mid)]); }
            M.bridge(f, wx0, wx1, wyt, hs, pal, s2, hangKind);
          } else {
            hangers().forEach(([hx, up]) => { if (up !== null) M.rope(f, hx, up, wyt, pal, s2 + Math.round(hx * 7), hangKind); });
            if (kind === "log") M.log(f, wx0, wx1, wyt, pal, s2);
            else if (kind === "slab") { const lk = lookAt(zone, gx0, gy, look), tinted = lk === look ? look : Object.assign({}, lk, { stone: lk.lip, core: lk.shadowTint }); M.slab(f, wx0, wx1, wyt, tinted, s2); lip(f, wx0, wx1, wyt, lk, s2); }
            else M.planks(f, wx0, wx1, wyt, pal, s2, 0.3);
          }
          start = -1;
        }
      }
    }
  }

  // Де шматок ставить свічки й гриби (для Light2D у сцені): той самий обхід країв, що й paintChunk, але без
  // малювання. Лише ті, що лежать у самому шматку: у запасі довкола їх малює сусідній шматок.
  function lightsOfChunk(zone, wx0, wy0, sx, sy, look, seed) {
    const cell = zone.cell, M = 12;
    const cx0 = Math.floor((wx0 - zone.origin[0]) / cell) - M, cy0 = Math.floor((wy0 - zone.origin[1]) / cell) - M;
    const gw = Math.ceil(sx / cell) + 2 * M + 1, gh = Math.ceil(sy / cell) + 2 * M + 1;
    const nothing = new Proxy(function () {}, { get: () => nothing, apply: () => nothing, set: () => true });
    const frame = { c: nothing, ppu: 48, x: wx => (wx - wx0) * 48, y: wy => (sy - (wy - wy0)) * 48 };
    root.LevelLightLog = [];
    drawTops(frame, zone, cx0, cy0, gw, gh, look, seed);
    const found = root.LevelLightLog.filter(l => l.x >= wx0 && l.x < wx0 + sx && l.y >= wy0 && l.y < wy0 + sy);
    root.LevelLightLog = null;
    return found;
  }

  root.LevelTerrain = { paintChunk, loadTextures, lightsOfChunk };
})(window);
