using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Collects interactables the player is standing in and activates the closest available one.
    /// Objects register through their own trigger colliders, so nothing searches the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        private readonly List<IInteractable> _inRange = new List<IInteractable>();

        private void Awake()
        {
            if (_player == null)
            {
                _player = GetComponentInParent<PlayerCharacter>();
            }

            if (_player == null)
            {
                Debug.LogError("PlayerInteractor requires a PlayerCharacter.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_player != null)
            {
                _player.InteractPressed += OnInteractPressed;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.InteractPressed -= OnInteractPressed;
            }
            _inRange.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null && !_inRange.Contains(interactable))
            {
                _inRange.Add(interactable);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var interactable = other.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                _inRange.Remove(interactable);
            }
        }

        /// <summary>
        /// What to show the player right now, or empty when there is nothing to do. A dead player
        /// is offered nothing.
        /// </summary>
        public string CurrentPrompt
        {
            get
            {
                if (_player == null || !_player.IsAlive)
                {
                    return string.Empty;
                }

                var nearest = FindNearest();
                return nearest != null ? nearest.Prompt : string.Empty;
            }
        }

        private void OnInteractPressed()
        {
            var candidate = FindNearest();
            if (candidate != null && candidate.CanInteract && _player.IsAlive)
            {
                candidate.Interact(_player);
            }
        }

        private IInteractable FindNearest()
        {
            IInteractable best = null;
            var bestDistance = float.MaxValue;
            for (var i = _inRange.Count - 1; i >= 0; i--)
            {
                var candidate = _inRange[i];
                if (candidate == null || (candidate is Object unityObject && unityObject == null))
                {
                    _inRange.RemoveAt(i);
                    continue;
                }

                if (candidate is not Component component)
                {
                    best ??= candidate;
                    continue;
                }

                var distance = Vector2.SqrMagnitude(
                    component.transform.position - transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }
    }
}
