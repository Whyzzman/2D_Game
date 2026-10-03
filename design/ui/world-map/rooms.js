/* Тінь Забутого Королівства — нутро кімнат на карті зони.
   Правила користувача (2026-10-03): картинки не повторюються, і фон — не геометрія, а м'який
   живопис. Тому: усі форми — плавні криві (сплайн через точки), краї «живі» (легке спотворення
   шумом), три плани з серпанком і розмиттям, джерело світла й промені, туман над підлогою,
   зерно й віньєтка. Кожна кімната має свій мотив (жоден не повторюється) і свою підлогу. */
(function () {
  "use strict";
  var NS = "http://www.w3.org/2000/svg";
  var C;
  var defsReady = new WeakSet();

  function el(name, attrs, parent) {
    var node = document.createElementNS(NS, name);
    for (var key in attrs) { node.setAttribute(key, attrs[key]); }
    (parent || C.layer).appendChild(node);
    return node;
  }

  function mix(a, b, t) {
    var pa = parseInt(a.slice(1), 16), pb = parseInt(b.slice(1), 16);
    var r = Math.round(((pa >> 16) & 255) * (1 - t) + ((pb >> 16) & 255) * t);
    var g = Math.round(((pa >> 8) & 255) * (1 - t) + ((pb >> 8) & 255) * t);
    var bl = Math.round((pa & 255) * (1 - t) + (pb & 255) * t);
    return "#" + ((1 << 24) + (r << 16) + (g << 8) + bl).toString(16).slice(1);
  }

  // Плавна крива через точки (Catmull-Rom → кубічні Безьє).
  function smooth(points, closed) {
    var p = points, n = p.length;
    if (n < 2) { return ""; }
    var d = "M" + p[0][0].toFixed(1) + "," + p[0][1].toFixed(1);
    var count = closed ? n : n - 1;
    for (var i = 0; i < count; i++) {
      var p0 = p[(i - 1 + n) % n], p1 = p[i], p2 = p[(i + 1) % n], p3 = p[(i + 2) % n];
      if (!closed) { p0 = p[Math.max(0, i - 1)]; p3 = p[Math.min(n - 1, i + 2)]; }
      var c1 = [p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6];
      var c2 = [p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6];
      d += " C" + c1[0].toFixed(1) + "," + c1[1].toFixed(1) + " " + c2[0].toFixed(1) + "," + c2[1].toFixed(1) + " " + p2[0].toFixed(1) + "," + p2[1].toFixed(1);
    }
    return d + (closed ? " Z" : "");
  }

  function setupDefs(svg) {
    if (defsReady.has(svg)) { return; }
    defsReady.add(svg);
    var defs = svg.querySelector("defs");
    // «Живий» край: шум зсуває контур на кілька пікселів — мазок, а не лінійка.
    var paint = el("filter", { id: "paint", x: "-10%", y: "-10%", width: "120%", height: "120%" }, defs);
    el("feTurbulence", { type: "fractalNoise", baseFrequency: "0.045", numOctaves: "2", seed: "3", result: "n" }, paint);
    el("feDisplacementMap", { in: "SourceGraphic", in2: "n", scale: "4", xChannelSelector: "R", yChannelSelector: "G", result: "d" }, paint);
    el("feGaussianBlur", { in: "d", stdDeviation: "0.6" }, paint);
    var far = el("filter", { id: "far", x: "-20%", y: "-20%", width: "140%", height: "140%" }, defs);
    el("feTurbulence", { type: "fractalNoise", baseFrequency: "0.03", numOctaves: "2", seed: "9", result: "n" }, far);
    el("feDisplacementMap", { in: "SourceGraphic", in2: "n", scale: "6", xChannelSelector: "R", yChannelSelector: "G", result: "d" }, far);
    el("feGaussianBlur", { in: "d", stdDeviation: "2.4" }, far);
    var soft = el("filter", { id: "soft", x: "-50%", y: "-50%", width: "200%", height: "200%" }, defs);
    el("feGaussianBlur", { stdDeviation: "6" }, soft);
    var grain = el("filter", { id: "grain", x: "0", y: "0", width: "100%", height: "100%" }, defs);
    el("feTurbulence", { type: "fractalNoise", baseFrequency: "0.9", numOctaves: "2", seed: "5" }, grain);
    el("feColorMatrix", { values: "0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.55 0" }, grain);
  }

  // ------------------------------------------------------------- будівельні мазки

  // Стовбур: звужується догори, вигнутий, з корінням-розтрубом.
  function trunk(x, b, h, w, lean, fill) {
    var L = [], R = [];
    for (var i = 0; i <= 6; i++) {
      var t = i / 6, y = b - h * t;
      var bend = Math.sin(t * Math.PI * 0.8) * lean;
      var half = w * (1 - t * 0.65) * (i === 0 ? 1.9 : 1) / 2;
      L.push([x + bend - half + (C.r() - 0.5) * 1.5, y]);
      R.unshift([x + bend + half + (C.r() - 0.5) * 1.5, y]);
    }
    el("path", { d: smooth(L.concat(R), true), fill: fill });
    return [x + lean * Math.sin(Math.PI * 0.8), b - h];
  }

  // Гілка — плавна стрічка, що тоншає.
  function bough(x, y, dx, dy, w, fill) {
    var mx = x + dx * 0.5 + (C.r() - 0.5) * 8, my = y + dy * 0.5 - 6;
    el("path", { d: "M" + x + "," + (y - w / 2) + " Q" + mx + "," + (my - w / 3) + " " + (x + dx) + "," + (y + dy) + " Q" + mx + "," + (my + w / 3) + " " + x + "," + (y + w / 2) + " Z", fill: fill });
  }

  // Крона: м'яка хмара з кількох блобів.
  function canopy(x, y, rx, ry, fill, opacity) {
    var pts = [];
    for (var i = 0; i < 12; i++) {
      var a = i / 12 * Math.PI * 2, k = 0.75 + C.r() * 0.35;
      pts.push([x + Math.cos(a) * rx * k, y + Math.sin(a) * ry * k]);
    }
    el("path", { d: smooth(pts, true), fill: fill, opacity: opacity === undefined ? 1 : opacity });
  }

  // Пагорби, скелі, обриси: плавна лінія від одного краю до іншого й униз.
  function ridge(B, base, amp, fill, steps) {
    var pts = [[B.x - 10, B.y + B.h + 10], [B.x - 10, base]];
    steps = steps || 7;
    for (var i = 0; i <= steps; i++) {
      pts.push([B.x + B.w * i / steps, base - C.r() * amp]);
    }
    pts.push([B.x + B.w + 10, base], [B.x + B.w + 10, B.y + B.h + 10]);
    el("path", { d: smooth(pts.slice(1, -1), false) + " L" + (B.x + B.w + 10) + "," + (B.y + B.h + 10) + " L" + (B.x - 10) + "," + (B.y + B.h + 10) + " Z", fill: fill });
  }

  function mote(x, y, rr) {
    el("circle", { cx: x, cy: y, r: rr * 3.2, fill: C.glow, opacity: 0.16, filter: "url(#soft)" });
    el("circle", { cx: x, cy: y, r: rr, fill: mix(C.glow, "#ffffff", 0.4), opacity: 0.9 });
  }

  function layer(filter) {
    C.layer = el("g", filter ? { filter: "url(#" + filter + ")" } : {}, C.root);
    return C.layer;
  }

  // ------------------------------------------------------------- мотиви
  // Кожен — дальній план (у серпанку), середній (мотив), ближній (підлога) — і своє світло.
  var MOTIFS = {
    gate: function (B, f) {
      layer("far"); for (var i = 0; i < 5; i++) { trunk(B.x + B.w * (0.1 + i * 0.2), f, B.h * (0.7 + C.r() * 0.3), 6, (C.r() - 0.5) * 10, C.far); }
      layer("paint");
      var x = B.x + B.w * 0.64, w = B.w * 0.16, h = B.h * 0.62;
      el("path", { d: "M" + (x - w) + "," + f + " L" + (x - w) + "," + (f - h * 0.6) + " C" + (x - w) + "," + (f - h * 1.05) + " " + (x + w) + "," + (f - h * 1.05) + " " + (x + w) + "," + (f - h * 0.6)
        + " L" + (x + w) + "," + f + " L" + (x + w * 0.6) + "," + f + " L" + (x + w * 0.6) + "," + (f - h * 0.6) + " C" + (x + w * 0.6) + "," + (f - h * 0.82) + " " + (x - w * 0.6) + "," + (f - h * 0.82) + " " + (x - w * 0.6) + "," + (f - h * 0.6) + " L" + (x - w * 0.6) + "," + f + " Z", fill: C.ink });
      var top = trunk(B.x + B.w * 0.24, f, B.h * 0.7, 7, -6, C.ink);
      bough(top[0], top[1] + 10, -24, -10, 4, C.ink); bough(top[0], top[1] + 18, 26, -14, 4, C.ink);
    },
    roots: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.7, 10, C.far);
      layer("paint");
      for (var i = 0; i < 6; i++) {
        var x = B.x + 6 + C.r() * (B.w - 12), l = B.h * (0.35 + C.r() * 0.4);
        el("path", { d: smooth([[x - 3, B.y - 2], [x + (C.r() - 0.5) * 10, B.y + l * 0.5], [x + (C.r() - 0.5) * 6, B.y + l]], false), stroke: C.ink, "stroke-width": 2 + C.r() * 2, fill: "none", "stroke-linecap": "round" });
      }
    },
    road: function (B, f) {
      layer("far"); for (var i = 0; i < 3; i++) { canopy(B.x + B.w * (0.2 + i * 0.3), B.y + B.h * 0.4, B.w * 0.18, B.h * 0.25, C.far); }
      layer("paint");
      ridge(B, f - 8, 6, C.mid, 5);
      el("path", { d: smooth([[B.x + B.w * 0.46, f], [B.x + B.w * 0.46, f - 18], [B.x + B.w * 0.5, f - 24], [B.x + B.w * 0.54, f - 18], [B.x + B.w * 0.54, f]], false) + " Z", fill: C.ink });
      var cx = B.x + B.w * 0.76;
      el("path", { d: smooth([[cx - 26, f - 4], [cx - 6, f - 16], [cx + 18, f - 22], [cx + 20, f - 14], [cx - 4, f - 6]], true), fill: C.ink });
      el("circle", { cx: cx - 16, cy: f - 6, r: 8, fill: "none", stroke: C.ink, "stroke-width": 3 });
    },
    thorns: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.6, 18, C.far);
      layer("paint");
      for (var i = 0; i < 4; i++) {
        var x = B.x + B.w * (0.12 + i * 0.25), s = 12 + C.r() * 10;
        var pts = [];
        for (var k = 0; k < 9; k++) { var a = Math.PI + k / 8 * Math.PI; var rr = s * (k % 2 ? 1.25 : 0.85); pts.push([x + Math.cos(a) * rr, f + Math.sin(a) * rr * 1.3]); }
        el("path", { d: smooth(pts, true), fill: C.ink });
      }
      layer();
      for (var m = 0; m < 9; m++) { mote(B.x + C.r() * B.w, B.y + 10 + C.r() * B.h * 0.55, 1.4); }
    },
    tower: function (B, f) {
      layer("far"); el("rect", { x: B.x + B.w * 0.18, y: B.y, width: B.w * 0.64, height: B.h, fill: C.far, rx: 6 });
      layer("paint");
      for (var y = B.y + 40; y < f - 20; y += 52) {
        el("path", { d: smooth([[B.x - 4, y], [B.x + B.w * 0.3, y + 2], [B.x + B.w * 0.6, y + 1]], false), stroke: C.ink, "stroke-width": 5, fill: "none", "stroke-linecap": "round" });
      }
      var lx = B.x + B.w * 0.74;
      el("path", { d: "M" + lx + "," + f + " L" + (lx - 2) + "," + (B.y + 14) + " M" + (lx + 11) + "," + f + " L" + (lx + 9) + "," + (B.y + 14), stroke: C.ink, "stroke-width": 2.2, "stroke-linecap": "round" });
      for (var r = f - 8; r > B.y + 18; r -= 10) { el("path", { d: "M" + (lx - 1) + "," + r + " l11,0", stroke: C.ink, "stroke-width": 1.6 }); }
      layer(); C.shaft(B.x + B.w * 0.3, B.y, B.w * 0.3);
    },
    bigtree: function (B, f) {
      layer("far"); for (var i = 0; i < 4; i++) { trunk(B.x + B.w * (0.08 + i * 0.12), f, B.h * 0.9, 8, (C.r() - 0.5) * 8, C.far); }
      layer("paint");
      var x = B.x + B.w * 0.68, w = B.w * 0.2;
      var top = trunk(x, f, B.h * 1.05, w, -10, C.ink);
      bough(x - w * 0.2, B.y + B.h * 0.28, -B.w * 0.45, -B.h * 0.18, 9, C.ink);
      bough(x + w * 0.1, B.y + B.h * 0.2, B.w * 0.35, -B.h * 0.2, 7, C.ink);
      // Дупло зі світлом.
      layer();
      el("ellipse", { cx: x - w * 0.1, cy: f - B.h * 0.36, rx: w * 0.13, ry: w * 0.26, fill: C.glow, opacity: 0.55, filter: "url(#soft)" });
      el("ellipse", { cx: x - w * 0.1, cy: f - B.h * 0.36, rx: w * 0.08, ry: w * 0.18, fill: mix(C.glow, "#ffffff", 0.3), opacity: 0.8 });
      void top;
    },
    vines: function (B, f) {
      layer("far"); for (var i = 0; i < 3; i++) { canopy(B.x + B.w * (0.15 + i * 0.35), B.y + 6, B.w * 0.22, B.h * 0.3, C.far); }
      layer("paint");
      for (var k = 0; k < 8; k++) {
        var x = B.x + 8 + k * (B.w - 16) / 7, l = B.h * (0.25 + C.r() * 0.45);
        el("path", { d: smooth([[x, B.y - 2], [x + 4, B.y + l * 0.35], [x - 3, B.y + l * 0.7], [x + 1, B.y + l]], false), stroke: C.ink, "stroke-width": 1.6, fill: "none" });
      }
      layer();
      for (var m = 0; m < 8; m++) { var vx = B.x + 8 + m * (B.w - 16) / 7; mote(vx + 1, B.y + B.h * (0.25 + C.r() * 0.45), 1.6); }
    },
    columns: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.55, 12, C.far, 4);
      layer("paint");
      [0.18, 0.4, 0.64, 0.86].forEach(function (t, i) {
        var x = B.x + B.w * t, h = B.h * (i % 2 ? 0.7 : 0.45), w = 7;
        el("path", { d: smooth([[x - w, f], [x - w + 1, f - h * 0.5], [x - w, f - h], [x - 2, f - h - 4], [x + 3, f - h + 2], [x + w, f - h - 2], [x + w - 1, f - h * 0.5], [x + w, f]], false) + " Z", fill: C.ink });
      });
    },
    bridge: function (B, f) {
      layer("far"); for (var i = 0; i < 6; i++) { trunk(B.x + B.w * (0.05 + i * 0.18), B.y + B.h + 20, B.h * 1.2, 5, 0, C.far); }
      layer("paint");
      var y = B.y + B.h * 0.6;
      el("path", { d: smooth([[B.x - 4, y], [B.x + B.w * 0.5, y + 7], [B.x + B.w + 4, y]], false), stroke: C.ink, "stroke-width": 4, fill: "none" });
      el("path", { d: smooth([[B.x - 4, y - 12], [B.x + B.w * 0.5, y - 4], [B.x + B.w + 4, y - 12]], false), stroke: C.ink, "stroke-width": 1.3, fill: "none" });
      for (var x = B.x + 10; x < B.x + B.w - 6; x += 16) { var sag = Math.sin((x - B.x) / B.w * Math.PI) * 7; el("path", { d: "M" + x + "," + (y + sag - 12 + sag * 0.1) + " L" + x + "," + (y + sag), stroke: C.ink, "stroke-width": 1 }); }
    },
    antlers: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.5, 22, C.far);
      layer("paint");
      ridge(B, f - 10, 14, C.mid, 5);
      for (var i = 0; i < 3; i++) {
        var x = B.x + B.w * (0.18 + i * 0.32), y = f - 12;
        el("path", { d: smooth([[x, y], [x - 2, y - 14], [x - 12, y - 26], [x - 18, y - 30]], false) + " M" + x + "," + y + smooth([[x, y], [x + 2, y - 14], [x + 12, y - 26], [x + 18, y - 30]], false).slice(1)
          + " M" + (x - 6) + "," + (y - 19) + " q-6,-2 -9,-8 M" + (x + 6) + "," + (y - 19) + " q6,-2 9,-8", stroke: C.ink, "stroke-width": 2.6, fill: "none", "stroke-linecap": "round" });
      }
    },
    stalactites: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.65, 8, C.far);
      layer("paint");
      var pts = [[B.x - 6, B.y - 6]];
      for (var i = 0; i <= 10; i++) {
        var x = B.x + B.w * i / 10, l = (i % 2) ? 6 + C.r() * B.h * 0.45 : 3;
        pts.push([x - 3, B.y + 2], [x, B.y + l], [x + 3, B.y + 2]);
      }
      pts.push([B.x + B.w + 6, B.y - 6]);
      el("path", { d: smooth(pts, true), fill: C.ink });
      for (var k = 0; k < 3; k++) { var sx = B.x + B.w * (0.2 + k * 0.3); el("path", { d: smooth([[sx - 7, f + 2], [sx - 2, f - 10], [sx, f - 18], [sx + 2, f - 10], [sx + 7, f + 2]], false), fill: C.ink }); }
    },
    arch: function (B, f) {
      layer("far"); for (var i = 0; i < 3; i++) { canopy(B.x + B.w * (0.2 + i * 0.3), B.y + B.h * 0.35, B.w * 0.16, B.h * 0.22, C.far); }
      layer("paint");
      var x = B.x + B.w * 0.4, w = B.w * 0.26, h = B.h * 0.8;
      el("path", { d: "M" + (x - w) + "," + f + " L" + (x - w) + "," + (f - h * 0.55) + " C" + (x - w) + "," + (f - h) + " " + (x + w * 0.4) + "," + (f - h * 1.05) + " " + (x + w * 0.55) + "," + (f - h * 0.8)
        + " C" + (x + w * 0.45) + "," + (f - h * 0.72) + " " + (x - w * 0.45) + "," + (f - h * 0.85) + " " + (x - w * 0.55) + "," + (f - h * 0.5) + " L" + (x - w * 0.55) + "," + f + " Z", fill: C.ink });
      el("path", { d: smooth([[x + w * 1.1, f], [x + w * 1.05, f - h * 0.35], [x + w * 1.15, f - h * 0.42], [x + w * 1.3, f - h * 0.3], [x + w * 1.3, f]], false) + " Z", fill: C.ink });
    },
    log: function (B, f) {
      layer("far"); for (var i = 0; i < 7; i++) { trunk(B.x + B.w * (0.05 + i * 0.15), f, B.h * 1.3, 4, 0, C.far); }
      layer("paint");
      el("path", { d: smooth([[B.x + B.w * 0.08, f - 2], [B.x + B.w * 0.25, f - 8], [B.x + B.w * 0.44, f - 15], [B.x + B.w * 0.45, f - 8], [B.x + B.w * 0.26, f - 1]], true), fill: C.ink });
      el("path", { d: smooth([[B.x + B.w * 0.6, f], [B.x + B.w * 0.7, f - 9], [B.x + B.w * 0.86, f - 7], [B.x + B.w * 0.9, f]], false) + " Z", fill: C.ink });
      layer(); el("ellipse", { cx: B.x + B.w * 0.445, cy: f - 11.5, rx: 2.5, ry: 4, fill: C.glow, opacity: 0.6 });
    },
    mushrooms: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.55, 16, C.far);
      layer("paint");
      var caps = [];
      for (var i = 0; i < 7; i++) {
        var x = B.x + 14 + i * (B.w - 28) / 6 + (C.r() - 0.5) * 10, h = 10 + C.r() * 26, s = 6 + C.r() * 10;
        el("path", { d: smooth([[x - 2, f], [x - 1, f - h * 0.5], [x - 1.5, f - h]], false) + " L" + (x + 1.5) + "," + (f - h) + " " + smooth([[x + 1.5, f - h], [x + 1, f - h * 0.5], [x + 2, f]], false).slice(1) + " Z", fill: C.ink });
        caps.push([x, f - h, s]);
      }
      layer();
      caps.forEach(function (c) {
        el("ellipse", { cx: c[0], cy: c[1], rx: c[2] * 2.2, ry: c[2] * 1.4, fill: C.glow, opacity: 0.25, filter: "url(#soft)" });
        el("path", { d: "M" + (c[0] - c[2]) + "," + c[1] + " C" + (c[0] - c[2]) + "," + (c[1] - c[2] * 0.9) + " " + (c[0] + c[2]) + "," + (c[1] - c[2] * 0.9) + " " + (c[0] + c[2]) + "," + c[1] + " Q" + c[0] + "," + (c[1] + 2) + " " + (c[0] - c[2]) + "," + c[1] + " Z", fill: mix(C.glow, "#ffffff", 0.15), opacity: 0.85 });
      });
    },
    pond: function (B, f) {
      layer("far"); for (var i = 0; i < 4; i++) { canopy(B.x + B.w * (0.12 + i * 0.26), B.y + B.h * 0.3, B.w * 0.15, B.h * 0.2, C.far); }
      layer("paint");
      var top = trunk(B.x + B.w * 0.84, f, B.h * 0.7, 6, -8, C.ink);
      bough(top[0], top[1] + 8, -18, -8, 3, C.ink);
      for (var k = 0; k < 7; k++) { var rx = B.x + B.w * 0.16 + k * 3.5; el("path", { d: smooth([[rx, f - 4], [rx + 1, f - 12 - k], [rx + 3, f - 20 - k * 1.5]], false), stroke: C.ink, "stroke-width": 1.2, fill: "none" }); }
      layer();
      el("ellipse", { cx: B.x + B.w * 0.5, cy: f - 4, rx: B.w * 0.32, ry: 5, fill: C.glow, opacity: 0.3, filter: "url(#soft)" });
      el("path", { d: smooth([[B.x + B.w * 0.2, f - 4], [B.x + B.w * 0.5, f - 6], [B.x + B.w * 0.8, f - 4]], false), stroke: mix(C.glow, "#ffffff", 0.3), "stroke-width": 1, fill: "none", opacity: 0.7 });
    },
    waterfall: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.6, 20, C.far);
      layer();
      var x = B.x + B.w * 0.34;
      el("rect", { x: x - 12, y: B.y, width: 24, height: f - B.y, fill: C.glow, opacity: 0.25, filter: "url(#soft)" });
      el("path", { d: smooth([[x - 4, B.y], [x - 5, B.y + (f - B.y) * 0.5], [x - 6, f]], false) + " L" + (x + 6) + "," + f + " " + smooth([[x + 6, f], [x + 5, B.y + (f - B.y) * 0.5], [x + 4, B.y]], false).slice(1) + " Z", fill: mix(C.glow, "#e8ffe0", 0.5), opacity: 0.55 });
      el("ellipse", { cx: x, cy: f - 2, rx: 24, ry: 5, fill: "#e8ffe0", opacity: 0.35, filter: "url(#soft)" });
      layer("paint");
      for (var i = 0; i < 5; i++) { var rx = B.x + B.w * (0.56 + i * 0.09); el("path", { d: smooth([[rx, B.y - 2], [rx + 5, B.y + 18], [rx - 2, B.y + 30 + C.r() * 30]], false), stroke: C.ink, "stroke-width": 2.6, fill: "none", "stroke-linecap": "round" }); }
    }    ,
    // ---------------- Гробниця
    altarhall: function (B, f) {
      layer("far"); arcade(B, 4, C.far);
      layer("paint");
      var x = B.x + B.w * 0.62;
      el("path", { d: smooth([[x - 26, f], [x - 24, f - 16], [x + 24, f - 16], [x + 26, f]], false) + " Z", fill: C.ink });
      for (var i = -2; i <= 2; i++) { el("rect", { x: x + i * 9 - 1.5, y: f - 16 - 8 - Math.abs(i) * 2, width: 3, height: 8 + Math.abs(i) * 2, fill: C.ink }); }
      layer(); for (var k = -2; k <= 2; k++) { mote(x + k * 9, f - 26 - Math.abs(k) * 2, 1.5); }
      el("ellipse", { cx: x, cy: f - 30, rx: 40, ry: 26, fill: C.glow, opacity: 0.12, filter: "url(#soft)" });
    },
    sarcophagi: function (B, f) {
      layer("far"); arcade(B, 5, C.far);
      layer("paint");
      [0.22, 0.5, 0.78].forEach(function (t) {
        var x = B.x + B.w * t, w = Math.min(34, B.w * 0.12);
        el("path", { d: smooth([[x - w, f], [x - w, f - 12], [x - w + 3, f - 15], [x + w - 3, f - 15], [x + w, f - 12], [x + w, f]], false) + " Z", fill: C.ink });
        el("path", { d: smooth([[x - w * 0.7, f - 15], [x - w * 0.3, f - 21], [x + w * 0.3, f - 21], [x + w * 0.7, f - 15]], false), fill: C.ink });
      });
    },
    niches: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.75, 6, C.far, 4);
      layer("paint");
      for (var row = 0; row < 2; row++) {
        for (var i = 0; i < 5; i++) {
          var x = B.x + B.w * (0.12 + i * 0.19), y = B.y + B.h * (0.3 + row * 0.3);
          el("path", { d: "M" + (x - 7) + "," + (y + 10) + " L" + (x - 7) + "," + y + " C" + (x - 7) + "," + (y - 9) + " " + (x + 7) + "," + (y - 9) + " " + (x + 7) + "," + y + " L" + (x + 7) + "," + (y + 10) + " Z", fill: C.ink });
          if ((i + row) % 2 === 0) { layer(); el("circle", { cx: x, cy: y + 4, r: 2.2, fill: mix(C.glow, "#ffffff", 0.2), opacity: 0.45 }); layer("paint"); }
        }
      }
    },
    bellshaft: function (B, f) {
      layer("far"); el("rect", { x: B.x + B.w * 0.25, y: B.y, width: B.w * 0.5, height: B.h, fill: C.far, rx: 4 });
      layer("paint");
      var x = B.x + B.w * 0.5, by = B.y + B.h * 0.32, s = Math.min(18, B.w * 0.2);
      el("path", { d: "M" + x + "," + B.y + " L" + x + "," + (by - s * 1.6), stroke: C.ink, "stroke-width": 2 });
      el("path", { d: smooth([[x - s, by], [x - s * 0.8, by - s], [x - s * 0.4, by - s * 1.6], [x + s * 0.4, by - s * 1.6], [x + s * 0.8, by - s], [x + s, by]], false) + " Z", fill: C.ink });
      el("path", { d: smooth([[x + 2, by], [x + 6, by + B.h * 0.25], [x + 3, f - 10]], false), stroke: C.ink, "stroke-width": 1.4, fill: "none" });
      for (var y = B.y + B.h * 0.55; y < f - 8; y += 28) { el("path", { d: smooth([[B.x - 2, y], [B.x + B.w * 0.3, y + 2]], false), stroke: C.ink, "stroke-width": 4, fill: "none" }); }
    },
    ossuary: function (B, f) {
      layer("far"); arcade(B, 3, C.far);
      layer("paint");
      ridge(B, f - 14, 16, C.mid, 9);
      layer(); for (var i = 0; i < 10; i++) { el("circle", { cx: B.x + C.r() * B.w, cy: f - 6 - C.r() * 18, r: 1.6 + C.r() * 1.4, fill: "#e8dcc3", opacity: 0.35 }); }
    },
    knight: function (B, f) {
      layer("far"); arcade(B, 3, C.far);
      layer("paint");
      var x = B.x + B.w * 0.55, h = B.h * 0.75;
      el("path", { d: smooth([[x - 16, f], [x - 12, f - h * 0.55], [x - 8, f - h * 0.8], [x - 4, f - h * 0.92], [x + 4, f - h * 0.92], [x + 8, f - h * 0.8], [x + 12, f - h * 0.55], [x + 16, f]], false) + " Z", fill: C.ink });
      el("path", { d: "M" + x + "," + (f - h * 0.86) + " L" + x + "," + (f - 4) + " M" + (x - 7) + "," + (f - h * 0.6) + " L" + (x + 7) + "," + (f - h * 0.6), stroke: mix(C.glow, C.ink, 0.6), "stroke-width": 2 });
      el("path", { d: smooth([[x - 22, f], [x - 22, f - 8], [x + 22, f - 8], [x + 22, f]], false) + " Z", fill: C.ink });
    },
    rubble: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.5, 14, C.far);
      layer("paint");
      el("path", { d: smooth([[B.x + B.w * 0.2, f + 4], [B.x + B.w * 0.35, f - B.h * 0.35], [B.x + B.w * 0.5, f - B.h * 0.5], [B.x + B.w * 0.62, f - B.h * 0.3], [B.x + B.w * 0.8, f + 4]], false) + " Z", fill: C.ink });
      el("path", { d: smooth([[B.x + B.w * 0.12, f], [B.x + B.w * 0.13, f - B.h * 0.6], [B.x + B.w * 0.17, f - B.h * 0.66], [B.x + B.w * 0.19, f]], false) + " Z", fill: C.ink });
      layer(); C.shaft(B.x + B.w * 0.45, B.y, B.w * 0.12);
    },
    chains: function (B, f) {
      layer("far"); arcade(B, 3, C.far);
      layer("paint");
      for (var i = 0; i < 4; i++) {
        var x = B.x + B.w * (0.18 + i * 0.22), l = B.h * (0.3 + C.r() * 0.35);
        el("path", { d: smooth([[x, B.y - 2], [x + 3, B.y + l * 0.5], [x, B.y + l]], false), stroke: C.ink, "stroke-width": 1.8, fill: "none", "stroke-dasharray": "3 2" });
      }
      var cx = B.x + B.w * 0.62, cy = B.y + B.h * 0.55;
      el("path", { d: "M" + cx + "," + B.y + " L" + cx + "," + (cy - 14), stroke: C.ink, "stroke-width": 1.6 });
      el("path", { d: smooth([[cx - 9, cy + 10], [cx - 10, cy - 6], [cx, cy - 14], [cx + 10, cy - 6], [cx + 9, cy + 10]], false) + " Z", fill: "none", stroke: C.ink, "stroke-width": 2 });
    },
    reliquary: function (B, f) {
      layer("far"); ridge(B, B.y + B.h * 0.5, 10, C.far);
      layer("paint");
      var x = B.x + B.w * 0.5;
      el("path", { d: smooth([[x - 12, f - 8], [x - 12, f - 18], [x - 8, f - 24], [x + 8, f - 24], [x + 12, f - 18], [x + 12, f - 8]], false) + " Z", fill: C.ink });
      layer(); el("ellipse", { cx: x, cy: f - 18, rx: 26, ry: 18, fill: C.glow, opacity: 0.3, filter: "url(#soft)" }); mote(x, f - 20, 2.2);
    },
    gatehall: function (B, f) {
      layer("far"); arcade(B, 6, C.far);
      layer("paint");
      var x = B.x + B.w * 0.5, w = B.w * 0.14, h = B.h * 0.7;
      el("path", { d: "M" + (x - w) + "," + f + " L" + (x - w) + "," + (f - h * 0.6) + " C" + (x - w) + "," + (f - h) + " " + (x + w) + "," + (f - h) + " " + (x + w) + "," + (f - h * 0.6) + " L" + (x + w) + "," + f + " Z", fill: C.ink });
      layer(); el("circle", { cx: x, cy: f - h * 0.5, r: w * 0.35, fill: "none", stroke: C.glow, "stroke-width": 1.6, opacity: 0.6 });
      layer("paint");
      [0.15, 0.85].forEach(function (t) { var sx = B.x + B.w * t; el("path", { d: smooth([[sx - 9, f], [sx - 7, f - B.h * 0.5], [sx, f - B.h * 0.62], [sx + 7, f - B.h * 0.5], [sx + 9, f]], false) + " Z", fill: C.ink }); });
    },
    // ---------------- Столиця
    ashroad: function (B, f) {
      layer("far"); skyline(B, 6, C.far);
      layer("paint");
      var cx = B.x + B.w * 0.3;
      el("path", { d: smooth([[cx - 22, f - 4], [cx - 4, f - 15], [cx + 18, f - 20], [cx + 20, f - 12], [cx - 2, f - 5]], true), fill: C.ink });
      el("circle", { cx: cx - 12, cy: f - 6, r: 8, fill: "none", stroke: C.ink, "stroke-width": 3 });
      brazier(B.x + B.w * 0.72, f);
    },
    citygate: function (B, f) {
      layer("far"); skyline(B, 4, C.far);
      layer("paint");
      var x = B.x + B.w * 0.45, w = B.w * 0.18, h = B.h * 0.8;
      el("path", { d: "M" + (x - w * 1.6) + "," + f + " L" + (x - w * 1.6) + "," + (f - h) + " L" + (x + w * 1.6) + "," + (f - h) + " L" + (x + w * 1.6) + "," + f + " L" + (x + w) + "," + f + " L" + (x + w) + "," + (f - h * 0.45) + " C" + (x + w) + "," + (f - h * 0.75) + " " + (x - w) + "," + (f - h * 0.75) + " " + (x - w) + "," + (f - h * 0.45) + " L" + (x - w) + "," + f + " Z", fill: C.ink });
      for (var i = -2; i <= 2; i++) { el("path", { d: "M" + (x + i * w * 0.38) + "," + (f - h * 0.62) + " L" + (x + i * w * 0.38) + "," + (f - h * 0.3), stroke: C.ink, "stroke-width": 2 }); }
    },
    market: function (B, f) {
      layer("far"); skyline(B, 7, C.far);
      layer("paint");
      [0.2, 0.5, 0.8].forEach(function (t, i) {
        var x = B.x + B.w * t, w = B.w * 0.1;
        el("path", { d: smooth([[x - w * 1.2, f - 18], [x, f - 26 - i * 3], [x + w * 1.2, f - 18]], false) + " Z", fill: C.ink });
        el("path", { d: "M" + (x - w) + "," + f + " L" + (x - w) + "," + (f - 18) + " M" + (x + w) + "," + f + " L" + (x + w) + "," + (f - 18), stroke: C.ink, "stroke-width": 2 });
      });
      el("path", { d: smooth([[B.x + B.w * 0.05, B.y + B.h * 0.42], [B.x + B.w * 0.3, B.y + B.h * 0.42]], false), stroke: C.ink, "stroke-width": 4, fill: "none" });
    },
    belltower: function (B, f) {
      layer("far"); skyline(B, 3, C.far);
      layer("paint");
      el("path", { d: smooth([[B.x + B.w * 0.2, f + 4], [B.x + B.w * 0.38, f - B.h * 0.45], [B.x + B.w * 0.5, f - B.h * 0.62], [B.x + B.w * 0.64, f - B.h * 0.4], [B.x + B.w * 0.82, f + 4]], false) + " Z", fill: C.ink });
      var x = B.x + B.w * 0.52, y = f - B.h * 0.62, s = 9;
      el("path", { d: smooth([[x - s, y], [x - s * 0.7, y - s], [x, y - s * 1.5], [x + s * 0.7, y - s], [x + s, y]], false) + " Z", fill: C.ink, transform: "rotate(-18 " + x + " " + y + ")" });
    },
    temple: function (B, f) {
      layer("far"); arcade(B, 5, C.far);
      layer("paint");
      var x = B.x + B.w * 0.55;
      el("path", { d: smooth([[x - B.w * 0.22, f], [x - B.w * 0.22, f - 12], [x + B.w * 0.22, f - 12], [x + B.w * 0.22, f]], false) + " Z", fill: C.ink });
      [-0.16, 0.16].forEach(function (t) { var cx = x + B.w * t; el("path", { d: smooth([[cx - 4, f - 12], [cx - 3, f - B.h * 0.6], [cx + 3, f - B.h * 0.6], [cx + 4, f - 12]], false) + " Z", fill: C.ink }); });
      brazier(x - B.w * 0.08, f - 12); brazier(x + B.w * 0.08, f - 12);
    },
    gardens: function (B, f) {
      layer("far"); for (var i = 0; i < 4; i++) { canopy(B.x + B.w * (0.12 + i * 0.26), B.y + B.h * 0.4, B.w * 0.12, B.h * 0.2, C.far); }
      layer("paint");
      [0.18, 0.46, 0.74].forEach(function (t) { var top = trunk(B.x + B.w * t, f, B.h * 0.6, 5, (C.r() - 0.5) * 10, C.ink); bough(top[0], top[1] + 8, -16, -8, 3, C.ink); bough(top[0], top[1] + 12, 14, -10, 3, C.ink); });
      el("path", { d: smooth([[B.x + B.w * 0.55, B.y + B.h * 0.38], [B.x + B.w * 0.9, B.y + B.h * 0.38]], false), stroke: C.ink, "stroke-width": 4, fill: "none" });
      layer(); for (var k = 0; k < 6; k++) { mote(B.x + C.r() * B.w, B.y + B.h * (0.2 + C.r() * 0.5), 1.1); }
    },
    approach: function (B, f) {
      layer("far"); skyline(B, 5, C.far);
      layer("paint");
      [0.12, 0.36, 0.64, 0.88].forEach(function (t) {
        var x = B.x + B.w * t; el("path", { d: smooth([[x - 6, f], [x - 5, f - B.h * 0.7], [x + 5, f - B.h * 0.7], [x + 6, f]], false) + " Z", fill: C.ink });
      });
      layer(); [0.24, 0.76].forEach(function (t) { var x = B.x + B.w * t; el("path", { d: "M" + (x - 6) + "," + (B.y + B.h * 0.2) + " l12,0 l0,24 l-6,-5 l-6,5 Z", fill: "#8f1d2c", opacity: 0.85 }); });
    },
    throne: function (B, f) {
      layer("far"); arcade(B, 5, C.far);
      layer("paint");
      var x = B.x + B.w * 0.68;
      el("path", { d: smooth([[x - 22, f], [x - 22, f - 10], [x + 22, f - 10], [x + 22, f]], false) + " Z", fill: C.ink });
      el("path", { d: "M" + (x - 12) + "," + (f - 10) + " L" + (x - 12) + "," + (f - B.h * 0.55) + " L" + (x - 6) + "," + (f - B.h * 0.62) + " L" + x + "," + (f - B.h * 0.7) + " L" + (x + 6) + "," + (f - B.h * 0.62) + " L" + (x + 12) + "," + (f - B.h * 0.55) + " L" + (x + 12) + "," + (f - 10) + " Z", fill: C.ink });
      layer(); el("path", { d: "M" + (B.x + B.w * 0.2) + "," + (B.y + B.h * 0.15) + " l14,0 l0,34 l-7,-6 l-7,6 Z M" + (B.x + B.w * 0.4) + "," + (B.y + B.h * 0.15) + " l14,0 l0,34 l-7,-6 l-7,6 Z", fill: "#8f1d2c", opacity: 0.85 });
      el("ellipse", { cx: x, cy: f - B.h * 0.4, rx: 30, ry: 40, fill: C.glow, opacity: 0.12, filter: "url(#soft)" });
    }
  };

  // Ряд стрілчастих арок у глибині — склепіння.
  function arcade(B, n, fill) {
    var d = "M" + (B.x - 4) + "," + (B.y + B.h + 4) + " L" + (B.x - 4) + "," + B.y + " L" + (B.x + B.w + 4) + "," + B.y + " L" + (B.x + B.w + 4) + "," + (B.y + B.h + 4) + " Z";
    var w = B.w / n;
    for (var i = 0; i < n; i++) {
      var x0 = B.x + i * w + w * 0.18, x1 = B.x + (i + 1) * w - w * 0.18, top = B.y + B.h * 0.18;
      d += " M" + x0 + "," + (B.y + B.h + 4) + " L" + x0 + "," + (top + w * 0.4) + " C" + x0 + "," + top + " " + x1 + "," + top + " " + x1 + "," + (top + w * 0.4) + " L" + x1 + "," + (B.y + B.h + 4) + " Z";
    }
    el("path", { d: d, fill: fill, "fill-rule": "evenodd", opacity: 0.85 });
  }

  // Обрис міста в серпанку — дахи, шпилі, вежі.
  function skyline(B, n, fill) {
    var pts = [[B.x - 6, B.y + B.h + 6], [B.x - 6, B.y + B.h * 0.7]];
    for (var i = 0; i < n; i++) {
      var x = B.x + B.w * (i + 0.5) / n, h = B.h * (0.25 + C.r() * 0.35);
      pts.push([x - B.w / n * 0.4, B.y + B.h * 0.72], [x - B.w / n * 0.35, B.y + B.h * 0.72 - h * 0.6], [x, B.y + B.h * 0.72 - h], [x + B.w / n * 0.35, B.y + B.h * 0.72 - h * 0.6], [x + B.w / n * 0.4, B.y + B.h * 0.72]);
    }
    pts.push([B.x + B.w + 6, B.y + B.h * 0.7], [B.x + B.w + 6, B.y + B.h + 6]);
    el("path", { d: smooth(pts, true), fill: fill, opacity: 0.85 });
  }

  // Жаровня: чаша на нозі й вогонь над нею.
  function brazier(x, f) {
    el("path", { d: smooth([[x - 2, f], [x - 1.5, f - 12], [x - 8, f - 14], [x - 7, f - 18], [x + 7, f - 18], [x + 8, f - 14], [x + 1.5, f - 12], [x + 2, f]], false) + " Z", fill: C.ink });
    var keep = C.layer; layer();
    el("ellipse", { cx: x, cy: f - 24, rx: 14, ry: 16, fill: "#ff9c4a", opacity: 0.3, filter: "url(#soft)" });
    el("path", { d: smooth([[x - 5, f - 18], [x - 3, f - 26], [x, f - 31], [x + 3, f - 25], [x + 5, f - 18]], false) + " Z", fill: "#ffb35a", opacity: 0.85 });
    C.layer = keep;
  }

  // Барви зон для кімнат: темне склепіння, середній тон, чорнило силуетів, світло.
  var PALETTES = {
    tomb: { dark: "#0d131b", mid: "#212d3a", ink: "#04070b", glow: "#f0d9a0" },
    forest: { dark: "#123029", mid: "#2a5a4e", ink: "#040908", glow: "#a8ff5a" },
    capital: { dark: "#14120f", mid: "#2c2822", ink: "#070605", glow: "#ffd27a" },
    boss: { dark: "#2a0d10", mid: "#5a1a20", ink: "#120406", glow: "#ff5a5a" }
  };

  // Мотив кожної кімнати наявних зон — свій, без повторів у межах зони.
  var ROOM_MOTIFS = {
    room_tomb_entrance: "altarhall", room_tomb_burial_hall: "sarcophagi", room_tomb_guard_passage: "niches",
    room_tomb_collapsed_crypt: "rubble", room_tomb_guardian_approach: "knight", room_tomb_guardian_arena: "gatehall",
    room_tomb_royal_ossuary: "ossuary", room_tomb_bell_shaft: "bellshaft", room_tomb_mason_cache: "chains",
    room_tomb_reliquary: "reliquary",
    room_forest_root_tunnel: "roots", room_forest_entrance: "gate", room_forest_royal_road: "road",
    room_forest_broken_arch: "arch", room_forest_poison_grove: "thorns", room_forest_watchtower: "tower",
    room_forest_watchtower_top: "columns", room_forest_deep: "vines", room_forest_warden_approach: "bridge",
    room_forest_warden_arena: "antlers", room_forest_far_gate: "log", room_forest_thorn_course: "mushrooms",
    room_capital_ash_road: "ashroad", room_capital_city_gate: "citygate", room_capital_market: "market",
    room_capital_bell_tower: "belltower", room_capital_temple: "temple", room_capital_gardens: "gardens",
    room_capital_palace_approach: "approach", room_capital_throne_hall: "throne"
  };

  window.RoomArt = {
    palettes: PALETTES,
    motifFor: function (areaId) { return ROOM_MOTIFS[areaId] || ""; },
    has: function (motif) { return !!MOTIFS[motif]; },
    /** Ціла кімната для картинки гри: склепіння-градієнт і сцена. */
    paint: function (svg, B, motif, palette, r) {
      setupDefs(svg);
      var gid = "base" + Math.round(B.w) + "_" + Math.round(B.h);
      var grad = el("linearGradient", { id: gid, x1: 0, y1: 0, x2: 0, y2: 1 }, svg.querySelector("defs"));
      el("stop", { offset: "0", "stop-color": palette.dark }, grad);
      el("stop", { offset: "1", "stop-color": palette.mid }, grad);
      el("rect", { x: B.x, y: B.y, width: B.w, height: B.h, fill: "url(#" + gid + ")" }, svg);
      var g = el("g", {}, svg);
      return window.RoomArt.draw(g, B, motif, palette, r, svg);
    },
    draw: function (g, B, motif, tint, r, svg) {
      setupDefs(svg);
      var id = "clip" + Math.round(B.x) + "_" + Math.round(B.y);
      var clip = el("clipPath", { id: id }, svg.querySelector("defs"));
      el("rect", { x: B.x, y: B.y, width: B.w, height: B.h }, clip);
      var root = el("g", { "clip-path": "url(#" + id + ")" }, g);
      C = { root: root, layer: root, r: r, ink: tint.ink, glow: tint.glow,
        far: mix(tint.mid, tint.glow, 0.12), mid: mix(tint.ink, tint.mid, 0.45) };
      // Світло кімнати: м'яка пляма зверху, звідки воно падає.
      var lx = B.x + B.w * (0.25 + r() * 0.5);
      el("ellipse", { cx: lx, cy: B.y + B.h * 0.25, rx: B.w * 0.5, ry: B.h * 0.55, fill: mix(tint.mid, tint.glow, 0.28), opacity: 0.32, filter: "url(#soft)" });
      C.shaft = function (x, y, w) {
        el("path", { d: "M" + x + "," + y + " L" + (x + w) + "," + y + " L" + (x + w * 1.8) + "," + (B.y + B.h) + " L" + (x + w * 0.6) + "," + (B.y + B.h) + " Z", fill: mix(tint.glow, "#ffffff", 0.4), opacity: 0.08, filter: "url(#soft)" });
      };
      C.shaft(lx - B.w * 0.08, B.y, B.w * 0.12);
      var floor = B.y + B.h - 6 - r() * 4;
      if (MOTIFS[motif]) { MOTIFS[motif](B, floor); }
      // Ближній план: плавна підлога з уступами, темна, з м'яким краєм.
      layer("paint");
      ridge(B, floor + 2, 7, C.ink, 6);
      // Туман над підлогою, зерно, віньєтка.
      layer();
      el("ellipse", { cx: B.x + B.w * 0.5, cy: floor, rx: B.w * 0.55, ry: 7, fill: mix(tint.glow, "#ffffff", 0.2), opacity: 0.12, filter: "url(#soft)" });
      el("rect", { x: B.x, y: B.y, width: B.w, height: B.h, filter: "url(#grain)", opacity: 0.18 });
      var vid = "vig" + id;
      var vg = el("radialGradient", { id: vid, cx: "50%", cy: "45%", r: "75%" }, svg.querySelector("defs"));
      el("stop", { offset: "0.55", "stop-color": "#000", "stop-opacity": 0 }, vg);
      el("stop", { offset: "1", "stop-color": "#000", "stop-opacity": 0.55 }, vg);
      el("rect", { x: B.x, y: B.y, width: B.w, height: B.h, fill: "url(#" + vid + ")" });
      return floor;
    }
  };
})();
