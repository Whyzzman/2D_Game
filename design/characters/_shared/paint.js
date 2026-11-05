// Шар вигляду персонажів у дусі Hollow Knight (2026-11-05, користувач: «не реалістично — згладжено, затемнено,
// трохи незрозуміло, плащі; на рівні Hollow Knight»; решено Claude за постійною згодою). Код персонажів малює
// ламаними й пласкими заливками; цей шар перехоплює полотно:
//   • кожна ламана стає плавною кривою (Chaikin) — жодного кута, форми течуть;
//   • заливка темніє й тьмяніє до силуету, з м'яким переходом від світлого боку до тіні — тіло читається плямою;
//   • світло лишається тільки там, де світить: очі, маски, полум'я, леза — їх шар не чіпає.
// Рухи, пози й удари ті самі.   Paint.wrap(ctx) — один раз на контекст кадру.
(function (root) {
  "use strict";
  const LIGHT = [-0.62, -0.78];          // до світла, у координатах полотна (y вниз)
  const DARK = 0.58, DESAT = 0.45;       // наскільки тіло темніє й тьмяніє до силуету
  const COOL = [14, 20, 34];             // холодна тінь ночі, куди тягнуться кольори

  function rgbOf(c) {
    if (typeof c !== "string" || c[0] !== "#") return null;
    let h = c.slice(1);
    if (h.length === 3) h = h.split("").map(x => x + x).join("");
    if (h.length !== 6 && h.length !== 8) return null;
    if (h.length === 8 && parseInt(h.slice(6, 8), 16) < 250) return null;
    return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
  }
  // колір тіла: темніше, ближче до сірого, з холодом ночі
  function night(c, k) {
    const grey = (c[0] + c[1] + c[2]) / 3;
    return c.map((v, i) => {
      const d = v + (grey - v) * DESAT;
      return Math.max(0, Math.min(255, Math.round((d * DARK + COOL[i] * (1 - DARK) * 0.6) * k)));
    });
  }
  const css = c => `rgb(${c.join(",")})`;

  function chaikin(pts, closed, rounds) {
    let p = pts;
    for (let r = 0; r < rounds; r++) {
      const out = [], n = p.length;
      if (!closed) out.push(p[0]);
      for (let i = 0; i < (closed ? n : n - 1); i++) {
        const a = p[i], b = p[(i + 1) % n];
        out.push([a[0] * 0.75 + b[0] * 0.25, a[1] * 0.75 + b[1] * 0.25], [a[0] * 0.25 + b[0] * 0.75, a[1] * 0.25 + b[1] * 0.75]);
      }
      if (!closed) out.push(p[n - 1]);
      p = out;
    }
    return p;
  }

  function wrap(g) {
    if (g.__painted) return g;
    g.__painted = true;
    const P = CanvasRenderingContext2D.prototype, o = {};
    for (const k of ["beginPath", "moveTo", "lineTo", "closePath", "quadraticCurveTo", "bezierCurveTo", "arc", "ellipse", "rect",
      "fill", "stroke", "clip", "fillRect"]) o[k] = P[k].bind(g);

    let box = null, poly = null;          // межі шляху (світові) і ламана, що ще не намальована (світові точки)
    const world = (x, y) => { const m = g.getTransform(); return [m.a * x + m.c * y + m.e, m.b * x + m.d * y + m.f]; };
    const grow = (w) => {
      if (!box) box = [w[0], w[1], w[0], w[1]];
      else { box[0] = Math.min(box[0], w[0]); box[1] = Math.min(box[1], w[1]); box[2] = Math.max(box[2], w[0]); box[3] = Math.max(box[3], w[1]); }
    };
    const add = (x, y) => { const w = world(x, y); grow(w); return w; };
    // ламана йде в шлях плавною: 2 кроки Chaikin для фігур із 4+ точок, 1 — для довших відкритих ліній
    function flush(closing) {
      if (!poly) return;
      const pts = poly.pts, closed = closing || poly.closed;
      let out = pts;
      // гладимо лише ламану, що почалася з moveTo; продовження кривої лишається як є
      if (poly.start && pts.length >= 4 && closed) out = chaikin(pts, true, 2);
      else if (poly.start && pts.length >= 3 && !closed) out = chaikin(pts, false, 1);
      g.save(); g.setTransform(1, 0, 0, 1, 0, 0);
      (poly.start ? o.moveTo : o.lineTo)(...out[0]); for (let i = 1; i < out.length; i++) o.lineTo(...out[i]);
      if (closed) o.closePath();
      g.restore();
      poly = null;
    }
    g.beginPath = () => { poly = null; box = null; o.beginPath(); };
    g.moveTo = (x, y) => { flush(false); poly = { pts: [add(x, y)], closed: false, start: true }; };
    g.lineTo = (x, y) => { const w = add(x, y); if (poly) poly.pts.push(w); else poly = { pts: [w], closed: false, start: false }; };
    g.closePath = () => { if (poly) { poly.closed = true; flush(true); } else o.closePath(); };
    // криві й дуги вже плавні: ламана перед ними йде як є, щоб крива продовжила її кінець
    const raw = (fn) => (...a) => {
      if (poly) { const p = poly; poly = null; g.save(); g.setTransform(1, 0, 0, 1, 0, 0); (p.start ? o.moveTo : o.lineTo)(...p.pts[0]); p.pts.slice(1).forEach(q => o.lineTo(...q)); g.restore(); }
      fn(...a);
    };
    g.quadraticCurveTo = raw((cx, cy, x, y) => { add(cx, cy); add(x, y); o.quadraticCurveTo(cx, cy, x, y); });
    g.bezierCurveTo = raw((a, b, c, d, x, y) => { add(a, b); add(c, d); add(x, y); o.bezierCurveTo(a, b, c, d, x, y); });
    g.arc = raw((x, y, r, a0, a1, ccw) => { add(x - r, y - r); add(x + r, y + r); o.arc(x, y, r, a0, a1, ccw); });
    g.ellipse = raw((x, y, rx, ry, rot, a0, a1, ccw) => { const r = Math.max(rx, ry); add(x - r, y - r); add(x + r, y + r); o.ellipse(x, y, rx, ry, rot, a0, a1, ccw); });
    g.rect = raw((x, y, w, h) => { add(x, y); add(x + w, y + h); o.rect(x, y, w, h); });

    function material() {
      if (g.globalCompositeOperation !== "source-over" || g.globalAlpha < 0.95 || !box) return null;
      const c = rgbOf(g.fillStyle);
      if (!c) return null;
      const mx = Math.max(...c), mn = Math.min(...c), lum = (c[0] + c[1] + c[2]) / 3;
      if (lum < 16) return null;                                    // чорнило
      if ((mx >= 200 && mx - mn >= 60) || lum >= 200) return null;   // те, що світить: очі, маски, полум'я, лезо
      if (box[2] - box[0] < 3 || box[3] - box[1] < 3) return null;
      return c;
    }
    function gradient(c) {
      const [x0, y0, x1, y1] = box, cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, r = Math.hypot(x1 - x0, y1 - y0) / 2 + 1;
      const inv = g.getTransform().invertSelf();
      const loc = (x, y) => [inv.a * x + inv.c * y + inv.e, inv.b * x + inv.d * y + inv.f];
      const grad = g.createLinearGradient(...loc(cx + LIGHT[0] * r, cy + LIGHT[1] * r), ...loc(cx - LIGHT[0] * r, cy - LIGHT[1] * r));
      grad.addColorStop(0, css(night(c, 1.35)));
      grad.addColorStop(0.5, css(night(c, 1.0)));
      grad.addColorStop(1, css(night(c, 0.6)));
      return grad;
    }
    g.fill = (...a) => {
      flush(true);
      const c = material();
      if (!c) { o.fill(...a); return; }
      const keep = g.fillStyle; g.fillStyle = gradient(c); o.fill(...a); g.fillStyle = keep;
    };
    g.stroke = (...a) => { flush(false); o.stroke(...a); };
    g.clip = (...a) => { flush(true); o.clip(...a); };
    g.fillRect = (x, y, w, h) => {
      const saved = box; box = null; add(x, y); add(x + w, y); add(x, y + h); add(x + w, y + h);
      const c = material();
      if (c) { const keep = g.fillStyle; g.fillStyle = gradient(c); o.fillRect(x, y, w, h); g.fillStyle = keep; }
      else o.fillRect(x, y, w, h);
      box = saved;
    };
    return g;
  }

  root.Paint = { wrap };
})(window);
