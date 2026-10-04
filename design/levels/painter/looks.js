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
    }
  };
})(window);
