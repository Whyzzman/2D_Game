// Дві сцени макета Лісу. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Камінь і земля Лісу: холодна зелена серцевина, мох на гранях, місячне срібло на канті.
  const EARTH = {
    core: "#090c0b", stone: "#1f2a25", rim: "#b4c8c4",
    lip: "#4e6656", lipDark: "#2a3a31", lipLight: "#b9c9b8"
  };
  const MOSS = "#4e6656";

  function edge(canvas) {
    const f = new Frame(canvas);
    f.sky("#1a2428", "#0b1012");
    f.moon(24.5, 14.2, 1.1, "#d6e2dc");

    // Далекий план: руїна абатства в тумані, ряд дерев за нею.
    f.treeRow({ from: -2, to: 34, gap: 3.2, ground: 2, height: 9, width: 0.35, color: "#1b2527", seed: 3 });
    f.abbey(6, 17.5, 2, 13.5, "#1d2729", 5);
    f.fog(1, 9, "#3c5054", 0.55);

    // Середній план: густіший ліс, світліший туман між рядами.
    f.treeRow({ from: -1, to: 34, gap: 4.4, ground: 1.5, height: 11, width: 0.6, color: "#141c1d", seed: 8 });
    f.fog(0.5, 5, "#33454a", 0.4);

    // Місячний промінь крізь крони — маршрут освітлений.
    f.shaft(20.5, 18.4, 0.28, 2.6, 18, "#c8d8d2", 0.22);
    f.shaft(12.4, 18.6, 0.24, 1.2, 14, "#c8d8d2", 0.12);

    // Ігрова площина: узлісся, земля горбом, коріння-уступи, старий кам'яний поріг абатства.
    f.mass({ points: [[-1, -1], [-1, 2.2], [6.4, 2.2], [7.0, 2.7], [13.8, 2.7], [14.6, 2.0], [22.6, 2.0],
      [23.1, 2.5], [33, 2.5], [33, -1]], tops: [1, 3, 5, 7], seed: 41, erosion: 0.18 }, EARTH);
    f.moss(-1, 6.4, 2.2, MOSS, 1); f.moss(7.0, 13.8, 2.7, MOSS, 2); f.moss(23.1, 33, 2.5, MOSS, 3);

    // Уступи: зламаний карниз абатства і два корені, що вигнулись дугою.
    f.mass({ points: [[3.2, 6.4], [7.6, 6.4], [7.4, 5.8], [6.2, 5.5], [4.4, 5.7], [3.4, 6.0]], tops: [0], seed: 42, erosion: 0.14 }, EARTH);
    f.moss(3.2, 7.6, 6.4, MOSS, 4);
    f.root([[9.8, 2.6], [10.6, 5.6], [12.8, 9.4], [15.6, 9.9], [17.4, 9.6], [18.2, 7.4], [18.6, 2.2]], 0.55, "#0f1513", 6);
    f.mass({ points: [[13.2, 9.9], [17.6, 9.9], [17.5, 9.4], [15.4, 9.15], [13.4, 9.4]], tops: [0], seed: 43, erosion: 0.12 }, EARTH);
    f.moss(13.2, 17.6, 9.9, MOSS, 5);
    f.mass({ points: [[24.4, 6.8], [29.2, 6.8], [29.0, 6.2], [27.2, 5.9], [25.2, 6.1]], tops: [0], seed: 44, erosion: 0.14 }, EARTH);
    f.moss(24.4, 29.2, 6.8, MOSS, 6);

    // Ліва стіна — схил узлісся, що йде в темряву.
    f.mass({ points: [[-1, 2.2], [1.2, 2.2], [1.8, 5.4], [1.4, 8.8], [2.2, 12.6], [1.8, 19.5], [-1, 19.5]],
      tops: [], seed: 45, erosion: 0.4 }, EARTH);

    f.hero(9.2, 2.7, 1);

    // Передній план: стовбур, що перекриває край, і гілка зверху — лише силует.
    f.foreground((c, fr) => {
      const S = fr.S;
      c.beginPath();
      c.moveTo(...fr.px(29.6, -1));
      c.quadraticCurveTo(...fr.px(30.6, 4), ...fr.px(30.2, 19));
      c.lineTo(...fr.px(33, 19)); c.lineTo(...fr.px(33, -1)); c.closePath(); c.fill();
      c.lineCap = "round"; c.strokeStyle = c.fillStyle;
      const r = rng(9);
      let x = 30.4, y = 15.6;
      c.lineWidth = S * 0.5;
      c.beginPath(); c.moveTo(...fr.px(x, y));
      for (let i = 0; i < 9; i++) { x -= 0.9; y += 0.25 + (r() - 0.5) * 0.4; c.lineTo(...fr.px(x, y)); }
      c.stroke();
      for (let x2 = 0; x2 < 29; x2 += 0.6 + r() * 1.8) {
        if (r() < 0.5) continue;
        const [px, py] = fr.px(x2, -0.1);
        c.beginPath(); c.ellipse(px, py, S * (0.3 + r() * 0.7), S * (0.3 + r() * 0.5), 0, Math.PI, 0); c.fill();
      }
    });

    f.dust(70, "#d6e2dc", 5);
    f.vignette(0.6);
  }

  function roots(canvas) {
    const f = new Frame(canvas);
    const DEEP = { core: "#080a09", stone: "#1d2520", rim: "#7fae9a", lip: "#3e5244", lipDark: "#222e27", lipLight: "#a8bea8" };
    f.sky("#0e1512", "#060807");

    // Далекий план: стіна з переплетеного коріння в зеленкуватому тумані.
    const r = rng(12);
    for (let i = 0; i < 16; i++) {
      const x = r() * 34 - 1;
      f.root([[x, 19], [x + (r() - 0.5) * 3, 13], [x + (r() - 0.5) * 4, 7], [x + (r() - 0.5) * 3, 1]], 0.35 + r() * 0.4, "#141c18", 20 + i);
    }
    f.fog(0, 12, "#2c4236", 0.5);

    // Промінь крізь дупло нагорі — і отруйне світло заводі знизу.
    f.shaft(16.2, 18.6, 0.06, 1.4, 16, "#c7d9c9", 0.2);
    f.glow(17, 0.8, 8, "#7bb045", 0.28);

    // Заводь з отрутою.
    const c = f.c, S = f.S;
    const [wx0, wy0] = f.px(10.2, 1.3), [wx1, wy1] = f.px(23.8, -1);
    const wg = c.createLinearGradient(0, wy0, 0, wy1);
    wg.addColorStop(0, "rgba(140,190,90,0.55)"); wg.addColorStop(0.2, "rgba(45,70,35,0.85)"); wg.addColorStop(1, "rgba(10,18,10,0.95)");
    c.fillStyle = wg; c.fillRect(wx0, wy0, wx1 - wx0, wy1 - wy0);
    const rb = rng(14);
    for (let i = 0; i < 14; i++) {
      const [bx, by] = f.px(10.6 + rb() * 12.8, 1.2 - rb() * 0.4);
      c.fillStyle = "rgba(180,220,120,0.35)";
      c.beginPath(); c.arc(bx, by, S * (0.04 + rb() * 0.08), 0, Math.PI * 2); c.fill();
    }

    // Ігрова площина: стеля з землі й коріння, береги, корінь-міст через заводь.
    f.mass({ points: [[-1, 19.5], [33, 19.5], [33, 15.2], [28.0, 14.6], [24.6, 15.8], [19.2, 16.6], [16.9, 17.4],
      [15.4, 17.4], [12.4, 16.0], [8.0, 15.2], [4.2, 16.0], [-1, 15.0]], seed: 51, erosion: 0.55, course: 1.3 }, DEEP);
    f.mass({ points: [[-1, -1], [-1, 8.8], [2.8, 8.8], [3.6, 7.6], [3.3, 5.6], [6.4, 5.6], [7.0, 4.2], [6.8, 3.2],
      [8.6, 3.2], [9.4, 1.6], [10.6, 1.4], [10.4, -1]], tops: [1, 4, 7], seed: 52, erosion: 0.4 }, DEEP);
    f.mass({ points: [[23.6, -1], [23.4, 1.4], [25.4, 1.6], [26.0, 3.4], [25.8, 3.6], [28.4, 3.6], [29.0, 5.2],
      [28.8, 5.4], [33, 5.4], [33, -1]], tops: [1, 4, 7], seed: 53, erosion: 0.4 }, DEEP);
    [[-1, 2.8, 8.8], [3.3, 6.4, 5.6], [6.8, 8.6, 3.2], [23.4, 25.4, 1.6], [25.8, 28.4, 3.6], [28.8, 33, 5.4]]
      .forEach(([a, b, y], i) => f.moss(a, b, y, "#3e5244", 10 + i));

    // Корінь-міст: по ньому йдуть, тож верх рівний і з кантом.
    f.root([[9.6, 3.0], [12.4, 3.8], [16.8, 4.0], [21.2, 3.7], [24.2, 2.6]], 0.7, "#121a15", 7);
    f.mass({ points: [[12.6, 4.15], [21.0, 4.15], [20.6, 3.7], [16.8, 3.55], [13.0, 3.7]], tops: [0], seed: 54, erosion: 0.08 }, DEEP);
    f.moss(12.6, 21.0, 4.15, "#3e5244", 20);

    // Високий уступ — тайник видно, але треба другий стрибок.
    f.mass({ points: [[27.2, 10.2], [33, 10.2], [33, 9.0], [30.0, 8.6], [27.8, 9.1]], tops: [0], seed: 55, erosion: 0.3 }, DEEP);
    f.glow(29.8, 10.9, 1.6, "#d9b45a", 0.32);

    // Бліді гриби на дорозі.
    [[2.2, 8.8], [4.6, 5.6], [7.6, 3.2], [16.2, 4.15], [26.8, 3.6], [31.2, 10.2]].forEach(([x, y], i) =>
      f.fungus(x, y, 0.55 + (i % 3) * 0.15, "#a6dccd", "#6fd0c4"));

    f.hero(4.8, 5.6, 1);

    // Передній план: корені зі стелі й темна брила.
    f.foreground((cx, fr) => {
      const rr = rng(60);
      for (let i = 0; i < 6; i++) {
        const x = 2 + rr() * 28;
        f.root([[x, 19], [x + (rr() - 0.5), 17], [x + (rr() - 0.5) * 1.5, 14.5 - rr() * 2]], 0.18 + rr() * 0.2, "#050606", 70 + i);
      }
      cx.beginPath();
      cx.moveTo(...fr.px(33, -1)); cx.lineTo(...fr.px(33, 3.6));
      cx.quadraticCurveTo(...fr.px(31, 3.9), ...fr.px(30, 1.4));
      cx.quadraticCurveTo(...fr.px(29.4, 0.2), ...fr.px(27.5, -1));
      cx.closePath(); cx.fill();
    });

    f.dust(60, "#c9e0c6", 11);
    f.vignette(0.68);
  }

  edge(document.getElementById("edge"));
  roots(document.getElementById("roots"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
