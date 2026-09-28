using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A stone with words cut into it (item 5 of the big list, gamedesign/STORY.md): the story is
    /// told by seven of these rather than by a cutscene. Interact reads it — the text stands over
    /// the screen with the game running, as in design/ui/ending/ending.html frame 2 — and walking
    /// away, or interact again, puts it down. One is read at a time.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Inscription : MonoBehaviour, IInteractable
    {
        /// <summary>Further from the stone than this and the text goes.</summary>
        public const float ReadRange = 2.6f;

        [SerializeField, TextArea(2, 5)] private string _text = string.Empty;

        private Transform _reader;

        /// <summary>The stone being read now, or null.</summary>
        public static Inscription Current { get; private set; }

        public static event Action<Inscription> Opened;
        public static event Action Closed;

        public string Text => _text;

        public bool CanInteract => !string.IsNullOrEmpty(_text);

        public string Prompt => Current == this || string.IsNullOrEmpty(_text) ? string.Empty : "Читати";

        public void Interact(PlayerCharacter player)
        {
            if (Current == this)
            {
                Close();
                return;
            }

            Close();
            if (!CanInteract || player == null)
            {
                return;
            }

            _reader = player.transform;
            Current = this;
            Opened?.Invoke(this);
        }

        /// <summary>Puts down whatever stone is being read.</summary>
        public static void Close()
        {
            if (Current == null)
            {
                return;
            }

            Current = null;
            Closed?.Invoke();
        }

        private void Update()
        {
            if (Current != this)
            {
                return;
            }

            if (_reader == null || Vector2.Distance(_reader.position, transform.position) > ReadRange)
            {
                Close();
            }
        }

        private void OnDisable()
        {
            if (Current == this)
            {
                Close();
            }
        }
    }
}
