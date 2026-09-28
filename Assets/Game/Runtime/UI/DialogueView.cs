using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The conversation from design/ui/dialogue/dialogue.html (frames 2–4), drawn for
    /// <see cref="DialogueService"/>, which keeps the behaviour: the name plate, the page typed
    /// out, the diamonds counting pages, and the merchant's «Торгувати / Піти». Interact, Enter,
    /// Space or A turn the page; Escape or B end the conversation.
    /// </summary>
    public sealed class DialogueView : IHudOverlay
    {
        private const string OpenClass = "dlg--open";
        private const string ChoosingClass = "dlg--choosing";
        private const string TypingClass = "dlg--typing";

        private readonly VisualElement _overlay;
        private readonly Label _name;
        private readonly Label _epithet;
        private readonly Label _text;
        private readonly VisualElement _pages;
        private readonly Button[] _choices = new Button[2];
        private DialogueService _dialogue;
        private PlayerInputReader _input;
        private MenuInput.Repeater _horizontal;
        private MenuInput.Repeater _vertical;
        private int _choice;
        private int _openedFrame = -1;
        private int _shownCharacters = -1;

        public DialogueView(VisualElement root)
        {
            _overlay = root?.Q("dlg");
            if (_overlay == null)
            {
                return;
            }

            _name = _overlay.Q<Label>("dlg-name");
            _epithet = _overlay.Q<Label>("dlg-epithet");
            _text = _overlay.Q<Label>("dlg-text");
            _pages = _overlay.Q("dlg-pages");
            _choices[0] = _overlay.Q<Button>("dlg-trade");
            _choices[1] = _overlay.Q<Button>("dlg-leave");
            if (_choices[0] != null) { _choices[0].clicked += () => _dialogue?.Trade(); }
            if (_choices[1] != null) { _choices[1].clicked += () => _dialogue?.Close(); }
        }

        public bool IsOpen => _overlay != null && _overlay.ClassListContains(OpenClass);

        /// <summary>What the text box shows now, for tests.</summary>
        public string ShownText => _text != null ? _text.text : string.Empty;

        public void Attach(DialogueService dialogue, PlayerCharacter player)
        {
            Detach();
            _dialogue = dialogue;
            _input = player != null ? player.GetComponent<PlayerInputReader>() : null;
            if (_dialogue == null || _overlay == null)
            {
                return;
            }

            _dialogue.Opened += OnOpened;
            _dialogue.Closed += OnClosed;
            _dialogue.PageChanged += OnPageChanged;
        }

        public void Detach()
        {
            if (_dialogue != null)
            {
                _dialogue.Opened -= OnOpened;
                _dialogue.Closed -= OnClosed;
                _dialogue.PageChanged -= OnPageChanged;
            }

            _dialogue = null;
            _overlay?.RemoveFromClassList(OpenClass);
        }

        public void Back() => _dialogue?.Close();

        private void OnOpened()
        {
            _overlay.AddToClassList(OpenClass);
            _openedFrame = Time.frameCount;
            _name.text = _dialogue.Speaker.DisplayName;
            _epithet.text = _dialogue.Speaker.Epithet;
        }

        private void OnClosed()
        {
            _overlay.RemoveFromClassList(OpenClass);
            _overlay.RemoveFromClassList(ChoosingClass);
        }

        private void OnPageChanged()
        {
            _shownCharacters = -1;
            _overlay.EnableInClassList(ChoosingClass, _dialogue.IsChoosing);
            if (_dialogue.IsChoosing && _dialogue.Topic == null)
            {
                // The merchant with nothing new to say: a greeting of his own, then the choice.
                _text.text = "Золото маєш — товар знайдеться.";
            }

            _pages.Clear();
            for (var i = 0; i < _dialogue.PageCount && _dialogue.PageCount > 1; i++)
            {
                var diamond = new VisualElement { pickingMode = PickingMode.Ignore };
                diamond.AddToClassList("dlg__page");
                diamond.EnableInClassList("dlg__page--on", i == _dialogue.Page);
                _pages.Add(diamond);
            }

            SetChoice(0);
            Draw();
        }

        private void Draw()
        {
            if (_dialogue == null || _dialogue.IsChoosing)
            {
                return;
            }

            var visible = _dialogue.VisibleCharacters;
            if (visible != _shownCharacters)
            {
                _shownCharacters = visible;
                _text.text = _dialogue.PageText.Substring(0, visible);
            }

            _overlay.EnableInClassList(TypingClass, _dialogue.IsTyping);
        }

        public void Tick()
        {
            if (!IsOpen || _dialogue == null)
            {
                return;
            }

            Draw();
            if (Time.frameCount == _openedFrame)
            {
                // The press that started the conversation must not also turn its first page.
                return;
            }

            if (MenuInput.PadBackPressed())
            {
                Back();
                return;
            }

            var pressed = MenuInput.Confirmed() || (_input != null && _input.InteractPressedThisFrame);
            if (!_dialogue.IsChoosing)
            {
                if (pressed)
                {
                    _dialogue.Advance();
                }

                return;
            }

            var step = _horizontal.Step(MenuInput.ReadHorizontal()) + _vertical.Step(MenuInput.ReadVertical());
            if (step != 0)
            {
                SetChoice((_choice + 1) % 2);
            }

            if (pressed)
            {
                if (_choice == 0) { _dialogue.Trade(); }
                else { _dialogue.Close(); }
            }
        }

        private void SetChoice(int index)
        {
            _choice = index;
            for (var i = 0; i < _choices.Length; i++)
            {
                _choices[i]?.EnableInClassList("btn--focused", i == _choice);
            }
        }
    }
}
