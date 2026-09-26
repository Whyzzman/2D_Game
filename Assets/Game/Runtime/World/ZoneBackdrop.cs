using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Shows a zone's background layers only while the player is in that zone.
    ///
    /// Parallax layers wrap around the camera forever — that is what stops a dash outrunning the
    /// backdrop — so without this the forest's trees were drawn over the Tomb's nave everywhere
    /// in the level. The layers cross-fade as the player walks through the tunnel between the two,
    /// rather than switching.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneBackdrop : MonoBehaviour
    {
        [SerializeField] private string _zoneId = string.Empty;
        [SerializeField, Min(0.05f)] private float _fadeTime = 1.4f;

        private SpriteRenderer[] _renderers = System.Array.Empty<SpriteRenderer>();
        private float[] _alphas = System.Array.Empty<float>();
        private float _visibility;
        private float _target;
        private WorldState _world;

        public string ZoneId => _zoneId;
        public float Visibility => _visibility;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _alphas = new float[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                _alphas[i] = _renderers[i].color.a;
            }
        }

        private void OnEnable()
        {
            _world = WorldState.Current != null ? WorldState.Current : FindAnyObjectByType<WorldState>();
            if (_world != null)
            {
                _world.ZoneEntered += OnZoneEntered;
            }
        }

        private void OnDisable()
        {
            if (_world != null)
            {
                _world.ZoneEntered -= OnZoneEntered;
            }
        }

        private void Start()
        {
            // No world service (a test scene): stay visible, as before.
            var current = _world != null ? _world.CurrentZoneId : string.Empty;
            _target = string.IsNullOrEmpty(current) || current == _zoneId ? 1f : 0f;
            _visibility = _target;
            Apply();
        }

        private void OnZoneEntered(ZoneDefinition zone)
        {
            _target = zone != null && zone.ZoneId == _zoneId ? 1f : 0f;
        }

        private void Update()
        {
            if (Mathf.Approximately(_visibility, _target))
            {
                return;
            }

            _visibility = Mathf.MoveTowards(_visibility, _target, Time.unscaledDeltaTime / _fadeTime);
            Apply();
        }

        private void Apply()
        {
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var c = renderer.color;
                renderer.color = new Color(c.r, c.g, c.b, _alphas[i] * _visibility);
                renderer.enabled = _visibility > 0.001f;
            }
        }
    }
}
