using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// An optional trial of arms (item 4 of the big list, the user's choice of 2026-09-28): strike
    /// the gong, the way closes on both sides, and waves of enemies the hero already knows come one
    /// after another. The last one beaten opens the way and, the first time only, lays out the
    /// reward; a death or a rest calls the trial off and it can be started again.
    /// The gong is this object; the waves, walls and reward are data set by the generator.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class TrialArena : MonoBehaviour, IInteractable
    {
        [Serializable]
        public sealed class Wave
        {
            public GameObject[] Enemies = Array.Empty<GameObject>();
            public Vector2[] Positions = Array.Empty<Vector2>();
        }

        [SerializeField] private string _trialId = string.Empty;
        [SerializeField] private string _displayName = "Випробування";
        [SerializeField] private GameProgress _progress;
        [SerializeField] private EnemyRegistry _registry;
        [SerializeField] private Wave[] _waves = Array.Empty<Wave>();
        [Tooltip("Turned on while the trial runs: the walls that close the ground.")]
        [SerializeField] private GameObject[] _barriers = Array.Empty<GameObject>();
        [Tooltip("Turned on once the trial is first won: the prize pickups.")]
        [SerializeField] private GameObject[] _rewards = Array.Empty<GameObject>();
        [SerializeField, Min(0f)] private float _firstWaveDelay = 1.2f;
        [SerializeField, Min(0f)] private float _waveGap = 1.5f;

        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private int _wave = -1;
        private bool _waveSpawned;
        private float _nextWaveAt = -1f;

        /// <summary>The trial, the wave now coming (from 1) and how many there are.</summary>
        public static event Action<TrialArena, int, int> WaveStarted;
        public static event Action<TrialArena> Won;
        public static event Action<TrialArena> CalledOff;

        public string TrialId => _trialId;
        public string DisplayName => _displayName;
        public bool IsRunning => _wave >= 0;
        public int CurrentWave => _wave + 1;
        public int WaveCount => _waves.Length;
        public int AliveCount => _alive.Count;
        public IReadOnlyList<EnemyController> Alive => _alive;
        public bool IsWon => _progress != null && _progress.IsPickupCollected(_trialId);

        public bool CanInteract => !IsRunning && _waves.Length > 0 && isActiveAndEnabled;
        public string Prompt => $"Вдарити в гонг: {_displayName}";

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_registry == null) { _registry = FindAnyObjectByType<EnemyRegistry>(); }
            if (string.IsNullOrEmpty(_trialId))
            {
                Debug.LogError("Trial has no id; its reward could never be saved.", this);
            }
        }

        private void OnEnable()
        {
            if (_registry != null) { _registry.WorldReset += OnWorldReset; }
            if (_progress != null) { _progress.Loaded += ApplySaved; }
        }

        private void OnDisable()
        {
            if (_registry != null) { _registry.WorldReset -= OnWorldReset; }
            if (_progress != null) { _progress.Loaded -= ApplySaved; }
            CallOff(false);
        }

        private void Start() => ApplySaved();

        private void ApplySaved()
        {
            SetActive(_barriers, false);
            // Taken prizes hide themselves (WorldPickup); an untaken one waits where it fell.
            SetActive(_rewards, IsWon);
        }

        public void Interact(PlayerCharacter player)
        {
            if (player == null || !player.IsAlive || !CanInteract)
            {
                return;
            }

            AudioManager.Play(AudioId.TrialGong, transform.position);
            SetActive(_barriers, true);
            _alive.Clear();
            _wave = 0;
            _waveSpawned = false;
            _nextWaveAt = Time.time + _firstWaveDelay;
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (!_waveSpawned)
            {
                if (Time.time >= _nextWaveAt)
                {
                    SpawnWave(_waves[_wave]);
                    _waveSpawned = true;
                    WaveStarted?.Invoke(this, _wave + 1, _waves.Length);
                }

                return;
            }

            _alive.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
            if (_alive.Count > 0)
            {
                return;
            }

            if (_wave + 1 < _waves.Length)
            {
                _wave++;
                _waveSpawned = false;
                _nextWaveAt = Time.time + _waveGap;
                return;
            }

            Win();
        }

        private void SpawnWave(Wave wave)
        {
            for (var i = 0; i < wave.Enemies.Length; i++)
            {
                if (wave.Enemies[i] == null)
                {
                    continue;
                }

                var at = i < wave.Positions.Length ? wave.Positions[i] : (Vector2)transform.position;
                var enemy = Instantiate(wave.Enemies[i], at, Quaternion.identity).GetComponent<EnemyController>();
                if (enemy == null)
                {
                    continue;
                }

                // Its death is the trial's business: the registry must not keep it beaten, or the
                // next attempt's enemy on the same spot would wake up already dead.
                enemy.Died += OnEnemyDied;
                _alive.Add(enemy);
            }
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            _registry?.Forget(enemy);
            _alive.Remove(enemy);
        }

        private void Win()
        {
            var first = !IsWon;
            _wave = -1;
            SetActive(_barriers, false);
            if (first && _progress != null)
            {
                _progress.MarkPickupCollected(_trialId);
                _progress.SaveNow();
                SetActive(_rewards, true);
            }

            AudioManager.Play(AudioId.TrialGong, transform.position, 0.7f);
            Won?.Invoke(this);
        }

        private void OnWorldReset(WorldResetReason reason) => CallOff(true);

        /// <summary>A death or a rest ends the attempt: its enemies go, the way opens.</summary>
        private void CallOff(bool announce)
        {
            if (!IsRunning)
            {
                return;
            }

            foreach (var enemy in _alive)
            {
                if (enemy == null)
                {
                    continue;
                }

                enemy.Died -= OnEnemyDied;
                _registry?.Forget(enemy);
                Destroy(enemy.gameObject);
            }

            _alive.Clear();
            _wave = -1;
            SetActive(_barriers, false);
            if (announce)
            {
                CalledOff?.Invoke(this);
            }
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            foreach (var target in objects)
            {
                if (target != null)
                {
                    target.SetActive(active);
                }
            }
        }
    }
}
