using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The controls screen from design/ui/controls/controls.html, shared by the pause and the
    /// title screen the way <see cref="OptionsPanel"/> is. Every action has two keyboard cells
    /// and a pad cell; arrows or the stick pick a cell, Enter or A waits for a new key (Escape
    /// or Start cancel the wait), Backspace or X empties the second key, and «Скинути все» asks
    /// first. The rules — swapping, fixed keys — are <see cref="BindingEditor"/>'s; this draws
    /// them and says what happened.
    /// </summary>
    public sealed class ControlsPanel
    {
        private const string OpenClass = "controls--open";
        private const float PulsePeriod = 0.5f;

        private readonly VisualElement _panel;
        private readonly Action _onClosed;
        private readonly List<Button[]> _cells = new List<Button[]>();
        private readonly Label _note;
        private readonly Button _reset;
        private readonly VisualElement _confirm;
        private readonly Button[] _confirmButtons = new Button[2];
        private BindingEditor _editor;
        private MenuInput.Repeater _vertical;
        private MenuInput.Repeater _horizontal;
        private int _row;
        private int _slot;
        private int _confirmFocus;
        private int _listenEndedFrame = -1;
        private int _openedFrame = -1;
        private (int row, int slot) _swapped = (-1, -1);

        public ControlsPanel(VisualElement root, Action onClosed)
        {
            _panel = root?.Q("controls-panel");
            _onClosed = onClosed;
            if (_panel == null)
            {
                return;
            }

            _panel.Clear();
            var header = Box("controls__header");
            header.Add(Display("КЕРУВАННЯ", "controls__title"));
            _panel.Add(header);
            var body = Box("controls__body");
            var columns = Box("controls__cols");
            var left = Box("controls__col");
            var right = Box("controls__col");
            right.AddToClassList("controls__col--right");
            columns.Add(left);
            columns.Add(right);
            body.Add(columns);
            BuildRows(left, right);
            _note = UiParts.Text(string.Empty, "controls__note");
            body.Add(_note);
            _reset = BuildFooter(body);
            _panel.Add(body);
            _confirm = BuildConfirm();
            _panel.Add(_confirm);
        }

        public bool IsOpen => _panel != null && _panel.ClassListContains(OpenClass);
        public bool IsListening => _editor != null && _editor.IsListening;
        public bool IsConfirmOpen => _confirm != null && _confirm.ClassListContains("controls__confirm--open");

        /// <summary>
        /// True while a key is being waited for, and on the frame the wait ended: the Escape or
        /// Start that cancelled it must not also close the screen or the pause.
        /// </summary>
        public bool SwallowsCancel => IsListening || Time.frameCount == _listenEndedFrame;

        /// <summary>The note under the table; for tests.</summary>
        public string Note => _note != null && _note.ClassListContains("controls__note--shown") ? _note.text : string.Empty;

        public BindingEditor Editor => _editor;

        public void Open()
        {
            if (_panel == null || IsOpen)
            {
                return;
            }

            _editor = new BindingEditor();
            _panel.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            ShowNote(string.Empty, false);
            CloseConfirm();
            _row = 0;
            _slot = KeyBindings.PrimarySlot;
            Redraw();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _editor?.Dispose();
            _editor = null;
            _panel.RemoveFromClassList(OpenClass);
            _onClosed?.Invoke();
        }

        /// <summary>Escape or B: out of the wait, out of the question, or out of the screen.</summary>
        public void Back()
        {
            if (SwallowsCancel)
            {
                return;
            }

            if (IsConfirmOpen)
            {
                CloseConfirm();
                return;
            }

            Close();
        }

        // ---------- actions ----------

        /// <summary>Starts waiting for a key for one cell (Enter, A or a click).</summary>
        public bool BeginListening(int row, int slot)
        {
            if (_editor == null || row < 0 || row >= KeyBindings.Rows.Length)
            {
                return false;
            }

            Focus(row, slot);
            var target = KeyBindings.Rows[row];
            if (BindingEditor.IsFixed(target, slot))
            {
                ShowNote(slot == KeyBindings.PadSlot
                    ? $"«{target.Label}» на геймпаді не змінюється."
                    : $"«{target.Label}» не змінюється.", true);
                return false;
            }

            if (!_editor.Listen(target, slot, OnListened))
            {
                return false;
            }

            ShowNote(string.Empty, false);
            Redraw();
            return true;
        }

        /// <summary>What a finished wait or a direct assignment leads to on screen.</summary>
        public void Show(BindingOutcome outcome)
        {
            _swapped = (-1, -1);
            switch (outcome.Change)
            {
                case BindingChange.Swapped:
                    _swapped = (Array.IndexOf(KeyBindings.Rows, outcome.Other), outcome.OtherSlot);
                    ShowNote(outcome.Message, false);
                    break;
                case BindingChange.Refused:
                    ShowNote(outcome.Message, true);
                    break;
                default:
                    ShowNote(string.Empty, false);
                    break;
            }

            Redraw();
        }

        public void ClearAlternative(int row)
        {
            if (_editor != null && row >= 0 && row < KeyBindings.Rows.Length
                && _editor.ClearAlternative(KeyBindings.Rows[row]))
            {
                ShowNote(string.Empty, false);
                Redraw();
            }
        }

        public void AskReset()
        {
            _confirm.AddToClassList("controls__confirm--open");
            SetConfirmFocus(0);
        }

        public void ConfirmReset()
        {
            _editor?.ResetAll();
            CloseConfirm();
            ShowNote("Клавіші повернуто до стандартних.", false);
            Redraw();
        }

        private void CloseConfirm()
        {
            _confirm?.RemoveFromClassList("controls__confirm--open");
        }

        private void OnListened(BindingOutcome outcome)
        {
            _listenEndedFrame = Time.frameCount;
            Show(outcome);
        }

        // ---------- input ----------

        /// <summary>Keyboard and pad; run every frame by whoever holds the screen.</summary>
        public void Tick()
        {
            if (!IsOpen)
            {
                return;
            }

            if (IsListening)
            {
                Pulse();
                return;
            }

            // Not on the frame it opened or a wait ended: that press already did its job.
            if (Time.frameCount == _openedFrame || Time.frameCount == _listenEndedFrame)
            {
                return;
            }

            if (IsConfirmOpen)
            {
                var step = _horizontal.Step(MenuInput.ReadHorizontal());
                if (step != 0) { SetConfirmFocus(1 - _confirmFocus); }
                if (MenuInput.Confirmed())
                {
                    if (_confirmFocus == 1) { ConfirmReset(); } else { CloseConfirm(); }
                }

                return;
            }

            var vertical = _vertical.Step(MenuInput.ReadVertical());
            var horizontal = _horizontal.Step(MenuInput.ReadHorizontal());
            if (vertical != 0) { MoveRow(vertical); }
            if (horizontal != 0) { MoveSlot(horizontal); }

            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (OnReset)
            {
                if (MenuInput.Confirmed()) { AskReset(); }
                return;
            }

            if ((keyboard != null && keyboard.backspaceKey.wasPressedThisFrame)
                || (pad != null && pad.buttonWest.wasPressedThisFrame))
            {
                ClearAlternative(_row);
            }

            if (MenuInput.Confirmed())
            {
                BeginListening(_row, _slot);
            }
        }

        private bool OnReset => _row == KeyBindings.Rows.Length;

        private void MoveRow(int step)
        {
            var count = KeyBindings.Rows.Length + 1;
            for (var i = 1; i <= count; i++)
            {
                var next = ((_row + step * i) % count + count) % count;
                if (next == KeyBindings.Rows.Length || !BindingEditor.IsFixed(KeyBindings.Rows[next], _slot))
                {
                    Focus(next, _slot);
                    return;
                }
            }
        }

        private void MoveSlot(int step)
        {
            if (OnReset)
            {
                return;
            }

            for (var i = 1; i <= 3; i++)
            {
                var next = ((_slot + step * i) % 3 + 3) % 3;
                if (!BindingEditor.IsFixed(KeyBindings.Rows[_row], next))
                {
                    Focus(_row, next);
                    return;
                }
            }
        }

        private void Focus(int row, int slot)
        {
            _row = row;
            _slot = slot;
            _swapped = (-1, -1);
            Redraw();
        }

        private void Pulse()
        {
            if (_row >= _cells.Count)
            {
                return;
            }

            var on = Mathf.Repeat(Time.unscaledTime, PulsePeriod * 2f) < PulsePeriod;
            _cells[_row][_slot].EnableInClassList("bind--pulse", on);
        }

        // ---------- drawing ----------

        private void Redraw()
        {
            if (_editor == null)
            {
                return;
            }

            for (var r = 0; r < _cells.Count; r++)
            {
                var row = KeyBindings.Rows[r];
                for (var s = 0; s < 3; s++)
                {
                    var cell = _cells[r][s];
                    var focused = r == _row && s == _slot;
                    var listening = focused && IsListening;
                    cell.Clear();
                    cell.EnableInClassList("bind--focused", focused);
                    cell.EnableInClassList("bind--listening", listening);
                    cell.EnableInClassList("bind--pulse", false);
                    cell.EnableInClassList("bind--swapped", _swapped.row == r && _swapped.slot == s);
                    var isFixed = BindingEditor.IsFixed(row, s);
                    cell.EnableInClassList("bind--fixed", isFixed);
                    if (listening)
                    {
                        cell.Add(UiParts.Text("Натисніть клавішу…", "bind__listen"));
                        continue;
                    }

                    if (s == KeyBindings.PadSlot && row.IsPadFixed && !row.PadModifier)
                    {
                        if (row.PadFixedText.Length > 5)
                        {
                            cell.Add(UiParts.Text(row.PadFixedText, "bind__text"));
                        }
                        else
                        {
                            cell.Add(Glyph(row.PadFixedText, true));
                        }

                        continue;
                    }

                    if (s == KeyBindings.PadSlot && row.PadModifier)
                    {
                        cell.Add(UiParts.Text("LT +", "bind__text"));
                    }

                    var glyph = _editor.Glyph(row, s);
                    cell.Add(string.IsNullOrEmpty(glyph)
                        ? UiParts.Text("—", "bind__text")
                        : Glyph(glyph, s == KeyBindings.PadSlot));
                }
            }

            _reset?.EnableInClassList("btn--focused", OnReset);
        }

        private static Label Glyph(string text, bool pad)
        {
            var label = UiParts.Text(text, "key");
            label.EnableInClassList("key--pad", pad);
            return label;
        }

        private void ShowNote(string text, bool warn)
        {
            if (_note == null)
            {
                return;
            }

            _note.text = text;
            _note.EnableInClassList("controls__note--shown", !string.IsNullOrEmpty(text));
            _note.EnableInClassList("controls__note--warn", warn);
        }

        // ---------- building ----------

        private void BuildRows(VisualElement left, VisualElement right)
        {
            left.Add(Head());
            right.Add(Head());
            string section = null;
            for (var r = 0; r < KeyBindings.Rows.Length; r++)
            {
                var row = KeyBindings.Rows[r];
                var column = r < KeyBindings.LeftColumnRows ? left : right;
                if (row.Section != section)
                {
                    section = row.Section;
                    column.Add(Display(section.ToUpperInvariant(), "controls__section"));
                }

                var line = Box("controls__row");
                line.AddToClassList("controls__action");
                line.Add(UiParts.Text(row.Label, "controls__name"));
                var cells = new Button[3];
                for (var s = 0; s < 3; s++)
                {
                    var index = r;
                    var slot = s;
                    var cell = new Button();
                    cell.AddToClassList("bind");
                    cell.EnableInClassList("bind--pad", s == KeyBindings.PadSlot);
                    cell.clicked += () =>
                    {
                        if (!IsListening) { BeginListening(index, slot); }
                    };
                    line.Add(cell);
                    cells[s] = cell;
                }

                _cells.Add(cells);
                column.Add(line);
            }
        }

        private static VisualElement Head()
        {
            var head = Box("controls__row");
            head.AddToClassList("controls__head");
            var name = UiParts.Text("ДІЯ", "controls__head-text");
            name.AddToClassList("controls__name");
            head.Add(name);
            foreach (var (text, pad) in new[] { ("КЛАВІША", false), ("ДОДАТКОВА", false), ("ГЕЙМПАД", true) })
            {
                var cell = UiParts.Text(text, "controls__head-text");
                cell.AddToClassList("controls__cell-head");
                cell.EnableInClassList("controls__cell-head--pad", pad);
                head.Add(cell);
            }

            return head;
        }

        private Button BuildFooter(VisualElement body)
        {
            var foot = Box("controls__foot");
            foot.Add(Hint("Змінити", ("Enter", false), ("A", true)));
            foot.Add(Hint("Очистити додаткову", ("Backspace", false), ("X", true)));
            foot.Add(Hint("Назад", ("Esc", false), ("B", true)));
            foot.Add(Box("controls__spacer"));
            var reset = new Button();
            reset.AddToClassList("btn");
            reset.AddToClassList("btn--last");
            reset.AddToClassList("controls__reset");
            reset.Add(Display("СКИНУТИ ВСЕ", "btn__text"));
            reset.clicked += AskReset;
            foot.Add(reset);
            body.Add(foot);
            return reset;
        }

        private static VisualElement Hint(string text, params (string key, bool pad)[] keys)
        {
            var hint = Box("controls__hint");
            foreach (var (key, pad) in keys)
            {
                hint.Add(Glyph(key, pad));
            }

            hint.Add(UiParts.Text(text, "controls__hint-text"));
            return hint;
        }

        private VisualElement BuildConfirm()
        {
            var confirm = Box("controls__confirm");
            var panel = Box("controls__confirm-panel");
            var header = Box("controls__header");
            header.Add(Display("СКИНУТИ КЛАВІШІ?", "controls__title"));
            panel.Add(header);
            var body = Box("controls__body");
            body.Add(UiParts.Text(
                "Усі дії повернуться до стандартних клавіш і кнопок. Інші налаштування не зміняться.",
                "controls__confirm-text"));
            var row = Box("controls__confirm-row");
            _confirmButtons[0] = ConfirmButton("СКАСУВАТИ", CloseConfirm, "btn--primary");
            _confirmButtons[1] = ConfirmButton("СКИНУТИ", ConfirmReset, "btn--danger");
            row.Add(_confirmButtons[0]);
            row.Add(_confirmButtons[1]);
            body.Add(row);
            panel.Add(body);
            confirm.Add(panel);
            return confirm;
        }

        private static Button ConfirmButton(string text, Action action, string style)
        {
            var button = new Button();
            button.AddToClassList("btn");
            button.AddToClassList("btn--last");
            button.AddToClassList(style);
            button.Add(Display(text, "btn__text"));
            button.clicked += action;
            return button;
        }

        private void SetConfirmFocus(int index)
        {
            _confirmFocus = index;
            for (var i = 0; i < _confirmButtons.Length; i++)
            {
                _confirmButtons[i].EnableInClassList("btn--focused", i == index);
            }
        }

        /// <summary>Titles, section names and buttons use the display face, as everywhere else.</summary>
        private static Label Display(string text, string className)
        {
            var label = UiParts.Text(text, className);
            label.AddToClassList("display");
            return label;
        }

        private static VisualElement Box(string className)
        {
            var box = new VisualElement();
            box.AddToClassList(className);
            return box;
        }
    }
}
