using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A box that is live only during an attack's active frames. It sweeps for hurtboxes instead of
    /// relying on trigger callbacks, so a window of two or three physics steps cannot be missed.
    /// One activation hits each target at most once.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private bool _drawGizmo = true;
        private readonly Collider2D[] _overlaps = new Collider2D[16];
        private readonly HashSet<CombatActor> _alreadyHit = new HashSet<CombatActor>();
        private ContactFilter2D _filter;
        private CombatActor _owner;
        private IDamageAugment _augment;
        private DamageInfo _template;
        private Vector2 _localOffset;
        private Vector2 _size;
        private int _facing = 1;

        public event Action<CombatActor, DamageResult> Hit;

        public bool IsActive { get; private set; }

        private void Awake()
        {
            _filter = new ContactFilter2D { useTriggers = true };
            _filter.SetLayerMask(_targetMask);
            _owner = GetComponentInParent<CombatActor>();
            _augment = GetComponentInParent<IDamageAugment>();
        }

        private void OnDisable()
        {
            End();
        }

        public void Begin(in DamageInfo template, Vector2 localOffset, Vector2 size, int facing)
        {
            _template = template;
            _localOffset = localOffset;
            _size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            _facing = facing >= 0 ? 1 : -1;
            _alreadyHit.Clear();
            IsActive = true;
            // Sweep immediately so a one-step window still connects.
            Sweep();
        }

        public void End()
        {
            IsActive = false;
            _alreadyHit.Clear();
        }

        private void FixedUpdate()
        {
            if (IsActive)
            {
                Sweep();
            }
        }

        private void Sweep()
        {
            var center = Center;
            var count = Physics2D.OverlapBox(center, _size, 0f, _filter, _overlaps);
            for (var i = 0; i < count; i++)
            {
                var hurtbox = _overlaps[i].GetComponentInParent<Hurtbox>();
                if (hurtbox == null || hurtbox.Owner == null || IsSameSide(hurtbox.Owner)
                    || !_alreadyHit.Add(hurtbox.Owner))
                {
                    continue;
                }

                var contact = _overlaps[i].ClosestPoint(center);
                var direction = new Vector2(
                    Mathf.Approximately(contact.x, center.x) ? _facing : Mathf.Sign(contact.x - center.x),
                    0f);
                // The target is only known here, so this is where bonuses that depend on it are
                // decided — and where a critical is rolled, once per target per activation.
                var aimed = _template.AimedAt(contact, direction);
                if (_augment != null)
                {
                    aimed = _augment.Augment(aimed, hurtbox.Owner);
                }

                var result = hurtbox.Owner.ApplyDamage(aimed);
                if (result.HasConnected)
                {
                    HitStop.Request(_template.HitStop);
                    Hit?.Invoke(hurtbox.Owner, result);
                }
            }
        }

        /// <summary>
        /// Friendly fire is off: an attacker never hits its own faction. Neutral targets such as
        /// training dummies are always valid.
        /// </summary>
        private bool IsSameSide(CombatActor target)
        {
            return _owner != null
                && target.Faction != Faction.Neutral
                && target.Faction == _owner.Faction;
        }

        private Vector2 Center => (Vector2)transform.position
            + new Vector2(_localOffset.x * _facing, _localOffset.y);

        private void OnDrawGizmos()
        {
            if (!_drawGizmo || !IsActive)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.65f);
            Gizmos.DrawWireCube(Center, _size);
        }
    }
}
