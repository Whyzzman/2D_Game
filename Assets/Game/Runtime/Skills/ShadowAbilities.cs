using UnityEngine;

namespace ForgottenKingdom
{
    public enum ShadowAction { None, Dash, Strike }

    /// <summary>Movement and targeting for Shadow; slots, costs and timing remain in active skills.</summary>
    [DisallowMultipleComponent]
    public sealed class ShadowAbilities : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _dashSpeed = 18f;
        [SerializeField, Min(0f)] private float _dashIFrames = 0.15f;
        [SerializeField, Min(0f)] private float _strikeRange = 3f;
        [SerializeField, Min(0f)] private float _strikeVerticalRange = 1.2f;
        [SerializeField, Min(0f)] private float _strikeSeparation = 1.2f;
        [SerializeField, Min(0f)] private float _strikePhysical = 20f;
        [SerializeField, Min(0f)] private float _strikeDark = 25f;
        [SerializeField, Min(0f)] private float _dexterityScaling = 0.02f;
        [SerializeField, Min(0f)] private float _cuttingDamage = 12f;
        [SerializeField] private LayerMask _solidMask = 1 << 9;
        private readonly Collider2D[] _targets = new Collider2D[32];
        private PlayerMotor2D _motor;
        private PlayerStats _stats;
        private CombatActor _actor;
        private PlayerSkills _skills;
        private int _direction;
        public event System.Action<ShadowAction> Started;
        public event System.Action Ended;
        public ShadowAction Action { get; private set; }
        public int Facing => _direction;
        public bool CanCut => _skills != null && _skills.Effect(SkillEffectType.CuttingShadow) > 0f;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor2D>();
            _stats = GetComponent<PlayerStats>();
            _actor = GetComponent<CombatActor>();
            _skills = FindAnyObjectByType<PlayerSkills>();
        }

        public void Begin(ShadowAction action)
        {
            Action = action;
            _direction = _motor.DashDirection;
            if (action == ShadowAction.Dash)
            {
                _actor.SetInvulnerable(ImmunitySource.Shadow, _dashIFrames);
                _motor.SetSkillVelocity(new Vector2(_direction * _dashSpeed, 0f));
            }
            Started?.Invoke(action);
        }

        public void StrikeDisplacement()
        {
            var target = FindTarget();
            if (target == null) { return; }
            var targetPosition = (Vector2)target.transform.position;
            var destination = new Vector2(targetPosition.x + _direction * _strikeSeparation,
                _motor.transform.position.y);
            if (_motor.TryDisplace(destination))
            {
                _direction = -_direction;
            }
            else
            {
                _motor.TryDisplace(new Vector2(targetPosition.x - _direction * _strikeSeparation,
                    _motor.transform.position.y));
            }
        }

        private CombatActor FindTarget()
        {
            var origin = (Vector2)transform.position;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(1 << 10);
            var count = Physics2D.OverlapCircle(origin, _strikeRange, filter, _targets);
            CombatActor closest = null;
            var best = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var target = _targets[i].GetComponentInParent<CombatActor>();
                if (target == null || !target.IsAlive || target.Faction == _actor.Faction) { continue; }
                var delta = (Vector2)target.transform.position - origin;
                if (delta.x * _direction < 0f || Mathf.Abs(delta.y) > _strikeVerticalRange
                    || delta.sqrMagnitude >= best
                    || Physics2D.Raycast(origin, delta.normalized, delta.magnitude, _solidMask)) { continue; }
                closest = target;
                best = delta.sqrMagnitude;
            }
            return closest;
        }

        public DamageInfo BuildDamage(AttackDefinition attack)
        {
            var power = _stats != null ? _stats.ShadowPower : 1f;
            var dexterity = _stats != null ? _stats.GetFinalStat(CharacterStat.Dexterity) : 0;
            var damage = Action == ShadowAction.Dash ? _cuttingDamage * power
                : _strikePhysical * (1f + dexterity * _dexterityScaling) + _strikeDark * power;
            return new DamageInfo(damage, new Vector2(_direction, 0f), DamageType.Dark,
                gameObject, this, knockback: attack.Knockback, staggerDamage: attack.StaggerDamage,
                guardDamage: attack.GuardDamage, hitStop: attack.HitStop,
                tags: DamageTags.Shadow | DamageTags.Melee);
        }

        public void Cancel()
        {
            if (Action == ShadowAction.None) { return; }
            _motor.SetSkillVelocity(null);
            _actor.Invulnerability.Deactivate(ImmunitySource.Shadow);
            Action = ShadowAction.None;
            Ended?.Invoke();
        }
    }
}
