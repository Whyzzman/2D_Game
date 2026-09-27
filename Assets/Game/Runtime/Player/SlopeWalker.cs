using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Walking on slopes. The movement logic keeps thinking in flat ground — run speed, jumps,
    /// dashes all tuned there — and this turns its answer into a velocity along the surface, so
    /// the player neither slides nor hops going down, nor stops at the foot of a rise, and runs
    /// at the same pace along it. Leaving the ground (a jump, a ledge) leaves the slope.
    /// </summary>
    public sealed class SlopeWalker
    {
        /// <summary>Surfaces flatter than this are level ground and need nothing.</summary>
        private const float LevelNormal = 0.999f;

        private float _flatX;

        public bool IsOnSlope { get; private set; }
        public Vector2 Normal { get; private set; } = Vector2.up;

        /// <summary>What the flat-ground logic should believe the velocity is.</summary>
        public Vector2 ToFlat(Vector2 velocity) => IsOnSlope ? new Vector2(_flatX, 0f) : velocity;

        /// <summary>
        /// The logic's flat answer laid onto the ground under the feet. Only while grounded and
        /// not rising: a jump off a slope is a jump, straight up from it.
        /// </summary>
        public Vector2 FromFlat(Vector2 flat, bool grounded, Vector2 groundNormal, float minimumGroundNormal)
        {
            if (!grounded || flat.y > 0f || groundNormal.y >= LevelNormal || groundNormal.y < minimumGroundNormal)
            {
                IsOnSlope = false;
                Normal = Vector2.up;
                return flat;
            }

            IsOnSlope = true;
            Normal = groundNormal.normalized;
            _flatX = flat.x;
            // Along the surface, to the right; the same speed the flat ground would give.
            var tangent = new Vector2(Normal.y, -Normal.x);
            return tangent * flat.x;
        }

        public void Reset()
        {
            IsOnSlope = false;
            Normal = Vector2.up;
            _flatX = 0f;
        }
    }
}
