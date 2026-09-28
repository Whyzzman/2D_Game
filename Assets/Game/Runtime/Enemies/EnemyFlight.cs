using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The ash raven's flight (item 2 of the big list, 2026-09-28), the game's first enemy in the
    /// air. It holds its controller and flies itself: hovering over its perch, then — once it sees
    /// the hero — circling to a point above and to one side, rearing back (the attack's windup,
    /// its telegraph and sound) and diving at where the hero was, then climbing away to do it
    /// again. Upward strikes and the pogo are the answers. Dead, it falls; a rest puts it back on
    /// its perch.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController), typeof(EnemyMotor2D), typeof(Rigidbody2D))]
    public sealed class EnemyFlight : MonoBehaviour
    {
        public enum FlightState { Hover, Approach, Windup, Dive, Climb, Reel, Return }

        [SerializeField, Min(0.1f)] private float _flySpeed = 5f;
        [SerializeField, Min(0.1f)] private float _diveSpeed = 13f;
        [SerializeField, Min(0.05f)] private float _diveTime = 0.55f;
        [SerializeField, Min(0.05f)] private float _climbTime = 0.6f;
        [Tooltip("Where it waits before a dive, from the hero: to one side and up.")]
        [SerializeField] private Vector2 _perchOffset = new Vector2(2.6f, 3.2f);
        [SerializeField, Min(0f)] private float _pauseBetweenDives = 1.1f;
        [Tooltip("Out of sight this long, it gives up and flies home.")]
        [SerializeField, Min(0.5f)] private float _giveUpTime = 3f;
        [SerializeField, Min(0f)] private float _fallGravity = 3f;
        [SerializeField] private LayerMask _solidMask = 1 << 9;

        private EnemyController _enemy;
        private EnemyMotor2D _motor;
        private Rigidbody2D _body;
        private CombatActor _actor;
        private Vector2 _home;
        private Vector2 _diveDirection;
        private float _stateSince;
        private float _lastSeen;
        private float _lastDive = -10f;

        public FlightState State { get; private set; } = FlightState.Hover;

        /// <summary>Raised as a dive leaves its windup; where it is going.</summary>
        public event Action<Vector2> DiveStarted;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _motor = GetComponent<EnemyMotor2D>();
            _body = GetComponent<Rigidbody2D>();
            _actor = GetComponent<CombatActor>();
            _home = transform.position;
        }

        private void OnEnable()
        {
            _enemy.IsHeld = true;
            _enemy.Died += OnDied;
            _enemy.EnemyReset += OnReset;
            if (_actor != null) { _actor.DamageTaken += OnDamageTaken; }
            _body.gravityScale = 0f;
        }

        private void OnDisable()
        {
            _enemy.Died -= OnDied;
            _enemy.EnemyReset -= OnReset;
            if (_actor != null) { _actor.DamageTaken -= OnDamageTaken; }
        }

        private void FixedUpdate()
        {
            if (!_enemy.IsAlive)
            {
                return;
            }

            var position = (Vector2)transform.position;
            var target = _enemy.Target;
            var sees = CanSee(target, position);
            if (sees)
            {
                _lastSeen = Time.time;
            }

            switch (State)
            {
                case FlightState.Hover:
                    Fly(_home + new Vector2(0f, 0.3f * Mathf.Sin(Time.time * 2f)), _flySpeed * 0.4f);
                    if (sees) { Enter(FlightState.Approach); }
                    break;

                case FlightState.Approach:
                    if (target == null || Time.time - _lastSeen > _giveUpTime)
                    {
                        Enter(FlightState.Return);
                        break;
                    }

                    var side = position.x >= target.transform.position.x ? 1f : -1f;
                    var perch = (Vector2)target.transform.position + new Vector2(_perchOffset.x * side, _perchOffset.y);
                    Fly(perch, _flySpeed);
                    Face(target.transform.position.x - position.x);
                    if (Vector2.Distance(position, perch) < 0.7f && sees
                        && Time.time - _lastDive >= _pauseBetweenDives && _enemy.TryAttackWith(0))
                    {
                        Enter(FlightState.Windup);
                    }

                    break;

                case FlightState.Windup:
                    _motor.SetVelocity(Vector2.zero);
                    if (target != null) { Face(target.transform.position.x - position.x); }
                    if (!_enemy.IsAttacking)
                    {
                        Enter(FlightState.Climb);
                    }
                    else if (_enemy.AttackPhase != AttackPhase.Windup)
                    {
                        var aim = target != null ? (Vector2)target.transform.position : position + Vector2.down;
                        _diveDirection = (aim - position).normalized;
                        _lastDive = Time.time;
                        DiveStarted?.Invoke(aim);
                        Enter(FlightState.Dive);
                    }

                    break;

                case FlightState.Dive:
                    _motor.SetVelocity(_diveDirection * _diveSpeed);
                    if (Time.time - _stateSince >= _diveTime
                        || Physics2D.Raycast(position, _diveDirection, 0.7f, _solidMask))
                    {
                        Enter(FlightState.Climb);
                    }

                    break;

                case FlightState.Climb:
                case FlightState.Reel:
                    if (State == FlightState.Climb)
                    {
                        _motor.SetVelocity(new Vector2(-_diveDirection.x * _flySpeed * 0.4f, _flySpeed));
                    }

                    if (Time.time - _stateSince >= (State == FlightState.Climb ? _climbTime : 0.3f))
                    {
                        Enter(FlightState.Approach);
                    }

                    break;

                case FlightState.Return:
                    Fly(_home, _flySpeed * 0.8f);
                    if (sees) { Enter(FlightState.Approach); }
                    else if (Vector2.Distance(position, _home) < 0.4f) { Enter(FlightState.Hover); }
                    break;
            }
        }

        private void Enter(FlightState next)
        {
            State = next;
            _stateSince = Time.time;
        }

        private void Fly(Vector2 to, float speed)
        {
            var delta = to - (Vector2)transform.position;
            _motor.SetVelocity(delta.magnitude < 0.05f ? Vector2.zero
                : delta.normalized * Mathf.Min(speed, delta.magnitude / Time.fixedDeltaTime));
        }

        private void Face(float dx)
        {
            if (Mathf.Abs(dx) > 0.2f)
            {
                _motor.Face(dx > 0f ? 1 : -1);
            }
        }

        private bool CanSee(PlayerCharacter target, Vector2 from)
        {
            if (target == null || !target.IsAlive)
            {
                return false;
            }

            var to = (Vector2)target.transform.position + Vector2.up * 0.8f;
            return Vector2.Distance(from, to) <= _enemy.Definition.DetectionRange
                && !Physics2D.Linecast(from, to, _solidMask);
        }

        /// <summary>A hit knocks it back a little and breaks off whatever it was doing.</summary>
        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            if (!_enemy.IsAlive || State == FlightState.Dive)
            {
                return;
            }

            _motor.SetVelocity(info.HitDirection.normalized * 4f);
            Enter(FlightState.Reel);
        }

        private void OnDied(EnemyController enemy)
        {
            // Out of the sky.
            _body.gravityScale = _fallGravity;
        }

        private void OnReset(EnemyController enemy)
        {
            _body.gravityScale = 0f;
            _enemy.IsHeld = true;
            Enter(FlightState.Hover);
        }
    }
}
