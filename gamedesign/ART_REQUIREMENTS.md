# Что нужно от арта — бриф по итогам PHASE 15

Этот документ — итог фиксации геймплейной архитектуры. Всё перечисленное ниже
**уже существует в коде** как состояние, событие или хук; не хватает только
графики и звука. Ничего из этого не требует менять геймплей.

Составлено 2026-09-20, после PHASE 15. Числа — `TUNING.md`
(источник правды — ассеты `Assets/Game/Data`).

## Как это устроено технически

Ни одно из состояний ниже не «угадывается» аниматором по скорости или позиции.
Каждое либо читается напрямую (`PlayerCharacter.Action`, `PlayerMotor2D.State`,
`WallMovement.State`, `EnemyController.State`, `BossController.State`,
`ShadowAbilities.Action`), либо приходит событием. Это значит, что аниматор
можно собрать как чистое отображение состояния, без собственной логики.

События, на которые вешается графика и звук:

| Система | События |
| --- | --- |
| Игрок | `ActionChanged`, `DashStarted`, `PerfectDodge`, `Parried`, `Blocked`, `GuardBroken`, `Stunned`, `Died`, `ReadyToRespawn` |
| Стены | `WallMovement.StateChanged` |
| Тень | `ShadowAbilities.Started`, `Ended` |
| Умения | `ActiveSkillController.SkillStarted`, `SkillEnded`, `SlotChanged` |
| Магия | `SpellCaster.CastStarted`, `SpellReleased`, `CastInterrupted`; `SpellProjectile.Hit` |
| Статусы | `StatusEffects.BuildupChanged`, `Applied`, `Ticked`, `Removed` |
| Бонусы | `TemporaryBuffs.BuffsChanged` |
| Бой | `CombatActor.DamageTaken`, `AttackDodged`, `AttackParried`, `Killed` |
| Прогресс | `ExperienceGained`, `LevelChanged`, `StatChanged`, `SkillUnlocked`, `SpellLearned`, `ItemAdded`, `CurrencyChanged`, `LoadoutChanged` |

## Состояния анимации игрока

Базовые: `Idle`, `Run`, `Jump`, `Fall`, `Land`.

Движение по стенам: `WallSlide`, `WallRun`, `WallJump`.

Рывки: `Dash`, `AirDash`, `ShadowDash`.

Бой: `Attack1`, `Attack2`, `Attack3`, `Heavy`, `Block`, `Parry`, `GuardBroken`,
`Hurt`, `Stunned`, `Death`.

Умения и магия: `Cast`, `GroundBreaker`, `DashStrike`, `ShadowStrike`,
`WeaponSwitch`.

Отдельно: анимации не должны диктовать тайминги — окна замаха, урона и
восстановления приходят из данных атаки (`AttackDefinition`), а скорость
масштабируется множителем оружия. Анимация подстраивается под данные, не
наоборот.

## Состояния анимации врагов

`Idle`, `Patrol`, `Alert`, `Chase`, `ReturnToPatrol`, `Search`,
`Attack` (по одному варианту на каждую атаку из `EnemyDefinition`),
`Guard`, `GuardImpact`, `GuardBroken`, `Hurt`, `Stunned`, `Death`.

Реакции на статусы — наложения поверх любого состояния, а не отдельные
состояния: горит, отравлен, заморожен, кровоточит.

## Состояния анимации босса

Страж Гробниці: `Dormant`, `Intro`, `Idle`, `Walk`, `Slash`, `ShieldBash`,
`Charge`, `Combo` (три удара), `Recoil`, `PhaseTransition`, `JumpSlam`,
`Stunned`, `Death`.

Вторая фаза отличается отсутствием щита, темпом и наличием `JumpSlam` —
визуально это должно читаться сразу.

## Нужные VFX

**Удары:** обычный, тяжёлый, критический, тёмный (Shadow), попадание по щиту.

**Защита:** блок, парирование, поломка блока, оглушение (poise).

**Движение:** рывок, воздушный рывок, скольжение по стене, бег по стене,
след теневого рывка, исчезновение и появление Shadow Strike.

**Магия:** снаряд огня и его попадание, снаряд льда и его попадание,
Вибух Сили, Мановий Щит.

**Статусы:** накопление и всплеск кровотечения, аура яда, пламя горения,
лёд замедления. Плюс слабая обратная связь на сопротивление — что удар «не
зашёл» так, как ожидалось.

**Мир:** подбор добычи с оттенком по редкости, зажжение алтаря, открытие
барьера арены, переход фазы босса, ударная волна.

## Что ещё не имеет визуала вообще

Интерфейс: HUD и экран инвентаря/снаряжения существуют как HTML-прототипы
(`design/ui/hud/hud.html`, `design/ui/inventory/inventory.html`) и **ждут
согласования**; дерево умений, книга заклинаний, полоса маны, панель заклинаний
и индикаторы статусов не нарисованы даже в прототипе. Это первое, что нужно
добавить в прототип перед переносом в Unity.
