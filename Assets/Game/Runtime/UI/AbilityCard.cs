using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// "Ability gained" — the card from design/ui/tutorial/tutorial.html (frame 5), shown when an
    /// ability is earned in the world (<see cref="AbilityAnnouncements"/>). As in Hollow Knight it
    /// is the lesson itself: the name, a line on what it does and which button does it. The game
    /// waits under it until the player continues; a second ability waits its turn.
    /// </summary>
    public sealed class AbilityCard
    {
        private const string OpenClass = "ability--open";

        /// <summary>
        /// What each ability does and the action it is used through; the card shows that action's
        /// keys as the player has set them (controls.html, frame 7).
        /// </summary>
        private static readonly Dictionary<string, (string text, string how, string action)> Lessons =
            new Dictionary<string, (string, string, string)>
            {
                [AbilityIds.Dash] = ("Короткий ривок уперед, крізь удари.", "Натисни", "Dash"),
                [AbilityIds.DoubleJump] = ("Тепер можна відштовхнутися від повітря.",
                    "У повітрі натисни", "Jump"),
                [AbilityIds.AirDash] = ("Ривок тепер працює й у повітрі.", "У повітрі натисни", "Dash"),
                [AbilityIds.WallRun] = ("Стіна більше не перешкода: по ній можна бігти вгору.",
                    "Притиснись до стіни й тримай", "Jump"),
            };

        private readonly VisualElement _card;
        private readonly Label _name;
        private readonly Label _text;
        private readonly Label _how;
        private readonly Label _key;
        private readonly Label _pad;
        private readonly VisualElement _howRow;
        private readonly Queue<string> _waiting = new Queue<string>();
        private PlayerInputReader _input;
        private GameProgress _progress;
        private int _openedFrame = -1;

        public AbilityCard(VisualElement root)
        {
            _card = root?.Q("ability-card");
            if (_card == null)
            {
                return;
            }

            _name = _card.Q<Label>("ability-name");
            _text = _card.Q<Label>("ability-text");
            _how = _card.Q<Label>("ability-how");
            _key = _card.Q<Label>("ability-key");
            _pad = _card.Q<Label>("ability-pad");
            _howRow = _how?.parent;
            var continueButton = _card.Q<Button>("ability-continue");
            if (continueButton != null) { continueButton.clicked += Continue; }
        }

        public bool IsOpen => _card != null && _card.ClassListContains(OpenClass);

        /// <summary>The ability shown right now, for tests and tools.</summary>
        public string Showing { get; private set; }

        public void Attach(PlayerCharacter player, GameProgress progress)
        {
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            _progress = progress;
            AbilityAnnouncements.Announced -= Show;
            AbilityAnnouncements.Announced += Show;
        }

        public void Detach()
        {
            AbilityAnnouncements.Announced -= Show;
            _waiting.Clear();
            if (IsOpen)
            {
                Close();
            }
        }

        public void Show(string abilityId)
        {
            if (_card == null || string.IsNullOrEmpty(abilityId))
            {
                return;
            }

            if (IsOpen)
            {
                _waiting.Enqueue(abilityId);
                return;
            }

            Fill(abilityId);
            _card.AddToClassList(OpenClass);
            _openedFrame = UnityEngine.Time.frameCount;
            GamePause.Push();
            _input?.SetGameplayEnabled(false);
        }

        /// <summary>E, Enter, Space or the pad's A; never on the frame the card appeared.</summary>
        public void Tick()
        {
            if (!IsOpen || UnityEngine.Time.frameCount == _openedFrame)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (MenuInput.Confirmed() || (keyboard != null && keyboard.eKey.wasPressedThisFrame))
            {
                Continue();
            }
        }

        public void Continue()
        {
            if (!IsOpen)
            {
                return;
            }

            Close();
            if (_waiting.Count > 0)
            {
                Show(_waiting.Dequeue());
            }
        }

        private void Close()
        {
            _card.RemoveFromClassList(OpenClass);
            Showing = null;
            GamePause.Pop();
            _input?.SetGameplayEnabled(true);
        }

        private void Fill(string abilityId)
        {
            Showing = abilityId;
            var ability = _progress != null && _progress.World != null
                ? _progress.World.Ability(abilityId)
                : null;
            if (_name != null)
            {
                _name.text = (ability != null && !string.IsNullOrEmpty(ability.DisplayName)
                    ? ability.DisplayName : abilityId).ToUpperInvariant();
            }

            var known = Lessons.TryGetValue(abilityId, out var lesson);
            if (_text != null) { _text.text = known ? lesson.text : string.Empty; }
            if (_howRow != null)
            {
                _howRow.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (!known)
            {
                return;
            }

            if (_how != null) { _how.text = lesson.how; }
            if (_key != null) { _key.text = KeyBindings.Glyph(lesson.action, gamepad: false); }
            if (_pad != null) { _pad.text = KeyBindings.Glyph(lesson.action, gamepad: true); }
        }
    }
}
