/* Тінь Забутого Королівства — силуети зон для карти світу.
   Кожна зона — своя ілюстрація з деталей: орієнтир (собор, палац, велике дерево, кристали…),
   будинки з вікнами, що світяться, контрфорси, дахи, дерева, а поверх — підсвітка кольором зони.
   Три плани: дальній (світліший, у серпанку), середній, ближній (найтемніший).
   Ці ж правила гра використає для процедурних островів, доки художник не намалює карту. */
(function () {
  "use strict";
  var NS = "http://www.w3.org/2000/svg";
  var C; // контекст поточного острова

  function el(name, attrs, parent) {
    var node = document.createElementNS(NS, name);
    for (var key in attrs) { node.setAttribute(key, attrs[key]); }
    (parent || C.g).appendChild(node);
    return node;
  }

  function mix(a, b, t) {
    var pa = parseInt(a.slice(1), 16), pb = parseInt(b.slice(1), 16);
    var r = Math.round(((pa >> 16) & 255) * (1 - t) + ((pb >> 16) & 255) * t);
    var g = Math.round(((pa >> 8) & 255) * (1 - t) + ((pb >> 8) & 255) * t);
    var bl = Math.round((pa & 255) * (1 - t) + (pb & 255) * t);
    return "#" + ((1 << 24) + (r << 16) + (g << 8) + bl).toString(16).slice(1);
  }

  // Колір плану: дальній — у серпанку кольору зони, ближній — майже чорний.
  function fill(layer) {
    if (C.faded) { return ["#232120", "#1b1a19", "#141312"][layer]; }
    return [mix("#1d1b19", C.tint, 0.22), "#141210", "#0b0a09"][layer];
  }

  function shape(d, layer, rim) {
    var p = el("path", { d: d, fill: fill(layer) });
    if (!C.faded && rim !== false) {
      p.setAttribute("stroke", C.tint);
      p.setAttribute("stroke-opacity", layer === 0 ? 0.25 : 0.55);
      p.setAttribute("stroke-width", layer === 2 ? 0.9 : 1.1);
      p.setAttribute("stroke-linejoin", "round");
    }
    return p;
  }

  function light(x, y, rr, colour) {
    if (C.faded) { return; }
    el("circle", { cx: x, cy: y, r: rr * 3, fill: colour || C.glow, opacity: 0.18 });
    el("circle", { cx: x, cy: y, r: rr, fill: colour || C.glow, opacity: 0.95 });
  }

  // Стрілчасте вікно (ланцет), світиться не завжди.
  function lancet(x, y, w, h, lit) {
    var d = "M" + (x - w / 2) + "," + y + " L" + (x - w / 2) + "," + (y - h + w / 2) + " Q" + (x - w / 2) + "," + (y - h) + " " + x + "," + (y - h - w * 0.3)
      + " Q" + (x + w / 2) + "," + (y - h) + " " + (x + w / 2) + "," + (y - h + w / 2) + " L" + (x + w / 2) + "," + y + " Z";
    if (C.faded) { el("path", { d: d, fill: "#0d0c0b" }); return; }
    if (lit) { el("path", { d: d, fill: C.glow, opacity: 0.22, filter: "url(#blur3)" }); el("path", { d: d, fill: mix(C.glow, "#3a2a14", 0.25), opacity: 0.8 }); }
    else { el("path", { d: d, fill: "#060505" }); }
  }

  function rose(x, y, rr) {
    if (C.faded) { el("circle", { cx: x, cy: y, r: rr, fill: "#0d0c0b" }); return; }
    el("circle", { cx: x, cy: y, r: rr * 2.2, fill: C.glow, opacity: 0.16 });
    el("circle", { cx: x, cy: y, r: rr, fill: C.glow, opacity: 0.85 });
    for (var i = 0; i < 8; i++) {
      var a = i * Math.PI / 4;
      el("line", { x1: x, y1: y, x2: x + Math.cos(a) * rr, y2: y + Math.sin(a) * rr, stroke: "#120f0c", "stroke-width": 0.9 });
    }
    el("circle", { cx: x, cy: y, r: rr * 0.3, fill: "#120f0c" });
  }

  // ------------------------------------------------------------- будівлі

  // Вежа: тіло, шпиль із пінаклями, вікна; варіанти — зубці замість шпиля.
  function tower(x, b, w, h, layer, o) {
    o = o || {};
    var spire = o.spire === undefined ? w * 1.6 : o.spire;
    var d = "M" + (x - w / 2) + "," + b + " L" + (x - w / 2) + "," + (b - h);
    if (o.battlement) {
      var n = Math.max(3, Math.round(w / 5));
      for (var i = 0; i < n; i++) {
        var x0 = x - w / 2 + i * w / n, x1 = x0 + w / n;
        d += " L" + x0 + "," + (b - h - (i % 2 === 0 ? 5 : 0)) + " L" + x1 + "," + (b - h - (i % 2 === 0 ? 5 : 0));
      }
      d += " L" + (x + w / 2) + "," + (b - h);
    } else {
      // Пінаклі по кутах і шпиль.
      d += " L" + (x - w / 2 - 1) + "," + (b - h - w * 0.5) + " L" + (x - w / 2 + 2) + "," + (b - h)
        + " L" + (x - w * 0.22) + "," + (b - h) + " L" + x + "," + (b - h - spire) + " L" + (x + w * 0.22) + "," + (b - h)
        + " L" + (x + w / 2 - 2) + "," + (b - h) + " L" + (x + w / 2 + 1) + "," + (b - h - w * 0.5) + " L" + (x + w / 2) + "," + (b - h);
    }
    d += " L" + (x + w / 2) + "," + b + " Z";
    shape(d, layer);
    // Хрест/флюгер.
    if (!o.battlement && o.cross !== false && !C.faded) {
      el("line", { x1: x, y1: b - h - spire, x2: x, y2: b - h - spire - 6, stroke: C.tint, "stroke-width": 1, "stroke-opacity": 0.8 });
    }
    // Ряд вікон.
    var rows = Math.max(1, Math.floor(h / 26));
    for (var k = 0; k < rows; k++) {
      var wy = b - 10 - k * 24;
      if (w > 16) { lancet(x - w * 0.2, wy, w * 0.16, 12, C.r() < 0.55); lancet(x + w * 0.2, wy, w * 0.16, 12, C.r() < 0.55); }
      else { lancet(x, wy, Math.max(2.5, w * 0.3), 10, C.r() < 0.6); }
    }
    if (o.rose) { rose(x, b - h + 12, w * 0.22); }
  }

  // Собор: неф із фронтоном, дві вежі зі шпилями, розетка, контрфорси.
  function cathedral(x, b, w, h, layer) {
    var nave = "M" + (x - w * 0.36) + "," + b + " L" + (x - w * 0.36) + "," + (b - h * 0.55) + " L" + x + "," + (b - h * 0.78)
      + " L" + (x + w * 0.36) + "," + (b - h * 0.55) + " L" + (x + w * 0.36) + "," + b + " Z";
    // Контрфорси — косі опори з боків.
    for (var s = -1; s <= 1; s += 2) {
      for (var i = 0; i < 2; i++) {
        var bx = x + s * (w * 0.4 + i * w * 0.08);
        shape("M" + bx + "," + b + " L" + bx + "," + (b - h * (0.32 - i * 0.08)) + " L" + (bx - s * w * 0.06) + "," + (b - h * (0.48 - i * 0.08)) + " L" + (bx - s * w * 0.06) + "," + b + " Z", layer);
      }
    }
    shape(nave, layer);
    rose(x, b - h * 0.5, w * 0.08);
    lancet(x - w * 0.18, b - 6, w * 0.07, h * 0.28, true);
    lancet(x + w * 0.18, b - 6, w * 0.07, h * 0.28, C.r() < 0.7);
    // Портал.
    lancet(x, b, w * 0.12, h * 0.22, false);
    tower(x - w * 0.36, b, w * 0.18, h * 0.82, layer, { spire: h * 0.45 });
    tower(x + w * 0.36, b, w * 0.18, h * 0.82, layer, { spire: h * 0.45 });
  }

  function house(x, b, w, h, layer, o) {
    o = o || {};
    var roof = h * 0.7;
    var d = "M" + (x - w / 2) + "," + b + " L" + (x - w / 2) + "," + (b - h) + " L" + (x - w / 2 - 3) + "," + (b - h)
      + " L" + x + "," + (b - h - roof) + " L" + (x + w / 2 + 3) + "," + (b - h) + " L" + (x + w / 2) + "," + (b - h) + " L" + (x + w / 2) + "," + b + " Z";
    if (o.broken) { d = "M" + (x - w / 2) + "," + b + " L" + (x - w / 2) + "," + (b - h) + " L" + (x - w / 2 - 3) + "," + (b - h) + " L" + (x - w * 0.1) + "," + (b - h - roof * 0.8)
      + " L" + (x + w * 0.05) + "," + (b - h - roof * 0.4) + " L" + (x + w * 0.2) + "," + (b - h - roof * 0.55) + " L" + (x + w / 2) + "," + (b - h) + " L" + (x + w / 2) + "," + b + " Z"; }
    shape(d, layer);
    if (o.chimney) { shape("M" + (x + w * 0.2) + "," + (b - h - roof * 0.4) + " l0,-" + (roof * 0.6) + " l5,0 l0," + (roof * 0.3) + " Z", layer); }
    // Фахверк і вікно.
    if (!C.faded && w > 10) {
      el("path", { d: "M" + (x - w / 2) + "," + (b - h * 0.5) + " L" + (x + w / 2) + "," + (b - h * 0.5) + " M" + (x - w / 2) + "," + (b - h) + " L" + x + "," + (b - h * 0.5) + " L" + (x + w / 2) + "," + (b - h),
        stroke: "#000", "stroke-width": 1, fill: "none", opacity: 0.7 });
    }
    var lit = o.lit === undefined ? C.r() < 0.6 : o.lit;
    if (!C.faded) {
      el("rect", { x: x - w * 0.15, y: b - h * 0.42, width: w * 0.3, height: h * 0.28, fill: lit ? C.glow : "#060505", opacity: lit ? 0.9 : 1 });
      if (lit) { el("rect", { x: x - w * 0.3, y: b - h * 0.6, width: w * 0.6, height: h * 0.6, fill: C.glow, opacity: 0.12 }); }
    }
  }

  function dome(x, b, w, h, layer, telescope) {
    var drum = h * 0.45;
    shape("M" + (x - w / 2) + "," + b + " L" + (x - w / 2) + "," + (b - drum) + " A" + (w / 2) + "," + (h * 0.55) + " 0 0 1 " + (x + w / 2) + "," + (b - drum) + " L" + (x + w / 2) + "," + b + " Z", layer);
    if (!C.faded) {
      // Ребра купола.
      for (var i = -1; i <= 1; i++) {
        el("path", { d: "M" + (x + i * w * 0.22) + "," + (b - drum) + " Q" + (x + i * w * 0.18) + "," + (b - drum - h * 0.4) + " " + x + "," + (b - drum - h * 0.55), stroke: C.tint, "stroke-opacity": 0.5, "stroke-width": 0.8, fill: "none" });
      }
      el("path", { d: "M" + (x - w * 0.08) + "," + (b - drum - h * 0.5) + " l" + (w * 0.16) + ",0", stroke: C.glow, "stroke-width": 2, opacity: 0.9 });
    }
    lancet(x - w * 0.25, b - 4, w * 0.08, drum * 0.6, true);
    lancet(x + w * 0.25, b - 4, w * 0.08, drum * 0.6, C.r() < 0.6);
    if (telescope) {
      shape("M" + (x + w * 0.05) + "," + (b - drum - h * 0.35) + " l" + (w * 0.55) + ",-" + (h * 0.45) + " l4,6 l-" + (w * 0.52) + "," + (h * 0.45) + " Z", layer);
    }
  }

  // Дерево: стовбур і гілки рекурсивно; «crown» — розлога крона (осінній ліс), «dead» — голе.
  function tree(x, b, h, layer, style) {
    var parts = [];
    function branch(x0, y0, len, ang, width, depth) {
      var x1 = x0 + Math.cos(ang) * len, y1 = y0 - Math.sin(ang) * len;
      parts.push("M" + (x0 - width / 2) + "," + y0 + " L" + (x1 - width * 0.3) + "," + y1 + " L" + (x1 + width * 0.3) + "," + y1 + " L" + (x0 + width / 2) + "," + y0 + " Z");
      if (depth <= 0) { return [x1, y1]; }
      var tips = [];
      var n = depth > 2 ? 2 : 3;
      for (var i = 0; i < n; i++) {
        var a = ang + (i - (n - 1) / 2) * (0.55 + C.r() * 0.3);
        tips = tips.concat([branch(x1, y1, len * (0.62 + C.r() * 0.15), a, width * 0.6, depth - 1)]);
      }
      return tips;
    }
    var tips = branch(x, b, h * 0.42, Math.PI / 2 + (C.r() - 0.5) * 0.2, Math.max(3, h * 0.07), 3);
    shape(parts.join(" "), layer, style !== "dead");
    if (style === "crown" || style === "glow") {
      // Крона з кількох округлих мас.
      var masses = "";
      for (var k = 0; k < 6; k++) {
        var mx = x + (C.r() - 0.5) * h * 0.9, my = b - h * (0.62 + C.r() * 0.35), rr = h * (0.14 + C.r() * 0.12);
        masses += "M" + (mx - rr) + "," + my + " a" + rr + "," + (rr * 0.8) + " 0 1 0 " + (rr * 2) + ",0 a" + rr + "," + (rr * 0.8) + " 0 1 0 -" + (rr * 2) + ",0 ";
      }
      var crown = shape(masses, layer, false);
      if (!C.faded) {
        crown.setAttribute("fill", style === "glow" ? mix(fill(layer), C.tint, 0.25) : mix(fill(layer), C.tint, 0.35));
        for (var j = 0; j < 9; j++) { light(x + (C.r() - 0.5) * h * 0.8, b - h * (0.55 + C.r() * 0.4), 1.1, C.glow); }
      }
    }
    if (style === "moss" && !C.faded) {
      for (var m = 0; m < 6; m++) {
        var t = flat(tips)[m % flat(tips).length] || [x, b - h];
        el("path", { d: "M" + t[0] + "," + t[1] + " q2," + (8 + C.r() * 10) + " -1," + (16 + C.r() * 12), stroke: C.tint, "stroke-opacity": 0.6, "stroke-width": 1, fill: "none" });
      }
    }
  }

  function flat(list) { var out = []; (function walk(x) { if (typeof x[0] === "number") { out.push(x); } else { x.forEach(walk); } })(list); return out; }

  function crystal(x, b, w, h, layer, lean) {
    lean = lean || 0;
    var tipX = x + lean;
    var d = "M" + (x - w / 2) + "," + b + " L" + (x - w * 0.4 + lean * 0.6) + "," + (b - h * 0.75) + " L" + tipX + "," + (b - h) + " L" + (x + w * 0.4 + lean * 0.6) + "," + (b - h * 0.75) + " L" + (x + w / 2) + "," + b + " Z";
    var p = shape(d, layer);
    if (!C.faded) {
      p.setAttribute("fill", mix(fill(layer), C.tint, layer === 0 ? 0.35 : 0.5));
      // Світла грань.
      el("path", { d: "M" + tipX + "," + (b - h) + " L" + (x + w * 0.4 + lean * 0.6) + "," + (b - h * 0.75) + " L" + (x + w / 2) + "," + b + " L" + (x + w * 0.05) + "," + b + " Z", fill: C.glow, opacity: 0.45 });
      el("path", { d: "M" + tipX + "," + (b - h) + " L" + (x + w * 0.05) + "," + b, stroke: "#ffffff", "stroke-opacity": 0.5, "stroke-width": 0.8 });
    }
  }

  function arches(x0, x1, b, h, layer, n) {
    var w = (x1 - x0) / n;
    var d = "M" + x0 + "," + b + " L" + x0 + "," + (b - h) + " L" + x1 + "," + (b - h) + " L" + x1 + "," + b;
    for (var i = n - 1; i >= 0; i--) {
      var ax = x0 + i * w;
      d += " L" + (ax + w * 0.85) + "," + b + " L" + (ax + w * 0.85) + "," + (b - h * 0.45) + " Q" + (ax + w / 2) + "," + (b - h * 0.85) + " " + (ax + w * 0.15) + "," + (b - h * 0.45) + " L" + (ax + w * 0.15) + "," + b;
    }
    shape(d + " Z", layer);
  }

  function wall(x0, x1, b, h, layer) {
    var d = "M" + x0 + "," + b + " L" + x0 + "," + (b - h);
    for (var x = x0; x < x1; x += 8) { d += " L" + x + "," + (b - h - 5) + " L" + Math.min(x1, x + 4) + "," + (b - h - 5) + " L" + Math.min(x1, x + 4) + "," + (b - h) + " L" + Math.min(x1, x + 8) + "," + (b - h); }
    shape(d + " L" + x1 + "," + b + " Z", layer);
  }

  function column(x, b, h, w, layer, broken) {
    var top = broken ? "L" + (x + w / 2) + "," + (b - h * 0.8) + " L" + x + "," + (b - h) + " L" + (x - w / 2) + "," + (b - h * 0.9) : "L" + (x + w / 2 + 2) + "," + (b - h) + " L" + (x - w / 2 - 2) + "," + (b - h);
    shape("M" + (x - w / 2) + "," + b + " L" + (x + w / 2) + "," + b + " " + top + " Z", layer);
  }

  function statue(x, b, h, layer, wings) {
    var d = "M" + (x - h * 0.12) + "," + b + " L" + (x - h * 0.1) + "," + (b - h * 0.65) + " L" + (x - h * 0.06) + "," + (b - h * 0.85) + " A" + (h * 0.07) + "," + (h * 0.07) + " 0 1 1 " + (x + h * 0.06) + "," + (b - h * 0.85)
      + " L" + (x + h * 0.1) + "," + (b - h * 0.65) + " L" + (x + h * 0.12) + "," + b + " Z";
    if (wings) { d += " M" + (x - h * 0.06) + "," + (b - h * 0.75) + " Q" + (x - h * 0.5) + "," + (b - h * 1.1) + " " + (x - h * 0.4) + "," + (b - h * 0.4) + " Z M" + (x + h * 0.06) + "," + (b - h * 0.75) + " Q" + (x + h * 0.5) + "," + (b - h * 1.1) + " " + (x + h * 0.4) + "," + (b - h * 0.4) + " Z"; }
    shape(d, layer);
  }

  function graves(x0, x1, b, layer) {
    var d = "";
    for (var x = x0; x < x1; x += 7 + C.r() * 6) {
      var h = 5 + C.r() * 6;
      d += C.r() < 0.5 ? "M" + x + "," + b + " l0,-" + h + " a3,3 0 0 1 6,0 l0," + h + " Z " : "M" + (x + 2) + "," + b + " l0,-" + (h + 3) + " l-2,0 l0,-2 l2,0 l0,-2 l2,0 l0,2 l2,0 l0,2 l-2,0 l0," + (h + 3) + " Z ";
    }
    shape(d, layer, false);
  }

  function mountain(x, b, w, h, snow) {
    shape("M" + (x - w / 2) + "," + b + " L" + (x - w * 0.15) + "," + (b - h * 0.7) + " L" + x + "," + (b - h) + " L" + (x + w * 0.2) + "," + (b - h * 0.65) + " L" + (x + w / 2) + "," + b + " Z", 0);
    if (snow && !C.faded) {
      el("path", { d: "M" + (x - w * 0.15) + "," + (b - h * 0.7) + " L" + x + "," + (b - h) + " L" + (x + w * 0.2) + "," + (b - h * 0.65) + " L" + (x + w * 0.08) + "," + (b - h * 0.72) + " L" + x + "," + (b - h * 0.62) + " L" + (x - w * 0.08) + "," + (b - h * 0.74) + " Z", fill: "#e8f2ff", opacity: 0.75 });
    }
  }

  function snowcaps(x0, x1, y) {
    if (C.faded) { return; }
    el("path", { d: "M" + x0 + "," + y + " Q" + ((x0 + x1) / 2) + "," + (y - 4) + " " + x1 + "," + y + " L" + x1 + "," + (y + 2) + " L" + x0 + "," + (y + 2) + " Z", fill: "#eef6ff", opacity: 0.8 });
  }

  function hut(x, b, w, h, layer) {
    var legs = "";
    for (var i = -1; i <= 1; i++) { legs += "M" + (x + i * w * 0.4 - 1) + "," + b + " l0,-" + (h * 0.5) + " l2,0 l0," + (h * 0.5) + " Z "; }
    shape(legs, layer, false);
    house(x, b - h * 0.5, w, h * 0.5, layer, { lit: true });
  }

  function fall(x, top, bottom, w, colour) {
    if (C.faded) { return; }
    el("rect", { x: x - w * 1.5, y: top, width: w * 3, height: bottom - top, fill: colour, opacity: 0.18, filter: "url(#blur3)" });
    el("rect", { x: x - w / 2, y: top, width: w, height: bottom - top, fill: colour, opacity: 0.8 });
  }

  function bell(x, y, s) {
    shape("M" + (x - s) + "," + y + " Q" + (x - s) + "," + (y - s * 1.6) + " " + x + "," + (y - s * 1.7) + " Q" + (x + s) + "," + (y - s * 1.6) + " " + (x + s) + "," + y + " Z", 1);
  }

  function shard(x, y, w, h, layer) {
    shape("M" + x + "," + (y - h) + " L" + (x + w / 2) + "," + y + " L" + x + "," + (y + h * 0.4) + " L" + (x - w / 2) + "," + y + " Z", layer);
  }

  // ------------------------------------------------------------- зони
  // half — половина ширини плато, b — його верх. Кожна — свій орієнтир і впізнаваний рядок будинків.
  var ZONES = {
    tomb: function (h, b) {
      tower(-h * 0.55, b, 22, 70, 0, { spire: 30 });
      graves(-h * 0.85, h * 0.85, b, 1);
      // Мавзолей із колонами й фронтоном.
      shape("M" + (-h * 0.35) + "," + b + " L" + (-h * 0.35) + "," + (b - 46) + " L0," + (b - 74) + " L" + (h * 0.35) + "," + (b - 46) + " L" + (h * 0.35) + "," + b + " Z", 1);
      for (var i = -2; i <= 2; i++) { column(i * h * 0.12, b, 42, 6, 2); }
      lancet(0, b, 14, 32, true);
      statue(-h * 0.62, b, 40, 2, false);
      statue(h * 0.62, b, 40, 2, false);
      tower(h * 0.42, b, 16, 52, 1, { spire: 26 });
      column(h * 0.8, b, 28, 7, 2, true);
    },
    forest: function (h, b) {
      tree(-h * 0.55, b, 90, 0, "crown");
      tree(h * 0.6, b, 80, 0, "crown");
      // Велике дерево-орієнтир.
      tree(-h * 0.05, b, 135, 1, "crown");
      arches(h * 0.2, h * 0.8, b, 30, 1, 3);
      tree(-h * 0.75, b, 60, 2, "dead");
      tree(h * 0.45, b, 70, 2, "crown");
      column(-h * 0.35, b, 34, 8, 2, true);
    },
    village: function (h, b) {
      // Дзвіниця-орієнтир.
      tower(h * 0.42, b, 24, 92, 0, { spire: 42 });
      bell(h * 0.42, b - 66, 6);
      house(-h * 0.7, b, 30, 26, 1, { chimney: true });
      house(-h * 0.4, b, 34, 34, 1, {});
      house(-h * 0.08, b, 28, 24, 2, { broken: true });
      house(h * 0.15, b, 26, 30, 1, { chimney: true });
      house(h * 0.72, b, 30, 24, 2, {});
      tree(-h * 0.2, b, 48, 2, "dead");
      // Колодязь.
      shape("M" + (h * 0.02) + "," + b + " l0,-8 l14,0 l0,8 Z M" + h * 0.02 + "," + (b - 8) + " l7,-10 l7,10 Z", 2);
    },
    marsh: function (h, b) {
      tree(-h * 0.6, b, 85, 0, "moss");
      tree(h * 0.55, b, 95, 0, "moss");
      tree(0, b, 120, 1, "glow");
      hut(-h * 0.3, b, 30, 46, 2);
      hut(h * 0.32, b, 26, 40, 2);
      if (!C.faded) { el("ellipse", { cx: 0, cy: b - 1, rx: h * 0.7, ry: 3, fill: C.glow, opacity: 0.55, filter: "url(#blur3)" }); }
      tree(h * 0.82, b, 50, 2, "dead");
    },
    depths: function (h, b) {
      mountain(-h * 0.2, b, h * 1.1, 95, false);
      // Кузні з комінами, лава.
      tower(-h * 0.55, b, 26, 50, 1, { battlement: true });
      tower(h * 0.1, b, 34, 64, 1, { battlement: true });
      shape("M" + (h * 0.42) + "," + b + " l0,-70 l10,0 l0,70 Z M" + (h * 0.6) + "," + b + " l0,-54 l8,0 l0,54 Z", 2);
      fall(-h * 0.2, b - 70, b + 30, 4, "#ff7a2a");
      fall(h * 0.75, b - 10, b + 40, 3, "#ff5a1a");
      light(h * 0.46, b - 74, 2.5, "#ffb347");
      light(h * 0.64, b - 58, 2, "#ffb347");
      house(-h * 0.8, b, 24, 20, 2, { lit: true });
      // Велике колесо шахти й ланцюги.
      if (!C.faded) {
        el("circle", { cx: -h * 0.05, cy: b - 30, r: 24, fill: "none", stroke: "#0b0a09", "stroke-width": 5 });
        el("circle", { cx: -h * 0.05, cy: b - 30, r: 24, fill: "none", stroke: C.tint, "stroke-width": 1, "stroke-opacity": 0.6 });
        for (var k = 0; k < 8; k++) { var a = k * Math.PI / 4; el("line", { x1: -h * 0.05, y1: b - 30, x2: -h * 0.05 + Math.cos(a) * 24, y2: b - 30 + Math.sin(a) * 24, stroke: "#0b0a09", "stroke-width": 2.5 }); }
        el("path", { d: "M" + (h * 0.3) + "," + (b - 80) + " L" + (h * 0.3) + "," + (b - 20) + " M" + (h * 0.3 - 8) + "," + (b - 80) + " L" + (h * 0.3 + 30) + "," + (b - 80), stroke: "#0b0a09", "stroke-width": 3 });
        el("ellipse", { cx: 0, cy: b - 2, rx: h * 0.8, ry: 4, fill: "#ff6a2a", opacity: 0.6, filter: "url(#blur3)" });
        for (var e = 0; e < 14; e++) { light((C.r() - 0.5) * h * 1.6, b - 20 - C.r() * 100, 0.8, "#ffb347"); }
      }
      tower(h * 0.3, b, 18, 40, 2, { battlement: true });
    },
    crystal: function (h, b) {
      crystal(-h * 0.45, b, 34, 110, 0, -8);
      crystal(h * 0.4, b, 30, 95, 0, 10);
      crystal(0, b, 46, 140, 1, 4);
      crystal(-h * 0.2, b, 22, 70, 1, -6);
      crystal(h * 0.2, b, 20, 64, 2, 8);
      crystal(-h * 0.7, b, 18, 48, 2, -10);
      crystal(h * 0.72, b, 22, 56, 2, 6);
      // Риштування шахти.
      if (!C.faded) { el("path", { d: "M" + (-h * 0.85) + "," + b + " l0,-30 l24,0 l0,30 M" + (-h * 0.85) + "," + (b - 15) + " l24,0 M" + (-h * 0.85) + "," + b + " l24,-30", stroke: "#3a2f25", "stroke-width": 2, fill: "none" }); }
    },
    citadel: function (h, b) {
      wall(-h * 0.85, h * 0.85, b, 34, 1);
      tower(-h * 0.6, b, 30, 76, 1, { battlement: true });
      tower(h * 0.58, b, 30, 70, 1, { battlement: true });
      tower(0, b, 44, 108, 0, { spire: 40 });
      tower(-h * 0.25, b, 20, 58, 2, { spire: 24 });
      tower(h * 0.25, b, 20, 62, 2, { spire: 24 });
      lancet(0, b, 16, 24, true);
      // Шибениці й ґрати.
      shape("M" + (h * 0.8) + "," + b + " l0,-40 l16,0 l0,3 l-13,0 l0,37 Z", 2, false);
    },
    capital: function (h, b) {
      tower(-h * 0.72, b, 22, 80, 0, { spire: 34 });
      cathedral(-h * 0.3, b, 110, 120, 1);
      tower(h * 0.15, b, 30, 150, 0, { spire: 60, rose: true });
      statue(h * 0.42, b, 62, 2, true);
      house(h * 0.66, b, 26, 30, 2, {});
      tower(h * 0.85, b, 18, 60, 1, { spire: 30 });
      arches(-h * 0.95, -h * 0.55, b, 24, 2, 2);
    },
    cathedral: function (h, b) {
      cathedral(0, b, 150, 140, 1);
      tower(-h * 0.75, b, 18, 60, 0, { spire: 28 });
      tower(h * 0.75, b, 18, 66, 0, { spire: 30 });
      if (!C.faded) {
        el("rect", { x: -h * 0.95, y: b - 16, width: h * 1.9, height: 18, fill: C.tint, opacity: 0.35 });
        el("line", { x1: -h * 0.95, x2: h * 0.95, y1: b - 16, y2: b - 16, stroke: C.glow, "stroke-width": 1.2, opacity: 0.8 });
      }
    },
    aqueducts: function (h, b) {
      arches(-h * 0.9, h * 0.9, b, 54, 1, 6);
      arches(-h * 0.6, h * 0.5, b - 54, 34, 0, 4);
      tower(h * 0.7, b, 26, 100, 0, { spire: 30 });
      tower(-h * 0.7, b, 22, 80, 1, { battlement: true });
      fall(-h * 0.25, b - 54, b + 40, 4, "#7ff0ff");
      fall(h * 0.2, b - 88, b + 30, 3, "#7ff0ff");
      // Водяне колесо.
      if (!C.faded) { el("circle", { cx: h * 0.45, cy: b - 14, r: 13, fill: "none", stroke: C.tint, "stroke-width": 1.5, "stroke-opacity": 0.7 }); }
    },
    ice: function (h, b) {
      mountain(-h * 0.4, b, h * 1.1, 130, true);
      mountain(h * 0.45, b, h * 0.9, 100, true);
      wall(-h * 0.85, h * 0.85, b, 30, 1);
      tower(-h * 0.55, b, 30, 84, 1, { battlement: true });
      tower(0, b, 34, 112, 1, { spire: 44 });
      tower(h * 0.55, b, 28, 90, 1, { spire: 36 });
      snowcaps(-h * 0.55 - 15, -h * 0.55 + 15, b - 88);
      snowcaps(-h * 0.85, h * 0.85, b - 34);
      if (!C.faded) { for (var i = -6; i <= 6; i++) { el("path", { d: "M" + (i * h * 0.13) + "," + (b + 4) + " l2,10 l2,-10 Z", fill: "#dff0ff", opacity: 0.7 }); } }
    },
    observatory: function (h, b) {
      dome(-h * 0.3, b, 70, 70, 1, true);
      dome(h * 0.4, b, 50, 52, 1, false);
      tower(h * 0.05, b, 22, 96, 0, { spire: 30 });
      tower(-h * 0.75, b, 18, 54, 2, { spire: 22 });
      // Армілярна сфера над вежею.
      if (!C.faded) {
        el("ellipse", { cx: h * 0.05, cy: b - 140, rx: 16, ry: 16, fill: "none", stroke: C.glow, "stroke-width": 1.2, opacity: 0.85 });
        el("ellipse", { cx: h * 0.05, cy: b - 140, rx: 16, ry: 6, fill: "none", stroke: C.glow, "stroke-width": 1, opacity: 0.85, transform: "rotate(-25 " + (h * 0.05) + " " + (b - 140) + ")" });
        for (var i = 0; i < 10; i++) { light((C.r() - 0.5) * h * 1.8, b - 60 - C.r() * 110, 0.9, "#fff1c4"); }
      }
    },
    palace: function (h, b) {
      // Палац: центральний корпус із великим шпилем, крила, сад.
      tower(-h * 0.8, b, 20, 70, 0, { spire: 30 });
      tower(h * 0.8, b, 20, 70, 0, { spire: 30 });
      shape("M" + (-h * 0.62) + "," + b + " L" + (-h * 0.62) + "," + (b - 60) + " L" + (h * 0.62) + "," + (b - 60) + " L" + (h * 0.62) + "," + b + " Z", 1);
      for (var i = -5; i <= 5; i++) { if (i !== 0) { lancet(i * h * 0.11, b - 10, 5, 20, C.r() < 0.7); lancet(i * h * 0.11, b - 36, 4, 12, C.r() < 0.5); } }
      tower(-h * 0.35, b - 60, 24, 40, 1, { spire: 32 });
      tower(h * 0.35, b - 60, 24, 40, 1, { spire: 32 });
      tower(0, b - 60, 40, 64, 1, { spire: 56, rose: true });
      if (!C.faded) {
        el("path", { d: "M" + (-h * 0.2) + "," + (b - 60) + " l0,30 l5,-5 l5,5 l0,-30 Z M" + (h * 0.15) + "," + (b - 60) + " l0,30 l5,-5 l5,5 l0,-30 Z", fill: "#8f1d2c", opacity: 0.9 });
      }
      tree(-h * 0.95, b, 40, 2, "crown");
      tree(h * 0.95, b, 40, 2, "crown");
    },
    shadow: function (h, b) {
      tower(-h * 0.45, b, 26, 90, 1, { spire: 40 });
      tower(h * 0.1, b, 34, 125, 1, { spire: 50 });
      tower(h * 0.6, b, 22, 70, 1, { spire: 30 });
      // Уламки, що висять у повітрі, і зламані шпилі.
      shard(-h * 0.75, b - 70, 18, 26, 0);
      shard(h * 0.85, b - 95, 14, 22, 0);
      shard(-h * 0.1, b - 150, 12, 18, 0);
      shard(h * 0.4, b - 130, 16, 24, 0);
      column(-h * 0.8, b, 40, 8, 2, true);
      column(h * 0.35, b, 34, 8, 2, true);
      if (!C.faded) {
        el("path", { d: "M" + (-h * 0.45) + "," + (b - 40) + " l6,12 l-4,10 l6,14 M" + (h * 0.1) + "," + (b - 60) + " l-5,14 l6,12 l-4,20", stroke: C.glow, "stroke-width": 1.4, fill: "none", opacity: 0.9 });
      }
    },
    throne: function (h, b) {
      // Чорний трон-шпиль під затемненням.
      if (!C.faded) {
        el("circle", { cx: 0, cy: b - 150, r: 30, fill: "#2a0508" });
        el("circle", { cx: 0, cy: b - 150, r: 30, fill: "none", stroke: C.glow, "stroke-width": 3, opacity: 0.9 });
        el("circle", { cx: 0, cy: b - 150, r: 40, fill: C.glow, opacity: 0.15, filter: "url(#blur8)" });
      }
      tower(-h * 0.7, b, 22, 80, 0, { spire: 40 });
      tower(h * 0.7, b, 22, 84, 0, { spire: 40 });
      tower(-h * 0.35, b, 26, 100, 1, { spire: 50 });
      tower(h * 0.35, b, 26, 104, 1, { spire: 50 });
      tower(0, b, 44, 130, 1, { spire: 70, rose: true });
      wall(-h * 0.9, h * 0.9, b, 22, 2);
      // Шипи огорожі.
      var spikes = "";
      for (var x = -h * 0.88; x < h * 0.88; x += 7) { spikes += "M" + x + "," + (b - 22) + " l2,-10 l2,10 Z "; }
      shape(spikes, 2, false);
    }
  };

  window.Towns = {
    draw: function (g, kind, options) {
      C = { g: g, r: options.r, tint: options.tint, glow: options.glow, faded: options.faded };
      (ZONES[kind] || ZONES.capital)(options.half, options.top);
    }
  };
})();
