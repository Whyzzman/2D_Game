using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Makes an enemy leave loot when it really dies. One death rolls the table exactly once, and
    /// putting the enemy back arms it again — so a rest loop pays, but a reset never does.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class LootDropper : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private DropTable _dropTable;
        [SerializeField] private LootService _loot;
        [SerializeField] private Vector2 _dropOffset = new Vector2(0f, -0.4f);
        private bool _hasDropped;

        public bool HasDropped => _hasDropped;
        public DropTable DropTable => _dropTable;

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
            if (_loot == null)
            {
                _loot = FindAnyObjectByType<LootService>();
            }
        }

        private void OnDied(EnemyController enemy)
        {
            if (_hasDropped || _dropTable == null || _loot == null)
            {
                return;
            }

            // Guarded by a flag: a second death event, or a delayed one, drops nothing.
            _hasDropped = true;
            _loot.SpawnDrops(_dropTable, (Vector2)transform.position + _dropOffset);
        }

        private void OnReset(EnemyController enemy)
        {
            _hasDropped = false;
        }
    }
}
