using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum ReactiveMotion
    {
        /// <summary>Hangs from its top: chains, garlands, lanterns, banners, moss.</summary>
        Hanging,
        /// <summary>Stands on its foot: roots, reeds, grass, burnt saplings.</summary>
        Standing
    }

    /// <summary>
    /// The world answering the hero without a single enemy about (GAME_FEEL_IDEAS item 13): a prop
    /// that sways as the hero brushes past, swings from a swing or a dash beside it, and shivers when
    /// the hero lands hard nearby — a damped spring about its anchor (the top of a hanging thing, the
    /// foot of a standing one). Presentation only: it has no collider and moves nothing else.
    ///
    /// The props are dressing already in the zone scenes; <c>SceneReactive.Apply</c> puts this on them
    /// by name. They find the hero themselves (a zone object finds the services it needs).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ReactiveProp : MonoBehaviour
    {
        [SerializeField] private ReactiveMotion _motion = ReactiveMotion.Hanging;
        [Tooltip("Half-width of the space where passing by moves it.")]
        [SerializeField, Min(0.1f)] private float _reach = 1.4f;
        [SerializeField, Range(1f, 40f)] private float _maxAngle = 12f;
        [SerializeField, Min(1f)] private float _stiffness = 22f;
        [SerializeField, Min(0f)] private float _damping = 3f;
        [Tooltip("Degrees per second of swing per unit of the hero's speed while inside the reach.")]
        [SerializeField, Min(0f)] private float _brush = 9f;

        private static readonly List<ReactiveProp> Live = new List<ReactiveProp>();

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector2 _anchor;
        private float _height = 1f;
        private float _angle;
        private float _velocity;
        private bool _resting = true;

        public ReactiveMotion Motion => _motion;
        public float Angle => _angle;

        /// <summary>Set by the scene recipe from the prop's name.</summary>
        public void Configure(ReactiveMotion motion, float reach, float maxAngle)
        {
            _motion = motion;
            _reach = reach;
            _maxAngle = maxAngle;
        }

        private void OnEnable()
        {
            Live.Add(this);
            HeroSignals.Attach();
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
            var bounds = Bounds();
            _height = Mathf.Max(0.2f, bounds.size.y);
            _anchor = _motion == ReactiveMotion.Hanging
                ? new Vector2(bounds.center.x, bounds.max.y)
                : new Vector2(bounds.center.x, bounds.min.y);
        }

        private void OnDisable()
        {
            Live.Remove(this);
            Settle();
        }

        /// <summary>Every prop near <paramref name="at"/> takes a swing, the nearer the more.</summary>
        public static void Gust(Vector2 at, float radius, float strength, float direction)
        {
            foreach (var prop in Live)
            {
                var offset = prop.Middle - at;
                if (Mathf.Abs(offset.x) > radius + prop._reach || Mathf.Abs(offset.y) > radius + prop._height)
                {
                    continue;
                }

                var falloff = 1f - Mathf.Clamp01(offset.magnitude / (radius + prop._reach));
                var side = Mathf.Abs(direction) > 0.01f ? Mathf.Sign(direction) : Mathf.Sign(offset.x == 0f ? 1f : offset.x);
                prop.Push(side * strength * falloff);
            }
        }

        private Vector2 Middle => _anchor + Vector2.down * (_motion == ReactiveMotion.Hanging ? _height * 0.5f : -_height * 0.5f);

        /// <summary>Hanging things swing away from the push at the bottom, standing ones bend at the top.</summary>
        private void Push(float degreesPerSecond)
        {
            _velocity += _motion == ReactiveMotion.Hanging ? degreesPerSecond : -degreesPerSecond;
            _resting = false;
        }

        private void Update()
        {
            var dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) { return; }

            var hero = HeroSignals.Hero;
            if (hero == null) { HeroSignals.Attach(); }
            else
            {
                var offset = (Vector2)hero.position - _anchor;
                var vertical = _motion == ReactiveMotion.Hanging ? -offset.y : offset.y;
                if (Mathf.Abs(offset.x) < _reach && vertical > -1.2f && vertical < _height + 0.4f)
                {
                    var speed = HeroSignals.Velocity.x;
                    if (Mathf.Abs(speed) > 0.5f) { Push(speed * _brush * dt * 4f); }
                }
            }

            if (_resting) { return; }

            _velocity += (-_stiffness * _angle - _damping * _velocity) * dt;
            _angle = Mathf.Clamp(_angle + _velocity * dt, -_maxAngle, _maxAngle);
            if (Mathf.Abs(_angle) < 0.02f && Mathf.Abs(_velocity) < 0.05f)
            {
                Settle();
                return;
            }

            var turn = Quaternion.Euler(0f, 0f, _angle);
            transform.SetPositionAndRotation(
                (Vector3)_anchor + turn * (_basePosition - (Vector3)_anchor),
                turn * _baseRotation);
        }

        private void Settle()
        {
            _angle = 0f;
            _velocity = 0f;
            _resting = true;
            transform.SetPositionAndRotation(_basePosition, _baseRotation);
        }

        private Bounds Bounds()
        {
            var renderer = GetComponentInChildren<SpriteRenderer>();
            return renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.one);
        }
    }

    /// <summary>
    /// The hero as the world's props see it: where it is, how fast, and the moments that stir the air
    /// (a swing, a dash, a hard landing). Found once and kept; re-found if the hero is replaced.
    /// </summary>
    public static class HeroSignals
    {
        private static PlayerCharacter _player;
        private static PlayerMotor2D _motor;
        private static float _nextSearch;

        /// <summary>A dash went off here, facing this way. Lights and props listen.</summary>
        public static event Action<Vector2, float> Dashed;

        public static Transform Hero => _player != null ? _player.transform : null;
        public static Vector2 Velocity => _motor != null ? _motor.Velocity : Vector2.zero;

        public static void Attach()
        {
            if (_player != null || Time.unscaledTime < _nextSearch) { return; }

            _nextSearch = Time.unscaledTime + 1f;
            _player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            if (_player == null) { return; }

            _motor = _player.GetComponent<PlayerMotor2D>();
            _player.DashStarted += OnDash;
            if (_player.Combat != null) { _player.Combat.AttackStarted += OnSwing; }
        }

        private static void OnDash()
        {
            if (_player == null) { return; }
            var facing = _motor != null ? _motor.FacingDirection : 1;
            Dashed?.Invoke(_player.transform.position, facing);
            ReactiveProp.Gust(_player.transform.position, 1.6f, 90f, facing);
        }

        private static void OnSwing(AttackDefinition attack)
        {
            if (_player == null) { return; }
            var facing = _motor != null ? _motor.FacingDirection : 1;
            ReactiveProp.Gust((Vector2)_player.transform.position + new Vector2(facing * 1.2f, 0.9f), 1f, 70f, facing);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            _player = null;
            _motor = null;
            _nextSearch = 0f;
            Dashed = null;
        }
    }
}
