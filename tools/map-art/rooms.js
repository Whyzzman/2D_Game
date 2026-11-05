// Картинки кімнат карти без zsh (Windows): ті, яких ще немає, з design/ui/world-map/map-rooms.json
// (ContentBuilder.ExportMapRooms). Запуск: node tools/map-art/rooms.js [art] — лише кімнати цього art (напр. marsh).
const fs = require("fs"), os = require("os"), path = require("path"), { spawn } = require("child_process");
const ROOT = path.resolve(__dirname, "..", ".."), OUT = path.join(ROOT, "Assets/Game/Art/Map/Rooms");
const URL = "http://localhost:8123/design/ui/world-map/render.html";
const chrome = process.env.CHROME || "C:/Program Files/Google/Chrome/Application/chrome.exe";
const only = process.argv[2];
function shot(file, w, h, url) {
  return new Promise((resolve) => {
    const profile = fs.mkdtempSync(path.join(os.tmpdir(), "map-"));
    const child = spawn(chrome, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--default-background-color=00000000",
      `--user-data-dir=${profile}`, "--no-first-run", `--window-size=${w},${h}`, "--virtual-time-budget=3000", `--screenshot=${file}`, url], { stdio: "ignore" });
    const t0 = Date.now(), timer = setInterval(() => {
      if ((fs.existsSync(file) && fs.statSync(file).size > 0) || Date.now() - t0 > 60000) {
        clearInterval(timer); setTimeout(() => { child.kill(); fs.rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 }, () => {}); resolve(); }, 300);
      }
    }, 100);
  });
}
(async () => {
  const rooms = JSON.parse(fs.readFileSync(path.join(ROOT, "design/ui/world-map/map-rooms.json"), "utf8")).rooms
    .filter((r) => (!only || r.art === only) && !fs.existsSync(path.join(OUT, `${r.id}.png`)));
  let next = 0;
  const work = async () => { while (next < rooms.length) { const r = rooms[next++];
    const w = Math.round(r.w), h = Math.round(r.h), s = h < 100 ? 2 : 1;
    await shot(path.join(OUT, `${r.id}.png`), w * s, h * s, `${URL}?room=${r.id}&art=${r.art}&w=${w}&h=${h}&s=${s}${r.boss ? "&boss=1" : ""}`); } };
  await Promise.all([1, 2, 3, 4].map(work));
  console.log(`map rooms: ${rooms.length} drawn`);
})();
