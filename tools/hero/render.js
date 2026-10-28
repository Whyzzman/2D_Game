// Кадри героя з design/characters/hero (hero.js) через headless Chrome: кожен стан — ряд кадрів, потім
// tools/hero/split.py ріже їх на PNG у Art/Source/Final/Characters/Player/<Стан>/ (рисований арт, генератор
// його не стирає) і на місце заглушок. Далі в Unity: CharacterNormals.AttachAll() для героя.
// Запуск: node tools/hero/render.js   (Chrome: змінна CHROME=<шлях>, інакше звичайне місце)
const fs = require("fs");
const os = require("os");
const path = require("path");
const http = require("http");
const { spawn } = require("child_process");

const ROOT = path.resolve(__dirname, "..", "..");
const OUT = path.join(os.tmpdir(), "hero-frames");
const URL = "http://localhost:8123/design/characters/hero/index.html";
const PLAYER = path.join(ROOT, "Assets/Game/Art/Characters/Player");

function chromePath() {
  if (process.env.CHROME) return process.env.CHROME;
  const c = ["C:/Program Files/Google/Chrome/Application/chrome.exe", "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/usr/bin/google-chrome"];
  return c.find((p) => fs.existsSync(p));
}

function answers(url) {
  return new Promise((resolve) => http.get(url, (res) => { res.resume(); resolve(res.statusCode === 200); }).on("error", () => resolve(false)));
}

function shot(chrome, file, w, h, url) {
  return new Promise((resolve) => {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), "hero-"));
    fs.rmSync(file, { force: true });
    const child = spawn(chrome, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
      `--user-data-dir=${profile}`, "--no-first-run", `--window-size=${w},${h}`, "--virtual-time-budget=20000", `--screenshot=${file}`, url], { stdio: "ignore" });
    const started = Date.now();
    const timer = setInterval(() => {
      const done = fs.existsSync(file) && fs.statSync(file).size > 0;
      if (done || Date.now() - started > 60000) {
        clearInterval(timer);
        setTimeout(() => { child.kill(); fs.rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 }, () => {}); resolve(done); }, 300);
      }
    }, 100);
  });
}

// Стани й кількість кадрів — як у заглушок (PlaceholderPlayerArt): контракт анімації той самий.
function jobs() {
  const list = [];
  for (const dir of fs.readdirSync(PLAYER)) {
    const full = path.join(PLAYER, dir);
    if (!fs.statSync(full).isDirectory() || dir === "Cloak") continue;
    if (dir === "Greatsword") {
      for (const sub of fs.readdirSync(full)) {
        const f2 = path.join(full, sub);
        if (!fs.statSync(f2).isDirectory()) continue;
        const n = fs.readdirSync(f2).filter((f) => f.endsWith(".png")).length;
        list.push({ state: sub, folder: `Greatsword/${sub}`, n, big: true });
      }
      continue;
    }
    const n = fs.readdirSync(full).filter((f) => f.endsWith(".png")).length;
    if (n > 0) list.push({ state: dir, folder: dir, n, big: false });
  }
  return list;
}

async function main() {
  const chrome = chromePath();
  if (!(await answers(URL))) {
    spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore", detached: true }).unref();
    for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  }
  fs.mkdirSync(OUT, { recursive: true });
  const all = jobs();
  let next = 0;
  const worker = async () => {
    while (next < all.length) {
      const j = all[next++];
      const file = path.join(OUT, `${j.folder.replace("/", "__")}.png`);
      await shot(chrome, file, 256 * j.n, 224, `${URL}?state=${j.state}&frames=${j.n}${j.big ? "&big=1" : ""}`);
    }
  };
  await Promise.all([1, 2, 3, 4, 5, 6].map(worker));
  fs.writeFileSync(path.join(OUT, "jobs.json"), JSON.stringify(all));
  console.log(`hero: ${all.length} states -> ${OUT}`);
}

main().catch((e) => { console.error(e); process.exit(1); });
