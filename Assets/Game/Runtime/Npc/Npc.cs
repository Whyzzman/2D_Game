using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A character standing in the level: the prompt «Говорити · name» in range, a conversation
    /// on interact (item 70). Characters move on with the story — the merchant from the tomb to
    /// the forest once the Guardian falls, the pilgrim three times — so each placement says from
    /// which boss it appears and after which it is gone; the other placements of the same
    /// character take over. Nothing to say in a fight: the prompt waits until it is over.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Npc : MonoBehaviour, IInteractable
    {
        [SerializeField] private NpcDefinition _definition;
        [Tooltip("Stands here only once this boss is beaten; empty for from the start.")]
        [SerializeField] private string _appearsAfterBoss = string.Empty;
        [Tooltip("Gone from here once this boss is beaten; empty to stay for good.")]
        [SerializeField] private string _leavesAfterBoss = string.Empty;
        [Tooltip("Where this is, for the journal «Люди»: «Гробниця · Вхід».")]
        [SerializeField] private string _placeName = string.Empty;
        [Tooltip("The figure and its collider; switched off while the character is elsewhere.")]
        [SerializeField] private GameObject _presence;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private DialogueService _dialogue;
        [SerializeField] private PlayerCombatStatus _combatStatus;

        public NpcDefinition Definition => _definition;
        public string PlaceName => _placeName;
        public bool IsPresent { get; private set; }

        public bool CanInteract => IsPresent && _dialogue != null && !_dialogue.IsOpen && !IsInCombat;

        public string Prompt
        {
            get
            {
                if (!IsPresent || _definition == null)
                {
                    return string.Empty;
                }

                return IsInCombat ? "Не до розмов у бою" : $"Говорити · {_definition.DisplayName}";
            }
        }

        private bool IsInCombat => _combatStatus != null && _combatStatus.IsInCombat;

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_dialogue == null) { _dialogue = FindAnyObjectByType<DialogueService>(); }
            if (_combatStatus == null) { _combatStatus = FindAnyObjectByType<PlayerCombatStatus>(); }
        }

        private void OnEnable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= Refresh;
            }
        }

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag == WorldFlag.BossDefeated)
            {
                Refresh();
            }
        }

        /// <summary>Whether the character stands here now, from the bosses beaten.</summary>
        public void Refresh()
        {
            var appeared = string.IsNullOrEmpty(_appearsAfterBoss)
                || (_progress != null && _progress.IsBossDefeated(_appearsAfterBoss));
            var left = !string.IsNullOrEmpty(_leavesAfterBoss)
                && _progress != null && _progress.IsBossDefeated(_leavesAfterBoss);
            IsPresent = appeared && !left && _definition != null;
            if (_presence != null && _presence.activeSelf != IsPresent)
            {
                _presence.SetActive(IsPresent);
            }
        }

        public void Interact(PlayerCharacter player)
        {
            if (CanInteract)
            {
                _dialogue.Begin(_definition);
            }
        }
    }
}
