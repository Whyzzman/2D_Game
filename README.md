# Тінь Забутого Королівства (Shadow of the Forgotten Kingdom)

A 2D action-RPG / metroidvania in a dark fantasy setting: a kingdom that has been dying for three
hundred years, and the Shadow in its cracks. The feel (movement, camera, combat rhythm) takes Hollow
Knight as a reference point; the look is the game's own (`gamedesign/VISUAL_BIBLE.md`).
Unity 6000.5.10f1, URP 2D, Input System; keyboard and gamepad; Mac (Apple Silicon) and Windows builds.

This file is the entry point to the project: what the game is, how to run it, and where things live.
Details are in the `gamedesign/` documents (table at the bottom). Rules for working on the project
are in `AGENTS.md`.

## What's in the game (2026-11-12)

- **Movement**: run, jump, double jump, dash and air dash, walls (slide, wall jump, wall run),
  slopes, grappling hook onto rings, up/down strikes and pogo.
- **Combat**: combos and heavy attack, block, parry, poise, status effects (bleed, poison, fire,
  frost), four styles (Warrior, Hunter, Mage, Shadow), healing flasks, mana and spells.
- **RPG**: experience and levels, 6 attributes, skill tree, 8 equipment slots, loot, a merchant.
- **World** — 5 levels (the author's decision of 2026-11-05; the other 10 zones planned in
  `gamedesign/WORLD_DESIGN.md` are shelved — `ContentBuilderWorldMap.ShelvedZones`), each in its own
  scene with seamless streaming; save altars and fast travel between them, a world map, secret rooms
  behind walls, vessel shards, trials, a bestiary:

  | Zone | Boss | Unlocks | Status |
  | --- | --- | --- | --- |
  | Забута Гробниця (Forgotten Tomb) | Страж Гробниці (Tomb Warden) | — | 27 rooms from the author's map; painted look |
  | Зів'ялий Ліс (Withered Forest) | Порожній Вартовий (Hollow Sentinel) | double jump | 35 rooms from the map; painted look |
  | Занепале Село (Fallen Village) | Дзвонар (Bell-Ringer) | grappling hook | 24 rooms from the map; own enemies, painted look |
  | Чумні Болота (Plague Marshes) | Чумний Олень (Plague Stag) | Plague Mask | 20 rooms from the map; own enemies, painted look |
  | Зруйнована Столиця (Ruined Capital) | Командор Попелу (Ash Commander), Король-Заклятий (Oathbound King) | ending | 19 rooms from the map; painted look |

- **Inhabitants**: Hnat, Myroslava, Severyn — dialogue, trading, relocating as the story progresses.
- **Interface** (UI Toolkit): HUD, menu, pause, inventory, character, skills, spells, map, altar,
  and a controls screen («Керування») with key rebinding.
- **Bosses** with their own mechanics (`BOSSES_SIGNATURES.md`); the King has a third phase — an
  eclipse and the «Стіна клятви» (Wall of the Oath); after his death comes a farewell and the
  ending (`KING_FINALE.md`).
- **Art**: the hero, enemies, bosses, backgrounds, stone and decor of all five zones are painted
  (Midjourney, Nano Banana, puppets).
- **Not final**: sound and music are procedural, synthesized in code; so are some VFX and UI icons
  (`gamedesign/KNOWN_ISSUES.md`). Playtesting follows `gamedesign/PLAYTEST.md`. Current work is in
  `gamedesign/DEVELOPMENT_PLAN.md`.

## How to run

1. Open the project in Unity 6000.5.10f1.
2. Open the scene **`Assets/Game/Scenes/MainMenu.unity`** (game with the menu, «Нова гра» — New Game)
   or **`Assets/Game/Scenes/TombTrial.unity`** (straight into the world, the hero at the Tomb
   entrance) and press Play. Zone scenes (`Scenes/Zones/Zone_*`) cannot be run on their own — they
   have no camera or player; the world scene (`TombTrial`) streams them in.
3. To see the whole world in the editor without Play — menu **Forgotten Kingdom → Scene → Open Whole World**.

## Controls (default)

| Action | Keyboard | Gamepad |
| --- | --- | --- |
| Move | A / D | left stick |
| Jump (second one in mid-air) | Space | bottom button |
| Attack / heavy attack | LMB, J / RMB, K | X / RB |
| Dash | L | RT |
| Block; parry when timed with the hit | Q (hold) | LB |
| Interact (altar, doors, people) | E | Y |
| Flask | R | D-pad up |
| Grappling hook | G | D-pad right |
| Skills 1–2, spells 1–3 | 1–5 | LT + button |
| Inventory / switch weapon / map | I, Tab / F / M | Select / D-pad left |

