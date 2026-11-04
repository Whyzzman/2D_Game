// Where the painter put candles and glowing mushrooms in a zone, for real lights in its scene
// (2026-11-04, «the game should look expensive»): headless Chrome runs the painter's own placement
// (index.html?what=lights) and the list goes to Assets/Game/Art/Levels/<Zone>/lights.json; then in Unity
// SceneLevelLights.Apply puts a Light2D on each, so the hero walking past a candle is warmed by it.
// Run after render.js: node tools/level-art/lights.js tomb
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawnSync } = require("child_process");

const ROOT = path.resolve(__dirname, "..", "..");
const zone = process.argv[2] || "tomb";
const Zone = zone.charAt(0).toUpperCase() + zone.slice(1);
const chrome = process.env.CHROME || ["C:/Program Files/Google/Chrome/Application/chrome.exe",
  "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/usr/bin/google-chrome"].find((p) => fs.existsSync(p));
const profile = fs.mkdtempSync(path.join(os.tmpdir(), "lights-"));
const url = `http://localhost:8123/design/levels/painter/index.html?zone=${zone}&what=lights`;
const out = spawnSync(chrome, ["--headless=new", "--disable-gpu", `--user-data-dir=${profile}`, "--no-first-run",
  "--virtual-time-budget=60000", "--dump-dom", url], { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 });
const m = /<pre id="lights">([\s\S]*?)<\/pre>/.exec(out.stdout || "");
if (!m) { console.error("no lights in the page (is design/ui/_shared/serve.js running?)"); process.exit(1); }
const lights = JSON.parse(m[1].replace(/&quot;/g, '"').replace(/&amp;/g, "&"));
const file = path.join(ROOT, "Assets/Game/Art/Levels", Zone, "lights.json");
fs.writeFileSync(file, JSON.stringify({ lights }, null, 0) + "\n");
const kinds = lights.reduce((a, l) => (a[l.kind] = (a[l.kind] || 0) + 1, a), {});
console.log(`${zone}: ${lights.length} lights ${JSON.stringify(kinds)} -> ${path.relative(ROOT, file)}`);
