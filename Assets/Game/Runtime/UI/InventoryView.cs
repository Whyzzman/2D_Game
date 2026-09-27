using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static ForgottenKingdom.UiParts;

namespace ForgottenKingdom
{
    /// <summary>
    /// The inventory and equipment window from design/ui/inventory/inventory.html and
    /// design/ui/equipment/equipment.html, drawn over the frozen game in the HUD's document.
    /// <see cref="InventoryScreen"/> owns the behaviour (opening, pausing, tabs, selection,
    /// equipping); this class only draws it and turns keys into its calls.
    ///
    /// Pages on top: Спорядження (items by category, with the comparison panel), Екіпірування
    /// (the eight slots, what fits the chosen one, the derived stats), Персонаж
    /// (<see cref="CharacterPage"/>), Уміння (<see cref="SkillsPage"/>) and Закляття
    /// (<see cref="SpellsPage"/>). Q/E or LB/RB turn the pages, Z/C or LT/RT the
    /// categories or branches, arrows or the stick move, Enter or A acts, X unequips or assigns;
    /// Escape, B or the inventory key close it. Opened from the pause, it returns there.
    /// </summary>
    public sealed class InventoryView : IHudOverlay
    {
        public enum Page
        {
            Items,
            Equipment,
            Character,
            Skills,
            Spells
        }

        private static readonly string[] Titles =
            { "СПОРЯДЖЕННЯ", "ЕКІПІРУВАННЯ", "ПЕРСОНАЖ", "УМІННЯ", "КНИГА ЗАКЛЯТЬ" };
        private static readonly string[] PageNames = { "items", "equipment", "character", "skills", "spells" };

        private const string OpenClass = "inv--open";
        private const int GridColumns = 8;

        private static readonly EquipmentSlot[][] SlotGroups =
        {
            new[] { EquipmentSlot.MainWeapon, EquipmentSlot.SecondaryWeapon },
            new[] { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Gloves, EquipmentSlot.Boots },
            new[] { EquipmentSlot.Accessory1, EquipmentSlot.Accessory2 }
        };

        private readonly VisualElement _overlay;
        private readonly Label _title;
        private readonly Label _purse;
        private readonly Button[] _pageButtons = new Button[5];
        private readonly VisualElement _characterPip;
        private readonly VisualElement _skillsPip;
        private readonly VisualElement[] _pages = new VisualElement[5];
        private readonly CharacterPage _character;
        private readonly SkillsPage _skillsPage;
        private readonly SpellsPage _spellsPage;
        private readonly Button[] _categoryButtons = new Button[InventoryScreen.Tabs.Length];

        // Items page.
        private readonly ScrollView _grid;
        private readonly Label _empty;
        private readonly VisualElement _detail;
        private readonly Label _name;
        private readonly Label _type;
        private readonly Label _desc;
        private readonly VisualElement _compareSection;
        private readonly Label _compareTitle;
        private readonly VisualElement _compareHead;
        private readonly Label _compareNames;
        private readonly VisualElement _compare;
        private readonly VisualElement _effectsSection;
        private readonly VisualElement _effects;
        private readonly VisualElement _requirementsSection;
        private readonly VisualElement _requirements;
        private readonly Button _equip;
        private readonly Label _equipText;
        private readonly Label _problem;
        private readonly Label _count;

        // Equipment page.
        private readonly List<Button> _slotRows = new List<Button>();
        private readonly List<ItemIcon> _slotIcons = new List<ItemIcon>();
        private readonly List<Label> _slotItems = new List<Label>();
        private readonly Label _note;
        private readonly ScrollView _list;
        private readonly Button _slotEquip;
        private readonly Button _slotUnequip;
        private readonly Label _slotProblem;
        private readonly VisualElement _stats;
        private readonly VisualElement _loadFill;
        private readonly Label _loadValue;
        private readonly List<string> _candidates = new List<string>();

        private InventoryScreen _screen;
        private PlayerEquipment _equipment;
        private PlayerStats _playerStats;
        private PlayerProgressionService _progression;
        private PlayerSkills _skills;
        private Page _page;
        private Action _onClosed;
        private int _slotIndex;
        private bool _inList;
        private string _candidateId = string.Empty;
        private int _openedFrame = -1;
        private MenuInput.Repeater _vertical;
        private MenuInput.Repeater _horizontal;

