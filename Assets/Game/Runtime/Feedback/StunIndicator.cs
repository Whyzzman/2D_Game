using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Stars circling over the head while a creature's balance is broken (the user's choice,
    /// 2026-09-27): the one sign that reads at a glance as "hit it now". It reads the poise meter
    /// that every enemy and boss already has and changes nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StunIndicator : MonoBehaviour
    {
        [SerializeField] private CombatActor _actor;
        [Tooltip("The visual the stars ride on, so they follow the body.")]
        [SerializeField] private Transform _anchor;
        [Tooltip("How far above the anchor the stars circle: just over the head.")]
        [SerializeField] private float _height = 1.2f;
        [SerializeField] private Sprite[] _frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(1f)] private float _framesPerSecond = 14f;
        [Tooltip("Unlit, so the stars read in a dark room.")]
        [SerializeField] private Material _material;
        [SerializeField, Min(0.01f)] private float _scale = 1f;

        private SpriteRenderer _renderer;
        private float _elapsed;

        public bool IsShowing => _renderer != null && _renderer.enabled;

        private void Awake()
        {
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            if (_frames.Length == 0)
            {
                return;
            }

            var stars = new GameObject("StunStars");
            stars.transform.SetParent(_anchor != null ? _anchor : transform, false);
            stars.transform.localPosition = new Vector3(0f, _height, 0f);
            stars.transform.localScale = Vector3.one * _scale;
            _renderer = stars.AddComponent<SpriteRenderer>();
            if (_material != null) { _renderer.sharedMaterial = _material; }
            _renderer.sortingLayerName = "VFXFront";
            _renderer.enabled = false;
        }

        private void LateUpdate()
        {
            if (_renderer == null)
            {
                return;
            }

            var stunned = _actor != null && _actor.IsAlive && _actor.Stagger != null && _actor.Stagger.IsStunned;
            if (!stunned)
            {
                _renderer.enabled = false;
                _elapsed = 0f;
                return;
            }

            _elapsed += Time.deltaTime;
            var frame = (int)(_elapsed * _framesPerSecond) % _frames.Length;
            _renderer.sprite = _frames[frame];
            _renderer.enabled = true;
        }

        private void OnDisable()
        {
            if (_renderer != null) { _renderer.enabled = false; }
        }
    }
}
