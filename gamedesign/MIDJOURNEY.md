# Midjourney — арт зон

Решение пользователя (2026-10-24): арт уровня «премиум» делается через Midjourney — Claude пишет
промпты, пользователь генерирует картинки, Claude вписывает их в игру (подгон под палитру, кадрирование,
маски, свет). Стиль — `VISUAL_BIBLE.md`; состав промптов, мотивы комнат и порядок — решено Claude по
постоянному согласию. Документ объединяет два параллельных черновика сессий 2026-10-24/25.

Порядок: **Гробница целиком → Лес → Село → Столица**. Следующая зона — после того, как Гробница
собрана в игре и показана скриншотами.

## Что Midjourney делает хорошо, а что нет

| Годится | Не годится |
| --- | --- |
| фоны комнат (дальний план с параллаксом) | анимация героя и врагов (кадры не совпадают между собой) |
| бесшовные текстуры (`--tile`) для камня, дерева, земли | ровные маски с прозрачностью (дадим чёрный фон — маску вырежет Claude) |
| отдельные предметы-декор на чёрном фоне | точная геометрия под коллайдеры уровня |
| концепты персонажей (для будущего аниматора) | надписи и герб в точности одинаковый на всех картинках |

Ходят по-прежнему по камню painter'а (коллайдеры из текста зоны). Midjourney даёт ему **фактуру**,
а комнате — **живописный фон** и **декор**.

## Как работать

1. **Настройки (веб-интерфейс, версия 8.2):** Version 8.2, Raw — **Standard** (Raw тянет к фото и 3D-рендеру — проверено), Stylization ~250, Weirdness 0,
   Variety 0; Standard — для якоря и проб, HD — для финальных фонов (выше разрешение). `--v` в промпт не
   писать — берётся версия из настроек. Ссылку `--sref` не печатать: перетащить картинку в строку промпта
   и выбрать «Style reference». Параметры в тексте промпта (`--ar`, `--no`) важнее ползунков.
2. **Шаг 0 — якорь стиля.** Перетащить в строку промпта карту зоны (`gamedesign/reference/maps/01_forgotten_tomb.webp`)
   как Style reference и сгенерировать промпт из §0. Выбрать одну лучшую картинку, сделать Upscale,
   скопировать её ссылку (или код стиля `--sref <число>`). Дальше во **всех** промптах вместо `<STYLE>`
   подставлять эту ссылку — тогда 27 комнат будут одной игрой, а не 27 разными.
3. **Из четырёх вариантов** брать тот, где: вид строго сбоку, нет людей, нет текста, **середина
   кадра спокойная и тёмная** (по ней ходит герой), интерес — по краям и вверху. Слишком светлую или
   пёструю не брать. Upscale (Subtle) → скачать (png/jpg/webp).
4. **Куда класть** (имя файла — из таблиц ниже): фоны комнат — `design/levels/midjourney/tomb/<id>.png`,
   текстуры — `…/tomb/textures/`, декор — `…/tomb/props/`, концепты — `design/levels/midjourney/concepts/`.
   Исходники идут в git, чтобы любая сессия могла их вписать.
5. Можно присылать порциями: «готово rooms 1–10» — Claude сразу вставляет и показывает скриншот.

Аспект (`--ar`) в таблице подобран под размер комнаты; если Midjourney не берёт крайний аспект,
ставьте ближайший (`1:2`, `2:1`) — лишнее Claude обрежет или достроит.

## Общие куски промпта

Подставлять в конец каждого промпта своего раздела.

**FON** (фоны комнат):

```
dark gothic fantasy, painterly 2D game background, soft brush strokes, layered fog, cold blue-grey stone, faded royal gold accents, hand-painted digital matte painting, visible soft brush strokes, muted desaturated palette, low contrast, dim, palette #2A2520 #3E4247 #2F3E52, faint violet shadow veins, volumetric light shafts, ancient ruined royal crypt, distant background layer for a 2D side-scroller, straight-on side view, flat orthographic, empty interior, calm dark empty centre, no characters --sref <STYLE> --no people, figures, creatures, text, letters, logo, frame, border, vignette, UI, watermark, foreground floor, saturated colors, bright sky, photo, photorealistic, 3d render, perspective floor, vanishing point
```

**TEX** (бесшовные текстуры):

```
seamless tileable texture, flat top-down orthographic, even soft lighting, no shadows, no perspective, painterly hand-painted game texture, muted desaturated palette --tile --ar 1:1 --sref <STYLE> --no text, objects, vignette, highlights, photo
```

**PROP** (декор):

