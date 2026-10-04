// Дві сцени макета Гробниці. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  const TOMB = {
    core: "#0a0b0e", stone: "#262b35", rim: "#a9bdd8",
    lip: "#77757a", lipDark: "#3a3b42", lipLight: "#c4beb0"
  };

  // Низ склепіння: ряд стрілчастих арок між опорами, від правого краю до лівого.
  function vaultUnderside(spans, springY, apexY) {
    const pts = [];
    spans.forEach(([l, r]) => {
      const cx = (l + r) / 2, half = (r - l) / 2;
      for (let i = 0; i <= 16; i++) {
        const t = i / 16; // 0 — права п'ята, 1 — ліва
        const x = r - (r - l) * t;
        const u = Math.abs(x - cx) / half; // 1 біля п'яти, 0 у вершині
        const y = springY + (apexY - springY) * Math.pow(1 - u, 0.62);
        pts.push([x, y]);
      }
    });
    return pts;
  }

  function hall(canvas) {
    const f = new Frame(canvas);
    f.sky("#141b26", "#0a0d12");

    // Далекий план: нава, що йде в туман.
    f.farNave({ offset: 1.5, span: 8.5, base: 0, h: 18.5, rib: "#18202c", window: "#253650" });
    f.fog(2, 10, "#34465c", 0.5);

    // Середній план: аркада бокового нефу, частина прольотів обвалена.
    f.arcade({ top: 14.5, base: 2.4, span: 4.6, archH: 8.2, col: 0.7, offset: 0.8, seed: 3, broken: 0.35,
      wall: "#1a2029", hole: "#0f131a", mist: "rgba(47,62,82,0.55)", column: "#1f2530", columnLight: "#29303d" });
    f.fog(1.5, 6, "#2F3E52", 0.28);

    // Ближній план: стіна самої зали — великі арки, у нішах лицарі-ефігії, прапори на колонах.
    f.arcade({ top: 17, base: 2.2, span: 8, archH: 11, col: 1.4, offset: 4, seed: 9, broken: 0,
      wall: "#222831", hole: "#151a22", mist: "rgba(47,62,82,0.35)", column: "#2a303b", columnLight: "#353d4c" });
    f.statue(4, 2.4, 4.6, "#313844", "#3f4858");
    f.statue(28, 2.4, 4.6, "#313844", "#3f4858");
    f.banner(8, 12.4, 4.4, "#3A1620", "#8A7434");
    f.banner(24, 12.4, 3.6, "#2a1119", "#6e5c2b");

    // Промені крізь пролом склепіння — маршрут освітлений.
    f.shaft(21, 16.6, 0.2, 2.4, 16, "#a9c0dd", 0.30);
    f.shaft(9.2, 16.8, 0.14, 1.2, 12, "#a9c0dd", 0.14);

    // Ігрова площина.
    const ceiling = [[-1, 19.5], [33, 19.5], [33, 15.4]]
      .concat(vaultUnderside([[24.2, 31.5], [16.2, 24.2], [8.2, 16.2], [1.6, 8.2]], 15.3, 17.6))
      .concat([[-1, 15.4]]);
    f.mass({ points: ceiling, seed: 4, erosion: 0.18, course: 1.1 }, TOMB);

    f.mass({ points: [[-1, 6.2], [1.5, 6.2], [2.1, 7.6], [1.8, 10.2], [2.4, 13.4], [2.2, 19.5], [-1, 19.5]],
      seed: 7, erosion: 0.3 }, TOMB);
    f.mass({ points: [[30.3, 5.6], [33, 5.6], [33, 17], [30.9, 17], [31.2, 12.2], [30.6, 8.6]],
      seed: 8, erosion: 0.3 }, TOMB);

    // Підлога зі сходинкою до вівтаря.
    f.mass({ points: [[-1, -1], [-1, 2], [12.6, 2], [13.1, 2.55], [13.6, 2.55], [14.0, 3.1], [21.4, 3.1],
      [22.0, 2.4], [33, 2.4], [33, -1]], tops: [1, 3, 5, 7], seed: 2, erosion: 0.12 }, TOMB);

    // Уламки склепіння, що стали уступами (з ланцюгами, на яких колись висіли люстри).
    f.chain(5.1, 6.6, 15.6, "#2c313a", 1);
    f.chain(8.7, 6.6, 15.6, "#2c313a", 1);
    f.mass({ points: [[4.6, 6.6], [9.2, 6.6], [9.0, 6.1], [8.3, 5.65], [7.0, 5.85], [5.6, 5.45], [4.9, 6.0]],
      tops: [0], seed: 11, erosion: 0.16 }, TOMB);
    f.mass({ points: [[14.6, 10.4], [18.4, 10.4], [18.2, 9.8], [16.9, 9.5], [15.4, 9.75]],
      tops: [0], seed: 12, erosion: 0.16 }, TOMB);
    f.mass({ points: [[23.4, 7.4], [27.4, 7.4], [27.2, 6.8], [25.7, 6.45], [24.2, 6.7]],
      tops: [0], seed: 13, erosion: 0.16 }, TOMB);

    f.rubble(0.4, 4.2, 2, "#30343c", 5);
    f.rubble(22.6, 25.4, 2.4, "#30343c", 6);
    f.rubble(28.8, 30.2, 2.4, "#30343c", 7);

    f.altar(17.6, 3.1);
    f.hero(10.2, 2, 1);

    // Передній план: колона, ланцюг, обламана арка — лише силует.
    f.foreground((c, fr) => {
      const S = fr.S;
      const [x0, y0] = fr.px(-0.8, 19), [x1, y1] = fr.px(0.9, -1);
      c.fillRect(x0, y0, x1 - x0, y1 - y0);
      const [bx, by] = fr.px(-0.8, 1.6); c.fillRect(bx, by, S * 2.4, S * 2.6);
      const [cx, cy] = fr.px(-0.8, 15.4); c.fillRect(cx, cy, S * 2.2, S * 0.5);
      c.beginPath();
      const [ax, ay] = fr.px(27, 18.5); c.moveTo(ax, ay);
      const [bx2, by2] = fr.px(33, 18.5); c.lineTo(bx2, by2);
      const [cx2, cy2] = fr.px(33, 14.5); c.lineTo(cx2, cy2);
      c.quadraticCurveTo(fr.px(30.5, 16.4)[0], fr.px(30.5, 16.4)[1], fr.px(29.2, 17.1)[0], fr.px(29.2, 17.1)[1]);
      c.lineTo(fr.px(28.4, 16.6)[0], fr.px(28.4, 16.6)[1]);
      c.lineTo(fr.px(27.6, 17.4)[0], fr.px(27.6, 17.4)[1]);
      c.closePath(); c.fill();
      c.strokeStyle = "#07080b"; c.lineWidth = S * 0.09;
      for (let y = 18.4; y > 10.5; y -= 0.3) {
        const [px, py] = fr.px(31.6, y);
        c.beginPath(); c.ellipse(px, py, S * 0.1, S * 0.17, 0, 0, Math.PI * 2); c.stroke();
      }
      const r = rng(21);
      for (let x = 3; x < 31; x += 1 + r() * 3) {
        if (r() < 0.45) continue;
        const [px, py] = fr.px(x, -0.2);
        c.beginPath(); c.ellipse(px, py, S * (0.6 + r() * 1.2), S * (0.25 + r() * 0.4), 0, Math.PI, 0); c.fill();
      }
    });

    f.dust(140, "#c9d6e8", 3);
    f.vignette(0.62);
  }

  function crypt(canvas) {
    const f = new Frame(canvas);
    const CRYPT = { core: "#0b0f11", stone: "#25302f", rim: "#7fb4bc", lip: "#68716f", lipDark: "#343c3e", lipLight: "#b3bfb6" };
    f.sky("#0f171b", "#080b0d");

    // Далекий план: затоплені прольоти старої крипти, ледь видні.
    f.farNave({ offset: 4, span: 10, base: -1, h: 15, rib: "#141d21", window: "#1b2e33" });
    f.fog(0, 9, "#24414a", 0.4);
    f.arcade({ top: 11, base: 0.6, span: 5.2, archH: 6.8, col: 0.8, offset: 2.5, seed: 5, broken: 0.55,
      wall: "#172024", hole: "#0c1214", mist: "rgba(36,65,74,0.6)", column: "#1c262a", columnLight: "#253136" });
    f.fog(0, 4, "#24414a", 0.3);

    // Струмінь з тріщини і світло води — головне джерело кадру.
    f.shaft(17.2, 15.6, 0.0, 1.0, 15, "#9fd6dd", 0.22);
    f.glow(17, 1.4, 9, "#3f8f99", 0.32);

    // Вода в чаші склепу.
    const c = f.c, S = f.S;
    const [wx0, wy0] = f.px(9.2, 1.6), [wx1, wy1] = f.px(25.4, -1);
    const wg = c.createLinearGradient(0, wy0, 0, wy1);
    wg.addColorStop(0, "rgba(90,170,180,0.55)"); wg.addColorStop(0.15, "rgba(30,70,78,0.85)"); wg.addColorStop(1, "rgba(8,20,24,0.95)");
    c.fillStyle = wg; c.fillRect(wx0, wy0, wx1 - wx0, wy1 - wy0);
    const r = rng(4);
    c.strokeStyle = "rgba(170,225,230,0.35)"; c.lineWidth = S * 0.03;
    for (let i = 0; i < 26; i++) {
      const x = 9.6 + r() * 15.2, y = 1.5 - r() * 2.2, w = 0.4 + r() * 1.6;
      const [a, b] = f.px(x, y), [d] = f.px(x + w, y);
      c.beginPath(); c.moveTo(a, b); c.lineTo(d, b); c.stroke();
    }
    // струмінь
    const [sx, sy] = f.px(17.05, 14.6), [, sb] = f.px(0, 1.6);
    const sg = c.createLinearGradient(0, sy, 0, sb);
    sg.addColorStop(0, "rgba(200,240,245,0.75)"); sg.addColorStop(1, "rgba(160,220,230,0.35)");
    c.fillStyle = sg; c.fillRect(sx - S * 0.1, sy, S * 0.22, sb - sy);
    f.glow(17.1, 1.7, 2.2, "#cdeff2", 0.4);

    // Ігрова площина: обвалена крипта, що стала печерою.
    f.mass({ points: [[-1, 19.5], [33, 19.5], [33, 15.6], [27.5, 15.0], [24.0, 16.2], [20.4, 15.2], [17.6, 14.6],
      [16.6, 14.6], [13.5, 15.8], [9.8, 16.6], [6.2, 15.2], [3.0, 15.9], [-1, 15.4]], seed: 31, erosion: 0.55, course: 1.2 }, CRYPT);

    // Ліва скеля з уступами — спуск до води.
    f.mass({ points: [[-1, -1], [-1, 9.4], [2.6, 9.4], [3.4, 8.2], [3.1, 6.6], [5.6, 6.6], [6.3, 5.4], [6.0, 4.6],
      [7.4, 4.6], [8.2, 3.4], [8.0, 1.6], [9.6, 1.6], [9.4, -1]], tops: [1, 4, 7, 10], seed: 32, erosion: 0.4 }, CRYPT);
    // Права скеля вгору: підйом по три клітинки (1,5 юніта).
    f.mass({ points: [[25.0, -1], [24.8, 1.6], [26.4, 1.6], [26.9, 2.9], [26.6, 3.1], [28.6, 3.1], [29.2, 4.4],
      [29.0, 4.6], [33, 4.6], [33, -1]], tops: [1, 4, 7], seed: 33, erosion: 0.4 }, CRYPT);
    // Камінь посеред води — сходинка над чашею.
    f.mass({ points: [[12.4, 2.2], [14.6, 2.2], [14.9, 1.2], [14.3, 0.2], [12.9, 0.3], [12.1, 1.3]],
      tops: [0], seed: 34, erosion: 0.3 }, CRYPT);
    f.mass({ points: [[20.0, 2.6], [22.3, 2.6], [22.6, 1.5], [21.8, 0.3], [20.4, 0.4], [19.8, 1.5]],
      tops: [0], seed: 35, erosion: 0.3 }, CRYPT);
    // Високий уступ над водою — тайник видно, але не дістати без другого стрибка.
    f.mass({ points: [[27.6, 9.2], [33, 9.2], [33, 8.0], [30.5, 7.6], [28.4, 8.1]], tops: [0], seed: 36, erosion: 0.3 }, CRYPT);
    f.glow(29.6, 9.9, 1.6, "#d9b45a", 0.35);

    // Бліді гриби на камені — тьмяний натяк на дорогу.
    [[3.2, 9.4], [5.0, 6.6], [7.6, 4.6], [27.6, 3.1], [30.4, 9.2]].forEach(([x, y], i) => {
      f.glow(x, y + 0.15, 0.7, "#6fd0c4", 0.5);
      const [px, py] = f.px(x, y);
      c.fillStyle = "#9fe3d8";
      c.beginPath(); c.ellipse(px, py - S * 0.12, S * 0.12, S * 0.07, 0, Math.PI, 0); c.fill();
      c.fillRect(px - S * 0.015, py - S * 0.12, S * 0.03, S * 0.12);
      void i;
    });
    f.rubble(0.2, 2.4, 9.4, "#2c3437", 8);
    f.hero(4.6, 6.6, 1);

    // Передній план: коріння зі стелі й темна брила внизу.
    f.foreground((cx, fr) => {
      const S2 = fr.S, rr = rng(40);
      cx.strokeStyle = "#07090a"; cx.lineCap = "round";
      for (let i = 0; i < 7; i++) {
        let x = 19 + rr() * 10, y = 18.5;
        cx.lineWidth = S2 * (0.08 + rr() * 0.14);
        cx.beginPath();
        let [px, py] = fr.px(x, y); cx.moveTo(px, py);
        const len = 2 + rr() * 4.5;
        for (let s = 0; s < len; s += 0.4) {
          x += (rr() - 0.5) * 0.5; y -= 0.4;
          [px, py] = fr.px(x, y); cx.lineTo(px, py);
        }
        cx.stroke();
      }
      cx.beginPath();
      cx.moveTo(...fr.px(-1, -1)); cx.lineTo(...fr.px(-1, 4.2));
      cx.quadraticCurveTo(...fr.px(1.6, 4.6), ...fr.px(3.2, 1.8));
      cx.quadraticCurveTo(...fr.px(4.4, 0.4), ...fr.px(6.5, -1));
      cx.closePath(); cx.fill();
    });

    f.dust(90, "#bfe6ea", 9);
    f.vignette(0.66);
  }

  hall(document.getElementById("hall"));
  crypt(document.getElementById("crypt"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
