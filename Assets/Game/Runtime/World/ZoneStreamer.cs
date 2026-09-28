using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenKingdom
{
    /// <summary>
    /// Keeps the zones around the player in memory (item 3 of the big list, the user's choices of
    /// 2026-09-28): the world scene holds the player, the camera and the services, each zone's level
    /// is its own scene, and the zone the player stands in is loaded together with its neighbours,
    /// so a border is crossed on foot with nothing to wait for. Zones further away are unloaded.
    ///
    /// On start the zones are loaded at once and the player is held still until they are in;
    /// fast travel and respawn ask for their destination under the fade with <see cref="Prepare"/>.
    /// Anything that looks for objects of the level listens to <see cref="ZonesChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(10)] // after GameProgress has read the save it starts from
    [DisallowMultipleComponent]
    public sealed class ZoneStreamer : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerCharacter _player;
        [Tooltip("How many zone crossings around the player stay loaded. One keeps the neighbours "
            + "in, so walking over a border never waits for a load.")]
        [SerializeField, Min(0)] private int _neighbourDepth = 1;
        [SerializeField, Min(0.05f)] private float _pollInterval = 0.2f;

        private readonly HashSet<string> _loaded = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _loading = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _unloading = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _pinned = new HashSet<string>(StringComparer.Ordinal);
        private WorldGraph _graph;
        private Rigidbody2D _held;
        private string _worldScene;
        private string _zoneId = string.Empty;
        private int _preparing;
        private int _changedFrame = -1;
        private float _nextPoll;

        public static ZoneStreamer Current { get; private set; }

        /// <summary>
        /// Zone scenes came or went. Raised a frame after the change, once the new objects have
        /// woken and started, so a listener finds them whole.
        /// </summary>
        public static event Action ZonesChanged;

        /// <summary>Raised once, when the first zones are in and the player may move.</summary>
        public event Action Ready;

        public bool IsReady { get; private set; }
        public bool IsBusy => _loading.Count > 0 || _unloading.Count > 0 || _changedFrame >= 0;
        public string CurrentZoneId => _zoneId;
        public IReadOnlyCollection<string> LoadedZones => _loaded;
        public bool IsLoaded(string zoneId) => _loaded.Contains(zoneId);

        /// <summary>How far ahead zones are kept; tests lower it to watch a zone go and come back.</summary>
        public int NeighbourDepth
        {
            get => _neighbourDepth;
            set
            {
                _neighbourDepth = Mathf.Max(0, value);
                Reconcile();
            }
        }

        private void Awake()
        {
            if (Current != null && Current != this)
            {
                Debug.LogError("A second ZoneStreamer was found; the world can only have one.", this);
                enabled = false;
                return;
            }

            Current = this;
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            _graph = _progress != null ? _progress.World : null;
            _worldScene = gameObject.scene.name;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            // A zone already open — the editor with the whole world loaded for a play test.
            foreach (var zone in StreamedZones())
            {
                if (SceneManager.GetSceneByName(zone.ContentScene).isLoaded)
                {
                    _loaded.Add(zone.ZoneId);
                }
            }

            _zoneId = StartZone();
            var needed = ZoneStreamingPlan.Needed(_graph, _zoneId, _neighbourDepth);
            needed.ExceptWith(_loaded);
            if (needed.Count > 0 && _player != null)
            {
                // Nothing to stand on until the ground arrives, a frame from now.
                _held = _player.GetComponent<Rigidbody2D>();
                if (_held != null) { _held.simulated = false; }
            }

            foreach (var zoneId in needed)
            {
                // Asynchronously even now: this Awake runs inside the world scene's own load, and
                // a blocking load from here would wait on the load that is running it. The player
                // is held until the ground is in.
                if (CanLoad(zoneId, out var scene))
                {
                    _loading.Add(zoneId);
                    SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                }
            }

            _changedFrame = Time.frameCount;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (Current == this)
            {
                Current = null;
            }
        }

        private void Update()
        {
            if (_changedFrame >= 0 && _loading.Count == 0 && _unloading.Count == 0
                && Time.frameCount > _changedFrame)
            {
                _changedFrame = -1;
                if (!IsReady)
                {
                    IsReady = true;
                    Ready?.Invoke();
                    if (_held != null) { _held.simulated = true; }
                    _held = null;
                }

                ZonesChanged?.Invoke();
            }

            if (!IsReady || Time.unscaledTime < _nextPoll)
            {
                return;
            }

            _nextPoll = Time.unscaledTime + _pollInterval;
            var here = ZoneAt(_player != null ? (Vector2)_player.transform.position : Vector2.zero);
            if (!string.IsNullOrEmpty(here) && here != _zoneId)
            {
                _zoneId = here;
            }

            Reconcile();
        }

        /// <summary>
        /// Loads a zone and its neighbours and waits until they stand, for a move under a fade
        /// (fast travel, a respawn at a far altar). Nothing is unloaded meanwhile: the player is
        /// still standing in the old place until the caller moves them.
        /// </summary>
        public IEnumerator Prepare(string zoneId)
        {
            if (!ZoneStreamingPlan.IsStreamed(_graph, zoneId))
            {
                yield break;
            }

            _preparing++;
            var wanted = ZoneStreamingPlan.Needed(_graph, zoneId, _neighbourDepth);
            _pinned.UnionWith(wanted);
            Reconcile();
            while (!_loaded.IsSupersetOf(wanted) || _loading.Count > 0 || _changedFrame >= 0)
            {
                yield return null;
            }

            _pinned.ExceptWith(wanted);
            _zoneId = zoneId;
            _preparing--;
        }

        /// <summary>The same, for a point of the world: the zone whose area holds it.</summary>
        public IEnumerator PrepareAt(Vector2 position)
        {
            var zoneId = ZoneAt(position);
            return string.IsNullOrEmpty(zoneId) ? Empty() : Prepare(zoneId);
        }

        private static IEnumerator Empty()
        {
            yield break;
        }

        public string ZoneAt(Vector2 position)
        {
            var area = _graph != null ? _graph.AreaAt(_worldScene, position) : null;
            return area != null ? area.ZoneId : string.Empty;
        }

        private string StartZone()
        {
            if (_graph == null)
            {
                return string.Empty;
            }

            var saved = _progress != null ? _graph.ZoneForCheckpoint(_progress.CheckpointId) : null;
            if (saved != null && ZoneStreamingPlan.IsStreamed(_graph, saved.ZoneId))
            {
                return saved.ZoneId;
            }

            var here = _player != null ? ZoneAt(_player.transform.position) : string.Empty;
            if (ZoneStreamingPlan.IsStreamed(_graph, here))
            {
                return here;
            }

            return _graph.DefaultZone != null ? _graph.DefaultZone.ZoneId : string.Empty;
        }

        private void Reconcile()
        {
            if (_graph == null || !isActiveAndEnabled)
            {
                return;
            }

            var needed = ZoneStreamingPlan.Needed(_graph, _zoneId, _neighbourDepth);
            needed.UnionWith(_pinned);
            foreach (var zoneId in needed)
            {
                if (!_loaded.Contains(zoneId) && !_loading.Contains(zoneId) && CanLoad(zoneId, out var scene))
                {
                    _loading.Add(zoneId);
                    SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                }
            }

            if (_preparing > 0)
            {
                return;
            }

            foreach (var zoneId in new List<string>(_loaded))
            {
                if (needed.Contains(zoneId) || _unloading.Contains(zoneId))
                {
                    continue;
                }

                var scene = SceneManager.GetSceneByName(_graph.Zone(zoneId).ContentScene);
                if (scene.isLoaded)
                {
                    _unloading.Add(zoneId);
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private bool CanLoad(string zoneId, out string scene)
        {
            scene = _graph.Zone(zoneId)?.ContentScene;
            if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene))
            {
                return true;
            }

            Debug.LogError($"Zone '{zoneId}' has no scene '{scene}' in the build; it cannot be loaded.", this);
            return false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var zoneId = ZoneOfScene(scene.name);
            if (zoneId == null)
            {
                return;
            }

            _loading.Remove(zoneId);
            _loaded.Add(zoneId);
            _changedFrame = Time.frameCount;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            var zoneId = ZoneOfScene(scene.name);
            if (zoneId == null)
            {
                return;
            }

            _unloading.Remove(zoneId);
            _loaded.Remove(zoneId);
            _changedFrame = Time.frameCount;
        }

        private string ZoneOfScene(string sceneName)
        {
            foreach (var zone in StreamedZones())
            {
                if (zone.ContentScene == sceneName)
                {
                    return zone.ZoneId;
                }
            }

            return null;
        }

        private IEnumerable<ZoneDefinition> StreamedZones()
        {
            if (_graph == null)
            {
                yield break;
            }

            foreach (var zone in _graph.Zones)
            {
                if (ZoneStreamingPlan.IsStreamed(_graph, zone != null ? zone.ZoneId : null))
                {
                    yield return zone;
                }
            }
        }
    }
}
