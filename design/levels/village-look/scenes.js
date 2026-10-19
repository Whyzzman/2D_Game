// Дві сцени макета Села. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Земля й дерево Села: тепла бура серцевина, жар на гранях, світлий кант із пилом.
  const EARTH = { core: "#0c0907", stone: "#2a211a", rim: "#d79a5c", lip: "#6a5640", lipDark: "#3a2e22", lipLight: "#d8c4a0" };
  const HOUSE = { wall: "#2c231c", beam: "#140f0b", roof: "#2a2016", roofLight: "#8a7044", window: "#0b0807", windowLit: "#f0a04e", lit: 0.25, broken: 0.4 };
  const NEAR_HOUSE = { wall: "#1e1813", beam: "#0b0806", roof: "#1b140e", roofLight: "#6a5634", window: "#070504", windowLit: "#f4a852", lit: 0.35, broken: 0.3 };

  function square(canvas) {
    const f = new Frame(canvas);
    f.sky("#1c1414", "#5a3424");
    f.glow(16, -2, 22, "#d0642a", 0.35);

    // Далекий план: дзвіниця проти жаркого неба, дальні хати, дим зі Столиці внизу.
    f.belfry(23.5, 2, 2.6, 9.5, { stone: "#241a17", bell: "#6e522c", bellGlow: "#f2b05a", slit: "#3b2418" });
    const far = rng(3);
    for (let x = -1; x < 33; x += 3.4 + far() * 2) {
      if (x > 21 && x < 26) continue;
      f.house(x, 2.2, 2.4 + far() * 1.4, 2 + far() * 1.2, { wall: "#2a1d18", beam: "#1a120e", roof: "#24180f", roofLight: "#5a4028",
        window: "#1a110c", windowLit: "#e08a40", lit: 0.15, broken: 0.5 }, 10 + Math.round(x * 3));
    }
    f.smoke(-1, 33, -1, 6, "#4a3026", 0.5, 4);
    f.fog(0.5, 7, "#5a3a2a", 0.35);

    // Середній план: хати площі, деякі з вогником у вікні.
    f.house(3.5, 2, 5.2, 3.6, HOUSE, 21);
    f.house(25.2, 2.4, 5.6, 3.8, HOUSE, 22);
    f.fog(1, 4, "#4a3026", 0.25);

    // Ігрова площина: бруківка площі, дахи й помости — уступи.
    f.mass({ points: [[-1, -1], [-1, 2], [10.6, 2], [11.2, 2.5], [21.4, 2.5], [22.0, 2.0], [33, 2.0], [33, -1]],
      tops: [1, 3, 5], seed: 61, erosion: 0.14 }, EARTH);
    // дерев'яний поміст на палях зліва і дах сараю справа
    f.mass({ points: [[0.6, 6.6], [6.2, 6.6], [6.0, 6.2], [0.8, 6.2]], tops: [0], seed: 62, erosion: 0.06 }, EARTH);
    [1.2, 3.4, 5.6].forEach(x => { const [px, py] = f.px(x, 6.2); f.c.fillStyle = "#140f0b"; f.c.fillRect(px, py, f.S * 0.22, f.S * 4.2); });
    f.mass({ points: [[12.8, 9.4], [17.6, 9.4], [17.4, 8.9], [15.2, 8.65], [13.0, 8.9]], tops: [0], seed: 63, erosion: 0.12 }, EARTH);
    f.chain(13.4, 9.0, 18.6, "#24180f", 1);
    f.chain(17.0, 9.0, 18.6, "#24180f", 1);
    f.mass({ points: [[24.2, 7.2], [29.8, 7.2], [29.6, 6.7], [27.0, 6.4], [24.4, 6.7]], tops: [0], seed: 64, erosion: 0.12 }, EARTH);

    f.well(16.2, 2.5, { stone: "#2e241c", beam: "#140f0b", roof: "#22180f" });
    f.hero(9.0, 2.5, 1);

    // Передній план: стріха хати, що нависає, і тин унизу — лише силует.
    f.foreground((c, fr) => {
      const S = fr.S, r = rng(70);
      c.beginPath();
      c.moveTo(...fr.px(26, 19)); c.lineTo(...fr.px(33, 19)); c.lineTo(...fr.px(33, 13.8));
      c.quadraticCurveTo(...fr.px(30, 15.6), ...fr.px(26, 16.2)); c.closePath(); c.fill();
      c.strokeStyle = c.fillStyle; c.lineCap = "round"; c.lineWidth = S * 0.18;
      for (let x = -0.6; x < 8; x += 0.55 + r() * 0.3) {
        c.beginPath(); c.moveTo(...fr.px(x, -0.5)); c.lineTo(...fr.px(x + (r() - 0.5) * 0.3, 0.9 + r() * 0.7)); c.stroke();
      }
      c.lineWidth = S * 0.1;
      c.beginPath(); c.moveTo(...fr.px(-1, 0.5)); c.quadraticCurveTo(...fr.px(3.5, 0.8), ...fr.px(8, 0.4)); c.stroke();
    });

    f.dust(80, "#f0c890", 6);
    f.vignette(0.6);
  }

  function church(canvas) {
    const f = new Frame(canvas);
    const STONE = { core: "#0b0908", stone: "#2a2420", rim: "#e0a868", lip: "#6e6254", lipDark: "#3a322a", lipLight: "#d4c4a6" };
    f.sky("#140f0d", "#0a0807");

    // Далекий план: апсида з розбитим вітражем — головне тепле світло.
    f.arcade({ top: 18, base: 1.4, span: 7.2, archH: 9.6, col: 1.0, offset: 2.4, seed: 7, broken: 0.2,
      wall: "#1d1714", hole: "#120e0c", mist: "rgba(90,58,40,0.45)", column: "#231c18", columnLight: "#2f2620" });
    f.stainedWindow(16, 6.5, 4.2, 9.5, ["#a8452a", "#c98a3a", "#3a5a7a", "#7a2a3a", "#d9b45a"], 9);
    f.shaft(16, 15.8, 0.12, 2.6, 15, "#f0b070", 0.24);
    f.fog(1.5, 9, "#4a3024", 0.35);

    // Ігрова площина: кам'яна підлога з вівтарною сходинкою, балки й хори — уступи.
    f.mass({ points: [[-1, 19.5], [33, 19.5], [33, 16.6], [-1, 16.6]], seed: 71, erosion: 0.1, course: 1.1 }, STONE);
    f.mass({ points: [[-1, -1], [-1, 1.6], [12.4, 1.6], [12.9, 2.1], [19.1, 2.1], [19.6, 1.6], [33, 1.6], [33, -1]],
      tops: [1, 3, 5], seed: 72, erosion: 0.1 }, STONE);
    f.mass({ points: [[-1, 1.6], [1.4, 1.6], [1.9, 6], [1.6, 12], [2.2, 16.6], [-1, 16.6]], seed: 73, erosion: 0.25 }, STONE);
    f.mass({ points: [[30.6, 1.6], [33, 1.6], [33, 16.6], [30.2, 16.6], [30.8, 11], [30.4, 5.4]], seed: 74, erosion: 0.25 }, STONE);
    // хори зліва (кам'яний балкон) і балка справа
    f.mass({ points: [[1.8, 7.4], [8.2, 7.4], [8.0, 6.8], [5.0, 6.4], [2.0, 6.8]], tops: [0], seed: 75, erosion: 0.14 }, STONE);
    f.mass({ points: [[22.6, 9.8], [29.8, 9.8], [29.8, 9.3], [22.6, 9.3]], tops: [0], seed: 76, erosion: 0.04 },
      { core: "#120c08", stone: "#2c1f15", rim: "#d79a5c", lip: "#5a4430", lipDark: "#2e2218", lipLight: "#c8a87a" });
    f.chain(23.4, 9.8, 16.6, "#1a120c", 1);

    // Лави — темні силуети на підлозі.
    const c = f.c, S = f.S;
    c.fillStyle = "#130e0b";
    [3.5, 6.4, 22.4, 25.4].forEach(x => {
      c.fillRect(...f.px(x, 2.4), 2.2 * S, 0.25 * S);
      c.fillRect(...f.px(x + 0.1, 2.15), 0.18 * S, 0.55 * S);
      c.fillRect(...f.px(x + 1.9, 2.15), 0.18 * S, 0.55 * S);
      c.fillRect(...f.px(x, 3.2), 0.16 * S, 1.6 * S);
    });
    // Вівтар і свічки.
    c.fillStyle = "#2c241d"; c.fillRect(...f.px(14.6, 3.4), 2.8 * S, 1.3 * S);
    [14.2, 14.8, 17.2, 17.8, 16.0].forEach((x, i) => f.candle(x, x === 16 ? 3.4 : 2.1, 0.4 + (i % 2) * 0.2, "#f4a852"));

    f.hero(9.6, 1.6, 1);

    f.foreground((cx, fr) => {
      cx.beginPath();
      cx.moveTo(...fr.px(-1, 19)); cx.lineTo(...fr.px(1.6, 19)); cx.lineTo(...fr.px(1.6, -1)); cx.lineTo(...fr.px(-1, -1)); cx.closePath(); cx.fill();
      cx.beginPath(); cx.moveTo(...fr.px(1.6, 18)); cx.quadraticCurveTo(...fr.px(5, 17.6), ...fr.px(7, 19)); cx.lineTo(...fr.px(1.6, 19)); cx.closePath(); cx.fill();
    });

    f.dust(90, "#f0c890", 8);
    f.vignette(0.66);
  }

  square(document.getElementById("square"));
  church(document.getElementById("church"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
