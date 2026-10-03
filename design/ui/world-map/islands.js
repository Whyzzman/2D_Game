/* Тінь Забутого Королівства — спільне малювання островів зон: макет карти світу (world-map.html)
   і рендер арту для гри (render.html) беруть його звідси, тож гра виглядає так, як погоджено. */
(function () {
  "use strict";
  var NS = "http://www.w3.org/2000/svg";
  // ---------------------------------------------------------------- випадковість
  function rng(seed) { var s = seed >>> 0; return function () { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; }; }
  function hash(text) { var h = 2166136261; for (var i = 0; i < text.length; i++) { h ^= text.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; }

  function el(name, attrs, parent) {
    var node = document.createElementNS(NS, name);
    for (var key in attrs) { node.setAttribute(key, attrs[key]); }
    if (parent) { parent.appendChild(node); }
    return node;
  }

  function defs(svg) {
    var d = el("defs", {}, svg);
    var blur = el("filter", { id: "blur8", x: "-50%", y: "-50%", width: "200%", height: "200%" }, d);
    el("feGaussianBlur", { stdDeviation: "8" }, blur);
    var blur2 = el("filter", { id: "blur3", x: "-50%", y: "-50%", width: "200%", height: "200%" }, d);
    el("feGaussianBlur", { stdDeviation: "3" }, blur2);
    var fog = el("filter", { id: "fog", x: "-20%", y: "-20%", width: "140%", height: "140%" }, d);
    el("feTurbulence", { type: "fractalNoise", baseFrequency: "0.006 0.012", numOctaves: "3", seed: "7" }, fog);
    el("feColorMatrix", { values: "0 0 0 0 0.62  0 0 0 0 0.6  0 0 0 0 0.56  0 0 0 0.9 -0.32" }, fog);
    return d;
  }

  // ---------------------------------------------------------------- острів
  // Верх — ілюстрація зони (towns.js), низ — рвана скеля зі звисами; підсвітка кольором зони.
  function island(svg, z, mode, seedExtra) {
    var r = rng(hash(z.id + (seedExtra || "")));
    var g = el("g", { transform: "translate(" + z.x + "," + z.y + ")" }, svg);
    var w = z.w * (mode === "big" ? 1 : 1.12), half = w / 2, top = -6;
    if (mode === "big") { mode = "lit"; }
    var faded = mode === "silhouette";
    // Сяйво зони.
    if (!faded) {
      el("ellipse", { cx: 0, cy: -40, rx: half * 1.05, ry: 85, fill: z.glow, opacity: 0.26, filter: "url(#blur8)" }, g);
      el("ellipse", { cx: 0, cy: 10, rx: half * 0.8, ry: 30, fill: z.tint, opacity: 0.18, filter: "url(#blur8)" }, g);
    }
    // Нижня скеля: кілька звисів.
    var rock = "M" + (-half) + "," + top;
    var steps = 9;
    for (var i = 1; i <= steps; i++) {
      var t = i / steps;
      var x = -half + w * t;
      var depth = Math.sin(Math.PI * t) * (55 + r() * 30) + 8;
      rock += " L" + (x - w / steps * 0.5) + "," + (top + depth * (0.6 + r() * 0.3)) + " L" + (x - w / steps * 0.25) + "," + (top + depth + r() * 26);
    }
    rock += " L" + half + "," + top + " Z";
    var gradId = "rock" + z.id + (seedExtra || "") + mode;
    var rg = el("linearGradient", { id: gradId, x1: 0, y1: 0, x2: 0, y2: 1 }, svg.querySelector("defs"));
    el("stop", { offset: "0", "stop-color": faded ? "#232120" : "#3a352e" }, rg);
    el("stop", { offset: "0.35", "stop-color": faded ? "#161514" : "#221f1b" }, rg);
    el("stop", { offset: "1", "stop-color": "#0a0909" }, rg);
    var rockFill = "url(#" + gradId + ")";
    el("path", { d: rock, fill: rockFill }, g);
    if (!faded) {
      el("path", { d: rock, fill: "none", stroke: z.tint, "stroke-opacity": 0.35, "stroke-width": 1.5 }, g);
    }
    // Плато й забудова.
    el("rect", { x: -half, y: top - 6, width: w, height: 10, fill: faded ? "#1e1d1b" : "#2c2924" }, g);
    // Забудова — ілюстрація зони (towns.js): орієнтир, будинки з вікнами, дерева, підсвітка кольором зони.
    var townScale = half / 150;
    var town = el("g", { transform: "translate(0," + (top - 4) + ") scale(" + townScale + ") translate(0," + (4 - top) + ")" }, g);
    window.Towns.draw(town, z.kind, { r: r, tint: z.tint, glow: z.glow, faded: faded, half: 150, top: top - 4 });
    return g;
  }

  // Вигляд острова кожної зони: ширина, кант і світло (ті самі, що в макеті карти світу).
  var LOOKS = {
    tomb: { w: 250, tint: "#9fb0c8", glow: "#f0d9a0" }, forest: { w: 290, tint: "#d9862f", glow: "#ffb35a" },
    village: { w: 240, tint: "#a27a58", glow: "#ff9c4a" }, marsh: { w: 240, tint: "#7fa83c", glow: "#b6ff5a" },
    depths: { w: 230, tint: "#c4552f", glow: "#ff6a2a" }, crystal: { w: 240, tint: "#5a7cff", glow: "#7fb6ff" },
    citadel: { w: 250, tint: "#a33a32", glow: "#ff5a3a" }, capital: { w: 300, tint: "#b6aa94", glow: "#ffd27a" },
    cathedral: { w: 250, tint: "#6aa0b0", glow: "#9ee0ff" }, aqueducts: { w: 250, tint: "#3fa3b5", glow: "#6ff0ff" },
    ice: { w: 280, tint: "#b9d4f0", glow: "#e8f6ff" }, observatory: { w: 220, tint: "#7d5ad6", glow: "#c9a6ff" },
    palace: { w: 300, tint: "#e8d6a4", glow: "#ffe6a0" }, shadow: { w: 270, tint: "#8a4dff", glow: "#b47aff" },
    throne: { w: 260, tint: "#c0303f", glow: "#ff4a6a" }
  };

  window.Islands = { rng: rng, hash: hash, el: el, defs: defs, island: island, looks: LOOKS };
})();
