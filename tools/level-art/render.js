// Арт рівнів, намальований painter'ом (design/levels/painter) з тексту зони, через headless Chrome:
// камінь площини шматками 16 × 16 юнітів (48 px/юніт) і тло кімнат (mid — паралакс, near — стіна).
// Потім у Unity: ContentBuilder.RefreshLevelArt і Scene<Зона>.Apply; кімнати з фоном Midjourney —
// ще раз `python tools/midjourney/place.py <зона>` (render перемальовує їхні mid і near).
// Запуск (macOS і Windows): node tools/level-art/render.js tomb [паралельно=4]
// Chrome: змінна CHROME=<шлях>, інакше — звичайне місце встановлення.
const fs = require("fs");
const os = require("os");
const path = require("path");
const http = require("http");
const { spawn, spawnSync } = require("child_process");

const ROOT = path.resolve(__dirname, "..", "..");
const zone = process.argv[2] || "tomb";
const jobsAtOnce = Math.max(1, parseInt(process.argv[3] || "4", 10));
const Zone = zone.charAt(0).toUpperCase() + zone.slice(1);
const OUT = path.join(ROOT, "Assets/Game/Art/Levels", Zone);
const URL = "http://localhost:8123/design/levels/painter/index.html";

function chromePath() {
  if (process.env.CHROME) return process.env.CHROME;
  const candidates = process.platform === "win32"
    ? ["C:/Program Files/Google/Chrome/Application/chrome.exe",
       "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe",
       path.join(os.homedir(), "AppData/Local/Google/Chrome/Application/chrome.exe")]
    : ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/usr/bin/google-chrome", "/usr/bin/chromium"];
  const found = candidates.find((p) => fs.existsSync(p));
  if (!found) throw new Error("no Chrome found; set CHROME=<path>");
  return found;
}

function answers(url) {
  return new Promise((resolve) => {
    http.get(url, (res) => { res.resume(); resolve(res.statusCode === 200); }).on("error", () => resolve(false));
  });
}

async function serve() {
  if (await answers(URL)) return null;
  const server = spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore" });
  for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  return server;
}

// Chrome writes the picture and may not quit by itself: wait for the file, then stop it.
function shot(chrome, file, w, h, url) {
  return new Promise((resolve) => {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), "level-art-"));
    fs.rmSync(file, { force: true });
    const child = spawn(chrome, ["--headless=new", "--disable-gpu", "--hide-scrollbars",
      "--default-background-color=00000000", `--user-data-dir=${profile}`, "--no-first-run",
      "--no-default-browser-check", `--window-size=${w},${h}`, "--virtual-time-budget=60000",
      `--screenshot=${file}`, url], { stdio: "ignore" });
    const started = Date.now();
    const timer = setInterval(() => {
      const done = fs.existsSync(file) && fs.statSync(file).size > 0;
      if (done || Date.now() - started > 60000) {
        clearInterval(timer);
        setTimeout(() => {
          child.kill();
          // Windows keeps the profile locked for a moment after the kill: retry, then let it be.
          fs.rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 }, () => {});
          if (!done) console.log(`failed: ${file}`);
          resolve(done);
        }, 300);
      }
    }, 100);
  });
}

async function main() {
  const chrome = chromePath();
  const server = await serve();
  for (const dir of ["Terrain", "Rooms", "Breakables"]) fs.mkdirSync(path.join(OUT, dir), { recursive: true });
  const listed = spawnSync(process.execPath, [path.join(__dirname, "jobs.js"), zone], { encoding: "utf8" });
  if (listed.status !== 0) throw new Error(listed.stderr);
  const jobs = listed.stdout.trim().split(/\r?\n/).filter(Boolean).map((line) => {
    const [kind, name, w, h, query] = line.split(" ");
    const dir = kind === "terrain" ? "Terrain" : kind === "breakable" ? "Breakables" : "Rooms";
    return { file: path.join(OUT, dir, `${name}.png`), w, h, url: `${URL}?zone=${zone}&${query}` };
  });
  let next = 0;
  const worker = async () => {
    while (next < jobs.length) {
      const job = jobs[next++];
      await shot(chrome, job.file, job.w, job.h, job.url);
    }
  };
  await Promise.all(Array.from({ length: jobsAtOnce }, worker));
  if (server) server.kill();
  const count = (dir) => fs.readdirSync(path.join(OUT, dir)).filter((f) => f.endsWith(".png")).length;
  console.log(`level art: ${count("Terrain")} terrain, ${count("Rooms")} rooms, ${count("Breakables")} walls -> ${OUT}`);
}

main().catch((e) => { console.error(e.message || e); process.exit(1); });
