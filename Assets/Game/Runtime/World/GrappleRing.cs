using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// An iron ring the grapple catches on (item 2 of the big list, the user's choice of
    /// 2026-09-28: rings only, never any ceiling). Rings announce themselves, so the grapple never
    /// searches the scene; the one the grapple would take right now glows.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GrappleRing : MonoBehaviour
    {
        private static readonly List<GrappleRing> _all = new List<GrappleRing>();

        [Tooltip("Lit while this is the ring the grapple would catch.")]
        [SerializeField] private SpriteRenderer _glow;

        public static IReadOnlyList<GrappleRing> All => _all;

        public Vector2 Anchor => transform.position;
        public bool IsTargeted { get; private set; }

        private void OnEnable() => _all.Add(this);

        private void OnDisable()
        {
            _all.Remove(this);
            SetTargeted(false);
        }

        public void SetTargeted(bool targeted)
        {
            IsTargeted = targeted;
            if (_glow != null)
            {
                _glow.enabled = targeted;
            }
        }
    }

    /// <summary>
    /// Which ring the grapple takes, as a pure question: the nearest one in reach that the hero is
    /// facing or aiming at, with nothing solid in between. A ring overhead counts whichever way
    /// the hero faces; one behind does not, so turning round is how the other one is chosen.
    /// </summary>
    public static class GrappleTargeting
    {
        /// <summary>How far behind the hero's front a ring may be and still be taken (a ring straight up).</summary>
        public const float BehindAllowance = 0.6f;

        public static int Choose(Vector2 origin, int facing, IReadOnlyList<Vector2> anchors, float range,
            System.Func<Vector2, Vector2, bool> isBlocked)
        {
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < anchors.Count; i++)
            {
                var delta = anchors[i] - origin;
                var distance = delta.magnitude;
                // Behind the hero is out, unless the ring hangs mostly overhead: a ring straight up is taken
                // whichever way he faces (2026-11-05: a step forward put the ring over his head a hand behind
                // the hand, and the line refused it).
                var overhead = delta.y > 0f && Mathf.Abs(delta.x) <= delta.y * 0.75f;
                if (distance > range || distance < 0.3f || (delta.x * facing < -BehindAllowance && !overhead))
                {
                    continue;
                }

                if (distance >= bestDistance || (isBlocked != null && isBlocked(origin, anchors[i])))
                {
                    continue;
                }

                best = i;
                bestDistance = distance;
            }

            return best;
        }
    }
}
