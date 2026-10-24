# Midjourney — фоны комнат

Решено пользователем 2026-10-25: визуал поднимать картинками Midjourney; Claude пишет промпты, пользователь
генерирует. Первая цель — **дальний слой (`_mid`) комнат Гробницы**: это непрозрачная картинка за стеной
комнаты, она видна сквозь арки и двигается с камерой (`ContentBuilderLevelArt`). Ближняя стена (`_near`)
с прозрачными арками пока остаётся процедурной — Midjourney не делает прозрачность.

## Как работать

1. **Стиль.** Загрузить в Midjourney карту мира `gamedesign/reference/maps/` (любую — это стиль игры) и
   добавлять её ссылку в каждый промпт через `--sref <ссылка>`. Без неё — просто общий хвост ниже.
2. **Генерация.** Вставить промпт комнаты целиком (начало + общий хвост + `--ar`). Из четырёх вариантов
   выбрать тот, где **середина кадра спокойная и тёмная** (по ней ходит герой), а интерес — по краям и вверху.
   Upscale (Subtle), скачать.
3. **Имя файла = id комнаты**: `room_tomb_entrance.png` (jpg/webp тоже можно) → в папку
   `design/levels/midjourney/tomb/`.
4. **Вставка в игру:** `tools/midjourney/place.sh tomb` — обрезает по центру под пропорции комнаты и
   ставит вместо `Assets/Game/Art/Levels/Tomb/Rooms/<комната>_mid.png`. Unity подхватит при открытии.
   После `tools/level-art/render.sh tomb` (смена текста уровня) — запустить `place.sh` ещё раз.

## Общий хвост (дописывать к каждому промпту)

```
, dark fantasy gothic tomb interior, ruined ancient royal architecture, pointed arches, ribbed vaults,
eroded stone, painterly concept art, soft volumetric fog, cold blue-grey palette #2A2520 #3E4247 #2F3E52,
faint faded gold accents, tiny violet shadow corruption veins, low contrast, dark low-key lighting,
distant background layer for a 2D side-scrolling platformer, orthographic side view, no characters,
no text, no floor in the foreground, calm empty centre --style raw --stylize 150
```

Правила (`VISUAL_BIBLE.md` §2, §10): дальний фон — светлота низкая (L* 10–30), насыщенность низкая, без
резких деталей; тёплое — только алтари; фиолет Тени ≤ 5 %. Слишком светлую или пёструю картинку не брать.

## Промпты комнат Гробницы

`--ar` — пропорции комнаты (скрипт всё равно обрежет точно).

| Комната | Промпт (начало) | `--ar` |
| --- | --- | --- |
| `room_tomb_entrance` | entrance hall of a forgotten royal tomb, great broken gate far behind, pale cold daylight falling through a cracked dome | `2:1` |
| `room_tomb_burial_hall` | long burial hall, rows of stone sarcophagi in deep alcoves, a warm candle-lit altar glow far away | `5:2` |
| `room_tomb_west_shaft` | narrow vertical stone shaft, worn carved steps in the walls, darkness below | `1:1` |
| `room_tomb_guard_passage` | long guards' passage, statues of armoured knights in niches, torn blue royal banners | `5:2` |
| `room_tomb_chain_shaft` | very tall shaft, giant rusted chains hanging from the darkness above, distant lattice of beams | `1:3` |
| `room_tomb_guardian_approach` | grand stair-hall before a sealed bronze door with the crown-flame crest, oppressive silence | `8:7` |
| `room_tomb_guardian_arena` | vast circular crypt arena, colossal broken statue of a guardian knight behind, cracks glowing faint violet | `8:7` |
| `room_tomb_royal_ossuary` | small royal ossuary, walls of stacked skulls and bones framed in faded gold | `12:13` |
| `room_tomb_upper_passage` | low upper corridor, cracked ribbed ceiling, dust drifting in thin light beams | `3:1` |
| `room_tomb_statue_hall` | tall hall of weeping queen statues, faces eroded, veils of cobweb | `2:3` |
| `room_tomb_east_chapel` | eastern chapel, tall shattered stained-glass windows, dim cold moonlight, pews overturned | `7:3` |
| `room_tomb_flooded_crypt` | tall flooded crypt, black still water below, dripping vaults, reflections of pale light | `5:9` |
| `room_tomb_drowned_cache` | small dry niche above water, hidden treasure alcove, moss and wet stone | `1:1` |
| `room_tomb_east_hall` | eastern hall where roots of the forest break through the vaults, first green light | `2:1` |
| `room_tomb_falls` | underground waterfall pouring from a broken aqueduct, mist, cold spray | `1:1` |
| `room_tomb_lower_crypt` | lower crypt, low heavy arches, rows of plain coffins, cold damp air | `4:3` |
| `room_tomb_lower_chain` | lower end of the great chains, counterweights and rusted gears in the dark | `5:6` |
| `room_tomb_reliquary` | small reliquary, empty golden reliquary cases behind iron grilles | `1:1` |
| `room_tomb_rubble_slope` | collapsed hall, a slope of fallen masonry and broken columns, dust in the air | `1:1` |
| `room_tomb_buried_cache` | half-buried storeroom, cracked urns and chests under rubble | `5:4` |
| `room_tomb_collapsed_crypt` | huge collapsed crypt, the vault fallen in, a hole to darkness above, roots hanging | `5:4` |
| `room_tomb_crypt_nest` | high nest in a crypt wall, twigs and bones, something has lived here | `1:1` |
| `room_tomb_column` | a single colossal carved column rising through floors, vertical darkness | `4:7` |
| `room_tomb_walled_cell` | walled-up monk's cell, scratched tally marks, a narrow slit of light | `2:1` |
| `room_tomb_mason_cache` | stonemason's cell, half-carved statues, chisels, blocks of stone | `1:1` |
| `room_tomb_bell_shaft` | bell shaft, a huge cracked bronze bell hanging in the gloom above | `4:7` |
| `room_tomb_arcade` | arcade gallery, a rhythm of pointed arches receding into fog | `3:2` |

Пример целиком (Вход):

```
entrance hall of a forgotten royal tomb, great broken gate far behind, pale cold daylight falling through
a cracked dome, dark fantasy gothic tomb interior, ruined ancient royal architecture, pointed arches,
ribbed vaults, eroded stone, painterly concept art, soft volumetric fog, cold blue-grey palette #2A2520
#3E4247 #2F3E52, faint faded gold accents, tiny violet shadow corruption veins, low contrast, dark low-key
lighting, distant background layer for a 2D side-scrolling platformer, orthographic side view, no
characters, no text, no floor in the foreground, calm empty centre --style raw --stylize 150 --ar 2:1
```

Порядок: сначала `entrance`, `burial_hall`, `guardian_arena` (их видят первыми) — Claude ставит их в игру и
показывает скриншот, потом остальные.
