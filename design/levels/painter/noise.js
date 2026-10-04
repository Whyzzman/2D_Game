// Шум для painter рівнів: детермінований, щоб повторний рендер давав ту саму картинку.
(function (root) {
  "use strict";
  function hash(x, y, seed) {
    let h = (Math.imul(x | 0, 374761393) + Math.imul(y | 0, 668265263) + Math.imul(seed | 0, 2246822519)) | 0;
    h = Math.imul(h ^ (h >>> 13), 1274126177);
    return ((h ^ (h >>> 16)) >>> 0) / 4294967295;
  }
  const smooth = t => t * t * (3 - 2 * t);
  function noise2(x, y, seed) {
    const xi = Math.floor(x), yi = Math.floor(y), xf = smooth(x - xi), yf = smooth(y - yi);
    const a = hash(xi, yi, seed), b = hash(xi + 1, yi, seed), c = hash(xi, yi + 1, seed), d = hash(xi + 1, yi + 1, seed);
    return a + (b - a) * xf + (c - a) * yf + (a - b - c + d) * xf * yf;
  }
  function fbm(x, y, seed, octaves) {
    let sum = 0, amp = 0.5, f = 1, norm = 0;
    for (let i = 0; i < (octaves || 4); i++) { sum += amp * noise2(x * f, y * f, seed + i * 17); norm += amp; amp *= 0.5; f *= 2.03; }
    return sum / norm;
  }
  function rng(seed) {
    let s = seed >>> 0;
    return () => { s = (Math.imul(s ^ (s >>> 15), 2246822519) + 0x9e3779b9) >>> 0; return s / 4294967295; };
  }
  function seedOf(text) { let h = 7; for (const ch of text) h = (Math.imul(h, 31) + ch.charCodeAt(0)) | 0; return h >>> 0; }
  // Шум у пів кроку кольору на кожен піксель: без нього 8-бітні градієнти променів і туману
  // лягають смугами.
  function dither(canvas, seed) {
    const c = canvas.getContext("2d"), img = c.getImageData(0, 0, canvas.width, canvas.height), d = img.data;
    const r = rng(seed || 5);
    for (let i = 0; i < d.length; i += 4) {
      if (d[i + 3] === 0) continue;
      const n = (r() - 0.5) * 2.2;
      d[i] += n; d[i + 1] += n; d[i + 2] += n;
      if (d[i + 3] < 255) d[i + 3] = Math.max(1, d[i + 3] + (r() - 0.5) * 2.2);
    }
    c.putImageData(img, 0, 0);
  }
  root.PaintNoise = { hash, noise2, fbm, rng, seedOf, dither };
})(window);
