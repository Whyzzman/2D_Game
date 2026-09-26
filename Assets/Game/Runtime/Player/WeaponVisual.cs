using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The weapon in the player's hand, as its own object on a mount point. Nothing about the
    /// player's own sprite knows which weapon is equipped, so real art can replace this without
    /// touching combat or equipment. With no sprite assigned it simply draws nothing and the game
    /// plays on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponVisual : MonoBehaviour
    {
        [Tooltip("The socket the weapon hangs from; flips with the player.")]
        [SerializeField] private Transform _mount;
        [SerializeField] private SpriteRenderer _renderer;
        private Transform _owner;
        private IFacing _facing;
        private WeaponDefinition _weapon;

        public WeaponDefinition Weapon => _weapon;
        public Transform Mount => _mount;

        private void Awake()
        {
            if (_mount == null) { _mount = transform; }
            if (_renderer == null) { _renderer = GetComponentInChildren<SpriteRenderer>(true); }
            _owner = transform.root;
            _facing = _owner.GetComponentInChildren<IFacing>();
        }

        /// <summary>Hides the old weapon and shows the new one. Null means an empty hand.</summary>
        public void Show(WeaponDefinition weapon)
        {
            _weapon = weapon;
            if (_renderer == null)
            {
                return;
            }

            // No sprite, nothing drawn. The character's own frames already hold the weapon; a
            // stretched white square next to them was the most visible placeholder in the game.
            if (weapon == null || weapon.VisualSprite == null)
            {
                _renderer.enabled = false;
                return;
            }

            _renderer.enabled = true;
            _renderer.color = weapon.VisualColor;
            _renderer.sprite = weapon.VisualSprite;

            var size = weapon.VisualSize;
            _renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
            ApplyOffset();
        }

        private void LateUpdate()
        {
            ApplyOffset();
        }

        /// <summary>
        /// The visual mirrors with the player, exactly like the attack hitbox does, so a swing to
        /// the left never draws a sword pointing right.
        /// </summary>
        private void ApplyOffset()
        {
            if (_weapon == null || _mount == null)
            {
                return;
            }

            var facing = _facing?.FacingDirection ?? 1;
            var offset = _weapon.VisualOffset;
            _mount.localPosition = new Vector3(offset.x * facing, offset.y, 0f);
            var scale = _mount.localScale;
            _mount.localScale = new Vector3(Mathf.Abs(scale.x) * facing, scale.y, scale.z);
        }
    }
}
