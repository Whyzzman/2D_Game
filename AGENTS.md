# Тінь Забутого Королівства — правила разработки

Обзор игры, запуск, клавиши, устройство проекта — [`README.md`](README.md). Claude Code получает этот файл через
`CLAUDE.md`. Читать: этот файл → [общие правила](.agents/global-agent-rules/AGENTS.md) →
[стиль C#](.agents/global-agent-rules/code-style.md) при работе с C# →
[UI-пайплайн](.agents/global-agent-rules/pipelines/unity-ui-toolkit-interface-pipeline.md)
при работе с интерфейсом → документ затрагиваемой функции (`gamedesign/DEVELOPMENT_PLAN.md`,
таблица «Где что описано»). Пути — от корня проекта. Явное указание пользователя важнее
локальных правил, локальные — важнее общих.

## Работа с пользователем

- Отвечать по-украински; название игры украинское. Документация — по-русски
  (документы, написанные по-украински, остаются украинскими), код — по-английски.
- Списки улучшений — по одному пункту, в порядке пользователя (план — `gamedesign/DEVELOPMENT_PLAN.md`).
- **Постоянное согласие (2026-10-19): все решения принимать самому, не спрашивать** — макеты (Approval 1),
  сюжетные тексты, механики, баланс, арт, следующий шаг. Опора — `WORLD_DESIGN.md`, `STORY.md`,
  `VISUAL_BIBLE.md`; каждое решение записать в коммит и документ функции («решено Claude по постоянному
  согласию»), чтобы пользователь проверил всё в конце.
- Ориентир ощущения — Hollow Knight (движение, камера, ритм боя); внешне игра на него не похожа
  (`gamedesign/VISUAL_BIBLE.md`).
- Вид: зоны — по картам пользователя `gamedesign/reference/maps/` (комнаты, связи, вертикаль, иконки,
  палитра); детали уровней решать самому. Процедурный арт и макеты — **плавные формы** (кривые, слои
  глубины, туман, мягкий свет, органичные края), не прямоугольники и ломаные; **ничего не повторять** из
  комнаты в комнату (свой seed, свой мотив) — проверять скриншотом.
- Проверенную работу коммитить (один коммит на пункт) и пушить в `origin/main` без вопросов.
  Непроверенное и сломанное в `main` не коммитить.
- Детальную проверку и плейтест пользователь делает в конце разработки — не ждать его отзыва.
- Пока тесты идут в фоне — делать следующий шаг (вне `Assets/Game`, если идёт сборка с копированием назад).
- Работа в облаке (без Unity) — `gamedesign/CLOUD_WORK.md`.
- Большие классы (600+ строк: `EnemyController`, `PlayerCharacter`, `BossController`,
  `ContentBuilder`) дробить попутно: правя такой класс, выносить затронутую связную часть
  (образец — `EnemySenses`). Отдельных рефакторингов «ради структуры» не делать.

## Проект

- Unity **6000.5.10f1**, URP 17.6 2D (`Assets/Settings/UniversalRP.asset`, `Renderer2D.asset`),
  Input System 1.20 (`Assets/Settings/InputSystem_Actions.inputactions`), Test Framework 1.7,
  UI Toolkit. Git: `main`, `origin` = https://github.com/Whyzzman/2D_Game.git.
- Код в `Assets/Game`, сборки `Game.Runtime`, `Game.Editor`, `Game.Tests.EditMode`,
  `Game.Tests.PlayMode`. Слои: Player 8, Ground 9, Enemy 10, PlayerAttack 11, EnemyAttack 12.
  `Light2D` требует ссылки на `Unity.RenderPipelines.Universal.2D.Runtime` в asmdef.
- Сцены: `MainMenu` (первая), `TombTrial` — сцена мира (игрок, камера, службы), уровни зон —
  `Scenes/Zones/Zone_*`, их грузит `ZoneStreamer` (`gamedesign/WORLD_SCENES.md`); `CoreMovement`.
  Сцены грузить **по имени**; тест грузит мир через `TestWorld.Load()`. Весь мир для правки —
  меню `Forgotten Kingdom/Scene/Open Whole World`. Службы сцены мира, ищущие объекты уровня,
  обновляют списки по `ZoneStreamer.ZonesChanged`; объект зоны находит службы сам (ссылки
  между сценами не сохраняются).
- Документы разработки — в `gamedesign/`, вне `Assets/`. Текущее состояние и открытые
  задачи — `gamedesign/DEVELOPMENT_PLAN.md`; история — `gamedesign/archive/`.

## Архитектурные правила

- **Анимация читает геймплей, никогда наоборот**: клип подгоняется под `AttackDefinition`, ИИ
  не называет клипы, спрайт не задаёт хитбокс.
- **Боевой фидбек** — только `CombatFeedbackService` (событие удара — `CombatActor.DamageTaken`).
  Не звать `Shake()` / `HitStop.Request()` из боевого кода. Профили `Data/Feedback` — только
  подача, без урона и дистанций.
- **Звук** — только `AudioManager.Play(AudioId.X, …)`: геймплей называет момент, не файл.
  Музыка процедурная (`PlaceholderMusicArt`), в сцены её вписывает `SceneMusic.Apply`.
- **`Time.timeScale`** пишет только `GameTimeScale` (`SetPaused` / `SetFrozen` / `SetBaseScale`).
- **Интерфейс**: новый экран — сначала HTML в `design/ui/` на согласование (Approval 1), потом
  UXML/USS/C# в `Assets/Game/UI/` (Approval 2; у перенесённых экранов он предварительный до
  финального плейтеста). Просмотр HTML: `node design/ui/_shared/serve.js` →
  http://localhost:8123/. Все экраны документа HUD делят одно пространство USS-классов — новому
  экрану свой префикс. Правила — `gamedesign/UI_UX_GUIDE.md`.
