using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Spikes, as in Hollow Knight: touching them hurts and puts the player back on the last
    /// ground they stood on clear of spikes (<see cref="PlayerRespawner.BeginHazardReturn"/>); a
    /// swing down onto them bounces instead (<see cref="PlayerCombat"/>). They have no hurtbox —
    /// the swing asks <see cref="Overlap"/> — and until painted art exists they draw a row of
    /// placeholder teeth themselves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Spikes : MonoBehaviour
    {
        private static readonly List<Spikes> Active = new List<Spikes>();

        [SerializeField] private Vector2 _size = new Vector2(2.4f, 0.4f);
        [SerializeField, Min(0f)] private float _damage = 20f;
        [SerializeField] private Color _color = new Color32(142, 131, 117, 255);

        private PlayerCharacter _player;
        private PlayerRespawner _respawner;
        private Collider2D _playerCollider;

        public Rect Area
        {
            get
            {
                var centre = (Vector2)transform.position;
                return new Rect(centre - _size * 0.5f, _size);
            }
        }

        /// <summary>True when any spikes overlap this box; what the swing down and the motor ask.</summary>
        public static bool Overlap(Vector2 centre, Vector2 size)
        {
            var box = new Rect(centre - size * 0.5f, size);
            foreach (var spikes in Active)
            {
                if (spikes != null && spikes.Area.Overlaps(box))
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            _player = FindAnyObjectByType<PlayerCharacter>();
            _respawner = FindAnyObjectByType<PlayerRespawner>();
            _playerCollider = _player != null ? _player.GetComponent<CapsuleCollider2D>() : null;
            if (GetComponentInChildren<SpriteRenderer>() == null)
            {
                BuildPlaceholder();
            }
        }

        private void OnEnable() => Active.Add(this);

        private void OnDisable() => Active.Remove(this);

        private void FixedUpdate()
        {
            if (_player == null || _playerCollider == null || !_player.IsAlive)
            {
                return;
            }

            var bounds = _playerCollider.bounds;
            if (!Area.Overlaps(new Rect(bounds.min, bounds.size)))
            {
                return;
            }

            // A pogo off these spikes wins over touching them on the same step.
            var motor = _player.GetComponent<PlayerMotor2D>();
            if (motor != null && motor.IsBouncing)
            {
                return;
            }

            if (_respawner != null && _respawner.IsSequenceRunning)
            {
                return;
            }

            _player.Actor.ApplyDamage(new DamageInfo(_damage, Vector2.up, attacker: gameObject, source: this,
                isUnblockable: true));
            if (_player.IsAlive && motor != null && motor.HasSafeGround)
            {
                // A scene without the respawner (the movement sandbox) still gets the return.
                if (_respawner == null || !_respawner.BeginHazardReturn(motor.LastSafeGround))
                {
                    motor.ResetTo(motor.LastSafeGround);
                }
            }
        }

        /// <summary>A row of grey teeth; replaced by painted art like every other placeholder.</summary>
        private void BuildPlaceholder()
        {
            const int pixelsPerUnit = 50;
            var width = Mathf.Max(4, Mathf.RoundToInt(_size.x * pixelsPerUnit));
            var height = Mathf.Max(4, Mathf.RoundToInt(_size.y * pixelsPerUnit));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var tooth = height;
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var x = 0; x < width; x++)
            {
                var along = (x % tooth) / (float)tooth;
                var top = (1f - Mathf.Abs(along * 2f - 1f)) * height;
                for (var y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, y < top ? _color : clear);
                }
            }

            texture.Apply();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit);
            renderer.sortingOrder = 5;
        }
    }
}
