using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom
{
    /// <summary>
    /// Development-only readout of the combat state plus a quick level restart. It is off by
    /// default and is not part of the game interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerCombat _combat;
        [SerializeField] private PlayerDefense _defense;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private PlayerCombatStatus _combatStatus;
        [SerializeField] private BossEncounter _bossEncounter;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private LootService _loot;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private InventoryScreen _screen;
        [SerializeField] private PlayerProgressionService _progression;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private ActiveSkillController _activeSkills;
        [SerializeField] private TemporaryBuffs _buffs;
        [SerializeField] private PlayerSpellbook _spellbook;
        [SerializeField] private SpellCaster _caster;
        [SerializeField] private Mana _mana;
        [Tooltip("Skill the debug unlock key grants.")]
        [SerializeField] private string _debugSkillId = "warrior_training";
        [Tooltip("Item handed over by the debug give key.")]
        [SerializeField] private string _debugItemId = "iron_scrap";
        [Tooltip("Attribute the debug allocate key spends a point on.")]
        [SerializeField] private CharacterStat _debugStat = CharacterStat.Strength;
        [Tooltip("Everything the equipment debug key hands over at once.")]
        [SerializeField] private string[] _testEquipment =
        {
            "old_sword", "iron_greatsword", "traveler_hood", "traveler_coat",
            "guard_chestplate", "ring_of_life", "ring_of_endurance", "iron_talisman",
            "ring_of_might"
        };
        [Tooltip("F1 toggles the readout at runtime, F5 restarts the level.")]
        [SerializeField] private bool _isVisible = false;
        [SerializeField] private bool _logDamage = false;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly System.Collections.Generic.List<string> _buffLines =
            new System.Collections.Generic.List<string>();
        private EnemyController[] _enemies = System.Array.Empty<EnemyController>();
        private TrainingDummy[] _dummies = System.Array.Empty<TrainingDummy>();
        private TrainingAttacker[] _attackers = System.Array.Empty<TrainingAttacker>();
        private string _lastDamage = string.Empty;
        private GUIStyle _style;

        private void Start()
        {
            _enemies = FindObjectsByType<EnemyController>();
            _dummies = FindObjectsByType<TrainingDummy>();
            _attackers = FindObjectsByType<TrainingAttacker>();
            if (_player != null)
            {
                _player.Actor.DamageTaken += OnPlayerDamaged;
            }
        }

        private void OnEnable()
        {
            // Until the real inventory is drawn, this readout is how a developer build shows it.
            _screen?.AddPresenter(this);
        }

        private void OnDisable()
        {
            DebugPanels.Report(this, false);
            _screen?.RemovePresenter(this);
        }

        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.Actor.DamageTaken -= OnPlayerDamaged;
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame && DebugPanels.MayToggle(this))
            {
                _isVisible = !_isVisible;
                DebugPanels.Report(this, _isVisible);
            }

            // Every action below — F2 to F12, the digits and the rest — works only while this
            // panel is open, so none of them can go off from a gameplay key.
            if (!_isVisible)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                GameTimeScale.Reset();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (keyboard.f2Key.wasPressedThisFrame && _respawner != null)
            {
                // Softlock escape: send the player back to the last altar without dying.
                _respawner.RespawnPlayer();
            }

            if (keyboard.f3Key.wasPressedThisFrame && _respawner != null)
            {
                _respawner.ResetWorld(WorldResetReason.Debug);
            }

            if (keyboard.f4Key.wasPressedThisFrame && _respawner != null)
            {
                _respawner.Kill();
            }

            HandleSaveKeys(keyboard);

            var boss = _bossEncounter != null ? _bossEncounter.Boss : null;
            if (boss == null)
            {
                return;
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                // Skip straight to phase two by taking the boss past its threshold.
                boss.Actor.Health.SetCurrent(boss.Actor.Health.Max * 0.45f);
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                boss.Actor.ClearInvulnerability();
                boss.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.left,
                    isUnblockable: true, attacker: _player != null ? _player.gameObject : null));
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                boss.ResetBoss();
            }
        }

        private void HandleSaveKeys(Keyboard keyboard)
        {
            if (_progress == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                _progress.SaveNow();
                Debug.Log($"Saved to {SaveSystem.FilePath}");
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                _progress.LoadGame();
                _respawner?.ApplySavedCheckpoint();
                Debug.Log(DescribeSave());
            }

            if (keyboard.f11Key.wasPressedThisFrame)
            {
                SaveSystem.Delete();
                Debug.Log("Save deleted.");
            }

            if (keyboard.f12Key.wasPressedThisFrame)
            {
                _respawner?.StartNewGame();
                Debug.Log("New game started.");
            }

            HandleLootKeys(keyboard);
        }

        private void HandleLootKeys(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                _progress.AddCurrency(100);
                Debug.Log($"Gave 100 gold; total {_progress.Currency}.");
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                var result = _progress.AddItem(_debugItemId);
                Debug.Log($"Gave '{_debugItemId}': {result}; "
                    + $"total {_progress.GetItemAmount(_debugItemId)}.");
            }

            if (keyboard.digit3Key.wasPressedThisFrame && _loot != null && _player != null)
            {
                _loot.Spawn(new DropResult(_debugItemId, false, 1),
                    (Vector2)_player.transform.position + new Vector2(1.5f, 0.5f));
                Debug.Log($"Spawned a '{_debugItemId}' pickup next to the player.");
            }

            if (keyboard.digit4Key.wasPressedThisFrame && _loot != null)
            {
                RollTableSample();
            }

            HandleEquipmentKeys(keyboard);
        }

        private void HandleEquipmentKeys(Keyboard keyboard)
        {
            if (keyboard.digit5Key.wasPressedThisFrame)
            {
                GiveAllTestEquipment();
            }

            if (keyboard.digit6Key.wasPressedThisFrame && _equipment != null)
            {
                var result = _equipment.Equip(_debugItemId);
                Debug.Log($"Equip '{_debugItemId}': {result}.");
            }

            if (keyboard.digit7Key.wasPressedThisFrame)
            {
                ClearEquipment();
            }

            if (keyboard.digit8Key.wasPressedThisFrame)
            {
                PrintDerivedStats();
            }

            if (keyboard.digit9Key.wasPressedThisFrame && _screen != null)
            {
                _screen.Toggle();
                Debug.Log($"Inventory screen {(_screen.IsOpen ? "opened" : "closed")}.");
            }

            HandleProgressionKeys(keyboard);
        }

        private void HandleProgressionKeys(Keyboard keyboard)
        {
            if (_progression == null)
            {
                return;
            }

            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                var levels = _progression.AddExperience(500, "debug");
                Debug.Log($"Gave 500 XP; {levels} level(s) gained, now level "
                    + $"{_progression.Level} with {_progression.StatPoints} point(s).");
            }

            if (keyboard.minusKey.wasPressedThisFrame)
            {
                _progression.GrantStatPoints(10);
                Debug.Log($"Gave 10 stat points; {_progression.StatPoints} unspent.");
            }

            if (keyboard.equalsKey.wasPressedThisFrame)
            {
                _progression.AllocatePoint(_debugStat);
                Debug.Log($"Put a point into {_debugStat}: now "
                    + $"{_stats?.GetFinalStat(_debugStat)}.");
            }

            if (keyboard.backslashKey.wasPressedThisFrame)
            {
                PrintDamageBreakdown();
            }

            HandleSkillKeys(keyboard);
        }

        private void HandleSkillKeys(Keyboard keyboard)
        {
            if (_skills == null)
            {
                return;
            }

            if (keyboard.leftBracketKey.wasPressedThisFrame)
            {
                _skills.GrantBonusPoints(5);
                Debug.Log($"Gave 5 skill points; {_skills.AvailableSkillPoints} available.");
            }

            if (keyboard.rightBracketKey.wasPressedThisFrame)
            {
                _skills.DebugUnlock(_debugSkillId);
                Debug.Log($"Unlocked '{_debugSkillId}': rank {_skills.RankOf(_debugSkillId)}.");
            }

            if (keyboard.semicolonKey.wasPressedThisFrame)
            {
                _skills.DebugUnlockAll();
                Debug.Log("Unlocked every skill.");
            }

            if (keyboard.quoteKey.wasPressedThisFrame)
            {
                _skills.DebugReset();
                _activeSkills?.LoadSlots();
                Debug.Log("Forgot every skill.");
            }

            if (keyboard.commaKey.wasPressedThisFrame)
            {
                _activeSkills?.ResetCooldowns();
                _caster?.ResetCasting();
                Debug.Log("Skill and spell cooldowns reset.");
            }

            if (keyboard.periodKey.wasPressedThisFrame && _spellbook?.Database != null)
            {
                foreach (var spell in _spellbook.Database.Spells)
                {
                    _spellbook.Learn(spell.SpellId);
                }

                for (var slot = 0; slot < PlayerSpellbook.SlotCount
                    && slot < _spellbook.Database.Spells.Count; slot++)
                {
                    _spellbook.AssignSlot(slot, _spellbook.Database.Spells[slot].SpellId);
                }

                Debug.Log("Learned every spell and filled the spell bar.");
            }

            if (keyboard.slashKey.wasPressedThisFrame)
            {
                _mana?.RestoreToFull();
                PrintSpellBreakdown();
            }
        }

        /// <summary>
        /// Prints the spell in the first slot end to end: base, what intelligence adds, what the
        /// tree multiplies it by, and what each enemy's resistance would leave of it.
        /// </summary>
        public void PrintSpellBreakdown()
        {
            var spell = _spellbook?.SlotSpell(0);
            if (spell == null || _caster == null)
            {
                return;
            }

            var breakdown = _caster.Evaluate(spell);
            var report = $"{spell.DisplayName} [{ResistanceProfile.Display(spell.Element)}]: "
                + $"{breakdown}, mana {_caster.ManaCostOf(spell):0.#}, "
                + $"cast {_caster.CastTimeOf(spell):0.##}s, crit {_caster.SpellCritChance:0.##}";
            foreach (var enemy in _enemies)
            {
                var resistances = enemy != null ? enemy.Actor.Resistances : null;
                if (resistances != null)
                {
                    report += $"\n  vs {enemy.name}: x{resistances.Multiplier(spell.Element):0.##}"
                        + $" = {breakdown.Total * resistances.Multiplier(spell.Element):0.#}";
                }
            }

            Debug.Log(report);
        }

        /// <summary>
        /// Prints a swing end to end: what the weapon gives, what the attributes add, what the
        /// attack multiplies it by and what armour would leave of it. The fastest way to see
        /// whether a build is doing what the numbers promise.
        /// </summary>
        public void PrintDamageBreakdown()
        {
            var weapon = _equipment?.ActiveWeaponDefinition;
            if (weapon == null || _stats == null)
            {
                return;
            }

            var breakdown = _stats.WeaponDamage;
            var light = weapon.LightAttack(0);
            var heavy = weapon.HeavyAttack;
            Debug.Log($"{weapon.DisplayName}: {breakdown}\n"
                + $"  light 1 x{light?.DamageMultiplier:0.##} = "
                + $"{WeaponDamageCalculator.AttackDamage(breakdown, light):0.#}\n"
                + $"  heavy x{heavy?.DamageMultiplier:0.##} = "
                + $"{WeaponDamageCalculator.AttackDamage(breakdown, heavy):0.#}\n"
                + $"  own defense {_stats.PhysicalDefense:0.#} would leave "
                + $"{_stats.Mitigate(new DamageInfo(WeaponDamageCalculator.AttackDamage(breakdown, light), Vector2.right)):0.#}");
        }

        /// <summary>Hands over every test weapon, armour and trinket so a loadout can be tried.</summary>
        public void GiveAllTestEquipment()
        {
            if (_progress == null)
            {
                return;
            }

            foreach (var itemId in _testEquipment)
            {
                _progress.AddItem(itemId);
            }

            Debug.Log($"Gave {_testEquipment.Length} test items.");
        }

        /// <summary>Strips everything but the main hand, which has no unarmed moveset to fall to.</summary>
        public void ClearEquipment()
        {
            if (_equipment == null)
            {
                return;
            }

            foreach (var slot in EquipmentSlots.All)
            {
                if (slot != EquipmentSlot.MainWeapon)
                {
                    _equipment.Unequip(slot);
                }
            }

            Debug.Log("Equipment cleared except the main weapon.");
        }

        public void PrintDerivedStats()
        {
            if (_stats == null)
            {
                return;
            }

            if (_progression != null)
            {
                Debug.Log($"Level {_progression.Level}, XP {_progression.Experience}/"
                    + $"{_progression.ExperienceForNextLevel}, "
                    + $"{_progression.StatPoints} unspent, "
                    + $"STR {_stats.GetFinalStat(CharacterStat.Strength)} "
                    + $"DEX {_stats.GetFinalStat(CharacterStat.Dexterity)} "
                    + $"VIT {_stats.GetFinalStat(CharacterStat.Vitality)} "
                    + $"END {_stats.GetFinalStat(CharacterStat.Endurance)}.");
            }

            var weapon = _equipment?.ActiveWeaponDefinition;
            Debug.Log($"HP {_stats.MaxHealth:0.#}, stamina {_stats.MaxStamina:0.#}, "
                + $"defense {_stats.PhysicalDefense:0.#}, "
                + $"stagger resist {_stats.StaggerResistance:0.##}, "
                + $"weight {_stats.EquipmentWeight:0.#}, "
                + $"weapon {(weapon != null ? weapon.DisplayName : "none")} "
                + $"base {(weapon != null ? weapon.BaseDamage : 0f):0.#}, "
                + $"{_stats.ActiveModifiers.Count} modifiers.");
        }

        /// <summary>What is worn, what it adds up to, and what the weapon in hand is worth.</summary>
        private void AppendEquipment()
        {
            if (_equipment == null)
            {
                return;
            }

            var weapon = _equipment.ActiveWeaponDefinition;
            _text.Append($"weapon: {(weapon != null ? weapon.DisplayName : "none")}");
            if (weapon != null)
            {
                _text.Append($" [{weapon.WeaponType}] base {weapon.BaseDamage:0.#}"
                    + $" speed {weapon.AttackSpeed:0.##} stagger {weapon.StaggerMultiplier:0.##}"
                    + $" combo {_combat?.ComboIndex}");
            }

            _text.AppendLine($"   {(_equipment.IsSecondaryActive ? "secondary" : "main")}"
                + $"   second: {SlotText(EquipmentSlot.SecondaryWeapon)}");
            _text.AppendLine($"worn: head {SlotText(EquipmentSlot.Head)}"
                + $"  chest {SlotText(EquipmentSlot.Chest)}"
                + $"  gloves {SlotText(EquipmentSlot.Gloves)}"
                + $"  boots {SlotText(EquipmentSlot.Boots)}"
                + $"  acc {SlotText(EquipmentSlot.Accessory1)}/"
                + $"{SlotText(EquipmentSlot.Accessory2)}");
            if (_stats != null)
            {
                _text.AppendLine($"derived: def {_stats.PhysicalDefense:0.#}"
                    + $"  poise resist {_stats.StaggerResistance:0.##}"
                    + $"  weight {_stats.EquipmentWeight:0.#}/{_stats.MaxEquipLoad:0.#}"
                    + $"  attack {_stats.ActiveWeaponAttack:0.#}"
                    + (_stats.MeetsWeaponRequirements ? string.Empty : " REQ UNMET")
                    + $"  modifiers {_stats.ActiveModifiers.Count}");
            }

            if (_progression != null && _stats != null)
            {
                _text.AppendLine($"level {_progression.Level}"
                    + $"  xp {_progression.Experience}/{_progression.ExperienceForNextLevel}"
                    + $"  points {_progression.StatPoints}"
                    + $"  STR {_stats.GetFinalStat(CharacterStat.Strength)}"
                    + $"  DEX {_stats.GetFinalStat(CharacterStat.Dexterity)}"
                    + $"  VIT {_stats.GetFinalStat(CharacterStat.Vitality)}"
                    + $"  END {_stats.GetFinalStat(CharacterStat.Endurance)}");
            }

            AppendSkills();
            if (_screen != null && _screen.IsOpen)
            {
                _text.AppendLine($"inventory open: {InventoryScreen.CategoryName(_screen.ActiveTab)}"
                    + $"  {_screen.Entries.Count} entries  selected '{_screen.SelectedItemId}'");
            }
        }

        /// <summary>What has been learned, what is on the bar and what is temporarily true.</summary>
        private void AppendSkills()
        {
            if (_skills == null)
            {
                return;
            }

            _text.Append($"skill points: {_skills.AvailableSkillPoints}"
                + $" (earned {_skills.EarnedSkillPoints}, spent {_skills.SpentSkillPoints})"
                + $"  learned {_skills.Ranks.Count}");
            _text.AppendLine(_skills.CanSpendPoints ? string.Empty : "  [in combat]");
            foreach (var pair in _skills.Ranks)
            {
                _text.Append($" {pair.Key}");
                if (pair.Value > 1)
                {
                    _text.Append($":{pair.Value}");
                }
            }

            _text.AppendLine();
            if (_activeSkills != null)
            {
                for (var slot = 0; slot < ActiveSkillController.SlotCount; slot++)
                {
                    var skill = _activeSkills.SlotSkill(slot);
                    var cooldown = _activeSkills.CooldownRemaining(slot);
                    _text.Append($"slot {slot + 1}: {(skill != null ? skill.SkillId : "-")}"
                        + (cooldown > 0f ? $" ({cooldown:0.0}s)" : " ready") + "   ");
                }

                _text.AppendLine();
            }

            if (_buffs != null && _buffs.ActiveCount > 0)
            {
                _buffLines.Clear();
                _buffs.CollectActive(_buffLines);
                _text.AppendLine($"buffs: {string.Join(", ", _buffLines)}");
            }
        }

        /// <summary>What is on the spell bar, what it costs and whether it is ready.</summary>
        private void AppendSpells()
        {
            if (_spellbook == null || _caster == null)
            {
                return;
            }

            _text.Append("spells:");
            for (var slot = 0; slot < PlayerSpellbook.SlotCount; slot++)
            {
                var spell = _spellbook.SlotSpell(slot);
                if (spell == null)
                {
                    _text.Append($"  {slot + 1}: -");
                    continue;
                }

                var cooldown = _caster.CooldownRemaining(slot);
                _text.Append($"  {slot + 1}: {spell.SpellId}"
                    + $" ({_caster.ManaCostOf(spell):0} mana"
                    + (cooldown > 0f ? $", {cooldown:0.0}s)" : ", ready)"));
            }

            _text.AppendLine(_caster.IsCasting
                ? $"   casting {_caster.CastingSpell.SpellId}"
                : string.Empty);
        }

        private string SlotText(EquipmentSlot slot)
        {
            var itemId = _equipment.GetEquipped(slot);
            return string.IsNullOrEmpty(itemId) ? "-" : itemId;
        }

        /// <summary>Rolls one enemy's table many times and prints the spread; a balance aid.</summary>
        private void RollTableSample()
        {
            var dropper = FindAnyObjectByType<LootDropper>();
            if (dropper == null || dropper.DropTable == null)
            {
                Debug.Log("No drop table to sample.");
                return;
            }

            var totals = new System.Collections.Generic.Dictionary<string, int>();
            var rolls = 0;
            for (var i = 0; i < 1000; i++)
            {
                foreach (var drop in _loot.Roll(dropper.DropTable))
                {
                    var key = drop.IsCurrency ? "gold" : drop.ItemId;
                    totals.TryGetValue(key, out var current);
                    totals[key] = current + drop.Amount;
                    rolls++;
                }
            }

            var report = $"1000 rolls of {dropper.DropTable.name}: {rolls} drops";
            foreach (var pair in totals)
            {
                report += $"; {pair.Key} {pair.Value} total ({pair.Value / 1000f:0.00} per kill)";
            }

            Debug.Log(report);
        }

        private string DescribeSave()
        {
            var data = _progress.Data;
            return $"Save v{data.version}  checkpoint '{data.checkpointId}'  "
                + $"altars [{string.Join(", ", data.activatedCheckpoints)}]  "
                + $"bosses [{string.Join(", ", data.defeatedBosses)}]  "
                + $"rewards [{string.Join(", ", data.acquiredRewards)}]  "
                + $"playtime {data.totalPlayTimeSeconds:0}s";
        }

        private void OnGUI()
        {
            if (!_isVisible || _player == null)
            {
                return;
            }

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = 14, richText = false };
            _text.Clear();
            if (_respawner != null)
            {
                _text.AppendLine($"checkpoint: "
                    + (string.IsNullOrEmpty(_respawner.ActiveCheckpointId)
                        ? "(default spawn)" : _respawner.ActiveCheckpointId)
                    + $"   deaths: {_respawner.DeathCount}"
                    + $"   in combat: {_combatStatus?.IsInCombat}"
                    + $"   enemies: {_respawner.Enemies?.Count}");
            }

            if (_progress != null)
            {
                _text.AppendLine($"save: v{_progress.Data.version}  "
                    + $"altars {_progress.ActivatedCheckpoints.Count}  "
                    + $"bosses {_progress.DefeatedBosses.Count}  "
                    + $"rewards {_progress.AcquiredRewards.Count}  "
                    + $"on disk {_progress.HasSaveOnDisk}");
                _text.Append($"gold: {_progress.Currency}   items:");
                foreach (var stack in _progress.Inventory.Stacks)
                {
                    _text.Append($" {stack.itemId} x{stack.amount}");
                }

                if (_progress.Inventory.DistinctItemCount == 0)
                {
                    _text.Append(" (none)");
                }

                _text.AppendLine($"   loot on ground: {_loot?.TransientPickupCount}");
            }

            AppendEquipment();

            var prompt = _interactor != null ? _interactor.CurrentPrompt : string.Empty;
            if (!string.IsNullOrEmpty(prompt))
            {
                _text.AppendLine($"[E] {prompt}");
            }

            _text.AppendLine($"action: {_player.Action}   move: {_motor?.State}");
            _text.AppendLine($"grounded: {_motor?.IsGrounded}   dash: {_motor?.IsDashing} "
                + $"ready: {_motor?.IsDashReady}   air dashes used: {_motor?.AirDashesUsed}");
            _text.AppendLine(
                $"i-frames: {_player.Actor.IsInvulnerable}   "
                + $"dodge window: {_player.Actor.Invulnerability?.IsActive(ImmunitySource.Dodge)}");
            _text.AppendLine(
                $"hp: {_player.Health.Current:0}/{_player.Health.Max:0}   "
                + $"stamina: {_player.Stamina?.Current:0}/{_player.Stamina?.Max:0}   "
                + $"mana: {_mana?.Current:0}/{_mana?.Max:0}   "
                + $"poise: {_player.Stagger?.Current:0}/{_player.Stagger?.Threshold:0}");
            AppendSpells();
            if (_defense != null)
            {
                _text.AppendLine($"guard: {_defense.Phase}   blocking: {_defense.IsBlocking}");
            }

            if (_combat != null)
            {
                var attack = _combat.CurrentAttack;
                _text.AppendLine(
                    $"combo: {_combat.ComboIndex + 1}   attack: {attack?.DisplayName ?? "-"}   "
                    + $"chain: {_combat.CanChain}   cancel: {_combat.IsCancelable}");
            }

            if (!string.IsNullOrEmpty(_lastDamage))
            {
                _text.AppendLine($"last hit on player: {_lastDamage}");
            }

            foreach (var dummy in _dummies)
            {
                if (dummy != null)
                {
                    _text.AppendLine($"{dummy.name}: hits {dummy.HitCount}, "
                        + $"damage {dummy.TotalDamageTaken:0}, "
                        + $"poise {dummy.Actor.Stagger?.Current:0}"
                        + (dummy.Actor.Stagger != null && dummy.Actor.Stagger.IsStunned
                            ? " STUNNED" : string.Empty));
                }
            }

            foreach (var attacker in _attackers)
            {
                if (attacker == null)
                {
                    continue;
                }

                var attack = attacker.CurrentAttack;
                var flags = attack == null
                    ? "idle"
                    : $"{attack.DisplayName} blockable:{!attack.IsUnblockable} "
                        + $"parryable:{attack.IsParryable}";
                _text.AppendLine($"{attacker.name}: {flags}   "
                    + $"poise {attacker.Actor.Stagger?.Current:0}"
                    + (attacker.Actor.Stagger != null && attacker.Actor.Stagger.IsStunned
                        ? " STUNNED" : string.Empty));
            }

            foreach (var enemy in _enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                _text.Append($"{enemy.name}: {enemy.State}  "
                    + $"hp {enemy.Actor.Health.Current:0}  "
                    + $"poise {enemy.Actor.Stagger?.Current:0}  "
                    + $"cooldown {enemy.AttackCooldownRemaining:0.0}");
                if (enemy.Guard != null)
                {
                    _text.Append($"  guard {enemy.Guard.CurrentGuard:0}/{enemy.Guard.MaxGuard:0}"
                        + (enemy.Guard.IsGuarding ? " UP" : string.Empty)
                        + (enemy.Guard.IsBroken ? " BROKEN" : string.Empty)
                        + $"  pressure {enemy.Guard.Pressure:0.0}");
                    for (var i = 0; i < enemy.Definition.AttackCount; i++)
                    {
                        _text.Append($"  {enemy.Definition.Attack(i).Kind.ToString()[..4]}"
                            + $" {enemy.OptionCooldownRemaining(i):0.0}");
                    }
                }

                _text.AppendLine();
            }

            AppendBoss();
            GUI.Label(new Rect(12f, 12f, 700f, 500f), _text.ToString(), _style);
            DrawEnemyStateLabels();
        }

        private void AppendBoss()
        {
            var boss = _bossEncounter != null ? _bossEncounter.Boss : null;
            if (boss == null)
            {
                return;
            }

            _text.AppendLine($"boss: {boss.DisplayName}  {boss.State}  "
                + $"phase {boss.Phase}/{boss.TotalPhases}  "
                + $"hp {boss.Actor.Health.Current:0}/{boss.Actor.Health.Max:0}  "
                + $"poise {boss.Actor.Stagger?.Current:0}/{boss.Actor.Stagger?.Threshold:0}"
                + (boss.Actor.Stagger != null && boss.Actor.Stagger.IsImmune ? " IMMUNE" : string.Empty));
            _text.AppendLine($"  last: {boss.LastAttack?.ToString() ?? "-"}  "
                + $"distance {boss.DistanceToPlayer:0.0}  "
                + $"invulnerable {boss.Actor.IsInvulnerable}  "
                + $"arena {(_bossEncounter.IsFightActive ? "ACTIVE" : "idle")}  "
                + $"defeated {_bossEncounter.IsBossDefeated}");
        }

        /// <summary>Puts each enemy's current state over its head, which is the fastest way to
        /// read the AI while playing.</summary>
        private void DrawEnemyStateLabels()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            foreach (var enemy in _enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var head = enemy.transform.position + Vector3.up * 1.7f;
                var screen = camera.WorldToScreenPoint(head);
                if (screen.z <= 0f)
                {
                    continue;
                }

                GUI.Label(new Rect(screen.x - 60f, Screen.height - screen.y, 120f, 20f),
                    enemy.State.ToString(), _style);
            }
        }

        private void OnPlayerDamaged(DamageInfo info, DamageResult result)
        {
            _lastDamage = $"{result.Outcome} {result.AppliedDamage:0} ({info.Type})";
            if (_logDamage)
            {
                Debug.Log($"Player {result.Outcome} for {result.AppliedDamage:0} from "
                    + (info.Attacker != null ? info.Attacker.name : "unknown"));
            }
        }
    }
}
