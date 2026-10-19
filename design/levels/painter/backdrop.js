// Тло кімнат для painter рівнів: два шари на кімнату.
//  mid  — далеке: нава в тумані, аркада бічного нефу (у грі зсувається паралаксом, тож більший за
//         кімнату на MARGIN з кожного боку);
//  near — стіна самої кімнати: великі арки з прозорими прорізами (крізь них видно mid), статуї,
//         прапори, промені й пил; у печерах — брили, сталактити, гриби.
// Кожна кімната має свій seed — жодна картинка не повторюється.
(function (root) {
  "use strict";
  const N = root.PaintNoise;
  const MARGIN = 3;

  function hexRgb(h) { return [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)]; }
  function rgba(h, a) { const [r, g, b] = hexRgb(h); return `rgba(${r},${g},${b},${a})`; }
  function shade(h, d) {
    const [r, g, b] = hexRgb(h);
    const f = v => Math.max(0, Math.min(255, Math.round(v * (1 + d))));
    return `rgb(${f(r)},${f(g)},${f(b)})`;
  }

  // Полотно у світових координатах: X0, Y0 — лівий нижній кут, S — пікселів на юніт.
  function Frame(canvas, X0, Y0, S) {
    this.c = canvas.getContext("2d");
    this.S = S; this.X0 = X0; this.Y0 = Y0;
    this.W = canvas.width / S; this.H = canvas.height / S;
    this.px = (x, y) => [(x - X0) * S, canvas.height - (y - Y0) * S];
  }

  Frame.prototype.sky = function (top, bottom) {
    const c = this.c, g = c.createLinearGradient(0, 0, 0, c.canvas.height);
    g.addColorStop(0, top); g.addColorStop(1, bottom);
    c.fillStyle = g; c.fillRect(0, 0, c.canvas.width, c.canvas.height);
  };

  Frame.prototype.pointedArch = function (x, w, y0, h) {
    const c = this.c;
    const [lx, ly] = this.px(x - w / 2, y0), [rx] = this.px(x + w / 2, y0);
    const [, sy] = this.px(x, y0 + Math.max(0, h - w * 0.75)), [tx, ty] = this.px(x, y0 + h);
    c.moveTo(lx, ly); c.lineTo(lx, sy);
    c.quadraticCurveTo(lx, ty + (sy - ty) * 0.15, tx, ty);
    c.quadraticCurveTo(rx, ty + (sy - ty) * 0.15, rx, sy);
    c.lineTo(rx, ly);
  };

  // Аркада: стіна від base до top, стрілчасті прорізи, пучкові колони з капітеллю.
  // cut — прорізи прозорі (крізь них видно далекий шар), інакше залиті темрявою.
  Frame.prototype.arcade = function (o) {
    const c = this.c, S = this.S, r = N.rng(o.seed);
    const x0 = this.X0, x1 = this.X0 + this.W;
    c.save();
    const [, ty] = this.px(0, o.top), [, by] = this.px(0, o.base);
    c.fillStyle = o.wall;
    c.fillRect(0, ty, c.canvas.width, by - ty);
    // ледь видна кладка стіни
    c.globalAlpha = 0.18;
    for (let y = o.base; y < o.top; y += 0.9) {
      const off = r() * 2;
      for (let x = x0 - off; x < x1; x += 1.4 + r() * 1.2) {
        const [px, py] = this.px(x, y + 0.9);
        c.strokeStyle = "#000"; c.lineWidth = Math.max(1, S * 0.03);
        c.strokeRect(px, py, S * 1.4, S * 0.9);
      }
    }
    c.globalAlpha = 1;
    const arches = [];
    for (let x = x0 + o.offset; x < x1 + o.span; x += o.span) {
      const broken = r() < (o.broken || 0);
      arches.push({ x, broken, h: o.archH * (broken ? 0.75 + r() * 0.15 : 1) });
    }
    arches.forEach(a => {
      c.beginPath();
      this.pointedArch(a.x, o.span * 0.62, o.base, a.h);
      if (o.cut) {
        c.globalCompositeOperation = "destination-out";
        c.fillStyle = "#000"; c.fill();
        c.globalCompositeOperation = "source-over";
        // по краю прорізу — тонка тінь укосу
        c.strokeStyle = rgba("#000000", 0.5); c.lineWidth = S * 0.12; c.stroke();
      } else {
        c.fillStyle = o.hole; c.fill();
        const [, top] = this.px(0, o.base + a.h), [, bot] = this.px(0, o.base);
        const g = c.createLinearGradient(0, top, 0, bot);
        g.addColorStop(0, "rgba(0,0,0,0)"); g.addColorStop(1, o.mist);
        c.fillStyle = g; c.fill();
      }
    });
    arches.forEach(a => {
      const cx = a.x + o.span / 2;
      const [px0, py0] = this.px(cx - o.col / 2, o.top), [, pyb] = this.px(0, o.base);
      c.fillStyle = o.column; c.fillRect(px0, py0, o.col * S, pyb - py0);
      c.fillStyle = o.columnLight;
      c.fillRect(px0 + o.col * S * 0.18, py0, o.col * S * 0.12, pyb - py0);
      c.fillRect(px0 + o.col * S * 0.62, py0, o.col * S * 0.06, pyb - py0);
      const [, cap] = this.px(0, o.base + o.archH * 0.62);
      c.fillRect(px0 - S * 0.14, cap, o.col * S + S * 0.28, S * 0.24);
      c.fillRect(px0 - S * 0.2, pyb - S * 0.35, o.col * S + S * 0.4, S * 0.35);
    });
    c.restore();
    return arches;
  };

  Frame.prototype.farNave = function (o) {
    const c = this.c, S = this.S;
    c.save();
    for (let x = this.X0 + o.offset; x < this.X0 + this.W + o.span; x += o.span) {
      c.beginPath(); this.pointedArch(x, o.span * 0.8, o.base, o.h);
      c.lineWidth = S * 0.7; c.strokeStyle = o.rib; c.stroke();
      c.beginPath(); this.pointedArch(x, o.span * 0.18, o.base + o.h * 0.35, o.h * 0.42);
      c.fillStyle = o.window; c.fill();
    }
    c.restore();
  };

  Frame.prototype.fog = function (y0, y1, color, alpha, seed) {
    const c = this.c, [, a] = this.px(0, y1), [, b] = this.px(0, y0);
    const g = c.createLinearGradient(0, a, 0, b);
    g.addColorStop(0, rgba(color, 0)); g.addColorStop(0.6, rgba(color, alpha)); g.addColorStop(1, rgba(color, alpha * 0.6));
    c.fillStyle = g; c.fillRect(0, a, c.canvas.width, b - a);
    const r = N.rng(seed || 1);
    for (let i = 0; i < Math.max(4, this.W / 3); i++) {
      const x = this.X0 + r() * this.W, y = y0 + r() * (y1 - y0) * 0.6;
      const [px, py] = this.px(x, y), rad = this.S * (2 + r() * 4);
      const gg = c.createRadialGradient(px, py, 0, px, py, rad);
      gg.addColorStop(0, rgba(color, alpha * 0.7)); gg.addColorStop(1, rgba(color, 0));
      c.fillStyle = gg; c.beginPath(); c.ellipse(px, py, rad * 1.8, rad * 0.45, 0, 0, Math.PI * 2); c.fill();
    }
  };

  Frame.prototype.beam = function (x, y, angle, width, length, color, alpha, seed) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.save();
    c.translate(px, py); c.rotate(angle);
    const g = c.createLinearGradient(0, 0, 0, length * S);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(0.7, rgba(color, alpha * 0.35)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g; c.filter = `blur(${S * 0.35}px)`;
    c.beginPath();
    c.moveTo(-width * S * 0.35, 0); c.lineTo(width * S * 0.35, 0);
    c.lineTo(width * S * 0.9, length * S); c.lineTo(-width * S * 0.9, length * S);
    c.closePath(); c.fill(); c.filter = "none";
    const r = N.rng(seed || 3);
    for (let i = 0; i < length * 3; i++) {
      const t = r(), off = (r() - 0.5) * width * (0.5 + t * 0.9);
      c.fillStyle = rgba(color, 0.25 + r() * 0.5);
      c.beginPath(); c.arc(off * S, t * length * S, S * (0.015 + r() * 0.03), 0, Math.PI * 2); c.fill();
    }
    c.restore();
  };

  Frame.prototype.glow = function (x, y, radius, color, alpha) {
    const c = this.c, [px, py] = this.px(x, y), rad = radius * this.S;
    const g = c.createRadialGradient(px, py, 0, px, py, rad);
    g.addColorStop(0, rgba(color, alpha)); g.addColorStop(0.35, rgba(color, alpha * 0.4)); g.addColorStop(1, rgba(color, 0));
    c.fillStyle = g; c.fillRect(px - rad, py - rad, rad * 2, rad * 2);
  };

  Frame.prototype.statue = function (x, y, h, color, light, pose) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    c.fillStyle = color;
    c.fillRect(px - S * 0.65, py - S * 0.7, S * 1.3, S * 0.7);
    c.beginPath();
    c.moveTo(px - S * 0.48, py - S * 0.7);
    c.lineTo(px - S * 0.38, py - S * (h - 0.9));
    c.quadraticCurveTo(px, py - S * (h + 0.15), px + S * 0.38, py - S * (h - 0.9));
    c.lineTo(px + S * 0.48, py - S * 0.7); c.closePath(); c.fill();
    c.fillStyle = light;
    if (pose === 0) {
      // меч остриєм униз, руки на руків'ї
      c.fillRect(px - S * 0.03, py - S * (h - 0.6), S * 0.06, S * (h - 1.4));
      c.fillRect(px - S * 0.22, py - S * (h - 1.2), S * 0.44, S * 0.06);
    } else {
      // схилена голова, складені руки
      c.beginPath(); c.ellipse(px, py - S * (h - 1.6), S * 0.2, S * 0.12, 0, 0, Math.PI * 2); c.fill();
      c.fillRect(px - S * 0.03, py - S * (h - 1.0), S * 0.06, S * 0.4);
    }
  };

  Frame.prototype.banner = function (x, top, len, cloth, gold) {
    const c = this.c, S = this.S, [px, py] = this.px(x, top);
    c.fillStyle = "#2a2520"; c.fillRect(px - S * 0.55, py - S * 0.06, S * 1.1, S * 0.12);
    c.fillStyle = cloth;
    c.beginPath(); c.moveTo(px - S * 0.45, py); c.lineTo(px + S * 0.45, py); c.lineTo(px + S * 0.42, py + len * S);
    c.lineTo(px, py + (len - 0.45) * S); c.lineTo(px - S * 0.4, py + len * S); c.closePath(); c.fill();
    c.fillStyle = gold;
    c.beginPath(); c.moveTo(px, py + S * 0.7); c.lineTo(px - S * 0.2, py + S * 1.15); c.lineTo(px + S * 0.2, py + S * 1.15); c.closePath(); c.fill();
    c.fillStyle = "rgba(0,0,0,0.35)"; c.fillRect(px + S * 0.2, py + S * 0.2, S * 0.25, len * S * 0.8);
  };

  Frame.prototype.chain = function (x, y0, y1, color) {
    const c = this.c, S = this.S;
    c.strokeStyle = color; c.lineWidth = Math.max(1, S * 0.05);
    for (let y = y0; y < y1; y += 0.22) {
      const [px, py] = this.px(x, y);
      c.beginPath(); c.ellipse(px, py, S * 0.06, S * 0.12, 0, 0, Math.PI * 2); c.stroke();
    }
  };

  Frame.prototype.dust = function (count, color, seed) {
    const c = this.c, r = N.rng(seed);
    for (let i = 0; i < count; i++) {
      const [px, py] = this.px(this.X0 + r() * this.W, this.Y0 + r() * this.H);
      c.fillStyle = rgba(color, 0.12 + r() * 0.4);
      c.beginPath(); c.arc(px, py, this.S * (0.012 + r() * 0.035), 0, Math.PI * 2); c.fill();
    }
  };

  // Брили печери: силует, піднятий знизу профілем шуму (або звислий згори — top=true).
  Frame.prototype.rocks = function (base, height, color, seed, top) {
    const c = this.c;
    c.fillStyle = color;
    c.beginPath();
    const yEdge = top ? this.Y0 + this.H + 1 : this.Y0 - 1;
    c.moveTo(...this.px(this.X0 - 1, yEdge));
    for (let x = this.X0 - 1; x <= this.X0 + this.W + 1; x += 0.2) {
      const n = N.fbm(x * 0.22, 0.5, seed, 4), spike = Math.pow(N.fbm(x * 0.9, 3.5, seed + 5, 2), 3) * 1.6;
      const h = height * (0.35 + n * 0.9) + (top ? spike * height * 0.5 : 0);
      c.lineTo(...this.px(x, top ? base - h : base + h));
    }
    c.lineTo(...this.px(this.X0 + this.W + 1, yEdge));
    c.closePath(); c.fill();
  };

  Frame.prototype.fungus = function (x, y, color) {
    const c = this.c, S = this.S, [px, py] = this.px(x, y);
    this.glow(x, y + 0.15, 0.7, color, 0.45);
    c.fillStyle = color;
    c.beginPath(); c.ellipse(px, py - S * 0.12, S * 0.12, S * 0.07, 0, Math.PI, 0); c.fill();
    c.fillRect(px - S * 0.015, py - S * 0.12, S * 0.03, S * 0.12);
  };

  Frame.prototype.skulls = function (x0, x1, y, seed) {
    const c = this.c, S = this.S, r = N.rng(seed);
    for (let x = x0; x < x1; x += 0.3 + r() * 0.25) {
      for (let k = 0; k < 3; k++) {
        const [px, py] = this.px(x + (r() - 0.5) * 0.2, y + k * 0.28 + r() * 0.08);
        c.fillStyle = shade("#6f6a5e", (r() - 0.5) * 0.4 - k * 0.12);
        c.beginPath(); c.ellipse(px, py, S * 0.13, S * 0.11, 0, 0, Math.PI * 2); c.fill();
        c.fillStyle = "#14110f";
        c.fillRect(px - S * 0.07, py - S * 0.02, S * 0.04, S * 0.04);
        c.fillRect(px + S * 0.03, py - S * 0.02, S * 0.04, S * 0.04);
      }
    }
  };

  // ---------- кімнати ----------

  function roomRect(zone, room) {
    const x = zone.origin[0] + room.x * zone.cell, y = zone.origin[1] + room.y * zone.cell;
    return { x, y, w: room.w * zone.cell, h: room.h * zone.cell };
  }

  function kindOf(room, look) {
    if (room.flags.some(f => f.startsWith("boss="))) return "arena";
    if (room.flags.indexOf("secret") >= 0) return "secret";
    if (look.caves.indexOf(room.id) >= 0) return "cave";
    if (room.h > room.w * 1.3) return "shaft";
    return "hall";
  }

  // Найнижча підлога кімнати (у світі) — щоб тло стояло на ній.
  function floorOf(zone, room) {
    let best = Infinity;
    for (let x = 2; x < room.w - 2; x++) {
      for (let y = 1; y < room.h - 1; y++) {
        const here = room.rows[y][x], below = room.rows[y - 1][x];
        if (root.LevelText.isAir(here) && !root.LevelText.isAir(below)) { best = Math.min(best, y); break; }
      }
    }
    return zone.origin[1] + (room.y + (best === Infinity ? 2 : best)) * zone.cell;
  }

  function paintMid(f, zone, room, look, rect, seed) {
    const kind = kindOf(room, look), r = N.rng(seed);
    const floor = floorOf(zone, room), top = rect.y + rect.h;
    if (kind === "cave") {
      f.sky("#0f171b", "#080b0d");
      f.fog(rect.y, top, look.caveFog, 0.35, seed);
      f.farNave({ offset: r() * 6, span: 9 + r() * 3, base: floor - 2, h: Math.min(rect.h, 16), rib: "#141d21", window: "#1b2e33" });
      f.rocks(floor - 1, 2.5 + r() * 2, "#11181b", seed + 1, false);
      f.rocks(top + 1, 2 + r() * 2, "#0e1416", seed + 2, true);
      f.fog(rect.y - 2, floor + 4, look.caveFog, 0.3, seed + 3);
      return;
    }
    if (kind === "shaft") {
      f.sky("#111721", "#090c11");
      // далекі стіни шахти: пілястри, вузькі вікна
      for (let x = f.X0 + r() * 2; x < f.X0 + f.W; x += 2.6 + r() * 1.4) {
        const [px, py] = f.px(x, top + 2), [, pb] = f.px(0, rect.y - 2);
        f.c.fillStyle = "#171d27"; f.c.fillRect(px, py, f.S * 0.8, pb - py);
        f.c.fillStyle = "#1e2531"; f.c.fillRect(px + f.S * 0.12, py, f.S * 0.1, pb - py);
      }
      for (let y = rect.y + 2 + r() * 3; y < top; y += 5 + r() * 4) {
        f.c.beginPath(); f.pointedArch(f.X0 + f.W * (0.3 + r() * 0.4), 0.7, y, 2.4); f.c.fillStyle = look.window; f.c.fill();
      }
      f.chain(f.X0 + f.W * (0.25 + r() * 0.2), rect.y, top + 3, "#161b23");
      f.fog(rect.y, top, look.fog, 0.22, seed);
      return;
    }
    f.sky(look.skyTop, look.skyBottom);
    const tall = kind === "arena";
    f.farNave({ offset: r() * 8, span: tall ? 12 : 8 + r() * 3, base: floor - 1, h: Math.max(10, Math.min(rect.h + 4, tall ? 26 : 19)),
      rib: look.ribs, window: look.window });
    if (tall) {
      const cx = rect.x + rect.w / 2, cy = top - 4.5;
      f.glow(cx, cy, 6, look.arenaGlow, 0.18);
      f.c.strokeStyle = rgba(look.arenaGlow, 0.25); f.c.lineWidth = f.S * 0.18;
      f.c.beginPath(); f.c.arc(...f.px(cx, cy), f.S * 2.4, 0, Math.PI * 2); f.c.stroke();
      for (let k = 0; k < 8; k++) {
        const a = k * Math.PI / 4;
        f.c.beginPath(); f.c.moveTo(...f.px(cx, cy)); f.c.lineTo(...f.px(cx + Math.cos(a) * 2.4, cy + Math.sin(a) * 2.4)); f.c.stroke();
      }
    }
    f.fog(floor, floor + rect.h * 0.7, look.fog, 0.45, seed);
    if (kind !== "secret") {
      f.arcade({ top: Math.min(top + 2, floor + 13), base: floor - 0.2, span: 4.2 + r() * 1.6, archH: Math.min(9, Math.max(4.5, rect.h * 0.55)),
        col: 0.7, offset: r() * 3, seed: seed + 4, broken: 0.25 + r() * 0.2, cut: false,
        wall: look.midWall, hole: look.midHole, mist: rgba(look.fog, 0.55), column: look.midColumn, columnLight: look.midColumnLight });
    }
    f.fog(floor - 1, floor + 4, look.fog, 0.3, seed + 5);
  }

  function paintNear(f, zone, room, look, rect, seed) {
    const kind = kindOf(room, look), r = N.rng(seed + 100);
    const floor = floorOf(zone, room), top = rect.y + rect.h;
    const c = f.c;
    if (kind === "cave") {
      f.rocks(top, 1.2 + r() * 1.5, look.caveRock, seed + 11, true);
      // брили біля підлоги, ледь світліші — ближчий план
      f.rocks(floor - 0.3, 0.8 + r() * 0.8, look.caveRockLight, seed + 12, false);
      for (let i = 0; i < Math.max(2, rect.w / 6); i++) f.fungus(rect.x + 1.5 + r() * (rect.w - 3), floor + r() * 0.3, look.fungus);
      // корені Лісу, що пройшли крізь склепіння
      c.strokeStyle = "#0d1112"; c.lineCap = "round";
      for (let i = 0; i < rect.w / 5; i++) {
        let x = rect.x + r() * rect.w, y = top - 0.5;
        c.lineWidth = f.S * (0.05 + r() * 0.1);
        c.beginPath(); c.moveTo(...f.px(x, y));
        const len = 1.5 + r() * 4;
        for (let s = 0; s < len; s += 0.35) { x += (r() - 0.5) * 0.45; y -= 0.35; c.lineTo(...f.px(x, y)); }
        c.stroke();
      }
      if (rect.h > 9) f.beam(rect.x + rect.w * (0.3 + r() * 0.4), top - 1, (r() - 0.5) * 0.3, 1.2, rect.h * 0.9, "#9fd6dd", 0.16, seed);
      f.dust(rect.w * rect.h / 6, "#bfe6ea", seed + 13);
      return;
    }
    if (kind === "secret") {
      c.fillStyle = "#16181d"; c.fillRect(0, 0, c.canvas.width, c.canvas.height);
      for (let x = rect.x + 1.6; x < rect.x + rect.w - 1.6; x += 2.2) {
        c.beginPath(); f.pointedArch(x, 1.4, floor + 0.4, 2.2); c.fillStyle = "#0d0f12"; c.fill();
        f.skulls(x - 0.5, x + 0.5, floor + 0.55, seed + Math.round(x * 10));
      }
      f.glow(rect.x + rect.w / 2, floor + 1, 3.5, look.secretGlow, 0.18);
      f.dust(rect.w * rect.h / 5, "#e6d6a8", seed + 14);
      return;
    }
    if (kind === "shaft") {
      // ближні пілястри по боках і світло згори — середина прозора
      [rect.x + 1.0, rect.x + rect.w - 1.0].forEach((x, k) => {
        const [px, py] = f.px(x - 0.5, top), [, pb] = f.px(0, rect.y);
        c.fillStyle = look.nearColumn; c.fillRect(px, py, f.S, pb - py);
        c.fillStyle = look.nearColumnLight; c.fillRect(px + f.S * (k ? 0.7 : 0.15), py, f.S * 0.12, pb - py);
      });
      f.chain(rect.x + rect.w * (0.35 + r() * 0.3), rect.y + rect.h * (0.3 + r() * 0.3), top, "#252a33");
      f.beam(rect.x + rect.w * (0.4 + r() * 0.2), top, (r() - 0.5) * 0.12, Math.min(2.2, rect.w * 0.3), rect.h, look.beam, 0.2, seed);
      f.dust(rect.w * rect.h / 5, look.dust, seed + 15);
      return;
    }
    // зала й арена: стіна з великими арками, прорізи — у далечінь
    const arena = kind === "arena";
    const span = arena ? 9 : 6.5 + r() * 2.5;
    const archH = arena ? Math.min(rect.h - 3, 16) : Math.min(12, Math.max(4.8, (top - floor) * 0.72));
    const arches = f.arcade({ top: top + 1, base: floor - 0.4, span, archH, col: arena ? 1.8 : 1.3, offset: span * (0.3 + r() * 0.5),
      seed: seed + 16, broken: 0, cut: true,
      wall: look.nearWall, hole: look.nearHole, mist: rgba(look.fog, 0.35), column: look.nearColumn, columnLight: look.nearColumnLight });
    arches.forEach((a, i) => {
      const cx = a.x;
      if (cx < rect.x + 1.5 || cx > rect.x + rect.w - 1.5) return;
      const roll = r();
      if (arena ? i % 2 === 1 : roll < 0.3) f.statue(cx, floor - 0.4, arena ? 6 : 4.4 + r() * 0.6, look.statue, look.statueLight, i % 2);
      const colX = cx + span / 2;
      if (colX < rect.x + rect.w - 1 && r() < (arena ? 0.9 : 0.4)) {
        const [cloth, gold] = look.banners[Math.floor(r() * look.banners.length)];
        f.banner(colX, floor + archH * 0.62 - 0.2, Math.min(4.6, archH * 0.45), cloth, gold);
      }
    });
    const beams = arena ? 3 : rect.w > 22 ? 2 : 1;
    for (let i = 0; i < beams; i++) {
      f.beam(rect.x + rect.w * ((i + 0.3 + r() * 0.4) / beams), top, (r() - 0.5) * 0.4, 1.4 + r() * 1.2, (top - floor) + 2,
        look.beam, 0.16 + r() * 0.1, seed + i);
    }
    f.dust(rect.w * rect.h / 5, look.dust, seed + 17);
  }

  function paintRoom(canvas, zone, room, layer, ppu, look) {
    const rect = roomRect(zone, room), seed = N.seedOf(room.id);
    const m = layer === "mid" ? MARGIN : 0;
    canvas.width = Math.round((rect.w + 2 * m) * ppu);
    canvas.height = Math.round((rect.h + 2 * m) * ppu);
    const f = new Frame(canvas, rect.x - m, rect.y - m, ppu);
    // Ліс і Село малюють своє тло (design/levels/forest-look, village-look); Гробниця — нави й аркади.
    const style = look.style === "forest" ? root.LevelBackdropForest
      : look.style === "village" ? root.LevelBackdropVillage : { paintMid, paintNear };
    (layer === "mid" ? style.paintMid : style.paintNear)(f, zone, room, look, rect, seed);
  }

  // Уся зона разом: тло кімнат (без паралаксу), потім камінь — для перегляду.
  function paintPreview(canvas, zone, ppu, look, seed, view) {
    const b = zone.bounds, cell = zone.cell;
    const X0 = view ? view.x : zone.origin[0] + b.minX * cell, Y0 = view ? view.y : zone.origin[1] + b.minY * cell;
    const W = view ? view.w : (b.maxX - b.minX) * cell, H = view ? view.h : (b.maxY - b.minY) * cell;
    canvas.width = Math.round(W * ppu); canvas.height = Math.round(H * ppu);
    const c = canvas.getContext("2d");
    c.fillStyle = "#0a0b0e"; c.fillRect(0, 0, canvas.width, canvas.height);
    for (const room of zone.rooms) {
      const rect = roomRect(zone, room);
      if (rect.x > X0 + W || rect.x + rect.w < X0 || rect.y > Y0 + H || rect.y + rect.h < Y0) continue;
      ["mid", "near"].forEach(layer => {
        const tmp = document.createElement("canvas");
        paintRoom(tmp, zone, room, layer, ppu, look);
        const m = layer === "mid" ? MARGIN : 0;
        c.save();
        c.beginPath();
        c.rect((rect.x - X0) * ppu, canvas.height - (rect.y + rect.h - Y0) * ppu, rect.w * ppu, rect.h * ppu);
        c.clip();
        c.drawImage(tmp, (rect.x - m - X0) * ppu, canvas.height - (rect.y + rect.h + m - Y0) * ppu);
        c.restore();
      });
    }
    const terrain = document.createElement("canvas");
    terrain.width = canvas.width; terrain.height = canvas.height;
    root.LevelTerrain.paintChunk(terrain, zone, X0, Y0, W, H, ppu, look, seed);
    c.drawImage(terrain, 0, 0);
  }

  root.LevelBackdrop = { paintRoom, paintPreview, roomRect, kindOf, floorOf, Frame, rgba, shade, MARGIN };
})(window);
