// Local-only receiver for Midjourney downloads: a page on cdn.midjourney.com POSTs image bytes to
// http://127.0.0.1:8125/<name> and they are saved as <folder>/<name>.png (gamedesign/MIDJOURNEY.md).
// Run: node tools/midjourney/receive.js <folder>
const http = require("http");
const fs = require("fs");
const path = require("path");
const OUT = process.argv[2];
http.createServer((req, res) => {
  res.setHeader("Access-Control-Allow-Origin", "https://cdn.midjourney.com");
  res.setHeader("Access-Control-Allow-Methods", "POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Private-Network", "true");
  if (req.method === "OPTIONS") { res.end(); return; }
  const name = decodeURIComponent((req.url || "").slice(1));
  if (req.method !== "POST" || !/^[a-z0-9_]+$/.test(name)) { res.statusCode = 400; res.end("bad"); return; }
  const chunks = [];
  req.on("data", (c) => chunks.push(c));
  req.on("end", () => {
    const buf = Buffer.concat(chunks);
    fs.writeFileSync(path.join(OUT, name + ".png"), buf);
    console.log("saved", name, buf.length);
    res.end("ok " + buf.length);
  });
}).listen(8125, "127.0.0.1", () => console.log("listening 8125"));
