# Список VFX — виробниче завдання

Зафіксовано 2026-09-21. Супутній документ до `VISUAL_BIBLE.md`.

**Головне правило.** Якщо ефект перекриває силует гравця, замах ворога, снаряд
або небезпеку — програє ефект. Читабельність вища за видовищність.

**Кожен ефект уже має подію в коді.** Нічого нового чіпляти не треба —
стовпець «Тригер» вказує реальну подію або стан.

**Стан на 2026-09-21 (PHASE 20): інтегровано.** 29 профілів фідбеку, 218 кадрів
ефектів, централізований `CombatFeedbackService`, трус камери, hit-stop,
спалахи, імпульси світла, сліди зброї й накладки станів. Усе — процедурні
плейсхолдери. Значення, пріоритети, доступність і відомі обмеження —
`COMBAT_FEEDBACK.md`.

| Блок | Стан |
| --- | --- |
| фізичний бій (удар / блок / парирування / злам / рівновага) | інтегровано, плейсхолдер |
| магія (вогонь / лід / аркана) | інтегровано, плейсхолдер |
| Тінь (ривок, удар, темна шкода) | інтегровано, плейсхолдер |
| стани (кровотеча / отрута / горіння / лід) | інтегровано, плейсхолдер |
| бос (ривок, стрибок-удар, зміна фази, смерть) | інтегровано, плейсхолдер |
| світ (чекпойнт, бар'єр, підбирання) | частково — чекпойнт і бар'єр із PHASE 19 |
| камера, hit-stop, доступність | **готово, не плейсхолдер** |
| damage numbers, HUD-реакції, віньєтка | **не зроблено** — чекають UI-фази |

Шари: `VFXBack` — під персонажами, `VFXFront` — над ними (див. Visual Bible §10).

**Сокети готові (PHASE 17–18).** `VisualSockets` на гравцеві, обох ворогах і
Стражі дає точки `Body`, `Ground`, `Weapon`, `Cast`, `Overhead`, а на
щитоносцях — ще `Shield`. Ефект бере позицію звідти, а не з власного зсуву.
Сокет щита Стража **зникає разом зі щитом у фазі 2 і повертається на reset** —
це той самий об'єкт, яким бос уже керує, тож правило живе в одному місці.
Відсутній сокет не кидає виняток: щит на зомбі падає назад на `Body`.

**Події анімації.** `StateChanged`, `ClipCompleted`, `FrameChanged` на всіх трьох
контролерах, плюс `PhaseTransitionStarted` на босі. Геймплей від них не
залежить — активні вікна й далі веде `AttackRunner` за даними.

**Оточення готове приймати ефекти (PHASE 19).** Шари `VFXBack` / `VFXFront`
заведені в `TagManager`. Точки, де ефекти вже мають місце:

| Хук | Де |
| --- | --- |
| полум'я вівтаря | `CheckpointVisual`, ореол `env_tomb_glow_warm` |
| запалення вівтаря | `Checkpoint.Activated` |
| печать арени | `BossEncounter` вмикає бар'єр |
| зняття печаті | той самий об'єкт після перемоги |
| освітлення фази 2 | `BossController.PhaseChanged` / `BossAnimationController.PhaseTransitionStarted` |
| пил і туман | шари `BackgroundNear` / `Foreground` |

Спрайтового світла достатньо для оточення (Visual Bible §139); справжніх
2D-джерел у проєкті немає і в цій фазі не додавалося.

---

## Бій — відгук на удар

| Ім'я | Тригер | Шар | Трив. | Пріор. | Вимоги до читабельності |
| --- | --- | --- | --- | --- | --- |
| `vfx_hit_light` | `Hitbox.Hit`, легка атака | VFXFront | 0.15 с | **P0** | малий спалах і іскри, не закриває ворога |
| `vfx_hit_heavy` | `Hitbox.Hit`, важка | VFXFront | 0.25 с | **P0** | більший удар, пил, метал |
| `vfx_hit_critical` | `PlayerOffense.CriticalLanded` | VFXFront | 0.30 с | P1 | окремий акцент, **не** просто більший розмір |
| `vfx_hit_dark` | попадання з `DamageType.Dark` | VFXFront | 0.25 с | P1 | темне ядро, фіолетовий кант |
| `vfx_block` | `PlayerDefense.BlockedHit` | VFXFront | 0.20 с | **P0** | іскри по щиту, спрямовані |
| `vfx_parry` | `PlayerDefense.Parried` | VFXFront | 0.35 с | **P0** | сильний спрямований спалах — головний відгук гри |
| `vfx_guard_break` | `GuardBroken` | VFXFront | 0.50 с | **P0** | велика тріщина щита |
| `vfx_stagger` | `StaggerMeter.Staggered` | VFXFront | 0.40 с | P1 | сильно, але коротко |
| `vfx_hurt_flash` | `CombatActor.DamageTaken` | — | 0.10 с | **P0** | **короткий** спалах, не довге блимання |

## Рух

| Ім'я | Тригер | Шар | Трив. | Пріор. | Нотатки |
| --- | --- | --- | --- | --- | --- |
| `vfx_dash` | `PlayerCharacter.DashStarted` | VFXBack | 0.18 с | **P0** | смуга руху, пил зі стоп |
| `vfx_air_dash` | ривок у повітрі | VFXBack | 0.18 с | P1 | без пилу, інша форма |
| `vfx_dust_wall` (було `vfx_wall_slide`) | `WallMovement.State` = Slide, кожні 0,11 с | VFXBack | 0.25 с | P2 | пил зі стіни, віддзеркалюється від неї — **зроблено** |
| `vfx_wall_kick` (було `vfx_wall_jump`) | → Jump | VFXBack | 0.23 с | P2 | поштовх від стіни — **зроблено** |
| `vfx_dust_jump` | `PlayerMotor2D.Jumped` (відрив від землі) | VFXBack | 0.23 с | P1 | пил в обидва боки від стоп — **зроблено** |
| `vfx_dust_land` | `PlayerAnimationController.Landed`, падіння ≥ 3 од/с | VFXBack | 0.30 с | P1 | розмір від сили приземлення — **зроблено** |
| `vfx_dust_kick` | старт бігу з місця й розворот на швидкості | VFXBack | 0.25 с | P1 | пил позаду, на рівному бігу немає — **зроблено** |
| `vfx_air_jump` | `PlayerMotor2D.AirJumped` | VFXBack | 0.21 с | P2 | бліде кільце під ногами — **зроблено** |
| `vfx_shadow_dash_start` | `ShadowAbilities.Started` (Dash) | VFXFront | 0.10 с | P1 | розчинення в дим |
| `vfx_shadow_dash_trail` | під час ривка Тіні | VFXBack | цикл | P1 | **окремий ассет**, не запечений у спрайт |
| `vfx_shadow_dash_end` | `ShadowAbilities.Ended` | VFXFront | 0.12 с | P1 | збирання з тіні |
| `vfx_shadow_strike` | `Started` (Strike) | VFXFront | 0.30 с | P1 | зникнення й поява |

## Магія

| Ім'я | Тригер | Шар | Трив. | Пріор. | Нотатки |
| --- | --- | --- | --- | --- | --- |
| `vfx_firebolt_projectile` | `SpellCaster.SpellReleased` | Projectiles | політ | **P0** | тепле ядро, темний дим; колір із `SpellDefinition.Color` |
| `vfx_firebolt_impact` | `SpellProjectile.Hit` | VFXFront | 0.25 с | **P0** | |
| `vfx_frostshard_projectile` | те саме | Projectiles | політ | **P0** | бліда кристалічна форма |
| `vfx_frostshard_impact` | те саме | VFXFront | 0.25 с | **P0** | |
| `vfx_arcane_burst` | випуск Вибуху Сили | VFXFront | 0.50 с | P1 | геометрія й руни, радіус **3.2 юніта** — має збігатися |
| `vfx_mana_shield` | `TemporaryBuffs.BuffsChanged` | VFXBack | цикл | P2 | стримана оболонка, не закриває гравця |
| `vfx_cast_charge` | `CastStarted` | VFXFront | цикл | P1 | ✅ 2026-09-27: `vfx_cast_orb` (1 кадр) — `CastCharge` масштабує, фарбує кольором заклинання й пульсує |
| `vfx_cast_interrupt` | `CastInterrupted` | VFXFront | 0.20 с | P2 | ✅ 2026-09-27: `vfx_cast_fizzle` (8 кадрів), блідий, фарбується кольором заклинання |

**Реакції ворогів (2026-09-27):** `vfx_enemy_telegraph` — відблиск на зброї на старті замаху
(0,25 с, VFXFront, зі світлом); `vfx_stun_stars` — петля зірочок над головою оглушеного ворога чи
боса. Подробиці — `COMBAT_FEEDBACK.md` §12в.

**Бос і арена (2026-09-27):** `vfx_boss_danger` — червоне кільце перед ривком і стрибком;
`vfx_arena_seal` / `vfx_arena_open` — закриття й злам печаті; `vfx_arena_runes` — петля рун на
закритому бар'єрі. Відповідають `vfx_boss_charge_warning`, `vfx_barrier_seal`, `vfx_barrier_open`,
`vfx_boss_rune_glow` із таблиць нижче. Подробиці — `COMBAT_FEEDBACK.md` §12г.

## Статуси

Усі чотири мають **різні силуети**, не лише кольори.

| Ім'я | Тригер | Шар | Трив. | Пріор. | Нотатки |
| --- | --- | --- | --- | --- | --- |
| `vfx_bleed_buildup` | `StatusEffects.BuildupChanged` | VFXFront | миттєвий | P1 | наростання, стримано |
| `vfx_bleed_proc` | `Applied` (Bleed) | VFXFront | 0.30 с | P1 | **короткий** сплеск, без надмірної крові |
| `vfx_poison_active` | `Applied`/`Removed` (Poison) | VFXBack | цикл | P1 | рідкі зелені частинки |
| `vfx_burn_active` | `Applied`/`Removed` (Burn) | VFXFront | цикл | P1 | локальне полум'я на корпусі |
| `vfx_frost_active` | `Applied`/`Removed` (Frost) | VFXFront | цикл | P1 | крижаний кант, помітне сповільнення |
| `vfx_status_tick` | `Ticked` | VFXFront | 0.10 с | P2 | дрібний, не спамити числами |
| `vfx_status_resist` | висока стійкість цілі | VFXFront | 0.15 с | P2 | тихий натяк, **без великого «RESIST»** |

**Заборонено:** фарбувати весь спрайт у зелений/червоний/синій на час статусу.
Тільки локальні накладки, частинки й кант.

## Бос

| Ім'я | Тригер | Шар | Трив. | Пріор. |
| --- | --- | --- | --- | --- |
| `vfx_boss_charge_warning` | початок ривка | VFXFront | 0.50 с | **P0** |
| `vfx_boss_slam` | `SlamImpact` | VFXFront | 0.40 с | **P0** |
| `vfx_boss_shockwave` | `Shockwave.Play` | VFXFront | 0.35 с | **P0** |
| `vfx_boss_phase_transition` | `BossController` фаза 2 | VFXFront | 2.00 с | **P0** |
| `vfx_boss_rune_glow` | під час бою | VFXBack | цикл | P1 |
| `vfx_boss_death` | `Defeated` | VFXFront | 2.50 с | **P0** |

Перехід фази: тріщини броні, витік темної енергії, пил, короткий імпульс.
**Без різкого перефарбовування всього екрана.**

## Світ

| Ім'я | Тригер | Шар | Трив. | Пріор. |
| --- | --- | --- | --- | --- |
| `vfx_checkpoint_idle` | `Checkpoint` запалений | VFXBack | цикл | **P0** |
| `vfx_checkpoint_activate` | `CheckpointActivated` | VFXFront | 0.80 с | **P0** | ✅ 2026-09-27: `vfx_altar_kindle` (12 кадрів, VFXBack) |
| `vfx_loot_common` / `_rare` / `_unique` | `WorldPickup` існує | VFXBack | цикл | P1 |
| `vfx_loot_pickup` | `WorldPickup.Collected` | VFXFront | 0.30 с | P1 |
| `vfx_barrier_seal` | `BossEncounter` запечатує арену | VFXFront | 0.60 с | **P0** |
| `vfx_barrier_open` | арена відкривається | VFXFront | 0.60 с | **P0** |
| `vfx_level_up` | `LevelChanged` | VFXFront | 1.00 с | P2 |
| `env_dust` / `env_fog` / `env_embers` | оточення | VFXBack | цикл | P1 |

Фонові частинки **ніколи не рендеряться над HUD**, а бойові снаряди завжди
читаються над оточенням.

---

## Технічні правила

- **VFX — окремі ассети.** Слід меча, слід Тіні, снаряди не запікаються в
  анімацію персонажа: інакше їх не перефарбувати й не перевикористати.
- **Візуал снаряда ≠ колайдер.** Габарити беруться з даних закляття.
- **Спільні матеріали.** Не робити власний шейдер під кожного ворога.
- **Запасний шлях.** Якщо шейдер (розчинення, спотворення) недоступний — має
  бути простий спрайтовий варіант.
- **Тінь ніколи не чисто чорна** — інакше зникне на темному фоні.
- **Безпека:** без частих повноекранних спалахів; тряска має класи
  малий / середній / великий і лишається вимикною.

## Мінімум для першого візуального зрізу (P0)

`vfx_hit_light`, `vfx_hit_heavy`, `vfx_block`, `vfx_parry`, `vfx_guard_break`,
`vfx_hurt_flash`, `vfx_dash`, `vfx_firebolt_projectile` + `_impact`,
`vfx_frostshard_projectile` + `_impact`, `vfx_boss_slam`, `vfx_boss_shockwave`,
`vfx_boss_phase_transition`, `vfx_boss_death`, `vfx_checkpoint_idle` +
`_activate`, `vfx_barrier_seal` + `_open`.

**17 ефектів** — цього достатньо, щоб бій читався.
