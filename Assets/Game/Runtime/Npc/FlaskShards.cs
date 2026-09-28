using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Counts the flask shards bought from the merchant and gives the hero one flask for each
    /// (item 70). The save holds the shards, never the flask count, so the two cannot disagree.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FlaskShards : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private GameProgress _progress;

        public int Count { get; private set; }

        private void Awake()
        {
            if (_player == null) { _player = GetComponent<PlayerCharacter>(); }
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
        }

        private void OnEnable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged += OnWorldChanged;
                _progress.Loaded += Apply;
            }
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.WorldChanged -= OnWorldChanged;
                _progress.Loaded -= Apply;
            }
        }

        private void Start() => Apply();

        private void OnWorldChanged(WorldFlag flag, string id)
        {
            if (flag == WorldFlag.RewardGranted && id.StartsWith(MerchantStock.FlaskShardPrefix))
            {
                Apply();
            }
        }

        public void Apply()
        {
            Count = 0;
            if (_progress != null)
            {
                foreach (var reward in _progress.AcquiredRewards)
                {
                    Count += reward.StartsWith(MerchantStock.FlaskShardPrefix) ? 1 : 0;
                }
            }

            if (_player != null)
            {
                _player.Flasks.SetBonusCharges(Count);
            }
        }
    }
}