        public InventoryView(VisualElement root)
        {
            _overlay = root?.Q("inventory");
            if (_overlay == null)
            {
                return;
            }

            _title = _overlay.Q<Label>("inv-title");
            _purse = _overlay.Q<Label>("inv-purse");
            for (var i = 0; i < _pageButtons.Length; i++)
            {
                var page = (Page)i;
                _pageButtons[i] = _overlay.Q<Button>($"inv-page-{PageNames[i]}");
                _pageButtons[i].clicked += () => ShowPage(page);
                _pages[i] = _overlay.Q($"inv-{PageNames[i]}");
            }

            _characterPip = _overlay.Q("inv-character-pip");
            _skillsPip = _overlay.Q("inv-skills-pip");
            _character = new CharacterPage(_pages[(int)Page.Character], Redraw);
            _skillsPage = new SkillsPage(_pages[(int)Page.Skills], Redraw);
            _spellsPage = new SpellsPage(_pages[(int)Page.Spells], Redraw);
            for (var i = 0; i < _categoryButtons.Length; i++)
            {
                var index = i;
                _categoryButtons[i] = _overlay.Q<Button>($"inv-cat-{i}");
                if (_categoryButtons[i] != null) { _categoryButtons[i].clicked += () => _screen?.SelectTab(index); }
            }

            _grid = _overlay.Q<ScrollView>("inv-grid");
            if (_grid != null)
            {
                // The grid is a wrapping row: USS has no grid layout.
                _grid.contentContainer.style.flexDirection = FlexDirection.Row;
                _grid.contentContainer.style.flexWrap = Wrap.Wrap;
            }

            _empty = _overlay.Q<Label>("inv-empty");
            _detail = _overlay.Q("inv-detail");
            _name = _overlay.Q<Label>("inv-name");
            _type = _overlay.Q<Label>("inv-type");
            _desc = _overlay.Q<Label>("inv-desc");
            _compareSection = _overlay.Q("inv-compare-section");
            _compareTitle = _overlay.Q<Label>("inv-compare-title");
            _compareHead = _overlay.Q("inv-compare-head");
            _compareNames = _overlay.Q<Label>("inv-compare-names");
            _compare = _overlay.Q("inv-compare");
            _effectsSection = _overlay.Q("inv-effects-section");
            _effects = _overlay.Q("inv-effects");
            _requirementsSection = _overlay.Q("inv-req-section");
            _requirements = _overlay.Q("inv-req");
            _equip = _overlay.Q<Button>("inv-equip");
            _equipText = _equip?.Q<Label>();
            if (_equip != null) { _equip.clicked += ActOnSelectedItem; }
            _problem = _overlay.Q<Label>("inv-problem");
            _count = _overlay.Q<Label>("inv-count");

            BuildSlotRows();
            _note = _overlay.Q<Label>("eq-note");
            _list = _overlay.Q<ScrollView>("eq-list");
            _slotEquip = _overlay.Q<Button>("eq-equip");
            _slotUnequip = _overlay.Q<Button>("eq-unequip");
            if (_slotEquip != null) { _slotEquip.clicked += EquipCandidate; }
            if (_slotUnequip != null) { _slotUnequip.clicked += UnequipSlot; }
            _slotProblem = _overlay.Q<Label>("eq-problem");
            _stats = _overlay.Q("eq-stats");
            _loadFill = _overlay.Q("eq-load-fill");
            _loadValue = _overlay.Q<Label>("eq-load-value");
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);
        public Page CurrentPage => _page;
        public EquipmentSlot SelectedSlot => EquipmentSlots.All[_slotIndex];
        public string SelectedCandidate => _candidateId;
        public IReadOnlyList<string> Candidates => _candidates;
        public CharacterPage Character => _character;
        public SkillsPage Skills => _skillsPage;
        public SpellsPage Spells => _spellsPage;

        /// <summary>Cells in the grid that hold an item, for tests and tools.</summary>
        public int ItemCellCount { get; private set; }

