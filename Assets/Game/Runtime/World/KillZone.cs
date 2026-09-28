using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A pit or a hazard that is simply fatal. It kills through the ordinary death flow rather
    /// than teleporting anyone, so respawn, world reset and the fade all behave as usual.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class KillZone : MonoBehaviour
    {
        [SerializeField] private PlayerRespawner _respawner;

        private void Reset()
        {
            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null || !player.IsAlive)
            {
                return;
            }

            if (_respawner == null)
            {
                // A pit of a zone scene; the respawner is in the world scene.
                _respawner = FindAnyObjectByType<PlayerRespawner>();
            }

            if (_respawner != null)
            {
                _respawner.Kill();
                return;
            }

            // Without a respawner this still has to be lethal, never survivable.
            player.Actor.ClearInvulnerability();
            player.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.up,
                isUnblockable: true, isParryable: false));
        }
    }
}
