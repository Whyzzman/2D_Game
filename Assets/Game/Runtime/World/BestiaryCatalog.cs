using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Who the bestiary lists and under which zone (item 4 of the big list, stage D): every kind
    /// of enemy in the order the zone meets them, and the bosses. Written by the generator; the
    /// page shows only the zones the hero has been in, and a kind not yet killed as «???».
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Bestiary Catalog", fileName = "BestiaryCatalog")]
    public sealed class BestiaryCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public EnemyDefinition Enemy;
            public string ZoneId = string.Empty;
        }

        [Serializable]
        public sealed class BossEntry
        {
            public string BossId = string.Empty;
            public string DisplayName = string.Empty;
            public string ZoneId = string.Empty;
        }

        [SerializeField] private Entry[] _enemies = Array.Empty<Entry>();
        [SerializeField] private BossEntry[] _bosses = Array.Empty<BossEntry>();

        public IReadOnlyList<Entry> Enemies => _enemies;
        public IReadOnlyList<BossEntry> Bosses => _bosses;
    }
}
