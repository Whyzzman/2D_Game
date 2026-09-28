using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// One thing a character can say: a few pages shown one after another, with no choice of
    /// reply (the user's choice, 2026-09-28: talking as in Hollow Knight). What the character says
    /// follows what the player has done — a beaten boss, a topic already heard.
    /// </summary>
    [Serializable]
    public sealed class DialogueTopic
    {
        [Tooltip("Stable id, remembered in the save once heard.")]
        public string id = string.Empty;
        [Tooltip("Said only once this boss is beaten; empty for none.")]
        public string requiresBoss = string.Empty;
        [Tooltip("Said only while this boss still lives; empty for none.")]
        public string untilBoss = string.Empty;
        [Tooltip("Said once, then the next topic that applies takes its place.")]
        public bool once = true;
        [TextArea(2, 5)] public string[] pages = Array.Empty<string>();
        [Tooltip("One line for the journal «Люди» once this has been heard.")]
        public string journalNote = string.Empty;
    }

    /// <summary>
    /// A character of the world: a name, an epithet under it, what they say and whether they
    /// trade. Placed in the level by <see cref="Npc"/>, which also decides where and when they
    /// stand there.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Npc", fileName = "Npc")]
    public sealed class NpcDefinition : ScriptableObject
    {
        [SerializeField] private string _npcId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private string _epithet = string.Empty;
        [SerializeField] private DialogueTopic[] _topics = Array.Empty<DialogueTopic>();
        [Tooltip("Set for the merchant: after talking, «Торгувати / Піти».")]
        [SerializeField] private MerchantStock _stock;

        public string NpcId => _npcId;
        public string DisplayName => _displayName;
        public string Epithet => _epithet;
        public DialogueTopic[] Topics => _topics;
        public MerchantStock Stock => _stock;
        public bool IsMerchant => _stock != null;

        /// <summary>The save's record that this character has been met.</summary>
        public string MetId => "met:" + _npcId;

        /// <summary>The save's record that a topic has been heard.</summary>
        public string HeardId(DialogueTopic topic) => $"talk:{_npcId}:{topic.id}";

        /// <summary>
        /// What the character says now: the first topic, in authored order, whose boss conditions
        /// hold and that is not a once-only topic already heard. Null when nothing applies.
        /// </summary>
        public DialogueTopic Pick(GameProgress progress)
        {
            foreach (var topic in _topics)
            {
                if (topic == null || topic.pages == null || topic.pages.Length == 0)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(topic.requiresBoss)
                    && (progress == null || !progress.IsBossDefeated(topic.requiresBoss)))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(topic.untilBoss) && progress != null
                    && progress.IsBossDefeated(topic.untilBoss))
                {
                    continue;
                }

                if (topic.once && progress != null && progress.HasReward(HeardId(topic)))
                {
                    continue;
                }

                return topic;
            }

            return null;
        }

        /// <summary>The journal's line: the note of the last topic heard, in authored order.</summary>
        public string LatestNote(GameProgress progress)
        {
            var note = string.Empty;
            foreach (var topic in _topics)
            {
                if (topic != null && !string.IsNullOrEmpty(topic.journalNote)
                    && progress != null && progress.HasReward(HeardId(topic)))
                {
                    note = topic.journalNote;
                }
            }

            return note;
        }

#if UNITY_EDITOR
        public void Configure(string id, string displayName, string epithet, DialogueTopic[] topics,
            MerchantStock stock)
        {
            _npcId = id;
            _displayName = displayName;
            _epithet = epithet;
            _topics = topics ?? Array.Empty<DialogueTopic>();
            _stock = stock;
        }
#endif
    }
}
