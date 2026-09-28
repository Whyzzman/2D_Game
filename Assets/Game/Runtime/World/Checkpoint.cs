using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// An altar. The first interaction lights it and makes it the respawn point; every later one
    /// is a rest, which also puts the ordinary enemies back. Resting is refused mid-fight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _checkpointId = "altar-01";
        [SerializeField] private string _displayName = "Вівтар";
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCombatStatus _combatStatus;
        [Tooltip("Where the player stands after respawning; keep it clear of walls and enemies.")]
        [SerializeField] private Transform _respawnPoint;
        [SerializeField] private SpriteRenderer _flame;
        [SerializeField] private Color _activeColor = new Color(1f, 0.76f, 0.35f);
        [SerializeField] private Color _currentColor = new Color(1f, 0.93f, 0.6f);
        [SerializeField] private Color _inactiveColor = new Color(0.35f, 0.35f, 0.38f);
        private ShrineMenu _menu;

        public event Action<Checkpoint> Activated;
        public event Action<Checkpoint> Rested;

        public string CheckpointId => _checkpointId;

        /// <summary>
        /// The name from the checkpoint database when there is one, so the altar, the map and the
        /// travel list can never call the same place two things. The local field is the fallback
        /// for scenes built without a world graph.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var definition = _progress != null && _progress.World != null
                    ? _progress.World.Checkpoint(_checkpointId)
                    : null;
                return definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                    ? definition.DisplayName
                    : _displayName;
            }
        }
        public bool IsActivated { get; private set; }
        public bool IsCurrent => _respawner != null
            && _respawner.ActiveCheckpointId == _checkpointId;

        public Vector2 RespawnPosition => _respawnPoint != null
            ? _respawnPoint.position
            : transform.position;

        public bool CanInteract => isActiveAndEnabled
            && _respawner != null
            && !_respawner.IsSequenceRunning
            && !IsInCombat;

        public string Prompt
        {
            get
            {
                if (IsInCombat)
                {
                    return "Не можна відпочивати в бою";
                }

                return IsActivated ? $"Відпочити ({DisplayName})" : $"Запалити {DisplayName}";
            }
        }

        private void Awake()
        {
            // An altar lives in its zone's scene and the services in the world scene; references
            // across scenes are not saved, so they are found here.
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_combatStatus == null) { _combatStatus = FindAnyObjectByType<PlayerCombatStatus>(); }
            if (_respawner == null)
            {
                Debug.LogError("Checkpoint requires a PlayerRespawner.", this);
                enabled = false;
                return;
            }

            if (_respawnPoint == null)
            {
                _respawnPoint = transform;
            }

            _menu = FindAnyObjectByType<ShrineMenu>();
            SetVisual();
        }

        private void OnEnable()
        {
            _respawner?.RegisterCheckpoint(this);
        }

        private void OnDisable()
        {
            _respawner?.UnregisterCheckpoint(this);
        }

        private void Start()
        {
            // An altar lit in an earlier session stays lit.
            IsActivated = _progress != null && _progress.IsCheckpointActivated(_checkpointId);
            SetVisual();
        }

        private void Update()
        {
            SetVisual();
        }

        public void Interact(PlayerCharacter player)
        {
            if (player == null || !player.IsAlive || !CanInteract)
            {
                return;
            }

            if (!IsActivated)
            {
                IsActivated = true;
                _respawner.ActivateCheckpoint(this, RespawnPosition);
                SetVisual();
                Activated?.Invoke(this);
                return;
            }

            // Already lit: the altar's menu, when something can show it — rest or travel.
            if (_menu != null && _menu.HasPresenter)
            {
                _menu.Open(this);
                return;
            }

            // Otherwise exactly as before: make it current again and rest.
            _respawner.SetRespawnPoint(RespawnPosition, _checkpointId);
            _respawner.RestAt(this);
            SetVisual();
            Rested?.Invoke(this);
        }

        private bool IsInCombat => _combatStatus != null && _combatStatus.IsInCombat;

        private void SetVisual()
        {
            if (_flame == null)
            {
                return;
            }

            _flame.color = !IsActivated
                ? _inactiveColor
                : (IsCurrent ? _currentColor : _activeColor);
        }
    }
}
