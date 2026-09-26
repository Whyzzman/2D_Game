using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The streak a blade leaves while it is actually swinging.
    ///
    /// It emits during the attack's real active window and nowhere else, which is what makes it
    /// information rather than decoration: the trail is on screen exactly while the hitbox is live.
    /// That also means it cannot linger into recovery and suggest the swing is still dangerous
    /// when it is not.
    ///
    /// Width comes from the equipped weapon, so a greatsword reads heavier than a sword without
    /// anyone writing a second trail.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class WeaponTrail : MonoBehaviour
    {
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private PlayerCombat _combat;
        [Tooltip("Width for a one-handed weapon, and for anything with no data of its own.")]
        [SerializeField, Min(0.01f)] private float _lightWidth = 0.16f;
        [Tooltip("Width for a two-hander; the whole reason a greatsword feels heavier.")]
        [SerializeField, Min(0.01f)] private float _heavyWidth = 0.34f;
        [SerializeField, Min(0.01f)] private float _lifetime = 0.14f;

        private void Awake()
        {
            if (_trail == null) { _trail = GetComponent<TrailRenderer>(); }
            if (_combat == null) { _combat = GetComponentInParent<PlayerCombat>(); }

            _trail.emitting = false;
            _trail.time = _lifetime;
            _trail.autodestruct = false;
        }

        private void OnEnable()
        {
            if (_combat != null)
            {
                _combat.AttackStarted += OnAttackStarted;
                _combat.AttackEnded += OnAttackEnded;
            }
        }

        private void OnDisable()
        {
            if (_combat != null)
            {
                _combat.AttackStarted -= OnAttackStarted;
                _combat.AttackEnded -= OnAttackEnded;
            }

            Stop();
        }

        private void OnAttackStarted(AttackDefinition attack)
        {
            var weapon = _combat != null ? _combat.Weapon : null;
            var isTwoHanded = weapon != null && weapon.WeaponType == WeaponType.Greatsword;
            var width = isTwoHanded ? _heavyWidth : _lightWidth;
            _trail.widthMultiplier = width;
            _trail.time = isTwoHanded ? _lifetime * 1.35f : _lifetime;
            _trail.Clear();
            _trail.emitting = true;
        }

        private void OnAttackEnded(AttackDefinition attack) => Stop();

        private void Stop()
        {
            if (_trail == null)
            {
                return;
            }

            _trail.emitting = false;
            // Cleared rather than left to fade: a streak still hanging in the air after the swing
            // reads as a swing that is still happening.
            _trail.Clear();
        }

        public bool IsEmitting => _trail != null && _trail.emitting;
    }
}
