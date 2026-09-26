using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Pays out an enemy's experience when it really dies. Built like
    /// <see cref="LootDropper"/> and for the same reason: one real death pays once, putting the
    /// enemy back arms it again, so a rest loop is farmable but a reset never pays.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class ExperienceReward : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private PlayerProgressionService _progression;
        [Tooltip("Overrides the enemy definition when above zero; for one-off encounters.")]
        [SerializeField, Min(0)] private int _overrideReward = 0;
        private bool _hasGranted;

        public bool HasGranted => _hasGranted;

        public int Reward => _overrideReward > 0
            ? _overrideReward
            : _enemy != null && _enemy.Definition != null ? _enemy.Definition.ExperienceReward : 0;

        private void Awake()
        {
            if (_enemy == null)
            {
                _enemy = GetComponent<EnemyController>();
            }
        }

        private void OnEnable()
        {
            _enemy.Died += OnDied;
            _enemy.EnemyReset += OnReset;
        }

        private void OnDisable()
        {
            _enemy.Died -= OnDied;
            _enemy.EnemyReset -= OnReset;
        }

        private void Start()
        {
            if (_progression == null)
            {
                _progression = FindAnyObjectByType<PlayerProgressionService>();
            }
        }

        private void OnDied(EnemyController enemy)
        {
            if (_hasGranted || _progression == null || Reward <= 0)
            {
                return;
            }

            // Guarded by a flag: a second death event, or a delayed one, pays nothing.
            _hasGranted = true;
            _progression.AddExperience(Reward,
                _enemy.Definition != null ? _enemy.Definition.DisplayName : name);
        }

        private void OnReset(EnemyController enemy)
        {
            _hasGranted = false;
        }
    }
}
