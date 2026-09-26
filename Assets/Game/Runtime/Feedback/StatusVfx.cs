using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The overlays that say what is currently wrong with a creature: frost on its edges, flames
    /// on its body, poison drifting off it.
    ///
    /// Restraint is the whole design. A creature can carry three statuses at once, and if each one
    /// painted the whole sprite the result would be an unreadable blob exactly when the player most
    /// needs to see a wind-up. So each overlay is small, only the two most important are shown at
    /// a time, and none of them ever tints the creature itself — the Visual Bible's rule that a
    /// status must never recolour a silhouette.
    ///
    /// It reads <see cref="StatusEffects"/> and changes nothing about it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusEffects))]
    public sealed class StatusVfx : MonoBehaviour
    {
        [SerializeField] private StatusEffects _statuses;
        [Tooltip("Where overlays sit; normally the body socket.")]
        [SerializeField] private Transform _anchor;
        [SerializeField] private Sprite _frostSprite;
        [SerializeField] private Sprite _burnSprite;
        [SerializeField] private Sprite _poisonSprite;
        [Tooltip("Two at once is already busy; three hides the creature.")]
        [SerializeField, Min(1)] private int _maximumConcurrent = 2;
        [SerializeField, Min(0.1f)] private float _scale = 1f;
        private SpriteRenderer[] _slots;

        private void Awake()
        {
            if (_statuses == null) { _statuses = GetComponent<StatusEffects>(); }
            if (_anchor == null) { _anchor = transform; }

            _slots = new SpriteRenderer[_maximumConcurrent];
            for (var i = 0; i < _slots.Length; i++)
            {
                var host = new GameObject($"StatusOverlay{i}");
                host.transform.SetParent(_anchor, false);
                host.transform.localPosition = Vector3.zero;
                host.transform.localScale = Vector3.one * _scale;
                var renderer = host.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
                // In front of the body but behind impact bursts: an overlay must never cover the
                // spark that tells the player their hit connected.
                renderer.sortingLayerName = "VFXFront";
                renderer.sortingOrder = -1;
                _slots[i] = renderer;
            }
        }

        /// <summary>
        /// Frost first, then burn, then poison. Frost changes how the creature moves, so it is the
        /// one the player most needs to know about; poison only ticks, so it yields.
        /// </summary>
        private void LateUpdate()
        {
            if (_statuses == null || _slots == null)
            {
                return;
            }

            var used = 0;
            used = TryShow(StatusType.Frost, _frostSprite, new Color(0.56f, 0.84f, 1f, 0.75f), used);
            used = TryShow(StatusType.Burn, _burnSprite, new Color(1f, 0.55f, 0.2f, 0.8f), used);
            used = TryShow(StatusType.Poison, _poisonSprite, new Color(0.5f, 0.66f, 0.24f, 0.7f),
                used);

            for (var i = used; i < _slots.Length; i++)
            {
                _slots[i].enabled = false;
            }
        }

        private int TryShow(StatusType type, Sprite sprite, Color tint, int used)
        {
            if (used >= _slots.Length || sprite == null || _statuses.Remaining(type) <= 0f)
            {
                return used;
            }

            var renderer = _slots[used];
            renderer.enabled = true;
            renderer.sprite = sprite;
            // A slow breath rather than a pulse: a throbbing overlay reads as a game object,
            // a drifting one reads as something happening to the creature.
            var breath = 1f + Mathf.Sin(Time.time * 2.6f + used) * 0.06f;
            renderer.color = new Color(tint.r, tint.g, tint.b, tint.a * breath);
            renderer.transform.localScale = Vector3.one * _scale * breath;
            return used + 1;
        }

        /// <summary>Hides everything at once, for death, reset and respawn.</summary>
        public void Clear()
        {
            if (_slots == null)
            {
                return;
            }

            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                    slot.enabled = false;
                }
            }
        }

        /// <summary>How many overlays are currently shown; used by tests and the debug readout.</summary>
        public int ActiveOverlays
        {
            get
            {
                if (_slots == null)
                {
                    return 0;
                }

                var count = 0;
                foreach (var slot in _slots)
                {
                    if (slot != null && slot.enabled)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
