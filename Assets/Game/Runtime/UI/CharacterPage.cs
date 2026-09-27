using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The "Персонаж" page of the inventory window, from design/ui/character/character.html:
    /// level and experience, the six attributes with a "+" each, what one more point in the
    /// focused attribute would give, the derived numbers, the current state and the status
    /// resistances. Points are spent at once and never refunded; the window saves on closing.
    /// </summary>
    public sealed class CharacterPage
    {
        private static readonly StatusType[] Statuses =
        {
            StatusType.Bleed, StatusType.Poison, StatusType.Burn, StatusType.Frost
        };

        private readonly Label _level;
        private readonly Label _experience;
        private readonly VisualElement _experienceFill;
        private readonly VisualElement _points;
        private readonly Label _pointsValue;
        private readonly List<VisualElement> _rows = new List<VisualElement>();
        private readonly List<Label> _values = new List<Label>();
        private readonly List<Label> _previews = new List<Label>();
        private readonly List<Button> _adds = new List<Button>();
        private readonly Label _tipTitle;
        private readonly Label _tipHead;
        private readonly Label _tipText;
        private readonly VisualElement _tipList;
        private readonly VisualElement _derived;
        private readonly VisualElement _state;
        private readonly VisualElement _resistances;
        private readonly System.Action _changed;

        private InventoryScreen _screen;
        private PlayerCharacter _player;
        private PlayerStats _stats;
        private PlayerProgressionService _progression;
        private SpellCaster _caster;
        private PlayerSpellbook _spellbook;
        private int _focus;

        internal CharacterPage(VisualElement page, System.Action changed)
        {
            _changed = changed;
            _level = page.Q<Label>("ch-level");
            _experience = page.Q<Label>("ch-xp");
            _experienceFill = page.Q("ch-xp-fill");
            _points = page.Q("ch-points");
            _pointsValue = page.Q<Label>("ch-points-value");
            _tipTitle = page.Q<Label>("ch-tip-title");
            _tipHead = page.Q<Label>("ch-tip-head");
            _tipText = page.Q<Label>("ch-tip-text");
            _tipList = page.Q("ch-tip-list");
            _derived = page.Q("ch-derived");
            _state = page.Q("ch-state");
            _resistances = page.Q("ch-resist");
            BuildRows(page.Q("ch-stats"));
        }

        public CharacterStat Focused => CharacterStats.Allocatable[_focus];

        public void Attach(InventoryScreen screen, PlayerCharacter player,
            PlayerProgressionService progression)
        {
            _screen = screen;
            _player = player;
            _stats = player != null ? player.Stats : null;
            _progression = progression;
            _caster = player != null ? player.GetComponent<SpellCaster>() : null;
            _spellbook = Object.FindAnyObjectByType<PlayerSpellbook>();
        }

        public void Focus(CharacterStat stat)
        {
            _focus = Mathf.Max(0, System.Array.IndexOf(CharacterStats.Allocatable, stat));
            _changed?.Invoke();
        }

        /// <summary>Spends a point in the attribute; the only way this page changes the character.</summary>
        public bool Allocate(CharacterStat stat)
        {
            Focus(stat);
            return _screen != null && _screen.AllocatePoint(stat);
        }

        public void Tick(int vertical)
        {
            if (vertical != 0)
            {
                _focus = Mathf.Clamp(_focus + vertical, 0, CharacterStats.Allocatable.Length - 1);
                _changed?.Invoke();
            }

            if (MenuInput.Confirmed())
            {
                Allocate(Focused);
            }
        }

        public void Draw()
        {
            if (_progression == null || _stats == null)
            {
                return;
            }

            _level.text = _progression.Level.ToString();
            var need = _progression.ExperienceForNextLevel;
            _experience.text = _progression.IsMaxLevel
                ? "Максимальний рівень"
                : $"{_progression.Experience:N0} / {need:N0} досвіду";
            _experienceFill.style.width = Length.Percent(_progression.IsMaxLevel || need <= 0
                ? 100f : Mathf.Clamp01((float)_progression.Experience / need) * 100f);
            var points = _progression.StatPoints;
            _pointsValue.text = points.ToString();
            _points.EnableInClassList("ch-points--none", points == 0);

            for (var i = 0; i < _rows.Count; i++)
            {
                var stat = CharacterStats.Allocatable[i];
                var focused = i == _focus;
                _rows[i].EnableInClassList("ch-stat--focused", focused);
                _values[i].text = _stats.GetFinalStat(stat).ToString();
                _previews[i].text = focused && _progression.CanAllocate(stat)
                    ? $"→ {_stats.GetFinalStat(stat) + 1}"
                    : string.Empty;
                _adds[i].SetEnabled(_progression.CanAllocate(stat));
            }

            DrawTip();
            UiParts.DerivedStats(_derived, _stats, withLoad: true);
            DrawState();
        }

        private void DrawTip()
        {
            var stat = Focused;
            var name = CharacterStats.Display(stat);
            var value = _stats.GetFinalStat(stat);
            _tipTitle.text = $"ЩО ДАЄ {name.ToUpperInvariant()}";
            _tipHead.text = $"{name} {value} → {value + 1}";
            _tipText.text = CharacterStats.Describe(stat);
            _tipList.Clear();
            foreach (var line in StatPreview.Lines(stat, _stats, _player.Equipment, _caster, _spellbook))
            {
                _tipList.Add(UiParts.Text("• " + line, "ch-tip__line"));
            }

            if (_progression.StatPoints > 0 && !_progression.CanAllocate(stat))
            {
                _tipList.Add(UiParts.Text("Досягнуто межі цієї характеристики.", "ch-tip__limit"));
            }
        }

        private void DrawState()
        {
            _state.Clear();
            var health = _player.Health;
            if (health != null) { UiParts.StatRow(_state, "Здоров'я", $"{health.Current:0} / {health.Max:0}"); }
            var mana = _player.GetComponent<Mana>();
            if (mana != null) { UiParts.StatRow(_state, "Мана", $"{mana.Current:0} / {mana.Max:0}"); }
            var stamina = _player.Stamina;
            if (stamina != null) { UiParts.StatRow(_state, "Витривалість", $"{stamina.Current:0} / {stamina.Max:0}"); }

            _resistances.Clear();
            foreach (var status in Statuses)
            {
                UiParts.StatRow(_resistances, StatusName(status),
                    $"{_stats.StatusResistance(status) * 100f:0}%");
            }
        }

        private static string StatusName(StatusType status)
        {
            switch (status)
            {
                case StatusType.Bleed: return "Кровотеча";
                case StatusType.Poison: return "Отрута";
                case StatusType.Burn: return "Опік";
                default: return "Холод";
            }
        }

        private void BuildRows(VisualElement container)
        {
            for (var i = 0; i < CharacterStats.Allocatable.Length; i++)
            {
                var stat = CharacterStats.Allocatable[i];
                var row = new VisualElement();
                row.AddToClassList("ch-stat");
                var names = new VisualElement { pickingMode = PickingMode.Ignore };
                names.AddToClassList("ch-stat__names");
                names.Add(UiParts.Text(CharacterStats.Display(stat), "ch-stat__name"));
                names.Add(UiParts.Text(CharacterStats.Hint(stat), "ch-stat__hint"));
                row.Add(names);
                var value = UiParts.Text(string.Empty, "ch-stat__value");
                value.AddToClassList("display");
                row.Add(value);
                var preview = UiParts.Text(string.Empty, "ch-stat__preview");
                row.Add(preview);
                var add = new Button { text = "+" };
                add.AddToClassList("ch-stat__add");
                add.clicked += () => Allocate(stat);
                row.Add(add);
                row.RegisterCallback<PointerDownEvent>(_ => Focus(stat));
                container?.Add(row);
                _rows.Add(row);
                _values.Add(value);
                _previews.Add(preview);
                _adds.Add(add);
            }
        }
    }
}