        public void Attach(PlayerCharacter player, GameProgress progress)
        {
            Detach();
            if (_overlay == null)
            {
                return;
            }

            _screen = UnityEngine.Object.FindAnyObjectByType<InventoryScreen>();
            _equipment = player != null ? player.Equipment : null;
            _playerStats = player != null ? player.Stats : null;
            _progression = UnityEngine.Object.FindAnyObjectByType<PlayerProgressionService>();
            _skills = UnityEngine.Object.FindAnyObjectByType<PlayerSkills>();
            if (_screen == null)
            {
                return;
            }

            _character.Attach(_screen, player, _progression);
            _skillsPage.Attach(_screen, _skills);
            _spellsPage.Attach(_screen, player);
            _screen.AddPresenter(this);
            _screen.Opened += OnOpened;
            _screen.Closed += OnClosed;
            _screen.ContentChanged += Redraw;
            if (_playerStats != null) { _playerStats.StatsChanged += Redraw; }
        }

        public void Detach()
        {
            if (_screen != null)
            {
                _screen.Opened -= OnOpened;
                _screen.Closed -= OnClosed;
                _screen.ContentChanged -= Redraw;
                _screen.RemovePresenter(this);
            }

            if (_playerStats != null) { _playerStats.StatsChanged -= Redraw; }
            _screen = null;
            _onClosed = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        /// <summary>
        /// Opens on a page. <paramref name="onClosed"/> runs when it closes: how the pause and the
        /// altar take over again after a window they opened.
        /// </summary>
        public bool Open(Page page, Action onClosed = null)
        {
            if (_screen == null)
            {
                return false;
            }

            _page = page;
            _onClosed = onClosed;
            _screen.Open();
            if (!_screen.IsOpen)
            {
                _onClosed = null;
            }

            return _screen.IsOpen;
        }

        public void Close()
        {
            _screen?.Close();
        }

        public void Back() => Close();

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            _inList = false;
            ShowPage(_page);
        }

        private void OnClosed()
        {
            _overlay.RemoveFromClassList(OpenClass);
            _page = Page.Items;
            var onClosed = _onClosed;
            _onClosed = null;
            onClosed?.Invoke();
        }

        public void ShowPage(Page page)
        {
            _page = page;
            for (var i = 0; i < _pages.Length; i++)
            {
                _pages[i]?.EnableInClassList("inv__page--active", i == (int)page);
                _pageButtons[i]?.EnableInClassList("tab--active", i == (int)page);
            }

            if (_title != null) { _title.text = Titles[(int)page]; }
            if (page == Page.Equipment)
            {
                SelectSlot(_slotIndex);
            }

            Redraw();
        }

        // ---------- input ----------

        /// <summary>Keyboard and pad; run every frame by the HUD on unscaled time.</summary>
        public void Tick()
        {
            // Not on the frame it opened: the key that opened it must not also act inside it.
            if (!IsOpen || Time.frameCount == _openedFrame)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (MenuInput.PadBackPressed())
            {
                Close();
                return;
            }

            var turn = Pressed(keyboard?.qKey, pad?.leftShoulder) ? -1
                : Pressed(keyboard?.eKey, pad?.rightShoulder) ? 1 : 0;
            if (turn != 0)
            {
                ShowPage((Page)(((int)_page + turn + _pages.Length) % _pages.Length));
                return;
            }

            var vertical = _vertical.Step(MenuInput.ReadVertical());
            var horizontal = _horizontal.Step(MenuInput.ReadHorizontal());
            switch (_page)
            {
                case Page.Items: TickItems(keyboard, pad, horizontal, vertical); break;
                case Page.Equipment: TickEquipment(keyboard, pad, horizontal, vertical); break;
                case Page.Character: _character.Tick(vertical); break;
                case Page.Skills: _skillsPage.Tick(keyboard, pad, horizontal, vertical); break;
                default: _spellsPage.Tick(keyboard, pad, horizontal, vertical); break;
            }
        }

        private void TickItems(Keyboard keyboard, Gamepad pad, int horizontal, int vertical)
        {
            if (Pressed(keyboard?.zKey, pad?.leftTrigger))
            {
                _screen.PreviousTab();
                return;
            }

            if (Pressed(keyboard?.cKey, pad?.rightTrigger))
            {
                _screen.NextTab();
                return;
            }

            var step = horizontal + vertical * GridColumns;
            if (step != 0)
            {
                MoveInGrid(step);
            }

            if (MenuInput.Confirmed())
            {
                ActOnSelectedItem();
            }
        }

