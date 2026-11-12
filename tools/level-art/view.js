// Кадр painter'а без Unity: камінь, уступи й тло кімнат там, де стоїть кімната (32 × 18 юнітів).
// Для швидкого погляду на правку тексту зони чи матеріалів, до render.js.
//   node tools/level-art/view.js tomb room_tomb_chain_shaft [out.png] [ppu=40] [dx=0] [dy=0]
// dx, dy — зсув кадру від лівого нижнього кута кімнати, в юнітах.
const fs = require("fs");
const path = require("path");
const http = require("http");
const { spawn } = require("child_process");

const ROOT = path.resolve(__dirname, "..", "..");
const [zone, roomId, outArg, ppuArg, dxArg, dyArg] = process.argv.slice(2);
const URL = "http://localhost:8123/design/levels/painter/index.html";

function roomCorner() {
  const text = fs.readFileSync(path.join(ROOT, "Assets/Game/Levels", `${zone}.txt`), "utf8");
  const cell = parseFloat((text.match(/^cell\s+([\d.]+)/m) || [0, "0.5"])[1]);
  const [ox, oy] = text.match(/^origin\s+(\S+)\s+(\S+)/m).slice(1).map(Number);
  const m = text.match(new RegExp(`^room ${roomId} .* at (-?\\d+) (-?\\d+)`, "m"));
  if (!m) throw new Error(`no room ${roomId}`);
  return [ox + Number(m[1]) * cell, oy + Number(m[2]) * cell];
}

const answers = (url) => new Promise((resolve) => {
  http.get(url, (res) => { res.resume(); resolve(res.statusCode === 200); }).on("error", () => resolve(false));
});

(async () => {
  let server = null;
  if (!(await answers(URL))) {
    server = spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore" });
    for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  }
  const [x, y] = roomCorner();
  const ppu = parseFloat(ppuArg || "40");
  const out = path.resolve(outArg || `Artifacts/views/${roomId}.png`);
  fs.mkdirSync(path.dirname(out), { recursive: true });
  const browser = await require("../_shared/browser.js").open();
  const vx = x + parseFloat(dxArg || "0"), vy = y + parseFloat(dyArg || "0");
  await browser.shot(out, Math.round(32 * ppu), Math.round(18 * ppu),
    `${URL}?zone=${zone}&what=view&x=${vx}&y=${vy}&ppu=${ppu}`);
  await browser.close();
  if (server) server.kill();
  console.log(out);
})().catch((e) => { console.error(e); process.exit(1); });
