using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every spell in the game, looked up by id, plus the checks that catch a broken one before a
    /// player ever casts it. Built the same way as the item and skill catalogues, for the same
    /// reason: one place that knows what exists.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Spell Database", fileName = "SpellDatabase")]
    public sealed class SpellDatabase : ScriptableObject
    {
        [SerializeField] private SpellDefinition[] _spells = System.Array.Empty<SpellDefinition>();
        private Dictionary<string, SpellDefinition> _byId;

        public IReadOnlyList<SpellDefinition> Spells => _spells;

        public bool TryGetSpell(string spellId, out SpellDefinition spell)
        {
            EnsureIndex();
            spell = null;
            return !string.IsNullOrEmpty(spellId) && _byId.TryGetValue(spellId, out spell);
        }

        public SpellDefinition GetSpell(string spellId)
        {
            return TryGetSpell(spellId, out var spell) ? spell : null;
        }

        public bool Contains(string spellId) => TryGetSpell(spellId, out _);

        public void Rebuild()
        {
            _byId = new Dictionary<string, SpellDefinition>();
            foreach (var spell in _spells)
            {
                if (spell == null || string.IsNullOrWhiteSpace(spell.SpellId))
                {
                    Debug.LogError($"Spell database '{name}' holds an entry without an id.", this);
                    continue;
                }

                if (!_byId.TryAdd(spell.SpellId, spell))
                {
                    Debug.LogError($"Spell id '{spell.SpellId}' is used by more than one spell; "
                        + "ids must be unique.", this);
                }
            }
        }

        /// <summary>Checks every spell and reports all the problems, not just the first.</summary>
        public bool ValidateSpells()
        {
            EnsureIndex();
            var isValid = true;
            foreach (var spell in _spells)
            {
                if (spell != null && !spell.Validate())
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        private void EnsureIndex()
        {
            if (_byId == null)
            {
                Rebuild();
            }
        }

        private void OnEnable() => _byId = null;

        private void OnValidate() => _byId = null;
    }
}