        private void TickEquipment(Keyboard keyboard, Gamepad pad, int horizontal, int vertical)
        {
            if (horizontal != 0)
            {
                _inList = horizontal > 0 && _candidates.Count > 0;
                Redraw();
            }

            if (vertical != 0)
            {
                if (_inList)
                {
                    var index = Mathf.Clamp(_candidates.IndexOf(_candidateId) + vertical, 0,
                        _candidates.Count - 1);
                    SelectCandidate(_candidates[index]);
                }
                else
                {
                    SelectSlot(Mathf.Clamp(_slotIndex + vertical, 0, EquipmentSlots.All.Length - 1));
                }
            }

            if (Pressed(keyboard?.xKey, pad?.buttonWest))
            {
                UnequipSlot();
            }

            if (MenuInput.Confirmed())
            {
                if (_inList)
                {
                    EquipCandidate();
                }
                else if (_candidates.Count > 0)
                {
                    _inList = true;
                    Redraw();
                }
            }
        }

        private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl key,
            UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            return (key != null && key.wasPressedThisFrame) || (button != null && button.wasPressedThisFrame);
        }

        private void MoveInGrid(int step)
        {
            var entries = _screen.Entries;
            if (entries.Count == 0)
            {
                return;
            }

            var current = IndexOfEntry(_screen.SelectedItemId);
            var next = Mathf.Clamp(current < 0 ? 0 : current + step, 0, entries.Count - 1);
            _screen.Select(entries[next].ItemId);
        }

