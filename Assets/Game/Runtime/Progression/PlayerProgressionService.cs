using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Owns experience, levels and attribute points. It holds only what the player actually chose
    /// — level, experience, unspent points and the four attributes — and nothing derived: health,
    /// stamina, defence and weapon damage are all worked out from these by
    /// <see cref="PlayerStats"/>, so a bonus can never be applied twice or left behind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerProgressionService : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private ProgressionCurve _curve;

        /// <summary>Amount gained and where it came from; the HUD's "+25 XP" hook.</summary>
        public event Action<int, string> ExperienceGained;
        public event Action<int, int> ExperienceChanged;
        public event Action<int, int> LevelChanged;
        public event Action<CharacterStat, int, int> StatChanged;

        public ProgressionCurve Curve => _curve;
        public PlayerProgressionData Data => _progress != null ? _progress.Player : null;
        public int Level => Data?.level ?? 1;
        public int Experience => Data?.experience ?? 0;
        public int StatPoints => Data?.statPoints ?? 0;
        public bool IsMaxLevel => _curve != null && Level >= _curve.MaxLevel;

        /// <summary>Experience still owed for the next level; zero at the cap.</summary>
        public int ExperienceForNextLevel =>
            _curve != null ? _curve.ExperienceForNextLevel(Level) : 0;

        private void Awake()
        {
            if (_progress == null)
            {
                _progress = FindAnyObjectByType<GameProgress>();
            }
        }

        private void OnEnable()
        {
            if (_progress != null)
            {
                _progress.Loaded += EnsureStarted;
            }
        }

        private void OnDisable()
        {
            if (_progress != null)
            {
                _progress.Loaded -= EnsureStarted;
            }
        }

        private void Start()
        {
            EnsureStarted();
        }

        /// <summary>
        /// Fills in a character that has never had attributes: a new game, or a save written
        /// before they existed. All four at zero is the marker, because no real character has
        /// that — the curve's starting values are the only source for what they should be.
        /// </summary>
        public void EnsureStarted()
        {
            var data = Data;
            if (data == null || _curve == null || data.stats.Total > 0)
            {
                return;
            }

            data.stats = _curve.StartingStats();
        }

        // ---------- experience ----------

        /// <summary>
        /// Adds experience and levels up as many times as it pays for, keeping the remainder.
        /// Negative amounts are refused rather than quietly turned into a loss.
        /// </summary>
        public int AddExperience(int amount, string source = "")
        {
            var data = Data;
            if (data == null || amount <= 0)
            {
                return 0;
            }

            if (IsMaxLevel)
            {
                // At the cap experience stops being collected, so it cannot silently overflow.
                ExperienceGained?.Invoke(0, source);
                return 0;
            }

            var startingLevel = data.level;
            data.experience += amount;
            var levelsGained = 0;
            while (!IsMaxLevel)
            {
                var required = _curve.ExperienceForNextLevel(data.level);
                if (required <= 0 || data.experience < required)
                {
                    break;
                }

                // The overflow carries into the next level rather than being thrown away.
                data.experience -= required;
                data.level++;
                data.statPoints += _curve.StatPointsPerLevel;
                levelsGained++;
            }

            if (IsMaxLevel)
            {
                data.experience = 0;
            }

            ExperienceGained?.Invoke(amount, source);
            ExperienceChanged?.Invoke(data.experience, ExperienceForNextLevel);
            if (levelsGained > 0)
            {
                // One event for the whole gain, however many levels it was worth: three popups
                // in a row for one kill would be noise.
                LevelChanged?.Invoke(startingLevel, data.level);
            }

            return levelsGained;
        }

        // ---------- attributes ----------

        public int GetBase(CharacterStat stat)
        {
            return _curve != null ? _curve.StartingStat(stat) : 0;
        }

        /// <summary>Everything above the starting value; what the player spent points on.</summary>
        public int GetAllocated(CharacterStat stat)
        {
            return Mathf.Max(0, GetChosen(stat) - GetBase(stat));
        }

        /// <summary>Base plus allocated, before anything worn is taken into account.</summary>
        public int GetChosen(CharacterStat stat)
        {
            return Data?.stats.Get(stat) ?? 0;
        }

        public bool CanAllocate(CharacterStat stat)
        {
            var data = Data;
            return data != null && _curve != null && data.statPoints > 0
                && Array.IndexOf(CharacterStats.Allocatable, stat) >= 0
                && data.stats.Get(stat) < _curve.StatCap;
        }

        /// <summary>
        /// Spends one point. Everything is checked before anything changes, so a refused
        /// allocation never costs a point.
        /// </summary>
        public bool AllocatePoint(CharacterStat stat)
        {
            if (!CanAllocate(stat))
            {
                return false;
            }

            var data = Data;
            var previous = data.stats.Get(stat);
            data.statPoints--;
            data.stats.Add(stat, 1);
            StatChanged?.Invoke(stat, previous, data.stats.Get(stat));
            return true;
        }

        // ---------- debug ----------

        /// <summary>Sets an attribute outright. Development only; it does not touch points.</summary>
        public void SetStat(CharacterStat stat, int value)
        {
            var data = Data;
            if (data == null || _curve == null)
            {
                return;
            }

            var previous = data.stats.Get(stat);
            data.stats.Set(stat, Mathf.Clamp(value, GetBase(stat), _curve.StatCap));
            StatChanged?.Invoke(stat, previous, data.stats.Get(stat));
        }

        public void GrantStatPoints(int amount)
        {
            var data = Data;
            if (data != null && amount > 0)
            {
                data.statPoints += amount;
            }
        }
    }
}
