using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The scarecrow's flock (item 5b of the 15 zones, the user's choice of 2026-10-19): when the
    /// scarecrow tears itself off its pole, ravens burst out of its straw and fight beside it. They
    /// are the scarecrow's, not the zone's: they give neither experience nor loot, are not kept as
    /// beaten, and fall apart when the scarecrow dies or the world resets, so the flock cannot be
    /// farmed and never outlives its master.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemySummoner : MonoBehaviour
    {
        [SerializeField] private GameObject _summon;
        [SerializeField, Min(1)] private int _count = 2;
        [Tooltip("Where the first raven appears, relative to the scarecrow; the rest spread sideways.")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.6f);
        [SerializeField, Min(0f)] private float _spread = 1.2f;

        private readonly List<EnemyController> _flock = new List<EnemyController>();
        private EnemyController _enemy;
        private EnemyDormancy _dormancy;
        private EnemyRegistry _registry;

        public IReadOnlyList<EnemyController> Flock => _flock;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
            _dormancy = GetComponent<EnemyDormancy>();
        }

        private void OnEnable()
        {
            if (_dormancy != null) { _dormancy.Woke += Release; }
            _enemy.Died += OnMasterGone;
            _enemy.EnemyReset += OnMasterGone;
        }

        private void OnDisable()
        {
            if (_dormancy != null) { _dormancy.Woke -= Release; }
            _enemy.Died -= OnMasterGone;
            _enemy.EnemyReset -= OnMasterGone;
            Disperse();
        }

        /// <summary>Lets the flock out; once per waking.</summary>
        public void Release()
        {
            if (_summon == null || _flock.Count > 0)
            {
                return;
            }

            if (_registry == null) { _registry = FindAnyObjectByType<EnemyRegistry>(); }
            for (var i = 0; i < _count; i++)
            {
                var side = i % 2 == 0 ? 1f : -1f;
                var at = (Vector2)transform.position + _offset + new Vector2(side * _spread * ((i + 1) / 2), 0f);
                var raven = Instantiate(_summon, at, Quaternion.identity).GetComponent<EnemyController>();
                if (raven == null)
                {
                    continue;
                }

                // Nothing to win from the flock itself: the scarecrow is the reward.
                Destroy(raven.GetComponent<ExperienceReward>());
                Destroy(raven.GetComponent<LootDropper>());
                raven.Died += OnRavenDied;
                _flock.Add(raven);
            }
        }

        private void OnRavenDied(EnemyController raven)
        {
            raven.Died -= OnRavenDied;
            _registry?.Forget(raven);
            _flock.Remove(raven);
        }

        private void OnMasterGone(EnemyController master) => Disperse();

        private void Disperse()
        {
            foreach (var raven in _flock)
            {
                if (raven == null)
                {
                    continue;
                }

                raven.Died -= OnRavenDied;
                _registry?.Forget(raven);
                Destroy(raven.gameObject);
            }

            _flock.Clear();
        }
    }
}
