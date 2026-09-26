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
        public string Prompt => CanInteract ? "Отримати: " + _displayName : _displayName;
        private void Awake() { _progress = FindAnyObjectByType<GameProgress>(); }
        public void Interact(PlayerCharacter player)
        {
            if (player == null || !player.IsAlive || !CanInteract) { return; }
            _progress.UnlockAbility(_abilityId);
            _progress.Save();
        }
    }
}
