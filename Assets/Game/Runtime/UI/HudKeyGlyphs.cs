using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The HUD's key glyphs — the weapon, skill and spell slots, the flasks and the interaction
    /// prompt — following the player's own keys (controls.html, frame 7) rather than the ones
    /// written into the markup.
    /// </summary>
    public sealed class HudKeyGlyphs
    {
        private readonly (Label label, string action)[] _glyphs;

        public HudKeyGlyphs(VisualElement root)
        {
            _glyphs = new[]
            {
                (SlotKey(root, "slot-weapon"), "SwitchWeapon"),
                (SlotKey(root, "slot-skill-0"), "ActiveSkill1"),
                (SlotKey(root, "slot-skill-1"), "ActiveSkill2"),
                (SlotKey(root, "slot-spell-0"), "CastSpell1"),
                (SlotKey(root, "slot-spell-1"), "CastSpell2"),
                (SlotKey(root, "slot-spell-2"), "CastSpell3"),
                (root?.Q<Label>(className: "flasks__key"), "Heal"),
                (root?.Q<Label>(className: "prompt__glyph"), "Interact")
            };
            KeyBindings.Changed += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            KeyBindings.Changed -= Refresh;
        }

        public void Refresh()
        {
            foreach (var (label, action) in _glyphs)
            {
                if (label == null)
                {
                    continue;
                }

                var glyph = KeyBindings.Glyph(action, gamepad: false);
                label.text = glyph;
                label.style.display = string.IsNullOrEmpty(glyph) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>The glyph of one HUD slot, for tests.</summary>
        public string TextOf(string action)
        {
            foreach (var (label, name) in _glyphs)
            {
                if (name == action && label != null)
                {
                    return label.text;
                }
            }

            return string.Empty;
        }

        private static Label SlotKey(VisualElement root, string slot) =>
            root?.Q(slot)?.Q<Label>(className: "slot__key");
    }
}
