// Дві сцени макета Царства Тіні. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Камінь Тіні: майже чорний фіолет, рожево-фіолетовий відблиск знизу (з безодні), блідий кант.
  const SHADE = { core: "#06030c", stone: "#1e1030", rim: "#d08aff", lip: "#3a2058", lipDark: "#1a0e2a", lipLight: "#e6c8ff" };
  const RUIN = { core: "#070410", stone: "#22143a", rim: "#b48cff", lip: "#43286a", lipDark: "#1c1030", lipLight: "#d8c0ff" };
  const VCRYST = { dark: "#1c0a34", mid: "#7a3ad0", light: "#e6c4ff", glow: "#a65cff" };
  const GHOST = { fill: "#7a4ad0", edge: "#e2c8ff" };
  const EYE = { glow: "#b070ff", stone: "#140a22", core: "#fff0ff", iris: "#b45cff", dark: "#2a0a48", pupil: "#05020a" };
  const VOID = { deep: "#030108", spark: "#c89cff" };

  function temple(canvas) {
    const f = new Frame(canvas);
    f.sky("#07030f", "#1f0c33");
    f.nebula(16, 13, 30, 2.2, -0.15, ["#4a1e7a", "#7a2a6a", "#2a1a6a"], 0.35, 901);
    f.stars(260, 6, 18, "#e8d8ff", 902, 2);

    // Чорне сонце над храмом — головне світло кадру: фіолетова корона, рожеві спалахи.
    f.eclipse(19.4, 12.8, 2.3, { halo: "#6a2aa8", corona: "#b25cff", flare: "#ff7ad0", disc: "#030106", rim: "#f0d0ff" }, 903);

    // Далекий план: уламки руїн у порожнечі на двох глибинах.
    f.floatingRuin(4.5, 9.5, 6, 4.2, { stone: "#1c0f32", window: "#5a2a8a" }, 911);
    f.floatingRuin(29.0, 7.4, 5, 3.4, { stone: "#1a0e2e", window: "#7a3aa0" }, 912);
    f.fog(3, 11, "#3a1a5a", 0.4);
    // Середній план: обвалена аркада храму — колони без склепіння, сонце між ними.
    [[3.0, 9.6, 1.0], [10.2, 12.4, 1.1], [16.4, 6.4, 0.9], [23.0, 11.2, 1.05], [28.4, 7.8, 0.95]].forEach(([x, h, w], i) =>
      f.brokenColumn(x, 2.4, h, w, "#160b28", "#c080ff", 913 + i));
    f.fog(0, 5, "#4a2070", 0.3);

    // Безодня під храмом і пасма тіні, що з неї піднімаються.
    f.voidBelow(3.2, VOID, 921);
    f.tendrils(12, 27, -1, 6.5, "#5a2490", 0.55, 922);

    // Ігрова площина: підлога храму зліва, обірвана над безоднею; уступ з оком-порталом справа.
    f.mass({ points: [[-1, -1], [-1, 3.0], [12.6, 3.0], [13.4, 1.6], [12.4, -0.2], [9.6, -1]], tops: [1], seed: 931, erosion: 0.24 }, SHADE);
    f.mass({ points: [[23.6, 0.6], [23.2, 2.2], [24.0, 4.2], [33, 4.2], [33, 0.6], [28.0, -0.6]], tops: [2], seed: 932, erosion: 0.26 }, SHADE);
    // тіньові платформи над безоднею — видно лише контуром (Місячне око), між ними — серп-стрибок
    f.shadePlatform(14.4, 16.8, 3.6, GHOST, 933);
    f.shadePlatform(18.4, 20.2, 5.0, GHOST, 934);
    f.shadePlatform(21.0, 22.8, 3.4, GHOST, 935);
    // око-портал на уступі справа, вівтар-око на підлозі зліва — чужий, холодний вогонь
    f.eyePortal(29.0, 7.4, 3.6, EYE, 936);
    f.c.fillStyle = "#0c0616";
    f.c.beginPath(); f.c.moveTo(...f.px(27.6, 4.2)); f.c.quadraticCurveTo(...f.px(29.0, 5.4), ...f.px(30.4, 4.2)); f.c.closePath(); f.c.fill();
    // зламана статуя без обличчя біля краю
    f.hooded(4.4, 3.0, 4.2, "#120a1e", "#b080e0");

    f.hero(9.0, 3.0, 1);

    // Передній план: уламок колони й висячі ланцюги.
    f.foreground((c, fr) => {
      c.beginPath();
      c.moveTo(...fr.px(-1, 19)); c.lineTo(...fr.px(2.2, 19));
      c.bezierCurveTo(...fr.px(1.6, 15), ...fr.px(2.4, 12.4), ...fr.px(1.4, 10.6)); c.lineTo(...fr.px(-1, 10.0)); c.closePath(); c.fill();
      c.beginPath();
      c.moveTo(...fr.px(33, -1)); c.lineTo(...fr.px(26.5, -1));
      c.quadraticCurveTo(...fr.px(30, 0.9), ...fr.px(33, 1.4)); c.closePath(); c.fill();
    });
    f.chain(25.4, 12.6, 18.6, "#160a24", 0.9);

    f.dust(70, "#e2c8ff", 23);
    f.vignette(0.66);
  }

  function bridge(canvas) {
    const f = new Frame(canvas);
    f.sky("#05020c", "#170a2a");
    f.nebula(10, 14, 22, 1.8, 0.25, ["#3a1a6a", "#6a2a7a"], 0.3, 951);
    f.stars(320, 4, 18, "#efe2ff", 952, 3);

    // Серп затемнення за шпилем — холодне світло всього кадру.
    f.crescentEclipse(21.5, 11.6, 3.1, { halo: "#7a3ad0", light: "#efe0ff", disc: "#06030c" });
    f.c.fillStyle = "#0c0618";
    f.c.beginPath(); f.c.moveTo(...f.px(20.4, 5.0)); f.c.bezierCurveTo(...f.px(20.9, 9), ...f.px(21.1, 12.5), ...f.px(21.5, 16.2));
    f.c.bezierCurveTo(...f.px(21.9, 12.5), ...f.px(22.1, 9), ...f.px(22.6, 5.0)); f.c.closePath(); f.c.fill();

    // Далекий план: руїни-острови, далекий міст, туман.
    f.floatingRuin(5.0, 11.0, 5.4, 3.6, { stone: "#120822", window: "#4a2a7a" }, 961);
    f.archBridge(-1, 12, 8.2, 0.6, { stone: "#120822" }, 962);
    f.farIsland(27.5, 9.6, 6, 3.6, "#130924", 963, "#b070ff");
    f.fog(2, 10, "#341a52", 0.42);

    f.voidBelow(2.4, VOID, 971);
    f.tendrils(0, 32, -1, 4.5, "#4a1e78", 0.5, 972);
    // уламки, що висять у порожнечі, з кришталем знизу
    f.island(14.2, 17.6, 1.6, 1.8, RUIN, VCRYST, 973);
    f.island(29.6, 32.4, 0.6, 1.4, RUIN, VCRYST, 974);

    // Ігрова площина: міст з арками, середній проліт обвалений — його тримають тіньові платформи.
    f.archBridge(-1, 12.8, 5.0, 2.4, { stone: "#170c28" }, 981);
    f.mass({ points: [[-1, 4.0], [-1, 5.0], [12.8, 5.0], [13.2, 4.3], [12.4, 3.9]], tops: [1], seed: 982, erosion: 0.12 }, RUIN);
    f.archBridge(19.6, 33, 5.0, 2.4, { stone: "#170c28" }, 983);
    f.mass({ points: [[19.2, 4.0], [19.6, 5.0], [33, 5.0], [33, 4.0]], tops: [1], seed: 984, erosion: 0.12 }, RUIN);
    f.shadePlatform(13.8, 15.6, 5.4, GHOST, 985);
    f.shadePlatform(16.6, 18.6, 6.2, GHOST, 986);
    // око-портал на мосту справа — вертикальний мигдаль у кам'яній рамі
    f.eyePortal(27.8, 7.6, 4.2, EYE, 987);
    f.c.fillStyle = "#0c0616";
    f.c.beginPath(); f.c.moveTo(...f.px(26.2, 5.0)); f.c.quadraticCurveTo(...f.px(27.8, 6.1), ...f.px(29.4, 5.0)); f.c.closePath(); f.c.fill();
    // балюстрада мосту: точені стовпчики з м'якою «грушею», частина впала
    const r = rng(988), c = f.c, S = f.S;
    c.fillStyle = "#120a20";
    for (let x = 0.2; x < 33; x += 0.75) {
      if ((x > 12.4 && x < 20.0) || (x > 25.6 && x < 30.2) || r() < 0.28) continue;
      const h = 0.85 - r() * (r() < 0.15 ? 0.4 : 0);
      c.beginPath(); c.moveTo(...f.px(x - 0.09, 5.0));
      c.bezierCurveTo(...f.px(x - 0.24, 5.0 + h * 0.3), ...f.px(x - 0.06, 5.0 + h * 0.6), ...f.px(x - 0.08, 5.0 + h));
      c.lineTo(...f.px(x + 0.08, 5.0 + h));
      c.bezierCurveTo(...f.px(x + 0.06, 5.0 + h * 0.6), ...f.px(x + 0.24, 5.0 + h * 0.3), ...f.px(x + 0.09, 5.0)); c.closePath(); c.fill();
    }
    c.beginPath(); c.moveTo(...f.px(0, 5.9)); c.lineTo(...f.px(12.2, 5.9)); c.moveTo(...f.px(20.2, 5.9)); c.lineTo(...f.px(25.4, 5.9));
    c.strokeStyle = "#120a20"; c.lineWidth = S * 0.1; c.stroke();
    f.hero(6.6, 5.0, 1);

    f.foreground((cx, fr) => {
      cx.beginPath();
      cx.moveTo(...fr.px(-1, 19)); cx.lineTo(...fr.px(7, 19));
      cx.quadraticCurveTo(...fr.px(3, 17.4), ...fr.px(-1, 16.2)); cx.closePath(); cx.fill();
      const rr = rng(990);
      for (let i = 0; i < 4; i++) {
        const x = 1 + rr() * 6, len = 1 + rr() * 2.2, w = 0.2 + rr() * 0.25;
        cx.beginPath(); cx.moveTo(...fr.px(x - w, 17.4)); cx.lineTo(...fr.px(x + w, 17.4));
        cx.quadraticCurveTo(...fr.px(x + w * 0.4, 17.4 - len * 0.6), ...fr.px(x, 17.4 - len)); cx.quadraticCurveTo(...fr.px(x - w * 0.4, 17.4 - len * 0.6), ...fr.px(x - w, 17.4)); cx.fill();
      }
    });

    f.dust(60, "#e2c8ff", 29);
    f.vignette(0.66);
  }

  temple(document.getElementById("temple"));
  bridge(document.getElementById("bridge"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
