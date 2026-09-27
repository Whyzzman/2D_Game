using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    /// <summary>One line of the controls screen: an action, or one direction of moving.</summary>
    public sealed class BindingRow
    {
        public BindingRow(string section, string label, string action, string part = null,
            bool padModifier = false, string padFixed = null, bool keyboardFixed = false)
        {
            Section = section;
            Label = label;
            Action = action;
            Part = part;
            PadModifier = padModifier;
            PadFixedText = padFixed;
            KeyboardFixed = keyboardFixed;
        }

        public string Section { get; }
        public string Label { get; }
        /// <summary>The action in the Player map.</summary>
        public string Action { get; }
        /// <summary>For moving: the direction of the WASD composite; null for plain buttons.</summary>
        public string Part { get; }
        /// <summary>On a pad this action is LT plus a button; only the button changes.</summary>
        public bool PadModifier { get; }
        /// <summary>Not changeable on a pad (the stick moves, Start pauses); what to show instead.</summary>
        public string PadFixedText { get; }
        public bool KeyboardFixed { get; }
        public bool IsPadFixed => PadFixedText != null;
    }

    /// <summary>
    /// The player's own keys and buttons (design/ui/controls/controls.html). They are stored with
    /// the settings, not the save — a new game keeps them — as the Input System's override JSON,
    /// and every copy of the actions the game uses (<see cref="PlayerInputReader"/>, the glyphs)
    /// applies them and follows <see cref="Changed"/>.
    ///
    /// Each action has two keyboard slots (a key or a mouse button) and one pad button. Fixed on
    /// purpose: pause, moving with the stick, the LT modifier and every key inside the menus, so
    /// a player can never lock themselves out of a menu.
    /// </summary>
    public static class KeyBindings
    {
        public const string KeyboardGroup = "Keyboard&Mouse";
        public const string GamepadGroup = "Gamepad";
        public const int PrimarySlot = 0;
        public const int AlternativeSlot = 1;
        public const int PadSlot = 2;

        /// <summary>The screen's rows, in order: two columns of two sections each.</summary>
        public static readonly BindingRow[] Rows =
        {
            new BindingRow("Рух", "Вгору", "Move", "up", padFixed: "лівий стік"),
            new BindingRow("Рух", "Вниз", "Move", "down", padFixed: "лівий стік"),
            new BindingRow("Рух", "Вліво", "Move", "left", padFixed: "лівий стік"),
            new BindingRow("Рух", "Вправо", "Move", "right", padFixed: "лівий стік"),
            new BindingRow("Рух", "Стрибок", "Jump"),
            new BindingRow("Рух", "Ривок", "Dash"),
            new BindingRow("Бій", "Атака", "LightAttack"),
            new BindingRow("Бій", "Важка атака", "HeavyAttack"),
            new BindingRow("Бій", "Блок / парирування", "Block"),
            new BindingRow("Бій", "Фляга", "Heal"),
            new BindingRow("Уміння й закляття", "Уміння 1", "ActiveSkill1", padModifier: true),
            new BindingRow("Уміння й закляття", "Уміння 2", "ActiveSkill2", padModifier: true),
            new BindingRow("Уміння й закляття", "Закляття 1", "CastSpell1", padModifier: true),
            new BindingRow("Уміння й закляття", "Закляття 2", "CastSpell2", padModifier: true),
            new BindingRow("Уміння й закляття", "Закляття 3", "CastSpell3", padModifier: true),
            new BindingRow("Уміння й закляття", "Зміна зброї", "SwitchWeapon"),
            new BindingRow("Інше", "Взаємодія", "Interact"),
            new BindingRow("Інше", "Спорядження", "Inventory"),
            new BindingRow("Інше", "Карта", "Map"),
            new BindingRow("Інше", "Пауза", "Pause", padFixed: "Start", keyboardFixed: true)
        };

        /// <summary>Rows in the left column; the rest go to the right one.</summary>
        public const int LeftColumnRows = 10;

        private static InputActionAsset _display;

        /// <summary>Raised when the player's keys change; everything showing or reading keys follows.</summary>
        public static event Action Changed;

        /// <summary>The project's actions, as authored: every copy starts from these.</summary>
        public static InputActionAsset Template => InputSystem.actions;

        public static string OverridesJson => GameSettings.Current.bindingOverrides ?? string.Empty;

        /// <summary>Puts the player's keys on a copy of the actions (and takes old ones off).</summary>
        public static void ApplyTo(InputActionAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            asset.RemoveAllBindingOverrides();
            var json = OverridesJson;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    asset.LoadBindingOverridesFromJson(json);
                }
                catch (Exception error)
                {
                    // A damaged entry must not take the controls with it: defaults still work.
                    Debug.LogWarning($"Key bindings could not be read ({error.Message}); using defaults.");
                }
            }
        }

        /// <summary>Stores the overrides of an edited copy and tells everyone.</summary>
        public static void Store(InputActionAsset edited)
        {
            var json = edited != null ? edited.SaveBindingOverridesAsJson() : string.Empty;
            GameSettings.Set(data => data.bindingOverrides = json);
            NotifyChanged();
        }

        public static void ResetAll()
        {
            GameSettings.Set(data => data.bindingOverrides = string.Empty);
            NotifyChanged();
        }

        private static void NotifyChanged()
        {
            if (_display != null)
            {
                ApplyTo(_display);
            }

            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Changed = null;
            _display = null;
        }

        // ---------- finding a slot's binding ----------

        public static bool IsKeyboard(InputBinding binding) =>
            binding.groups != null && binding.groups.Contains(KeyboardGroup);

        public static bool IsGamepad(InputBinding binding) =>
            binding.groups != null && binding.groups.Contains(GamepadGroup);

        /// <summary>The binding index behind a slot of a row in this action, or -1.</summary>
        public static int BindingIndex(InputAction action, BindingRow row, int slot)
        {
            if (action == null)
            {
                return -1;
            }

            var bindings = action.bindings;
            if (slot == PadSlot)
            {
                if (row.IsPadFixed && !row.PadModifier)
                {
                    return -1;
                }

                for (var i = 0; i < bindings.Count; i++)
                {
                    var b = bindings[i];
                    if (!IsGamepad(b)) { continue; }
                    if (row.PadModifier ? b.isPartOfComposite && b.name == "binding"
                        : !b.isComposite && !b.isPartOfComposite)
                    {
                        return i;
                    }
                }

                return -1;
            }

            var seen = 0;
            for (var i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (!IsKeyboard(b)) { continue; }
                var fits = row.Part != null
                    ? b.isPartOfComposite && b.name == row.Part
                    : !b.isComposite && !b.isPartOfComposite;
                if (!fits) { continue; }
                if (seen == slot) { return i; }
                seen++;
            }

            return -1;
        }

        // ---------- what the player reads ----------

        /// <summary>The key an action answers to now, as a glyph: "Space", "ЛКМ", "RT", "▲".</summary>
        public static string Glyph(string action, bool gamepad)
        {
            if (_display == null && Template != null)
            {
                _display = UnityEngine.Object.Instantiate(Template);
                ApplyTo(_display);
            }

            var found = _display != null ? _display.FindAction("Player/" + action) : null;
            if (found == null)
            {
                return string.Empty;
            }

            foreach (var b in found.bindings)
            {
                if (b.isComposite || string.IsNullOrEmpty(b.effectivePath)) { continue; }
                if (gamepad ? IsGamepad(b) && b.name != "modifier" : IsKeyboard(b))
                {
                    return Display(b.effectivePath);
                }
            }

            return string.Empty;
        }

        /// <summary>A control path as the screen names it.</summary>
        public static string Display(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var slash = path.IndexOf('/');
            var device = slash > 0 ? path.Substring(0, slash) : string.Empty;
            var control = slash >= 0 ? path.Substring(slash + 1) : path;
            switch (control)
            {
                case "leftButton": return "ЛКМ";
                case "rightButton": return "ПКМ";
                case "middleButton": return "СКМ";
                case "forwardButton": return "M5";
                case "backButton": return "M4";
                case "space": return "Space";
                case "escape": return "Esc";
                case "enter": case "numpadEnter": return "Enter";
                case "tab": return "Tab";
                case "backspace": return "Backspace";
                case "leftShift": return "Shift";
                case "rightShift": return "RShift";
                case "leftCtrl": return "Ctrl";
                case "rightCtrl": return "RCtrl";
                case "leftAlt": return "Alt";
                case "rightAlt": return "RAlt";
                case "leftMeta": case "rightMeta": return "Cmd";
                case "capsLock": return "Caps";
                case "upArrow": return "↑";
                case "downArrow": return "↓";
                case "leftArrow": return "←";
                case "rightArrow": return "→";
                case "backquote": return "`";
                case "minus": return "-";
                case "equals": return "=";
                case "leftBracket": return "[";
                case "rightBracket": return "]";
                case "semicolon": return ";";
                case "quote": return "'";
                case "comma": return ",";
                case "period": return ".";
                case "slash": return "/";
                case "backslash": return "\\";
                case "buttonSouth": return "A";
                case "buttonEast": return "B";
                case "buttonWest": return "X";
                case "buttonNorth": return "Y";
                case "leftShoulder": return "LB";
                case "rightShoulder": return "RB";
                case "leftTrigger": return "LT";
                case "rightTrigger": return "RT";
                case "leftStickPress": return "LS";
                case "rightStickPress": return "RS";
                case "select": return "Select";
                case "start": return "Start";
                case "dpad/up": return "▲";
                case "dpad/down": return "▼";
                case "dpad/left": return "◀";
                case "dpad/right": return "▶";
            }

            if (control.StartsWith("numpad") && control.Length > 6)
            {
                return "Num" + control.Substring(6);
            }

            if (device.Contains("Keyboard") && control.Length <= 3)
            {
                return control.ToUpperInvariant();
            }

            return control.Length > 0 ? char.ToUpperInvariant(control[0]) + control.Substring(1) : control;
        }

        /// <summary>
        /// "&lt;DualShock4GamepadHID&gt;/buttonSouth" and "&lt;Gamepad&gt;/buttonSouth" are the same
        /// button to the player; bindings are stored against the generic device so any pad works.
        /// </summary>
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '<')
            {
                return path ?? string.Empty;
            }

            var close = path.IndexOf('>');
            var slash = path.IndexOf('/');
            if (close < 0 || slash < 0)
            {
                return path;
            }

            var layout = path.Substring(1, close - 1);
            var rest = path.Substring(slash);
            if (layout == "Keyboard" || layout == "Mouse" || layout == "Gamepad")
            {
                return path;
            }

            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Gamepad")) { return "<Gamepad>" + rest; }
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Keyboard")) { return "<Keyboard>" + rest; }
            if (InputSystem.IsFirstLayoutBasedOnSecond(layout, "Mouse")) { return "<Mouse>" + rest; }
            return path;
        }
    }
}
