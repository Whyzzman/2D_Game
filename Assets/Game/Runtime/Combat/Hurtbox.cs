using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Marks a collider as a damageable part of an actor. Hitboxes only look for this component, so
    /// the collider can live on any child without the attacker knowing the body layout.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private CombatActor _owner;

        public CombatActor Owner => _owner;

        private void Reset()
        {
            _owner = GetComponentInParent<CombatActor>();
        }

        private void Awake()
        {
            if (_owner == null)
            {
                _owner = GetComponentInParent<CombatActor>();
            }

            if (_owner == null)
            {
                Debug.LogError("Hurtbox requires a CombatActor on itself or a parent.", this);
                enabled = false;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var bounds = GetComponent<Collider2D>().bounds;
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.7f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
