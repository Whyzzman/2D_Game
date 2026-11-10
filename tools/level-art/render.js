// Арт рівнів, намальований painter'ом (design/levels/painter) з тексту зони, через headless Chrome:
// камінь площини шматками 16 × 16 юнітів (48 px/юніт) і тло кімнат (mid — паралакс, near — стіна).
// Потім у Unity: ContentBuilder.RefreshLevelArt і Scene<Зона>.Apply; кімнати з фоном Midjourney —
// ще раз `python tools/midjourney/place.py <зона>` (render перемальовує їхні mid і near).
// Запуск (macOS і Windows): node tools/level-art/render.js tomb [паралельно=4]
// Браузер — Chrome або Brave (tools/_shared/browser.js); змінна CHROME=<шлях> задає інший.
// Лише частина робіт: третій аргумент — початок імені файлу через кому (t_10_,room_tomb_column).
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

async function main() {
  const server = await serve();
  const browser = await require("../_shared/browser.js").open();
  for (const dir of ["Terrain", "Rooms", "Breakables"]) fs.mkdirSync(path.join(OUT, dir), { recursive: true });
  const listed = spawnSync(process.execPath, [path.join(__dirname, "jobs.js"), zone], { encoding: "utf8" });
  if (listed.status !== 0) throw new Error(listed.stderr);
  const jobs = listed.stdout.trim().split(/\r?\n/).filter(Boolean).map((line) => {
    const [kind, name, w, h, query] = line.split(" ");
    const dir = kind === "terrain" ? "Terrain" : kind === "breakable" ? "Breakables" : "Rooms";
    return { file: path.join(OUT, dir, `${name}.png`), w, h, url: `${URL}?zone=${zone}&${query}` };
  });
  const only = (process.argv[4] || "").split(",").filter(Boolean);
  if (only.length) {
    for (let i = jobs.length - 1; i >= 0; i--) {
      if (!only.some((prefix) => path.basename(jobs[i].file).startsWith(prefix))) jobs.splice(i, 1);
    }
  }
  let next = 0;
  const worker = async () => {
    while (next < jobs.length) {
      const job = jobs[next++];
      await browser.shot(job.file, job.w, job.h, job.url);
    }
  };
  await Promise.all(Array.from({ length: jobsAtOnce }, worker));
  await browser.close();
  if (server) server.kill();
  const count = (dir) => fs.readdirSync(path.join(OUT, dir)).filter((f) => f.endsWith(".png")).length;
  console.log(`level art: ${count("Terrain")} terrain, ${count("Rooms")} rooms, ${count("Breakables")} walls -> ${OUT}`);
}

main().catch((e) => { console.error(e.message || e); process.exit(1); });
