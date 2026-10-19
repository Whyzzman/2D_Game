using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What a good does once bought. Stored by number: new kinds only at the end.</summary>
    public enum GoodKind
    {
        /// <summary>Charts a whole zone on the map (<c>map:&lt;zone&gt;</c>).</summary>
        ZoneMap,
        /// <summary>The player's own marks on the map, once per game.</summary>
        Quill,
        /// <summary>Marks a hidden place on the map (<c>rumour:&lt;area&gt;</c>) and says why.</summary>
        Rumour,
        /// <summary>One lockpick into the bag; each opens one locked chest.</summary>
        Lockpick,
        /// <summary>One more healing flask.</summary>
        FlaskShard,
        /// <summary>An item (a relic) into the bag.</summary>
        Relic,
        /// <summary>The smith's tempering (item 6c): its level is the good's target, bought in turn.</summary>
        Temper
    }

    /// <summary>One thing on the merchant's cart. Each is sold once.</summary>
    [Serializable]
    public sealed class MerchantGood
    {
        [Tooltip("Stable id; the save remembers «bought:<id>».")]
        public string id = string.Empty;
        public GoodKind kind;
        public string displayName = string.Empty;
        [Tooltip("Small line under the name in the list.")]
        public string subtitle = string.Empty;
        [TextArea(2, 4)] public string description = string.Empty;
        [Tooltip("What it does, in plain words, under the description.")]
        public string effect = string.Empty;
        [Min(0)] public int price;
        [Tooltip("The zone map charts, or the zone a rumour is about.")]
        public string zoneId = string.Empty;
        [Tooltip("A rumour's area, or the item a relic or lockpick puts in the bag.")]
        public string target = string.Empty;
        [Tooltip("A rumour's words, shown once bought.")]
        [TextArea(2, 4)] public string quote = string.Empty;
        [Tooltip("On the cart only once this boss is beaten (the forest's goods); empty for from the start.")]
        public string requiresBoss = string.Empty;
        [Tooltip("Iron scrap taken from the bag besides the gold (the smith's tempering).")]
        [Min(0)] public int iron;
    }

    /// <summary>
    /// The merchant's cart (item 70, 2026-09-28). Prices were set against what a zone pays out
    /// on one pass — the map and the flask shard cost about half, everything together about 1.4
    /// times — so buying means choosing but never grinding (the user's choice).
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Merchant Stock", fileName = "MerchantStock")]
    public sealed class MerchantStock : ScriptableObject
    {
        public const string LockpickItemId = "lockpick";
        public const string QuillRewardId = "map-quill";
        public const string FlaskShardPrefix = "flask-shard:";

        [SerializeField] private MerchantGood[] _goods = Array.Empty<MerchantGood>();

        public MerchantGood[] Goods => _goods;

        public static string BoughtId(MerchantGood good) => "bought:" + good.id;
        public static string MapRewardId(string zoneId) => "map:" + zoneId;
        public static string RumourRewardId(string areaId) => "rumour:" + areaId;

#if UNITY_EDITOR
        public void Configure(MerchantGood[] goods) => _goods = goods ?? Array.Empty<MerchantGood>();
#endif
    }
}
