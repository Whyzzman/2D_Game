#!/usr/bin/env python3
"""A walk through the whole game on paper, from the data the game really uses (BALANCE_WORLD.md).

  .venv/bin/python tools/balance/report.py          # prints the table; exit 1 when a rule is broken

Reads the zones' texts (who stands where), the enemies' definitions and prefabs (experience, health,
blows), the bosses' prefabs and rewards, and the level curve; walks the zones in the order of ORDER and
says what level the hero is at each boss, how many light blows things take and how hard they hit back.
"""
import glob, math, os, re, sys

ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
GAME = os.path.join(ROOT, "Assets", "Game")

# zone text, threat level, boss prefab(s) met there in order
ORDER = [
    ("tomb", 1, ["GuardianOfTheTomb"]),
    ("forest", 2, ["HollowWarden"]),
    ("village", 3, ["Bellringer"]),
    ("capital", 4, ["AshCommander"]),
    ("marsh", 5, ["PlagueStag"]),
    (None, 5, ["OathBoundKing"]),      # back in the capital, behind the Commander's hall
]
# Known and meant: the shield guard is the Tomb's elite (two of them), the Guardian is the wall that
# teaches blocking - its heaviest blows are the ones to block or leave.
ACCEPTED = {"ShieldGuard", "GuardianOfTheTomb"}
REPEAT = 1.3          # part of the fights is fought again after a rest or a death
HERO_BLOW = {1: 19, 2: 21, 3: 23, 4: 26, 5: 28}   # mean light blow by threat level (BALANCE_WORLD §2)
HERO_HEALTH = {1: 100, 2: 126, 3: 169, 4: 190, 5: 215}


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def number(text, field, default=0.0):
    m = re.search(rf"^\s*{field}: ([-\d.]+)", text, re.M)
    return float(m.group(1)) if m else default


def curve():
    text = read(os.path.join(GAME, "Data", "ProgressionCurve.asset"))
    base, exp, rnd = number(text, "_baseExperience"), number(text, "_experienceExponent"), number(text, "_experienceRounding")
    return lambda level: math.ceil(base * max(1, level) ** exp / rnd) * rnd


def definitions():
    found = {}
    for path in glob.glob(os.path.join(GAME, "Data", "**", "*.asset"), recursive=True):
        text = read(path)
        if "_experienceReward" not in text:
            continue
        blows = [float(x) for x in re.findall(r"^\s*_damage: ([\d.]+)", text, re.M)]
        found[os.path.basename(path)[:-6]] = (number(text, "_experienceReward"), blows)
    return found


def health(prefab):
    path = os.path.join(GAME, "Prefabs", prefab + ".prefab")
    return number(read(path), "_maxHealth") if os.path.exists(path) else 0


def boss_blows(prefab):
    text = read(os.path.join(GAME, "Prefabs", prefab + ".prefab"))
    return [float(x) for x in re.findall(r"^\s*_damage: ([\d.]+)", text, re.M) if float(x) > 0]


REWARD_ID = {"GuardianOfTheTomb": "guardian_core", "HollowWarden": "warden_sigil", "Bellringer": "bell_clapper",
             "PlagueStag": "plague_mask", "AshCommander": "commander_banner", "OathBoundKing": "king_seal"}


def rewards():
    """Reward id -> (gold, experience), from the scenes: the numbers the encounters really carry."""
    found = {}
    for path in glob.glob(os.path.join(GAME, "Scenes", "**", "*.unity"), recursive=True):
        for m in re.finditer(r"_rewardId: (\S+)\n\s*_rewardName: .*\n\s*_rewardCurrency: (\d+)\n\s*_rewardExperience: (\d+)",
                             read(path)):
            found[m.group(1)] = (float(m.group(2)), float(m.group(3)))
    return found


def enemies_of(zone):
    """Enemy prefab -> how many stand in the zone's text."""
    counts, keys, in_grid = {}, {}, False
    for line in read(os.path.join(GAME, "Levels", zone + ".txt")).splitlines():
        if line.startswith("room "):
            keys = {}
        elif line.startswith("key "):
            parts = line.split()
            if len(parts) >= 4 and parts[2] == "enemy":
                keys[parts[1]] = parts[3]
        elif line.strip() == "grid":
            in_grid = True
        elif line.strip() == "end":
            in_grid = False
        elif in_grid:
            for ch in line:
                if ch in keys:
                    counts[keys[ch]] = counts.get(keys[ch], 0) + 1
    return counts


def main():
    need, defs, paid = curve(), definitions(), rewards()
    level, xp, problems = 1, 0.0, []

    def gain(amount):
        nonlocal level, xp
        xp += amount
        while xp >= need(level):
            xp -= need(level)
            level += 1

    print(f"{'zone':9}{'threat':>7}{'in':>4}  enemies (count x hp, blows to kill, worst blow as % of hero)")
    for zone, threat, bosses in ORDER:
        entered = level
        blow, hero = HERO_BLOW[threat], HERO_HEALTH[threat]
        zone_xp, lines = 0.0, []
        for name, count in sorted(enemies_of(zone).items()) if zone else []:
            if name not in defs:
                continue
            reward, blows = defs[name]
            hp = health(name)
            zone_xp += reward * count
            worst = max(blows) if blows else 0
            lines.append(f"{name} {count}x{hp:.0f} ({hp / blow:.1f}, {100 * worst / hero:.0f}%)")
            if name in ACCEPTED:
                continue
            if hp and hp / blow > 8.5:
                problems.append(f"{zone}: {name} takes {hp / blow:.1f} light blows")
            if worst / hero > 0.3:
                problems.append(f"{zone}: {name} takes {100 * worst / hero:.0f}% of the hero in one blow")
        if zone:
            print(f"{zone:9}{threat:>7}{entered:>4}  " + "; ".join(lines))
        gain(zone_xp * REPEAT)
        for boss in bosses:
            hp, blows = health(boss), boss_blows(boss)
            gold, exp = paid.get(REWARD_ID[boss], (0, 0))
            hits = hp / blow
            print(f"{'':9}{'':>7}{level:>4}  BOSS {boss}: hp {hp:.0f} = {hits:.0f} blows; hits {min(blows):.0f}-{max(blows):.0f} "
                  f"({100 * max(blows) / hero:.0f}% of hero); pays {exp:.0f} xp, {gold:.0f} gold")
            if max(blows) / hero < 0.18:
                problems.append(f"{boss}: worst blow is only {100 * max(blows) / hero:.0f}% of the hero (want about 20-25)")
            if not 32 <= hits <= 50:
                problems.append(f"{boss}: {hits:.0f} light blows (want 35-45)")
            if max(blows) / hero > 0.3 and boss not in ACCEPTED:
                problems.append(f"{boss}: worst blow is {100 * max(blows) / hero:.0f}% of the hero")
            gain(exp)
    print(f"end: level {level}")
    for p in problems:
        print("!", p)
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
