using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The price of dying (item 8 of the 2026-09-27 list, the user's choices of 2026-09-28): every
    /// coin carried is left behind on the last solid ground the hero stood on — never over a pit or
    /// on spikes, so it can always be walked back to — and waits there as a
    /// <see cref="LostCurrencyMark"/>. Experience and items are not touched.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LostCurrencyDrop : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private GameProgress _progress;
        [Tooltip("The trace in the world; one is made per scene and moved to each new death.")]
        [SerializeField] private LostCurrencyMark _markPrefab;

        private LostCurrencyMark _mark;

        public LostCurrencyMark Mark => _mark;

        private void Awake()
        {
            if (_player == null) { _player = GetComponent<PlayerCharacter>(); }
            if (_motor == null) { _motor = GetComponent<PlayerMotor2D>(); }
            if (_markPrefab != null)
            {
                // Not a child of the player: it stays where the death was.
                _mark = Instantiate(_markPrefab);
                _mark.name = _markPrefab.name;
            }
        }

        private void OnEnable()
        {
            if (_player != null) { _player.Died += OnDied; }
        }

        private void OnDisable()
        {
            if (_player != null) { _player.Died -= OnDied; }
        }

        private void OnDestroy()
        {
            if (_mark != null) { Destroy(_mark.gameObject); }
        }

        private void OnDied()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_progress == null)
            {
                return;
            }

            _progress.DropAllCurrency(Where());
            // Saved at once: quitting on the death screen must not bring the purse back.
            _progress.Save();
        }

        /// <summary>The last ground stood on clear of spikes; the body itself if there was none.</summary>
        private Vector2 Where()
        {
            return _motor != null && _motor.HasSafeGround
                ? _motor.LastSafeGround
                : (Vector2)transform.position;
        }
    }
}
