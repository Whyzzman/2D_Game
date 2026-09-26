/* Крихітний статичний сервер, щоб прототип відкривався зі спільною таблицею
   стилів. Через file:// відносні посилання не резолвляться, і сторінка
   показується без оформлення — а погоджувати треба саме оформлення. */
const http = require("http");
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..", "..", "..");
const port = 8123;

const types = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png"
};

http.createServer((request, response) => {
  let relative = decodeURIComponent(request.url.split("?")[0]);
  if (relative === "/") {
    relative = "/design/ui/index.html";
  }

  const file = path.join(root, relative);
  if (!file.startsWith(root)) {
    response.writeHead(403).end("forbidden");
    return;
  }

  fs.readFile(file, (error, data) => {
    if (error) {
      response.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" });
      response.end("not found: " + relative);
      return;
    }

    response.writeHead(200, {
      "Content-Type": types[path.extname(file).toLowerCase()] || "application/octet-stream",
      "Cache-Control": "no-store"
    });
    response.end(data);
  });
}).listen(port, () => console.log("ui prototype on http://localhost:" + port + "/"));
