// Аркуш станів героя для перегляду (без нарізки): node tools/hero/sheet.js [out.png] [Idle:12,Run:10,...]
const path = require("path"), http = require("http"), { spawn } = require("child_process");
const ROOT = path.resolve(__dirname, "..", "..");
const URL = "http://localhost:8123/design/characters/hero/index.html";
const answers = (u) => new Promise((r) => http.get(u, (res) => { res.resume(); r(res.statusCode === 200); }).on("error", () => r(false)));
(async () => {
  let server = null;
  if (!(await answers(URL))) {
    server = spawn(process.execPath, [path.join(ROOT, "design/ui/_shared/serve.js")], { stdio: "ignore" });
    for (let i = 0; i < 50 && !(await answers(URL)); i++) await new Promise((r) => setTimeout(r, 100));
  }
  const states = process.argv[3] || "Idle:12,Run:10,JumpRise:3,Fall:3,Attack1:7,Dash:5";
  const rows = states.split(",").length, scale = 0.6;
  const b = await require("../_shared/browser.js").open();
  await b.shot(path.resolve(process.argv[2] || "Artifacts/hero-sheet.png"), Math.round(12 * 256 * scale), Math.round(rows * 224 * scale),
    `${URL}?sheet=1&states=${states}&scale=${scale}`);
  await b.close(); if (server) server.kill();
})().catch((e) => { console.error(e); process.exit(1); });
