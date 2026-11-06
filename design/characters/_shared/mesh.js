// Живе тіло для мальованих персонажів (2026-11-05, користувач: «моделі на рівні Hollow Knight»; решено Claude за
// постійною згодою). Мальований Midjourney малюнок не повертається як жорстка картонка: він — сітка трикутників, і
// кожна вершина йде за позою (корпус гнеться від стоп, голова окремо, поділ плаща гойдається, у звіра лапи
// ходять навколо плечей і стегон). Малюнок спершу чисто зменшується до розміру кадру (крок за кроком удвічі), тому
// в кадрі він чіткий, а не каша.
//   Mesh.prescale(img, w, h) — зменшена копія (полотно)
//   Mesh.draw(g, src, cols, rows, map) — map(u, v) → [x, y] у поточних координатах; u, v ∈ [0, 1], v = 0 — верх
(function (root) {
  "use strict";

  function prescale(img, w, h) {
    let cur = img, cw = img.width, ch = img.height;
    while (cw / 2 >= w && ch / 2 >= h) {
      const c = document.createElement("canvas");
      c.width = Math.max(1, Math.round(cw / 2)); c.height = Math.max(1, Math.round(ch / 2));
      const g = c.getContext("2d"); g.imageSmoothingEnabled = true; g.imageSmoothingQuality = "high";
      g.drawImage(cur, 0, 0, c.width, c.height);
      cur = c; cw = c.width; ch = c.height;
    }
    const c = document.createElement("canvas");
    c.width = Math.max(1, Math.round(w)); c.height = Math.max(1, Math.round(h));
    const g = c.getContext("2d"); g.imageSmoothingEnabled = true; g.imageSmoothingQuality = "high";
    g.drawImage(cur, 0, 0, c.width, c.height);
    return c;
  }

  // Трикутник малюнка (s — пікселі джерела) у трикутник кадру (d): афінне перетворення під кліпом трикутника,
  // трохи розширеного від центру, щоб між сусідами не світилися шви. Розширення — у пікселях кадру, не в одиницях
  // поточних координат (2026-11-06): герой малюється в масштабі 0,27, і 0,7 одиниці ставали 0,2 пікселя — по
  // плащу йшли діагональні шви, а кант їх ще й підсвічував.
  function tri(g, src, s0, s1, s2, d0, d1, d2, pad) {
    const cx = (d0[0] + d1[0] + d2[0]) / 3, cy = (d0[1] + d1[1] + d2[1]) / 3;
    const grow = (p) => {
      const dx = p[0] - cx, dy = p[1] - cy, l = Math.hypot(dx, dy) || 1;
      return [p[0] + dx / l * pad, p[1] + dy / l * pad];
    };
    const e0 = grow(d0), e1 = grow(d1), e2 = grow(d2);
    const [x0, y0] = s0, [x1, y1] = s1, [x2, y2] = s2;
    const [u0, v0] = d0, [u1, v1] = d1, [u2, v2] = d2;
    const det = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
    if (Math.abs(det) < 1e-6) return;
    const a = ((u1 - u0) * (y2 - y0) - (u2 - u0) * (y1 - y0)) / det;
    const b = ((v1 - v0) * (y2 - y0) - (v2 - v0) * (y1 - y0)) / det;
    const c = ((u2 - u0) * (x1 - x0) - (u1 - u0) * (x2 - x0)) / det;
    const d = ((v2 - v0) * (x1 - x0) - (v1 - v0) * (x2 - x0)) / det;
    const e = u0 - a * x0 - c * y0, f = v0 - b * x0 - d * y0;
    g.save();
    g.beginPath(); g.moveTo(e0[0], e0[1]); g.lineTo(e1[0], e1[1]); g.lineTo(e2[0], e2[1]); g.closePath();
    g.clip();
    g.transform(a, b, c, d, e, f);
    g.drawImage(src, 0, 0);
    g.restore();
  }

  // padPx — на скільки пікселів кадру розширити трикутник: тілу 1,1 (непрозоре, перекриття не видно); світлу, що
  // малюється додаванням («lighter»), — менше, бо перекриття там світиться смугою
  function draw(g, src, cols, rows, map, padPx) {
    const W = src.width, H = src.height, pts = [];
    for (let j = 0; j <= rows; j++) {
      const row = [];
      for (let i = 0; i <= cols; i++) row.push(map(i / cols, j / rows));
      pts.push(row);
    }
    const S = (i, j) => [i / cols * W, j / rows * H];
    // a pixel and a bit of the frame, in the current units (the transform may scale the figure down)
    const m = g.getTransform(), unit = Math.sqrt(Math.abs(m.a * m.d - m.b * m.c)) || 1, pad = (padPx == null ? 1.1 : padPx) / unit;
    for (let j = 0; j < rows; j++) for (let i = 0; i < cols; i++) {
      tri(g, src, S(i, j), S(i + 1, j), S(i + 1, j + 1), pts[j][i], pts[j][i + 1], pts[j + 1][i + 1], pad);
      tri(g, src, S(i, j), S(i + 1, j + 1), S(i, j + 1), pts[j][i], pts[j + 1][i + 1], pts[j + 1][i], pad);
    }
  }

  // поворот точки p навколо c на кут a (радіани, за годинниковою в координатах полотна)
  function rot(p, c, a) {
    const s = Math.sin(a), k = Math.cos(a), dx = p[0] - c[0], dy = p[1] - c[1];
    return [c[0] + dx * k - dy * s, c[1] + dx * s + dy * k];
  }

  root.Mesh = { prescale, draw, rot };
})(window);
