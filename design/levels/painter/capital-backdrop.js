// Тло кімнат Столиці для painter (переробка зон; погоджений макет — design/levels/capital-look, 2026-10-19).
// Два шари, як у Гробниці, Лісі й Селі:
//  mid  — далеке: попелясто-червоне небо зі згорілими шпилями палацу й димом; у храмі й криптах — нава
//         в темряві; у цистерні — склепіння над водою в синьому тумані; у башті — стовбур із вікнами;
//  near — ближнє: зруйновані фасади з арками, багряні стяги, жаровні (єдине тепле світло), попіл;
//         у цистерні — водоспад, темна вода й синя печать; у тронній залі — стяги й жаровні обабіч.
// Кожна кімната — зі свого seed: жоден шпиль, стяг чи фасад не повторюється.
(function (root) {
  "use strict";
  const N = root.PaintNoise;
  const B = root.LevelBackdrop;

  function kind(room, look) {
    if (room.flags.some(f => f.startsWith("boss="))) return "throne";
    if (room.flags.indexOf("secret") >= 0) return "secret";
    if (room.id === look.cistern) return "cistern";
    if (room.id === look.tower) return "tower";
    if (look.open.indexOf(room.id) >= 0) return "open";
    if (look.crypts.indexOf(room.id) >= 0) return "crypt";
    if (room.id === look.workshop) return "workshop";
    return "temple";
  }

  // Шпиль: стрункий конус із м'якими боками; у частини верхівка зламана й рвана.
  function spire(f, x, base, w, h, color, r) {
    const c = f.c, broken = r() < 0.45, top = base + h * (broken ? 0.72 + r() * 0.12 : 1), lean = (r() - 0.5) * 0.6;
    c.fillStyle = color;
    c.beginPath(); c.moveTo(...f.px(x - w / 2, base));
    c.bezierCurveTo(...f.px(x - w / 2, base + h * 0.45), ...f.px(x - w * 0.12, base + h * 0.75), ...f.px(x + lean - (broken ? w * 0.18 : 0), top));
    if (broken) {
      c.lineTo(...f.px(x + lean - w * 0.05, top - 0.5 - r() * 0.4));
      c.lineTo(...f.px(x + lean + w * 0.08, top + 0.2));
      c.lineTo(...f.px(x + lean + w * 0.16, top - 0.35));
    }
    c.bezierCurveTo(...f.px(x + w * 0.12, base + h * 0.75), ...f.px(x + w / 2, base + h * 0.45), ...f.px(x + w / 2, base));
    c.closePath(); c.fill();
  }

  // Силует палацу: хвиля дахів, вежі зі шпилями, у кількох вікнах ще жевріє.
  function palace(f, base, height, color, window, seed) {
    const c = f.c, S = f.S, r = N.rng(seed), x0 = f.X0 - 1, x1 = f.X0 + f.W + 1;
    c.fillStyle = color;
    c.beginPath(); c.moveTo(...f.px(x0, base));
    for (let x = x0; x <= x1; x += 0.5) c.lineTo(...f.px(x, base + height * (0.32 + 0.12 * N.fbm(x * 0.4, 1, seed, 2))));
    c.lineTo(...f.px(x1, base)); c.closePath(); c.fill();
    for (let x = x0 + r() * 2; x < x1; x += 2.2 + r() * 3.4) {
      const tw = 0.9 + r() * 1.1, th = height * (0.45 + r() * 0.35);
      c.fillStyle = color;
      c.fillRect(...f.px(x - tw / 2, base + th), tw * S, th * S);
      spire(f, x, base + th - 0.1, tw * 1.25, height * (0.25 + r() * 0.45), color, r);
      if (r() < 0.3) {
        const wy = base + th * (0.55 + r() * 0.3);
        c.fillStyle = window; c.beginPath(); f.pointedArch(x, tw * 0.28, wy, 0.7); c.fill();
        f.glow(x, wy + 0.3, 1.4, window, 0.28);
      }
    }
  }

  function smoke(f, y0, y1, color, alpha, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    for (let i = 0; i < f.W; i++) {
      const x = f.X0 + r() * f.W, y = y0 + r() * (y1 - y0);
      const [px, py] = f.px(x + N.fbm(x, y, seed, 2) * 2, y), rad = S * (1.5 + r() * 3.5);
      const g = c.createRadialGradient(px, py, 0, px, py, rad);
      g.addColorStop(0, B.rgba(color, alpha * (0.4 + r() * 0.6))); g.addColorStop(1, B.rgba(color, 0));
      c.fillStyle = g; c.beginPath(); c.ellipse(px, py, rad * 1.4, rad, 0, 0, Math.PI * 2); c.fill();
    }
  }

  // Зруйнована стіна з арками: верх обвалений хвилею, крізь проломи видно далеке.
  function ruinedArcade(f, base, top, look, seed) {
    const c = f.c, r = N.rng(seed);
    c.save();
    c.beginPath(); c.moveTo(...f.px(f.X0 - 1, base - 3));
    for (let x = f.X0 - 1; x <= f.X0 + f.W + 1; x += 0.25) {
      c.lineTo(...f.px(x, top + N.fbm(x * 0.35, 3, seed, 3) * 1.4 - Math.max(0, N.fbm(x * 0.12, 7, seed + 1, 2)) * 6));
    }
    c.lineTo(...f.px(f.X0 + f.W + 1, base - 3)); c.closePath(); c.clip();
    f.arcade({ top: top + 3, base, span: 5.2 + r() * 1.6, archH: Math.min(7, (top - base) * 0.75), col: 0.9, offset: r() * 3,
      seed: seed + 2, broken: 0.35, cut: false, wall: look.facade, hole: look.facadeHole, mist: B.rgba(look.emberFog, 0.45),
      column: look.facadeColumn, columnLight: look.facadeColumnLight });
    c.restore();
  }

  function longBanner(f, x, top, len, w, cloth, gold, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    c.fillStyle = "#1c1818"; c.fillRect(...f.px(x - w / 2 - 0.15, top + 0.08), (w + 0.3) * S, 0.16 * S);
    c.fillStyle = cloth;
    c.beginPath(); c.moveTo(...f.px(x - w / 2, top));
    c.bezierCurveTo(...f.px(x - w / 2 - 0.05, top - len * 0.4), ...f.px(x - w / 2 + 0.08, top - len * 0.7), ...f.px(x - w / 2 + 0.05, top - len));
    for (let k = 1; k <= 6; k++) c.lineTo(...f.px(x - w / 2 + w * k / 6, top - len + (k % 2 ? 0.35 + r() * 0.4 : r() * 0.2)));
    c.bezierCurveTo(...f.px(x + w / 2 + 0.08, top - len * 0.7), ...f.px(x + w / 2 - 0.05, top - len * 0.4), ...f.px(x + w / 2, top));
    c.closePath(); c.fill();
    c.strokeStyle = "rgba(0,0,0,0.3)"; c.lineWidth = S * 0.06;
    [-0.25, 0.2].forEach(d => { c.beginPath(); c.moveTo(...f.px(x + d * w, top - 0.2)); c.quadraticCurveTo(...f.px(x + d * w * 1.3, top - len * 0.5), ...f.px(x + d * w, top - len + 0.4)); c.stroke(); });
    c.fillStyle = B.rgba(gold, 0.8);
    c.beginPath(); c.moveTo(...f.px(x - w * 0.22, top - len * 0.32));
    c.lineTo(...f.px(x - w * 0.12, top - len * 0.18)); c.lineTo(...f.px(x, top - len * 0.26)); c.lineTo(...f.px(x + w * 0.12, top - len * 0.18));
    c.lineTo(...f.px(x + w * 0.22, top - len * 0.32)); c.closePath(); c.fill();
  }

  // Жаровня: чаша на ніжці, м'які язики полум'я, іскри.
  function brazier(f, x, ground, h, look, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    f.glow(x, ground + h + 0.5, 4.2, look.ember, 0.4);
    f.glow(x, ground + h + 0.4, 1.4, look.flame, 0.55);
    c.fillStyle = look.iron;
    c.beginPath(); c.moveTo(...f.px(x - 0.5, ground)); c.lineTo(...f.px(x - 0.1, ground + h * 0.6));
    c.lineTo(...f.px(x + 0.1, ground + h * 0.6)); c.lineTo(...f.px(x + 0.5, ground)); c.closePath(); c.fill();
    c.fillRect(...f.px(x - 0.08, ground + h), 0.16 * S, h * 0.4 * S);
    c.beginPath(); c.moveTo(...f.px(x - 0.6, ground + h + 0.05));
    c.quadraticCurveTo(...f.px(x, ground + h - 0.45), ...f.px(x + 0.6, ground + h + 0.05)); c.closePath(); c.fill();
    c.save(); c.globalCompositeOperation = "screen";
    for (let i = 0; i < 6; i++) {
      const fx = x + (r() - 0.5) * 0.7, fh = 0.4 + r() * 0.6;
      const [bx, by] = f.px(fx, ground + h), [tx, ty] = f.px(fx + (r() - 0.5) * 0.2, ground + h + fh);
      const g = c.createLinearGradient(bx, by, tx, ty);
      g.addColorStop(0, B.rgba(look.flame, 0.9)); g.addColorStop(1, B.rgba(look.ember, 0));
      c.fillStyle = g;
      c.beginPath(); c.moveTo(bx - S * 0.14, by);
      c.quadraticCurveTo(bx - S * 0.12, by - (by - ty) * 0.6, tx, ty);
      c.quadraticCurveTo(bx + S * 0.12, by - (by - ty) * 0.6, bx + S * 0.14, by); c.fill();
    }
    c.restore();
    c.fillStyle = B.rgba(look.flame, 0.8);
    for (let i = 0; i < 8; i++) {
      c.beginPath(); c.arc(...f.px(x + (r() - 0.5) * 1.4, ground + h + 0.8 + r() * 2.6), S * (0.015 + r() * 0.025), 0, Math.PI * 2); c.fill();
    }
  }

  function ash(f, count, color, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    for (let i = 0; i < count; i++) {
      const [px, py] = f.px(f.X0 + r() * f.W, f.Y0 + r() * f.H);
      c.fillStyle = B.rgba(color, 0.2 + r() * 0.5);
      c.beginPath(); c.ellipse(px, py, S * (0.03 + r() * 0.04), S * (0.015 + r() * 0.02), r() * 3, 0, Math.PI * 2); c.fill();
    }
  }

  function waterfall(f, x, top, bottom, w, look, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    const [l, t] = f.px(x - w / 2, top), [, b] = f.px(x, bottom);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, B.rgba(look.water, 0.55)); g.addColorStop(1, B.rgba(look.water, 0.85));
    c.save(); c.filter = `blur(${S * 0.08}px)`;
    c.fillStyle = g;
    c.beginPath(); c.moveTo(l, t);
    c.bezierCurveTo(l - S * 0.1, t + (b - t) * 0.4, l - S * 0.35, b - (b - t) * 0.2, l - S * 0.45, b);
    c.lineTo(l + w * S + S * 0.45, b);
    c.bezierCurveTo(l + w * S + S * 0.35, b - (b - t) * 0.2, l + w * S + S * 0.1, t + (b - t) * 0.4, l + w * S, t);
    c.closePath(); c.fill();
    c.strokeStyle = B.rgba(look.foam, 0.35); c.lineCap = "round";
    for (let i = 0; i < w * 9; i++) {
      const sx = x - w / 2 + r() * w, len = 1.5 + r() * 4, sy = top - r() * Math.max(0, top - bottom - len);
      c.lineWidth = S * (0.03 + r() * 0.05);
      c.beginPath(); c.moveTo(...f.px(sx, sy)); c.lineTo(...f.px(sx + (sx - x) * 0.05, sy - len)); c.stroke();
    }
    c.restore();
    for (let i = 0; i < 18; i++) {
      const [px, py] = f.px(x + (r() - 0.5) * (w + 1.6), bottom + r() * 0.8);
      const rad = S * (0.3 + r() * 0.6), gg = c.createRadialGradient(px, py, 0, px, py, rad);
      gg.addColorStop(0, B.rgba(look.foam, 0.35)); gg.addColorStop(1, B.rgba(look.foam, 0));
      c.fillStyle = gg; c.beginPath(); c.arc(px, py, rad, 0, Math.PI * 2); c.fill();
    }
  }

  // Темна вода від поверхні донизу кімнати, з відблисками.
  function water(f, x0, x1, surface, bottom, look, seed) {
    const c = f.c, S = f.S, r = N.rng(seed);
    const [l, t] = f.px(x0, surface), [rr, b] = f.px(x1, bottom);
    const g = c.createLinearGradient(0, t, 0, b);
    g.addColorStop(0, B.rgba(look.water, 0.8)); g.addColorStop(1, B.rgba(look.deep, 0.95));
    c.fillStyle = g; c.fillRect(l, t, rr - l, b - t);
    c.strokeStyle = B.rgba(look.glint, 0.45); c.lineCap = "round";
    for (let i = 0; i < (x1 - x0) * 2; i++) {
      const x = x0 + r() * (x1 - x0), y = surface - r() * r() * 1.4, len = 0.3 + r() * 1.1;
      c.lineWidth = S * (0.02 + r() * 0.03);
      c.globalAlpha = 1 - (surface - y) / 1.6;
      c.beginPath(); c.moveTo(...f.px(x, y)); c.lineTo(...f.px(x + len, y)); c.stroke();
    }
    c.globalAlpha = 1;
  }

  // Печать: синє коло з рунами, у центрі корона-полум'я, як на стягах.
  function seal(f, x, y, rad, look, seed) {
    const c = f.c, S = f.S, r = N.rng(seed), [px, py] = f.px(x, y), R = rad * S;
    f.glow(x, y, rad * 3.2, look.sealGlow, 0.35);
    c.save(); c.globalCompositeOperation = "screen";
    c.strokeStyle = B.rgba(look.sealLine, 0.9); c.lineWidth = S * 0.07;
    c.beginPath(); c.arc(px, py, R, 0, Math.PI * 2); c.stroke();
    c.lineWidth = S * 0.04; c.beginPath(); c.arc(px, py, R * 0.78, 0, Math.PI * 2); c.stroke();
    for (let i = 0; i < 12; i++) {
      const a = i / 12 * Math.PI * 2, cx = px + Math.cos(a) * R * 0.89, cy = py + Math.sin(a) * R * 0.89;
      c.beginPath(); c.moveTo(cx - S * 0.05, cy - S * 0.07); c.lineTo(cx + (r() - 0.5) * S * 0.1, cy + S * 0.07);
      c.moveTo(cx + S * 0.05, cy - S * 0.05); c.quadraticCurveTo(cx, cy, cx - S * 0.03, cy + S * 0.05); c.stroke();
    }
    c.lineWidth = S * 0.06;
    c.beginPath(); c.moveTo(px - R * 0.35, py + R * 0.25);
    c.quadraticCurveTo(px - R * 0.3, py - R * 0.2, px - R * 0.15, py - R * 0.4);
    c.quadraticCurveTo(px - R * 0.05, py - R * 0.1, px, py - R * 0.5);
    c.quadraticCurveTo(px + R * 0.05, py - R * 0.1, px + R * 0.15, py - R * 0.4);
    c.quadraticCurveTo(px + R * 0.3, py - R * 0.2, px + R * 0.35, py + R * 0.25);
    c.closePath(); c.stroke();
    c.restore();
  }

  // Кам'яний ангел зі зламаним крилом (як у макеті); h — висота разом із постаментом.
  function angel(f, x, ground, h, look) {
    const c = f.c, S = f.S, P = (dx, dy) => f.px(x + dx * h, ground + dy * h);
    c.fillStyle = look.statue;
    c.beginPath(); c.moveTo(...P(-0.32, 0)); c.lineTo(...P(-0.28, 0.08)); c.lineTo(...P(-0.22, 0.1)); c.lineTo(...P(-0.2, 0.26));
    c.lineTo(...P(0.2, 0.26)); c.lineTo(...P(0.22, 0.1)); c.lineTo(...P(0.28, 0.08)); c.lineTo(...P(0.32, 0)); c.closePath(); c.fill();
    c.beginPath(); c.moveTo(...P(-0.05, 0.64));
    c.bezierCurveTo(...P(-0.2, 0.78), ...P(-0.34, 0.96), ...P(-0.3, 1.14));
    c.bezierCurveTo(...P(-0.4, 1.0), ...P(-0.46, 0.8), ...P(-0.44, 0.58));
    [[-0.4, 0.5], [-0.35, 0.42], [-0.29, 0.36], [-0.22, 0.34]].forEach(([fx, fy], i) => {
      c.quadraticCurveTo(...P(fx + 0.02, fy + 0.08), ...P(fx + 0.035, fy + 0.02 + i * 0.01)); c.lineTo(...P(fx + 0.02, fy));
    });
    c.quadraticCurveTo(...P(-0.12, 0.46), ...P(-0.06, 0.52)); c.closePath(); c.fill();
    c.beginPath(); c.moveTo(...P(0.05, 0.64));
    c.bezierCurveTo(...P(0.16, 0.72), ...P(0.24, 0.8), ...P(0.25, 0.88));
    c.lineTo(...P(0.22, 0.82)); c.lineTo(...P(0.24, 0.76)); c.lineTo(...P(0.19, 0.72)); c.lineTo(...P(0.2, 0.64));
    c.quadraticCurveTo(...P(0.12, 0.54), ...P(0.06, 0.52)); c.closePath(); c.fill();
    c.beginPath(); c.moveTo(...P(-0.15, 0.26));
    c.bezierCurveTo(...P(-0.12, 0.45), ...P(-0.1, 0.6), ...P(-0.08, 0.7));
    c.quadraticCurveTo(...P(0, 0.74), ...P(0.08, 0.7));
    c.bezierCurveTo(...P(0.1, 0.6), ...P(0.12, 0.45), ...P(0.15, 0.26)); c.closePath(); c.fill();
    c.beginPath(); c.ellipse(...P(0.015, 0.78), h * S * 0.045, h * S * 0.055, 0.25, 0, Math.PI * 2); c.fill();
    c.strokeStyle = B.rgba(look.rim, 0.5); c.lineWidth = S * 0.06; c.lineCap = "round";
    c.beginPath(); c.moveTo(...P(-0.08, 0.7)); c.bezierCurveTo(...P(-0.1, 0.6), ...P(-0.12, 0.45), ...P(-0.15, 0.27)); c.stroke();
    c.beginPath(); c.moveTo(...P(-0.3, 1.14)); c.bezierCurveTo(...P(-0.4, 1.0), ...P(-0.46, 0.8), ...P(-0.44, 0.58)); c.stroke();
  }

  // Стяги вздовж стіни: кожен свого кольору з палітри, довжини й рваного низу.
  function bannerRow(f, rect, top, look, seed, chance) {
    const r = N.rng(seed);
    for (let x = rect.x + 2 + r() * 3; x < rect.x + rect.w - 2; x += 5 + r() * 5) {
      if (r() > chance) continue;
      const [cloth, gold] = look.banners[Math.floor(r() * look.banners.length)];
      longBanner(f, x, top, 2.6 + r() * 2.6, 1.0 + r() * 0.4, cloth, gold, seed + Math.round(x * 7));
    }
  }

  // ---------- шари ----------

  function paintMid(f, zone, room, look, rect, seed) {
    const k = kind(room, look), r = N.rng(seed);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "open") {
      f.sky(look.skyTop, look.skyBottom);
      f.glow(rect.x + rect.w * (0.2 + r() * 0.6), floor, Math.max(18, rect.w * 0.6), look.ember, 0.28);
      palace(f, floor + 1, Math.min(13, rect.h * 0.9), look.palace, look.ember, seed + 1);
      smoke(f, floor + 6, top + 2, look.smoke, 0.45, seed + 2);
      f.fog(floor - 1, floor + 8, look.fog, 0.38, seed + 3);
      return;
    }
    if (k === "throne") {
      f.sky(look.innerTop, look.innerBottom);
      f.farNave({ offset: r() * 6, span: 11, base: floor - 1, h: Math.min(rect.h + 4, 24), rib: look.vault, window: look.throneWindow });
      f.fog(floor - 1, floor + 10, look.emberFog, 0.35, seed + 4);
      return;
    }
    if (k === "cistern") {
      f.sky(look.cisternTop, look.cisternBottom);
      f.farNave({ offset: r() * 4, span: 6 + r() * 1.5, base: floor - 2, h: Math.min(rect.h + 2, 16), rib: look.cisternRib, window: look.cisternBottom });
      f.fog(floor - 1, floor + 7, look.cisternFog, 0.4, seed + 5);
      return;
    }
    if (k === "tower") {
      f.sky(look.innerTop, look.innerBottom);
      // стовбур дзвіниці: пілястри, вузькі вікна з червоним небом, ланцюг дзвона
      for (let x = f.X0 + r() * 2; x < f.X0 + f.W; x += 2.6 + r() * 1.4) {
        const [px, py] = f.px(x, top + 3), [, pb] = f.px(0, rect.y - 3);
        f.c.fillStyle = look.vault; f.c.fillRect(px, py, f.S * 0.8, pb - py);
      }
      for (let y = rect.y + 3 + r() * 3; y < top - 2; y += 6 + r() * 4) {
        const wx = f.X0 + f.W * (0.25 + r() * 0.5);
        f.c.beginPath(); f.pointedArch(wx, 0.8, y, 2.6); f.c.fillStyle = look.skyBottom; f.c.fill();
        f.glow(wx, y + 1.2, 2, look.ember, 0.18);
      }
      f.chain(f.X0 + f.W * (0.4 + r() * 0.2), rect.y, top + 3, look.chain);
      f.fog(rect.y, top, look.emberFog, 0.2, seed + 6);
      return;
    }
    // храм, крипти, майстерня, тайники — кам'яні нави в темряві
    f.sky(look.innerTop, look.innerBottom);
    f.farNave({ offset: r() * 4, span: 5 + r() * 2.5, base: floor - 1, h: Math.min(rect.h + 2, k === "temple" ? 14 : 9), rib: look.vault, window: look.innerBottom });
    f.fog(floor - 1, top, look.emberFog, k === "crypt" ? 0.18 : 0.26, seed + 7);
  }

  function paintNear(f, zone, room, look, rect, seed) {
    const k = kind(room, look), r = N.rng(seed + 100);
    const floor = B.floorOf(zone, room), top = rect.y + rect.h;
    if (k === "open") {
      ruinedArcade(f, floor - 0.2, floor + Math.min(9, (top - floor) * 0.7), look, seed + 10);
      bannerRow(f, rect, floor + Math.min(8, (top - floor) * 0.6), look, seed + 11, 0.55);
      if (room.id === look.angelRoom) angel(f, rect.x + rect.w * 0.5, floor, 7.2, look);
      for (let x = rect.x + 3 + r() * 4; x < rect.x + rect.w - 2; x += 9 + r() * 8) brazier(f, x, floor, 1.1 + r() * 0.2, look, seed + Math.round(x));
      ash(f, rect.w * rect.h / 4, look.ash, seed + 12);
      f.dust(rect.w * rect.h / 14, look.flame, seed + 13);
      return;
    }
    if (k === "throne") {
      bannerRow(f, rect, top - 2, look, seed + 14, 0.9);
      [rect.x + 4, rect.x + rect.w - 4].forEach((x, i) => brazier(f, x, floor, 1.4, look, seed + 15 + i));
      f.beam(rect.x + rect.w * 0.5, top, 0, 3, top - floor + 2, look.beam, 0.14, seed + 16);
      ash(f, rect.w * rect.h / 6, look.ash, seed + 17);
      return;
    }
    if (k === "cistern") {
      const fx = rect.x + rect.w * (0.3 + r() * 0.15);
      f.beam(fx, top + 1, 0.05, 1.8, top - floor + 2, look.coldBeam, 0.14, seed + 18);
      waterfall(f, fx, top, floor + 0.2, 1.6, look, seed + 19);
      seal(f, rect.x + rect.w * (0.65 + r() * 0.15), floor + Math.min(8, (top - floor) * 0.55), 1.5, look, seed + 20);
      water(f, rect.x, rect.x + rect.w, floor + 0.3, rect.y, look, seed + 21);
      f.dust(rect.w * rect.h / 7, look.foam, seed + 22);
      return;
    }
    if (k === "tower") {
      for (let y = rect.y + 6 + r() * 4; y < top - 4; y += 14 + r() * 8) brazier(f, rect.x + rect.w * (0.2 + r() * 0.6), y, 0.9, look, seed + Math.round(y));
      ash(f, rect.w * rect.h / 8, look.ash, seed + 23);
      return;
    }
    if (k === "temple") {
      bannerRow(f, rect, top - 1.5, look, seed + 24, 0.7);
      [rect.x + rect.w * 0.3, rect.x + rect.w * 0.7].forEach((x, i) => brazier(f, x, floor, 1.2, look, seed + 25 + i));
      f.dust(rect.w * rect.h / 7, look.flame, seed + 26);
      return;
    }
    if (k === "workshop") {
      // кузня: горн із жаром біля стіни й мокрий від жару камінь
      const hx = rect.x + rect.w * (0.6 + r() * 0.2);
      f.glow(hx, floor + 1, 6, look.ember, 0.4);
      f.c.fillStyle = look.iron; f.c.beginPath(); f.pointedArch(hx, 2.4, floor, 2.6); f.c.fill();
      f.glow(hx, floor + 0.8, 1.6, look.flame, 0.6);
      bannerRow(f, rect, top - 1.5, look, seed + 27, 0.4);
      f.dust(rect.w * rect.h / 6, look.flame, seed + 28);
      return;
    }
    if (k === "secret") {
      f.glow(rect.x + rect.w / 2, floor + 1, 3.5, look.secretGlow, 0.2);
      f.dust(rect.w * rect.h / 6, look.flame, seed + 29);
      return;
    }
    // крипти: одна жаровня й попіл
    brazier(f, rect.x + rect.w * (0.3 + r() * 0.4), floor, 0.9, look, seed + 30);
    ash(f, rect.w * rect.h / 10, look.ash, seed + 31);
  }

  root.LevelBackdropCapital = { paintMid, paintNear, kind };
})(window);
