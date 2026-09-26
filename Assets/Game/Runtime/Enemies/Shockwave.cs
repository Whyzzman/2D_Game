using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The ground wave left by the boss's jump slam: a short, wide hitbox with a matching visual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Shockwave : MonoBehaviour
    {
        [SerializeField] private Hitbox _hitbox;
        [SerializeField] private SpriteRenderer _visual;
        [SerializeField, Min(0f)] private float _damage = 14f;
        [SerializeField, Min(0f)] private float _staggerDamage = 18f;
        [SerializeField, Min(0f)] private float _knockback = 7f;
        [SerializeField, Min(0.01f)] private float _duration = 0.35f;
        [SerializeField] private Vector2 _size = new Vector2(6f, 1.2f);
        private float _remaining;

        private void Awake()
        {
            if (_hitbox == null)
            {
                Debug.LogError("Shockwave requires a hitbox.", this);
                enabled = false;
                return;
            }

            SetVisible(false);
        }

        public void Play(GameObject attacker, Vector2 origin, int facing)
        {
            transform.position = origin;
            _remaining = _duration;
            var damage = new DamageInfo(_damage, new Vector2(facing, 0f), DamageType.Physical,
                attacker, this, origin, _knockback, _staggerDamage, 0.05f);
            _hitbox.Begin(damage, Vector2.zero, _size, facing);
            SetVisible(true);
        }

        private void Update()
        {
            if (_remaining <= 0f)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_visual != null)
            {
                var progress = 1f - Mathf.Clamp01(_remaining / _duration);
                _visual.transform.localScale = new Vector3(
                    Mathf.Lerp(0.4f, 1f, progress) * _size.x, _size.y, 1f);
                var color = _visual.color;
                color.a = 1f - progress;
                _visual.color = color;
            }

            if (_remaining <= 0f)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _remaining = 0f;
            _hitbox.End();
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (_visual != null)
            {
                _visual.enabled = isVisible;
            }
        }
    }
}
