using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Afterimages behind a dash: copies of the player's current frame, left behind and fading.
    ///
    /// An ordinary dash leaves a few pale, cold copies — speed made visible. A Shadow dash is the
    /// signature move and looks it: the body itself thins to a silhouette, violet copies trail
    /// behind with a dark core, and the figure re-forms at the far end.
    ///
    /// Visual only. It reads the player's events and sprite and never touches movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AfterimageTrail : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private ShadowAbilities _shadow;
        [SerializeField] private SpriteRenderer _body;
        [Tooltip("Unlit, so the copies glow against a dark room.")]
        [SerializeField] private Material _material;
        [SerializeField] private Color _dashColor = new Color(0.62f, 0.74f, 0.95f, 0.42f);
        [SerializeField] private Color _shadowColor = new Color(0.6f, 0.3f, 1f, 0.7f);
        [SerializeField] private Color _shadowCoreColor = new Color(0.1f, 0.06f, 0.15f, 0.8f);
        [SerializeField, Min(0.01f)] private float _interval = 0.03f;
        [SerializeField, Min(0.05f)] private float _fadeTime = 0.24f;
        [SerializeField, Min(2)] private int _poolSize = 16;

        private readonly List<SpriteRenderer> _ghosts = new List<SpriteRenderer>();
        private readonly List<float> _born = new List<float>();
        private readonly List<Color> _tints = new List<Color>();
        private int _next;
        private float _emitUntil;
        private float _nextEmit;
        private bool _isShadow;
        private float _dissolveUntil;
        private Transform _root;

        private void Awake()
        {
            if (_player == null) { _player = GetComponentInParent<PlayerCharacter>(); }
            if (_shadow == null && _player != null) { _shadow = _player.GetComponent<ShadowAbilities>(); }
            _root = new GameObject("Afterimages").transform;
            for (var i = 0; i < _poolSize; i++)
            {
                var ghost = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
                ghost.transform.SetParent(_root, false);
                if (_material != null) { ghost.sharedMaterial = _material; }
                // On the characters' layer from the start, not only once shown: nothing that
                // belongs to a fight may sit on Default, even while idle in the pool.
                ghost.sortingLayerName = "Characters";
                ghost.enabled = false;
                _ghosts.Add(ghost);
                _born.Add(-10f);
                _tints.Add(Color.clear);
            }
        }

        private void OnEnable()
        {
            if (_player != null) { _player.DashStarted += OnDash; }
            if (_shadow != null) { _shadow.Started += OnShadow; }
        }

        private void OnDisable()
        {
            if (_player != null) { _player.DashStarted -= OnDash; }
            if (_shadow != null) { _shadow.Started -= OnShadow; }
            foreach (var ghost in _ghosts)
            {
                if (ghost != null) { ghost.enabled = false; }
            }

            RestoreBody();
        }

        private void OnDestroy()
        {
            if (_root != null) { Destroy(_root.gameObject); }
        }

        private void OnDash()
        {
            _isShadow = false;
            _emitUntil = Time.time + 0.2f;
            _nextEmit = 0f;
        }

        private void OnShadow(ShadowAction action)
        {
            _isShadow = true;
            _emitUntil = Time.time + (action == ShadowAction.Dash ? 0.28f : 0.18f);
            _dissolveUntil = _emitUntil;
            _nextEmit = 0f;
        }

        private void LateUpdate()
        {
            if (_body == null)
            {
                return;
            }

            if (Time.time < _emitUntil && Time.time >= _nextEmit)
            {
                _nextEmit = Time.time + _interval;
                Emit(_isShadow ? _shadowColor : _dashColor);
                if (_isShadow)
                {
                    // A dark core inside the violet: Shadow is black light, not purple paint.
                    Emit(_shadowCoreColor, 0.86f);
                }
            }

            for (var i = 0; i < _ghosts.Count; i++)
            {
                var ghost = _ghosts[i];
                if (!ghost.enabled)
                {
                    continue;
                }

                var age = (Time.time - _born[i]) / _fadeTime;
                if (age >= 1f)
                {
                    ghost.enabled = false;
                    continue;
                }

                var tint = _tints[i];
                ghost.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - age) * (1f - age));
            }

            // The body thins while the Shadow carries it, then re-forms.
            if (Time.time < _dissolveUntil)
            {
                var c = _body.color;
                _body.color = new Color(c.r, c.g, c.b, 0.28f);
            }
            else
            {
                RestoreBody();
            }
        }

        private void Emit(Color tint, float scale = 1f)
        {
            var ghost = _ghosts[_next];
            _born[_next] = Time.time;
            _tints[_next] = tint;
            _next = (_next + 1) % _ghosts.Count;

            ghost.sprite = _body.sprite;
            ghost.flipX = _body.flipX;
            ghost.transform.position = _body.transform.position;
            ghost.transform.rotation = _body.transform.rotation;
            ghost.transform.localScale = Vector3.Scale(_body.transform.lossyScale, new Vector3(scale, scale, 1f));
            ghost.sortingLayerID = _body.sortingLayerID;
            ghost.sortingOrder = _body.sortingOrder - 1;
            ghost.color = tint;
            ghost.enabled = true;
        }

        private void RestoreBody()
        {
            if (_body != null && _body.color.a < 0.99f)
            {
                var c = _body.color;
                _body.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }
}
