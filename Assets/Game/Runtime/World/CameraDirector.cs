using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom
{
    /// <summary>
    /// The camera's reactions to the fight, kept apart from the follow itself: it widens for a
    /// boss so the whole arena and every telegraph are on screen, and gives a parry a short push
    /// in — the beat that makes the timing feel earned.
    ///
    /// Restrained on purpose: a slow widen over a second, a punch of a few percent for a tenth of
    /// a second, and the reduced-motion setting turns the punch off.
    ///
    /// It also gives the follow its limits from the world graph: the extent of this scene's
    /// areas, so the view never shows past the world's ends, and for a boss fight the arena's
    /// area, so the fight stays framed. The areas are coarse slabs today; tighter per-room
    /// rectangles can replace them later without touching the camera.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraFollow2D))]
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField, Range(1f, 1.4f)] private float _bossZoom = 1.18f;
        [SerializeField, Min(0.05f)] private float _bossZoomTime = 1.1f;
        [SerializeField, Range(0f, 0.2f)] private float _parryPunch = 0.06f;
        [SerializeField, Min(0.02f)] private float _parryPunchTime = 0.12f;

        private CameraFollow2D _follow;
        private BossEncounter[] _encounters = System.Array.Empty<BossEncounter>();
        private PlayerCharacter _player;
        private GameProgress _progress;

        private void Awake()
        {
            _follow = GetComponent<CameraFollow2D>();
            _encounters = FindObjectsByType<BossEncounter>(FindObjectsInactive.Include);
            _player = FindAnyObjectByType<PlayerCharacter>();
            _progress = FindAnyObjectByType<GameProgress>();
        }

        private void Start()
        {
            _follow.SetWorldBounds(SceneExtent());
        }

        private void OnEnable()
        {
            foreach (var encounter in _encounters)
            {
                encounter.FightStarted += OnFightStarted;
                encounter.FightEnded += OnFightEnded;
            }

            if (_player != null)
            {
                _player.Parried += OnParried;
            }
        }

        private void OnDisable()
        {
            foreach (var encounter in _encounters)
            {
                if (encounter != null)
                {
                    encounter.FightStarted -= OnFightStarted;
                    encounter.FightEnded -= OnFightEnded;
                }
            }

            if (_player != null)
            {
                _player.Parried -= OnParried;
            }
        }

        private void OnFightStarted(BossController boss)
        {
            _follow.SetZoom(_bossZoom, _bossZoomTime);
            var graph = _progress != null ? _progress.World : null;
            var arena = graph != null && boss != null
                ? graph.AreaAt(SceneManager.GetActiveScene().name, boss.transform.position)
                : null;
            _follow.SetFightBounds(arena != null ? arena.WorldBounds : (Rect?)null);
        }

        private void OnFightEnded(BossController boss)
        {
            _follow.SetZoom(1f, _bossZoomTime * 1.5f);
            _follow.SetFightBounds(null);
        }

        /// <summary>Every area of this scene put together, or nothing when there is no graph.</summary>
        private Rect? SceneExtent()
        {
            var graph = _progress != null ? _progress.World : null;
            if (graph == null)
            {
                return null;
            }

            var scene = SceneManager.GetActiveScene().name;
            Rect? extent = null;
            foreach (var area in graph.Areas)
            {
                var zone = area != null ? graph.Zone(area.ZoneId) : null;
                if (area == null || (zone != null && !string.IsNullOrEmpty(zone.SceneName)
                    && zone.SceneName != scene))
                {
                    continue;
                }

                var bounds = area.WorldBounds;
                extent = extent.HasValue
                    ? Rect.MinMaxRect(Mathf.Min(extent.Value.xMin, bounds.xMin),
                        Mathf.Min(extent.Value.yMin, bounds.yMin),
                        Mathf.Max(extent.Value.xMax, bounds.xMax),
                        Mathf.Max(extent.Value.yMax, bounds.yMax))
                    : bounds;
            }

            return extent;
        }

        private void OnParried(DamageInfo info)
        {
            if (!FeedbackSettings.ReducedMotion)
            {
                _follow.Punch(_parryPunch, _parryPunchTime);
            }
        }
    }
}
