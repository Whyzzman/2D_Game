using System.Collections.Generic;
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
    /// areas, so the view never shows past the world's ends; the room the player is in
    /// (<see cref="CameraRooms"/>, rooms narrower than the view joined together), sliding into the
    /// next one when the player crosses over; and
    /// for a boss fight the arena's room, so the fight stays framed.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CameraFollow2D))]
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField, Range(1f, 1.4f)] private float _bossZoom = 1.18f;
        [SerializeField, Min(0.05f)] private float _bossZoomTime = 1.1f;
        [SerializeField, Range(0f, 0.2f)] private float _parryPunch = 0.06f;
        [SerializeField, Min(0.02f)] private float _parryPunchTime = 0.12f;
        [Tooltip("How long the view takes to slide into the next room.")]
        [SerializeField, Min(0f)] private float _roomChangeTime = 0.5f;

        private CameraFollow2D _follow;
        private BossEncounter[] _encounters = System.Array.Empty<BossEncounter>();
        private PlayerCharacter _player;
        private GameProgress _progress;
        private PlayerMotor2D _motor;
        private CameraRooms _rooms = new CameraRooms();
        private bool _hasGround;
        private int _room = -1;

        /// <summary>The rooms of this scene, for tests and debugging.</summary>
        public CameraRooms Rooms => _rooms;

        private void Awake()
        {
            _follow = GetComponent<CameraFollow2D>();
            _player = FindAnyObjectByType<PlayerCharacter>();
            _progress = FindAnyObjectByType<GameProgress>();
            _motor = _player != null ? _player.GetComponent<PlayerMotor2D>() : null;
        }

        private void Start()
        {
            _follow.SetWorldBounds(SceneExtent());
            _rooms = BuildRooms();
            FollowRoom(0f);
            _follow.SnapToTarget();
        }

        private void Update()
        {
            FollowRoom(_roomChangeTime);
        }

        private void OnEnable()
        {
            ZoneStreamer.ZonesChanged += OnZonesChanged;
            HookEncounters(true);

            if (_player != null)
            {
                _player.Parried += OnParried;
            }

            if (_motor != null)
            {
                _motor.Respawned += OnRespawned;
            }
        }

        private void OnDisable()
        {
            ZoneStreamer.ZonesChanged -= OnZonesChanged;
            HookEncounters(false);

            if (_player != null)
            {
                _player.Parried -= OnParried;
            }

            if (_motor != null)
            {
                _motor.Respawned -= OnRespawned;
            }
        }

        /// <summary>A respawn or a journey cuts straight to the new room, never slides.</summary>
        private void OnRespawned()
        {
            FollowRoom(0f);
            _follow.SnapToTarget();
        }

        private void FollowRoom(float blendTime)
        {
            if (_motor == null || _rooms.Count == 0)
            {
                return;
            }

            var room = _rooms.RoomAt(_motor.transform.position, blendTime > 0f ? _room : -1);
            if (room == _room && room >= 0)
            {
                return;
            }

            _room = room;
            _follow.SetRoomBounds(room >= 0 ? _rooms.Rooms[room].View : (Rect?)null, blendTime);
        }

        /// <summary>
        /// A zone came or went: the rooms are shaped by the ground that is there now, and the
        /// arenas to listen to are the loaded ones.
        /// </summary>
        private void OnZonesChanged()
        {
            HookEncounters(false);
            _encounters = ZoneObjects.All<BossEncounter>(includeInactive: true);
            HookEncounters(true);
            _rooms = BuildRooms();
            _room = -1;
            FollowRoom(_hasGround ? _roomChangeTime : 0f);
            if (!_hasGround)
            {
                // The first zones have just arrived and the player has just been placed on them.
                _hasGround = true;
                _follow.SnapToTarget();
            }
        }

        private void HookEncounters(bool on)
        {
            foreach (var encounter in _encounters)
            {
                if (encounter == null)
                {
                    continue;
                }

                encounter.FightStarted -= OnFightStarted;
                encounter.FightEnded -= OnFightEnded;
                if (on)
                {
                    encounter.FightStarted += OnFightStarted;
                    encounter.FightEnded += OnFightEnded;
                }
            }
        }

        private void OnFightStarted(BossController boss)
        {
            _follow.SetZoom(_bossZoom, _bossZoomTime);
            var graph = _progress != null ? _progress.World : null;
            var arena = graph != null && boss != null
                ? graph.AreaAt(SceneManager.GetActiveScene().name, boss.transform.position)
                : null;
            var room = arena != null ? _rooms.IndexOf(arena.AreaId) : -1;
            _follow.SetFightBounds(room >= 0 ? _rooms.Rooms[room].View
                : arena != null ? arena.WorldBounds : (Rect?)null);
        }

        private void OnFightEnded(BossController boss)
        {
            _follow.SetZoom(1f, _bossZoomTime * 1.5f);
            _follow.SetFightBounds(null);
        }

        /// <summary>The rooms of this scene, shaped by its ground; none without a graph.</summary>
        private CameraRooms BuildRooms()
        {
            if (_motor == null)
            {
                return new CameraRooms();
            }

            var areas = new List<(string, Rect)>();
            var arenas = new HashSet<string>();
            foreach (var area in SceneAreas())
            {
                areas.Add((area.AreaId, area.WorldBounds));
                if (area.IsBossArena)
                {
                    arenas.Add(area.AreaId);
                }
            }

            var view = GetComponent<Camera>();
            var viewWidth = view != null ? view.orthographicSize * 2f * view.aspect : 0f;

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(_motor.GroundMask);
            var hits = new List<Collider2D>();
            return CameraRooms.Build(areas, rect =>
            {
                Physics2D.OverlapArea(rect.min, rect.max, filter, hits);
                var solids = new List<Bounds>(hits.Count);
                foreach (var hit in hits)
                {
                    solids.Add(hit.bounds);
                }

                return solids;
            }, viewWidth, arenas.Contains);
        }

        private IEnumerable<WorldAreaDefinition> SceneAreas()
        {
            var graph = _progress != null ? _progress.World : null;
            if (graph == null)
            {
                yield break;
            }

            var scene = SceneManager.GetActiveScene().name;
            foreach (var area in graph.Areas)
            {
                var zone = area != null ? graph.Zone(area.ZoneId) : null;
                if (area != null && (zone == null || string.IsNullOrEmpty(zone.SceneName)
                    || zone.SceneName == scene))
                {
                    yield return area;
                }
            }
        }

        /// <summary>Every area of this scene put together, or nothing when there is no graph.</summary>
        private Rect? SceneExtent()
        {
            Rect? extent = null;
            foreach (var area in SceneAreas())
            {
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
