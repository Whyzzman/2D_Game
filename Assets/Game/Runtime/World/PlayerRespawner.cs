using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Owns the whole death loop in one place: the fade, the world reset, where the player comes
    /// back and what is restored. Health, checkpoints and enemies keep their own rules; this only
    /// decides the order things happen in.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRespawner : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private PlayerMotor2D _motor;
        [SerializeField] private PlayerCombatStatus _combatStatus;
        [SerializeField] private Transform _defaultSpawn;
        [SerializeField] private GameProgress _progress;
        [SerializeField] private EnemyRegistry _enemies;
        [SerializeField] private LootService _loot;
        [Tooltip("Falling below this height is fatal, whatever else is going on.")]
        [SerializeField] private float _worldFloorY = -12f;
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.35f;
        [Tooltip("Grace period after coming back, so a stray hit cannot kill on arrival.")]
        [SerializeField, Min(0f)] private float _spawnProtection = 0.8f;
        [SerializeField, Min(0f)] private float _restDuration = 0.8f;
        private readonly Dictionary<string, Checkpoint> _checkpoints =
            new Dictionary<string, Checkpoint>();
        private Vector2 _respawnPosition;
        private bool _isSequenceRunning;

        /// <summary>Fade hooks. The HUD will answer them once it exists; nothing else needs to.</summary>
        public event Action<float> FadeOutRequested;
        public event Action<float> FadeInRequested;
        public event Action PlayerDied;
        public event Action PlayerRespawned;
        public event Action<Checkpoint> CheckpointActivated;
        public event Action<Checkpoint> CheckpointRested;

        /// <summary>Raised under the fade, once the player stands at a fast-travel destination.</summary>
        public event Action<Checkpoint> Travelled;

        public string ActiveCheckpointId { get; private set; } = string.Empty;
        public Vector2 RespawnPosition => _respawnPosition;
        public int DeathCount { get; private set; }
        public bool IsSequenceRunning => _isSequenceRunning;
        public EnemyRegistry Enemies => _enemies;

        private void Awake()
        {
            if (_player == null || _motor == null || _defaultSpawn == null)
            {
                Debug.LogError("PlayerRespawner requires the player and a default spawn.", this);
                enabled = false;
                return;
            }

            _respawnPosition = _defaultSpawn.position;
        }

        private void OnEnable()
        {
            _player.ReadyToRespawn += BeginRespawn;
            _player.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            _player.ReadyToRespawn -= BeginRespawn;
            _player.Died -= OnPlayerDied;
        }

        /// <summary>
        /// The altars live in the zone scenes, which arrive a frame after this one: the saved
        /// checkpoint is resolved once the streamer says the first zones are in, against a
        /// complete registry.
        /// </summary>
        private void Start()
        {
            // Equipment first, then progression: the bars are sized from progression plus what is
            // worn, so reading them in the other order would show a maximum that is about to
            // change. Both are settled before the player is placed and the HUD reads anything.
            if (_progress != null)
            {
                _player.Equipment?.ApplySavedLoadout();
                _player.ApplyProgression(_progress.Player);
            }

            var streamer = ZoneStreamer.Current;
            if (streamer != null && !streamer.IsReady)
            {
                streamer.Ready += OnZonesReady;
                return;
            }

            ApplySavedCheckpoint();
        }

        private void OnZonesReady()
        {
            ZoneStreamer.Current.Ready -= OnZonesReady;
            ApplySavedCheckpoint();
        }

        public void ApplySavedCheckpoint()
        {
            if (_progress == null || string.IsNullOrEmpty(_progress.CheckpointId))
            {
                return;
            }

            var savedId = _progress.CheckpointId;
            var checkpoint = FindCheckpoint(savedId);
            if (checkpoint == null)
            {
                // The build no longer has that altar; start over from the level's spawn rather
                // than dropping the player at a position that means nothing here.
                Debug.LogWarning($"Saved checkpoint '{savedId}' is not in this level; "
                    + "falling back to the default spawn.");
                _progress.ClearCheckpoint();
                ActiveCheckpointId = string.Empty;
                _respawnPosition = _defaultSpawn.position;
                return;
            }

            ActiveCheckpointId = savedId;
            _respawnPosition = checkpoint.RespawnPosition;
            _motor.ResetTo(_respawnPosition);
        }

        private void LateUpdate()
        {
            if (!_isSequenceRunning && _player.IsAlive
                && _player.transform.position.y < _worldFloorY)
            {
                // Out of the world is fatal, and it goes through the ordinary death flow.
                Kill();
            }
        }

        /// <summary>Checkpoints announce themselves so nothing has to search the scene later.</summary>
        public void RegisterCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || string.IsNullOrEmpty(checkpoint.CheckpointId))
            {
                return;
            }

            if (_checkpoints.TryGetValue(checkpoint.CheckpointId, out var existing)
                && existing != checkpoint)
            {
                Debug.LogError($"Two checkpoints share the id '{checkpoint.CheckpointId}'; "
                    + "a save would not be able to tell them apart.", checkpoint);
                return;
            }

            _checkpoints[checkpoint.CheckpointId] = checkpoint;
        }

        public void UnregisterCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint != null)
            {
                _checkpoints.Remove(checkpoint.CheckpointId);
            }
        }

        /// <summary>Starts a brand new run: default progression and the level's own spawn.</summary>
        public void StartNewGame()
        {
            if (_progress == null)
            {
                return;
            }

            _progress.NewGame();
            ActiveCheckpointId = string.Empty;
            _respawnPosition = _defaultSpawn.position;
            _player.Equipment?.ApplySavedLoadout();
            _player.ApplyProgression(_progress.Player);
            ResetWorld(WorldResetReason.Debug);
            RestorePlayer();
        }

        public Checkpoint FindCheckpoint(string checkpointId)
        {
            return !string.IsNullOrEmpty(checkpointId)
                && _checkpoints.TryGetValue(checkpointId, out var checkpoint)
                ? checkpoint
                : null;
        }

        public void SetRespawnPoint(Vector2 position, string checkpointId)
        {
            _respawnPosition = position;
            ActiveCheckpointId = checkpointId;
            _progress?.SetCheckpoint(checkpointId, position);
        }

        /// <summary>Lighting an altar: it becomes the respawn point and restores the player.</summary>
        public void ActivateCheckpoint(Checkpoint checkpoint, Vector2 respawnPosition)
        {
            SetRespawnPoint(respawnPosition, checkpoint.CheckpointId);
            _progress?.MarkCheckpointActivated(checkpoint.CheckpointId);
            RestorePlayer();
            _progress?.Save();
            CheckpointActivated?.Invoke(checkpoint);
        }

        /// <summary>Resting: restores the player and puts every ordinary enemy back.</summary>
        public void RestAt(Checkpoint checkpoint)
        {
            if (_isSequenceRunning)
            {
                return;
            }

            StartCoroutine(RestRoutine(checkpoint));
        }

        public void ResetWorld(WorldResetReason reason)
        {
            _enemies?.ResetAll(reason);
            // Loot nobody picked up goes with the enemies that dropped it, so resting cannot be
            // used to pile up drops that were never collected.
            _loot?.ClearTransientLoot();
            _combatStatus?.Clear();

            // Anything the fight threw into the air or left on the floor goes with it. An arrow
            // still flying or a poison pool still burning would otherwise meet the player on the
            // far side of a respawn, having been fired at someone who no longer exists.
            SpellProjectile.ClearAll(Faction.Enemy);
            SpellProjectile.ClearAll(Faction.Player);
            HazardZone.ClearSpawned(Faction.Enemy);
            HazardZone.ClearSpawned(Faction.Player);
        }

        /// <summary>Kills the player outright; used by kill zones and by the debug key.</summary>
        public void Kill()
        {
            if (!_player.IsAlive)
            {
                return;
            }

            _player.Actor.ClearInvulnerability();
            _player.Actor.ApplyDamage(new DamageInfo(float.MaxValue, Vector2.up,
                isUnblockable: true, isParryable: false));
        }

        public void RespawnPlayer()
        {
            BeginRespawn();
        }

        private void OnPlayerDied()
        {
            DeathCount++;
            if (_progress != null)
            {
                _progress.Data.deaths++;
            }

            PlayerDied?.Invoke();
        }

        private void BeginRespawn()
        {
            if (_isSequenceRunning)
            {
                return;
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            _isSequenceRunning = true;
            FadeOutRequested?.Invoke(_fadeOutDuration);
            yield return new WaitForSecondsRealtime(_fadeOutDuration);

            // Under the fade: the altar's zone first, if the death was far from it; then the
            // world back, then the player, so nothing is seen teleporting.
            if (ZoneStreamer.Current != null)
            {
                yield return ZoneStreamer.Current.PrepareAt(_respawnPosition);
            }

            ResetWorld(WorldResetReason.PlayerDeath);
            RestorePlayer();
            _player.Actor.SetInvulnerable(ImmunitySource.Respawn, _spawnProtection);

            FadeInRequested?.Invoke(_fadeInDuration);
            yield return new WaitForSecondsRealtime(_fadeInDuration);
            _isSequenceRunning = false;
            PlayerRespawned?.Invoke();
        }

        private IEnumerator RestRoutine(Checkpoint checkpoint)
        {
            _isSequenceRunning = true;
            FadeOutRequested?.Invoke(_fadeOutDuration);
            yield return new WaitForSecondsRealtime(_fadeOutDuration);

            ResetWorld(WorldResetReason.CheckpointRest);
            RestorePlayer();
            _progress?.Save();
            yield return new WaitForSecondsRealtime(_restDuration);

            FadeInRequested?.Invoke(_fadeInDuration);
            yield return new WaitForSecondsRealtime(_fadeInDuration);
            _isSequenceRunning = false;
            CheckpointRested?.Invoke(checkpoint);
        }

        /// <summary>
        /// Moves the player to another lit altar. Refused while any other sequence runs or the
        /// player is dead; the caller has already checked everything that is about the world.
        /// </summary>
        public bool BeginTravel(Checkpoint destination, Action<bool> completed = null)
        {
            return destination != null && BeginTravel(destination.CheckpointId, completed);
        }

        /// <summary>
        /// The same by id, for an altar whose zone may not be loaded yet: it is loaded under the
        /// fade and the altar looked up once it stands.
        /// </summary>
        public bool BeginTravel(string destinationId, Action<bool> completed = null)
        {
            if (_isSequenceRunning || string.IsNullOrEmpty(destinationId) || !_player.IsAlive)
            {
                return false;
            }

            StartCoroutine(TravelRoutine(destinationId, completed));
            return true;
        }

        /// <summary>
        /// The same order as every other transition, so nothing is seen out of place: fade, put the
        /// world back, place the player, let the camera and the region's sound follow, write the
        /// save once the player is really standing there, then fade in. A destination that has
        /// vanished under the fade changes nothing and the player stays where they were.
        /// </summary>
        private IEnumerator TravelRoutine(string destinationId, Action<bool> completed)
        {
            _isSequenceRunning = true;
            var input = _player.GetComponent<PlayerInputReader>();
            input?.SetGameplayEnabled(false);
            // Nothing can land on a player who is between two places.
            _player.Actor.SetInvulnerable(ImmunitySource.Respawn,
                _fadeOutDuration + _fadeInDuration + _restDuration + _spawnProtection);
            FadeOutRequested?.Invoke(_fadeOutDuration);
            yield return new WaitForSecondsRealtime(_fadeOutDuration);

            var streamer = ZoneStreamer.Current;
            var zone = _progress != null && _progress.World != null
                ? _progress.World.ZoneForCheckpoint(destinationId)
                : null;
            if (streamer != null && zone != null)
            {
                yield return streamer.Prepare(zone.ZoneId);
            }

            var destination = FindCheckpoint(destinationId);
            var arrived = destination != null && destination.isActiveAndEnabled;
            if (arrived)
            {
                // A fast travel is a rest at the far end: enemies back, player refilled, nothing
                // from the old place still in the air.
                ResetWorld(WorldResetReason.CheckpointRest);
                SetRespawnPoint(destination.RespawnPosition, destination.CheckpointId);
                RestorePlayer();
                _player.Actor.SetInvulnerable(ImmunitySource.Respawn, _spawnProtection);
                _progress?.SaveNow();
                Travelled?.Invoke(destination);
                yield return new WaitForSecondsRealtime(_restDuration);
            }
            else
            {
                Debug.LogWarning("Fast travel destination vanished under the fade; "
                    + "the player stays where they were and nothing was saved.");
            }

            FadeInRequested?.Invoke(_fadeInDuration);
            yield return new WaitForSecondsRealtime(_fadeInDuration);
            input?.SetGameplayEnabled(true);
            _isSequenceRunning = false;
            completed?.Invoke(arrived);
        }

        /// <summary>
        /// Spikes: a short fade, the player back on the last safe ground, and a moment of grace —
        /// Hollow Knight's hazard respawn. No rest, no world reset; the fight around carries on.
        /// </summary>
        public bool BeginHazardReturn(Vector2 position)
        {
            if (_isSequenceRunning || _player == null || !_player.IsAlive)
            {
                return false;
            }

            StartCoroutine(HazardReturnRoutine(position));
            return true;
        }

        private IEnumerator HazardReturnRoutine(Vector2 position)
        {
            _isSequenceRunning = true;
            var input = _player.GetComponent<PlayerInputReader>();
            input?.SetGameplayEnabled(false);
            _player.Actor.SetInvulnerable(ImmunitySource.Respawn, HazardFade * 2f + _spawnProtection);
            FadeOutRequested?.Invoke(HazardFade);
            yield return new WaitForSecondsRealtime(HazardFade);

            if (_player.IsAlive)
            {
                var motor = _player.GetComponent<PlayerMotor2D>();
                if (motor != null) { motor.ResetTo(position); }
            }

            FadeInRequested?.Invoke(HazardFade);
            yield return new WaitForSecondsRealtime(HazardFade);
            input?.SetGameplayEnabled(true);
            _isSequenceRunning = false;
        }

        private const float HazardFade = 0.2f;

        /// <summary>
        /// Everything the player gets back. Health, stamina and poise go through their own APIs so
        /// their change events still fire for the bars.
        /// </summary>
        private void RestorePlayer()
        {
            _player.Respawn(_respawnPosition);
            _combatStatus?.Clear();
        }
    }
}
