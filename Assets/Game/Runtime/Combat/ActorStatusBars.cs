using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// World-space health and poise bars above an actor. This is a development aid for reading
    /// combat at a glance, not the game's interface, so it can be switched off per prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorStatusBars : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [SerializeField] private GameObject _root;
        [Tooltip("Pivot placed at the left edge of the bar; its X scale is the fill.")]
        [SerializeField] private Transform _healthFill;
        [SerializeField] private Transform _staggerFill;
        [SerializeField] private bool _isVisible = true;
        [Tooltip("On: hidden until the actor is hurt, then shown for a few seconds. A room of "
            + "full bars over every head is a prototype's look, not a game's.")]
        [SerializeField] private bool _showOnlyWhenHurt;
        [SerializeField, Min(0.1f)] private float _showFor = 3.5f;
        private Vector3 _healthScale = Vector3.one;
        private Vector3 _staggerScale = Vector3.one;
        private float _lastHealth = -1f;
        private float _hideAt;

        private void Awake()
        {
            if (_actor == null)
            {
                _actor = GetComponentInParent<CombatActor>();
            }

            if (_healthFill != null)
            {
                _healthScale = _healthFill.localScale;
            }

            if (_staggerFill != null)
            {
                _staggerScale = _staggerFill.localScale;
            }

            if (_root != null)
            {
                _root.SetActive(_isVisible && !_showOnlyWhenHurt);
            }
        }

        private void Update()
        {
            if (_showOnlyWhenHurt && _root != null && _root.activeSelf && Time.time >= _hideAt)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>Shows the bars for a while; a hit or a stagger build-up calls it.</summary>
        private void Reveal()
        {
            if (!_showOnlyWhenHurt || !_isVisible || _root == null || _actor == null
                || !_actor.IsAlive)
            {
                if (_root != null && _showOnlyWhenHurt && _actor != null && !_actor.IsAlive)
                {
                    _root.SetActive(false);
                }

                return;
            }

            _hideAt = Time.time + _showFor;
            _root.SetActive(true);
        }

        private void OnEnable()
        {
            if (_actor == null)
            {
                return;
            }

            _actor.Health.HealthChanged += OnHealthChanged;
            OnHealthChanged(_actor.Health.Current, _actor.Health.Max);
            if (_actor.Stagger != null)
            {
                _actor.Stagger.StaggerChanged += OnStaggerChanged;
                OnStaggerChanged(_actor.Stagger.Current, _actor.Stagger.Threshold);
            }
        }

        private void OnDisable()
        {
            if (_actor == null)
            {
                return;
            }

            _actor.Health.HealthChanged -= OnHealthChanged;
            if (_actor.Stagger != null)
            {
                _actor.Stagger.StaggerChanged -= OnStaggerChanged;
            }
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            Scale(_healthFill, _healthScale, max > 0f ? current / max : 0f);
            // Only a loss reveals the bars; a reset to full after a rest must not light up the room.
            if (_lastHealth >= 0f && current < _lastHealth)
            {
                Reveal();
            }
            else if (current >= max && _showOnlyWhenHurt && _root != null)
            {
                _root.SetActive(false);
            }

            _lastHealth = current;
        }

        private void OnStaggerChanged(float current, float threshold)
        {
            Scale(_staggerFill, _staggerScale, threshold > 0f ? current / threshold : 0f);
            if (current > 0f && _root != null && _root.activeSelf)
            {
                _hideAt = Time.time + _showFor;
            }
        }

        private static void Scale(Transform fill, Vector3 baseScale, float normalized)
        {
            if (fill != null)
            {
                fill.localScale = new Vector3(
                    baseScale.x * Mathf.Clamp01(normalized), baseScale.y, baseScale.z);
            }
        }
    }
}
