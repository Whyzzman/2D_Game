using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Where a door stands.</summary>
    public enum DoorState
    {
        /// <summary>Its condition is not met. Solid.</summary>
        Locked,

        /// <summary>The condition is met but the door waits to be pushed. Still solid.</summary>
        Unlocked,

        /// <summary>Passable, for good.</summary>
        Open
    }

    /// <summary>
    /// A barrier that opens once the world says it should, and stays open.
    ///
    /// The door out of the Tomb is the one that matters: it opens for anyone who has beaten the
    /// Guardian, which deliberately includes a save made before the forest existed. The state is
    /// read from what was achieved, never from a flag set the moment the boss died, so a player who
    /// finished the Tomb three versions ago walks straight out.
    ///
    /// It listens rather than polls: a boss falling, a lever pulled or a load raise one event and
    /// the door settles in the same frame — before the map redraws from the same event.
    ///
    /// Replaces the PHASE 24 progress gate, which checked on a timer and knew three conditions;
    /// this one reads any <see cref="WorldRequirement"/>, including shortcuts and key items.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PersistentDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private WorldRequirement _requirement =
            new WorldRequirement(RequirementKind.BossDefeated, "guardian-of-the-tomb");
        [Tooltip("Off: opens by itself the moment it unlocks. On: waits for the player to push it.")]
        [SerializeField] private bool _opensOnInteract;
        [Tooltip("Turned off once open; the solid blocking geometry.")]
        [SerializeField] private GameObject _barrier;
        [Tooltip("Turned on once open; the doorway behind the barrier.")]
        [SerializeField] private GameObject _openVisual;
        [Tooltip("Shown while locked, next to the door. Says that it is shut, never what opens it.")]
        [SerializeField] private string _lockedPrompt = "Зачинено";
        [SerializeField] private string _openPrompt = "Відчинити";
        private bool _isSilent;

        /// <summary>Raised the first time the door opens; the hook for its sound and its dust.</summary>
        public event Action<PersistentDoor> Opened;

        public DoorState State { get; private set; } = DoorState.Locked;
        public bool IsOpen => State == DoorState.Open;
        public WorldRequirement Requirement => _requirement;

        public bool CanInteract => _opensOnInteract && State == DoorState.Unlocked;
        public string Prompt => State == DoorState.Locked ? _lockedPrompt
            : (State == DoorState.Unlocked ? _openPrompt : string.Empty);

        private IWorldQuery World => (IWorldQuery)WorldState.Current ?? _progress;

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

        private void Start()
        {
            // Settled before the first frame is drawn: a door that is already earned must never
            // be seen shut and then pop open — nor heard opening, since it was open all along.
            _isSilent = true;
            Evaluate();
            _isSilent = false;
        }

        public void Interact(PlayerCharacter player)
        {
            if (player != null && player.IsAlive && CanInteract)
            {
                SetState(DoorState.Open);
            }
        }

        private void OnWorldChanged(WorldFlag flag, string id) => Evaluate();

        /// <summary>Re-reads the world at once. A door never closes again once open.</summary>
        public void Evaluate()
        {
            if (State == DoorState.Open)
            {
                ApplyVisuals();
                return;
            }

            var met = !_requirement.IsNone && WorldRules.IsMet(_requirement, World);
            SetState(!met ? DoorState.Locked
                : (_opensOnInteract ? DoorState.Unlocked : DoorState.Open));
        }

        private void SetState(DoorState state)
        {
            var wasOpen = State == DoorState.Open;
            State = state;
            ApplyVisuals();
            if (!wasOpen && state == DoorState.Open && !_isSilent)
            {
                Opened?.Invoke(this);
            }
        }

        private void ApplyVisuals()
        {
            if (_barrier != null)
            {
                _barrier.SetActive(State != DoorState.Open);
            }

            if (_openVisual != null)
            {
                _openVisual.SetActive(State == DoorState.Open);
            }
        }
    }
}
