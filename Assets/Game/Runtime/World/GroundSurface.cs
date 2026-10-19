using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What the ground is made of, for the feet: the step's sound and the dust's colour.
    /// Stored as a number on the blocks — new kinds only at the end.</summary>
    public enum SurfaceKind
    {
        Stone,
        Wood,
        Earth,
        Water,
        Ash
    }

    /// <summary>
    /// The material of one block of ground (GAME_FEEL_IDEAS item 4). The level builder tags blocks
    /// from the level text (planks are wood, the Forest floor earth, the Capital's ash); a block with
    /// no tag takes its zone's default (<see cref="Surfaces.DefaultFor"/>). Presentation only: no
    /// surface walks differently.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GroundSurface : MonoBehaviour
    {
        [SerializeField] private SurfaceKind _kind;

        public SurfaceKind Kind => _kind;

        public void Set(SurfaceKind kind) => _kind = kind;
    }

    /// <summary>Asks what is under the feet, and what that sounds and looks like.</summary>
    public static class Surfaces
    {
        private const float ProbeDepth = 0.45f;
        private static int _groundMask = -1;

        /// <summary>The block under <paramref name="feet"/>, or the zone's default over a gap.</summary>
        public static SurfaceKind Under(Vector2 feet)
        {
            if (_groundMask < 0) { _groundMask = LayerMask.GetMask("Ground"); }
            var hit = Physics2D.Raycast(feet + Vector2.up * 0.1f, Vector2.down, ProbeDepth, _groundMask);
            if (hit.collider != null)
            {
                var tagged = hit.collider.GetComponentInParent<GroundSurface>();
                if (tagged != null) { return tagged.Kind; }
            }

            var streamer = ZoneStreamer.Current;
            return DefaultFor(streamer != null ? streamer.ZoneAt(feet) : string.Empty);
        }

        /// <summary>What a zone is mostly made of, for blocks that carry no tag of their own.</summary>
        public static SurfaceKind DefaultFor(string zoneId)
        {
            switch (zoneId)
            {
                case "zone_withered_forest":
                case "zone_fallen_village":
                case "zone_plague_marsh":
                    return SurfaceKind.Earth;
                case "zone_ashen_capital":
                case "zone_ashen_depths":
                    return SurfaceKind.Ash;
                case "zone_royal_aqueducts":
                    return SurfaceKind.Water;
                default:
                    return SurfaceKind.Stone;
            }
        }

        /// <summary>The step. Every surface has its own, so a zone is felt through the feet.</summary>
        public static AudioId Footstep(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Wood: return AudioId.FootstepWood;
                case SurfaceKind.Earth: return AudioId.FootstepEarth;
                case SurfaceKind.Water: return AudioId.FootstepWater;
                case SurfaceKind.Ash: return AudioId.FootstepAsh;
                default: return AudioId.FootstepStone;
            }
        }

        /// <summary>
        /// Multiplies the pale movement dust: grey stone grit, warm splinters, dark loam, a cold
        /// spray, charcoal ash.
        /// </summary>
        public static Color DustTint(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Wood: return new Color(1f, 0.86f, 0.66f, 1f);
                case SurfaceKind.Earth: return new Color(0.78f, 0.82f, 0.62f, 1f);
                case SurfaceKind.Water: return new Color(0.72f, 0.9f, 1f, 0.85f);
                case SurfaceKind.Ash: return new Color(0.58f, 0.57f, 0.6f, 1f);
                default: return Color.white;
            }
        }
    }
}