```
single object, isolated on pure black background, centered, whole object visible with margin, straight-on side view, painterly dark gothic fantasy game asset, soft rim light, muted desaturated palette, faded gold accents --sref <STYLE> --no background, floor, shadow on ground, text, people, frame, photo, 3d render
```

## §0. Якорь стиля

Карта зоны — в Style reference; Raw выключен. Промпт, давший якорь Гробницы (2026-10-25, вариант 4 из 4):

```
side view of a vast gothic royal crypt hall, straight-on flat orthographic view like a theatre stage set, rows of pointed arches and clustered columns in parallel layers, stone effigies of knights resting in niches, faded blue banners with a golden crown and flame emblem, warm candlelight accents against cold blue-grey stone, drifting fog, pale light shafts falling from cracks in the vault, melancholic grandeur, dark gothic fantasy, hand-painted digital matte painting, illustrated 2D game background, visible soft brush strokes, painterly concept art, muted desaturated palette --ar 16:9 --no people, text, logo, watermark, photo, photorealistic, 3d render, perspective floor, vanishing point
```

«theatre stage set» даёт тёмную рваную рамку по краям — в промптах комнат его нет, рамка запрещена в `--no`.
Якорь сохранить как `design/levels/midjourney/tomb/_style_anchor.png`.

## §1. Гробница — фоны комнат (27)

Промпт = **текст строки** + `, ` + **FON** + `--ar` из строки. Имя файла:
`design/levels/midjourney/tomb/<id>.png`. Первыми — `entrance`, `burial_hall`, `guardian_arena` (их видят
первыми): Claude ставит их и показывает скриншот, потом остальные. У каждой комнаты свой мотив — повторов нет.

| # | id (имя файла) | Комната | --ar | Текст |
| --- | --- | --- | --- | --- |
| 1 | `room_tomb_entrance` | Вход | 2:1 | tomb entrance hall, massive half-open iron portcullis with grey dawn haze behind it, a tall hooded mourner statue beside the gate, tattered banner on a leaning spear, worn steps leading down |
| 2 | `room_tomb_burial_hall` | Поховальная зала | 5:2 | long royal burial hall, rows of stone sarcophagi with recumbent knight effigies, tall pointed niches, a warm amber altar glow in the far center, dusty iron chandeliers hanging from the vault |
| 3 | `room_tomb_west_shaft` | Западная шахта | 1:1 | narrow vertical stone shaft, remnants of a broken spiral stair clinging to the walls, iron rings set in stone, pale roots creeping down from above, faint light from the top |
| 4 | `room_tomb_guard_passage` | Проход стражи | 7:3 | guard corridor of the crypt, empty weapon racks with rusted halberds, round shields bearing crossed swords under a crown, low ribbed vault, guard alcoves with stone benches |
| 5 | `room_tomb_chain_shaft` | Цепная шахта | 1:3 | deep vertical shaft, enormous rusted chains hanging from darkness above, stone counterweights, the iron frame of an old lift cage, cold mist far below |
| 6 | `room_tomb_guardian_approach` | Подступ к Стражу | 8:7 | approach to a ceremonial hall, a grand staircase rising, twin colossal hooded knight statues holding swords point-down, a faint crimson glow beyond a great arch |
| 7 | `room_tomb_guardian_arena` | Арена Стража | 8:7 | ceremonial arena of a royal tomb, a giant carved crown and flame emblem on the back wall, dark crimson velvet drapes, tall candelabras, a royal sarcophagus on a raised dais, a dim rose window high above |
| 8 | `room_tomb_royal_ossuary` | Королевская костница | 1:1 | royal ossuary, walls built from stacked skulls and bones in ornamental patterns, a gilded crowned skull in a central niche, melted candle stubs |
| 9 | `room_tomb_upper_passage` | Верхний коридор | 3:1 | upper gallery corridor, broken stone balustrade, view through a row of arches into a vast dark nave below, heavy cobwebs, dust in the air |
| 10 | `room_tomb_statue_hall` | Зала статуй | 2:3 | tall narrow hall of statues, giant knight effigies stacked in niches up to the vault, one statue fallen and broken across the hall, a single light beam from above |
| 11 | `room_tomb_east_chapel` | Восточная часовня | 7:3 | small funerary chapel, rows of stone pews, a faded fresco of a crown and flame in the apse, an altar with candles, shards of blue stained glass glowing dimly |
| 12 | `room_tomb_flooded_crypt` | Затопленный склеп | 1:2 | flooded crypt, dark still water reflecting pointed arches, half-submerged tombstones, dripping water, pale cyan light seeping through cracks |
| 13 | `room_tomb_drowned_cache` | Тайник над водой | 1:1 | small hidden chamber above water, wet glistening stones, an old shelf with clay funeral urns, moss in the seams, water drips |
| 14 | `room_tomb_east_hall` | Восточная зала | 2:1 | eastern crypt hall with a broken ceiling, roots of a forest breaking through the vault, first green moss on the stones, pale moonlight falling in, transition from crypt to forest |
| 15 | `room_tomb_falls` | Водопад | 1:1 | underground waterfall pouring from a broken aqueduct arch into a dark pool, rising mist spray, glistening wet rock, blue-grey tones |
| 16 | `room_tomb_lower_crypt` | Нижний склеп | 4:3 | lower crypt of an older age, low heavy round romanesque arches, coffins stacked in wall niches, thick dust, a single hanging lantern |
| 17 | `room_tomb_lower_chain` | Нижняя цепь | 5:6 | bottom of a chain shaft, huge chains ending in a giant rusted gear mechanism and winch, iron beams, dripping water |
| 18 | `room_tomb_reliquary` | Реликварий | 1:1 | reliquary chamber, gilded reliquary caskets behind wrought iron grilles with quatrefoil lattice, faded gold, a dim warm glow |
| 19 | `room_tomb_rubble_slope` | Обвал | 1:1 | collapsed hall, a huge fallen column lying diagonally, broken arch fragments forming a slope, dust drifting in a light beam |
| 20 | `room_tomb_buried_cache` | Заваленный тайник | 5:4 | half-buried chamber, rubble covering a carved relief wall, a crushed sarcophagus, roots hanging from cracks |
| 21 | `room_tomb_collapsed_crypt` | Обрушенный склеп | 5:4 | collapsed crypt, tilted tombstones and leaning obelisks, a broken vault opening into darkness, a thin trickle of water falling |
| 22 | `room_tomb_crypt_nest` | Гнездо над склепом | 1:1 | lair above a crypt, piles of bones and rags, deep claw scratches on the walls, thick cobwebs, eerie stillness |
| 23 | `room_tomb_column` | Столп | 1:2 | a single colossal pillar rising through a vertical chamber, carved with a spiralling relief of marching knights, a broken capital at the top |
| 24 | `room_tomb_walled_cell` | Замурованная келья | 2:1 | walled-up monk cell, a bricked-over arch, rusted chains on the wall, a lone wooden stool, carved tally marks on stone |
| 25 | `room_tomb_mason_cache` | Келья каменщика | 1:1 | stonemason's cell, an unfinished knight statue, chisels and mallets on a bench, rough stone blocks, scaffold planks |
| 26 | `room_tomb_bell_shaft` | Колокольная шахта | 1:2 | bell shaft, a huge cracked bronze bell hanging in a vertical stone tower, frayed ropes, heavy timber beams |
| 27 | `room_tomb_arcade` | Арковая галерея | 3:2 | arched gallery, a rhythmic colonnade of pointed arches receding into fog, pools of pale light on the stone |

