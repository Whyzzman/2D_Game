using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The gargoyle's sleep (item 2 of the big list, 2026-09-28): it stands on its plinth as a
    /// statue, holding its controller, until the hero comes within reach or strikes it; then it
    /// takes a moment to tear itself loose and fights like anyone. A rest or a death puts it back
    /// to sleep where it stood.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyDormancy : MonoBehaviour
    {
        [Tooltip("How close the hero comes before the statue moves.")]
        [SerializeField, Min(0.5f)] private float _wakeRange = 3.5f;
        [Tooltip("From the first stir to the first step: the time to back off or strike first.")]
        [SerializeField, Min(0f)] private float _wakeTime = 0.9f;

        private EnemyController _enemy;
        private CombatActor _actor;
        private float _wakingSince = -1f;

        public bool IsAsleep { get; private set; }
        public bool IsWaking => _wakingSince >= 0f;
        public event Action Woke;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _actor = GetComponent<CombatActor>();
        }

        private void OnEnable()
        {
            _enemy.EnemyReset += OnReset;
            if (_actor != null) { _actor.DamageTaken += OnDamageTaken; }
            Sleep();
        }

        private void OnDisable()
        {
            _enemy.EnemyReset -= OnReset;
            if (_actor != null) { _actor.DamageTaken -= OnDamageTaken; }
        }

        private void Update()
        {
            if (!IsAsleep || !_enemy.IsAlive)
            {
                return;
            }

            if (_wakingSince < 0f)
            {
                var target = _enemy.Target;
                if (target != null && target.IsAlive
                    && Vector2.Distance(target.transform.position, transform.position) <= _wakeRange)
                {
                    _wakingSince = Time.time;
                }

                return;
            }

            if (Time.time - _wakingSince >= _wakeTime)
            {
                IsAsleep = false;
                _wakingSince = -1f;
                _enemy.IsHeld = false;
                Woke?.Invoke();
            }
        }

        /// <summary>Back on the plinth, still, till the next one walks past.</summary>
        public void Sleep()
        {
            IsAsleep = true;
            _wakingSince = -1f;
            _enemy.IsHeld = true;
        }

        private void OnReset(EnemyController enemy) => Sleep();

        /// <summary>Struck while it sleeps: no slow waking, it is up at once.</summary>
        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (IsAsleep && _enemy.IsAlive)
            {
                _wakingSince = Time.time - _wakeTime;
            }
        }
    }
}
