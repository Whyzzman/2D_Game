using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Which spells the player knows and which three are to hand. Kept apart from
    /// <see cref="PlayerSkills"/> on purpose: a skill unlocks a spell, but owning the spell is
    /// its own record, so a later scroll or reward can grant one without a skill existing for it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpellbook : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerSkills _skills;
        [SerializeField] private SpellDatabase _database;
        private readonly HashSet<string> _known = new HashSet<string>();
        private readonly string[] _slots = new string[SlotCount];

        public event Action SpellsChanged;
        public event Action<SpellDefinition> SpellLearned;
        public event Action<int, SpellDefinition> SlotChanged;

        public SpellDatabase Database => _database;
        public IReadOnlyCollection<string> KnownSpells => _known;

        public string SlotSpellId(int slot) =>
            slot >= 0 && slot < SlotCount ? _slots[slot] ?? string.Empty : string.Empty;

        public SpellDefinition SlotSpell(int slot)
        {
            var id = SlotSpellId(slot);
            return _database != null && !string.IsNullOrEmpty(id) ? _database.GetSpell(id) : null;
        }

        public bool Knows(string spellId) =>
            !string.IsNullOrEmpty(spellId) && _known.Contains(spellId);

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_skills == null) { _skills = FindAnyObjectByType<PlayerSkills>(); }
        }

        private void OnEnable()
        {
            if (_progress != null) { _progress.Loaded += ReloadFromSave; }
            // A skill that grants a spell hands it over the moment it is learned.
            if (_skills != null) { _skills.SkillUnlocked += OnSkillUnlocked; }
        }

        private void OnDisable()
        {
            if (_progress != null) { _progress.Loaded -= ReloadFromSave; }
            if (_skills != null) { _skills.SkillUnlocked -= OnSkillUnlocked; }
        }

        private void Start()
        {
            ReloadFromSave();
        }

        /// <summary>
        /// Reads the saved spellbook and reconciles it with the skills that grant spells. A file
        /// that has a granting skill but lost the spell — an older build, a hand edit — gets the
        /// spell back rather than leaving the player with a skill that does nothing.
        /// </summary>
        public void ReloadFromSave()
        {
            _known.Clear();
            if (_progress == null || _database == null)
            {
                Recalculate();
                return;
            }

            foreach (var spellId in _progress.Data.player.unlockedSpells)
            {
                if (_database.Contains(spellId))
                {
                    _known.Add(spellId);
                }
                else
                {
                    Debug.LogWarning($"Saved spell '{spellId}' is not in this build; ignored.");
                }
            }

            ReconcileWithSkills();
            SetSlotInternal(0, _progress.Data.player.spellSlot1);
            SetSlotInternal(1, _progress.Data.player.spellSlot2);
            SetSlotInternal(2, _progress.Data.player.spellSlot3);
            WriteBack();
            Recalculate();
        }

        /// <summary>Grants every spell the learned skills are supposed to have given.</summary>
        private void ReconcileWithSkills()
        {
            if (_skills?.Database == null)
            {
                return;
            }

            foreach (var skill in _skills.Database.Skills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.GrantedSpellId)
                    && _skills.HasSkill(skill.SkillId))
                {
                    _known.Add(skill.GrantedSpellId);
                }
            }
        }

        /// <summary>Learns a spell. Learning one twice does nothing, by design.</summary>
        public bool Learn(string spellId)
        {
            if (_database == null || !_database.TryGetSpell(spellId, out var spell)
                || !_known.Add(spellId))
            {
                return false;
            }

            WriteBack();
            Recalculate();
            SpellLearned?.Invoke(spell);
            _progress?.Save();
            return true;
        }

        /// <summary>
        /// Puts a known spell in a slot. The same spell cannot sit in two slots, and an empty
        /// string clears one.
        /// </summary>
        public bool AssignSlot(int slot, string spellId)
        {
            if (slot < 0 || slot >= SlotCount)
            {
                return false;
            }

            if (string.IsNullOrEmpty(spellId))
            {
                SetSlotInternal(slot, string.Empty);
                WriteBack();
                return true;
            }

            if (!Knows(spellId))
            {
                return false;
            }

            for (var other = 0; other < SlotCount; other++)
            {
                if (other != slot && _slots[other] == spellId)
                {
                    SetSlotInternal(other, string.Empty);
                }
            }

            SetSlotInternal(slot, spellId);
            WriteBack();
            return true;
        }

        private void SetSlotInternal(int slot, string spellId)
        {
            spellId ??= string.Empty;
            // A spell no longer known, or no longer in the build, leaves the bar quietly.
            if (!string.IsNullOrEmpty(spellId) && !Knows(spellId))
            {
                spellId = string.Empty;
            }

            if (_slots[slot] == spellId)
            {
                return;
            }

            _slots[slot] = spellId;
            SlotChanged?.Invoke(slot, SlotSpell(slot));
        }

        private void OnSkillUnlocked(SkillDefinition skill, int rank)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.GrantedSpellId))
            {
                Learn(skill.GrantedSpellId);
            }
        }

        private void WriteBack()
        {
            if (_progress == null)
            {
                return;
            }

            var list = _progress.Data.player.unlockedSpells;
            list.Clear();
            list.AddRange(_known);
            _progress.Data.player.spellSlot1 = _slots[0] ?? string.Empty;
            _progress.Data.player.spellSlot2 = _slots[1] ?? string.Empty;
            _progress.Data.player.spellSlot3 = _slots[2] ?? string.Empty;
        }

        private void Recalculate()
        {
            SpellsChanged?.Invoke();
        }
    }
}
