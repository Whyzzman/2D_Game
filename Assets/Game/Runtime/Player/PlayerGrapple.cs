using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The grapple (item 2 of the big list, the user's choices of 2026-09-28): the Ashen King's
    /// gift. Its own button (G, the d-pad right) throws a line to the ring the hero faces within
    /// reach; the hero is pulled straight to it and let go just short of it with a small lift and
    /// the air jump and air dash back, so a line of rings is crossed hand over hand, as the walls
    /// already give moves back. Rings only; a ring behind a wall is not offered.
    ///
    /// Movement goes through the motor's skill velocity, as the Shadow dash does; the rope is a
    /// line drawn from the hand to the ring. Owning nothing, pressing does nothing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerGrapple : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _range = 7.5f;
        [SerializeField, Min(1f)] private float _pullSpeed = 21f;
        [Tooltip("Let go this close to the ring, so the hero swings up past it rather than into it.")]
        [SerializeField, Min(0.1f)] private float _releaseDistance = 0.8f;
        [SerializeField, Min(0.1f)] private float _maxPullTime = 0.9f;
        [Tooltip("Upward speed given on letting go: a pop over the ring, as in a swing.")]
        [SerializeField, Min(0f)] private float _releaseLift = 7f;
        [SerializeField] private LayerMask _solidMask = 1 << 9;
        [SerializeField] private Vector2 _handOffset = new Vector2(0.2f, 1.1f);

        private readonly List<Vector2> _anchors = new List<Vector2>();
        private PlayerMotor2D _motor;
        private PlayerInputReader _input;
        private PlayerCharacter _player;
        private GameProgress _progress;
        private LineRenderer _rope;
        private GrappleRing _highlighted;
        private GrappleRing _target;
        private float _pullStarted;

        /// <summary>Raised when the line is thrown and catches; the ring it caught.</summary>
        public event Action<GrappleRing> Latched;
        public event Action Released;

        public bool IsPulling => _target != null;
        public GrappleRing Target => _target;
        public bool IsOwned => _progress != null && _progress.HasAbility(AbilityIds.Grapple);

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor2D>();
            _input = GetComponent<PlayerInputReader>();
            _player = GetComponent<PlayerCharacter>();
            _progress = FindAnyObjectByType<GameProgress>();
            _rope = BuildRope();
        }

        private void OnDisable() => Release(lift: false);

        private void Update()
        {
            var candidate = IsOwned && !IsPulling ? FindRing() : null;
            if (candidate != _highlighted)
            {
                _highlighted?.SetTargeted(false);
                _highlighted = candidate;
                _highlighted?.SetTargeted(true);
            }

            if (_input != null && _input.GrapplePressedThisFrame)
            {
                TryGrapple();
            }

            DrawRope();
        }

        /// <summary>Throws the line at the ring in reach, if there is one; false otherwise.</summary>
        public bool TryGrapple()
        {
            if (!IsOwned || IsPulling || (_player != null && !_player.IsAlive))
            {
                return false;
            }

            var ring = FindRing();
            if (ring == null)
            {
                return false;
            }

            _highlighted?.SetTargeted(false);
            _highlighted = null;
            _motor.CancelDash();
            _target = ring;
            _pullStarted = Time.time;
            Latched?.Invoke(ring);
            return true;
        }

        private void FixedUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var toRing = _target.Anchor - Hand;
            if (!_target.isActiveAndEnabled || (_player != null && !_player.IsAlive)
                || Time.time - _pullStarted > _maxPullTime)
            {
                Release(lift: false);
                return;
            }

            if (toRing.magnitude <= _releaseDistance)
            {
                Release(lift: true);
                return;
            }

            _motor.SetSkillVelocity(toRing.normalized * _pullSpeed);
        }

        private void Release(bool lift)
        {
            if (_target == null)
            {
                return;
            }

            _target = null;
            _motor.SetSkillVelocity(null);
            var carried = _motor.Velocity;
            _motor.ApplyImpulse(new Vector2(carried.x * 0.5f, Mathf.Min(carried.y, 0f)));
            if (lift)
            {
                // A bounce, as a pogo is: the pop is the ring's, not cut by a released jump.
                _motor.Bounce(_releaseLift);
            }

            // Like a wall: the air jump and the air dash come back, so rings chain.
            _motor.RefreshAirMoves();
            Released?.Invoke();
        }

        private Vector2 Hand => (Vector2)transform.position
            + new Vector2(_handOffset.x * _motor.FacingDirection, _handOffset.y);

        private GrappleRing FindRing()
        {
            var rings = GrappleRing.All;
            if (rings.Count == 0)
            {
                return null;
            }

            _anchors.Clear();
            foreach (var ring in rings)
            {
                _anchors.Add(ring.Anchor);
            }

            var index = GrappleTargeting.Choose(Hand, _motor.FacingDirection, _anchors, _range,
                (from, to) => Physics2D.Linecast(from, to, _solidMask));
            return index >= 0 ? rings[index] : null;
        }

        private LineRenderer BuildRope()
        {
            var host = new GameObject("GrappleRope");
            host.transform.SetParent(transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.05f;
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                line.sharedMaterial = new Material(shader);
            }

            line.startColor = line.endColor = new Color(0.72f, 0.66f, 0.58f);
            line.sortingLayerName = "Characters";
            line.sortingOrder = 30;
            line.enabled = false;
            return line;
        }

        private void DrawRope()
        {
            if (_rope == null)
            {
                return;
            }

            _rope.enabled = _target != null;
            if (_target != null)
            {
                _rope.SetPosition(0, Hand);
                _rope.SetPosition(1, _target.Anchor);
            }
        }
    }
}
