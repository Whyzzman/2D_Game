using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A ledge or plank (<c>=</c> in a zone's text) that is solid only from above: a jump from under
    /// it passes through and lands on top instead of knocking the head (user's request 2026-10-25).
    /// Walls and the rest of the stone stay solid on every side.
    ///
    /// The hero's motor sweeps its own moves, so it asks <see cref="Holds"/> rather than relying on
    /// the effector alone; the effector keeps the physics solver and other bodies in agreement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class OneWayPlatform : MonoBehaviour
    {
        /// <summary>How far the feet may sink below the top and still count as standing on it.</summary>
        public const float Tolerance = 0.05f;

        private Collider2D _collider;

        public float Top => (_collider != null ? _collider : GetComponent<Collider2D>()).bounds.max.y;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            var effector = GetComponent<PlatformEffector2D>();
            if (effector == null)
            {
                effector = gameObject.AddComponent<PlatformEffector2D>();
            }

            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            effector.useSideFriction = false;
            effector.useSideBounce = false;
            _collider.usedByEffector = true;
        }

        /// <summary>Whether feet at <paramref name="feetY"/> are on or above the top, so the ledge holds them.</summary>
        public bool Holds(float feetY) => feetY >= Top - Tolerance;

        /// <summary>
        /// Whether <paramref name="collider"/> stops a body whose feet are at <paramref name="feetY"/>:
        /// any ordinary collider does; a one-way ledge only from above.
        /// </summary>
        public static bool Stops(Collider2D collider, float feetY) =>
            collider == null || !collider.TryGetComponent<OneWayPlatform>(out var ledge) || ledge.Holds(feetY);

        /// <summary>Whether <paramref name="collider"/> is a one-way ledge at all.</summary>
        public static bool Is(Collider2D collider) => collider != null && collider.TryGetComponent<OneWayPlatform>(out _);
    }
}
