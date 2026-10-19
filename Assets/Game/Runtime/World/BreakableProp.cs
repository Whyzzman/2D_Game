using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// An urn, a heap of skulls, a crate (GAME_FEEL_IDEAS item 14): one blow from the hero and it
    /// bursts into pieces and dust, the first time with a few coins inside — Hollow Knight's joy of
    /// striking everything, and a small reason to look into corners. It stands again after a rest,
    /// a death or a journey, but the coins are paid once per game (remembered under its id), so a
    /// rest cannot be farmed.
    ///
    /// Neutral and empty of everything that matters: it pays no mana and blocks nothing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatActor))]
    public sealed class BreakableProp : MonoBehaviour
    {
        [SerializeField] private string _propId = string.Empty;
        [SerializeField, Min(0)] private int _gold;
        [SerializeField] private CombatActor _actor;
        [SerializeField] private Collider2D _hurtbox;
        [SerializeField] private SpriteRenderer _visual;
        [Tooltip("Multiplies the pale dust: the colour of what it was made of.")]
        [SerializeField] private Color _dustTint = new Color(0.8f, 0.74f, 0.66f, 1f);
        [SerializeField, Range(2, 8)] private int _pieces = 5;

        private const float PieceLife = 0.9f;
        private const int Coins = 3;

        private GameProgress _progress;
        private LootService _loot;
        private PlayerRespawner _respawner;
        private SpriteRenderer[] _shards;
        private Vector2[] _shardVelocity;
        private float[] _shardSpin;
        private float _brokenAt = -10f;

        public string PropId => _propId;
        public bool IsBroken { get; private set; }

        private void Awake()
        {
            if (_actor == null) { _actor = GetComponent<CombatActor>(); }
            _progress = FindAnyObjectByType<GameProgress>();
            _loot = FindAnyObjectByType<LootService>();
            _respawner = FindAnyObjectByType<PlayerRespawner>();
            _shards = new SpriteRenderer[_pieces];
            _shardVelocity = new Vector2[_pieces];
            _shardSpin = new float[_pieces];
            for (var i = 0; i < _pieces; i++)
            {
                var shard = new GameObject("Piece").AddComponent<SpriteRenderer>();
                shard.transform.SetParent(transform, false);
                if (_visual != null)
                {
                    shard.sprite = _visual.sprite;
                    shard.sharedMaterial = _visual.sharedMaterial;
                    shard.sortingLayerID = _visual.sortingLayerID;
                    shard.sortingOrder = _visual.sortingOrder + 1;
                }

                shard.enabled = false;
                _shards[i] = shard;
            }
        }

        private void OnEnable()
        {
            _actor.DamageTaken += OnDamageTaken;
            if (_respawner != null)
            {
                _respawner.CheckpointRested += OnRested;
                _respawner.PlayerRespawned += Reform;
                _respawner.Travelled += OnRested;
            }
        }

        private void OnDisable()
        {
            _actor.DamageTaken -= OnDamageTaken;
            if (_respawner != null)
            {
                _respawner.CheckpointRested -= OnRested;
                _respawner.PlayerRespawned -= Reform;
                _respawner.Travelled -= OnRested;
            }
        }

        private void OnRested(Checkpoint checkpoint) => Reform();

        private void OnDamageTaken(DamageInfo info, DamageResult result)
        {
            _actor.Health.RestoreToFull();
            if (IsBroken || info.Attacker == null || info.Attacker.GetComponentInParent<PlayerCharacter>() == null)
            {
                return;
            }

            Break(info.HitDirection);
        }

        public void Break(Vector2 direction)
        {
            if (IsBroken) { return; }

            IsBroken = true;
            _brokenAt = Time.time;
            var at = (Vector2)transform.position + Vector2.up * 0.4f;
            AudioManager.Play(AudioId.PropBreak, at);
            CombatFeedbackService.Play(FeedbackKind.LandDust, at, _dustTint, 1.1f);
            if (_visual != null) { _visual.enabled = false; }
            if (_hurtbox != null) { _hurtbox.enabled = false; }

            // Pieces fly the way the blow went, up first, each spinning its own way.
            var side = Mathf.Abs(direction.x) > 0.01f ? Mathf.Sign(direction.x) : 1f;
            for (var i = 0; i < _shards.Length; i++)
            {
                var shard = _shards[i];
                shard.enabled = true;
                shard.color = Color.white;
                shard.transform.localPosition = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.7f), 0f);
                shard.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                shard.transform.localScale = Vector3.one * Random.Range(0.18f, 0.32f);
                _shardVelocity[i] = new Vector2(side * Random.Range(0.5f, 3.2f) + Random.Range(-1f, 1f), Random.Range(3f, 6f));
                _shardSpin[i] = Random.Range(-540f, 540f);
            }

            PayOnce(at);
        }

        private void PayOnce(Vector2 at)
        {
            if (_gold <= 0 || _loot == null || string.IsNullOrEmpty(_propId)
                || (_progress != null && _progress.IsPickupCollected(_propId)))
            {
                return;
            }

            _progress?.MarkPickupCollected(_propId);
            // A few coins rather than one: the spill is the reward as much as the sum.
            var left = _gold;
            for (var i = 0; i < Coins && left > 0; i++)
            {
                var amount = i == Coins - 1 ? left : Mathf.Max(1, _gold / Coins);
                left -= amount;
                _loot.Spawn(new DropResult(string.Empty, true, amount), at + new Vector2((i - 1) * 0.35f, 0.2f));
            }
        }

        private void Reform()
        {
            if (!IsBroken) { return; }

            IsBroken = false;
            if (_visual != null) { _visual.enabled = true; }
            if (_hurtbox != null) { _hurtbox.enabled = true; }
            foreach (var shard in _shards) { shard.enabled = false; }
        }

        private void Update()
        {
            if (!IsBroken) { return; }

            var age = Time.time - _brokenAt;
            if (age > PieceLife)
            {
                if (_shards[0].enabled) { foreach (var shard in _shards) { shard.enabled = false; } }
                return;
            }

            var dt = Time.deltaTime;
            for (var i = 0; i < _shards.Length; i++)
            {
                _shardVelocity[i] += Vector2.down * 18f * dt;
                var t = _shards[i].transform;
                t.localPosition += (Vector3)(_shardVelocity[i] * dt);
                t.localRotation *= Quaternion.Euler(0f, 0f, _shardSpin[i] * dt);
                var color = _shards[i].color;
                color.a = 1f - Mathf.Clamp01((age - PieceLife * 0.5f) / (PieceLife * 0.5f));
                _shards[i].color = color;
            }
        }
    }
}
