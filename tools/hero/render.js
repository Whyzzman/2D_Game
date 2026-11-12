// Кадри героя з design/characters/hero (hero.js) через headless Chrome: кожен стан — ряд кадрів, потім
// tools/hero/split.py ріже їх на PNG на місце заглушок у Art/Characters/Player/<Стан>/ (рисований арт: шлях у
// Art/Painted.txt, генератор його не стирає). Далі в Unity: CharacterNormals.AttachAll() для героя.
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

// Стани й кількість кадрів — з таблиць PlaceholderPlayerArt (States, GreatswordStates): контракт анімації один.
function jobs() {
  const src = fs.readFileSync(path.join(ROOT, "Assets/Game/Editor/PlaceholderPlayerArt.cs"), "utf8");
  const table = (name) => {
    const at = src.indexOf("StateSpec[] " + name);
    const body = src.slice(at, src.indexOf("};", at));
    return [...body.matchAll(/StateSpec\(PlayerAnimationState\.(\w+), (\d+),/g)].map((m) => ({ state: m[1], n: +m[2] }));
  };
  return table("States").map((j) => ({ ...j, folder: j.state, big: false }))
    .concat(table("GreatswordStates").map((j) => ({ ...j, folder: "Greatsword/" + j.state, big: true })));
}

async function main() {
  // один headless-браузер на весь прогін (tools/_shared/browser.js: Chrome або Brave; Brave на --screenshot зависає)
  if (!(await answers(URL))) {
    spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore", detached: true }).unref();
    for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  }
  fs.mkdirSync(OUT, { recursive: true });
  const all = jobs();
  const browser = await require("../_shared/browser.js").open();
  let next = 0;
  const worker = async () => {
    while (next < all.length) {
      const j = all[next++];
      const file = path.join(OUT, `${j.folder.replace("/", "__")}.png`);
      await browser.shot(file, 256 * j.n, 224, `${URL}?state=${j.state}&frames=${j.n}${j.big ? "&big=1" : ""}`);
    }
  };
  await Promise.all([1, 2, 3, 4].map(worker));
  await browser.close();
  fs.writeFileSync(path.join(OUT, "jobs.json"), JSON.stringify(all));
  console.log(`hero: ${all.length} states -> ${OUT}`);
}

main().catch((e) => { console.error(e); process.exit(1); });
