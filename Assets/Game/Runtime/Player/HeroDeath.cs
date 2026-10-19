using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The hero's death as a moment rather than a cut (GAME_FEEL_IDEAS item 22): the world slows for
    /// half a second, the silhouette darkens and comes apart into shadows that drift upward, the score
    /// steps back and a low toll sounds. Under the respawn fade the body is restored, and on the far
    /// side the altar's voice answers while the hero gathers out of the dark again.
    ///
    /// Presentation only. The death delay, the fade and the reset stay with
    /// <see cref="PlayerCharacter"/> and <see cref="PlayerRespawner"/>; this reads their events and
    /// writes nothing but colours, its own wisps and the base time scale it borrowed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroDeath : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Draws a sprite as a flat colour (the SpriteSilhouette shader), like the dash copies.")]
        [SerializeField] private Material _material;

        [Header("Slow")]
        [Tooltip("Time scale while the hero falls. Scaled away by the hit-stop accessibility setting.")]
        [SerializeField, Range(0.05f, 1f)] private float _slowScale = 0.35f;
        [Tooltip("Real seconds the world stays slow.")]
        [SerializeField, Min(0f)] private float _slowDuration = 0.45f;

        [Header("Dissolve")]
        [SerializeField, Min(0f)] private float _darkenTime = 0.25f;
        [SerializeField, Min(0.05f)] private float _dissolveTime = 0.8f;
        [SerializeField, Range(4, 48)] private int _wispCount = 28;
        [SerializeField] private Color _wispColor = new Color(0.08f, 0.06f, 0.12f, 0.85f);
        [Tooltip("The faint cold edge the shadows keep, so they read against a dark room.")]
        [SerializeField] private Color _wispEdge = new Color(0.55f, 0.6f, 0.78f, 0.35f);
        [SerializeField, Min(0.1f)] private float _wispLife = 1.1f;
        [SerializeField, Min(0f)] private float _wispRise = 1.6f;

        [Header("Return")]
        [SerializeField, Min(0.05f)] private float _gatherTime = 0.5f;

        private const float WispSize = 0.22f;
        private static Sprite _wispSprite;

        private SpriteRenderer[] _wisps;
        private Vector2[] _velocity;
        private float[] _born;
        private Color _bodyColor = Color.white;
        private float _diedAt = -10f;
        private float _gatherFrom = -10f;
        private float _slowUntil;
        private bool _slowing;
        private bool _dissolving;

        /// <summary>True from the fall until the body has gone; for tests and the debug readout.</summary>
        public bool IsDissolving => _dissolving;

        /// <summary>How many shadows are drifting now.</summary>
        public int ActiveWisps
        {
            get
            {
                var count = 0;
                if (_wisps == null) { return 0; }
                foreach (var wisp in _wisps) { count += wisp != null && wisp.enabled ? 1 : 0; }
                return count;
            }
        }

        private void Awake()
        {
            if (_player == null) { _player = GetComponent<PlayerCharacter>(); }
            if (_body == null)
            {
                var animation = GetComponentInChildren<PlayerAnimationController>(true);
                _body = animation != null ? animation.Body : null;
            }

            if (_body != null) { _bodyColor = _body.color; }
            _wisps = new SpriteRenderer[_wispCount];
            _velocity = new Vector2[_wispCount];
            _born = new float[_wispCount];
            var sprite = WispSprite();
            for (var i = 0; i < _wispCount; i++)
            {
                // Loose in the scene, so the shadows stay where the hero fell.
                var wisp = new GameObject("HeroDeathWisp").AddComponent<SpriteRenderer>();
                wisp.sprite = sprite;
                if (_material != null) { wisp.sharedMaterial = _material; }
                wisp.sortingLayerName = _body != null ? _body.sortingLayerName : "Characters";
                wisp.sortingOrder = _body != null ? _body.sortingOrder + 2 : 10;
                wisp.enabled = false;
                _wisps[i] = wisp;
            }
        }

        private void OnEnable()
        {
            if (_player == null) { return; }
            _player.Died += OnDied;
            _player.ActionChanged += OnActionChanged;
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.Died -= OnDied;
                _player.ActionChanged -= OnActionChanged;
            }

            EndSlow();
            RestoreBody();
        }

        private void OnDestroy()
        {
            if (_wisps == null) { return; }
            foreach (var wisp in _wisps)
            {
                if (wisp != null) { Destroy(wisp.gameObject); }
            }
        }

        private void OnDied()
        {
            _diedAt = Time.unscaledTime;
            _gatherFrom = -10f;
            _dissolving = true;
            var position = (Vector2)transform.position;
            AudioManager.Play(AudioId.HeroFall, position);

            var slow = _slowDuration * FeedbackSettings.HitStopMultiplier;
            if (slow > 0.01f && GameTimeScale.BaseScale >= 0.999f)
            {
                GameTimeScale.SetBaseScale(_slowScale);
                _slowing = true;
                _slowUntil = Time.unscaledTime + slow;
            }
        }

        /// <summary>The hero leaves the dead state only by being put back at an altar.</summary>
        private void OnActionChanged(PlayerAction from, PlayerAction to)
        {
            if (from != PlayerAction.Dead || to == PlayerAction.Dead)
            {
                return;
            }

            _dissolving = false;
            _gatherFrom = Time.unscaledTime;
            EndSlow();
            // Opaque again at once, still dark: this runs inside the respawn, before the spawn
            // protection starts, and the protection blink remembers the alpha it finds then.
            if (_body != null) { _body.color = Dark(_bodyColor.a); }
            AudioManager.Play(AudioId.AltarCall, transform.position);
            // The shadows come back to the body instead of the body simply reappearing.
            Burst(gathering: true);
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            if (_slowing && now >= _slowUntil) { EndSlow(); }

            if (_dissolving) { Dissolve(now - _diedAt); }
            else if (_gatherFrom > 0f) { Gather(now - _gatherFrom); }

            AnimateWisps(now);
        }

        private void Dissolve(float age)
        {
            if (_body == null) { return; }

            // Darken first, then thin away while the shadows leave; the score stays stepped back.
            MusicDirector.Instance?.Duck();
            var dark = _darkenTime > 0f ? Mathf.Clamp01(age / _darkenTime) : 1f;
            var gone = Mathf.Clamp01((age - _darkenTime) / _dissolveTime);
            var color = Color.Lerp(_bodyColor, Dark(_bodyColor.a), dark);
            color.a = _bodyColor.a * (1f - gone);
            _body.color = color;

            if (age >= _darkenTime && _wisps.Length > 0 && !_wisps[0].enabled && _born[0] < _diedAt)
            {
                Burst(gathering: false);
            }
        }

        private void Gather(float age)
        {
            if (_body == null) { return; }

            // Colour only: the alpha belongs to the protection blink while it runs.
            var t = Mathf.Clamp01(age / _gatherTime);
            var eased = t * t * (3f - 2f * t);
            var color = Color.Lerp(Dark(1f), _bodyColor, eased);
            color.a = _body.color.a;
            _body.color = color;
            if (t >= 1f)
            {
                _gatherFrom = -10f;
                color = _bodyColor;
                color.a = _body.color.a;
                _body.color = color;
                foreach (var wisp in _wisps) { wisp.enabled = false; }
            }
        }

        /// <summary>
        /// Every shadow at once: out of the body's outline and upward, or, gathering, from a ring
        /// around the hero back into it. Each with its own drift, so no two deaths look the same.
        /// </summary>
        private void Burst(bool gathering)
        {
            if (_body == null) { return; }

            var bounds = _body.bounds;
            var centre = (Vector2)bounds.center;
            var now = Time.unscaledTime;
            for (var i = 0; i < _wisps.Length; i++)
            {
                var inside = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x) * 0.8f + centre.x * 0.2f,
                    Random.Range(bounds.min.y, bounds.max.y));
                if (gathering)
                {
                    var angle = Random.Range(0f, Mathf.PI * 2f);
                    var reach = Random.Range(1.2f, 2.2f);
                    var start = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f) * reach;
                    _wisps[i].transform.position = start;
                    _velocity[i] = (inside - start) / _gatherTime;
                }
                else
                {
                    _wisps[i].transform.position = inside;
                    var outward = (inside - centre) * 0.9f;
                    _velocity[i] = new Vector2(outward.x + Random.Range(-0.3f, 0.3f),
                        _wispRise * Random.Range(0.5f, 1.2f));
                }

                _born[i] = now + (gathering ? 0f : Random.Range(0f, _dissolveTime * 0.6f));
                // Leaving shadows wait for their own moment; gathering ones show at once.
                _wisps[i].enabled = gathering;
                _wisps[i].transform.localScale = Vector3.one * WispSize * Random.Range(0.7f, 1.4f);
            }
        }

        private void AnimateWisps(float now)
        {
            var gathering = _gatherFrom > 0f;
            var life = gathering ? _gatherTime : _wispLife;
            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _wisps.Length; i++)
            {
                var wisp = _wisps[i];
                var age = now - _born[i];
                var waiting = !gathering && _dissolving && age < 0f;
                if (waiting) { continue; }
                if (age < 0f || age > life || (!gathering && !_dissolving && !wisp.enabled))
                {
                    wisp.enabled = false;
                    continue;
                }

                wisp.enabled = true;
                var t = age / life;
                // A slow curl sideways as they rise, like smoke that has somewhere to be.
                var curl = Mathf.Sin((age * 3.1f) + i) * 0.35f;
                wisp.transform.position += (Vector3)((_velocity[i] + new Vector2(curl, 0f)) * dt);
                var fade = gathering ? Mathf.Sin(t * Mathf.PI) : 1f - t;
                var color = Color.Lerp(_wispEdge, _wispColor, gathering ? 1f - t : t);
                color.a *= fade;
                wisp.color = color;
                wisp.transform.localScale *= gathering ? 1f : 1f + dt * 0.6f;
            }
        }

        private void EndSlow()
        {
            if (!_slowing) { return; }

            _slowing = false;
            // Only hand back what was borrowed: if something else changed the scale since, it is theirs.
            if (Mathf.Approximately(GameTimeScale.BaseScale, _slowScale))
            {
                GameTimeScale.SetBaseScale(1f);
            }
        }

        private static Color Dark(float alpha) => new Color(0.05f, 0.04f, 0.07f, alpha);

        private void RestoreBody()
        {
            if (_body != null) { _body.color = _bodyColor; }
        }

        /// <summary>A soft round smudge, made once: the shadows need no art of their own.</summary>
        private static Sprite WispSprite()
        {
            if (_wispSprite != null) { return _wispSprite; }

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HeroDeathWisp", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Taller than wide, the top softer: a flame-shaped wisp rather than a dot.
                    var dx = (x - size * 0.5f + 0.5f) / (size * 0.38f);
                    var dy = (y - size * 0.42f) / (size * 0.5f);
                    var d = Mathf.Sqrt(dx * dx + dy * dy * (dy > 0f ? 0.7f : 1.4f));
                    var a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _wispSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _wispSprite;
        }
    }
}
