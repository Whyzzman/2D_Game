using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom
{
    /// <summary>One zone's colour grade: a global volume that is fully on while the player is there.</summary>
    [System.Serializable]
    public sealed class ZoneGrade
    {
        [SerializeField] private string _zoneId = string.Empty;
        [SerializeField] private Volume _volume;

        public string ZoneId => _zoneId;
        public Volume Volume => _volume;
    }

    /// <summary>
    /// The picture's post-processing (visual item 2, the user's choices of 2026-09-28: restrained,
    /// no film grain, a boss fight a little harsher, one switch in the settings).
    ///
    /// Three kinds of global volume, all children of the camera. Each zone has its grade, cross-
    /// faded when the player crosses a border. The effects volume — bloom and vignette — is the
    /// switchable part: the setting turns it off and leaves the zones' colour alone. The boss volume
    /// adds contrast and takes some colour out while a boss fights, more in its second phase, and
    /// eases away after; it also tightens the vignette a little.
    ///
    /// Reads the world and the bosses, never the other way round: nothing in gameplay knows this
    /// exists.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenGrade : MonoBehaviour
    {
        [SerializeField] private ZoneGrade[] _zones = System.Array.Empty<ZoneGrade>();
        [Tooltip("The zone graded when no world is loaded, as on the title screen.")]
        [SerializeField] private string _defaultZoneId = string.Empty;
        [SerializeField] private Volume _effects;
        [SerializeField] private Volume _boss;
        [SerializeField, Min(0.05f)] private float _zoneFadeTime = 1.4f;
        [SerializeField, Min(0.05f)] private float _bossFadeTime = 1.2f;
        [Tooltip("Share of the boss grade in the first phase; the second phase is all of it.")]
        [SerializeField, Range(0f, 1f)] private float _phaseOneShare = 0.6f;
        [Tooltip("Vignette added on top of the effects volume's at the full boss grade.")]
        [SerializeField, Range(0f, 0.5f)] private float _bossVignette = 0.1f;

        [Header("Eclipse (a boss's third phase: the King's)")]
        [Tooltip("Exposure taken off the whole picture at the full eclipse: the arena goes dark, and what "
            + "still shows is what gives light — the crown, a blow's flash, the telegraph of a zone.")]
        [SerializeField, Range(-4f, 0f)] private float _eclipseExposure = -1.5f;
        [SerializeField, Range(-100f, 0f)] private float _eclipseSaturation = -40f;
        [SerializeField, Range(0f, 0.5f)] private float _eclipseVignette = 0.18f;
        [SerializeField, Min(0.05f)] private float _eclipseFadeIn = 2.5f;
        [Tooltip("The light coming back when the fight ends: slower, the dawn after it.")]
        [SerializeField, Min(0.05f)] private float _eclipseFadeOut = 5f;

        private BossController[] _bosses = System.Array.Empty<BossController>();
        private Volume _eclipse;
        private Vignette _vignette;
        private float _baseVignette;
        private bool _snapped;

        public float BossAmount { get; private set; }

        /// <summary>How far the eclipse has come, 0–1.</summary>
        public float EclipseAmount { get; private set; }
        public Volume Effects => _effects;
        public Volume Boss => _boss;

        /// <summary>The grade's weight for a zone right now; 0 for a zone it does not know.</summary>
        public float ZoneWeight(string zoneId)
        {
            foreach (var zone in _zones)
            {
                if (zone != null && zone.ZoneId == zoneId && zone.Volume != null)
                {
                    return zone.Volume.weight;
                }
            }

            return 0f;
        }

        /// <summary>How much of the boss grade a fight in this state wants.</summary>
        public static float BossTarget(bool fighting, int phase, float phaseOneShare) =>
            !fighting ? 0f : phase >= 2 ? 1f : phaseOneShare;

        /// <summary>A fight in its third phase is under the eclipse; anything else is not.</summary>
        public static float EclipseTarget(bool fighting, int phase) => fighting && phase >= 3 ? 1f : 0f;

        private const float MaxFadeStep = 0.1f;

        private void Awake()
        {
            GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;
            if (_effects != null && _effects.profile != null && _effects.profile.TryGet(out _vignette))
            {
                // .profile hands back this volume's own copy, so the boss can tighten the vignette
                // without writing into the shared asset.
                _baseVignette = _vignette.intensity.value;
            }

            _eclipse = BuildEclipse();
        }

        /// <summary>
        /// Built in code rather than kept as an asset: two values of one override, and only those two
        /// are overridden, so the zone's own colour filter and contrast still come through the dark.
        /// </summary>
        private Volume BuildEclipse()
        {
            var host = new GameObject("GradeEclipse");
            host.transform.SetParent(transform, false);
            var volume = host.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 3f;
            volume.weight = 0f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var colour = profile.Add<ColorAdjustments>(false);
            colour.postExposure.Override(_eclipseExposure);
            colour.saturation.Override(_eclipseSaturation);
            volume.sharedProfile = profile;
            return volume;
        }

        private void Start()
        {
            FindBosses();
            ZoneStreamer.ZonesChanged += FindBosses;
        }

        private void OnDestroy()
        {
            ZoneStreamer.ZonesChanged -= FindBosses;
            if (_eclipse != null && _eclipse.sharedProfile != null)
            {
                Destroy(_eclipse.sharedProfile);
            }
        }

        private void FindBosses() => _bosses = ZoneObjects.All<BossController>(includeInactive: true);

        private void Update()
        {
            // A long frame (a zone streaming in) must not skip the fade it is part of.
            var dt = Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
            var current = WorldState.Current != null && !string.IsNullOrEmpty(WorldState.Current.CurrentZoneId)
                ? WorldState.Current.CurrentZoneId
                : _defaultZoneId;
            foreach (var zone in _zones)
            {
                if (zone?.Volume == null)
                {
                    continue;
                }

                var target = zone.ZoneId == current ? 1f : 0f;
                zone.Volume.weight = _snapped
                    ? Mathf.MoveTowards(zone.Volume.weight, target, dt / _zoneFadeTime)
                    : target;
            }

            _snapped = true;

            var bossTarget = 0f;
            var eclipseTarget = 0f;
            foreach (var boss in _bosses)
            {
                if (boss != null && boss.isActiveAndEnabled)
                {
                    bossTarget = Mathf.Max(bossTarget, BossTarget(boss.IsFighting, boss.Phase, _phaseOneShare));
                    eclipseTarget = Mathf.Max(eclipseTarget, EclipseTarget(boss.IsFighting, boss.Phase));
                }
            }

            EclipseAmount = Mathf.MoveTowards(EclipseAmount, eclipseTarget,
                dt / (eclipseTarget > EclipseAmount ? _eclipseFadeIn : _eclipseFadeOut));
            if (_eclipse != null)
            {
                _eclipse.weight = EclipseAmount;
            }

            BossAmount = Mathf.MoveTowards(BossAmount, bossTarget, dt / _bossFadeTime);
            if (_boss != null)
            {
                _boss.weight = BossAmount;
            }

            if (_effects != null)
            {
                _effects.weight = GameSettings.Current.postEffects ? 1f : 0f;
            }

            if (_vignette != null)
            {
                _vignette.intensity.value = _baseVignette + _bossVignette * BossAmount + _eclipseVignette * EclipseAmount;
            }
        }
    }
}
