using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The palace priest's craft (item 2 of the big list, 2026-09-28): in a fight it mends the
    /// most wounded ally near it, and when the hero reaches it, it steps away through the ash to
    /// somewhere further off. Both on their own cooldowns, so the answer — reach it and keep
    /// reaching it — always works. Its own attack is the definition's, fought by the controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemySupport : MonoBehaviour
    {
        [Header("Mending")]
        [SerializeField, Min(0.5f)] private float _healRange = 9f;
        [Tooltip("An ally below this share of its health is worth mending.")]
        [SerializeField, Range(0.1f, 1f)] private float _healBelow = 0.75f;
        [Tooltip("Share of the ally's maximum health given back.")]
        [SerializeField, Range(0.05f, 1f)] private float _healShare = 0.3f;
        [SerializeField, Min(0.5f)] private float _healCooldown = 7f;

        [Header("Stepping away")]
        [Tooltip("The hero this close is what makes it step away.")]
        [SerializeField, Min(0.5f)] private float _blinkTrigger = 2.6f;
        [SerializeField, Min(1f)] private float _blinkDistance = 5.5f;
        [SerializeField, Min(0.5f)] private float _blinkCooldown = 6f;
        [SerializeField] private LayerMask _groundMask = 1 << 9;

        private EnemyController _enemy;
        private EnemyMotor2D _motor;
        private CombatActor _actor;
        private EnemyRegistry _registry;
        private float _healReadyAt;
        private float _blinkReadyAt;

        /// <summary>The ally mended.</summary>
        public event Action<EnemyController> Healed;

        /// <summary>From where to where it stepped.</summary>
        public event Action<Vector2, Vector2> Blinked;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _motor = GetComponent<EnemyMotor2D>();
            _actor = GetComponent<CombatActor>();
        }

        private void Start()
        {
            _registry = FindAnyObjectByType<EnemyRegistry>();
            _enemy.EnemyReset += _ => { _healReadyAt = 0f; _blinkReadyAt = 0f; };
        }

        private bool InFight => _enemy.IsAlive && (_enemy.State == EnemyState.Chase
            || _enemy.State == EnemyState.Attack || _enemy.State == EnemyState.Alert);

        private void Update()
        {
            if (!InFight)
            {
                return;
            }

            if (Time.time >= _blinkReadyAt && !_enemy.IsAttacking)
            {
                TryBlink();
            }

            if (Time.time >= _healReadyAt)
            {
                TryHeal();
            }
        }

        /// <summary>Mends the most hurt ally in reach; false when nobody needs it.</summary>
        public bool TryHeal()
        {
            if (_registry == null)
            {
                return false;
            }

            EnemyController worst = null;
            var worstShare = _healBelow;
            foreach (var ally in _registry.Enemies)
            {
                if (ally == null || ally == _enemy || !ally.IsAlive
                    || Vector2.Distance(ally.transform.position, transform.position) > _healRange)
                {
                    continue;
                }

                var health = ally.GetComponent<CombatActor>().Health;
                if (health.Normalized < worstShare)
                {
                    worst = ally;
                    worstShare = health.Normalized;
                }
            }

            if (worst == null)
            {
                return false;
            }

            var mended = worst.GetComponent<CombatActor>().Health;
            mended.Heal(mended.Max * _healShare);
            _healReadyAt = Time.time + _healCooldown;
            Healed?.Invoke(worst);
            return true;
        }

        /// <summary>Steps away from a hero at arm's length, onto ground, not through a wall.</summary>
        public bool TryBlink()
        {
            var target = _enemy.Target;
            if (target == null || !target.IsAlive
                || Vector2.Distance(target.transform.position, transform.position) > _blinkTrigger)
            {
                return false;
            }

            var from = (Vector2)transform.position;
            var away = target.transform.position.x > from.x ? -1 : 1;
            foreach (var direction in new[] { away, -away })
            {
                var to = FindFooting(from, direction);
                if (to.HasValue)
                {
                    _motor.Teleport(to.Value);
                    _motor.Face(-direction);
                    _actor?.SetInvulnerable(ImmunitySource.Dodge, 0.3f);
                    _blinkReadyAt = Time.time + _blinkCooldown;
                    Blinked?.Invoke(from, to.Value);
                    return true;
                }
            }

            _blinkReadyAt = Time.time + 1f;
            return false;
        }

        private Vector2? FindFooting(Vector2 from, int direction)
        {
            // The body stands this high over its own ground; it lands as high over the new one.
            var under = Physics2D.Raycast(from, Vector2.down, 3f, _groundMask);
            var lift = under ? from.y - under.point.y : 0.9f;
            for (var step = _blinkDistance; step >= 2.5f; step -= 1f)
            {
                var x = from.x + direction * step;
                var chest = new Vector2(from.x, from.y + 1f);
                if (Physics2D.Linecast(chest, new Vector2(x, chest.y), _groundMask))
                {
                    continue;
                }

                var ground = Physics2D.Raycast(new Vector2(x, from.y + 2f), Vector2.down, 4.5f, _groundMask);
                if (ground)
                {
                    return new Vector2(x, ground.point.y + lift);
                }
            }

            return null;
        }
    }
}
