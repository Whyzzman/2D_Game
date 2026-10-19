// Дві сцени макета Акведуків. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Мокрий темний камінь: чорна серцевина, бірюзовий відблиск води на гранях, світлий мокрий кант.
  const STONE = { core: "#05080a", stone: "#152022", rim: "#5fd6d0", lip: "#2b4a47", lipDark: "#122220", lipLight: "#9ae0d6" };
  const DAIS = { core: "#070b0c", stone: "#1b2829", rim: "#7fe8e0", lip: "#35585a", lipDark: "#162827", lipLight: "#b4ece4" };
  const WATER = { water: "#0f4a52", deep: "#020708", glint: "#9ff0e6", foam: "#bff4ee" };
  const FALL = { water: "#5fd0cc", alpha: 0.55, to: 1.2 };
  const VALVE = { glow: "#3fe07a", ring: "#3fbf62", core: "#c8ffd4", pipe: "#0d1513" };
  const WHEEL = { wood: "#0b0d0c", hub: "#1a1f1d", rim: "#7fe0d6", drip: "#7fe0d6" };
  const COLUMN = { stone: "#0c1415", light: "#7fd8d0", wet: "#020607", weed: "#2f5a4a" };
  const WEED = "#2f5a4a";

  function wheels(canvas) {
    const f = new Frame(canvas);
    f.sky("#071012", "#0c1d20");
    f.glow(15, 2, 22, "#2fa8a4", 0.22);

    // Далекий план: дворядний акведук у тумані, з обвалених прольотів стікає вода.
    f.glow(22, 12, 12, "#4fc8c0", 0.18);
    f.aqueductRow({ from: -2, to: 34, span: 3.1, base: 3, top: 15.4, broken: 0.3, seed: 701, color: "#101d1f", rim: "#5fd6d0",
      tiers: [{ base: 3, spring: 8.6, ratio: 0.68 }, { base: 12.4, spring: 13.4, ratio: 0.42 }],
      spill: { water: "#5fd0cc", alpha: 0.16, to: 3 } });
    f.fog(2, 12, "#1f5254", 0.5);
    f.shaft(6.5, 18.6, 0.18, 1.6, 16, "#8fe8e0", 0.12);

    // Середній план: великі арки ближчого акведука — крізь них і над ними видно далекий.
    f.aqueductRow({ from: -4.2, to: 33, span: 7.4, base: 1, top: 12.4, broken: 0.25, seed: 702, color: "#0a1214", rim: "#5fd6d0",
      tiers: [{ base: 1, spring: 7.2, ratio: 0.72 }, { base: 10.2, spring: 10.8, ratio: 0.3 }],
      spill: { water: "#6fe0d8", alpha: 0.3, to: 1.3 } });
    f.caustics(-1, 33, 1.2, 7.5, "#6fe0d8", 0.35, 703);
    f.weedCurtain(-0.6, 2.4, 12.4, 2.0, WEED, 704);
    f.weedCurtain(25.6, 28.2, 12.4, 1.6, WEED, 706);
    f.fog(0.5, 5, "#245a5a", 0.35);

    // Колесо стоїть у каналі: його низ сховає вода.
    f.waterWheel(15.6, 3.0, 3.5, WHEEL, 711);

    // Ігрова площина: канал на всю ширину, течія праворуч, кам'яні набережні по боках.
    f.water(-1, 33, 1.25, -1, WATER, 712);
    f.current(10.8, 20.6, 1.25, 1, "#bff4ee", 713);
    f.reflection(15.6, 1.2, 3.0, 2.4, "#6fe0d8", 0.3);
    f.mass({ points: [[-1, -1], [-1, 2.6], [10.6, 2.6], [11.1, 1.8], [10.7, 0.2], [9.8, -1]], tops: [1], seed: 714, erosion: 0.16 }, STONE);
    f.mass({ points: [[20.4, -1], [20.0, 1.0], [20.6, 2.6], [33, 2.6], [33, -1]], tops: [2], seed: 715, erosion: 0.16 }, STONE);

    // Шлюзова будка на правій набережній: сходинка, стіна з напівкруглим проходом, вентиль.
    const c = f.c, S = f.S;
    c.fillStyle = "#0b1314";
    c.beginPath();
    c.moveTo(...f.px(24.4, 4.9)); c.lineTo(...f.px(24.4, 10.4));
    c.bezierCurveTo(...f.px(25.6, 11.4), ...f.px(31.0, 11.4), ...f.px(32.2, 10.4));
    c.lineTo(...f.px(32.2, 4.9)); c.closePath(); c.fill();
    c.fillStyle = "#040708";
    c.beginPath();
    c.moveTo(...f.px(29.4, 4.9)); c.lineTo(...f.px(29.4, 7.4));
    c.bezierCurveTo(...f.px(29.4, 8.5), ...f.px(31.2, 8.5), ...f.px(31.2, 7.4));
    c.lineTo(...f.px(31.2, 4.9)); c.closePath(); c.fill();
    f.mass({ points: [[23.6, 2.6], [23.8, 4.9], [32.6, 4.9], [33, 4.4], [33, 2.6]], tops: [1], seed: 716, erosion: 0.08 }, STONE);
    f.valve(26.8, 7.6, 0.55, VALVE);
    f.reflection(26.8, 1.2, 1.2, 1.8, "#3fe07a", 0.2);

    // Водомет на опорі ліворуч: дуга води в канал.
    f.spout(10.4, 5.6, 1, 3.0, { stone: "#121b1c", mane: "#1a2627", water: "#8fe8e0", to: 1.3 }, 717);

    f.hero(5.6, 2.6, 1);

    // Передній план: край опори й труба з вентилем праворуч.
    f.foreground((cx, fr) => {
      cx.beginPath();
      cx.moveTo(...fr.px(-1, -1)); cx.lineTo(...fr.px(-1, 19)); cx.lineTo(...fr.px(1.4, 19));
      cx.bezierCurveTo(...fr.px(0.9, 14), ...fr.px(1.6, 6), ...fr.px(0.6, -1)); cx.closePath(); cx.fill();
      cx.beginPath();
      cx.moveTo(...fr.px(33, 15.6)); cx.bezierCurveTo(...fr.px(30.5, 15.4), ...fr.px(30.2, 16.6), ...fr.px(30.4, 19));
      cx.lineTo(...fr.px(33, 19)); cx.closePath(); cx.fill();
    });

    f.drips(26, 2, 30, 3, 12, "#bff4ee", 718);
    f.dust(50, "#9ff0e6", 719);
    f.vignette(0.62);
  }

  function throne(canvas) {
    const f = new Frame(canvas);
    f.sky("#05090b", "#0a1618");

    // Далекий план: ребра склепінь і високі вікна з бірюзовим світлом.
    f.farNave({ offset: 2.4, span: 7.2, base: -1, h: 18, rib: "#0d1516", window: "#12393b" });
    f.arcade({ top: 17, base: 1.6, span: 6.4, archH: 10.6, col: 1.0, offset: 1.0, seed: 731, broken: 0.15,
      wall: "#0b1213", hole: "#060d0e", mist: "rgba(40,150,150,0.32)", column: "#111b1c", columnLight: "#192a2b" });
    f.glow(21.2, 9, 12, "#2fb8b0", 0.3);
    f.shaft(12.4, 18.4, -0.12, 1.4, 15, "#8fe8e0", 0.13);
    f.shaft(29.0, 18.4, 0.1, 1.2, 14, "#8fe8e0", 0.1);
    f.fog(1, 8, "#1d4a4c", 0.42);

    // Помост і статуя на троні; з боків помосту — два водоспади в басейн.
    f.waterfall(15.6, 9.2, 1.7, 0.8, WATER, 732);
    f.waterfall(27.4, 9.6, 1.7, 0.8, WATER, 733);
    f.lordStatue(21.2, 3.4, 12, { throne: "#0e1718", stone: "#1a2627", rim: "#7fe8e0", eye: "#7ff4ea", bronze: "#2c3a30" }, 734);
    f.water(-1, 33, 1.7, -1, WATER, 735);
    f.mass({ points: [[15.0, 0.4], [15.4, 2.6], [16.4, 3.4], [26.2, 3.4], [27.0, 2.6], [27.4, 0.4]], tops: [2], seed: 736, erosion: 0.1 }, DAIS);
    f.reflection(21.2, 1.65, 4.0, 3.2, "#7ff4ea", 0.35);
    f.caustics(-1, 33, 1.8, 8, "#6fe0d8", 0.3, 737);

    // Ліва підлога й галереї — сухі місця, коли вода піднімається.
    f.mass({ points: [[-1, -1], [-1, 2.4], [8.6, 2.4], [9.2, 1.6], [8.8, -1]], tops: [1], seed: 738, erosion: 0.16 }, STONE);
    f.mass({ points: [[-1, 5.8], [-1, 6.6], [6.4, 6.6], [6.8, 6.1], [5.8, 5.5], [1.0, 5.4]], tops: [1], seed: 739, erosion: 0.12 }, STONE);
    f.mass({ points: [[9.6, 9.8], [13.6, 9.8], [13.4, 9.3], [11.6, 9.0], [9.8, 9.3]], tops: [0], seed: 740, erosion: 0.1 }, STONE);
    f.mass({ points: [[28.6, 6.0], [28.4, 6.8], [33, 6.8], [33, 5.4], [29.6, 5.5]], tops: [1], seed: 741, erosion: 0.12 }, STONE);
    f.wetColumn(29.6, -1, 6.0, 0.6, Object.assign({ water: 1.7 }, COLUMN), 742);

    // Три вентилі: на лівій стіні, над високим уступом, на правій стіні.
    f.valve(1.6, 9.8, 0.5, VALVE);
    f.valve(11.6, 12.8, 0.45, VALVE);
    f.valve(31.0, 10.0, 0.5, VALVE);
    f.reflection(1.6, 1.65, 0.8, 1.2, "#3fe07a", 0.2);

    f.hero(5.0, 2.4, 1);

    // Передній план: водорості з краю склепіння й колона праворуч.
    f.foreground((cx, fr) => {
      const r = rng(750);
      cx.beginPath();
      cx.moveTo(...fr.px(-1, 19)); cx.lineTo(...fr.px(12, 19));
      cx.bezierCurveTo(...fr.px(8, 18.2), ...fr.px(3, 18.0), ...fr.px(-1, 16.4)); cx.closePath(); cx.fill();
      cx.strokeStyle = cx.fillStyle; cx.lineCap = "round";
      for (let x = 0; x < 9; x += 0.3 + r() * 0.5) {
        cx.lineWidth = fr.S * (0.04 + r() * 0.05);
        cx.beginPath(); cx.moveTo(...fr.px(x, 18.4));
        cx.quadraticCurveTo(...fr.px(x + (r() - 0.5) * 0.5, 17), ...fr.px(x + (r() - 0.5) * 0.4, 16.6 - r() * 2)); cx.stroke();
      }
    });

    f.drips(24, 0, 32, 4, 16, "#bff4ee", 751);
    f.dust(60, "#9ff0e6", 752);
    f.vignette(0.66);
  }

  wheels(document.getElementById("wheels"));
  throne(document.getElementById("throne"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
