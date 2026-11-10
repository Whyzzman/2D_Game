// Кадри ворогів і боса Гробниці з design/characters/enemies (enemies.js) через headless Chrome: кожен стан —
// ряд кадрів у %TEMP%/enemy-frames, потім tools/enemies/split.py ріже їх на місце заглушок (і в Art/Painted.txt).
// Кількість кадрів станів — з таблиць PlaceholderEnemyArt (контракт анімації той самий).
// Запуск: node tools/enemies/render.js [tomb|forest|vc|marsh]   (Chrome: змінна CHROME=<шлях>)
const fs = require("fs");
const os = require("os");
const path = require("path");
const http = require("http");
const { spawn } = require("child_process");

const ROOT = path.resolve(__dirname, "..", "..");
const OUT = path.join(os.tmpdir(), "enemy-frames");
const URL = "http://localhost:8123/design/characters/enemies/index.html";
const EDITOR = path.join(ROOT, "Assets/Game/Editor");
// 2026-11-06: the big bosses' arcs and the villager's bash ran past the frame and were cut straight (decided by Claude
// under standing approval)
const PAD_SIDE = 0.9, PAD_TOP = 0.5, PAD_COLS = 8;

// actor → таблиця станів, файл таблиці, тека, префікс, розмір кадру
const ACTORS = [
  { actor: "ZombieSoldier", table: "ZombieStates", file: "PlaceholderEnemyArt.cs", folder: "Enemies/ZombieSoldier", prefix: "enemy_zombie", w: 140, h: 176 },
  { actor: "ShieldGuard", table: "GuardStates", file: "PlaceholderEnemyArt.cs", folder: "Enemies/ShieldGuard", prefix: "enemy_shieldguard", w: 168, h: 176 },
  { actor: "BoneArcher", table: "ArcherStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/BoneArcher", prefix: "enemy_bone_archer", w: 150, h: 176 },
  { actor: "GuardianOfTheTomb", table: "BossStates", file: "PlaceholderEnemyArt.cs", folder: "Bosses/GuardianOfTheTomb", prefix: "boss_guardian", w: 272, h: 300 },
  // Ліс (enemies-forest.js)
  { zone: "forest", actor: "CorruptedHound", table: "HoundStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/CorruptedHound", prefix: "enemy_hound", w: 200, h: 120 },
  { zone: "forest", actor: "ForestArcher", table: "ArcherStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/ForestArcher", prefix: "enemy_archer", w: 150, h: 176 },
  { zone: "forest", actor: "RootShaman", table: "ShamanStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/RootShaman", prefix: "enemy_shaman", w: 156, h: 178 },
  { zone: "forest", actor: "RoyalSpearman", table: "SpearmanStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/RoyalSpearman", prefix: "enemy_spearman", w: 214, h: 178 },
  { zone: "forest", actor: "HollowWarden", table: "WardenStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Bosses/HollowWarden", prefix: "boss_warden", w: 268, h: 260 },
  // Село й Столиця (enemies-vc.js): таблиці кадрів — ті, на які вони посилаються в PlaceholderEnemyArt
  { zone: "vc", actor: "PossessedVillager", table: "SpearmanStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/PossessedVillager", prefix: "enemy_villager", w: 214, h: 178 },
  { zone: "vc", actor: "Scarecrow", table: "GargoyleStates", file: "PlaceholderEnemyArt.Capital.cs", folder: "Enemies/Scarecrow", prefix: "enemy_scarecrow", w: 214, h: 178 },
  { zone: "vc", actor: "Watchdog", table: "HoundStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/Watchdog", prefix: "enemy_watchdog", w: 200, h: 120 },
  { zone: "vc", actor: "RoyalCrossbowman", table: "ArcherStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/RoyalCrossbowman", prefix: "enemy_crossbowman", w: 160, h: 176 },
  { zone: "vc", actor: "PalacePriest", table: "ShamanStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/PalacePriest", prefix: "enemy_priest", w: 156, h: 178 },
  { zone: "vc", actor: "StoneGargoyle", table: "GargoyleStates", file: "PlaceholderEnemyArt.Capital.cs", folder: "Enemies/StoneGargoyle", prefix: "enemy_gargoyle", w: 220, h: 186 },
  { zone: "vc", actor: "AshRaven", table: "RavenStates", file: "PlaceholderEnemyArt.Capital.cs", folder: "Enemies/AshRaven", prefix: "enemy_raven", w: 140, h: 110 },
  { zone: "vc", actor: "Bellringer", table: "KingStates", file: "PlaceholderEnemyArt.King.cs", folder: "Bosses/Bellringer", prefix: "boss_bellringer", w: 300, h: 300 },
  { zone: "vc", actor: "AshCommander", table: "KingStates", file: "PlaceholderEnemyArt.King.cs", folder: "Bosses/AshCommander", prefix: "boss_commander", w: 320, h: 300 },
  { zone: "vc", actor: "OathBoundKing", table: "KingStates", file: "PlaceholderEnemyArt.King.cs", folder: "Bosses/OathBoundKing", prefix: "boss_king", w: 300, h: 312 },
  // Болота (enemies-marsh.js)
  { zone: "marsh", actor: "MarshCorpseEater", table: "HoundStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/MarshCorpseEater", prefix: "enemy_corpse_eater", w: 200, h: 120 },
  { zone: "marsh", actor: "PlagueDoctor", table: "ShamanStates", file: "PlaceholderEnemyArt.Forest.cs", folder: "Enemies/PlagueDoctor", prefix: "enemy_plague_doctor", w: 156, h: 178 },
  { zone: "marsh", actor: "MarshLeech", table: "RavenStates", file: "PlaceholderEnemyArt.Capital.cs", folder: "Enemies/MarshLeech", prefix: "enemy_leech", w: 140, h: 110 },
  { zone: "marsh", actor: "PlagueStag", table: "KingStates", file: "PlaceholderEnemyArt.King.cs", folder: "Bosses/PlagueStag", prefix: "boss_stag", w: 300, h: 312 }
].filter((a) => (a.zone || "tomb") === (process.argv[2] || "tomb"));