        private int IndexOfEntry(string itemId)
        {
            var entries = _screen.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].ItemId == itemId) { return i; }
            }

            return -1;
        }

        // ---------- actions ----------

        /// <summary>The detail panel's one action: equip what can be worn, use what can be eaten.</summary>
        public void ActOnSelectedItem()
        {
            if (_screen?.SelectedEntry?.Definition?.IsUsable == true)
            {
                UseSelectedItem();
            }
            else
            {
                EquipSelectedItem();
            }
        }

        /// <summary>
        /// Uses a healing item. The window closes straight to the game, not back to the pause or
        /// the altar it came from: the player is about to stand there eating.
        /// </summary>
        public void UseSelectedItem()
        {
            if (_screen == null || _screen.WhyCannotUseSelected() != null)
            {
                return;
            }

            _onClosed = null;
            _screen.UseSelected();
        }

        public void EquipSelectedItem()
        {
            var entry = _screen?.SelectedEntry;
            if (entry == null || !entry.Value.CanEquip || entry.Value.IsEquipped)
            {
                return;
            }

            _screen.EquipSelected();
        }

        public void SelectSlot(int index)
        {
            _slotIndex = Mathf.Clamp(index, 0, EquipmentSlots.All.Length - 1);
            _candidateId = string.Empty;
            Redraw();
        }

        public void SelectSlot(EquipmentSlot slot) => SelectSlot(Array.IndexOf(EquipmentSlots.All, slot));

        public void SelectCandidate(string itemId)
        {
            _candidateId = itemId ?? string.Empty;
            _inList = true;
            Redraw();
        }

        public void EquipCandidate()
        {
            if (_screen == null || _equipment == null || string.IsNullOrEmpty(_candidateId)
                || _equipment.CheckEquip(_candidateId, SelectedSlot) != EquipResult.Equipped)
            {
                return;
            }

            _screen.Equip(_candidateId, SelectedSlot);
        }

        public void UnequipSlot()
        {
            if (_screen == null || SelectedSlot == EquipmentSlot.MainWeapon)
            {
                return;
            }

            _screen.Unequip(SelectedSlot);
        }

        // ---------- drawing ----------

        private void Redraw()
        {
            if (!IsOpen || _screen == null)
            {
                return;
            }

            if (_purse != null) { _purse.text = _screen.Currency.ToString("N0"); }
            _characterPip?.EnableInClassList("pip--visible", _progression != null && _progression.StatPoints > 0);
            _skillsPip?.EnableInClassList("pip--visible", _skills != null && _skills.AvailableSkillPoints > 0);
            switch (_page)
            {
                case Page.Items: DrawItems(); break;
                case Page.Equipment: DrawEquipment(); break;
                case Page.Character: _character.Draw(); break;
                case Page.Skills: _skillsPage.Draw(); break;
                default: _spellsPage.Draw(); break;
            }
        }

        private void DrawItems()
        {
            for (var i = 0; i < _categoryButtons.Length; i++)
            {
                _categoryButtons[i]?.EnableInClassList("tab--active", i == _screen.TabIndex);
            }

            var entries = _screen.Entries;
            _grid.Clear();
            foreach (var entry in entries)
            {
                _grid.Add(ItemCell(entry));
            }

            ItemCellCount = entries.Count;
            // The rest of the row, and one row at least, as empty cells: a grid, not a list.
            var cells = Mathf.Max(GridColumns, (entries.Count + GridColumns - 1) / GridColumns * GridColumns);
            for (var i = entries.Count; i < cells; i++)
            {
                _grid.Add(EmptyCell());
            }

            if (_count != null)
            {
                _count.text = $"{InventoryScreen.CategoryName(_screen.ActiveTab)} — {entries.Count}";
            }

            var selected = _screen.SelectedEntry;
            var hasItem = selected != null && selected.Value.Definition != null;
            Show(_empty, !hasItem);
            Show(_detail, hasItem);
            if (hasItem)
            {
                DrawDetail(selected.Value);
            }
        }

        private VisualElement ItemCell(InventoryEntry entry)
        {
            var cell = new VisualElement();
            cell.AddToClassList("inv-slot");
            cell.AddToClassList(RarityClass("inv-slot--", entry.Definition.Rarity));
            cell.EnableInClassList("inv-slot--selected", entry.ItemId == _screen.SelectedItemId);
            var icon = new ItemIcon();
            icon.Show(entry.Definition);
            cell.Add(icon);
            if (entry.IsEquipped)
            {
                cell.Add(Marker("inv-slot__equipped"));
            }

            if (!string.IsNullOrEmpty(entry.AmountText))
            {
                cell.Add(Text(entry.Amount.ToString(), "inv-slot__count"));
            }

            var itemId = entry.ItemId;
            cell.RegisterCallback<PointerDownEvent>(_ => _screen.Select(itemId));
            return cell;
        }

        private static VisualElement EmptyCell()
        {
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.AddToClassList("inv-slot");
            cell.AddToClassList("inv-slot--empty");
            cell.Add(Marker("inv-slot__diamond"));
            return cell;
        }

        private void DrawDetail(InventoryEntry entry)
        {
            var item = entry.Definition;
            _name.text = item.DisplayName.ToUpperInvariant();
            _type.text = $"{InventoryScreen.RarityName(item.Rarity)} · {TypeName(item)}".ToUpperInvariant();
            foreach (var rarity in (ItemRarity[])Enum.GetValues(typeof(ItemRarity)))
            {
                _type.EnableInClassList(RarityClass("rar-", rarity), rarity == item.Rarity);
            }

            _desc.text = item.Description ?? string.Empty;
            Show(_desc, !string.IsNullOrWhiteSpace(item.Description));

            DrawComparison(entry);
            DrawEffects(item);
            DrawRequirements(item.Weapon, _requirements, _requirementsSection);

            Show(_equip, item.IsEquippable || item.IsUsable);
            if (item.IsUsable)
            {
                var why = _screen.WhyCannotUseSelected();
                _equip.SetEnabled(why == null);
                _equipText.text = "ВИКОРИСТАТИ";
                _problem.text = why ?? string.Empty;
            }
            else
            {
                _equip.SetEnabled(entry.CanEquip && !entry.IsEquipped);
                _equipText.text = entry.IsEquipped ? "ЕКІПІРОВАНО" : "ЕКІПІРУВАТИ";
                _problem.text = item.IsEquippable && !entry.IsEquipped && !entry.CanEquip
                    ? Problem(entry.ItemId, _equipment.DefaultSlotFor(entry.ItemId))
                    : string.Empty;
            }
            Show(_problem, !string.IsNullOrEmpty(_problem.text));
        }

        private void DrawComparison(InventoryEntry entry)
        {
            _compare.Clear();
            var item = entry.Definition;
            ItemDefinition worn = null;
            if (_equipment != null && item.Equipment != null)
            {
                var slot = _equipment.DefaultSlotFor(entry.ItemId);
                // A weapon bound for the empty second hand is judged against the one in use.
                if (item.Weapon != null && _equipment.GetEquippedItem(slot) == null)
                {
                    slot = _equipment.ActiveWeaponSlot;
                }

                worn = _equipment.GetEquippedItem(slot);
            }

            var rows = ItemComparison.Rows(item, worn, Wielder());
            Show(_compareSection, rows.Count > 0);
            if (rows.Count == 0)
            {
                return;
            }

            var comparing = rows[0].IsComparison;
            _compareTitle.text = comparing ? "ПОРІВНЯННЯ З ЕКІПІРОВАНИМ" : "ХАРАКТЕРИСТИКИ";
            _compareNames.text = comparing ? $"{worn.DisplayName} → {item.DisplayName}" : string.Empty;
            Show(_compareHead, comparing);
            foreach (var row in rows)
            {
                var line = new VisualElement { pickingMode = PickingMode.Ignore };
                line.AddToClassList("compare__row");
                line.Add(Text(row.Label, "compare__label"));
                line.Add(Text(row.IsComparison ? $"{row.From} → {row.To}" : row.To, "compare__val"));
                if (row.IsComparison)
                {
                    var delta = Text(DeltaText(row), "delta");
                    delta.AddToClassList(row.Trend == ComparisonTrend.Better ? "delta--up"
                        : row.Trend == ComparisonTrend.Worse ? "delta--down" : "delta--same");
                    line.Add(delta);
                }

                _compare.Add(line);
            }
        }

        /// <summary>Colour plus an arrow, so the change reads without telling colours apart.</summary>
        private static string DeltaText(ComparisonRow row)
        {
            switch (row.Trend)
            {
                case ComparisonTrend.Better: return "▲ " + row.Delta;
                case ComparisonTrend.Worse: return "▼ " + row.Delta;
                default: return row.Delta;
            }
        }

        private void DrawEffects(ItemDefinition item)
        {
            _effects.Clear();
            var lines = new List<string>();
            if (item.Equipment is AccessoryDefinition accessory)
            {
                accessory.DescribeInto(lines);
            }
            else if (item.Equipment != null)
            {
                foreach (var modifier in item.Equipment.Modifiers)
                {
                    if (modifier != null) { lines.Add(modifier.Describe()); }
                }
            }

            foreach (var line in lines)
            {
                _effects.Add(Text(line, "effect-line"));
            }

            Show(_effectsSection, lines.Count > 0);
        }

        private void DrawRequirements(WeaponDefinition weapon, VisualElement into, VisualElement section)
        {
            into.Clear();
            var has = weapon != null && weapon.HasRequirements && _playerStats != null;
            Show(section, has);
            if (!has)
            {
                return;
            }

            AddRequirement(into, "Сила", weapon.StrengthRequirement, CharacterStat.Strength);
            AddRequirement(into, "Спритність", weapon.DexterityRequirement, CharacterStat.Dexterity);
        }

        private void AddRequirement(VisualElement into, string name, int required, CharacterStat stat)
        {
            if (required <= 0)
            {
                return;
            }

            var have = _playerStats.GetFinalStat(stat);
            var met = have >= required;
            var label = Text(met ? $"✓ {name} {required}" : $"✕ {name} {required} — у вас {have}",
                "requirement");
            label.EnableInClassList("requirement--unmet", !met);
            into.Add(label);
        }

        /// <summary>Why equipping is refused, in words; a refusal never stays silent.</summary>
        private string Problem(string itemId, EquipmentSlot slot)
        {
            if (_equipment == null)
            {
                return string.Empty;
            }

            switch (_equipment.CheckEquip(itemId, slot))
            {
                case EquipResult.RequirementsNotMet:
                    var weapon = _equipment.Items != null ? _equipment.Items.GetItem(itemId)?.Weapon : null;
                    return weapon != null ? MissingText(weapon) : "Не виконано вимоги.";
                case EquipResult.AlreadyInOtherSlot:
                    return "Єдиний примірник уже в іншому слоті.";
                default:
                    return string.Empty;
            }
        }

        private string MissingText(WeaponDefinition weapon)
        {
            var parts = new List<string>();
            var strength = _playerStats != null ? _playerStats.GetFinalStat(CharacterStat.Strength) : 0;
            var dexterity = _playerStats != null ? _playerStats.GetFinalStat(CharacterStat.Dexterity) : 0;
            if (strength < weapon.StrengthRequirement)
            {
                parts.Add($"Сили: потрібно {weapon.StrengthRequirement}, у вас {strength}");
            }

            if (dexterity < weapon.DexterityRequirement)
            {
                parts.Add($"Спритності: потрібно {weapon.DexterityRequirement}, у вас {dexterity}");
            }

            return "Не вистачає " + string.Join("; ", parts) + ".";
        }

        // ---------- equipment page ----------

        private void BuildSlotRows()
        {
            for (var group = 0; group < SlotGroups.Length; group++)
            {
                var container = _overlay.Q($"eq-group-{group}");
                foreach (var slot in SlotGroups[group])
                {
                    var row = new Button();
                    row.AddToClassList("eq-slot");
                    var frame = new VisualElement { pickingMode = PickingMode.Ignore };
                    frame.AddToClassList("inv-slot");
                    frame.AddToClassList("inv-slot--medium");
                    var icon = new ItemIcon();
                    frame.Add(icon);
                    frame.Add(Marker("inv-slot__diamond"));
                    row.Add(frame);
                    var meta = new VisualElement { pickingMode = PickingMode.Ignore };
                    meta.AddToClassList("eq-slot__meta");
                    meta.Add(Text(EquipmentSlots.Display(slot).ToUpperInvariant(), "eq-slot__label"));
                    var item = Text(string.Empty, "eq-slot__item");
                    meta.Add(item);
                    row.Add(meta);
                    var index = Array.IndexOf(EquipmentSlots.All, slot);
                    row.clicked += () =>
                    {
                        _inList = false;
                        SelectSlot(index);
                    };
                    container?.Add(row);
                    // Kept in EquipmentSlots.All order, whatever the grouping.
                    while (_slotRows.Count <= index)
                    {
                        _slotRows.Add(null);
                        _slotIcons.Add(null);
                        _slotItems.Add(null);
                    }

                    _slotRows[index] = row;
                    _slotIcons[index] = icon;
                    _slotItems[index] = item;
                }
            }
        }

        private void DrawEquipment()
        {
            if (_equipment == null)
            {
                return;
            }

            for (var i = 0; i < _slotRows.Count; i++)
            {
                var slot = EquipmentSlots.All[i];
                var worn = _equipment.GetEquippedItem(slot);
                _slotRows[i].EnableInClassList("eq-slot--selected", i == _slotIndex);
                var frame = _slotIcons[i].parent;
                frame.EnableInClassList("inv-slot--empty", worn == null);
                foreach (var rarity in (ItemRarity[])Enum.GetValues(typeof(ItemRarity)))
                {
                    frame.EnableInClassList(RarityClass("inv-slot--", rarity), worn != null && worn.Rarity == rarity);
                }

                Show(_slotIcons[i], worn != null);
                if (worn != null) { _slotIcons[i].Show(worn); }
                _slotItems[i].text = worn != null ? worn.DisplayName : "Порожньо";
                _slotItems[i].EnableInClassList("eq-slot__item--empty", worn == null);
            }

            DrawCandidates();
            DrawDerived();
        }

        private void DrawCandidates()
        {
            var slot = SelectedSlot;
            _candidates.Clear();
            _candidates.AddRange(_equipment.OwnedItemsFor(slot));
            var catalogue = _equipment.Items;
            _candidates.Sort((a, b) => string.CompareOrdinal(
                catalogue?.GetItem(a)?.DisplayName ?? a, catalogue?.GetItem(b)?.DisplayName ?? b));
            if (!_candidates.Contains(_candidateId))
            {
                var worn = _equipment.GetEquipped(slot);
                _candidateId = _candidates.Contains(worn) ? worn
                    : _candidates.Count > 0 ? _candidates[0] : string.Empty;
            }

            if (_candidates.Count == 0) { _inList = false; }
            _note.text = $"Обрано «{EquipmentSlots.Display(slot)}» — показано лише те, що туди стане.";
            _list.Clear();
            foreach (var itemId in _candidates)
            {
                _list.Add(CandidateRow(itemId, catalogue?.GetItem(itemId)));
            }

            if (_candidates.Count == 0)
            {
                _list.Add(Text("Немає нічого, що стане в цей слот.", "list__empty"));
            }

            var result = string.IsNullOrEmpty(_candidateId)
                ? EquipResult.UnknownItem
                : _equipment.CheckEquip(_candidateId, slot);
            _slotEquip?.SetEnabled(result == EquipResult.Equipped);
            _slotUnequip?.SetEnabled(slot != EquipmentSlot.MainWeapon
                && !string.IsNullOrEmpty(_equipment.GetEquipped(slot)));
            _slotEquip?.EnableInClassList("btn--focused", _inList);
            _slotProblem.text = string.IsNullOrEmpty(_candidateId) ? string.Empty : Problem(_candidateId, slot);
            Show(_slotProblem, !string.IsNullOrEmpty(_slotProblem.text));
        }

        private VisualElement CandidateRow(string itemId, ItemDefinition item)
        {
            var row = new Button();
            row.AddToClassList("list__row");
            row.EnableInClassList("list__row--selected", itemId == _candidateId && _inList);
            row.EnableInClassList("list__row--focus-lost", itemId == _candidateId && !_inList);
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("inv-slot");
            frame.AddToClassList("inv-slot--small");
            if (item != null) { frame.AddToClassList(RarityClass("inv-slot--", item.Rarity)); }
            var icon = new ItemIcon();
            icon.Show(item);
            frame.Add(icon);
            row.Add(frame);
            row.Add(Text(item != null ? item.DisplayName : itemId, "list__name"));

            var slot = SelectedSlot;
            var tag = Text(string.Empty, "list__tag");
            if (_equipment.IsEquippedIn(itemId, slot))
            {
                tag.text = "ЕКІПІРОВАНО";
                tag.AddToClassList("list__tag--equipped");
            }
            else
            {
                switch (_equipment.CheckEquip(itemId, slot))
                {
                    case EquipResult.RequirementsNotMet:
                        tag.text = UnmetTag(item?.Weapon);
                        tag.AddToClassList("list__tag--unmet");
                        break;
                    case EquipResult.AlreadyInOtherSlot:
                        tag.text = "В ІНШОМУ СЛОТІ";
                        break;
                }
            }

            row.Add(tag);
            row.clicked += () => SelectCandidate(itemId);
            return row;
        }

        private string UnmetTag(WeaponDefinition weapon)
        {
            if (weapon == null || _playerStats == null)
            {
                return string.Empty;
            }

            return _playerStats.GetFinalStat(CharacterStat.Strength) < weapon.StrengthRequirement
                ? $"СИЛА {weapon.StrengthRequirement}"
                : $"СПРИТНІСТЬ {weapon.DexterityRequirement}";
        }

        private void DrawDerived()
        {
            UiParts.DerivedStats(_stats, _playerStats, withLoad: false);
            if (_playerStats == null)
            {
                return;
            }

            _loadFill.style.width = Length.Percent(Mathf.Clamp01(_playerStats.EquipLoadRatio) * 100f);
            _loadValue.text = $"{_playerStats.EquipmentWeight:0.0} / {_playerStats.MaxEquipLoad:0.0}";
        }

        // ---------- helpers ----------

        private ItemComparison.Wielder Wielder()
        {
            return _playerStats == null
                ? new ItemComparison.Wielder(0, 0, null)
                : new ItemComparison.Wielder(_playerStats.GetFinalStat(CharacterStat.Strength),
                    _playerStats.GetFinalStat(CharacterStat.Dexterity), _playerStats.Curve);
        }

        private static string TypeName(ItemDefinition item)
        {
            if (item.Weapon != null) { return WeaponDefinition.TypeName(item.Weapon.WeaponType); }
            if (item.Equipment is ArmorDefinition armor) { return EquipmentSlots.Display(armor.Slot); }
            if (item.Equipment is AccessoryDefinition) { return "Прикраса"; }
            return InventoryScreen.CategoryName(item.Category);
        }

        private static string RarityClass(string prefix, ItemRarity rarity)
        {
            return prefix + rarity.ToString().ToLowerInvariant();
        }
    }
}
