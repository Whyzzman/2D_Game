using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>One row the screen draws: an owned item and what the player may do with it.</summary>
    public readonly struct InventoryEntry
    {
        public readonly string ItemId;
        public readonly ItemDefinition Definition;
        public readonly int Amount;
        public readonly bool IsEquipped;
        public readonly bool CanEquip;

        public InventoryEntry(string itemId, ItemDefinition definition, int amount,
            bool isEquipped, bool canEquip)
        {
            ItemId = itemId;
            Definition = definition;
            Amount = amount;
            IsEquipped = isEquipped;
            CanEquip = canEquip;
        }

        /// <summary>Stacks show a count; a single sword does not need "x1" next to it.</summary>
        public string AmountText => Definition != null && Definition.IsStackable && Amount > 1
            ? $"x{Amount}"
            : string.Empty;
    }

    /// <summary>
    /// The inventory and equipment screen's behaviour: opening, pausing, which tab is showing,
    /// what is selected and what equipping does. It holds no widgets on purpose — the visual layer
    /// reads this and draws it, so the same rules are testable without a single pixel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryScreen : MonoBehaviour
    {
        /// <summary>The tabs, in the order they appear. Currency is not an item and is not listed.</summary>
        public static readonly ItemCategory[] Tabs =
        {
            ItemCategory.Weapon, ItemCategory.Armor, ItemCategory.Accessory,
            ItemCategory.Consumable, ItemCategory.Material, ItemCategory.KeyItem
        };

        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerProgressionService _progression;
        [SerializeField] private PlayerStats _stats;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private ActiveSkillController _activeSkills;
        [SerializeField] private PlayerSpellbook _spellbook;
        [SerializeField] private SpellCaster _caster;
        [Tooltip("Pauses the world while the screen is open; recommended for single player.")]
        [SerializeField] private bool _pausesGame = true;
        private readonly List<InventoryEntry> _entries = new List<InventoryEntry>();
        private readonly HashSet<object> _presenters = new HashSet<object>();
        private EquipmentLoadout _loadoutOnOpen;
        private int _statPointsOnOpen;
        private int _tabIndex;
        private string _selectedItemId = string.Empty;

        public event Action Opened;
        public event Action Closed;

        /// <summary>Raised whenever the list or the selection changes; the screen redraws once.</summary>
        public event Action ContentChanged;
        public event Action<string> SelectionChanged;

        public bool IsOpen { get; private set; }
        public ItemCategory ActiveTab => Tabs[_tabIndex];
        public int TabIndex => _tabIndex;
        public string SelectedItemId => _selectedItemId;
        public IReadOnlyList<InventoryEntry> Entries => _entries;
        public bool IsEmptyTab => _entries.Count == 0;
        public int Currency => _progress != null ? _progress.Currency : 0;
        public PlayerEquipment Equipment => _equipment;

        private void Awake()
        {
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_equipment == null && _player != null) { _equipment = _player.Equipment; }
            if (_input == null && _player != null)
            {
                _input = _player.GetComponent<PlayerInputReader>();
            }
            if (_progression == null) { _progression = FindAnyObjectByType<PlayerProgressionService>(); }
            if (_stats == null && _player != null) { _stats = _player.Stats; }
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
            if (_activeSkills == null && _player != null)
            {
                _activeSkills = _player.GetComponent<ActiveSkillController>();
            }
            if (_spellbook == null) { _spellbook = FindAnyObjectByType<PlayerSpellbook>(); }
            if (_caster == null && _player != null)
            {
                _caster = _player.GetComponent<SpellCaster>();
            }
        }

        private void OnEnable()
        {
            if (_input != null) { _input.InventoryToggled += OnTogglePressed; }
            if (_progress != null) { _progress.ItemAdded += OnItemChanged; }
            if (_equipment != null) { _equipment.EquipmentChanged += Rebuild; }
        }

        private void OnDisable()
        {
            if (_input != null) { _input.InventoryToggled -= OnTogglePressed; }
            if (_progress != null) { _progress.ItemAdded -= OnItemChanged; }
            if (_equipment != null) { _equipment.EquipmentChanged -= Rebuild; }
            // Leaving with the screen up would strand the world paused and the player deaf.
            if (IsOpen)
            {
                Close();
            }
        }

        /// <summary>
        /// True when something draws this screen. The key opens it only then: with nothing on
        /// screen a keypress would freeze the world behind an invisible menu. A release build has
        /// no drawing yet, so the key does nothing there until the real screen registers itself.
        /// </summary>
        public bool HasPresenter => _presenters.Count > 0;

        public void AddPresenter(object presenter)
        {
            if (presenter != null) { _presenters.Add(presenter); }
        }

        public void RemovePresenter(object presenter)
        {
            _presenters.Remove(presenter);
            if (!HasPresenter)
            {
                Close();
            }
        }

        /// <summary>The player's key. Closing always works; opening needs something to draw it.</summary>
        private void OnTogglePressed()
        {
            if (IsOpen || HasPresenter)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            // The menu is not an escape hatch: it cannot be opened out of a death or a stun, and
            // not over another screen (the pause, the map, an ability card) that holds the game.
            if (IsOpen || _player == null || !_player.IsAlive || GamePause.IsPaused)
            {
                return;
            }

            IsOpen = true;
            _loadoutOnOpen = _equipment != null ? _equipment.Loadout.Clone() : null;
            _statPointsOnOpen = _progression != null ? _progression.StatPoints : 0;
            _input?.SetGameplayEnabled(false);
            if (_pausesGame)
            {
                GamePause.Push();
            }

            Rebuild();
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            if (_pausesGame)
            {
                GamePause.Pop();
            }

            _input?.SetGameplayEnabled(true);
            // One write for a whole visit to the screen, and only when something really changed:
            // the loadout, or attribute points spent (skills save themselves on purchase).
            var loadoutChanged = _equipment != null && !_equipment.Loadout.SameAs(_loadoutOnOpen);
            var pointsSpent = _progression != null && _progression.StatPoints != _statPointsOnOpen;
            if (_progress != null && (loadoutChanged || pointsSpent))
            {
                _progress.Save();
            }

            Closed?.Invoke();
        }

        // ---------- tabs and selection ----------

        public void SelectTab(int index)
        {
            var wrapped = ((index % Tabs.Length) + Tabs.Length) % Tabs.Length;
            if (wrapped == _tabIndex)
            {
                return;
            }

            _tabIndex = wrapped;
            _selectedItemId = string.Empty;
            Rebuild();
        }

        public void SelectTab(ItemCategory category)
        {
            for (var i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i] == category)
                {
                    SelectTab(i);
                    return;
                }
            }
        }

        public void NextTab() => SelectTab(_tabIndex + 1);
        public void PreviousTab() => SelectTab(_tabIndex - 1);

        public void Select(string itemId)
        {
            if (_selectedItemId == itemId)
            {
                return;
            }

            _selectedItemId = itemId ?? string.Empty;
            SelectionChanged?.Invoke(_selectedItemId);
            ContentChanged?.Invoke();
        }

        /// <summary>Moves the selection by one; how a keyboard or a stick walks the list.</summary>
        public void MoveSelection(int step)
        {
            if (_entries.Count == 0)
            {
                return;
            }

            var current = IndexOf(_selectedItemId);
            var next = current < 0 ? 0 : current + step;
            next = ((next % _entries.Count) + _entries.Count) % _entries.Count;
            Select(_entries[next].ItemId);
        }

        public InventoryEntry? SelectedEntry
        {
            get
            {
                var index = IndexOf(_selectedItemId);
                return index >= 0 ? _entries[index] : (InventoryEntry?)null;
            }
        }

        private int IndexOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return -1;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].ItemId == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        // ---------- acting on the selection ----------

        /// <summary>Why the selected item cannot be used right now, or null when it can.</summary>
        public string WhyCannotUseSelected()
        {
            var item = SelectedEntry?.Definition;
            if (item == null || !item.IsUsable)
            {
                return "Цей предмет не використовується";
            }

            return _player != null ? _player.WhyCannotHeal() : "Немає гравця";
        }

        /// <summary>
        /// Uses the selected healing item: the screen closes and the player eats it in the open,
        /// as a flask is drunk — no healing on a paused screen. The item is spent once the player
        /// has committed to it.
        /// </summary>
        public bool UseSelected()
        {
            var entry = SelectedEntry;
            if (entry == null || WhyCannotUseSelected() != null)
            {
                return false;
            }

            var itemId = entry.Value.ItemId;
            var amount = entry.Value.Definition.HealAmount;
            Close();
            if (!_player.TryUseHealingItem(amount))
            {
                return false;
            }

            if (_progress != null && !_progress.TryRemoveItem(itemId))
            {
                Debug.LogWarning($"Used '{itemId}' but it was no longer in the bag.");
            }

            return true;
        }

        public EquipResult EquipSelected()
        {
            return Equip(_selectedItemId);
        }

        public EquipResult Equip(string itemId)
        {
            if (_equipment == null)
            {
                return EquipResult.UnknownItem;
            }

            var result = _equipment.Equip(itemId);
            if (result == EquipResult.Equipped)
            {
                Rebuild();
            }

            return result;
        }

        public EquipResult Equip(string itemId, EquipmentSlot slot)
        {
            if (_equipment == null)
            {
                return EquipResult.UnknownItem;
            }

            var result = _equipment.Equip(itemId, slot);
            if (result == EquipResult.Equipped)
            {
                Rebuild();
            }

            return result;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            if (_equipment == null || !_equipment.Unequip(slot))
            {
                return false;
            }

            Rebuild();
            return true;
        }

        /// <summary>
        /// The selected item against what is already worn, as ready-made lines. Nothing here
        /// decides which is better; the numbers say it.
        /// </summary>
        public List<string> DescribeSelected()
        {
            var lines = new List<string>();
            var entry = SelectedEntry;
            if (entry == null || entry.Value.Definition == null)
            {
                return lines;
            }

            var definition = entry.Value.Definition;
            lines.Add(definition.DisplayName);
            lines.Add(RarityName(definition.Rarity));
            if (!string.IsNullOrWhiteSpace(definition.Description))
            {
                lines.Add(definition.Description);
            }

            definition.Equipment?.DescribeInto(lines);
            DescribeWeaponInHand(definition, lines);
            return lines;
        }

        /// <summary>
        /// What this weapon would be worth in the player's own hands, and whether they can lift
        /// it at all. Both come from the same calculator combat uses, so the panel cannot lie.
        /// </summary>
        private void DescribeWeaponInHand(ItemDefinition definition, List<string> lines)
        {
            var weapon = definition.Weapon;
            if (weapon == null || _stats == null || _progression == null)
            {
                return;
            }

            var strength = _stats.GetFinalStat(CharacterStat.Strength);
            var dexterity = _stats.GetFinalStat(CharacterStat.Dexterity);
            var meets = weapon.MeetsRequirements(strength, dexterity);
            var breakdown = WeaponDamageCalculator.Evaluate(weapon, _progression.Curve, strength,
                dexterity, 0f, meets);
            lines.Add($"Сила атаки {breakdown.ScaledBase:0.#}");
            if (weapon.HasRequirements && !meets)
            {
                lines.Add($"Не вистачає: сила {strength}/{weapon.StrengthRequirement}, "
                    + $"спритність {dexterity}/{weapon.DexterityRequirement}");
            }
        }

        /// <summary>
        /// A plain numeric comparison against the item in the slot this one would go into, so the
        /// player can see the trade before committing to it.
        /// </summary>
        public List<string> CompareSelected()
        {
            var lines = new List<string>();
            var entry = SelectedEntry;
            if (entry == null || _equipment == null || entry.Value.Definition?.Equipment == null)
            {
                return lines;
            }

            var slot = _equipment.DefaultSlotFor(entry.Value.ItemId);
            // A weapon headed for the empty second hand is still judged against the one in use;
            // comparing it to nothing would tell the player nothing.
            if (entry.Value.Definition.Weapon != null && _equipment.GetEquippedItem(slot) == null)
            {
                slot = _equipment.ActiveWeaponSlot;
            }

            var worn = _equipment.GetEquippedItem(slot);
            if (worn == null || worn.ItemId == entry.Value.ItemId)
            {
                return lines;
            }

            lines.Add($"{EquipmentSlots.Display(slot)}: {worn.DisplayName}");
            var candidate = entry.Value.Definition.Weapon;
            var current = worn.Weapon;
            if (candidate != null && current != null)
            {
                AddDelta(lines, "Урон", candidate.BaseDamage, current.BaseDamage);
                AddDelta(lines, "Швидкість", candidate.AttackSpeed, current.AttackSpeed);
                AddDelta(lines, "Оглушення", candidate.StaggerMultiplier,
                    current.StaggerMultiplier);
                AddDelta(lines, "Витривалість", candidate.StaminaMultiplier,
                    current.StaminaMultiplier);
                return lines;
            }

            var newArmor = entry.Value.Definition.Equipment as ArmorDefinition;
            var oldArmor = worn.Equipment as ArmorDefinition;
            if (newArmor != null && oldArmor != null)
            {
                AddDelta(lines, "Захист", newArmor.Defense, oldArmor.Defense);
                AddDelta(lines, "Вага", newArmor.Weight, oldArmor.Weight);
            }

            return lines;
        }

        private static void AddDelta(List<string> lines, string label, float candidate,
            float current)
        {
            var delta = candidate - current;
            if (Mathf.Abs(delta) < 0.001f)
            {
                lines.Add($"{label} — без змін");
                return;
            }

            lines.Add($"{label} {(delta > 0f ? "+" : string.Empty)}{delta:0.##}");
        }

        // ---------- the list itself ----------

        /// <summary>
        /// Rebuilt only when something actually changed, never per frame: the screen redraws from
        /// this list, and a list that is rebuilt every frame is a list that stutters.
        /// </summary>
        public void Rebuild()
        {
            _entries.Clear();
            var catalogue = _progress != null ? _progress.Items : null;
            if (catalogue == null || _progress == null)
            {
                ContentChanged?.Invoke();
                return;
            }

            var stacks = _progress.Inventory.GetItemsByCategory(catalogue, ActiveTab);
            stacks.Sort(CompareStacks);
            foreach (var stack in stacks)
            {
                var definition = catalogue.GetItem(stack.itemId);
                var isEquipped = _equipment != null && _equipment.IsEquipped(stack.itemId);
                var canEquip = _equipment != null && definition.IsEquippable
                    && _equipment.CanEquip(stack.itemId,
                        _equipment.DefaultSlotFor(stack.itemId));
                _entries.Add(new InventoryEntry(stack.itemId, definition, stack.amount,
                    isEquipped, canEquip));
            }

            if (IndexOf(_selectedItemId) < 0)
            {
                _selectedItemId = _entries.Count > 0 ? _entries[0].ItemId : string.Empty;
                SelectionChanged?.Invoke(_selectedItemId);
            }

            ContentChanged?.Invoke();
        }

        /// <summary>Rarest first, then by name, so the list does not reshuffle as things stack.</summary>
        private static int CompareStacks(ItemStack left, ItemStack right)
        {
            return string.CompareOrdinal(left.itemId, right.itemId);
        }

        private void OnItemChanged(string itemId, int amount, int total)
        {
            if (IsOpen)
            {
                Rebuild();
            }
        }

        // ---------- the character sheet ----------

        /// <summary>
        /// The character tab, as ready-made lines. It is a separate tab on this same screen
        /// rather than a second menu, so one button opens everything about the character.
        /// </summary>
        public List<string> DescribeCharacter()
        {
            var lines = new List<string>();
            if (_progression == null || _stats == null)
            {
                return lines;
            }

            lines.Add($"Рівень {_progression.Level}");
            lines.Add(_progression.IsMaxLevel
                ? "Досвід: максимум"
                : $"Досвід {_progression.Experience} / {_progression.ExperienceForNextLevel}");
            lines.Add($"Вільні очки: {_progression.StatPoints}");
            foreach (var stat in CharacterStats.Allocatable)
            {
                var bonus = _stats.GetEquipmentBonus(stat);
                lines.Add($"{CharacterStats.Display(stat)} {_stats.GetFinalStat(stat)}"
                    + $" ({_progression.GetBase(stat)}+{_progression.GetAllocated(stat)}"
                    + (bonus != 0 ? $"{(bonus > 0 ? "+" : string.Empty)}{bonus} споряд." : string.Empty)
                    + ")");
            }

            lines.Add($"Здоров'я {_stats.MaxHealth:0}");
            lines.Add($"Витривалість {_stats.MaxStamina:0}");
            lines.Add($"Мана {_stats.MaxMana:0}");
            lines.Add($"Захист {_stats.PhysicalDefense:0.#}");
            lines.Add($"Опір оглушенню {_stats.StaggerResistance * 100f:0}%");
            lines.Add($"Вага {_stats.EquipmentWeight:0.#} / {_stats.MaxEquipLoad:0.#}");
            lines.Add($"Сила атаки {_stats.ActiveWeaponAttack:0.#}"
                + (_stats.MeetsWeaponRequirements ? string.Empty : " (вимоги не виконано)"));
            return lines;
        }

        /// <summary>
        /// What one more point in this attribute would change, worked out rather than guessed:
        /// the same curve the game uses answers the question.
        /// </summary>
        public string PreviewAllocation(CharacterStat stat)
        {
            if (_progression?.Curve == null || _stats == null || !_progression.CanAllocate(stat))
            {
                return string.Empty;
            }

            var curve = _progression.Curve;
            var current = _stats.GetFinalStat(stat);
            var next = current + 1;
            switch (stat)
            {
                case CharacterStat.Vitality:
                    return $"Здоров'я {curve.HealthFor(current):0} → {curve.HealthFor(next):0}";
                case CharacterStat.Endurance:
                    return $"Витривалість {curve.StaminaFor(current):0} → {curve.StaminaFor(next):0}"
                        + $", вага {curve.EquipLoadFor(current):0} → {curve.EquipLoadFor(next):0}";
                case CharacterStat.Intelligence:
                    var spell = _caster != null ? _spellbook?.SlotSpell(0) : null;
                    var damage = spell != null
                        ? $", шкода {_caster.Evaluate(spell).Total:0.#} → "
                            + $"{MagicDamageCalculator.Evaluate(spell, curve, next, _caster.SkillMultiplierFor(spell)).Total:0.#}"
                        : string.Empty;
                    return $"Мана {curve.ManaFor(current):0} → {curve.ManaFor(next):0}{damage}";
                case CharacterStat.Will:
                    return $"Сила Тіні ×{curve.ShadowPowerFor(current):0.00} → ×{curve.ShadowPowerFor(next):0.00}";
                default:
                    var weapon = _equipment?.ActiveWeaponDefinition;
                    if (weapon == null)
                    {
                        return string.Empty;
                    }

                    var strength = _stats.GetFinalStat(CharacterStat.Strength);
                    var dexterity = _stats.GetFinalStat(CharacterStat.Dexterity);
                    var after = stat == CharacterStat.Strength
                        ? WeaponDamageCalculator.Evaluate(weapon, curve, strength + 1, dexterity)
                        : WeaponDamageCalculator.Evaluate(weapon, curve, strength, dexterity + 1);
                    return $"Сила атаки {_stats.ActiveWeaponAttack:0.#} → {after.ScaledBase:0.#}";
            }
        }

        /// <summary>Spends a point and redraws; the screen's only way to change attributes.</summary>
        public bool AllocatePoint(CharacterStat stat)
        {
            if (_progression == null || !_progression.AllocatePoint(stat))
            {
                return false;
            }

            Rebuild();
            return true;
        }

        public bool CanAllocate(CharacterStat stat)
        {
            return _progression != null && _progression.CanAllocate(stat);
        }

        // ---------- the skill tree ----------

        /// <summary>What the screen needs to draw one node without asking anything else.</summary>
        public readonly struct SkillNode
        {
            public readonly SkillDefinition Skill;
            public readonly int Rank;
            public readonly SkillPurchaseResult State;

            public SkillNode(SkillDefinition skill, int rank, SkillPurchaseResult state)
            {
                Skill = skill;
                Rank = rank;
                State = state;
            }

            public bool IsUnlocked => Rank > 0;
            public bool IsMaxRank => Skill != null && Rank >= Skill.MaxRank;
            public bool CanBuy => State == SkillPurchaseResult.Purchased;
            public string RankText => Skill != null && Skill.MaxRank > 1
                ? $"{Rank}/{Skill.MaxRank}"
                : string.Empty;
        }

        public int SkillPoints => _skills != null ? _skills.AvailableSkillPoints : 0;
        public bool CanSpendSkillPoints => _skills != null && _skills.CanSpendPoints;

        /// <summary>One branch's nodes with their current state, ready to draw.</summary>
        public List<SkillNode> SkillNodes(SkillBranch branch)
        {
            var nodes = new List<SkillNode>();
            if (_skills == null || _skills.Database == null)
            {
                return nodes;
            }

            foreach (var skill in _skills.Database.GetSkillsByBranch(branch))
            {
                nodes.Add(new SkillNode(skill, _skills.RankOf(skill.SkillId),
                    _skills.CanPurchase(skill.SkillId)));
            }

            return nodes;
        }

        /// <summary>
        /// The details panel for one skill: what it is, what it costs, what it needs and what it
        /// really does, generated from the same values the game reads.
        /// </summary>
        public List<string> DescribeSkill(string skillId)
        {
            var lines = new List<string>();
            if (_skills == null || _skills.Database == null
                || !_skills.Database.TryGetSkill(skillId, out var skill))
            {
                return lines;
            }

            var rank = _skills.RankOf(skillId);
            lines.Add(skill.DisplayName);
            lines.Add(skill.Type == SkillType.Active ? "Активна" : "Пасивна");
            if (!string.IsNullOrWhiteSpace(skill.Description))
            {
                lines.Add(skill.Description);
            }

            if (skill.MaxRank > 1)
            {
                lines.Add($"Ранг {rank}/{skill.MaxRank}");
            }

            // The next rank is what the player is deciding about, so that is what is shown.
            skill.DescribeInto(lines, Mathf.Max(1, rank == 0 ? 1 : rank));
            lines.Add($"Вартість: {skill.Cost}");
            if (skill.RequiredLevel > 1)
            {
                lines.Add($"Потрібен рівень {skill.RequiredLevel}");
            }

            foreach (var prerequisite in skill.Prerequisites)
            {
                var owned = _skills.HasSkill(prerequisite);
                var name = _skills.Database.TryGetSkill(prerequisite, out var parent)
                    ? parent.DisplayName
                    : prerequisite;
                lines.Add($"Потрібно: {name}{(owned ? " ✓" : string.Empty)}");
            }

            var verdict = _skills.CanPurchase(skillId);
            if (verdict != SkillPurchaseResult.Purchased && rank < skill.MaxRank)
            {
                lines.Add(PurchaseProblem(verdict));
            }

            return lines;
        }

        public SkillPurchaseResult BuySkill(string skillId)
        {
            if (_skills == null)
            {
                return SkillPurchaseResult.InvalidSkill;
            }

            var result = _skills.Purchase(skillId);
            if (result == SkillPurchaseResult.Purchased)
            {
                Rebuild();
            }

            return result;
        }

        public bool AssignActiveSkill(int slot, string skillId)
        {
            if (_activeSkills == null || !_activeSkills.AssignSlot(slot, skillId))
            {
                return false;
            }

            ContentChanged?.Invoke();
            return true;
        }

        public string ActiveSlotSkillId(int slot) =>
            _activeSkills != null ? _activeSkills.SlotSkillId(slot) : string.Empty;

        /// <summary>Every unlocked active skill, for the slot picker.</summary>
        public List<SkillDefinition> UnlockedActiveSkills()
        {
            var unlocked = new List<SkillDefinition>();
            if (_skills?.Database == null)
            {
                return unlocked;
            }

            foreach (var skill in _skills.Database.Skills)
            {
                if (skill != null && skill.IsActive && _skills.HasSkill(skill.SkillId))
                {
                    unlocked.Add(skill);
                }
            }

            return unlocked;
        }

        // ---------- the spellbook ----------

        /// <summary>The branches the tree screen shows, in order.</summary>
        public static readonly SkillBranch[] Branches =
        {
            SkillBranch.Warrior, SkillBranch.Hunter, SkillBranch.Mage, SkillBranch.Shadow
        };

        public IReadOnlyCollection<string> KnownSpells =>
            _spellbook != null ? _spellbook.KnownSpells : System.Array.Empty<string>();

        public string SpellSlotId(int slot) =>
            _spellbook != null ? _spellbook.SlotSpellId(slot) : string.Empty;

        public bool AssignSpell(int slot, string spellId)
        {
            if (_spellbook == null || !_spellbook.AssignSlot(slot, spellId))
            {
                return false;
            }

            ContentChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// The spellbook entry: what the spell does with this character's intelligence and
        /// skills, not what it would do for someone else.
        /// </summary>
        public List<string> DescribeSpell(string spellId)
        {
            var lines = new List<string>();
            if (_spellbook?.Database == null
                || !_spellbook.Database.TryGetSpell(spellId, out var spell))
            {
                return lines;
            }

            lines.Add(spell.DisplayName);
            if (!string.IsNullOrWhiteSpace(spell.Description))
            {
                lines.Add(spell.Description);
            }

            var damage = _caster != null ? _caster.Evaluate(spell).Total : spell.BaseDamage;
            var cost = _caster != null ? _caster.ManaCostOf(spell) : spell.ManaCost;
            var castTime = _caster != null ? _caster.CastTimeOf(spell) : spell.CastTime;
            spell.DescribeInto(lines, damage, cost, castTime);
            if (!_spellbook.Knows(spellId))
            {
                lines.Add("Ще не вивчено");
            }

            return lines;
        }

        public static string PurchaseProblem(SkillPurchaseResult result)
        {
            switch (result)
            {
                case SkillPurchaseResult.NotEnoughPoints: return "Бракує очок умінь";
                case SkillPurchaseResult.MissingPrerequisite: return "Спершу потрібне попереднє вміння";
                case SkillPurchaseResult.LevelTooLow: return "Замалий рівень";
                case SkillPurchaseResult.AlreadyMaxRank: return "Максимальний ранг";
                case SkillPurchaseResult.NotAllowedNow: return "Не в бою";
                case SkillPurchaseResult.InvalidSkill: return "Невідоме вміння";
                default: return string.Empty;
            }
        }

        public static string BranchName(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Warrior: return "Воїн";
                case SkillBranch.Hunter: return "Мисливець";
                case SkillBranch.Mage: return "Маг";
                case SkillBranch.Shadow: return "Тінь";
                default: return branch.ToString();
            }
        }

        public static string RarityName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return "Звичайний";
                case ItemRarity.Uncommon: return "Незвичайний";
                case ItemRarity.Rare: return "Рідкісний";
                case ItemRarity.Epic: return "Епічний";
                case ItemRarity.Legendary: return "Легендарний";
                default: return rarity.ToString();
            }
        }

        public static string CategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return "Зброя";
                case ItemCategory.Armor: return "Броня";
                case ItemCategory.Accessory: return "Прикраси";
                case ItemCategory.Consumable: return "Витратне";
                case ItemCategory.Material: return "Матеріали";
                case ItemCategory.KeyItem: return "Ключові";
                case ItemCategory.QuestItem: return "Завдання";
                case ItemCategory.Progression: return "Сюжетні";
                default: return category.ToString();
            }
        }
    }
}
