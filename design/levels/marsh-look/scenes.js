// Дві сцени макета Боліт. Координати в юнітах гри, y — угору, кадр 32 × 18.
(function () {
  "use strict";
  const { Frame, rng } = window.TombLook;

  // Гнила деревина й торф: чорно-зелена серцевина, ядовитий відблиск на гранях знизу, світлий мох на канті.
  const PEAT = { core: "#070908", stone: "#1b211a", rim: "#9fd35a", lip: "#4a5a34", lipDark: "#232c1c", lipLight: "#bcd890" };
  const BOARDS = { core: "#0b0907", stone: "#241c14", rim: "#a8d060", lip: "#4e3f2a", lipDark: "#2a2116", lipLight: "#b8a27a" };
  const POISON = { surface: "#6fa83a", water: "#1d3a1c", deep: "#050806", film: "#c4e86a", glint: "#dcf59a", glow: "#7fd040" };
  const HUT = { wall: "#1d1a14", beam: "#0b0907", roof: "#1c1a10", roofLight: "#5a6234", window: "#070806", windowLit: "#a8e060",
    lit: 0.6, broken: 0.3, slime: "#4a7a2a" };
  const MOSS = "#55703a";

  function tree(canvas) {
    const f = new Frame(canvas);
    f.sky("#0a100c", "#16241a");
    f.glow(16, 1, 24, "#5fae34", 0.22);

    // Далекий план: ряди мертвих дерев у тумані, далекі хати з вогниками.
    f.treeRow({ from: -2, to: 34, ground: 1.5, height: 10, width: 0.7, gap: 3.4, color: "#121a14", seed: 501 });
    f.fog(1, 8, "#2c4a2a", 0.45);
    f.treeRow({ from: -1, to: 33, ground: 1, height: 12, width: 1.0, gap: 5.2, color: "#0d130e", seed: 502 });
    f.fog(0.5, 5, "#3a5a30", 0.35);

    // Середній план: велетенське гниле дерево — стовбур, що розходиться корінням у воду.
    f.deadOak(12.5, 1.6, 17, 3.2, "#080a08", 511);
    f.root([[10.4, 2.4], [8.6, 1.4], [6.4, 1.6], [4.6, 0.6]], 0.55, "#080a08", 512);
    f.root([[14.4, 2.2], [16.8, 1.2], [19.0, 1.5]], 0.5, "#080a08", 513);
    f.beardMoss(8, 18, 13.5, 3.2, MOSS, 514);
    f.beardMoss(5, 11, 10.5, 2.4, MOSS, 515);
    f.cage(15.6, 8.4, 1.8, { chain: "#121410", iron: "#1c1f18", bones: "#8a8a70" });

    // Хата відьми на палях угорі праворуч — тепле зелене вікно, єдине «домашнє» світло.
    f.stiltHut(23.4, 9.6, 6.2, 3.6, HUT, 521);
    f.glow(26.5, 11.4, 2.2, "#c8f070", 0.3);

    // Ігрова площина: ядовита вода внизу, гнилі дошки над нею, торф'яні купини.
    f.toxicWater(-1, 33, 1.1, -1, POISON, 531);
    f.mass({ points: [[-1, 0.6], [-1, 2.4], [5.6, 2.4], [6.2, 1.6], [6.0, 0.6]], tops: [1], seed: 532, erosion: 0.2 }, PEAT);
    f.moss(-1, 5.6, 2.4, MOSS, 533);
    // дощатий настил на палях: рівний верх — по ньому ходять
    f.stilts([7.4, 10.2, 13.0, 15.8, 18.6], 2.6, -0.6, "#0b0907", "#4a7a2a", 534);
    f.mass({ points: [[6.6, 2.9], [20.4, 2.9], [20.3, 2.45], [6.7, 2.45]], tops: [0], seed: 535, erosion: 0.05 }, BOARDS);
    f.mass({ points: [[22.2, 2.6], [33, 2.6], [33, 0.6], [22.8, 0.6], [22.0, 1.6]], tops: [0], seed: 536, erosion: 0.2 }, PEAT);
    f.moss(22.2, 33, 2.6, MOSS, 537);
    // кореневий поміст під кліткою і містки до хати
    f.mass({ points: [[13.2, 6.6], [18.4, 6.6], [18.2, 6.1], [15.6, 5.8], [13.4, 6.1]], tops: [0], seed: 538, erosion: 0.14 }, PEAT);
    f.moss(13.2, 18.4, 6.6, MOSS, 539);
    f.mass({ points: [[20.2, 9.6], [30.6, 9.6], [30.5, 9.2], [20.3, 9.2]], tops: [0], seed: 540, erosion: 0.05 }, BOARDS);
    f.reeds(-1, 5, 2.4, "#3f5a2c", 541);
    f.reeds(22.4, 32, 2.6, "#3f5a2c", 542);
    f.wisps(6, 2, 30, 1.6, 5.5, "#9fe060", 543);

    f.hero(10.6, 2.9, 1);

    // Передній план: звисла гілка й купина рогозу — лише силует.
    f.foreground((c, fr) => {
      const r = rng(550);
      c.beginPath();
      c.moveTo(...fr.px(-1, 19)); c.lineTo(...fr.px(9, 19));
      c.quadraticCurveTo(...fr.px(4, 17.6), ...fr.px(-1, 15.6)); c.closePath(); c.fill();
      c.strokeStyle = c.fillStyle; c.lineCap = "round";
      for (let x = 26; x < 33.5; x += 0.15 + r() * 0.2) {
        c.lineWidth = fr.S * (0.04 + r() * 0.05);
        c.beginPath(); c.moveTo(...fr.px(x, -0.5));
        c.quadraticCurveTo(...fr.px(x + (r() - 0.5) * 0.4, 1.4), ...fr.px(x + (r() - 0.5) * 0.9, 1.8 + r() * 2.2)); c.stroke();
      }
    });

    f.dust(70, "#c8f08a", 9);
    f.vignette(0.62);
  }

  function wreck(canvas) {
    const f = new Frame(canvas);
    f.sky("#080c0a", "#122018");
    f.glow(20, 0, 22, "#4f9a2e", 0.25);

    // Далекий план: колони затопленого цвинтаря й руїна каплиці в тумані.
    f.abbey(1, 11, 1.4, 9.5, "#111813", 601);
    f.fog(1, 7, "#2a4428", 0.45);
    const r = rng(602);
    for (let x = 12; x < 33; x += 1.6 + r() * 2.2) f.gravestone(x, 1.4, 1 + r() * 1.4, (r() - 0.5) * 0.5, "#141c16");
    f.fog(0.5, 4, "#36552e", 0.4);

    // Ігрова площина: чорна вода, похилий корабель, палуба — підлога.
    f.toxicWater(-1, 33, 1.4, -1, POISON, 611);
    // корпус лежить на боці й сидить у воді: похилено, а низ ховає друга, непрозора вода
    f.c.save();
    f.c.translate(...f.px(16, 1.2)); f.c.rotate(0.07); f.c.translate(-f.px(16, 1.2)[0], -f.px(16, 1.2)[1]);
    f.wreck(7, 0.2, 18, { hull: "#0a0907", plank: "#5a4a32", moss: MOSS }, 612);
    f.c.restore();
    f.toxicWater(-1, 33, 1.4, -1, POISON, 621);
    // палуба: рівний верх
    f.mass({ points: [[8.4, 3.6], [23.4, 3.6], [23.2, 3.15], [8.6, 3.15]], tops: [0], seed: 613, erosion: 0.04 }, BOARDS);
    // берег зліва й причал справа
    f.mass({ points: [[-1, 0.4], [-1, 2.6], [5.0, 2.6], [5.6, 1.4], [5.2, 0.4]], tops: [1], seed: 614, erosion: 0.2 }, PEAT);
    f.moss(-1, 5.0, 2.6, MOSS, 615);
    f.stilts([26.4, 29.0, 31.6], 2.8, -0.6, "#0b0907", "#4a7a2a", 616);
    f.mass({ points: [[25.6, 3.1], [33, 3.1], [33, 2.65], [25.7, 2.65]], tops: [0], seed: 617, erosion: 0.05 }, BOARDS);
    // верх щогли — уступ, до нього кільце гака
    f.mass({ points: [[18.9, 9.5], [21.5, 9.5], [21.3, 9.1], [19.1, 9.1]], tops: [0], seed: 618, erosion: 0.04 }, BOARDS);
    const c = f.c, S = f.S;
    f.glow(23.6, 12.4, 1.2, "#9ec8ff", 0.4);
    c.strokeStyle = "#8aa6c8"; c.lineWidth = S * 0.09;
    c.beginPath(); c.arc(...f.px(23.6, 12.4), S * 0.32, 0, Math.PI * 2); c.stroke();
    f.chain(23.6, 12.7, 18.6, "#141814", 0.8);
    // вівтар на кормі
    f.altar(14.2, 3.6);
    f.reeds(-1, 4.6, 2.6, "#3f5a2c", 619);
    f.wisps(8, 1, 31, 1.8, 6, "#9fe060", 620);

    f.hero(11.2, 3.6, 1);

    f.foreground((cx, fr) => {
      cx.beginPath();
      cx.moveTo(...fr.px(33, 19)); cx.lineTo(...fr.px(25, 19));
      cx.quadraticCurveTo(...fr.px(29, 16.8), ...fr.px(33, 14.8)); cx.closePath(); cx.fill();
      cx.beginPath();
      cx.moveTo(...fr.px(-1, -1)); cx.lineTo(...fr.px(-1, 1.6));
      cx.quadraticCurveTo(...fr.px(2.5, 2.0), ...fr.px(5, -1)); cx.closePath(); cx.fill();
    });

    f.dust(60, "#c8f08a", 10);
    f.vignette(0.64);
  }

  tree(document.getElementById("tree"));
  wreck(document.getElementById("wreck"));
  if (location.search.indexOf("shot=") >= 0) {
    document.body.classList.add("shot");
    const which = location.search.match(/shot=(\w+)/)[1];
    document.querySelectorAll("figure").forEach(fig => { fig.style.display = fig.id === "f-" + which ? "" : "none"; });
  }
})();
