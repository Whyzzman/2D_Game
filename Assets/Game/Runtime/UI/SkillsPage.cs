using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The "Уміння" page of the inventory window, from design/ui/skills/skills.html: the four
    /// branches, the tree with its prerequisite lines, and the panel of the chosen node — what
    /// it does, what it needs, which active slot holds it, learning and assigning.
    ///
    /// The tree is drawn top to bottom as in the mockup; the data places nodes left to right
    /// (<see cref="SkillDefinition.Position"/> is depth, lane), so the page turns it.
    /// </summary>
    public sealed class SkillsPage
    {
        private const float ColumnWidth = 120f;
        private const float RowHeight = 116f;
        private const float NodeSize = 84f;
        private static readonly string[] SlotKeys = { "1", "2" };

        private readonly Button[] _branchButtons = new Button[InventoryScreen.Branches.Length];
        private readonly Label _points;
        private readonly VisualElement _pointsPip;
        private readonly VisualElement _tree;
        private readonly SkillLinks _links = new SkillLinks();
        private readonly List<SkillNodeElement> _nodes = new List<SkillNodeElement>();
        private readonly Label _name;
        private readonly Label _meta;
        private readonly Label _desc;
        private readonly Label _effectsTitle;
        private readonly VisualElement _effects;
        private readonly VisualElement _requirements;
        private readonly VisualElement _slotsSection;
        private readonly Button[] _slots = new Button[ActiveSkillController.SlotCount];
        private readonly Label[] _slotNames = new Label[ActiveSkillController.SlotCount];
        private readonly Button _buy;
        private readonly Label _buyText;
        private readonly Button _assign;
        private readonly Label _problem;
        private readonly System.Action _changed;

        private InventoryScreen _screen;
        private PlayerSkills _skills;
        private int _branch;
        private string _selected = string.Empty;

        internal SkillsPage(VisualElement page, System.Action changed)
        {
            _changed = changed;
            for (var i = 0; i < _branchButtons.Length; i++)
            {
                var index = i;
                _branchButtons[i] = page.Q<Button>($"sk-branch-{i}");
                if (_branchButtons[i] != null) { _branchButtons[i].clicked += () => SelectBranch(index); }
            }

            _points = page.Q<Label>("sk-points");
            _pointsPip = page.Q("sk-points-pip");
            _tree = page.Q("sk-tree");
            _tree?.Add(_links);
            _name = page.Q<Label>("sk-name");
            _meta = page.Q<Label>("sk-meta");
            _desc = page.Q<Label>("sk-desc");
            _effectsTitle = page.Q<Label>("sk-effects-title");
            _effects = page.Q("sk-effects");
            _requirements = page.Q("sk-req");
            _slotsSection = page.Q("sk-slots-section");
            for (var i = 0; i < _slots.Length; i++)
            {
                var slot = i;
                _slots[i] = page.Q<Button>($"sk-slot-{i}");
                _slotNames[i] = page.Q<Label>($"sk-slot-{i}-name");
                if (_slots[i] != null) { _slots[i].clicked += () => Assign(slot); }
            }

            _buy = page.Q<Button>("sk-buy");
            _buyText = _buy?.Q<Label>();
            if (_buy != null) { _buy.clicked += () => Buy(); }
            _assign = page.Q<Button>("sk-assign");
            if (_assign != null) { _assign.clicked += () => AssignNext(); }
            _problem = page.Q<Label>("sk-problem");
        }

        public SkillBranch Branch => InventoryScreen.Branches[_branch];
        public string Selected => _selected;
        public int NodeCount => _nodes.Count;

        public void Attach(InventoryScreen screen, PlayerSkills skills)
        {
            _screen = screen;
            _skills = skills;
        }

        public void SelectBranch(int index)
        {
            var count = InventoryScreen.Branches.Length;
            _branch = ((index % count) + count) % count;
            _selected = string.Empty;
            _changed?.Invoke();
        }

        public void SelectBranch(SkillBranch branch) =>
            SelectBranch(System.Array.IndexOf(InventoryScreen.Branches, branch));

        public void Select(string skillId)
        {
            _selected = skillId ?? string.Empty;
            _changed?.Invoke();
        }

        public SkillPurchaseResult Buy()
        {
            return _screen != null && !string.IsNullOrEmpty(_selected)
                ? _screen.BuySkill(_selected)
                : SkillPurchaseResult.InvalidSkill;
        }

        /// <summary>Puts the chosen active skill into a slot; the slot's own button picks which.</summary>
        public bool Assign(int slot)
        {
            return CanAssign && _screen.AssignActiveSkill(slot, _selected);
        }

        /// <summary>
        /// X or "Призначити в слот": the first empty slot, else the first one; a skill already in a
        /// slot moves to the other, so pressing again walks it between the two.
        /// </summary>
        public bool AssignNext()
        {
            if (!CanAssign)
            {
                return false;
            }

            var current = SlotOf(_selected);
            var target = current >= 0 ? (current + 1) % _slots.Length : 0;
            if (current < 0)
            {
                for (var i = 0; i < _slots.Length; i++)
                {
                    if (string.IsNullOrEmpty(_screen.ActiveSlotSkillId(i))) { target = i; break; }
                }
            }

            return Assign(target);
        }

        private bool CanAssign => _screen != null && _skills != null && SelectedSkill != null
            && SelectedSkill.IsActive && _skills.HasSkill(_selected);

        private SkillDefinition SelectedSkill =>
            _skills?.Database != null && _skills.Database.TryGetSkill(_selected, out var skill) ? skill : null;

        private int SlotOf(string skillId)
        {
            for (var i = 0; i < _slots.Length; i++)
            {
                if (_screen.ActiveSlotSkillId(i) == skillId) { return i; }
            }

            return -1;
        }

        public void Tick(Keyboard keyboard, Gamepad pad, int horizontal, int vertical)
        {
            if (Pressed(keyboard?.zKey, pad?.leftTrigger)) { SelectBranch(_branch - 1); return; }
            if (Pressed(keyboard?.cKey, pad?.rightTrigger)) { SelectBranch(_branch + 1); return; }
            if (horizontal != 0 || vertical != 0) { MoveSelection(horizontal, vertical); }
            if (Pressed(keyboard?.xKey, pad?.buttonWest)) { AssignNext(); }
            if (MenuInput.Confirmed()) { Buy(); }
        }

        private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl key,
            UnityEngine.InputSystem.Controls.ButtonControl button)
        {
            return (key != null && key.wasPressedThisFrame) || (button != null && button.wasPressedThisFrame);
        }

        /// <summary>To the nearest node in that direction on the drawn grid.</summary>
        private void MoveSelection(int horizontal, int vertical)
        {
            var nodes = _screen.SkillNodes(Branch);
            var from = nodes.Find(node => node.Skill.SkillId == _selected);
            if (from.Skill == null)
            {
                return;
            }

            var origin = Cell(from.Skill);
            string best = null;
            var bestScore = float.MaxValue;
            foreach (var node in nodes)
            {
                var delta = Cell(node.Skill) - origin;
                var along = horizontal != 0 ? delta.x * horizontal : delta.y * vertical;
                var across = horizontal != 0 ? Mathf.Abs(delta.y) : Mathf.Abs(delta.x);
                if (along <= 0f)
                {
                    continue;
                }

                var score = along + across * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = node.Skill.SkillId;
                }
            }

            if (best != null)
            {
                Select(best);
            }
        }

        /// <summary>Column and row on screen: the data's lane across, its depth down.</summary>
        private static Vector2 Cell(SkillDefinition skill) => new Vector2(skill.Position.y, skill.Position.x);

        // ---------- drawing ----------

        public void Draw()
        {
            if (_screen == null || _skills == null)
            {
                return;
            }

            for (var i = 0; i < _branchButtons.Length; i++)
            {
                _branchButtons[i]?.EnableInClassList("tab--active", i == _branch);
            }

            _points.text = _screen.SkillPoints.ToString();
            _pointsPip?.EnableInClassList("pip--visible", _screen.SkillPoints > 0);

            var nodes = _screen.SkillNodes(Branch);
            if (nodes.Count > 0 && nodes.FindIndex(node => node.Skill.SkillId == _selected) < 0)
            {
                _selected = nodes[0].Skill.SkillId;
            }

            DrawTree(nodes);
            DrawDetail(nodes.Find(node => node.Skill.SkillId == _selected));
        }

        private void DrawTree(List<InventoryScreen.SkillNode> nodes)
        {
            foreach (var node in _nodes)
            {
                node.RemoveFromHierarchy();
            }

            _nodes.Clear();
            var links = new List<(Vector2, Vector2, bool)>();
            foreach (var node in nodes)
            {
                var cell = Cell(node.Skill);
                var element = new SkillNodeElement();
                element.style.left = cell.x * ColumnWidth;
                element.style.top = cell.y * RowHeight;
                var slot = SlotOf(node.Skill.SkillId);
                element.Show(node.Skill, node.Rank, StateOf(node), node.Skill.SkillId == _selected,
                    slot >= 0 ? SlotKeys[slot] : null);
                var skillId = node.Skill.SkillId;
                element.RegisterCallback<PointerDownEvent>(_ => Select(skillId));
                _tree.Add(element);
                _nodes.Add(element);

                foreach (var prerequisite in node.Skill.Prerequisites)
                {
                    var parent = nodes.Find(candidate => candidate.Skill.SkillId == prerequisite);
                    if (parent.Skill == null)
                    {
                        continue;
                    }

                    var parentCell = Cell(parent.Skill);
                    links.Add((new Vector2(parentCell.x * ColumnWidth + NodeSize / 2f, parentCell.y * RowHeight + NodeSize),
                        new Vector2(cell.x * ColumnWidth + NodeSize / 2f, cell.y * RowHeight),
                        _skills.HasSkill(prerequisite)));
                }
            }

            _links.Set(links);
        }

        private static SkillNodeState StateOf(InventoryScreen.SkillNode node)
        {
            if (node.IsMaxRank) { return SkillNodeState.Max; }
            if (node.IsUnlocked) { return SkillNodeState.Unlocked; }
            return node.CanBuy ? SkillNodeState.Available : SkillNodeState.Locked;
        }

        private void DrawDetail(InventoryScreen.SkillNode node)
        {
            var skill = node.Skill;
            if (skill == null)
            {
                return;
            }

            _name.text = skill.DisplayName.ToUpperInvariant();
            _meta.text = ($"{(skill.IsActive ? "Активне вміння" : "Пасивне вміння")} · Ранг {node.Rank} / "
                + $"{skill.MaxRank} · Вартість {PointsText(skill.Cost)}").ToUpperInvariant();
            _desc.text = skill.Description ?? string.Empty;
            UiParts.Show(_desc, !string.IsNullOrWhiteSpace(skill.Description));

            // The rank being decided about: the next one, or the last once it is reached.
            var shownRank = Mathf.Min(skill.MaxRank, node.Rank + 1);
            _effectsTitle.text = skill.MaxRank > 1 ? $"ЕФЕКТ · РАНГ {shownRank}" : "ЕФЕКТ";
            _effects.Clear();
            var lines = new List<string>();
            skill.DescribeInto(lines, shownRank);
            foreach (var line in lines)
            {
                _effects.Add(UiParts.Text(line, "effect-line"));
            }

            DrawRequirements(skill, node);
            DrawSlots(skill);

            _buy.SetEnabled(node.CanBuy);
            _buyText.text = node.IsMaxRank ? "ВИВЧЕНО"
                : $"{(node.IsUnlocked ? "ПІДВИЩИТИ" : "ВИВЧИТИ")} ЗА {PointsText(skill.Cost).ToUpperInvariant()}";
            UiParts.Show(_assign, skill.IsActive);
            _assign.SetEnabled(CanAssign);
            _problem.text = !node.IsMaxRank && !node.CanBuy ? InventoryScreen.PurchaseProblem(node.State) : string.Empty;
            UiParts.Show(_problem, !string.IsNullOrEmpty(_problem.text));
        }

        private void DrawRequirements(SkillDefinition skill, InventoryScreen.SkillNode node)
        {
            _requirements.Clear();
            foreach (var prerequisite in skill.Prerequisites)
            {
                var name = _skills.Database.TryGetSkill(prerequisite, out var parent) ? parent.DisplayName : prerequisite;
                AddGate($"{name} — ранг 1", _skills.HasSkill(prerequisite));
            }

            if (skill.RequiredLevel > 1)
            {
                var level = Object.FindAnyObjectByType<PlayerProgressionService>();
                AddGate($"Рівень {skill.RequiredLevel}", level == null || level.Level >= skill.RequiredLevel);
            }

            if (!node.IsMaxRank)
            {
                AddGate($"Очок умінь: потрібно {skill.Cost}, у вас {_screen.SkillPoints}",
                    _screen.SkillPoints >= skill.Cost);
            }
        }

        private void AddGate(string text, bool met)
        {
            var label = UiParts.Text((met ? "✓ " : "✕ ") + text, "requirement");
            label.EnableInClassList("requirement--unmet", !met);
            _requirements.Add(label);
        }

        private void DrawSlots(SkillDefinition skill)
        {
            UiParts.Show(_slotsSection, skill.IsActive);
            if (!skill.IsActive)
            {
                return;
            }

            for (var i = 0; i < _slots.Length; i++)
            {
                var held = _screen.ActiveSlotSkillId(i);
                var name = !string.IsNullOrEmpty(held) && _skills.Database.TryGetSkill(held, out var inSlot)
                    ? inSlot.DisplayName
                    : "Порожньо";
                _slotNames[i].text = name;
                _slotNames[i].EnableInClassList("sk-slot__name--empty", string.IsNullOrEmpty(held));
                _slots[i].EnableInClassList("sk-slot--taken", !string.IsNullOrEmpty(held));
                _slots[i].SetEnabled(CanAssign);
            }
        }

        private static string PointsText(int points)
        {
            var tail = points % 10 == 1 && points % 100 != 11 ? "очко"
                : points % 10 >= 2 && points % 10 <= 4 && (points % 100 < 10 || points % 100 >= 20) ? "очки"
                : "очок";
            return $"{points} {tail}";
        }
    }
}
