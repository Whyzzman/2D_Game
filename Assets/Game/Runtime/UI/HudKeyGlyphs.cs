using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The HUD's key glyphs — the weapon, skill and spell slots, the flasks and the interaction
    /// prompt — following the player's own keys (controls.html, frame 7) rather than the ones
    /// written into the markup. A glyph also teaches without text: the first time its action
    /// becomes possible it pulses softly for a few beats (GAME_FEEL_IDEAS.md, item 21).
    /// </summary>
    public sealed class HudKeyGlyphs
    {
        private const float InviteSeconds = 2.4f;
        private const float InviteBeats = 3f;
        private const float InviteGrowth = 0.22f;

        /// <summary>Actions already pointed out since the game started: an invitation comes once.</summary>
        private static readonly HashSet<string> Invited = new HashSet<string>();

        private readonly (Label label, string action)[] _glyphs;
        private readonly Dictionary<string, float> _inviting = new Dictionary<string, float>();
        private readonly List<string> _ended = new List<string>();

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

        /// <summary>
        /// The action has just become possible. The first time, its glyph pulses; later calls do
        /// nothing, so the caller may say it every frame.
        /// </summary>
        public void Invite(string action)
        {
            if (Invited.Add(action))
            {
                _inviting[action] = 0f;
            }
        }

        /// <summary>True while the glyph of the action is pulsing.</summary>
        public bool IsInviting(string action) => _inviting.ContainsKey(action);

        /// <summary>Forgets what was pointed out; a new game teaches again.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetInvitations() => Invited.Clear();

        public void Tick(float dt)
        {
            if (_inviting.Count == 0)
            {
                return;
            }

            foreach (var (label, action) in _glyphs)
            {
                if (label == null || !_inviting.TryGetValue(action, out var clock))
                {
                    continue;
                }

                clock += dt;
                if (clock >= InviteSeconds)
                {
                    label.style.scale = StyleKeyword.Null;
                    label.style.opacity = StyleKeyword.Null;
                    _ended.Add(action);
                    continue;
                }

                _inviting[action] = clock;
                // Whole beats, so the glyph ends at rest; with reduced motion it only brightens.
                var beat = 0.5f - 0.5f * Mathf.Cos(clock / InviteSeconds * InviteBeats * 2f * Mathf.PI);
                var size = FeedbackSettings.ReducedMotion ? 1f : 1f + InviteGrowth * beat;
                label.style.scale = new Scale(new Vector3(size, size, 1f));
                label.style.opacity = 0.7f + 0.3f * beat;
            }

            foreach (var action in _ended)
            {
                _inviting.Remove(action);
            }

            _ended.Clear();
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
