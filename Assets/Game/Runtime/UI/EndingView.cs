using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The ending from design/ui/ending/ending.html (frames 3–5), drawn for
    /// <see cref="EndingSequence"/>, which keeps the behaviour: the three lines on black, the
    /// earlier ones dimmed; the credits rolling up; the summary with «Продовжити». Holding
    /// interact for a second skips the lines or the credits; Enter, Space, interact or A on the
    /// summary go on.
    /// </summary>
    public sealed class EndingView : IHudOverlay
    {
        public const float HoldToSkip = 1f;
        private const string OpenClass = "end--open";
        private const string PartClass = "end--";

        private readonly VisualElement _overlay;
        private readonly Label[] _lines = new Label[EndingSequence.Lines.Length];
        private readonly VisualElement _roll;
        private readonly VisualElement _credits;
        private readonly Label _time;
        private readonly Label _deaths;
        private readonly Label _map;
        private readonly Label _kills;
        private readonly Label _secrets;
        private readonly Label _level;
        private readonly Button _continue;
        private EndingSequence _ending;
        private PlayerInputReader _input;
        private float _held;
        private int _summaryFrame = -1;

        public EndingView(VisualElement root)
        {
            _overlay = root?.Q("end");
            if (_overlay == null)
            {
                return;
            }

            for (var i = 0; i < _lines.Length; i++)
            {
                _lines[i] = _overlay.Q<Label>($"end-line-{i}");
            }

            _credits = _overlay.Q("end-credits");
            _roll = _overlay.Q("end-roll");
            _time = _overlay.Q<Label>("end-time");
            _deaths = _overlay.Q<Label>("end-deaths");
            _map = _overlay.Q<Label>("end-map");
            _kills = _overlay.Q<Label>("end-kills");
            _secrets = _overlay.Q<Label>("end-secrets");
            _level = _overlay.Q<Label>("end-level");
            _continue = _overlay.Q<Button>("end-continue");
            if (_continue != null) { _continue.clicked += () => _ending?.Continue(); }
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);

        /// <summary>The summary's values as shown, top to bottom, for tests.</summary>
        public string[] SummaryValues => new[]
        {
            _time?.text, _deaths?.text, _map?.text, _kills?.text, _secrets?.text, _level?.text
        };

        /// <summary>The lines on screen now (dimmed or not), for tests.</summary>
        public int LinesShown
        {
            get
            {
                var shown = 0;
                foreach (var line in _lines)
                {
                    shown += line != null && line.ClassListContains("end__line--on") ? 1 : 0;
                }

                return shown;
            }
        }

        public void Attach(EndingSequence ending, PlayerCharacter player)
        {
            Detach();
            _ending = ending;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (_ending != null && _overlay != null)
            {
                _ending.Changed += Draw;
                Draw();
            }
        }

        public void Detach()
        {
            if (_ending != null)
            {
                _ending.Changed -= Draw;
            }

            _ending = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        /// <summary>Escape does nothing here: the ending is left by skipping or by «Продовжити».</summary>
        public void Back()
        {
        }

        private void Draw()
        {
            if (_overlay == null || _ending == null)
            {
                return;
            }

            var part = _ending.Current;
            _overlay.EnableInClassList(OpenClass, part != EndingSequence.Part.None);
            foreach (EndingSequence.Part each in System.Enum.GetValues(typeof(EndingSequence.Part)))
            {
                _overlay.EnableInClassList(PartClass + each.ToString().ToLowerInvariant(), each == part);
            }

            for (var i = 0; i < _lines.Length; i++)
            {
                if (_lines[i] == null)
                {
                    continue;
                }

                _lines[i].text = EndingSequence.Lines[i];
                _lines[i].EnableInClassList("end__line--on", part == EndingSequence.Part.Lines && i <= _ending.Line);
                _lines[i].EnableInClassList("end__line--past", i < _ending.Line);
            }

            _held = 0f;
            if (part == EndingSequence.Part.Summary)
            {
                var summary = _ending.Summary;
                _time.text = GameSummary.FormatTime(summary.Seconds);
                _deaths.text = summary.Deaths.ToString();
                _map.text = $"{summary.MapPercent}%";
                _kills.text = summary.Kills.ToString();
                _secrets.text = $"{summary.SecretsFound} / {summary.SecretsTotal}";
                _level.text = summary.Level.ToString();
                _continue?.AddToClassList("btn--focused");
                _summaryFrame = Time.frameCount;
            }
        }

        public void Tick()
        {
            if (!IsOpen || _ending == null)
            {
                return;
            }

            if (_ending.Current == EndingSequence.Part.Credits && _roll != null && _credits != null)
            {
                // From under the bottom edge to past the top, over the credits' time.
                var travel = _credits.resolvedStyle.height + _roll.resolvedStyle.height;
                if (!float.IsNaN(travel))
                {
                    _roll.style.translate = new Translate(0f, _credits.resolvedStyle.height - travel * _ending.CreditsProgress);
                }
            }

            if (_ending.Current == EndingSequence.Part.Summary)
            {
                if (Time.frameCount != _summaryFrame
                    && (MenuInput.Confirmed() || (_input != null && _input.InteractPressedThisFrame)))
                {
                    _ending.Continue();
                }

                return;
            }

            _held = _input != null && _input.InteractHeld ? _held + Time.unscaledDeltaTime : 0f;
            if (_held >= HoldToSkip)
            {
                _held = 0f;
                _ending.Skip();
            }
        }
    }
}
