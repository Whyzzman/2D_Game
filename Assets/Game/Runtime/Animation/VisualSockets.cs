using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Named attachment points on an actor, so effects never guess at offsets.</summary>
    public enum VisualSocket
    {
        /// <summary>Centre of mass: hurt flashes, status overlays, shadow dissolve.</summary>
        Body,
        /// <summary>Between the feet, on the ground line: dust, landing, dash puffs.</summary>
        Ground,
        /// <summary>The weapon's grip; the blade and its trail hang from here.</summary>
        Weapon,
        /// <summary>The casting hand for the player; the runed core for the Guardian.</summary>
        Cast,
        /// <summary>Head height: stun, guard break, level-up, the Guardian's eyes.</summary>
        Overhead,
        /// <summary>The shield face, where a blocked blow strikes. Shield carriers only.</summary>
        Shield
    }

    /// <summary>
    /// One lookup for every point an effect can be spawned at, on any actor. Without this each
    /// effect would carry its own hardcoded offset, and moving a character's proportions would
    /// silently misplace all of them at once.
    ///
    /// Sockets live under the visual, so they inherit the facing flip the motor already applies and
    /// an effect spawned at the weapon hand appears on the correct side by construction.
    ///
    /// The same component serves the player, the ordinary enemies and the boss. A socket an actor
    /// does not have simply falls back, so a shield spark asked for on a zombie lands on its body
    /// rather than throwing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VisualSockets : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _ground;
        [SerializeField] private Transform _weapon;
        [SerializeField] private Transform _cast;
        [SerializeField] private Transform _overhead;
        [Tooltip("Only on shield carriers; everything else leaves this empty.")]
        [SerializeField] private Transform _shield;

        /// <summary>
        /// The transform for a socket, falling back to the actor root. An effect in a slightly
        /// wrong place is a visual flaw; a null reference during combat is a crash, so this never
        /// returns null.
        /// </summary>
        public Transform Socket(VisualSocket socket)
        {
            switch (socket)
            {
                case VisualSocket.Body: return _body != null ? _body : transform;
                case VisualSocket.Ground: return _ground != null ? _ground : transform;
                case VisualSocket.Weapon: return _weapon != null ? _weapon : transform;
                case VisualSocket.Cast: return _cast != null ? _cast : transform;
                case VisualSocket.Overhead: return _overhead != null ? _overhead : transform;
                case VisualSocket.Shield:
                    return _shield != null ? _shield : Socket(VisualSocket.Body);
                default: return transform;
            }
        }

        public Vector3 Position(VisualSocket socket) => Socket(socket).position;

        public bool Has(VisualSocket socket) => socket switch
        {
            VisualSocket.Body => _body != null,
            VisualSocket.Ground => _ground != null,
            VisualSocket.Weapon => _weapon != null,
            VisualSocket.Cast => _cast != null,
            VisualSocket.Overhead => _overhead != null,
            VisualSocket.Shield => _shield != null,
            _ => false
        };

        /// <summary>
        /// True when every socket an actor of this kind needs is wired; used by the content build
        /// and the tests to catch omissions. The shield is excluded because most actors have none.
        /// </summary>
        public bool IsComplete => _body != null && _ground != null && _weapon != null
            && _cast != null && _overhead != null;
    }
}
