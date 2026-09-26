using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    public enum SkillPurchaseResult
    {
        Purchased,
        InvalidSkill,
        NotEnoughPoints,
        MissingPrerequisite,
        LevelTooLow,
        AlreadyMaxRank,
        /// <summary>Skills are bought between fights, not in the middle of one.</summary>
        NotAllowedNow
    }

    /// <summary>
    /// What the player has learned. It is the only thing that knows which skills are owned and at
    /// what rank; everything else asks it a question by effect type rather than by skill name, so
    /// no gameplay script ever contains "if the player has skill X".
    ///
    /// Unspent points are deliberately not stored. They are the points the level has earned, plus
    /// any granted elsewhere, minus what has been spent — worked out fresh every time. That makes
    /// a duplicated grant structurally impossible rather than something a migration has to avoid.
    ///
    /// Its Start runs ahead of the default order. The skill bar restores its slots in its own
    /// Start and drops any skill not owned yet; with the two in an unspecified order the bar
    /// came back empty whenever the scene's composition shifted the order.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerSkills : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerProgressionService _progression;
        [SerializeField] private SkillDatabase _database;
        [SerializeField] private PlayerCombatStatus _combatStatus;
        [Tooltip("Levels needed per skill point: 2 means one point at level 2, 4, 6 and so on.")]
        [SerializeField, Min(1)] private int _levelsPerSkillPoint = 2;
        private readonly Dictionary<string, int> _ranks = new Dictionary<string, int>();
        private readonly float[] _effects =
            new float[Enum.GetValues(typeof(SkillEffectType)).Length];

        /// <summary>Raised after anything about the learned skills changes.</summary>
        public event Action SkillsChanged;
        public event Action<SkillDefinition, int> SkillUnlocked;
        public event Action<int> SkillPointsChanged;

        public SkillDatabase Database => _database;
        public IReadOnlyDictionary<string, int> Ranks => _ranks;

        /// <summary>Points the level has earned altogether; never decreases.</summary>
        public int EarnedSkillPoints => TotalEarnedForLevel(
            _progression != null ? _progression.Level : 1)
            + (_progress != null ? _progress.Data.player.bonusSkillPoints : 0);

        public int SpentSkillPoints
        {
            get
            {
                var spent = 0;
                foreach (var pair in _ranks)
                {
                    if (_database != null && _database.TryGetSkill(pair.Key, out var skill))
                    {
                        spent += skill.Cost * pair.Value;
                    }
                }

                return spent;
            }
        }

        public int AvailableSkillPoints => Mathf.Max(0, EarnedSkillPoints - SpentSkillPoints);

        /// <summary>
        /// Skills are learned between fights. The tree can be read anywhere; spending is what
        /// waits, so a hard fight cannot be solved by shopping in the middle of it.
        /// </summary>
        public bool CanSpendPoints => _combatStatus == null || !_combatStatus.IsInCombat;

        public int TotalEarnedForLevel(int level)
        {
            return Mathf.Max(0, level) / Mathf.Max(1, _levelsPerSkillPoint);
        }

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_progression == null) { _progression = FindAnyObjectByType<PlayerProgressionService>(); }
            if (_combatStatus == null) { _combatStatus = FindAnyObjectByType<PlayerCombatStatus>(); }
        }

        private void OnEnable()
        {
            if (_progress != null) { _progress.Loaded += ReloadFromSave; }
            if (_progression != null) { _progression.LevelChanged += OnLevelChanged; }
        }

        private void OnDisable()
        {
            if (_progress != null) { _progress.Loaded -= ReloadFromSave; }
            if (_progression != null) { _progression.LevelChanged -= OnLevelChanged; }
        }

        private void Start()
        {
            ReloadFromSave();
        }

        /// <summary>
        /// Reads the saved skills and rebuilds the effect totals from scratch. Because the totals
        /// are recomputed rather than accumulated, loading twice cannot double a passive.
        /// </summary>
        public void ReloadFromSave()
        {
            _ranks.Clear();
            if (_progress == null)
            {
                Recalculate();
                return;
            }

            foreach (var entry in _progress.Data.player.unlockedSkills)
            {
                if (!TryParse(entry, out var skillId, out var rank))
                {
                    continue;
                }

                if (_database == null || !_database.TryGetSkill(skillId, out var skill))
                {
                    // A skill from another build: kept out of the runtime rather than crashing.
                    Debug.LogWarning($"Saved skill '{skillId}' is not in this build; ignored.");
                    continue;
                }

                if (rank > skill.MaxRank)
                {
                    Debug.LogWarning($"Saved skill '{skillId}' is rank {rank} but only "
                        + $"{skill.MaxRank} exist; clamped.");
                    rank = skill.MaxRank;
                }

                _ranks[skillId] = rank;
            }

            Recalculate();
        }

        // ---------- questions the rest of the game asks ----------

        public int RankOf(string skillId)
        {
            return !string.IsNullOrEmpty(skillId) && _ranks.TryGetValue(skillId, out var rank)
                ? rank
                : 0;
        }

        public bool HasSkill(string skillId)
        {
            return RankOf(skillId) > 0;
        }

        /// <summary>
        /// The total for one kind of effect across every learned skill. This is how combat,
        /// movement and the guard read the tree: by what they need, never by skill name.
        /// </summary>
        public float Effect(SkillEffectType type)
        {
            return _effects[(int)type];
        }

        /// <summary>A share expressed as a multiplier, for the common "+15% damage" case.</summary>
        public float Multiplier(SkillEffectType type)
        {
            return Mathf.Max(0f, 1f + _effects[(int)type]);
        }

        // ---------- buying ----------

        public SkillPurchaseResult CanPurchase(string skillId)
        {
            if (_database == null || !_database.TryGetSkill(skillId, out var skill))
            {
                return SkillPurchaseResult.InvalidSkill;
            }

            var rank = RankOf(skillId);
            if (rank >= skill.MaxRank)
            {
                return SkillPurchaseResult.AlreadyMaxRank;
            }

            // Prerequisites are reported before the level: a node the player has not worked
            // towards at all is a more useful answer than one they merely have to wait for.
            foreach (var prerequisite in skill.Prerequisites)
            {
                if (!HasSkill(prerequisite))
                {
                    return SkillPurchaseResult.MissingPrerequisite;
                }
            }

            if (_progression != null && _progression.Level < skill.RequiredLevel)
            {
                return SkillPurchaseResult.LevelTooLow;
            }

            if (AvailableSkillPoints < skill.Cost)
            {
                return SkillPurchaseResult.NotEnoughPoints;
            }

            if (!CanSpendPoints)
            {
                return SkillPurchaseResult.NotAllowedNow;
            }

            return SkillPurchaseResult.Purchased;
        }

        /// <summary>
        /// Learns a skill, or raises its rank. Every condition is checked before anything is
        /// written, so a refused purchase leaves the character exactly as it was.
        /// </summary>
        public SkillPurchaseResult Purchase(string skillId)
        {
            var verdict = CanPurchase(skillId);
            if (verdict != SkillPurchaseResult.Purchased)
            {
                return verdict;
            }

            var skill = _database.GetSkill(skillId);
            var rank = RankOf(skillId) + 1;
            _ranks[skillId] = rank;
            WriteBack();
            Recalculate();
            SkillUnlocked?.Invoke(skill, rank);
            // Learning a skill is durable progress and is written at once, like a boss kill.
            _progress?.Save();
            return SkillPurchaseResult.Purchased;
        }

        // ---------- development only ----------

        /// <summary>Grants points from somewhere other than a level; the boss reward uses it.</summary>
        public void GrantBonusPoints(int amount)
        {
            if (_progress == null || amount <= 0)
            {
                return;
            }

            _progress.Data.player.bonusSkillPoints += amount;
            SkillPointsChanged?.Invoke(AvailableSkillPoints);
        }

        /// <summary>Learns a skill ignoring every rule. Development only.</summary>
        public void DebugUnlock(string skillId)
        {
            if (_database == null || !_database.TryGetSkill(skillId, out var skill))
            {
                return;
            }

            var rank = Mathf.Min(skill.MaxRank, RankOf(skillId) + 1);
            _ranks[skillId] = rank;
            GrantBonusPoints(skill.Cost);
            WriteBack();
            Recalculate();
            // Announced like an ordinary unlock, so everything that reacts to learning a skill —
            // the spellbook above all — behaves the same way it would in a real game.
            SkillUnlocked?.Invoke(skill, rank);
        }

        public void DebugUnlockAll()
        {
            if (_database == null)
            {
                return;
            }

            foreach (var skill in _database.Skills)
            {
                if (skill != null)
                {
                    _ranks[skill.SkillId] = skill.MaxRank;
                    GrantBonusPoints(skill.Cost * skill.MaxRank);
                }
            }

            WriteBack();
            Recalculate();
            foreach (var skill in _database.Skills)
            {
                if (skill != null)
                {
                    SkillUnlocked?.Invoke(skill, skill.MaxRank);
                }
            }
        }

        /// <summary>Forgets everything learned. Development only; this is not a respec.</summary>
        public void DebugReset()
        {
            _ranks.Clear();
            WriteBack();
            Recalculate();
        }

        // ---------- internals ----------

        private void OnLevelChanged(int previous, int current)
        {
            SkillPointsChanged?.Invoke(AvailableSkillPoints);
        }

        private void WriteBack()
        {
            if (_progress == null)
            {
                return;
            }

            var list = _progress.Data.player.unlockedSkills;
            list.Clear();
            foreach (var pair in _ranks)
            {
                list.Add(pair.Value > 1 ? $"{pair.Key}:{pair.Value}" : pair.Key);
            }
        }

        /// <summary>
        /// Adds every learned skill's effects up from nothing. Called on load, on purchase and on
        /// a debug change — never incrementally, so the totals cannot drift.
        /// </summary>
        private void Recalculate()
        {
            Array.Clear(_effects, 0, _effects.Length);
            if (_database != null)
            {
                foreach (var pair in _ranks)
                {
                    if (!_database.TryGetSkill(pair.Key, out var skill))
                    {
                        continue;
                    }

                    foreach (var effect in skill.Effects)
                    {
                        if (effect != null)
                        {
                            _effects[(int)effect.Type] += effect.ValuePerRank * pair.Value;
                        }
                    }
                }
            }

            SkillsChanged?.Invoke();
            SkillPointsChanged?.Invoke(AvailableSkillPoints);
        }

        private static bool TryParse(string entry, out string skillId, out int rank)
        {
            skillId = entry;
            rank = 1;
            if (string.IsNullOrWhiteSpace(entry))
            {
                return false;
            }

            var separator = entry.IndexOf(':');
            if (separator < 0)
            {
                return true;
            }

            skillId = entry.Substring(0, separator);
            return !string.IsNullOrEmpty(skillId)
                && int.TryParse(entry.Substring(separator + 1), out rank) && rank > 0;
        }
    }
}
