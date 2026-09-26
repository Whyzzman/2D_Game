using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every skill in the game, looked up by id, plus the checks that catch a badly built tree
    /// before a player ever sees it: duplicate ids, prerequisites that point at nothing, and
    /// cycles that would make a node impossible to reach.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Skill Database", fileName = "SkillDatabase")]
    public sealed class SkillDatabase : ScriptableObject
    {
        [SerializeField] private SkillDefinition[] _skills = System.Array.Empty<SkillDefinition>();
        private Dictionary<string, SkillDefinition> _byId;

        public IReadOnlyList<SkillDefinition> Skills => _skills;

        public bool TryGetSkill(string skillId, out SkillDefinition skill)
        {
            EnsureIndex();
            skill = null;
            return !string.IsNullOrEmpty(skillId) && _byId.TryGetValue(skillId, out skill);
        }

        public SkillDefinition GetSkill(string skillId)
        {
            return TryGetSkill(skillId, out var skill) ? skill : null;
        }

        public bool Contains(string skillId)
        {
            return TryGetSkill(skillId, out _);
        }

        /// <summary>The nodes of one tree, ordered the way the screen lays them out.</summary>
        public List<SkillDefinition> GetSkillsByBranch(SkillBranch branch)
        {
            var matches = new List<SkillDefinition>();
            foreach (var skill in _skills)
            {
                if (skill != null && skill.Branch == branch)
                {
                    matches.Add(skill);
                }
            }

            matches.Sort((left, right) =>
            {
                var column = left.Position.x.CompareTo(right.Position.x);
                return column != 0 ? column : left.Position.y.CompareTo(right.Position.y);
            });
            return matches;
        }

        public void Rebuild()
        {
            _byId = new Dictionary<string, SkillDefinition>();
            foreach (var skill in _skills)
            {
                if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
                {
                    Debug.LogError($"Skill database '{name}' holds an entry without an id.", this);
                    continue;
                }

                if (!_byId.TryAdd(skill.SkillId, skill))
                {
                    // Two skills answering to the same id would make a save ambiguous.
                    Debug.LogError($"Skill id '{skill.SkillId}' is used by more than one skill; "
                        + "ids must be unique.", this);
                }
            }
        }

        /// <summary>
        /// Checks the whole tree and reports every problem it finds rather than the first one.
        /// Returns true when the tree is sound. Meant for authoring and for a test.
        /// </summary>
        public bool ValidateTree()
        {
            EnsureIndex();
            var isValid = true;
            foreach (var skill in _skills)
            {
                if (skill == null)
                {
                    continue;
                }

                if (skill.Cost < 1 || skill.MaxRank < 1)
                {
                    Debug.LogError($"Skill '{skill.SkillId}' has a cost or rank below one.", skill);
                    isValid = false;
                }

                // Unity always builds the serialized block, so "has no data" shows up as data
                // that could not work: no swing to perform, or no cooldown to pay for it.
                if (skill.IsActive
                    && (skill.Active == null || skill.Active.Attack == null
                        || skill.Active.Cooldown <= 0f))
                {
                    Debug.LogError($"Active skill '{skill.SkillId}' has no usable skill data: "
                        + "an active skill needs a swing and a cooldown.", skill);
                    isValid = false;
                }

                foreach (var prerequisite in skill.Prerequisites)
                {
                    if (!Contains(prerequisite))
                    {
                        Debug.LogError($"Skill '{skill.SkillId}' requires '{prerequisite}', "
                            + "which is not in the database.", skill);
                        isValid = false;
                    }
                }

                if (HasCycle(skill.SkillId, new HashSet<string>()))
                {
                    Debug.LogError($"Skill '{skill.SkillId}' is part of a prerequisite cycle; "
                        + "it could never be bought.", skill);
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>Walks prerequisites depth first; a node seen twice on one path is a cycle.</summary>
        private bool HasCycle(string skillId, HashSet<string> visiting)
        {
            if (!visiting.Add(skillId))
            {
                return true;
            }

            if (TryGetSkill(skillId, out var skill))
            {
                foreach (var prerequisite in skill.Prerequisites)
                {
                    if (Contains(prerequisite) && HasCycle(prerequisite, visiting))
                    {
                        return true;
                    }
                }
            }

            visiting.Remove(skillId);
            return false;
        }

        private void EnsureIndex()
        {
            if (_byId == null)
            {
                Rebuild();
            }
        }

        private void OnEnable()
        {
            _byId = null;
        }

        private void OnValidate()
        {
            _byId = null;
        }
    }
}
