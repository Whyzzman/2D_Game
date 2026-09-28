using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A wall that is not quite like the others (item 4 of the big list, the user's choice of
    /// 2026-09-28): a faint crack, and three blows from the hero bring it down, as in Hollow
    /// Knight. Behind it is a secret room under a veil that lifts once the wall falls. The fall is
    /// remembered for good under the wall's id; a load finds it already open.
    ///
    /// Only the hero's blows count, one per hit however hard; the wall's health is a formality
    /// that refills, so it can never be killed like an enemy.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatActor))]
    public sealed class BreakableWall : MonoBehaviour
    {
        [SerializeField] private string _wallId = string.Empty;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private CombatActor _actor;
        [SerializeField, Min(1)] private int _hitsToBreak = 3;
        [Tooltip("What stops the hero: turned off when the wall falls.")]
        [SerializeField] private Collider2D _solid;
        [SerializeField] private Collider2D _hurtbox;
        [SerializeField] private SpriteRenderer _visual;
        [Tooltip("Darkness over the room behind; it fades once the wall is down.")]
        [SerializeField] private SpriteRenderer _veil;
        [SerializeField, Min(0.05f)] private float _veilFade = 0.8f;
        [SerializeField, Min(0f)] private float _shakeTime = 0.18f;

        private Color _baseColor = Color.white;
        private Vector3 _restPosition;
        private float _shakeUntil;
        private float _veilAlpha = 1f;
        private float _veilStart = 1f;

        /// <summary>Raised when the wall is struck down, not when a load finds it open.</summary>
        public static event Action<BreakableWall> Broken;

        public string WallId => _wallId;
        public int Hits { get; private set; }
        public bool IsBroken { get; private set; }
        public float VeilAlpha => _veil != null ? _veil.color.a : 0f;

        private void Awake()
        {
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_visual != null)
            {
                _baseColor = _visual.color;
                _restPosition = _visual.transform.localPosition;
            }

            if (_veil != null) { _veilStart = _veil.color.a; }
            if (string.IsNullOrEmpty(_wallId))
            {
                Debug.LogError("Breakable wall has no id; it could never be saved.", this);
            }
        }

        private void OnEnable()
        {
            _actor.DamageTaken += OnDamageTaken;
            if (_progress != null) { _progress.Loaded += ApplySaved; }
        }

        private void OnDisable()
        {
            _actor.DamageTaken -= OnDamageTaken;
            if (_progress != null) { _progress.Loaded -= ApplySaved; }
        }

        private void Start() => ApplySaved();

        private void ApplySaved()
        {
            if (_progress != null && _progress.IsPickupCollected(_wallId))
            {
                SetBroken(true);
                SetVeil(0f);
            }
            else if (IsBroken)
            {
                // A new game over a world where it had fallen: the wall stands again.
                IsBroken = false;
                Hits = 0;
                SetBroken(false);
                SetVeil(_veilStart);
            }
        }

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            _actor.Health.RestoreToFull();
            if (IsBroken || info.Attacker == null
                || info.Attacker.GetComponentInParent<PlayerCharacter>() == null)
            {
                return;
            }

            Hits++;
            _shakeUntil = Time.time + _shakeTime;
            if (_visual != null)
            {
                // Each blow shows: the stone darkens a little more toward the break.
                var wear = Mathf.Clamp01((float)Hits / _hitsToBreak) * 0.35f;
                _baseColor.a = _visual.color.a;
                _visual.color = Color.Lerp(_baseColor, Color.black, wear);
            }

            if (Hits < _hitsToBreak)
            {
                AudioManager.Play(AudioId.WallCrack, transform.position);
                return;
            }

            if (_progress != null)
            {
                _progress.MarkPickupCollected(_wallId);
                _progress.SaveNow();
            }

            AudioManager.Play(AudioId.WallCrumble, transform.position);
            SetBroken(true);
            Broken?.Invoke(this);
        }

        private void SetBroken(bool broken)
        {
            IsBroken = broken;
            if (_solid != null) { _solid.enabled = !broken; }
            if (_hurtbox != null) { _hurtbox.enabled = !broken; }
            if (_visual != null)
            {
                foreach (var part in _visual.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    part.enabled = !broken;
                }

                _visual.color = _baseColor;
                _visual.transform.localPosition = _restPosition;
            }
        }

        private void SetVeil(float alpha)
        {
            _veilAlpha = alpha;
            if (_veil == null)
            {
                return;
            }

            var color = _veil.color;
            color.a = alpha;
            _veil.color = color;
            _veil.enabled = alpha > 0.001f;
        }

        private void Update()
        {
            if (_visual != null && !IsBroken)
            {
                _visual.transform.localPosition = Time.time < _shakeUntil
                    ? _restPosition + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.05f)
                    : _restPosition;
            }

            if (IsBroken && _veilAlpha > 0f)
            {
                SetVeil(Mathf.Max(0f, _veilAlpha - Time.deltaTime * _veilStart / _veilFade));
            }
        }
    }
}