## §2. Гробница — текстуры (6)

Промпт = **текст** + `, ` + **TEX**. Файл: `design/levels/midjourney/tomb/textures/<имя>.png`.

| Имя файла | Для чего | Текст |
| --- | --- | --- |
| `tex_tomb_rock` | масса камня, по которой ходят | rough dark granite rock surface, natural cracks, chipped edges, dust in crevices, cold grey-brown |
| `tex_tomb_masonry` | кладка стен | old gothic stone masonry wall, large irregular blocks, eroded rounded corners, mortar seams filled with dust |
| `tex_tomb_moss` | мох в швах, низ стен | damp mossy stone, dark cold green moss in patches, wet sheen |
| `tex_tomb_marble` | гладкий мрамор (не зацепиться) | polished dark grey marble with thin cracks and long pale vertical veins |
| `tex_tomb_floor` | плиты пола, кант | worn tombstone floor slabs with faint illegible carved letters, scuffed |
| `tex_tomb_bones` | костница, гнездо | densely packed old bones and skulls, yellowed ivory, dusty |

## §3. Гробница — декор (12)

Промпт = **текст** + `, ` + **PROP** + `--ar` из строки. Файл: `design/levels/midjourney/tomb/props/<имя>.png`.
Фон должен быть именно чёрным — по нему вырезается прозрачность.

