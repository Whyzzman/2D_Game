using System;
using System.Collections;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A way made permanently easier: a bridge let down, a scaffold kicked loose, a gate unbarred.
    ///
    /// The lever is this object, and it is placed on the far side of what it opens — the player
    /// earns a shortcut by getting there the long way first. Pulling it records the id for good;
    /// death, rest and reload all bring the world back with the shortcut open, and pulling it again
    /// does nothing. The graph's connection with the same persistent id opens on the map in the
    /// same event.
    ///
    /// What changes is data in the Inspector: objects that appear (the bridge's collider and art),
    /// objects that disappear (a barred gate), and one part that swings or slides into place so the
    /// change is seen rather than simply being there next frame.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Shortcut : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _shortcutId = string.Empty;
        [SerializeField] private string _displayName = "Важіль";
        [SerializeField] private GameProgress _progress;
        [Tooltip("Turned on once open: the bridge, the scaffold — the new way itself.")]
        [SerializeField] private GameObject[] _revealWhenOpen = Array.Empty<GameObject>();
        [Tooltip("Turned off once open: bars, chains, rubble.")]
        [SerializeField] private GameObject[] _hideWhenOpen = Array.Empty<GameObject>();
        [Tooltip("Swings or slides from its closed pose to its open one. Visual only.")]
        [SerializeField] private Transform _animatedPart;
        [SerializeField] private Vector3 _closedEuler;
        [SerializeField] private Vector3 _openEuler;
        [SerializeField] private Vector3 _closedOffset;
        [SerializeField] private Transform _leverHandle;
        [SerializeField, Min(0f)] private float _openDuration = 1.1f;

        private Vector3 _openPosition;
        private Coroutine _opening;

        /// <summary>Raised when the lever is actually pulled — not when a load finds it open.</summary>
        public event Action<Shortcut> Unlocked;

        public string ShortcutId => _shortcutId;
        public bool IsUnlocked { get; private set; }

        /// <summary>True while the change is still playing out; the way is not open yet.</summary>
        public bool IsOpening => _opening != null;

        public bool CanInteract => !IsUnlocked && _progress != null;
        public string Prompt => IsUnlocked ? string.Empty : $"Потягнути: {_displayName}";

        private void Awake()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }

            if (_animatedPart != null)
            {
                _openPosition = _animatedPart.localPosition;
            }

            if (string.IsNullOrEmpty(_shortcutId))
            {
                Debug.LogError("Shortcut has no id; it could never be saved.", this);
            }
        }

        private void OnEnable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += ApplySaved;
            }
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= ApplySaved;
            }

            _opening = null;
        }

        private void Start()
        {
            ApplySaved();
        }

        public void Interact(PlayerCharacter player)
        {
            if (player == null || !player.IsAlive || !CanInteract)
            {
                return;
            }

            // Recorded first, then shown: the write cannot be lost to a death during the swing.
            var state = WorldState.Current;
            var isNew = state != null ? state.UnlockShortcut(_shortcutId)
                : _progress.UnlockShortcut(_shortcutId);
            if (state == null && isNew)
            {
                _progress.Save();
            }
        }

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag == WorldFlag.ShortcutUnlocked && id == _shortcutId && !IsUnlocked)
            {
                Open(animate: isActiveAndEnabled);
            }
        }

        /// <summary>Brings the shortcut to whatever the save says, with no animation or sound.</summary>
        public void ApplySaved()
        {
            var unlocked = _progress != null && _progress.IsShortcutUnlocked(_shortcutId);
            if (unlocked)
            {
                Open(animate: false);
            }
            else
            {
                IsUnlocked = false;
                SetPose(0f);
                SetOpenObjects(false);
            }
        }

        private void Open(bool animate)
        {
            var wasUnlocked = IsUnlocked;
            IsUnlocked = true;
            if (!animate || _openDuration <= 0f)
            {
                SetPose(1f);
                SetOpenObjects(true);
                return;
            }

            if (!wasUnlocked)
            {
                Unlocked?.Invoke(this);
            }

            if (_opening == null)
            {
                _opening = StartCoroutine(OpenRoutine());
            }
        }

        private IEnumerator OpenRoutine()
        {
            // Hidden obstacles go at once; the new way arrives with its animation, so nothing can
            // be stood on before it is visibly there.
            foreach (var target in _hideWhenOpen)
            {
                if (target != null) { target.SetActive(false); }
            }

            for (var t = 0f; t < _openDuration; t += Time.deltaTime)
            {
                SetPose(Mathf.SmoothStep(0f, 1f, t / _openDuration));
                yield return null;
            }

            SetPose(1f);
            SetOpenObjects(true);
            _opening = null;
        }

        private void SetOpenObjects(bool isOpen)
        {
            foreach (var target in _revealWhenOpen)
            {
                if (target != null) { target.SetActive(isOpen); }
            }

            foreach (var target in _hideWhenOpen)
            {
                if (target != null) { target.SetActive(!isOpen); }
            }
        }

        private void SetPose(float amount)
        {
            if (_animatedPart != null)
            {
                _animatedPart.localRotation = Quaternion.Euler(
                    Vector3.Lerp(_closedEuler, _openEuler, amount));
                _animatedPart.localPosition = _openPosition + _closedOffset * (1f - amount);
            }

            if (_leverHandle != null)
            {
                _leverHandle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(35f, -35f, amount));
            }
        }
    }
}
