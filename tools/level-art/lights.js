// Where the painter put candles and glowing mushrooms in a zone, for real lights in its scene
// (2026-11-04, «the game should look expensive»): headless Chrome runs the painter's own placement
// (index.html?what=lights) and the list goes to Assets/Game/Art/Levels/<Zone>/lights.json; then in Unity
// SceneLevelLights.Apply puts a Light2D on each, so the hero walking past a candle is warmed by it.
// Run after render.js: node tools/level-art/lights.js tomb
const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..", "..");
const zone = process.argv[2] || "tomb";
const Zone = zone.charAt(0).toUpperCase() + zone.slice(1);
const url = `http://localhost:8123/design/levels/painter/index.html?zone=${zone}&what=lights`;

(async () => {
  const browser = await require("../_shared/browser.js").open();
  const html = await browser.dom(url);
  await browser.close();
  const m = /<pre id="lights">([\s\S]*?)<\/pre>/.exec(html);
  if (!m) { console.error("no lights in the page (is design/ui/_shared/serve.js running?)"); process.exit(1); }
  const lights = JSON.parse(m[1].replace(/&quot;/g, '"').replace(/&amp;/g, "&"));
  const file = path.join(ROOT, "Assets/Game/Art/Levels", Zone, "lights.json");
  fs.writeFileSync(file, JSON.stringify({ lights }, null, 0) + "\n");
  const kinds = lights.reduce((a, l) => (a[l.kind] = (a[l.kind] || 0) + 1, a), {});
  console.log(`${zone}: ${lights.length} lights ${JSON.stringify(kinds)} -> ${path.relative(ROOT, file)}`);
})();