function table(file, name) {
  const src = fs.readFileSync(path.join(EDITOR, file), "utf8");
  const at = src.indexOf(`[] ${name} =`);
  const body = src.slice(at, src.indexOf("};", at));
  return [...body.matchAll(/AnimationState\.(\w+), (\d+),/g)].map((m) => ({ state: m[1], n: +m[2] }));
}

function chromePath() {
  if (process.env.CHROME) return process.env.CHROME;
  return ["C:/Program Files/Google/Chrome/Application/chrome.exe", "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/usr/bin/google-chrome"].find((p) => fs.existsSync(p));
}

const answers = (url) => new Promise((r) => http.get(url, (res) => { res.resume(); r(res.statusCode === 200); }).on("error", () => r(false)));

function shot(chrome, file, w, h, url) {
  return new Promise((resolve) => {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), "enemy-"));
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

async function main() {
  const chrome = chromePath();
  if (!(await answers(URL))) {
    spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore", detached: true }).unref();
    for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  }
  fs.mkdirSync(OUT, { recursive: true });
  const jobs = [];
  for (const a of ACTORS) for (const s of table(a.file, a.table)) jobs.push({ ...a, ...s });
  let next = 0;
  const worker = async () => {
    while (next < jobs.length) {
      const j = jobs[next++];
      // room round the frame (most of its width each side, half its height above), frames in a grid: an arc or a
      // lunge past the frame is not cut by a straight line; split.py trims each state to what was drawn
      j.px = Math.round(j.w * PAD_SIDE); j.pt = Math.round(j.h * PAD_TOP); j.cols = Math.min(j.n, PAD_COLS);
      const W = j.w + 2 * j.px, H = j.h + j.pt;
      await shot(chrome, path.join(OUT, `${j.actor}__${j.state}.png`), W * j.cols, H * Math.ceil(j.n / j.cols),
        `${URL}?actor=${j.actor}&state=${j.state}&frames=${j.n}&px=${j.px}&pt=${j.pt}&cols=${j.cols}`);
    }
  };
  await Promise.all([1, 2, 3, 4, 5, 6].map(worker));
  fs.writeFileSync(path.join(OUT, "jobs.json"), JSON.stringify(jobs));
  console.log(`enemies: ${jobs.length} states -> ${OUT}`);
}

main().catch((e) => { console.error(e); process.exit(1); });
