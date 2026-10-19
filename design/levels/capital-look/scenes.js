// Дві сцени макета Столиці. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Попелястий камінь Столиці (PlaceholderCapitalArt): сажа в серцевині, попіл на гранях, блідий кант.
  const ASH = { core: "#141213", stone: "#3a3635", rim: "#d8c2a8", lip: "#7a726b", lipDark: "#3a3635", lipLight: "#cfc4b6" };
  const FIRE = { iron: "#1b1615", ember: "#c85a22", flame: "#f09a48" };
  const BANNER = "#5a1e22", BANNER_DARK = "#381417", GOLD = "#8a7434";

  function angel(canvas) {
    const f = new Frame(canvas);
    f.sky("#1e1416", "#6a2a1e");
    f.glow(9, 3, 20, "#c85a22", 0.3);

    // Далекий план: згорілий палац зі шпилями проти попелясто-червоного неба, дим над ним.
    f.palace(-2, 34, 4, 13, "#1c1414", 41, "#c85a22");
    f.smoke(-1, 33, 8, 17, "#3a2a28", 0.5, 7);
    f.fog(1, 9, "#5a2e24", 0.4);

    // Середній план: фасади з порожніми арками, вікно-друге ще жевріє, довгі стяги.
    f.c.save(); f.c.beginPath();
    { f.c.moveTo(...f.px(-1, -1));
      for (let x = -1; x <= 33; x += 0.25) f.c.lineTo(...f.px(x, 10.5 + window.TombLook.fbm(x * 0.35, 3, 17, 3) * 1.4 - Math.max(0, window.TombLook.fbm(x * 0.12, 7, 18, 2)) * 6));
      f.c.lineTo(...f.px(33, -1)); f.c.closePath(); f.c.clip(); }
    f.arcade({ top: 13, base: 2.2, span: 5.6, archH: 6.4, col: 0.9, offset: 1.6, seed: 13, broken: 0.35,
      wall: "#221a1a", hole: "#140f10", mist: "rgba(120,52,36,0.45)", column: "#2a2221", columnLight: "#3a302e" });
    f.c.restore();
    f.longBanner(4.4, 9.8, 5.2, 1.3, BANNER, GOLD, 31);
    f.longBanner(26.8, 8.4, 3.2, 1.2, BANNER_DARK, GOLD, 32);
    f.fog(1, 5, "#4a2620", 0.3);

    // Ігрова площина: бруківка площі з ангелом, карниз і балкон — уступи.
    f.mass({ points: [[-1, -1], [-1, 2], [11.4, 2], [11.9, 2.6], [20.6, 2.6], [21.1, 2.0], [33, 2.0], [33, -1]],
      tops: [1, 3, 5], seed: 81, erosion: 0.14, course: 0.9 }, ASH);
    f.mass({ points: [[-1, 8.2], [5.4, 8.2], [5.0, 7.5], [2.2, 7.0], [-1, 6.6]], tops: [0], seed: 82, erosion: 0.16 }, ASH);
    f.mass({ points: [[22.4, 9.6], [28.6, 9.6], [28.4, 9.0], [25.4, 8.6], [22.6, 9.0]], tops: [0], seed: 83, erosion: 0.14 }, ASH);
    f.chain(23.2, 9.1, 18.6, "#1b1615", 1);
    f.chain(27.8, 9.1, 18.6, "#1b1615", 1);
    f.mass({ points: [[12.0, 12.6], [16.2, 12.6], [16.0, 12.1], [14.0, 11.8], [12.2, 12.1]], tops: [0], seed: 84, erosion: 0.12 }, ASH);

    f.angel(16.2, 2.6, 7.2, { stone: "#3a3433", light: "#d8c2a8" });
    f.brazier(10.2, 2.0, 1.2, FIRE, 5);
    f.brazier(23.4, 2.0, 1.1, FIRE, 6);
    f.rubble(17.8, 20.4, 2.6, "#3a3635", 9);
    f.hero(7.2, 2.0, 1);

    // Передній план: обвалена колона зліва і край даху вгорі справа — лише силует.
    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(-1, -1)); c.lineTo(...fr.px(-1, 19)); c.lineTo(...fr.px(0.6, 19));
      c.bezierCurveTo(...fr.px(1.0, 12), ...fr.px(0.7, 6), ...fr.px(1.3, 2.4));
      c.lineTo(...fr.px(2.4, 1.6)); c.lineTo(...fr.px(2.6, -1)); c.closePath(); c.fill();
      c.beginPath();
      c.moveTo(...fr.px(24, 19)); c.lineTo(...fr.px(33, 19)); c.lineTo(...fr.px(33, 15.4));
      c.bezierCurveTo(...fr.px(30, 16.4), ...fr.px(27, 17.4), ...fr.px(24, 19)); c.fill();
      const r = rng(90);
      for (let x = -0.5; x < 33; x += 1.2 + r() * 2.5) {
        if (x > 3 && x < 26) { x += 6 + r() * 4; continue; }
        c.beginPath(); c.ellipse(...fr.px(x, 0.1), fr.S * (0.4 + r() * 0.7), fr.S * (0.2 + r() * 0.3), 0, Math.PI, 0); c.fill();
      }
    });

    f.ash(140, "#b8aca0", 11);
    f.dust(40, "#f09a48", 12);
    f.vignette(0.6);
  }

  function cistern(canvas) {
    const f = new Frame(canvas);
    const WATER = { water: "#1a2a36", deep: "#06090c", glint: "#7ab8e0", foam: "#a8c8dc" };
    const SEAL = { glow: "#3a7ad8", line: "#8ac4ff" };
    f.sky("#0e1012", "#08090a");

    // Далекий план: склепіння на колонах, що тонуть у темряві й синьому тумані.
    f.farNave({ offset: 3.0, span: 6.4, base: -1, h: 15.5, rib: "#1c1c1e", window: "#0c0e10" });
    f.arcade({ top: 16, base: 1.2, span: 6.8, archH: 10.4, col: 1.2, offset: 0.8, seed: 21, broken: 0.25,
      wall: "#1a191a", hole: "#0c0d0f", mist: "rgba(40,70,100,0.35)", column: "#222122", columnLight: "#2c2b2c" });
    f.seal(25.2, 9.8, 1.5, SEAL, 3);
    f.fog(0.5, 7, "#1e3446", 0.4);

    // Пролом у склепінні: звідти падає вода й ледь сіре світло.
    f.shaft(12.6, 18.4, 0.05, 1.8, 17, "#8aa8c0", 0.14);
    f.waterfall(12.6, 18.6, 1.3, 1.6, WATER, 4);

    // Ігрова площина: стеля, кам'яні помости над водою, стіни по краях.
    f.mass({ points: [[-1, 19.5], [33, 19.5], [33, 16.8], [16.2, 16.6], [14.4, 17.6], [11.0, 17.8], [9.4, 16.8], [-1, 17.0]], seed: 91, erosion: 0.18, course: 1.0 }, ASH);
    f.water(-1, 33, 1.3, -1, WATER, 6);
    f.reflection(26.6, 1.25, 2.0, 2.4, "#8ac4ff", 0.3);
    f.reflection(12.6, 1.25, 2.2, 2.0, "#a8c8dc", 0.25);
    f.mass({ points: [[-1, -1], [-1, 3.2], [7.6, 3.2], [8.0, 2.6], [7.4, 0.4], [6.0, -1]], tops: [1], seed: 92, erosion: 0.16 }, ASH);
    f.mass({ points: [[19.4, 4.2], [24.6, 4.2], [24.4, 3.5], [24.8, 0.8], [19.6, 0.6], [19.2, 3.5]], tops: [0], seed: 93, erosion: 0.14 }, ASH);
    f.mass({ points: [[28.4, -1], [28.0, 2.0], [28.6, 6.4], [33, 6.4], [33, -1]], tops: [2], seed: 94, erosion: 0.16 }, ASH);
    f.mass({ points: [[14.6, 8.6], [18.8, 8.6], [18.6, 8.1], [16.6, 7.8], [14.8, 8.1]], tops: [0], seed: 95, erosion: 0.12 }, ASH);
    f.chain(15.2, 8.2, 16.7, "#141213", 1);
    f.chain(18.2, 8.2, 16.7, "#141213", 1);

    f.brazier(22.0, 4.2, 1.0, FIRE, 7);

    f.hero(4.6, 3.2, 1);

    // Передній план: колона, що встає з води, і звислі корені зі стелі.
    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(30.9, -1)); c.bezierCurveTo(...fr.px(30.5, 6), ...fr.px(31.1, 12), ...fr.px(30.7, 19)); c.lineTo(...fr.px(33, 19)); c.lineTo(...fr.px(33, -1)); c.closePath(); c.fill();
      c.strokeStyle = c.fillStyle; c.lineCap = "round";
      const r = rng(77);
      for (let x = 1; x < 9; x += 0.7 + r() * 1.2) {
        c.lineWidth = fr.S * (0.06 + r() * 0.08);
        c.beginPath(); c.moveTo(...fr.px(x, 19));
        c.quadraticCurveTo(...fr.px(x + (r() - 0.5) * 0.8, 17.2), ...fr.px(x + (r() - 0.5) * 0.6, 16.4 - r() * 1.6)); c.stroke();
      }
    });

    f.dust(70, "#a8c8dc", 8);
    f.vignette(0.7);
  }

  angel(document.getElementById("angel"));
  cistern(document.getElementById("cistern"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
