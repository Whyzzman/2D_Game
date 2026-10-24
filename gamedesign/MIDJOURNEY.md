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
2. **Якорь стиля для фонов не ставить** (§1: он повторял композицию). Для текстур, декора и концептов — по
   желанию: перетащить карту зоны (`gamedesign/reference/maps/01_forgotten_tomb.webp`) как Style reference (`<STYLE>` в
   хвостах TEX и PROP). Единство фонов держит общий хвост FON, а не картинка-образец.
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
hand-drawn 2D side-scroller game environment, painterly, soft ink outlines, deep layered parallax planes fading into mist, dark silhouetted foreground shapes at the edges, organic flowing forms, one dominant hue with a single accent glow, atmospheric haze, drifting faint glowing particles, asymmetric composition, open negative space, no central focal object, side view, enclosed underground, no sky, empty, no characters --s 300 --no people, characters, creatures, trees, sky, text, logo, frame, border, UI, watermark, symmetry, centered composition, photo, photorealistic, 3d render, vanishing point, perspective floor
```

**TEX** (бесшовные текстуры):

```
seamless tileable texture, flat top-down orthographic, even soft lighting, no shadows, no perspective, painterly hand-painted game texture, muted desaturated palette --tile --ar 1:1 --sref <STYLE> --no text, objects, vignette, highlights, photo
```

**PROP** (декор):

```
single object, isolated on pure black background, centered, whole object visible with margin, straight-on side view, painterly dark gothic fantasy game asset, soft rim light, muted desaturated palette, faded gold accents --sref <STYLE> --no background, floor, shadow on ground, text, people, frame, photo, 3d render
```

## §0. Якорь стиля (для фонов не используется с 2026-10-25)

Карта зоны — в Style reference; Raw выключен. Промпт, давший якорь Гробницы (2026-10-25, вариант 4 из 4):

```
side view of a vast gothic royal crypt hall, straight-on flat orthographic view like a theatre stage set, rows of pointed arches and clustered columns in parallel layers, stone effigies of knights resting in niches, faded blue banners with a golden crown and flame emblem, warm candlelight accents against cold blue-grey stone, drifting fog, pale light shafts falling from cracks in the vault, melancholic grandeur, dark gothic fantasy, hand-painted digital matte painting, illustrated 2D game background, visible soft brush strokes, painterly concept art, muted desaturated palette --ar 16:9 --no people, text, logo, watermark, photo, photorealistic, 3d render, perspective floor, vanishing point
```

«theatre stage set» даёт тёмную рваную рамку по краям — в промптах комнат его нет, рамка запрещена в `--no`.
Якорь сохранить как `design/levels/midjourney/tomb/_style_anchor.png`.

## §1. Гробница — фоны комнат (27)

Промпт = **текст строки** + `, ` + **FON** + `--ar` из строки. Имя файла:
`design/levels/midjourney/tomb/<id>.png`. Первыми — `entrance`, `burial_hall`, `guardian_arena` (их видят
первыми): Claude ставит их и показывает скриншот, потом остальные.

Решения пользователя 2026-10-25: фоны разнообразнее и разного цвета; «в каждом рисунке зала и в центре что-то
стоит» — сделать каждую часть уровня особенной, как в Hollow Knight. Поэтому: **якоря стиля нет** (он сам был
симметричной залой и повторял композицию во всех картинках); у каждой комнаты своя форма (шахта, пропасть,
грот, водопад, логово, корни) и своя гамма; хвост FON описывает качества Hollow Knight словами — асимметрия,
силуэты по краям, глубокие слои тумана, одна главная гамма с одним акцентом, — но не называет игру, чтобы
Midjourney не копировал чужой вид (`VISUAL_BIBLE.md` §3). Без «underground, no sky» залы выходили городом под
открытым небом с деревьями — хвост это запрещает; соседние комнаты не делить одну гамму (Зала — тёплые свечи на
холодном камне, Арена — кармин).

| # | id (имя файла) | Комната | --ar | Текст |
| --- | --- | --- | --- | --- |
| 1 | `room_tomb_entrance` | Вход | 2:1 | crumbling tomb gateway half swallowed by rock on the left, a broken portcullis hanging askew, pale dawn light spilling diagonally from a crack above, a hooded statue toppled on its side, ash grey and dawn silver hues |
| 2 | `room_tomb_burial_hall` | Поховальная зала | 5:2 | underground royal burial vault deep beneath the earth, rows of stone sarcophagi receding to the right in staggered layers inside a cavernous crypt, clusters of candles glowing on the lids, iron chandeliers hanging from a rough rock ceiling, warm amber candle glow against cold grey-green stone |
| 3 | `room_tomb_west_shaft` | Западная шахта | 1:1 | narrow vertical rock shaft, fragments of a ruined spiral stair clinging to one wall, pale roots and moss hanging down, drips of light from far above, damp moss green hues |
| 4 | `room_tomb_guard_passage` | Проход стражи | 7:3 | long low corridor curving like a rib cage, rusted halberds leaning in alcoves, collapsed heaps of old armour, rust brown and faded navy hues |
| 5 | `room_tomb_chain_shaft` | Цепная шахта | 1:3 | colossal vertical abyss, giant rusted chains plunging from darkness through layers of fog, a broken lift cage dangling off to one side, rust orange and charcoal hues |
| 6 | `room_tomb_guardian_approach` | Подступ к Стражу | 8:7 | cavern stairway carved into living rock, giant stone hands emerging from the wall, a faint crimson glow seeping from above, dusky violet hues |
| 7 | `room_tomb_guardian_arena` | Арена Стража | 8:7 | vast round chamber like the inside of a crown, a colossal broken crown sculpture lying tilted in the background, torn crimson banners, gold dust drifting, deep crimson and gold hues |
| 8 | `room_tomb_royal_ossuary` | Королевская костница | 1:1 | cave walls overgrown with bones like coral, skulls forming an organic honeycomb, a single crowned skull glinting in a hollow, bone ivory and sepia hues |
| 9 | `room_tomb_upper_passage` | Верхний коридор | 3:1 | narrow ledge gallery high above a void, a broken balustrade, glimpses of distant towers through holes in the wall, dust motes in thin beams, dusty beige and grey hues |
| 10 | `room_tomb_statue_hall` | Зала статуй | 2:3 | tall chasm with giant weathered knight statues half buried in the rock walls at different heights, one fallen diagonally across the gap, pale moonlight silver and slate hues |
| 11 | `room_tomb_east_chapel` | Восточная часовня | 7:3 | small half-flooded chapel, shattered stained glass glowing in pools on the floor, leaning pews, a tilted altar, muted blue and dusty rose hues |
| 12 | `room_tomb_flooded_crypt` | Затопленный склеп | 1:2 | drowned crypt, still black water mirroring broken arches, sinking tombstones, faint bioluminescent algae on the walls, deep teal and cyan hues |
| 13 | `room_tomb_drowned_cache` | Тайник над водой | 1:1 | small grotto above dark water, glistening wet rocks, hanging strands of moss, a faint green glow rising from below, green-teal hues |
| 14 | `room_tomb_east_hall` | Восточная зала | 2:1 | ruined hall invaded by giant tree roots bursting through the ceiling, moss and small ferns on the stones, moonlight filtering through the roots, moss green and silver hues |
| 15 | `room_tomb_falls` | Водопад | 1:1 | towering underground waterfall cascading from a broken aqueduct, clouds of mist, glowing spray, wet black rock, icy turquoise and white hues |
| 16 | `room_tomb_lower_crypt` | Нижний склеп | 4:3 | ancient low catacomb, heavy round arches sinking unevenly into the ground, coffin niches stacked irregularly, a lone lantern, earthy brown and ochre hues |
| 17 | `room_tomb_lower_chain` | Нижняя цепь | 5:6 | bottom of a pit, enormous rusted gears and winches half sunk in sludge, chains coiled like serpents, oily black and rust red hues |
| 18 | `room_tomb_reliquary` | Реликварий | 1:1 | hidden vault of golden reliquaries behind twisted iron lattices, a warm dim glow, tarnished gold and bronze hues |
| 19 | `room_tomb_rubble_slope` | Обвал | 1:1 | collapsed cavern, huge fallen pillars forming a diagonal slope, a storm of dust in a single light shaft, sandstone and grey haze hues |
| 20 | `room_tomb_buried_cache` | Заваленный тайник | 5:4 | chamber crushed by earth, roots pushing through a carved relief wall, broken urns spilling soil, clay brown and umber hues |
| 21 | `room_tomb_collapsed_crypt` | Обрушенный склеп | 5:4 | cavern where a crypt fell into a sinkhole, tilted tombstones on islands of rock, a thin waterfall from above, cold blue-grey and green-black hues |
| 22 | `room_tomb_crypt_nest` | Гнездо над склепом | 1:1 | creature lair, walls draped in webs and shadow tendrils, bones heaped into a nest, a sickly violet glow, violet and black hues |
| 23 | `room_tomb_column` | Столп | 1:2 | a lone colossal pillar rising out of a misty chasm, spiral carvings of marching knights, rock walls fading into fog, pale limestone and cold blue hues |
| 24 | `room_tomb_walled_cell` | Замурованная келья | 2:1 | cramped walled-up cell, rusted chains on the wall, scratched tally marks, a single candle stub glowing, dim orange in near darkness |
| 25 | `room_tomb_mason_cache` | Келья каменщика | 1:1 | abandoned sculptor's workshop, a half-carved giant face emerging from a stone block, rickety scaffolding, chalky white and warm grey hues |
| 26 | `room_tomb_bell_shaft` | Колокольная шахта | 1:2 | vertical tower shaft, a giant cracked bell hanging askew, frayed ropes, splintered timber beams, green patina bronze and brown hues |
| 27 | `room_tomb_arcade` | Арковая галерея | 3:2 | colonnade curving away into fog, arches broken off at different heights, ivy on the stones, soft lavender dusk and grey hues |

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
  видна сквозь арки и двигается с камерой (`ContentBuilderLevelArt`). `python tools/midjourney/place.py tomb`
  (Pillow: `pip install pillow`; macOS и Windows) обрезает по центру под пропорции комнаты, масштабирует и
  тонирует под дальний план (`VISUAL_BIBLE.md` §10: светлота L* 6–40 мягким плечом, насыщенность ×0,7 — гамма
  комнаты остаётся своей). После `tools/level-art/render.sh tomb` запустить `place.py` ещё раз. Слой `near` (стена с
  арками) остаётся от painter'а, но получает текстуру кладки.
- **Текстуры** → painter (`design/levels/painter/terrain.js`) накладывает их на массы камня в тоне
  зоны; из текстуры строится карта нормалей для 2D-света.
- **Декор** → прозрачность по чёрному фону, спрайты на слой `EnvironmentBack`, расстановка по комнатам
  без повторов.
- Исходники — `design/levels/midjourney/`, результат — в `Assets/Game/Art/` с записью в `ART_ASSET_LIST.md`.
