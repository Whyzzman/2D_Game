using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The runtime progression service. Gameplay talks to this and nothing else: it holds what has
    /// been lit, beaten and earned, and asks <see cref="SaveSystem"/> to write when something
    /// durable changes. Lookups run off sets, so a long list of ids stays cheap.
    /// </summary>
    /// <summary>What kind of durable world fact just changed.</summary>
    public enum WorldFlag
    {
        CheckpointActivated,
        BossDefeated,
        RewardGranted,
        AbilityUnlocked,
        PickupCollected,
        AreaDiscovered,
        ShortcutUnlocked,
        ZoneIntroSeen
    }

    [DisallowMultipleComponent]
    public sealed class GameProgress : MonoBehaviour, IWorldQuery
    {
        [Tooltip("Off for test scenes that should always start from a clean slate.")]
        [SerializeField] private bool _loadOnAwake = true;
        [Tooltip("Saves asked for within this window are written once, at the end of it.")]
        [SerializeField, Min(0f)] private float _saveDebounce = 0.15f;
        [SerializeField] private ItemDatabase _items;
        [Tooltip("The shape of the world: which altar is in which zone, and what the map shows. "
            + "Without it everything reads as zone one and old saves gain no map.")]
        [SerializeField] private WorldGraph _world;
        private readonly HashSet<string> _activatedCheckpoints = new HashSet<string>();
        private readonly HashSet<string> _collectedPickups = new HashSet<string>();
        private readonly HashSet<string> _defeatedBosses = new HashSet<string>();
        private readonly HashSet<string> _acquiredRewards = new HashSet<string>();
        private readonly HashSet<string> _unlockedAbilities = new HashSet<string>();
        private readonly HashSet<string> _discoveredAreas = new HashSet<string>();
        private readonly HashSet<string> _unlockedShortcuts = new HashSet<string>();
        private readonly HashSet<string> _seenZoneIntros = new HashSet<string>();
        private Coroutine _pendingSave;

        /// <summary>Raised after progression has been replaced, so the world can reapply it.</summary>
        public event Action Loaded;
        public event Action Saved;

        /// <summary>
        /// Raised once per new durable fact — an altar lit, a boss beaten, a room found, a shortcut
        /// opened. Doors and the map redraw from this instead of polling; a repeat of something
        /// already known raises nothing.
        /// </summary>
        public event Action<WorldFlag, string> WorldChanged;

        /// <summary>Item id, amount gained, new total. The HUD and future quests listen here.</summary>
        public event Action<string, int, int> ItemAdded;
        public event Action<string, int, int> ItemRemoved;
        public event Action<int, int> CurrencyChanged;

        /// <summary>Raised when what the player wears is recorded; the HUD and screen listen.</summary>
        public event Action<EquipmentLoadout> LoadoutChanged;

        public GameProgressData Data { get; private set; } = GameProgressData.CreateNew();

        public bool HasSaveOnDisk => SaveSystem.HasValidSave();
        public string CheckpointId => Data.checkpointId;
        public Vector2 RespawnPosition => Data.respawnPosition;
        public PlayerProgressionData Player => Data.player;
        public IReadOnlyCollection<string> ActivatedCheckpoints => _activatedCheckpoints;
        public IReadOnlyCollection<string> DefeatedBosses => _defeatedBosses;
        public IReadOnlyCollection<string> AcquiredRewards => _acquiredRewards;
        public IReadOnlyCollection<string> CollectedPickups => _collectedPickups;
        public IReadOnlyCollection<string> DiscoveredAreas => _discoveredAreas;
        public IReadOnlyCollection<string> UnlockedShortcuts => _unlockedShortcuts;
        public ItemCollection Inventory => Data.inventory;
        public ItemDatabase Items => _items;
        public EquipmentLoadout Loadout => Data.equipment;
        public int Currency => Data.player.currency;

        private void Awake()
        {
            if (_loadOnAwake)
            {
                LoadGame();
            }
            else
            {
                Adopt(GameProgressData.CreateNew());
            }
        }

        private void Update()
        {
            // Play time is only persisted at a save point, so this costs nothing in between.
            Data.totalPlayTimeSeconds += Time.unscaledDeltaTime;
        }

        // ---------- lifecycle ----------

        /// <summary>Starts over: default progression, no altars, no bosses beaten, nothing earned.</summary>
        public void NewGame()
        {
            Adopt(GameProgressData.CreateNew());
            SaveSystem.Delete();
            SaveNow();
            Loaded?.Invoke();
        }

        /// <summary>Reads the file (or the backup, or defaults) and publishes it to the world.</summary>
        public void LoadGame()
        {
            Adopt(SaveSystem.Read());
            Loaded?.Invoke();
        }

        /// <summary>Drops progression without touching the file; used by tests and the debug key.</summary>
        public void ResetProgress()
        {
            Adopt(GameProgressData.CreateNew());
            SaveSystem.Delete();
            Loaded?.Invoke();
        }

        private void Adopt(GameProgressData data)
        {
            Data = data ?? GameProgressData.CreateNew();
            Data.Migrate();
            // Always, not only for old versions: SaveSystem.Read has already raised the version
            // by the time the data arrives here, so an old file cannot be told apart by it. The
            // migration only adds what the save proves and is idempotent, so a current save
            // gains nothing it did not already have.
            WorldDiscoveryMigration.Apply(Data, _world);

            if (_world != null && Data.discoveredAreas.Count == 0)
            {
                // A brand new run knows the room it starts in, and nothing else.
                Data.MarkAreaDiscovered(_world.StartAreaId);
            }

            Rebuild(_activatedCheckpoints, Data.activatedCheckpoints);
            Rebuild(_defeatedBosses, Data.defeatedBosses);
            Rebuild(_acquiredRewards, Data.acquiredRewards);
            Rebuild(_collectedPickups, Data.collectedPickups);
            Rebuild(_unlockedAbilities, Data.player.unlockedAbilities);
            Rebuild(_discoveredAreas, Data.discoveredAreas);
            Rebuild(_unlockedShortcuts, Data.unlockedShortcuts);
            Rebuild(_seenZoneIntros, Data.seenZoneIntros);
        }

        private static void Rebuild(HashSet<string> set, List<string> source)
        {
            set.Clear();
            foreach (var id in source)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    set.Add(id);
                }
            }
        }

        // ---------- progression ----------

        public void SetCheckpoint(string checkpointId, Vector2 respawnPosition)
        {
            Data.checkpointId = checkpointId ?? string.Empty;
            Data.respawnPosition = respawnPosition;
            // Recorded alongside the altar rather than worked out at load, so a save always says
            // where it was made even if the graph changes shape later.
            Data.zoneId = CurrentZone?.ZoneId ?? Data.zoneId;
        }

        /// <summary>
        /// The region this save belongs to, resolved from the altar it was made at. Falls back to
        /// the first zone for an unknown or missing checkpoint, so a damaged or future file starts
        /// somewhere real instead of nowhere.
        /// </summary>
        public ZoneDefinition CurrentZone => _world != null
            ? _world.Resolve(Data.checkpointId, Data.zoneId)
            : null;

        public WorldGraph World => _world;

        /// <summary>True when the saved checkpoint belongs to a zone this build still has.</summary>
        public bool HasKnownZone => _world == null
            || _world.ZoneForCheckpoint(Data.checkpointId) != null;

        /// <summary>Clears a checkpoint id that no longer exists in this build.</summary>
        public void ClearCheckpoint()
        {
            Data.checkpointId = string.Empty;
        }

        public void MarkCheckpointActivated(string checkpointId)
        {
            if (!string.IsNullOrEmpty(checkpointId) && _activatedCheckpoints.Add(checkpointId))
            {
                Data.MarkCheckpointActivated(checkpointId);
                WorldChanged?.Invoke(WorldFlag.CheckpointActivated, checkpointId);
            }
        }

        public bool IsCheckpointActivated(string checkpointId)
        {
            return !string.IsNullOrEmpty(checkpointId) && _activatedCheckpoints.Contains(checkpointId);
        }

        public void MarkBossDefeated(string bossId)
        {
            if (!string.IsNullOrEmpty(bossId) && _defeatedBosses.Add(bossId))
            {
                Data.MarkBossDefeated(bossId);
                WorldChanged?.Invoke(WorldFlag.BossDefeated, bossId);
            }
        }

        /// <summary>Returns true only the first time; entering a known room changes nothing.</summary>
        public bool MarkAreaDiscovered(string areaId)
        {
            if (string.IsNullOrEmpty(areaId) || !_discoveredAreas.Add(areaId))
            {
                return false;
            }

            Data.MarkAreaDiscovered(areaId);
            WorldChanged?.Invoke(WorldFlag.AreaDiscovered, areaId);
            return true;
        }

        public bool IsAreaDiscovered(string areaId)
        {
            return !string.IsNullOrEmpty(areaId) && _discoveredAreas.Contains(areaId);
        }

        /// <summary>Idempotent: a second pull of the same lever opens nothing new and saves nothing.</summary>
        public bool UnlockShortcut(string shortcutId)
        {
            if (string.IsNullOrEmpty(shortcutId) || !_unlockedShortcuts.Add(shortcutId))
            {
                return false;
            }

            Data.UnlockShortcut(shortcutId);
            WorldChanged?.Invoke(WorldFlag.ShortcutUnlocked, shortcutId);
            return true;
        }

        public bool IsShortcutUnlocked(string shortcutId)
        {
            return !string.IsNullOrEmpty(shortcutId) && _unlockedShortcuts.Contains(shortcutId);
        }

        public bool HasSeenZoneIntro(string zoneId)
        {
            return !string.IsNullOrEmpty(zoneId) && _seenZoneIntros.Contains(zoneId);
        }

        public bool MarkZoneIntroSeen(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || !_seenZoneIntros.Add(zoneId))
            {
                return false;
            }

            Data.MarkZoneIntroSeen(zoneId);
            WorldChanged?.Invoke(WorldFlag.ZoneIntroSeen, zoneId);
            return true;
        }

        /// <summary>A progression reward, or a key item that is actually in the bag.</summary>
        public bool HasProgression(string progressionId)
        {
            return HasReward(progressionId)
                || (!string.IsNullOrEmpty(progressionId) && HasItem(progressionId));
        }

        public bool IsWorldPickupCollected(string pickupId) => IsPickupCollected(pickupId);

        public bool IsBossDefeated(string bossId)
        {
            return !string.IsNullOrEmpty(bossId) && _defeatedBosses.Contains(bossId);
        }

        /// <summary>Granting is idempotent: a reward already owned is never handed out twice.</summary>
        public bool GrantReward(string rewardId)
        {
            if (string.IsNullOrEmpty(rewardId) || !_acquiredRewards.Add(rewardId))
            {
                return false;
            }

            Data.GrantReward(rewardId);
            WorldChanged?.Invoke(WorldFlag.RewardGranted, rewardId);
            return true;
        }

        public bool HasReward(string rewardId)
        {
            return !string.IsNullOrEmpty(rewardId) && _acquiredRewards.Contains(rewardId);
        }

        public bool HasAbility(string abilityId)
        {
            return !string.IsNullOrEmpty(abilityId) && _unlockedAbilities.Contains(abilityId);
        }

        public bool UnlockAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId) || !_unlockedAbilities.Add(abilityId))
            {
                return false;
            }

            Data.player.unlockedAbilities.Add(abilityId);
            WorldChanged?.Invoke(WorldFlag.AbilityUnlocked, abilityId);
            return true;
        }

        // ---------- items and currency ----------

        public int GetItemAmount(string itemId)
        {
            return Data.inventory.GetAmount(itemId);
        }

        public bool HasItem(string itemId, int amount = 1)
        {
            return Data.inventory.HasItem(itemId, amount);
        }

        /// <summary>
        /// Adds an item by id. An id the database does not know is refused outright rather than
        /// quietly turning into something else, and a unique item is never counted twice.
        /// </summary>
        public AddItemResult AddItem(string itemId, int amount = 1)
        {
            if (_items != null && !_items.TryGetItem(itemId, out _))
            {
                Debug.LogError($"Unknown item id '{itemId}'; nothing was added.");
                return AddItemResult.InvalidItem;
            }

            var definition = _items != null ? _items.GetItem(itemId) : null;
            var isUnique = definition != null && definition.IsUnique;
            var result = Data.inventory.Add(itemId, amount, isUnique);
            if (result != AddItemResult.Added)
            {
                return result;
            }

            ItemAdded?.Invoke(itemId, amount, Data.inventory.GetAmount(itemId));
            if (definition != null && definition.AutosaveOnAcquire)
            {
                // Key and story items are written at once, so a crash cannot swallow them.
                SaveNow();
            }

            return AddItemResult.Added;
        }

        public bool TryRemoveItem(string itemId, int amount = 1)
        {
            if (!Data.inventory.TryRemove(itemId, amount))
            {
                return false;
            }

            ItemRemoved?.Invoke(itemId, amount, Data.inventory.GetAmount(itemId));
            return true;
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Data.player.currency += amount;
            CurrencyChanged?.Invoke(Data.player.currency, amount);
        }

        /// <summary>Spends the full amount or nothing; currency can never go negative.</summary>
        public bool TrySpendCurrency(int amount)
        {
            if (amount <= 0 || Data.player.currency < amount)
            {
                return false;
            }

            Data.player.currency -= amount;
            CurrencyChanged?.Invoke(Data.player.currency, -amount);
            return true;
        }

        public bool IsPickupCollected(string pickupId)
        {
            return !string.IsNullOrEmpty(pickupId) && _collectedPickups.Contains(pickupId);
        }

        public void MarkPickupCollected(string pickupId)
        {
            if (!string.IsNullOrEmpty(pickupId) && _collectedPickups.Add(pickupId))
            {
                Data.MarkPickupCollected(pickupId);
                WorldChanged?.Invoke(WorldFlag.PickupCollected, pickupId);
            }
        }

        /// <summary>
        /// Stores a new loadout in progression. It is not written to disk here: equipment changes
        /// while a menu is open, and the screen asks for one save when it closes instead of one
        /// per click.
        /// </summary>
        public void SetLoadout(EquipmentLoadout loadout)
        {
            if (loadout == null)
            {
                return;
            }

            Data.equipment = loadout.Clone();
            LoadoutChanged?.Invoke(Data.equipment);
        }

        // ---------- saving ----------

        /// <summary>
        /// Asks for a save. Several requests in the same moment collapse into one write, so a boss
        /// death that also grants a reward and moves the checkpoint touches the disk once.
        /// </summary>
        public void Save()
        {
            if (!isActiveAndEnabled || _saveDebounce <= 0f)
            {
                SaveNow();
                return;
            }

            _pendingSave ??= StartCoroutine(SaveSoon());
        }

        /// <summary>Writes immediately; for quitting and for anything that must not be lost.</summary>
        public void SaveNow()
        {
            if (SaveSystem.Write(Data))
            {
                Saved?.Invoke();
            }
        }

        private IEnumerator SaveSoon()
        {
            yield return new WaitForSecondsRealtime(_saveDebounce);
            _pendingSave = null;
            SaveNow();
        }

        private void OnApplicationQuit()
        {
            // Best effort only; the real guarantees are the altar and the boss.
            SaveNow();
        }
    }
}