- **Мир** спрашивают через `WorldState` / `GameProgress` (`IWorldQuery`), не через сохранение.
- **Настройки** (`GameSettings`) — отдельный файл от прогресса; новая игра их не стирает.
- **Отладочное** в боевом уровне закрывать `DeveloperOnly` и проверять тестом.
- **Клавиши**: у каждой переназначаемой действия вторая клавиша клавиатуры — пустая привязка со
  стабильным id; новое действие — её и строку в `KeyBindings.Rows`. Тесты, меняющие клавиши,
  зовут `KeyBindings.ResetAll()`.
- **Enum, хранимые числом** (`FeedbackKind`, `AudioId`, `PlayerAnimationState` и подобные) —
  новые значения только в конец.
- **Высоты уровня** — от дискретного прыжка на 50 Гц: 1,97 (второй — ≈3,43); ступень
  основного пути ≤ 1,7. Уступы `=` односторонние (`OneWayPlatform`): уступ, закрывающий ворота способности,
  делать камнем `#`, иначе в ворота впрыгнут снизу (`LevelCheck` это ловит). Проходимость держит `TheWholeMainPathIsWalkableEastAndBackWithoutAnyAbility`.
- **Кадры игрока** 256×224, 100 PPU, pivot у стоп (128, 0), нарисованы лицом вправо; мотор
  зеркалит `Visual` через scale.x.
- **Персонажи — lit-спрайты** (`ContentBuilder.LitSpriteMaterial`) с нормалями (`CharacterNormals`,
  вызывается в `Build`); свет им даёт свой `CharacterFill` на слое `Characters`, глобальный свет
  зоны этот слой не трогает. Новый персонаж — так же, иначе он чёрный или плоский.

## Контент и генератор

- `ContentBuilder.Build` генерирует арт, звук, данные и префабы; существующие сцены **не
  трогает** — сцены источник истины уровня, правки делать в сцене (в генераторе — лишь
  рецепт). `BuildWithScenes` стирает ручные правки сцен: только для восстановления.
- Сценные правки — редакторские `Scene*.Apply` (`SceneAmbience`, `SceneMusic`,
  `SceneForestLook`, `SceneScreenGrade`, `SceneCharacterLight`, `SceneNpcs`, `SceneTombLook`): трогают только своё, повторный запуск безопасен; `BuildAtmosphere` зовёт
  тот же рецепт. Правящие уровень идут через `WorldScenes.Edit` (собрать мир → правка →
  разрезать по зонам). Сменил их арт — прогнать `Apply` после `Build` (второй `executeMethod` цикла).
- Новые зоны (и перестроенные по картам) пишутся текстом в `Assets/Game/Levels/*.txt`, проверяются
  `LevelCheck` и рисуются `LevelTools.RenderPreviews` до сборки (`gamedesign/LEVEL_TOOL.md`). Гробница
  Лес и Село уже такие: сцену из текста собирают `SceneTomb.Apply` / `SceneForest.Apply` /
  `SceneVillage.Apply` (после `Build`), тесты берут места из текста (`TombScene.Spot`, `ForestScene.Spot`),
  не числами. Новая зона — ещё `ProjectSettings/EditorBuildSettings.asset` из копии (сцена зоны). Картинки комнат карты — `ExportMapRooms` → `tools/map-art/render.sh
  rooms` → `RefreshMapArt`. Нарисованная зона (Гробница): сменил текст — `node tools/level-art/render.js <зона>`
  → `RefreshLevelArt` → `Scene<Зона>.Apply` (`LEVEL_TOOL.md`, «Нарисованный уровень»).
- `Build` пересоздаёт дочерние объекты префабов врагов с новыми fileID — переопределения
  сцены на них пропадут; настраивать через генератор.
- Арт и звук пишет генератор и перезаписывает при каждой сборке — кроме рисованного: файл, чей путь
  стоит в `Assets/Game/Art/Painted.txt`, генератор не трогает. Рисованный PNG — на место плейсхолдера
  плюс строка в списке (`tools/_shared/painted.py`; `gamedesign/ART_ASSET_LIST.md` §1). Второй копии
  арта в проекте нет.
