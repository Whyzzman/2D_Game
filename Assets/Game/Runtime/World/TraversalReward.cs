using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>A repeat-safe training reward using the ordinary interaction and save APIs.</summary>
    public sealed class TraversalReward : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _abilityId = AbilityIds.AirDash;
        [SerializeField] private string _displayName = "Air Dash";
        private GameProgress _progress;
        public bool CanInteract => _progress != null && !_progress.HasAbility(_abilityId);
        public string Prompt => CanInteract ? "Отримати: " + DisplayName : DisplayName;

        /// <summary>The world graph's Ukrainian name; the serialized one is only a fallback.</summary>
        private string DisplayName
        {
            get
            {
                var ability = _progress != null && _progress.World != null
                    ? _progress.World.Ability(_abilityId)
                    : null;
                return ability != null && !string.IsNullOrEmpty(ability.DisplayName)
                    ? ability.DisplayName
                    : _displayName;
            }
        }
        private void Awake() { _progress = FindAnyObjectByType<GameProgress>(); }
        public void Interact(PlayerCharacter player)
        {
            if (player == null || !player.IsAlive || !CanInteract) { return; }
            _progress.UnlockAbility(_abilityId);
            _progress.Save();
            AbilityAnnouncements.Announce(_abilityId);
        }
    }
}
