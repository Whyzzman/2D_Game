using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The "Закляття" page of the inventory window, from design/ui/spellbook/spellbook.html:
    /// every spell of the book — the learned ones to choose from, the others greyed with the
    /// skill that teaches them — the three slots that match the keys in combat, and the chosen
    /// spell's numbers as this character would cast it. Arrows pick a spell, 1–3 or ←/→ the
    /// slot, Enter or A put it there, Backspace or X take it off.
    /// </summary>
    public sealed class SpellsPage
    {
        private static readonly string[] SlotActions = { "CastSpell1", "CastSpell2", "CastSpell3" };

        private readonly VisualElement _list;
        private readonly Button[] _slotCards = new Button[PlayerSpellbook.SlotCount];
        private readonly Label[] _slotKeys = new Label[PlayerSpellbook.SlotCount];
        private readonly Label[] _slotNames = new Label[PlayerSpellbook.SlotCount];
        private readonly SpellIcon[] _slotIcons = new SpellIcon[PlayerSpellbook.SlotCount];
        private readonly Label _name;
        private readonly Label _meta;
        private readonly Label _desc;
        private readonly VisualElement _stats;
        private readonly Button _assign;
        private readonly Label _assignText;
        private readonly Button _remove;
        private readonly Label _problem;
        private readonly System.Action _changed;
        private readonly List<SpellDefinition> _spells = new List<SpellDefinition>();

        private InventoryScreen _screen;
        private PlayerSpellbook _book;
        private PlayerSkills _skills;
        private SpellCaster _caster;
        private int _selected;
        private int _target;

        internal SpellsPage(VisualElement page, System.Action changed)
        {
            _changed = changed;
            _list = page.Q("sp-list");
            for (var i = 0; i < _slotCards.Length; i++)
            {
                var slot = i;
                _slotCards[i] = page.Q<Button>($"sp-slot-{i}");
                _slotKeys[i] = page.Q<Label>($"sp-slot-{i}-key");
                _slotNames[i] = page.Q<Label>($"sp-slot-{i}-name");
                _slotIcons[i] = new SpellIcon();
                _slotCards[i]?.Q("sp-slot-" + i + "-frame")?.Add(_slotIcons[i]);
                if (_slotCards[i] != null) { _slotCards[i].clicked += () => Target(slot); }
            }

            _name = page.Q<Label>("sp-name");
            _meta = page.Q<Label>("sp-meta");
            _desc = page.Q<Label>("sp-desc");
            _stats = page.Q("sp-stats");
            _assign = page.Q<Button>("sp-assign");
            _assignText = _assign?.Q<Label>();
            if (_assign != null) { _assign.clicked += () => Assign(); }
            _remove = page.Q<Button>("sp-remove");
            if (_remove != null) { _remove.clicked += () => Remove(); }
            _problem = page.Q<Label>("sp-problem");
        }

        public string SelectedSpellId => _selected >= 0 && _selected < _spells.Count ? _spells[_selected].SpellId : string.Empty;
        public int TargetSlot => _target;
        public int SpellCount => _spells.Count;

        public void Attach(InventoryScreen screen, PlayerCharacter player)
        {
            _screen = screen;
            _book = Object.FindAnyObjectByType<PlayerSpellbook>();
            _skills = Object.FindAnyObjectByType<PlayerSkills>();
            _caster = player != null ? player.GetComponent<SpellCaster>() : null;
        }

        public void Select(string spellId)
        {
            var index = _spells.FindIndex(spell => spell.SpellId == spellId);
            if (index >= 0)
            {
                _selected = index;
                _changed?.Invoke();
            }
        }

        public void Target(int slot)
        {
            _target = Mathf.Clamp(slot, 0, PlayerSpellbook.SlotCount - 1);
            _changed?.Invoke();
        }

        /// <summary>Puts the chosen spell into the target slot; only a learned one goes in.</summary>
        public bool Assign()
        {
            var id = SelectedSpellId;
            return _screen != null && _book != null && _book.Knows(id) && _screen.AssignSpell(_target, id);
        }

        /// <summary>Takes the chosen spell out of whichever slot holds it.</summary>
        public bool Remove()
        {
            var slot = SlotOf(SelectedSpellId);
            return slot >= 0 && _screen != null && _screen.AssignSpell(slot, string.Empty);
        }

        public void Tick(Keyboard keyboard, Gamepad pad, int horizontal, int vertical)
        {
            if (vertical != 0 && _spells.Count > 0)
            {
                _selected = Mathf.Clamp(_selected + vertical, 0, _spells.Count - 1);
                _changed?.Invoke();
            }

            if (horizontal != 0)
            {
                Target((_target + horizontal + PlayerSpellbook.SlotCount) % PlayerSpellbook.SlotCount);
            }

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) { Target(0); }
                if (keyboard.digit2Key.wasPressedThisFrame) { Target(1); }
                if (keyboard.digit3Key.wasPressedThisFrame) { Target(2); }
            }

            if ((keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
                || (pad != null && pad.buttonWest.wasPressedThisFrame))
            {
                Remove();
            }

            if (MenuInput.Confirmed())
            {
                Assign();
            }
        }

        // ---------- drawing ----------

        public void Draw()
        {
            if (_book == null || _book.Database == null)
            {
                return;
            }

            _spells.Clear();
            foreach (var spell in _book.Database.Spells)
            {
                if (spell != null) { _spells.Add(spell); }
            }

            // Learned first, in book order; the rest after them.
            _spells.Sort((a, b) => (_book.Knows(a.SpellId) ? 0 : 1).CompareTo(_book.Knows(b.SpellId) ? 0 : 1));
            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _spells.Count - 1));
            DrawList();
            DrawSlots();
            DrawDetail();
        }

        private void DrawList()
        {
            _list.Clear();
            for (var i = 0; i < _spells.Count; i++)
            {
                var spell = _spells[i];
                var known = _book.Knows(spell.SpellId);
                var row = new Button();
                row.AddToClassList("sp-spell");
                row.EnableInClassList("sp-spell--selected", i == _selected);
                row.EnableInClassList("sp-spell--locked", !known);
                var frame = new VisualElement { pickingMode = PickingMode.Ignore };
                frame.AddToClassList("sp-spell__icon");
                var icon = new SpellIcon();
                icon.Show(spell, known);
                frame.Add(icon);
                row.Add(frame);
                var body = new VisualElement { pickingMode = PickingMode.Ignore };
                body.AddToClassList("sp-spell__body");
                body.Add(UiParts.Text(spell.DisplayName, "sp-spell__name"));
                var meta = UiParts.Text(known ? Meta(spell) : Locked(spell), "sp-spell__meta");
                if (known) { meta.AddToClassList(ElementClass(spell.Element)); }
                body.Add(meta);
                row.Add(body);
                var slot = SlotOf(spell.SpellId);
                var tag = UiParts.Text(slot >= 0 ? $"СЛОТ {slot + 1}" : "—", "sp-spell__slot");
                tag.EnableInClassList("sp-spell__slot--empty", slot < 0);
                row.Add(tag);
                var index = i;
                row.clicked += () =>
                {
                    _selected = index;
                    _changed?.Invoke();
                };
                _list.Add(row);
            }
        }

        private void DrawSlots()
        {
            var catalogue = _book.Database;
            for (var i = 0; i < _slotCards.Length; i++)
            {
                if (_slotCards[i] == null) { continue; }
                var id = _book.SlotSpellId(i);
                var spell = !string.IsNullOrEmpty(id) ? catalogue.GetSpell(id) : null;
                _slotCards[i].EnableInClassList("sp-slot--target", i == _target);
                _slotKeys[i].text = $"КЛАВІША {KeyBindings.Glyph(SlotActions[i], gamepad: false)}";
                _slotNames[i].text = spell != null ? spell.DisplayName : "Порожньо";
                _slotNames[i].EnableInClassList("sp-slot__name--empty", spell == null);
                _slotIcons[i].Show(spell, true);
                UiParts.Show(_slotIcons[i], spell != null);
            }
        }

        private void DrawDetail()
        {
            if (_spells.Count == 0)
            {
                return;
            }

            var spell = _spells[_selected];
            var known = _book.Knows(spell.SpellId);
            _name.text = spell.DisplayName.ToUpperInvariant();
            _meta.text = $"{ResistanceProfile.Display(spell.Element)} · {TypeName(spell.Type)}".ToUpperInvariant();
            foreach (var element in new[] { "sp-el-fire", "sp-el-frost", "sp-el-arcane", "sp-el-dark" })
            {
                _meta.RemoveFromClassList(element);
            }

            _meta.AddToClassList(ElementClass(spell.Element));
            _desc.text = spell.Description ?? string.Empty;
            UiParts.Show(_desc, !string.IsNullOrWhiteSpace(spell.Description));

            _stats.Clear();
            var damage = _caster != null ? _caster.Evaluate(spell).Total : spell.BaseDamage;
            var cost = _caster != null ? _caster.ManaCostOf(spell) : spell.ManaCost;
            var castTime = _caster != null ? _caster.CastTimeOf(spell) : spell.CastTime;
            if (spell.Type == SpellType.SelfBuff)
            {
                UiParts.StatRow(_stats, "Зниження шкоди", $"{spell.DamageReduction * 100f:0}%");
                UiParts.StatRow(_stats, "Триває", $"{spell.BuffDuration:0.#} с");
            }
            else
            {
                UiParts.StatRow(_stats, "Шкода", damage.ToString("0"));
            }

            UiParts.StatRow(_stats, "Стихія", ResistanceProfile.Display(spell.Element));
            UiParts.StatRow(_stats, "Вартість мани", cost.ToString("0.#"));
            UiParts.StatRow(_stats, "Час читання", $"{castTime:0.00} с");
            UiParts.StatRow(_stats, "Відкат", $"{spell.Cooldown:0.0} с");
            if (spell.Type != SpellType.SelfBuff)
            {
                UiParts.StatRow(_stats, "Масштаб. Інтелект", ItemComparison.ScalingGrade(spell.IntelligenceScaling));
            }

            if (spell.StatusBuildup != null)
            {
                foreach (var buildup in spell.StatusBuildup)
                {
                    UiParts.StatRow(_stats, $"Накопичення: {StatusName(buildup.type)}", buildup.amount.ToString("0"));
                }
            }

            _assignText.text = $"ПРИЗНАЧИТИ В СЛОТ {_target + 1}";
            _assign.SetEnabled(known && _book.SlotSpellId(_target) != spell.SpellId);
            _remove.SetEnabled(SlotOf(spell.SpellId) >= 0);
            _problem.text = known ? string.Empty : Locked(spell) + ".";
            UiParts.Show(_problem, !known);
        }

        // ---------- helpers ----------

        private int SlotOf(string spellId)
        {
            if (_book == null || string.IsNullOrEmpty(spellId)) { return -1; }
            for (var i = 0; i < PlayerSpellbook.SlotCount; i++)
            {
                if (_book.SlotSpellId(i) == spellId) { return i; }
            }

            return -1;
        }

        private string Meta(SpellDefinition spell)
        {
            var cost = _caster != null ? _caster.ManaCostOf(spell) : spell.ManaCost;
            var time = _caster != null ? _caster.CastTimeOf(spell) : spell.CastTime;
            return $"{ResistanceProfile.Display(spell.Element)} · {cost:0} мани · {time:0.00} с".ToUpperInvariant();
        }

        /// <summary>Why a spell is not in the book yet: the skill that teaches it.</summary>
        private string Locked(SpellDefinition spell)
        {
            if (_skills?.Database != null)
            {
                foreach (var skill in _skills.Database.Skills)
                {
                    if (skill != null && skill.GrantedSpellId == spell.SpellId)
                    {
                        return $"Потрібне вміння «{skill.DisplayName}»";
                    }
                }
            }

            return "Ще не вивчено";
        }

        private static string TypeName(SpellType type)
        {
            switch (type)
            {
                case SpellType.Projectile: return "Снаряд";
                case SpellType.Area: return "Вибух довкола";
                case SpellType.SelfBuff: return "На себе";
                case SpellType.Beam: return "Промінь";
                case SpellType.Summon: return "Виклик";
                default: return "Рух";
            }
        }

        private static string StatusName(StatusType status)
        {
            switch (status)
            {
                case StatusType.Bleed: return "кровотеча";
                case StatusType.Poison: return "отрута";
                case StatusType.Burn: return "горіння";
                default: return "холод";
            }
        }

        internal static string ElementClass(DamageType element)
        {
            switch (element)
            {
                case DamageType.Fire: return "sp-el-fire";
                case DamageType.Frost: return "sp-el-frost";
                case DamageType.Dark: return "sp-el-dark";
                default: return "sp-el-arcane";
            }
        }
    }

    /// <summary>A spell's picture by its element until it has painted art: flame, shard, star, shield.</summary>
    public sealed class SpellIcon : VisualElement
    {
        private SpellDefinition _spell;
        private bool _known;

        public SpellIcon()
        {
            AddToClassList("sp-icon");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Show(SpellDefinition spell, bool known)
        {
            _spell = spell;
            _known = known;
            style.backgroundImage = spell != null && spell.Icon != null
                ? new StyleBackground(spell.Icon)
                : new StyleBackground(StyleKeyword.None);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_spell == null || _spell.Icon != null || contentRect.width < 1f)
            {
                return;
            }

            var p = context.painter2D;
            var s = Mathf.Min(contentRect.width, contentRect.height) / 24f;
            var o = new Vector2((contentRect.width - 24f * s) * 0.5f, (contentRect.height - 24f * s) * 0.5f);
            Vector2 At(float x, float y) => o + new Vector2(x, y) * s;
            Color ink = _known ? (Color)ColorFor(_spell.Element) : (Color)new Color32(142, 131, 117, 255);
            p.fillColor = ink;
            p.strokeColor = ink;
            p.lineWidth = 1.8f * s;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            if (_spell.Type == SpellType.SelfBuff)
            {
                p.MoveTo(At(12f, 3f)); p.LineTo(At(19f, 6f)); p.LineTo(At(19f, 12f));
                p.BezierCurveTo(At(19f, 16f), At(16f, 19f), At(12f, 21f));
                p.BezierCurveTo(At(8f, 19f), At(5f, 16f), At(5f, 12f));
                p.LineTo(At(5f, 6f)); p.ClosePath(); p.Stroke();
                return;
            }

            switch (_spell.Element)
            {
                case DamageType.Fire:
                    p.MoveTo(At(12f, 2.5f));
                    p.BezierCurveTo(At(13f, 7f), At(18f, 9f), At(18f, 14.5f));
                    p.BezierCurveTo(At(18f, 18.5f), At(15.3f, 21f), At(12f, 21f));
                    p.BezierCurveTo(At(8.7f, 21f), At(6f, 18.5f), At(6f, 14.5f));
                    p.BezierCurveTo(At(6f, 11f), At(9f, 10f), At(9.5f, 7f));
                    p.BezierCurveTo(At(11f, 9f), At(11.5f, 6f), At(12f, 2.5f));
                    p.ClosePath(); p.Fill();
                    break;
                case DamageType.Frost:
                    for (var i = 0; i < 3; i++)
                    {
                        var a = i * Mathf.PI / 3f;
                        var d = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 9f;
                        p.MoveTo(At(12f - d.x, 12f - d.y));
                        p.LineTo(At(12f + d.x, 12f + d.y));
                    }

                    p.Stroke();
                    break;
                default:
                    for (var i = 0; i < 8; i++)
                    {
                        var a = i * Mathf.PI / 4f - Mathf.PI / 2f;
                        var r = i % 2 == 0 ? 9f : 3.5f;
                        var point = At(12f + Mathf.Cos(a) * r, 12f + Mathf.Sin(a) * r);
                        if (i == 0) { p.MoveTo(point); } else { p.LineTo(point); }
                    }

                    p.ClosePath(); p.Fill();
                    break;
            }
        }

        private static Color32 ColorFor(DamageType element)
        {
            switch (element)
            {
                // ui.css --burn, --frost, --arcane; dark borrows the unique rarity's violet.
                case DamageType.Fire: return new Color32(255, 140, 51, 255);
                case DamageType.Frost: return new Color32(143, 214, 255, 255);
                case DamageType.Dark: return new Color32(155, 107, 214, 255);
                default: return new Color32(107, 143, 255, 255);
            }
        }
    }
}
