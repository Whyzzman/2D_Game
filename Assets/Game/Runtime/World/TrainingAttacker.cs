using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A stationary sparring partner with no AI: it faces the player and cycles through a
    /// telegraphed normal, heavy and unblockable attack, which is what the defensive mechanics need
    /// to be tested against.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatActor))]
    public sealed class TrainingAttacker : MonoBehaviour, IFacing
    {
        [SerializeField] private CombatActor _actor;
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private Transform _visual;
        [SerializeField] private SpriteRenderer _telegraph;
        [Tooltip("Pause between attacks.")]
        [SerializeField, Min(0.1f)] private float _interval = 2.6f;
        [Tooltip("The player must be this close for an attack to start.")]
        [SerializeField, Min(0f)] private float _range = 2.6f;
        [SerializeField] private AttackDefinition[] _attacks =
        {
            new AttackDefinition()
        };
        [Tooltip("Telegraph colour per attack; the last one is reused when the list is shorter.")]
        [SerializeField] private Color[] _telegraphColors =
        {
            new Color(1f, 0.85f, 0.3f)
        };
        private readonly AttackRunner _runner = new AttackRunner();
        private Transform _player;
        private Vector3 _visualScale = Vector3.one;
        private float _nextAttackAt;
        private int _attackIndex;
        private int _facing = -1;

        public event Action<AttackDefinition> AttackTelegraphed;

        public CombatActor Actor => _actor;
        public int FacingDirection => _facing;
        public AttackDefinition CurrentAttack => _runner.Current;
        public bool IsAttacking => _runner.IsBusy;

        private void Awake()
        {
            if (_actor == null)
            {
                _actor = GetComponent<CombatActor>();
            }

            if (_hitbox == null)
            {
                Debug.LogError("TrainingAttacker requires a hitbox.", this);
                enabled = false;
                return;
            }

            if (_visual != null)
            {
                _visualScale = _visual.localScale;
            }

            SetTelegraph(false, Color.white);
        }

        private void OnEnable()
        {
            _runner.ActiveStarted += OnActiveStarted;
            _runner.ActiveEnded += OnActiveEnded;
            _runner.Finished += OnFinished;
        }

        private void OnDisable()
        {
            _runner.ActiveStarted -= OnActiveStarted;
            _runner.ActiveEnded -= OnActiveEnded;
            _runner.Finished -= OnFinished;
            _runner.Cancel();
            _hitbox?.End();
            SetTelegraph(false, Color.white);
        }

        private void Start()
        {
            var player = FindAnyObjectByType<PlayerCharacter>();
            if (player != null)
            {
                _player = player.transform;
            }
            _nextAttackAt = Time.time + _interval;
        }

        private void FixedUpdate()
        {
            _runner.Tick(Time.fixedDeltaTime);
            if (_runner.IsBusy || _player == null || !_actor.IsAlive)
            {
                return;
            }

            FaceThePlayer();
            if (Time.time < _nextAttackAt
                || Mathf.Abs(_player.position.x - transform.position.x) > _range)
            {
                return;
            }

            BeginNextAttack();
        }

        private void BeginNextAttack()
        {
            var attack = _attacks != null && _attacks.Length > 0
                ? _attacks[_attackIndex % _attacks.Length]
                : null;
            if (attack == null)
            {
                return;
            }

            SetTelegraph(true, TelegraphColor(_attackIndex % Mathf.Max(1, _attacks.Length)));
            _runner.Begin(attack, 1f);
            AttackTelegraphed?.Invoke(attack);
            _attackIndex++;
        }

        private void FaceThePlayer()
        {
            var delta = _player.position.x - transform.position.x;
            if (Mathf.Abs(delta) < 0.05f)
            {
                return;
            }

            _facing = delta > 0f ? 1 : -1;
            if (_visual != null)
            {
                _visual.localScale = new Vector3(
                    Mathf.Abs(_visualScale.x) * _facing, _visualScale.y, _visualScale.z);
            }
        }

        private Color TelegraphColor(int index)
        {
            if (_telegraphColors == null || _telegraphColors.Length == 0)
            {
                return new Color(1f, 0.85f, 0.3f);
            }

            return _telegraphColors[Mathf.Min(index, _telegraphColors.Length - 1)];
        }

        private void OnActiveStarted(AttackDefinition attack)
        {
            _hitbox.Begin(attack.BuildDamage(gameObject, this, _facing),
                attack.HitboxOffset, attack.HitboxSize, _facing);
        }

        private void OnActiveEnded()
        {
            _hitbox.End();
        }

        private void OnFinished(AttackDefinition attack)
        {
            SetTelegraph(false, Color.white);
            _nextAttackAt = Time.time + _interval;
        }

        private void SetTelegraph(bool isVisible, Color color)
        {
            if (_telegraph == null)
            {
                return;
            }

            _telegraph.enabled = isVisible;
            _telegraph.color = color;
        }
    }
}