- Сменился смысл сериализованного поля в незакоммиченном префабе — откатить префаб
  (`git checkout`), чтобы генератор собрал его заново, иначе останется старое значение.
- Ловушки Unity: `ScriptableObject`-ассет — только в файле со своим именем (иначе грузится
  `null` без ошибок); ссылки на ассеты, взятые до `EditorSceneManager.NewScene`, мертвы — грузить
  после; `Rect` не читается `JsonUtility` — писать `SerializedProperty.rectValue`; `uniqueID`
  слоя сортировки беззнаковый — писать `longValue`, хеш маскировать в 31 бит.
- Файлы — UTF-8 без BOM (кириллица).

## Проверка

- **Быстрый режим (просьба пользователя, 2026-10-24): игру надо закончить быстро.** Править прямо в
  основном проекте — открытый Editor пользователя сам перекомпилирует, и результат сразу виден.
  В живом Editor через Unity CLI (`unity command`) можно звать генератор и `Scene*.Apply` и читать
  консоль; в Play Mode не входить, второй Editor не запускать. Каждый пункт: правка → нет ошибок
  компиляции в консоли → скриншот результата пользователю (`SendUserFile`) → коммит и push.
- Тесты — не на каждый пункт: полный EditMode + PlayMode на копии в фоне раз на **8** пунктов (и
  перед вливанием больших веток), найденное — отдельным коммитом, работу не останавливать.
- Тесты и билды — Unity в batchmode на копии `Artifacts/Verification/Project`:
  `/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/MacOS/Unity`. Все пути
  (`-projectPath`, `-logFile`, `-testResults`) — в двойных кавычках (в пути пробелы и кириллица).
- Цикл: `rsync -a --delete Assets/Game/ <копия>/Assets/Game/` (слеш в конце обязателен) →
  `-executeMethod ForgottenKingdom.EditorTools.ContentBuilder.Build` → `rsync -a` обратно
  **до** следующей синхронизации (иначе `--delete` сотрёт сгенерированное — дважды так теряли
  контент) → откатить шум генератора (`git checkout` незатронутых префабов врагов и
  `Data/Skills/hunter_efficiency.asset`, `Prefabs/LostCurrencyMark.prefab`) → снова синхронизировать → тесты. Пока цикл идёт, не
  править `Assets/Game` и не запускать второй. `Assets/Settings` и `ProjectSettings/EditorBuildSettings.asset` (сцены зон) копировать отдельно.
  Сменились кадры персонажей — `Build` один раз идёт ~40 мин (атлас перепаковывается на каждый
  импорт); тогда префабы врагов — не шум, не откатывать.
- **Windows** (с 2026-11): Unity CLI `unity test --mode EditMode|PlayMode --filter … --output <xml> <копия>`;
  синхронизация — `robocopy Assets\Game <копия>\Assets\Game /MIR /XF Zz*.cs Zz*.cs.meta` (тесты `Zz*` живут
  только в копии), полный цикл без `Build` — `tools/tests/cycle.sh <метка>` (сцены `ZzApplyScenes` → EditMode →
  PlayMode, провалы — `tools/tests/failures.py`); сцены и `Art/CharacterNormals` потом скопировать обратно.
- PlayMode-тесты ждут по `Time.realtimeSinceStartup`, не по числу кадров. Под нагрузкой кадр
  бывает 0,1 с: бот, решающий раз в кадр, ставит `Time.maximumDeltaTime = fixedDeltaTime`;
  плавность мерить скоростью (путь / dt), не шагом за кадр. Нестабильность воспроизводить
  медленными кадрами (`Thread.Sleep` в `Update` временного компонента), а не повторами.
- Скриншоты — временные тесты `Zz*Capture.cs` только в копии, затем удалить. Скриншот не
  доказывает взаимодействие.
- zsh: `rm -f шаблон*` без совпадений рвёт цепочку `&&` — использовать `find … -delete`.
- Сборки игры: `BuildTools.BuildMac` (Apple Silicon) на копии; модуля Windows на этом Mac нет.
- **Место на диске** (просьба пользователя): перед каждым полным PlayMode и когда
  `du -sh Artifacts/Verification/Project/Library` > 10 ГБ (Unity на копии не запущен) — переносить в
  Корзину (`~/.Trash`, не `rm`) `Library` копии, старые сборки и логи `Artifacts/`. Первый цикл после
  этого — полный импорт. Сказать пользователю, что Корзину очищает он.
- Скрипты цикла держать не в scratchpad сессии: он стирается между сессиями.
- Отчёт: кратко — результат, проверки (выполнено / неприменимо / недоступно с причиной),
  ограничения. Успех сборки или тестов — только по результату запуска.

## Развитие инструкций

Держать этот файл коротким: факты и правила, без истории. Новое проверенное ограничение или
договорённость — сюда; поведение игры — в документ функции; общие приёмы — в
`.agents/global-agent-rules/`. Устаревшее удалять, дубли объединять.
