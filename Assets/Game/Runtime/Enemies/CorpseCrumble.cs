using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A body that does not stay (visual item 4, the user's choice of 2026-09-28): once the death
    /// clip has played and the body has lain still a moment, it greys and crumbles into ash that
    /// drifts up, and is gone. A rest revives the enemy exactly as before — the visual comes back
    /// with it — so nothing about the world's respawn rules changes.
    ///
    /// Presentation only: it hides the visual root, never the enemy. A body lost down a pit is
    /// already hidden and does not crumble.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorpseCrumble : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private ParticleSystem _ash;
        [Tooltip("Seconds from death to the first crack: the death clip, then a still moment.")]
        [SerializeField, Min(0f)] private float _delay = 2.3f;
        [SerializeField, Min(0.05f)] private float _crumbleTime = 0.7f;
        [SerializeField] private Color _ashTint = new Color(0.36f, 0.34f, 0.32f);

        private EnemyController _enemy;
        private float _diedAt = -1f;
        private float _crumbleStarted = -1f;
        private Color _bodyColor = Color.white;
        private Vector3 _bodyScale = Vector3.one;
        private bool _captured;

        public bool IsCrumbling => _crumbleStarted >= 0f;
        public bool IsGone { get; private set; }

        private void Awake()
        {
            if (_health == null) { _health = GetComponent<Health>(); }
            _enemy = GetComponent<EnemyController>();
        }

        private void OnEnable()
        {
            if (_health != null) { _health.Died += OnDied; }
            if (_enemy != null) { _enemy.EnemyReset += OnReset; }
        }

        private void OnDisable()
        {
            if (_health != null) { _health.Died -= OnDied; }
            if (_enemy != null) { _enemy.EnemyReset -= OnReset; }
        }

        private void OnDied()
        {
            _diedAt = Time.time;
            _crumbleStarted = -1f;
            IsGone = false;
        }

        private void OnReset(EnemyController enemy) => Restore();

        private void Update()
        {
            if (_diedAt < 0f || IsGone)
            {
                return;
            }

            if (_crumbleStarted < 0f)
            {
                if (Time.time - _diedAt < _delay)
                {
                    return;
                }

                if (_visualRoot == null || !_visualRoot.activeSelf)
                {
                    // Lost down a pit: nothing left to crumble.
                    _diedAt = -1f;
                    return;
                }

                Begin();
            }

            var t = Mathf.Clamp01((Time.time - _crumbleStarted) / _crumbleTime);
            if (_body != null)
            {
                var grey = Color.Lerp(_bodyColor, _ashTint * _bodyColor, Mathf.Clamp01(t * 2.5f));
                grey.a = _bodyColor.a * (1f - t * t);
                _body.color = grey;
                // It settles as it goes, sinking into its own dust.
                _body.transform.localScale = new Vector3(_bodyScale.x * (1f + 0.08f * t),
                    _bodyScale.y * (1f - 0.35f * t * t), _bodyScale.z);
            }

            if (t >= 1f)
            {
                Finish();
            }
        }

        private void Begin()
        {
            _crumbleStarted = Time.time;
            if (_body != null)
            {
                _bodyColor = _body.color;
                _bodyScale = _body.transform.localScale;
                _captured = true;
            }

            if (_ash != null)
            {
                _ash.transform.position = _body != null ? _body.bounds.center : transform.position;
                if (_body != null)
                {
                    var shape = _ash.shape;
                    shape.scale = new Vector3(Mathf.Max(0.2f, _body.bounds.size.x * 0.8f),
                        Mathf.Max(0.2f, _body.bounds.size.y * 0.8f), 1f);
                }

                _ash.Play(true);
            }

            AudioManager.Play(AudioId.CorpseCrumble, transform.position);
        }

        private void Finish()
        {
            IsGone = true;
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(false);
            }

            RestoreBody();
        }

        private void Restore()
        {
            _diedAt = -1f;
            _crumbleStarted = -1f;
            IsGone = false;
            RestoreBody();
        }

        private void RestoreBody()
        {
            // Only what Begin captured: before a crumble the body is as the animation left it.
            if (_body != null && _captured)
            {
                _body.color = _bodyColor;
                _body.transform.localScale = _bodyScale;
                _captured = false;
            }
        }
    }
}
