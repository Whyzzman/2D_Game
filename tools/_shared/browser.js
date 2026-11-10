// One headless browser for every tool that photographs a page (painter, characters, map), driven over the
// DevTools protocol instead of `--screenshot`: Brave hangs on that flag (2026-11-10, macOS), and one browser
// for the whole run is faster than one per picture. Chrome or Brave; CHROME=<path> overrides.
//   const browser = await require("../_shared/browser.js").open();
//   await browser.shot(file, w, h, url);        // transparent PNG once the page says document.title = "done"
//   const html = await browser.dom(url);        // the page's HTML at that moment
//   await browser.close();
// For a page that never sets the title pass { settle: ms }: it is taken that long after it has loaded.
const fs = require("fs");
const os = require("os");
const path = require("path");
const { spawn } = require("child_process");

function browserPath() {
  if (process.env.CHROME) return process.env.CHROME;
  const candidates = process.platform === "win32"
    ? ["C:/Program Files/Google/Chrome/Application/chrome.exe",
       "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe",
       path.join(os.homedir(), "AppData/Local/Google/Chrome/Application/chrome.exe"),
       "C:/Program Files/BraveSoftware/Brave-Browser/Application/brave.exe"]
    : ["/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
       "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser",
       "/usr/bin/google-chrome", "/usr/bin/chromium"];
  const found = candidates.find((p) => fs.existsSync(p));
  if (!found) throw new Error("no Chrome or Brave found; set CHROME=<path>");
  return found;
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function open() {
  const profile = fs.mkdtempSync(path.join(os.tmpdir(), "tool-browser-"));
  const port = 9300 + Math.floor(Math.random() * 600);
  const child = spawn(browserPath(), ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
    "--no-default-browser-check", `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, "about:blank"],
    { stdio: "ignore" });
  const base = `http://127.0.0.1:${port}`;
  let up = false;
  for (let i = 0; i < 80 && !up; i++) {
    try { await (await fetch(`${base}/json/version`)).json(); up = true; } catch (e) { await sleep(250); }
  }
  if (!up) { child.kill(); throw new Error("the browser did not open its debugging port"); }

  // A tab of its own for each page, so several can be drawn at once.
  async function withPage(url, w, h, timeout, settle, work) {
    const target = await (await fetch(`${base}/json/new?about:blank`, { method: "PUT" })).json();
    const ws = new WebSocket(target.webSocketDebuggerUrl);
    await new Promise((resolve, reject) => { ws.onopen = resolve; ws.onerror = reject; });
    let id = 0;
    const pending = new Map();
    ws.onmessage = (m) => {
      const d = JSON.parse(m.data);
      if (d.id && pending.has(d.id)) { pending.get(d.id)(d.result || {}); pending.delete(d.id); }
    };
    const send = (method, params = {}) => new Promise((r) => { pending.set(++id, r); ws.send(JSON.stringify({ id, method, params })); });
    try {
      await send("Emulation.setDeviceMetricsOverride", { width: w, height: h, deviceScaleFactor: 1, mobile: false });
      await send("Emulation.setDefaultBackgroundColorOverride", { color: { r: 0, g: 0, b: 0, a: 0 } });
      await send("Page.navigate", { url });
      const started = Date.now();
      let done = false;
      while (!done && Date.now() - started < timeout) {
        await sleep(100);
        const title = await send("Runtime.evaluate", { expression: "document.readyState + '|' + document.title" });
        const value = (title.result && title.result.value) || "";
        done = settle > 0 ? value.startsWith("complete|") && Date.now() - started > settle : value === "complete|done";
      }
      return await work(send, done);
    } finally {
      ws.close();
      await fetch(`${base}/json/close/${target.id}`).catch(() => {});
    }
  }

  return {
    async shot(file, w, h, url, { timeout = 120000, settle = 0 } = {}) {
      fs.rmSync(file, { force: true });
      return withPage(url, Number(w), Number(h), timeout, settle, async (send, done) => {
        if (!done) { console.log(`failed (page never finished): ${file}`); return false; }
        const picture = await send("Page.captureScreenshot", { format: "png" });
        if (!picture.data) { console.log(`failed: ${file}`); return false; }
        fs.writeFileSync(file, Buffer.from(picture.data, "base64"));
        return true;
      });
    },
    async dom(url, { timeout = 120000, settle = 0 } = {}) {
      return withPage(url, 1280, 720, timeout, settle, async (send) => {
        const html = await send("Runtime.evaluate", { expression: "document.documentElement.outerHTML" });
        return (html.result && html.result.value) || "";
      });
    },
    async close() {
      child.kill();
      await sleep(300);
      fs.rm(profile, { recursive: true, force: true, maxRetries: 10, retryDelay: 200 }, () => {});
    },
  };
}

module.exports = { open, browserPath };
