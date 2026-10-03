# Тінь Забутого Королівства — правила разработки

Читать: этот файл → [общие правила](.agents/global-agent-rules/AGENTS.md) →
[стиль C#](.agents/global-agent-rules/code-style.md) при работе с C# →
[UI-пайплайн](.agents/global-agent-rules/pipelines/unity-ui-toolkit-interface-pipeline.md)
при работе с интерфейсом → документ затрагиваемой функции (`gamedesign/DEVELOPMENT_PLAN.md`,
таблица «Где что описано»). Пути — от корня проекта. Явное указание пользователя важнее
локальных правил, локальные — важнее общих.

## Работа с пользователем

- Отвечать по-украински; название игры украинское. Документация — по-русски
  (документы, написанные по-украински, остаются украинскими), код — по-английски.
- Списки улучшений — строго по одному пункту, в порядке пользователя; следующий — только по
  его команде. Неясно, как делать, или не хватает информации — **спросить** (AskUserQuestion,
  рекомендуемый вариант первым), а не выбирать самому. Сюжет, механики, баланс и арт не решать
  за пользователя.
- Ориентир ощущения — Hollow Knight (движение, камера, ритм боя); внешне игра на него не похожа
  (`gamedesign/VISUAL_BIBLE.md`).
- Проверенную работу коммитить (один коммит на пункт) и пушить в `origin/main` без вопросов.
  Непроверенное и сломанное не коммитить.
- Детальную проверку и плейтест пользователь делает в конце разработки — не ждать его отзыва.
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
  основного пути ≤ 1,7. Проходимость держит `TheWholeMainPathIsWalkableEastAndBackWithoutAnyAbility`.
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
  уже такая: сцену из текста собирает `SceneTomb.Apply` (после `Build`), тесты берут места из текста
  (`TombScene.Spot`), не числами. Картинки комнат карты — `ExportMapRooms` → `tools/map-art/render.sh
  rooms` → `RefreshMapArt`.
- `Build` пересоздаёт дочерние объекты префабов врагов с новыми fileID — переопределения
  сцены на них пропадут; настраивать через генератор.
- Весь арт и звук — процедурные плейсхолдеры, генератор перезаписывает их при каждой сборке.
  Рисованный арт класть в `Assets/Game/Art/Source/Final/<тот же путь>` (манифест —
  `gamedesign/ART_ASSET_LIST.md`).
- Сменился смысл сериализованного поля в незакоммиченном префабе — откатить префаб
  (`git checkout`), чтобы генератор собрал его заново, иначе останется старое значение.
- Ловушки Unity: `ScriptableObject`-ассет — только в файле со своим именем (иначе грузится
  `null` без ошибок); ссылки на ассеты, взятые до `EditorSceneManager.NewScene`, мертвы — грузить
  после; `Rect` не читается `JsonUtility` — писать `SerializedProperty.rectValue`; `uniqueID`
  слоя сортировки беззнаковый — писать `longValue`, хеш маскировать в 31 бит.
- Файлы — UTF-8 без BOM (кириллица).

## Проверка

- Живой Editor пользователя (открыт на основном проекте) — **только чтение** через Unity CLI
  (`unity status` / `unity command`). Менять в нём что-либо, входить в Play Mode — только по
  прямой просьбе. Второй Editor на основном проекте не запускать.
- Сборка контента, тесты и билды — Unity в batchmode на копии `Artifacts/Verification/Project`:
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
- Объём: после каждого пункта — весь EditMode и связанные PlayMode-наборы (`-testFilter`), затем
  коммит и push. Полный PlayMode (~10 мин) — раз на **8** пунктов; найденное — отдельным
  коммитом. В отчёте — какие наборы шли и сколько пунктов до полного прогона.
- PlayMode-тесты ждут по `Time.realtimeSinceStartup`, не по числу кадров. Под нагрузкой кадр
  бывает 0,1 с: бот, решающий раз в кадр, ставит `Time.maximumDeltaTime = fixedDeltaTime`;
  плавность мерить скоростью (путь / dt), не шагом за кадр. Нестабильность воспроизводить
  медленными кадрами (`Thread.Sleep` в `Update` временного компонента), а не повторами.
- Скриншоты — временные тесты `Zz*Capture.cs` только в копии, затем удалить. Скриншот не
  доказывает взаимодействие.
- zsh: `rm -f шаблон*` без совпадений рвёт цепочку `&&` — использовать `find … -delete`.
- Сборки игры: `BuildTools.BuildMac` (Apple Silicon) на копии; модуля Windows на этом Mac нет.
- **Место на диске** (просьба пользователя, 2026-09-28): кэш импорта копии растёт на каждой
  перегенерации кадров (за ~20 циклов — 38 ГБ). Перед каждым полным PlayMode-прогоном (раз на 8
  пунктов) и всякий раз, когда `du -sh Artifacts/Verification/Project/Library` > 10 ГБ, — пока на
  копии не запущен Unity — переносить в Корзину (`~/.Trash`, не `rm`) `Library` копии и старые
  сборки (`Artifacts/Build*`, `Verification/{Build,MacBuild,TestPlayer}`), оставив последнюю
  нужную. Первый цикл после этого дольше (полный импорт). Сказать пользователю, что Корзину
  очищает он.
- Отчёт: кратко — результат, проверки (выполнено / неприменимо / недоступно с причиной),
  ограничения. Успех сборки или тестов — только по результату запуска.

## Развитие инструкций

Держать этот файл коротким: факты и правила, без истории. Новое проверенное ограничение или
договорённость — сюда; поведение игры — в документ функции; общие приёмы — в
`.agents/global-agent-rules/`. Устаревшее удалять, дубли объединять.
