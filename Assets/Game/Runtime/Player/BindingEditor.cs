using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenKingdom
{
    public enum BindingChange
    {
        Assigned,
        /// <summary>The key was another action's; that action got this one's old key.</summary>
        Swapped,
        Unchanged,
        /// <summary>A fixed key or button; nothing changed, <see cref="BindingOutcome.Message"/> says why.</summary>
        Refused,
        Cancelled
    }

    public readonly struct BindingOutcome
    {
        public BindingOutcome(BindingChange change, string message, BindingRow other = null, int otherSlot = -1)
        {
            Change = change;
            Message = message ?? string.Empty;
            Other = other;
            OtherSlot = otherSlot;
        }

        public BindingChange Change { get; }
        public string Message { get; }
        /// <summary>The row that gave up its key in a swap.</summary>
        public BindingRow Other { get; }
        public int OtherSlot { get; }
    }

    /// <summary>
    /// Changes the player's keys on a private copy of the actions and stores each change at once
    /// (<see cref="KeyBindings.Store"/>). The rules from the approved mockup: a key already in use
    /// swaps places, so no action is ever left without a key; pause, moving with the stick and the
    /// LT modifier are fixed, and a refusal always says why. Keyboard slots take keys and mouse
    /// buttons, the pad slot takes pad buttons; a button under LT only competes with the others
    /// under LT, a plain button only with plain ones.
    /// </summary>
    public sealed class BindingEditor : IDisposable
    {
        private readonly InputActionAsset _asset;
        private InputActionRebindingExtensions.RebindingOperation _operation;

        public BindingEditor(InputActionAsset template = null)
        {
            template ??= KeyBindings.Template;
            if (template == null)
            {
                return;
            }

            _asset = UnityEngine.Object.Instantiate(template);
            _asset.Disable();
            KeyBindings.ApplyTo(_asset);
        }

        public bool IsListening => _operation != null;

        public void Dispose()
        {
            CancelListening();
            if (_asset == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_asset);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_asset);
            }
        }

        // ---------- reading ----------

        public static bool IsFixed(BindingRow row, int slot) =>
            slot == KeyBindings.PadSlot ? row.IsPadFixed && !row.PadModifier : row.KeyboardFixed;

        /// <summary>The slot's current control path; empty when the slot holds nothing.</summary>
        public string PathOf(BindingRow row, int slot)
        {
            var action = Action(row);
            var index = KeyBindings.BindingIndex(action, row, slot);
            return index >= 0 ? action.bindings[index].effectivePath ?? string.Empty : string.Empty;
        }

        /// <summary>What the cell shows.</summary>
        public string Glyph(BindingRow row, int slot) => KeyBindings.Display(PathOf(row, slot));

        // ---------- changing ----------

        /// <summary>
        /// Puts a control into a slot. A key another action holds swaps with this slot's old one;
        /// if that would leave the other action with nothing, it is refused instead.
        /// </summary>
        public BindingOutcome Assign(BindingRow row, int slot, string path)
        {
            if (_asset == null || IsFixed(row, slot))
            {
                return new BindingOutcome(BindingChange.Refused, $"«{row.Label}» змінити не можна.");
            }

            path = KeyBindings.Normalize(path);
            var refusal = Refusal(slot, path);
            if (refusal != null)
            {
                return new BindingOutcome(BindingChange.Refused, refusal);
            }

            var current = PathOf(row, slot);
            if (Same(current, path))
            {
                return new BindingOutcome(BindingChange.Unchanged, string.Empty);
            }

            BindingRow other = null;
            var otherSlot = -1;
            FindHolder(row, slot, path, ref other, ref otherSlot);
            if (other != null)
            {
                if (string.IsNullOrEmpty(current) && otherSlot != KeyBindings.AlternativeSlot)
                {
                    return new BindingOutcome(BindingChange.Refused,
                        $"{KeyBindings.Display(path)} — основна клавіша «{other.Label}»; вона лишилася б без клавіші.");
                }

                Set(other, otherSlot, current);
            }

            Set(row, slot, path);
            KeyBindings.Store(_asset);
            return other == null
                ? new BindingOutcome(BindingChange.Assigned, string.Empty)
                : new BindingOutcome(BindingChange.Swapped,
                    $"{KeyBindings.Display(path)} перейшла до «{row.Label}», а «{other.Label}» отримала "
                    + (string.IsNullOrEmpty(current) ? "порожню клітинку" : KeyBindings.Display(current))
                    + " — клавіші поміняно місцями.", other, otherSlot);
        }

        /// <summary>Empties the second keyboard key; the first one and the pad button always stay.</summary>
        public bool ClearAlternative(BindingRow row)
        {
            if (_asset == null || row.KeyboardFixed || string.IsNullOrEmpty(PathOf(row, KeyBindings.AlternativeSlot)))
            {
                return false;
            }

            Set(row, KeyBindings.AlternativeSlot, string.Empty);
            KeyBindings.Store(_asset);
            return true;
        }

        public void ResetAll()
        {
            CancelListening();
            _asset?.RemoveAllBindingOverrides();
            KeyBindings.ResetAll();
        }

        // ---------- listening ----------

        /// <summary>
        /// Waits for the next key (or pad button) and assigns it. Escape cancels on the keyboard,
        /// Start on a pad; the key that started the wait is not taken, as only a press counts.
        /// </summary>
        public bool Listen(BindingRow row, int slot, Action<BindingOutcome> done)
        {
            if (_asset == null || IsFixed(row, slot) || IsListening)
            {
                return false;
            }

            var action = Action(row);
            var index = KeyBindings.BindingIndex(action, row, slot);
            if (index < 0)
            {
                return false;
            }

            var pad = slot == KeyBindings.PadSlot;
            string chosen = null;
            var operation = action.PerformInteractiveRebinding(index)
                .WithCancelingThrough(pad ? "<Gamepad>/start" : "<Keyboard>/escape")
                .OnMatchWaitForAnother(0.05f)
                // The choice goes through Assign, which knows about swaps and fixed keys.
                .OnApplyBinding((_, path) => chosen = path)
                .OnComplete(op => Finish(op, row, slot, chosen, done))
                .OnCancel(op => Finish(op, row, slot, null, done));
            if (pad)
            {
                operation.WithControlsHavingToMatchPath("<Gamepad>")
                    .WithControlsExcluding("<Gamepad>/leftStick")
                    .WithControlsExcluding("<Gamepad>/leftStick/*")
                    .WithControlsExcluding("<Gamepad>/rightStick")
                    .WithControlsExcluding("<Gamepad>/rightStick/*")
                    .WithControlsExcluding("<Gamepad>/dpad");
            }
            else
            {
                operation.WithControlsHavingToMatchPath("<Keyboard>")
                    .WithControlsHavingToMatchPath("<Mouse>")
                    .WithControlsExcluding("<Keyboard>/anyKey")
                    .WithControlsExcluding("<Mouse>/position")
                    .WithControlsExcluding("<Mouse>/delta")
                    .WithControlsExcluding("<Mouse>/scroll")
                    .WithControlsExcluding("<Mouse>/scroll/*")
                    .WithControlsExcluding("<Pointer>/position")
                    .WithControlsExcluding("<Pointer>/delta");
            }

            _operation = operation;
            operation.Start();
            return true;
        }

        public void CancelListening()
        {
            _operation?.Cancel();
            _operation?.Dispose();
            _operation = null;
        }

        private void Finish(InputActionRebindingExtensions.RebindingOperation op, BindingRow row, int slot,
            string chosen, Action<BindingOutcome> done)
        {
            if (_operation == op)
            {
                _operation = null;
            }

            op.Dispose();
            var outcome = string.IsNullOrEmpty(chosen)
                ? new BindingOutcome(BindingChange.Cancelled, string.Empty)
                : Assign(row, slot, chosen);
            done?.Invoke(outcome);
        }

        // ---------- rules ----------

        private static string Refusal(int slot, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "Порожню клавішу призначити не можна.";
            }

            var pad = slot == KeyBindings.PadSlot;
            if (pad != path.StartsWith("<Gamepad>"))
            {
                return pad ? "Сюди — лише кнопка геймпада." : "Сюди — лише клавіша чи кнопка миші.";
            }

            if (Same(path, "<Keyboard>/escape") || Same(path, "<Gamepad>/start"))
            {
                return $"{KeyBindings.Display(path)} — пауза, її не можна призначити іншій дії.";
            }

            if (Same(path, "<Gamepad>/leftTrigger"))
            {
                return "LT — модифікатор умінь і заклять, його не можна призначити окремій дії.";
            }

            if (path.StartsWith("<Gamepad>/leftStick"))
            {
                return "Лівий стік рухає героя; його не можна призначити.";
            }

            return null;
        }

        /// <summary>The slot that already holds this key, among the slots it competes with.</summary>
        private void FindHolder(BindingRow row, int slot, string path, ref BindingRow other, ref int otherSlot)
        {
            var pad = slot == KeyBindings.PadSlot;
            foreach (var candidate in KeyBindings.Rows)
            {
                if (pad && candidate.PadModifier != row.PadModifier)
                {
                    continue;
                }

                var slots = pad
                    ? new[] { KeyBindings.PadSlot }
                    : new[] { KeyBindings.PrimarySlot, KeyBindings.AlternativeSlot };
                foreach (var s in slots)
                {
                    if (candidate == row && s == slot)
                    {
                        continue;
                    }

                    if (Same(PathOf(candidate, s), path))
                    {
                        other = candidate;
                        otherSlot = s;
                        return;
                    }
                }
            }
        }

        private void Set(BindingRow row, int slot, string path)
        {
            var action = Action(row);
            var index = KeyBindings.BindingIndex(action, row, slot);
            if (index < 0)
            {
                return;
            }

            var authored = action.bindings[index].path ?? string.Empty;
            if (Same(authored, path))
            {
                action.RemoveBindingOverride(index);
            }
            else
            {
                // An empty override switches the binding off; that is what an empty slot is.
                action.ApplyBindingOverride(index, path ?? string.Empty);
            }
        }

        private InputAction Action(BindingRow row) => _asset != null ? _asset.FindAction("Player/" + row.Action) : null;

        private static bool Same(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