| Имя файла | --ar | Текст |
| --- | --- | --- |
| `prop_tomb_effigy_statue` | 1:2 | standing stone knight effigy statue, hands crossed over a sword pointing down, eroded face |
| `prop_tomb_mourner_statue` | 1:2 | tall hooded mourner statue, veiled face, robes, weathered limestone |
| `prop_tomb_sarcophagus` | 2:1 | stone sarcophagus with a recumbent knight effigy on the lid, carved side panels |
| `prop_tomb_candelabra` | 1:2 | tall wrought iron candelabra with melted wax candles, warm flames |
| `prop_tomb_banner` | 1:3 | hanging tattered royal banner, faded blue velvet, gold embroidered crown and flame emblem, frayed bottom edge |
| `prop_tomb_chains` | 1:3 | heavy rusted iron chains hanging vertically, links clearly readable |
| `prop_tomb_urn` | 1:1 | group of three clay and bronze funeral urns, cracked |
| `prop_tomb_bone_pile` | 2:1 | pile of old bones and skulls with a dented helmet |
| `prop_tomb_broken_column` | 1:2 | broken gothic clustered column with a fallen capital lying beside it |
| `prop_tomb_crown_altar` | 1:1 | small stone altar shaped like a crown with three pointed tips and a flame bowl in the middle, warm amber glow |
| `prop_tomb_cobweb` | 1:1 | large dusty cobweb in a corner, soft silver strands |
| `prop_tomb_roots` | 1:2 | pale gnarled roots hanging down from above, thin tendrils |

## §4. Концепты персонажей (на потом)

В игру напрямую не идут (анимации нужен аниматор), но фиксируют вид героя и врагов — по ним
Claude правит процедурные спрайты, а позже работает художник. Файл: `design/levels/midjourney/concepts/<имя>.png`.

| Имя файла | --ar | Промпт |
| --- | --- | --- |
| `concept_hero` | 3:2 | character turnaround sheet of a lone wanderer, front, side and back views, deep hood hiding the face in shadow, dark wool cloak widening downward into a triangle silhouette, old steel sword, worn leather straps, a dull grey-blue scarf accent, faint purple veins glowing on one forearm, slim agile build, 6.5 heads tall, dark gothic fantasy, painterly concept art, plain dark grey background --sref <STYLE> --no text, logo, photo, 3d render |
| `concept_zombie_soldier` | 3:2 | character turnaround sheet of an undead royal soldier, hunched, asymmetric shoulders, rusted chainmail and torn blue tabard, grey-green dead skin, faint purple glow in the eye sockets, broken sword, dark gothic fantasy, painterly concept art, plain dark grey background --sref <STYLE> --no text, logo, photo, 3d render |
| `concept_shieldguard` | 3:2 | character turnaround sheet of an undead shield guard, heavy rectangular silhouette, tall crested helmet, large tower shield with crossed swords under a crown, purple light in cracks of the armor, dark gothic fantasy, painterly concept art, plain dark grey background --sref <STYLE> --no text, logo, photo, 3d render |
| `concept_bone_archer` | 3:2 | character turnaround sheet of a skeletal archer, tall thin silhouette, long bow forming a strong horizontal line, tattered hood, quiver of black arrows, dark gothic fantasy, painterly concept art, plain dark grey background --sref <STYLE> --no text, logo, photo, 3d render |
| `concept_tomb_guardian` | 3:2 | character turnaround sheet of a colossal tomb guardian knight boss, monumental trapezoid silhouette, crown on the helmet, dark crimson cape, great shield with a crown and flame emblem, symmetrical and solemn, second variant with cracked armor and purple shadow smoke pouring from the chest, dark gothic fantasy, painterly concept art, plain dark grey background --sref <STYLE> --no text, logo, photo, 3d render |

## Как Claude вписывает результат

- **Фоны** → слой `mid` комнаты (`Art/Levels/Tomb/Rooms/<id>_mid.png`) — непрозрачная картинка за стеной,
  видна сквозь арки и двигается с камерой (`ContentBuilderLevelArt`). `tools/midjourney/place.sh tomb`
  обрезает по центру под пропорции комнаты и масштабирует (macOS, `sips`; на Windows — порт при первой
  вставке). Затем тон под план «средний фон» (`VISUAL_BIBLE.md` §10: L* 10–30, низкая насыщенность),
  туман снизу. После `tools/level-art/render.sh tomb` запустить `place.sh` ещё раз. Слой `near` (стена с
  арками) остаётся от painter'а, но получает текстуру кладки.
- **Текстуры** → painter (`design/levels/painter/terrain.js`) накладывает их на массы камня в тоне
  зоны; из текстуры строится карта нормалей для 2D-света.
- **Декор** → прозрачность по чёрному фону, спрайты на слой `EnvironmentBack`, расстановка по комнатам
  без повторов.
- Исходники — `design/levels/midjourney/`, результат — в `Assets/Game/Art/` с записью в `ART_ASSET_LIST.md`.
