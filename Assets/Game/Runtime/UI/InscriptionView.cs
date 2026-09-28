using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The words of the stone being read, over the upper middle of the screen
    /// (design/ui/ending/ending.html frame 2). Draws <see cref="Inscription.Current"/>; the game
    /// is not paused and nothing here takes input.
    /// </summary>
    public sealed class InscriptionView
    {
        private const string OpenClass = "ins--open";
        private readonly VisualElement _panel;
        private readonly Label _text;

        public InscriptionView(VisualElement root)
        {
            _panel = root?.Q("ins");
            _text = root?.Q<Label>("ins-text");
        }

        public bool IsOpen => _panel != null && _panel.ClassListContains(OpenClass);
        public string ShownText => _text != null ? _text.text : string.Empty;

        public void Attach()
        {
            Detach();
            Inscription.Opened += OnOpened;
            Inscription.Closed += OnClosed;
            if (Inscription.Current != null)
            {
                OnOpened(Inscription.Current);
            }
        }

        public void Detach()
        {
            Inscription.Opened -= OnOpened;
            Inscription.Closed -= OnClosed;
            OnClosed();
        }

        private void OnOpened(Inscription stone)
        {
            if (_panel == null)
            {
                return;
            }

            _text.text = stone.Text;
            _panel.AddToClassList(OpenClass);
        }

        private void OnClosed() => _panel?.RemoveFromClassList(OpenClass);
    }
}
