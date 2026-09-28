using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A body whose frames name where a loose piece of it hangs from — the hero's cloak, a hound's
    /// tail, a zombie's rags. <see cref="CloakTail"/> reads it; the clip's per-frame anchors
    /// supply it.
    /// </summary>
    public interface ITrailAnchorSource
    {
        /// <summary>The body the piece hangs off; its transform places the anchor.</summary>
        SpriteRenderer Body { get; }

        /// <summary>
        /// The hanging point on the frame now shown: x, y in units from the body's pivot, drawn
        /// facing right; z the angle it hangs at. False when the clip names none.
        /// </summary>
        bool TryGetTrailAnchor(out Vector3 anchor);
    }
}
