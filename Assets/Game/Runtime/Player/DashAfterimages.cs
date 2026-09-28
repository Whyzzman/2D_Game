using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The dash leaves cold silver silhouettes of the hero behind it (hero animation 2.0, the
    /// user's choice of 2026-09-28): the shape of the frame shown, in the colour of the hero's lit
    /// edge, fading within a fifth of a second. Deliberately not a black smoke trail. Reads the
    /// animation state only; nothing here changes the dash.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DashAfterimages : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationController _animation;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Draws a sprite as a flat colour (the SpriteSilhouette shader).")]
        [SerializeField] private Material _material;
        [SerializeField] private Color _tint = new Color(0.77f, 0.85f, 0.96f, 0.5f);
        [SerializeField, Min(0.01f)] private float _interval = 0.035f;
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.22f;
        [SerializeField, Range(1, 12)] private int _count = 6;

        private SpriteRenderer[] _ghosts;
        private float[] _born;
        private int _next;
        private float _nextAt;
        private bool _wasDashing;

        /// <summary>How many silhouettes are showing now.</summary>
        public int ActiveCount
        {
            get
            {
                var count = 0;
                if (_ghosts == null)
                {
                    return 0;
                }

                foreach (var ghost in _ghosts)
                {
                    count += ghost != null && ghost.enabled ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>The silhouettes themselves, for tests and the debug readout.</summary>
        public SpriteRenderer[] Ghosts => _ghosts;

        private void Awake()
        {
            if (_animation == null) { _animation = GetComponent<PlayerAnimationController>(); }
            if (_body == null && _animation != null) { _body = _animation.Body; }
            _ghosts = new SpriteRenderer[_count];
            _born = new float[_count];
            for (var i = 0; i < _count; i++)
            {
                // Loose in the scene rather than under the hero, so they stay where they were left.
                var ghost = new GameObject("HeroAfterimage");
                var renderer = ghost.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = _material;
                renderer.enabled = false;
                _ghosts[i] = renderer;
            }
        }

        private void OnDestroy()
        {
            if (_ghosts == null)
            {
                return;
            }

            foreach (var ghost in _ghosts)
            {
                if (ghost != null)
                {
                    Destroy(ghost.gameObject);
                }
            }
        }

        private void OnDisable()
        {
            if (_ghosts == null)
            {
                return;
            }

            foreach (var ghost in _ghosts)
            {
                if (ghost != null)
                {
                    ghost.enabled = false;
                }
            }
        }

        private static bool IsDash(PlayerAnimationState state) =>
            state == PlayerAnimationState.Dash || state == PlayerAnimationState.AirDash
            || state == PlayerAnimationState.DashStrike;

        private void LateUpdate()
        {
            var dashing = _animation != null && _body != null && _body.enabled && IsDash(_animation.State);
            if (dashing && (!_wasDashing || Time.time >= _nextAt))
            {
                Leave();
                _nextAt = Time.time + _interval;
            }

            _wasDashing = dashing;
            Fade();
        }

        private void Leave()
        {
            var ghost = _ghosts[_next];
            _born[_next] = Time.time;
            _next = (_next + 1) % _ghosts.Length;
            var from = _body.transform;
            ghost.sprite = _body.sprite;
            ghost.transform.SetPositionAndRotation(from.position, from.rotation);
            ghost.transform.localScale = from.lossyScale;
            ghost.sortingLayerID = _body.sortingLayerID;
            // Behind the figure and its cloak: the trail is where the hero was, not over them.
            ghost.sortingOrder = _body.sortingOrder - 3;
            ghost.color = _tint;
            ghost.enabled = ghost.sprite != null;
        }

        private void Fade()
        {
            for (var i = 0; i < _ghosts.Length; i++)
            {
                var ghost = _ghosts[i];
                if (ghost == null || !ghost.enabled)
                {
                    continue;
                }

                var left = 1f - (Time.time - _born[i]) / _fadeTime;
                if (left <= 0f)
                {
                    ghost.enabled = false;
                    continue;
                }

                var color = _tint;
                color.a *= left * left;
                ghost.color = color;
            }
        }
    }
}
