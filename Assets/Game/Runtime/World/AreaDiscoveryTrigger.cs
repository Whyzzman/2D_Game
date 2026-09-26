using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Reports that the player is inside a specific area, for places the graph's bounds cannot
    /// describe honestly — a secret alcove next to the path, a room whose shape is not a box.
    ///
    /// Entering the same trigger again changes nothing: discovery is recorded once by
    /// <see cref="GameProgress"/>, and only a new area asks for a save.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AreaDiscoveryTrigger : MonoBehaviour
    {
        [SerializeField] private string _areaId = string.Empty;

        public string AreaId => _areaId;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                WorldState.Current?.ReportTrigger(_areaId, true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                WorldState.Current?.ReportTrigger(_areaId, false);
            }
        }

        private static bool IsPlayer(Collider2D other)
        {
            // Only the body counts: the player's interaction sensor and attack boxes are children
            // with their own colliders, and must not report the room twice.
            var player = other.GetComponentInParent<PlayerCharacter>();
            return player != null && other.attachedRigidbody != null
                && other.attachedRigidbody.gameObject == player.gameObject
                && other.gameObject == player.gameObject;
        }
    }
}
