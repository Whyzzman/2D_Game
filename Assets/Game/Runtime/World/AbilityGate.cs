using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The readable half of a movement obstacle. The obstacle itself is level geometry — a ledge
    /// too high, a gap too wide, a shaft with nothing to hold — and that geometry is what keeps the
    /// player out. This component never blocks anything: it only says so.
    ///
    /// Before the ability exists the prompt is deliberately vague, "cannot reach yet", so the
    /// Tomb does not spoil the Warden's reward by name. Once the ability is owned the marker swaps
    /// to its unlocked look — the only hint that it is time to come back. There is no popup.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AbilityGate : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _requiredAbilityId = AbilityIds.DoubleJump;
        [Tooltip("The graph connection this obstacle stands on, so the map can point at it.")]
        [SerializeField] private string _connectionId = string.Empty;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private GameObject _lockedVisual;
        [SerializeField] private GameObject _unlockedVisual;
        [Tooltip("Shown near the obstacle before the ability is owned. Never names the ability.")]
        [SerializeField] private string _optionalPrompt = "Поки що не дістатися";

        public string RequiredAbilityId => _requiredAbilityId;
        public string ConnectionId => _connectionId;
        public bool IsPassable { get; private set; }

        /// <summary>Never interactive: the ability is used by moving, not by pressing a button.</summary>
        public bool CanInteract => false;

        public string Prompt => IsPassable ? string.Empty : _optionalPrompt;

        private void Awake()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }
        }

        private void OnEnable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += Evaluate;
            }
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= Evaluate;
            }
        }

        private void Start() => Evaluate();

        public void Interact(PlayerCharacter player)
        {
        }

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag == WorldFlag.AbilityUnlocked)
            {
                Evaluate();
            }
        }

        public void Evaluate()
        {
            var world = (IWorldQuery)WorldState.Current ?? _progress;
            IsPassable = world != null && world.HasAbility(_requiredAbilityId);
            if (_lockedVisual != null)
            {
                _lockedVisual.SetActive(!IsPassable);
            }

            if (_unlockedVisual != null)
            {
                _unlockedVisual.SetActive(IsPassable);
            }
        }
    }
}
