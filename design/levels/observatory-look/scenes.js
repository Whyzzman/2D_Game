// Дві сцени макета Обсерваторії. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Індиго-камінь: темна синя серцевина, золотий зоряний відблиск на канті, де стоять.
  const INDIGO = { core: "#07061a", stone: "#1c1a3e", rim: "#c9b6ff", lip: "#3b3570", lipDark: "#1a1838", lipLight: "#e8d49a" };
  // Скеля летючих островів — холодніша, з фіолетовим відблиском знизу.
  const ISLE = { core: "#08071c", stone: "#221c48", rim: "#a99cff", lip: "#463c86", lipDark: "#1d1940", lipLight: "#f0dca0" };
  const CRYSTAL = { dark: "#1a1446", mid: "#5a48c8", light: "#cfc4ff", glow: "#8f7bff" };
  const GOLD = "#f2cf7a";
  const SKY = { skyTop: "#05041a", skyBottom: "#1a1450", nebula: ["#5b3db8", "#2f5fc0", "#a24fb0"], star: "#f6eedc", glow: "#7a64e0", frame: "#0a0920" };

  function library(canvas) {
    const f = new Frame(canvas);
    f.sky("#06051a", "#141236");
    f.glow(16, 9, 22, "#3a2c90", 0.25);

    // Далекий план: склепіння бібліотеки в темряві, високі вікна з туманністю й зорями.
    f.farNave({ offset: 2.5, span: 9, base: 2, h: 16, rib: "#100e2a", window: "#1d1846" });
    f.starWindow(6.4, 6.8, 2.4, 8.6, SKY, 701);
    f.starWindow(25.6, 7.4, 2.0, 7.8, Object.assign({}, SKY, { nebula: ["#2f5fc0", "#6a3db0"] }), 702);
    f.fog(1, 9, "#2a2468", 0.35);

    // Середній план: стіни полиць під стрілчастими арками, галерея з балюстрадою.
    f.shelves(-0.5, 4.6, 1.8, 15.5, { wood: "#0d0b1e", board: "#2a2140", gilt: GOLD, books: ["#3a2a5a", "#5a2a3a", "#2a3a5a", "#4a3a22", "#2a4a44", "#55406a"] }, 711);
    f.shelves(9.6, 14.2, 2.4, 13.2, { wood: "#0c0a1c", board: "#261e3c", gilt: GOLD, books: ["#4a2a4a", "#2a2a5a", "#3e3020", "#22404a", "#5a3040"] }, 712);
    f.shelves(18.4, 23.0, 2.4, 14.4, { wood: "#0c0a1c", board: "#261e3c", gilt: GOLD, books: ["#2a3260", "#503040", "#3a4a2a", "#4a2a5a", "#5c4628"] }, 713);
    f.shelves(28.2, 32.6, 1.8, 16.2, { wood: "#0d0b1e", board: "#2a2140", gilt: GOLD, books: ["#3e2a60", "#2a4054", "#5a3a24", "#5a2a44"] }, 714);
    f.fog(0.5, 5, "#30287a", 0.3);
    // сузір'я, намальоване на склепінні
    f.constellation([[11.2, 16.2], [12.6, 15.4], [14.4, 16.0], [15.6, 14.9], [17.6, 15.6], [19.0, 16.6]], GOLD, 0.55);

    // Оррері — серце зали: висить над прогалиною в підлозі, планети на латунних орбітах.
    f.orrery(16.2, 9.8, 3.4, { brass: "#c9a24e", glow: "#8a70ff", sun: "#ffe0a0", planets: ["#9db8ff", "#e0a070", "#c890ff", "#8fe0d0", "#f0d0a0"] }, 721);

    // Ігрова площина: підлога зліва й справа, між ними — колодязь у темряву, через нього зоряний місток.
    // колодязь під містком — темрява з кількома далекими зорями знизу
    {
      const c = f.c, S = f.S, [l, t] = f.px(11.0, 2.2), [rr, b] = f.px(21.0, -1);
      const g = c.createLinearGradient(0, t, 0, b);
      g.addColorStop(0, "rgba(6,5,22,0.75)"); g.addColorStop(1, "rgba(2,2,10,1)");
      c.fillStyle = g; c.fillRect(l, t, rr - l, b - t);
      const r = rng(730);
      for (let i = 0; i < 26; i++) { const [px, py] = f.px(11.6 + r() * 8.8, -0.8 + r() * 2.6); c.fillStyle = "rgba(200,190,255," + (0.2 + r() * 0.5) + ")"; c.beginPath(); c.arc(px, py, S * (0.01 + r() * 0.02), 0, Math.PI * 2); c.fill(); }
    }
    f.mass({ points: [[-1, -1], [-1, 2.4], [11.2, 2.4], [11.6, 1.2], [11.0, -1]], tops: [1], seed: 731, erosion: 0.22 }, INDIGO);
    f.mass({ points: [[20.6, -1], [20.2, 1.0], [20.8, 2.4], [33, 2.4], [33, -1]], tops: [2], seed: 732, erosion: 0.22 }, INDIGO);
    f.lens(10.2, 2.4, { brass: "#b08a3e", light: "#bfe0ff", dim: "#3a3a60" }, true);
    f.lightBridge(11.4, 20.6, 2.4, { edge: "#e8f0ff", core: "#7aa8ff" }, 733);
    // галерея праворуч угорі: читальні столи зі свічками, кам'яний уступ
    f.mass({ points: [[22.4, 7.6], [31.2, 7.6], [31.0, 7.0], [26.4, 6.2], [22.6, 7.0]], tops: [0], seed: 734, erosion: 0.16 }, INDIGO);
    f.candle(24.2, 7.6, 0.4, "#ffc66b"); f.candle(24.6, 7.6, 0.3, "#ffc66b");
    f.candle(29.4, 7.6, 0.5, "#ffc66b");
    f.altar(26.6, 2.4);
    // невеликий уступ над колодязем зліва — до нього злітають сторінки
    f.mass({ points: [[2.8, 6.4], [7.4, 6.4], [7.2, 5.9], [5.0, 5.4], [3.0, 5.9]], tops: [0], seed: 735, erosion: 0.14 }, INDIGO);
    f.tome(13.6, 5.8, 1.0, { cover: "#2a1a3a", page: "#e8dcc0", glow: "#c8b0ff" }, 736);

    f.hero(6.0, 2.4, 1);

    // Передній план: темна колона з ажуром ліворуч і драбина, що звисає згори праворуч.
    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(-1, -1)); c.lineTo(...fr.px(1.3, -1));
      c.bezierCurveTo(...fr.px(1.0, 6), ...fr.px(1.6, 13), ...fr.px(0.8, 19)); c.lineTo(...fr.px(-1, 19)); c.closePath(); c.fill();
      c.lineWidth = fr.S * 0.12; c.strokeStyle = c.fillStyle;
      c.beginPath(); c.moveTo(...fr.px(30.2, 19)); c.lineTo(...fr.px(30.4, 11.8)); c.moveTo(...fr.px(31.4, 19)); c.lineTo(...fr.px(31.5, 11.8)); c.stroke();
      c.lineWidth = fr.S * 0.08;
      for (let y = 12.2; y < 19; y += 0.7) { c.beginPath(); c.moveTo(...fr.px(30.3, y)); c.lineTo(...fr.px(31.45, y)); c.stroke(); }
    });

    f.dust(80, "#f2dc9a", 17);
    f.vignette(0.6);
  }

  function isles(canvas) {
    const f = new Frame(canvas);
    f.sky("#03021a", "#1a1650");
    // Небо: туманність навскоси, зорі, далекий купол Обсерваторії з телескопом.
    f.nebula(17, 11, 34, 2.6, 0.38, ["#5b3db8", "#2b56c0", "#9c4aa8"], 0.4, 801);
    f.nebula(6, 15, 14, 1.4, -0.2, ["#3a6ad0", "#7a4ac8"], 0.3, 802);
    f.stars(520, 3, 18, "#f6eedc", 803, 7);
    f.constellation([[22.4, 16.4], [23.8, 15.2], [25.6, 15.8], [27.0, 14.6], [26.2, 13.2]], GOLD, 0.6);
    f.meteor(12.4, 15.8, 4.2, 0.42, "#e8f0ff");

    // Далекий план: купол на вершині вежі (бій з Астрологом угорі), далекі острови в тумані.
    f.farIsland(5.4, 8.0, 9, 5.2, "#14113a", 811);
    // вежа під куполом: м'які боки, що розширюються донизу
    f.c.fillStyle = "#14113a";
    f.c.beginPath(); f.c.moveTo(...f.px(2.6, 8.2)); f.c.bezierCurveTo(...f.px(3.6, 9.4), ...f.px(3.5, 10.6), ...f.px(3.4, 11.4));
    f.c.lineTo(...f.px(7.4, 11.4)); f.c.bezierCurveTo(...f.px(7.3, 10.6), ...f.px(7.2, 9.4), ...f.px(8.2, 8.2)); f.c.closePath(); f.c.fill();
    f.dome(5.4, 11.4, 2.5, { body: "#15123e", light: "#3a3486", rib: "#c9a24e", slit: "#ffe2a0", lamp: "#ffd890" }, 812);
    // телескоп, що визирає зі щілини
    f.c.strokeStyle = "#5e4c2a"; f.c.lineCap = "round"; f.c.lineWidth = f.S * 0.22;
    f.c.beginPath(); f.c.moveTo(...f.px(5.5, 12.6)); f.c.lineTo(...f.px(7.6, 15.2)); f.c.stroke();
    f.glow(7.7, 15.3, 0.5, "#ffe2a0", 0.45);
    f.farIsland(14.5, 6.8, 5, 3.4, "#120f34", 813, "#9a88ff");
    f.farIsland(28.8, 9.4, 6, 4.2, "#110e30", 814, "#ffd890");
    f.fog(2, 9, "#2a2670", 0.4);
    f.farIsland(21.5, 4.6, 7, 3.8, "#16123c", 815);
    f.fog(-1, 4, "#352c86", 0.35);

    // Орбіта острова, що пливе: читається пунктиром, як на мапі.
    f.orbit(17.4, 6.6, 4.4, 1.6, -0.12, GOLD, 0.45);

    // Ігрова площина: острови з рівним верхом, кришталь звисає знизу.
    f.island(-1.5, 6.4, 3.2, 3.6, ISLE, CRYSTAL, 821);
    f.island(9.4, 13.6, 5.4, 2.6, ISLE, CRYSTAL, 822);
    f.island(19.2, 22.6, 7.6, 2.0, ISLE, CRYSTAL, 823);
    f.island(24.6, 33.4, 4.4, 4.4, ISLE, CRYSTAL, 824);
    // кільце гака між островами — холодне світло
    const c = f.c, S = f.S;
    f.glow(17.0, 11.6, 1.2, "#9ec8ff", 0.45);
    c.strokeStyle = "#9cb6e0"; c.lineWidth = S * 0.09;
    c.beginPath(); c.arc(...f.px(17.0, 11.6), S * 0.32, 0, Math.PI * 2); c.stroke();
    f.chain(17.0, 11.9, 18.6, "#1c1a3a", 0.8);
    // руїна вежі-сховку на правому острові: зірка-тайник за тріснутою стіною
    f.c.fillStyle = "#0c0a24";
    f.c.beginPath(); f.c.moveTo(...f.px(28.0, 4.4)); f.c.bezierCurveTo(...f.px(28.5, 6.5), ...f.px(28.3, 8.6), ...f.px(28.6, 9.8));
    f.c.lineTo(...f.px(29.1, 9.3)); f.c.lineTo(...f.px(29.5, 10.4)); f.c.lineTo(...f.px(30.1, 9.6)); f.c.quadraticCurveTo(...f.px(30.6, 9.9), ...f.px(30.9, 9.2));
    f.c.bezierCurveTo(...f.px(30.8, 8.0), ...f.px(31.2, 6.2), ...f.px(31.4, 4.4)); f.c.closePath(); f.c.fill();
    f.c.fillStyle = "#1e1a4a"; f.c.beginPath(); f.pointedArch(29.6, 0.9, 6.6, 1.8); f.c.closePath(); f.c.fill();
    // тріснута стіна сховку: тепла зірка просвічує крізь щілини
    f.glow(29.6, 5.4, 1.1, GOLD, 0.45); f.glow(29.6, 5.4, 0.16, "#fff6dc", 0.9);
    f.c.strokeStyle = "rgba(242,207,122,0.55)"; f.c.lineWidth = f.S * 0.03;
    f.c.beginPath(); f.c.moveTo(...f.px(29.2, 4.6)); f.c.lineTo(...f.px(29.5, 5.3)); f.c.lineTo(...f.px(29.3, 5.9)); f.c.lineTo(...f.px(29.7, 6.4)); f.c.stroke();
    f.lens(26.0, 4.4, { brass: "#b08a3e", light: "#bfe0ff", dim: "#3a3a60" }, false);

    f.hero(3.4, 3.2, 1);

    // Передній план: край острова знизу ліворуч і кришталь, що звисає згори праворуч.
    f.foreground((cx, fr) => {
      cx.beginPath();
      cx.moveTo(...fr.px(-1, -1)); cx.lineTo(...fr.px(-1, 0.8));
      cx.quadraticCurveTo(...fr.px(3, 1.4), ...fr.px(6.5, -1)); cx.closePath(); cx.fill();
      const r = rng(850);
      for (let i = 0; i < 6; i++) {
        const x = 24 + r() * 9, len = 1.2 + r() * 2.6, w = 0.25 + r() * 0.35;
        cx.beginPath(); cx.moveTo(...fr.px(x - w, 19)); cx.lineTo(...fr.px(x + w, 19));
        cx.quadraticCurveTo(...fr.px(x + w * 0.4, 19 - len * 0.6), ...fr.px(x, 19 - len)); cx.quadraticCurveTo(...fr.px(x - w * 0.4, 19 - len * 0.6), ...fr.px(x - w, 19)); cx.fill();
      }
    });

    f.dust(70, "#cfc4ff", 19);
    f.vignette(0.58);
  }

  library(document.getElementById("library"));
  isles(document.getElementById("isles"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