Full list and rebinding — `gamedesign/CONTROLS.md`.

## Developer keys (editor and development builds only)

- **Ctrl+N — noclip**: fly through walls to inspect zones. WASD or arrows, Shift — faster,
  mouse wheel or `-` / `=` — zoom up to ×5; Ctrl+N again — land. While flying the camera ignores
  room bounds and you take no damage.
- **`** — world panel (unlock zones, shortcuts, fast travel, teleport to an altar), **F1** — combat
  and progress debugging, **F2** — player animation, **F6** — enemy animation, **F9** — environment,
  **Ctrl+F** — feedback. Only one panel is open at a time, and its keys work only while it is open.

## Project structure

```
Assets/Game/
  Runtime/     game code (Game.Runtime assembly)
  Editor/      ContentBuilder content generator and Scene*.Apply scene passes (Game.Editor)
  Levels/      zones as text: tomb, forest, village, marsh, capital (.txt; gamedesign/LEVEL_TOOL.md)
  Art/ Audio/  generator art and sound; painted files are listed in Art/Painted.txt (the generator leaves them alone);
               Art/Levels — zones rendered by the painter
  Scenes/      MainMenu, TombTrial (world: player, camera, services), Zones/Zone_* (zone levels)
  Tests/       EditMode and PlayMode
design/
  ui/          HTML screen mockups (view: node design/ui/_shared/serve.js → localhost:8123)
  levels/      Tomb look mockup and the painter — code that draws a zone from its text
tools/         map art (map-art) and level art (level-art) rendering via headless Chrome
gamedesign/    development documents (in Russian; some in Ukrainian)
```

Key rules (in detail — `AGENTS.md`):

- **Content is generated by code.** `ContentBuilder.Build` writes art, sound, data and prefabs;
  scenes are changed only by the editor `Scene*.Apply` passes. A zone is written as text in
  `Levels/*.txt`, checked for traversability (`LevelCheck`) and built into a scene
  (`SceneTomb.Apply` and so on).
- **Painted zone** (the Tomb for now): text → `node tools/level-art/render.js <zone>` →
  `ContentBuilder.RefreshLevelArt` → `Scene<Zone>.Apply`. Blocks are colliders only; everything
  visible is the painter's images (`gamedesign/LEVEL_TOOL.md`, "Painted level").
- **Verification** — batchmode tests on the `Artifacts/Verification/Project` copy; the live editor
  is for viewing only.

## Documents (`gamedesign/`)

| Topic | Document |
| --- | --- |
| Current state, open tasks | `DEVELOPMENT_PLAN.md` (history — `archive/DEVELOPMENT_HISTORY.md`) |
| World: 15 zones, order, bosses, abilities | `WORLD_DESIGN.md`, the author's maps — `reference/maps/` |
| Zones as text, checking, painter | `LEVEL_TOOL.md` |
| World graph, zone scenes, map | `WORLD_GRAPH.md`, `WORLD_SCENES.md`, `WORLD_MAP.md` |
| Ability gates, fast travel | `PROGRESSION_GATES.md`, `FAST_TRAVEL.md` |
| The Capital | `CAPITAL.md` |
| Story, inscriptions, ending, New Game+ | `STORY.md` |
| Inhabitants, merchant | `NPC_AND_SHOP.md` |
| Secret rooms, shards, trials, bestiary | `EXPLORATION.md` |
| Balance numbers | `TUNING.md` |
| Keys and debugging | `CONTROLS.md` |
| Visual style | `VISUAL_BIBLE.md`, `CONCEPT_BRIEFS.md`, `ART_REQUIREMENTS.md` |
| Tomb environment | `ENVIRONMENT_TOMB.md` |
| Art: asset list and swapping in painted art | `ART_ASSET_LIST.md`, `ANIMATION_LIST.md`, `ANIMATION_PIPELINE.md`, `VFX_LIST.md` |
| Combat feedback | `COMBAT_FEEDBACK.md`; game-feel improvement ideas — `GAME_FEEL_IDEAS.md` |
| Sound and music | `AUDIO_DESIGN.md`, `AUDIO_ASSET_LIST.md`, `MUSIC_SYSTEM.md` |
| Interface | `UI_UX_GUIDE.md`, mockups — `design/ui/` |
| Known issues, QA | `KNOWN_ISSUES.md`, `VERTICAL_SLICE_QA.md` |
