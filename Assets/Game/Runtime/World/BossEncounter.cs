using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Runs the boss arena as an encounter rather than a room: it wakes the boss, seals the way
    /// out, reports the fight to the HUD, puts everything back when the player dies, and opens the
    /// arena for good once the boss is beaten. A boss recorded as defeated never wakes again.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossEncounter : MonoBehaviour
    {
        [SerializeField] private BossController _boss;
        [SerializeField] private GameObject _barrier;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerRespawner _respawner;
        [Tooltip("Item granted once when the boss falls; also the marker that the reward was paid.")]
        [SerializeField] private string _rewardId = "guardian_core";
        [SerializeField] private string _rewardName = "Ядро Стража";
        [Tooltip("Coin reward handed over with the item.")]
        [SerializeField, Min(0)] private int _rewardCurrency = 500;
        [Tooltip("Experience for the kill; paid once, with the rest of the reward.")]
        [SerializeField, Min(0)] private int _rewardExperience = 600;
        [Tooltip("Traversal ability unlocked with the reward. Empty for a boss that grants none.")]
        [SerializeField] private string _rewardAbilityId = string.Empty;
        [SerializeField] private PlayerProgressionService _progression;
        [SerializeField, Min(0f)] private float _unlockDelay = 1.2f;
        private float _unlockAt = -1f;

        /// <summary>The HUD listens to these instead of polling for an active boss.</summary>
        public event Action<BossController> FightStarted;
        public event Action<BossController> FightEnded;
        public event Action<BossController> BossDefeated;
        public event Action<string, string> RewardGranted;

        /// <summary>Raised when a boss reward also hands over a movement ability.</summary>
        public event Action<string> AbilityUnlocked;

        public bool IsFightActive { get; private set; }

        /// <summary>
        /// The boss is dead but the room has not opened yet. Anything that moves the player — fast
        /// travel above all — waits for this beat to finish, so a reward is never left behind.
        /// </summary>
        public bool IsSettling => _unlockAt > 0f;
        public BossController Boss => _boss;

        /// <summary>The barrier that seals the arena; exposed so it can be inspected while closed.</summary>
        public GameObject Barrier => _barrier;
        public bool IsArenaSealed => _barrier != null && _barrier.activeSelf;
        public string RewardId => _rewardId;

        /// <summary>True once this boss has been beaten, in this session or an earlier one.</summary>
        public bool IsBossDefeated => _progress != null && _progress.IsBossDefeated(BossId);

        public string BossId => _boss != null ? _boss.BossId : string.Empty;

        private void Awake()
        {
            if (_boss == null)
            {
                Debug.LogError("BossEncounter requires a boss.", this);
                enabled = false;
                return;
            }

            SetBarrier(false);
        }

        private void OnEnable()
        {
            _boss.Defeated += OnBossDefeated;
            if (_respawner != null)
            {
                _respawner.PlayerDied += OnPlayerDied;
            }
        }

        private void OnDisable()
        {
            _boss.Defeated -= OnBossDefeated;
            if (_respawner != null)
            {
                _respawner.PlayerDied -= OnPlayerDied;
            }
        }

        private void Start()
        {
            if (!IsBossDefeated)
            {
                return;
            }

            // Already beaten: the arena is an ordinary room and the boss is gone for good.
            _boss.gameObject.SetActive(false);
            SetBarrier(false);
        }

        private void Update()
        {
            if (_unlockAt > 0f && Time.time >= _unlockAt)
            {
                _unlockAt = -1f;
                SetBarrier(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsFightActive || IsBossDefeated || _boss == null || !_boss.gameObject.activeSelf)
            {
                return;
            }

            var player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null || !player.IsAlive)
            {
                return;
            }

            IsFightActive = true;
            SetBarrier(true);
            _boss.BeginFight(player);
            FightStarted?.Invoke(_boss);
        }

        /// <summary>The player fell before winning: the encounter goes back to how it was found.</summary>
        private void OnPlayerDied()
        {
            if (!IsFightActive)
            {
                return;
            }

            IsFightActive = false;
            _boss.ResetBoss();
            SetBarrier(false);
            _unlockAt = -1f;
            FightEnded?.Invoke(_boss);
        }

        private void OnBossDefeated(BossController boss)
        {
            IsFightActive = false;
            // The doors hold shut for a beat so the kill reads before the room opens.
            _unlockAt = Time.time + _unlockDelay;

            if (_progress != null && !_progress.IsBossDefeated(boss.BossId))
            {
                // One transaction: the kill and its reward are recorded together, then written
                // once, so a file can never hold a dead boss without its reward.
                _progress.MarkBossDefeated(boss.BossId);
                if (_progress.GrantReward(_rewardId))
                {
                    // Story rewards are handed over directly rather than dropped, so they cannot
                    // be missed, and the whole payout lands before a single write.
                    _progress.AddItem(_rewardId);
                    _progress.AddCurrency(_rewardCurrency);
                    // Inside the same first-victory guard as the item and the coins, so a boss
                    // already recorded as beaten can never pay its experience a second time.
                    _progression?.AddExperience(_rewardExperience, boss.BossId);
                    // The movement unlock rides inside the same guard as everything else, so it is
                    // granted exactly once and is on disk before the player can jump with it.
                    if (!string.IsNullOrEmpty(_rewardAbilityId))
                    {
                        _progress.UnlockAbility(_rewardAbilityId);
                        AbilityUnlocked?.Invoke(_rewardAbilityId);
                    }

                    RewardGranted?.Invoke(_rewardId, _rewardName);
                }
                _progress.SaveNow();
            }

            BossDefeated?.Invoke(boss);
            FightEnded?.Invoke(boss);
        }

        private void SetBarrier(bool isClosed)
        {
            if (_barrier != null)
            {
                _barrier.SetActive(isClosed);
            }
        }
    }
}
