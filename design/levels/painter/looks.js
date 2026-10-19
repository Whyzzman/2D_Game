// Вигляд зон для painter: кольори каменю, тла й світла, і який характер у кожної кімнати.
// Гробниця — погоджений макет design/levels/tomb-look (2026-10-04).
(function (root) {
  "use strict";
  root.LevelLooks = {
    tomb: {
      // камінь площини
      core: "#0a0b0e", stone: "#20252e", rim: "#a9bdd8",
      lip: "#77757a", lipDark: "#3a3b42", lipLight: "#c4beb0", chain: "#2e333c",
      // тло
      skyTop: "#151c28", skyBottom: "#0a0d12",
      fog: "#34465c", ribs: "#18202c", window: "#253650",
      midWall: "#1a2029", midHole: "#0f131a", midColumn: "#1f2530", midColumnLight: "#29303d",
      nearWall: "#222831", nearHole: "#151a22", nearColumn: "#2a303b", nearColumnLight: "#353d4c",
      statue: "#313844", statueLight: "#3f4858",
      banners: [["#3A1620", "#8A7434"], ["#2a1119", "#6e5c2b"], ["#1d2638", "#8A7434"]],
      beam: "#a9c0dd", dust: "#c9d6e8",
      caveFog: "#24414a", caveRock: "#141b1e", caveRockLight: "#1d272b", fungus: "#6fd0c4",
      secretGlow: "#d9b45a", arenaGlow: "#c9d6ff",
      // кімнати, що стали печерами (обвал, вода), і кімнати з водою
      caves: ["room_tomb_collapsed_crypt", "room_tomb_rubble_slope", "room_tomb_falls", "room_tomb_lower_crypt",
        "room_tomb_flooded_crypt", "room_tomb_buried_cache", "room_tomb_crypt_nest"],
      water: ["room_tomb_flooded_crypt", "room_tomb_falls", "room_tomb_collapsed_crypt"]
    },
    // Ліс — погоджений макет design/levels/forest-look (2026-10-19): холодна зелень, місячне срібло,
    // мох на кантах; тло — forest-backdrop.js.
    forest: {
      style: "forest",
      core: "#090c0b", stone: "#1f2a25", rim: "#b4c8c4",
      lip: "#4e6656", lipDark: "#2a3a31", lipLight: "#b9c9b8", chain: "#121a15",
      moss: "#4e6656", hang: "roots",
      skyTop: "#1a2428", skyBottom: "#0b1012", moon: "#d6e2dc",
      farTree: "#1b2527", midTree: "#141c1d", nearTree: "#0c1111", ruin: "#1d2729", nearRuin: "#131a1a",
      fog: "#3c5054", fogLow: "#33454a", beam: "#c8d8d2", dust: "#d6e2dc",
      thicketTop: "#141c1c", thicketBottom: "#090d0d",
      caveTop: "#0e1512", caveBottom: "#060807", caveRoot: "#141c18", nearRoot: "#070a09", caveFog: "#2c4236",
      caveRock: "#0c110f", caveDust: "#c9e0c6", fungus: "#a6dccd", fungusGlow: "#6fd0c4",
      poisonGlow: "#7bb045", secretGlow: "#d9b45a",
      // як у генераторі (ContentBuilderForestRooms): нижче лінії землі — під землею; закриті — своя стіна
      groundLine: -5.5,
      enclosed: ["room_forest_root_tunnel", "room_forest_gate_arch", "room_forest_old_graves",
        "room_forest_stone_bridge", "room_forest_grove_stash", "room_forest_hunter_hollow",
        "room_forest_hollow_trunk", "room_forest_falconer_nest", "room_forest_under_walkway",
        "room_forest_root_nest", "room_forest_ravine", "room_forest_ravine_foot", "room_forest_deep_roots",
        "room_forest_tree_grave", "room_forest_lower_gallery", "room_forest_thorn_course",
        "room_forest_mushroom_pool", "room_forest_mushroom_cache", "room_forest_falconer_ladder",
        "room_forest_poison_grove"],
      underground: ["room_forest_root_tunnel", "room_forest_hollow_trunk", "room_forest_root_nest"],
      poison: ["room_forest_poison_grove", "room_forest_mushroom_pool", "room_forest_deep_roots"],
      caves: []
    },
    // Село — погоджений макет design/levels/village-look (2026-10-19): присмерк із жаром над Столицею,
    // темне дерево, глина й солома, бронза дзвону; тло — village-backdrop.js.
    village: {
      style: "village",
      core: "#0c0907", stone: "#2a211a", rim: "#d79a5c",
      lip: "#6a5640", lipDark: "#3a2e22", lipLight: "#d8c4a0", chain: "#24180f",
      skyTop: "#1c1414", skyBottom: "#5a3424", ember: "#d0642a", smoke: "#4a3026", fog: "#5a3a2a",
      towerStone: "#241a17", bell: "#6e522c", bellGlow: "#f2b05a",
      farHouse: { wall: "#2a1d18", beam: "#1a120e", roof: "#24180f", roofLight: "#5a4028", window: "#1a110c", windowLit: "#e08a40", lit: 0.15, broken: 0.5 },
      nearHouse: { wall: "#1e1813", beam: "#0b0806", roof: "#1b140e", roofLight: "#6a5634", window: "#070504", windowLit: "#f4a852", lit: 0.35, broken: 0.3 },
      beam: "#f0c08a", warmBeam: "#f0b070", dust: "#f0c890",
      innerTop: "#140f0d", innerBottom: "#0a0807", warmFog: "#4a3024",
      nave: "#1d1714", naveHole: "#120e0c", naveColumn: "#231c18", naveColumnLight: "#2f2620", vault: "#1a1411",
      glass: ["#a8452a", "#c98a3a", "#3a5a7a", "#7a2a3a", "#d9b45a"], flame: "#f4a852",
      logFar: "#1c1510", beamWood: "#140e0a", barrel: "#2a1e14", secretGlow: "#d9b45a",
      // як у генераторі (ContentBuilderVillage): відкриті вулиці — небо; кам'яні — церква, крипта, погреби
      open: ["room_village_hill", "room_village_lower_street", "room_village_feather", "room_village_square",
        "room_village_stilt_houses", "room_village_upper_houses", "room_village_upper_street",
        "room_village_marsh_road", "room_village_bell"],
      church: "room_village_church",
      stone: ["room_village_church", "room_village_crypt_cache", "room_village_belfry_base", "room_village_bell_shaft",
        "room_village_cellar", "room_village_well_cache", "room_village_bridge", "room_village_stair"],
      caves: []
    }
  };
})(window);
