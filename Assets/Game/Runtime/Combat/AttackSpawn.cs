using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What an attack puts into the world instead of, or besides, swinging a hitbox.</summary>
    public enum AttackSpawnKind
    {
        /// <summary>An ordinary melee swing; the hitbox does all the work.</summary>
        None,
        /// <summary>Fires a <see cref="SpellProjectile"/> along the attacker's facing.</summary>
        Projectile,
        /// <summary>Leaves a <see cref="HazardZone"/> on the ground.</summary>
        Hazard
    }

    /// <summary>
    /// The ranged half of an attack, as data.
    ///
    /// Before this, an enemy attack could only be a hitbox in front of the attacker, so an archer
    /// or a caster had nowhere to exist. Rather than give the archer its own firing code — and then
    /// the shaman its own, and then the boss a third — the emission is described here and shared by
    /// <see cref="EnemyAttackOption"/> and <see cref="BossAttack"/> alike. What comes out is an
    /// ordinary projectile or hazard carrying an ordinary <see cref="DamageInfo"/>, so an arrow is
    /// blocked, resisted and mitigated by exactly the code a sword swing is.
    /// </summary>
    [Serializable]
    public sealed class AttackSpawn
    {
        [SerializeField] private AttackSpawnKind _kind = AttackSpawnKind.None;
        [Tooltip("Projectile or hazard prefab. Without one the attack stays a plain swing.")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("Muzzle or drop point, relative to the attacker. X is mirrored by facing.")]
        [SerializeField] private Vector2 _offset = new Vector2(0.7f, 0.2f);

        [Header("Projectile")]
        [SerializeField, Min(0.1f)] private float _speed = 9f;
        [SerializeField, Min(0.5f)] private float _range = 14f;
        [SerializeField, Min(1)] private int _pierce = 1;
        [SerializeField] private Color _color = Color.white;

        [Header("Hazard")]
        [Tooltip("Drop the zone under the target rather than in front of the attacker.")]
        [SerializeField] private bool _atTargetPosition = true;
        [Tooltip("How far from the attacker the zone may be placed, so it cannot cross the arena.")]
        [SerializeField, Min(0f)] private float _maxPlacementDistance = 7f;

        [Header("Hazard pattern")]
        [SerializeField] private HazardPattern _pattern = HazardPattern.Single;
        [Tooltip("How many zones a rain drops, or how many steps a march takes each way.")]
        [SerializeField, Min(1)] private int _count = 1;
        [Tooltip("Seconds between one zone and the next.")]
        [SerializeField, Min(0f)] private float _interval = 0.25f;
        [Tooltip("March, Converge: distance between neighbouring zones.")]
        [SerializeField, Min(0.1f)] private float _spacing = 2.5f;
        [Tooltip("March: the row runs behind the attacker too.")]
        [SerializeField] private bool _bothSides;
        [Tooltip("March: every second use lands in the gaps of the one before it.")]
        [SerializeField] private bool _alternate;

        [NonSerialized] private int _emissions;

        public AttackSpawnKind Kind => _kind;
        public HazardPattern Pattern => _pattern;
        public GameObject Prefab => _prefab;
        public Vector2 Offset => _offset;
        public float Speed => _speed;
        public float Range => _range;
        public int Pierce => _pierce;
        public Color Color => _color;
        public bool AtTargetPosition => _atTargetPosition;
        public float MaxPlacementDistance => _maxPlacementDistance;

        /// <summary>True when this actually has something to emit.</summary>
        public bool IsActive => _kind != AttackSpawnKind.None && _prefab != null;

        /// <summary>
        /// Puts the thing into the world. Called at the moment the swing turns active, so the
        /// telegraph the player read is the telegraph the shot honours.
        /// </summary>
        public void Emit(in DamageInfo damage, Faction faction, Transform attacker, int facing,
            Transform target)
        {
            if (!IsActive || attacker == null)
            {
                return;
            }

            var origin = (Vector2)attacker.position
                + new Vector2(_offset.x * Mathf.Sign(facing == 0 ? 1 : facing), _offset.y);

            if (_kind == AttackSpawnKind.Projectile)
            {
                EmitProjectile(damage, faction, origin, facing, target);
                return;
            }

            EmitHazard(damage, faction, attacker, origin, facing, target);
        }

        private void EmitProjectile(in DamageInfo damage, Faction faction, Vector2 origin,
            int facing, Transform target)
        {
            var instance = UnityEngine.Object.Instantiate(_prefab, origin, Quaternion.identity);
            var projectile = instance.GetComponent<SpellProjectile>();
            if (projectile == null)
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            // Aimed along facing, with only the height of the target taken into account. A shot
            // that tracked the target fully would be unreadable, and one locked flat could never
            // reach a player standing on a ledge.
            var direction = new Vector2(facing >= 0 ? 1f : -1f, 0f);
            if (target != null)
            {
                var rise = Mathf.Clamp(target.position.y - origin.y, -2.5f, 2.5f);
                var run = Mathf.Max(1.5f, Mathf.Abs(target.position.x - origin.x));
                direction = new Vector2(direction.x, rise / run);
            }

            // Re-labelled as a projectile: the swing data said how hard it hits, not how it flies.
            var shot = damage.WithTags(DamageTags.Projectile).AimedAt(origin, direction);
            projectile.Launch(shot, faction, direction, _speed, _range, _pierce, _color);
        }

        private void EmitHazard(in DamageInfo damage, Faction faction, Transform attacker,
            Vector2 origin, int facing, Transform target)
        {
            if (_pattern == HazardPattern.Rain)
            {
                HazardVolley.Rain(_prefab, faction, damage.Attacker, target, _count, _interval);
                return;
            }

            if (_pattern == HazardPattern.March || _pattern == HazardPattern.Converge)
            {
                var steps = HazardPatterns.March(_count, Mathf.Abs(_offset.x), _spacing, _bothSides,
                    _alternate && _emissions++ % 2 == 1);
                HazardVolley.March(_prefab, faction, damage.Attacker, attacker.position, facing, steps, _interval,
                    converge: _pattern == HazardPattern.Converge);
                return;
            }

            var position = origin;
            if (_atTargetPosition && target != null)
            {
                var offset = Mathf.Clamp(target.position.x - attacker.position.x,
                    -_maxPlacementDistance, _maxPlacementDistance);
                position = new Vector2(attacker.position.x + offset, target.position.y + _offset.y);
            }

            var instance = UnityEngine.Object.Instantiate(_prefab, position, Quaternion.identity);
            var zone = instance.GetComponent<HazardZone>();
            if (zone == null)
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            zone.Arm(faction, damage.Attacker);
        }
    }
}
