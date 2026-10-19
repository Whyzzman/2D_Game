// Дві сцени макета Собору. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng, fbm } = window.TombLook;

  // Сірий камінь Собору: холодна серцевина, бірюзовий відблиск води на гранях, світлий кант зі свічковим пилом.
  const STONE = { core: "#0a0d11", stone: "#2a3038", rim: "#8fd0cc", lip: "#59626c", lipDark: "#262c33", lipLight: "#c9d2d8" };
  const WET = { core: "#071012", stone: "#1d2c30", rim: "#7fd8d0", lip: "#3f5a5c", lipDark: "#1b2729", lipLight: "#a9dcd6" };
  const PILLAR = { stone: "#1a1f26", light: "#3c4652", dark: "#0b0e12" };
  const PILLAR_FAR = { stone: "#141920", light: "#222a33", dark: "#0d1116" };
  const ROSE = { glow: "#4fb8b0", dark: "#0a1214", stone: "#151a20", gold: "#d8b060", core: "#ffe2a0",
    warm: ["#e0a040", "#c8803a", "#f0c870", "#b0583a"], cold: ["#3fa8a8", "#2f6a8a", "#6ad0c0", "#2a8a9a"] };
  const SAINT = { stone: "#2a3036", light: "#a8c8cc", face: "#0b0f12", crack: "#7ff0e0", halo: "#e8c070", water: "#7fd8d0" };
  const DEEP = { top: "#2a8a8a", mid: "#13555c", deep: "#052028", ray: "#9ff0e0", caustic: "#bff6ea", glint: "#d8fff4" };
  const CANDLE = "#ffc66b";

  function nave(canvas) {
    const f = new Frame(canvas);
    f.sky("#090c11", "#131a22");

    // Далекий план: ребра склепінь у темряві, вузькі вікна; розетка — серце кадру.
    f.farNave({ offset: -2, span: 9, base: 4, h: 15, rib: "#10151b", window: "#16242a" });
    f.roseWindow(16, 12.2, 3.9, ROSE, 701);
    f.shaft(16, 9.2, 0.06, 2.4, 10, "#9ad8d0", 0.16);
    f.shaft(14.6, 9.6, -0.18, 1.4, 9, "#f0c070", 0.12);
    f.shaft(17.6, 9.4, 0.26, 1.2, 9.5, "#f0c070", 0.1);
    f.fog(2, 10, "#1e3a40", 0.35);

    // Середній план: пучкові колони нефу з ребрами, що сходяться над розеткою.
    f.pillar(3.2, 1, 11.5, 1.1, PILLAR_FAR, [[9.6, 15.6]]);
    f.pillar(28.8, 1, 11.5, 1.1, PILLAR_FAR, [[22.4, 15.6]]);
    f.pillar(9.6, 1, 10.4, 0.9, PILLAR_FAR, [[16, 16.4]]);
    f.pillar(22.4, 1, 10.4, 0.9, PILLAR_FAR, [[16, 16.4]]);
    f.chain(6.6, 9.5, 19, "#0f1318", 0.9);
    f.chain(25.6, 10.2, 19, "#0f1318", 0.9);
    f.fog(0.5, 5, "#2a5a5c", 0.3);

    // Вода внизу нефу: розетка відбивається в ній.
    f.water(-1, 33, 1.0, -1, { water: "#103a3e", deep: "#03090b", glint: "#9ff0e0" }, 711);
    f.reflection(16, 1.0, 5.5, 2.2, "#e0b060", 0.3);
    f.reflection(16, 1.0, 2.6, 1.4, "#7fd8d0", 0.35);

    // Свята над водою — ще статуя, але тріщина вже світить.
    f.saint(16, 1.6, 5.6, SAINT);

    // Ігрова площина: дві сходинки підлоги з боків, хори-балкони, зламаний амвон під розеткою.
    f.mass({ points: [[-1, -1], [-1, 2.4], [6.4, 2.4], [6.9, 1.6], [7.6, 1.2], [7.4, -1]], tops: [1], seed: 721, erosion: 0.14 }, STONE);
    f.mass({ points: [[33, -1], [33, 2.4], [25.6, 2.4], [25.1, 1.6], [24.4, 1.2], [24.6, -1]], tops: [1], seed: 722, erosion: 0.14 }, STONE);
    f.mass({ points: [[-1, 6.2], [5.6, 6.2], [5.4, 5.6], [3.2, 5.0], [-1, 4.6]], tops: [0], seed: 723, erosion: 0.16 }, STONE);
    f.balustrade(-1, 5.4, 6.2, 0.7, PILLAR, 724);
    f.mass({ points: [[33, 6.2], [26.4, 6.2], [26.6, 5.6], [28.8, 5.0], [33, 4.6]], tops: [0], seed: 725, erosion: 0.16 }, STONE);
    f.balustrade(26.6, 33, 6.2, 0.7, PILLAR, 726);
    f.mass({ points: [[12.2, 8.6], [19.8, 8.6], [19.6, 8.1], [16.6, 7.6], [13.8, 7.8], [12.4, 8.1]], tops: [0], seed: 727, erosion: 0.18 }, STONE);
    // свічки на хорах і на сходинках — тепле світло проти бірюзового
    [[0.6, 6.2], [1.2, 6.2], [3.6, 6.2], [27.6, 6.2], [30.2, 6.2], [31.4, 6.2], [2.4, 2.4], [4.8, 2.4], [27.4, 2.4], [29.9, 2.4], [13.2, 8.6], [18.7, 8.6]]
      .forEach(([x, y], i) => f.candle(x, y, 0.3 + (i % 3) * 0.12, CANDLE));
    f.dust(40, "#ffd890", 12);

    f.hero(29.4, 6.2, -1);

    // Передній план: темна колона праворуч і кадило на ланцюгу ліворуч — лише силует.
    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(33, -1)); c.lineTo(...fr.px(31.4, -1));
      c.bezierCurveTo(...fr.px(31.7, 6), ...fr.px(31.5, 12), ...fr.px(31.8, 19)); c.lineTo(...fr.px(33, 19)); c.closePath(); c.fill();
      c.strokeStyle = c.fillStyle; c.lineWidth = fr.S * 0.06;
      c.beginPath(); c.moveTo(...fr.px(2.2, 19)); c.quadraticCurveTo(...fr.px(2.0, 15.5), ...fr.px(2.4, 12.6)); c.stroke();
      c.beginPath(); c.ellipse(...fr.px(2.4, 12.2), fr.S * 0.4, fr.S * 0.5, 0, 0, Math.PI * 2); c.fill();
      c.beginPath(); c.moveTo(...fr.px(-1, 19)); c.lineTo(...fr.px(7, 19)); c.quadraticCurveTo(...fr.px(2.5, 17.8), ...fr.px(-1, 16.2)); c.closePath(); c.fill();
    });

    f.dust(50, "#a8e8e0", 13);
    f.vignette(0.6);
  }

  function deep(canvas) {
    const f = new Frame(canvas);
    f.sky("#081014", "#0c1a20");
    f.glow(8, 15, 12, "#4fb8b0", 0.18);

    // Над водою: склепіння нижньої крипти, арки в тумані, свічки на уламку балкона.
    f.arcade({ top: 18, base: 7.6, span: 6.4, archH: 8.2, col: 1.0, offset: 2.6, seed: 801, broken: 0.25,
      wall: "#0f151a", hole: "#0a1014", mist: "rgba(60,140,140,0.35)", column: "#141b21", columnLight: "#202a32" });
    f.fog(8, 13, "#1c4a4c", 0.35);
    f.waterfall(4.2, 18.5, 8.2, 1.4, { water: "#4fb8b0", foam: "#c8fff4" }, 802);

    // Під водою (малюється до товщі — потоне в бірюзі): ряди хористів, колони, мертве дерево, похила люстра.
    const r = rng(803);
    for (let x = 1; x < 33; x += 3.6 + r() * 2.4) {
      const h = 5 + r() * 2.5;
      f.pillar(x, -0.5, h, 0.7 + r() * 0.3, { stone: "#06161a", light: "#123034", dark: "#030b0d" }, null);
    }
    f.choirRow(-1, 33, 0.4, 2.4, "#041114", 804);
    f.deadOak(25.2, 0.8, 6.0, 0.9, "#041114", 805);
    f.mass({ points: [[-1, -1], [-1, 0.8], [8, 0.6], [14, 0.9], [22, 0.5], [33, 0.8], [33, -1]], tops: [], seed: 806, erosion: 0.3 }, WET);
    f.saint(15.6, 0.8, 4.4, { stone: "#061518", light: "#4a8a8a", face: "#061012", crack: "#5ad8c8", halo: "#3a8a80", water: "#5ad8c8" });
    f.chandelier(20.4, 4.6, 3.4, { chain: "#051214", iron: "#0a1c20", lit: false }, 807, 0.32);

    f.underwater(-1, 33, 8.0, -1, DEEP, 811);

    // Ігрова площина над водою: уламок балкона хорів, похилий камінь, свічки; вниз — тільки пірнаючи.
    f.mass({ points: [[-1, 9.2], [9.4, 9.2], [9.2, 8.6], [6.4, 7.4], [2.2, 6.8], [-1, 7.0]], tops: [0], seed: 821, erosion: 0.18 }, WET);
    f.balustrade(4.4, 9.2, 9.2, 0.6, PILLAR, 822);
    f.mass({ points: [[24.6, 8.9], [33, 8.9], [33, 6.4], [28.4, 6.9], [24.8, 8.2]], tops: [0], seed: 823, erosion: 0.2 }, WET);
    f.mass({ points: [[14.8, 11.6], [19.2, 11.6], [19.0, 11.1], [17.0, 10.6], [15.0, 11.0]], tops: [0], seed: 824, erosion: 0.14 }, STONE);
    f.chain(15.4, 11.6, 19, "#0f1318", 0.9);
    f.chain(18.6, 11.6, 19, "#0f1318", 0.9);
    f.bell(26.8, 13.8, 1.4, { bronze: "#3a3020", light: "#c8a060", glow: "#e0b060", chain: "#0f1318" });
    [[1.0, 9.2], [2.6, 9.2], [3.4, 9.2], [26.0, 8.9], [30.6, 8.9], [16.2, 11.6]].forEach(([x, y], i) => f.candle(x, y, 0.28 + (i % 3) * 0.1, CANDLE));
    f.reflection(2.4, 8.0, 3.4, 2.6, "#ffc66b", 0.25);
    f.reflection(28, 8.0, 3.4, 2.6, "#ffc66b", 0.22);

    f.hero(7.2, 9.2, 1);

    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(-1, 19)); c.lineTo(...fr.px(12, 19)); c.quadraticCurveTo(...fr.px(5, 17.4), ...fr.px(-1, 15.0)); c.closePath(); c.fill();
      c.beginPath();
      c.moveTo(...fr.px(33, -1)); c.lineTo(...fr.px(27, -1)); c.quadraticCurveTo(...fr.px(30.5, 1.6), ...fr.px(33, 2.4)); c.closePath(); c.fill();
      // водорості з дна — тонкі хвилясті пасма
      const rr = rng(830);
      c.strokeStyle = c.fillStyle; c.lineCap = "round";
      for (let x = -0.5; x < 6; x += 0.3 + rr() * 0.5) {
        c.lineWidth = fr.S * (0.05 + rr() * 0.06);
        c.beginPath(); c.moveTo(...fr.px(x, -1));
        c.bezierCurveTo(...fr.px(x + 0.4, 1), ...fr.px(x - 0.4, 2.4), ...fr.px(x + (fbm(x, 2, 830, 2) - 0.5), 3 + rr() * 2)); c.stroke();
      }
    });

    f.dust(40, "#d8fff4", 14);
    f.vignette(0.62);
  }

  nave(document.getElementById("nave"));
  deep(document.getElementById("deep"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
