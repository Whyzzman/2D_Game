using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Long-term character progression. Combat resources are deliberately absent: a load always
    /// starts the player rested, so a quit can never bank a half-empty stamina bar.
    /// </summary>
    [Serializable]
    public sealed class PlayerProgressionData
    {
        public int level = 1;
        public int experience;
        public int currency;
        /// <summary>Levels earned but not yet spent; they survive a quit like anything else.</summary>
        public int statPoints;
        /// <summary>
        /// The four numbers the player chose. Health, stamina, defence and weapon damage are all
        /// worked out from these and from equipment, never stored, so they can never drift apart.
        /// </summary>
        public CharacterStatBlock stats = new CharacterStatBlock();
        /// <summary>
        /// Skills owned, as "id" or "id:rank". Unspent skill points are never stored: they are
        /// worked out from the level and what has been bought, so a load can never hand out the
        /// same point twice.
        /// </summary>
        public List<string> unlockedSkills = new List<string>();
        /// <summary>Skill points from anywhere other than levelling, such as a boss reward.</summary>
        public int bonusSkillPoints;
        public string activeSkillSlot1 = string.Empty;
        public string activeSkillSlot2 = string.Empty;
        /// <summary>
        /// Spells the player knows. Kept apart from the skills that teach them so a later scroll
        /// or reward can grant one without a skill existing for it. Current mana is never here:
        /// a load always starts the player full.
        /// </summary>
        public List<string> unlockedSpells = new List<string>();
        public string spellSlot1 = string.Empty;
        public string spellSlot2 = string.Empty;
        public string spellSlot3 = string.Empty;
        /// <summary>
        /// Kept only for files written before attributes existed, where they were the real
        /// maxima. Once <see cref="stats"/> is set the curve decides, and these stop being read.
        /// </summary>
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        /// <summary>Traversal and combat abilities the player owns; dash is granted by default.</summary>
        public List<string> unlockedAbilities = new List<string>();

        public static PlayerProgressionData CreateDefault()
        {
            return new PlayerProgressionData
            {
                unlockedAbilities = new List<string> { AbilityIds.Dash }
            };
        }

        /// <summary>Guards against a hand-edited or half-written file producing broken values.</summary>
        public void Validate()
        {
            unlockedAbilities ??= new List<string>();
            unlockedSkills ??= new List<string>();
            unlockedSpells ??= new List<string>();
            activeSkillSlot1 ??= string.Empty;
            activeSkillSlot2 ??= string.Empty;
            spellSlot1 ??= string.Empty;
            spellSlot2 ??= string.Empty;
            spellSlot3 ??= string.Empty;
            bonusSkillPoints = Mathf.Max(0, bonusSkillPoints);
            Deduplicate(unlockedSkills);
            Deduplicate(unlockedSpells);
            stats ??= new CharacterStatBlock();
            stats.Validate();
            level = Mathf.Max(1, level);
            experience = Mathf.Max(0, experience);
            statPoints = Mathf.Max(0, statPoints);
            currency = Mathf.Max(0, currency);
            maxHealth = Mathf.Max(1f, maxHealth);
            maxStamina = Mathf.Max(1f, maxStamina);
            Deduplicate(unlockedAbilities);
        }

        internal static void Deduplicate(List<string> ids)
        {
            var seen = new HashSet<string>();
            for (var i = ids.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(ids[i]) || !seen.Add(ids[i]))
                {
                    ids.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>One mark made with the quill: which room, which of the five colours.</summary>
    [Serializable]
    public sealed class MapMark
    {
        public const int Colors = 5;
        public string areaId = string.Empty;
        public int color;
    }

    /// <summary>Ability identifiers used by progression; kept in one place, not spelled inline.</summary>
    public static class AbilityIds
    {
        public const string Dash = "dash";
        public const string DoubleJump = "double-jump";
        public const string AirDash = "air-dash";
        public const string WallRun = "wall-run";
        public const string ShadowDash = "shadow-dash";
        public const string Grapple = "grapple";
    }

    /// <summary>
    /// Everything that survives closing the game. Only durable progress lives here: where to
    /// respawn, what has been beaten, what has been earned. Nothing about the current fight.
    /// </summary>
    [Serializable]
    public sealed class GameProgressData
    {
        public const int CurrentVersion = 11;

        /// <summary>The first layout that knew about the map, shortcuts and zone title cards.</summary>
        public const int WorldMapVersion = 10;

        public int version = CurrentVersion;
        public string checkpointId = string.Empty;
        /// <summary>
        /// Which region the checkpoint belongs to. Written from version nine on; an older file has
        /// none, and none is correct for it — every altar that existed then was in the Tomb, and
        /// <see cref="WorldGraph"/> resolves it from the checkpoint id on load.
        /// </summary>
        public string zoneId = string.Empty;
        public Vector2 respawnPosition;
        /// <summary>
        /// Every altar ever lit. This is also the fast-travel list — a destination is an altar
        /// lit here whose database entry allows travel — so there is no second list to drift.
        /// </summary>
        public List<string> activatedCheckpoints = new List<string>();
        /// <summary>Map areas the player has stood in. Written from version ten on.</summary>
        public List<string> discoveredAreas = new List<string>();
        /// <summary>Shortcuts opened for good: a lowered bridge, a started lift.</summary>
        public List<string> unlockedShortcuts = new List<string>();
        /// <summary>Zones whose title card has been shown, so it plays once rather than per visit.</summary>
        public List<string> seenZoneIntros = new List<string>();
        public List<string> defeatedBosses = new List<string>();
        /// <summary>Progression pickups such as the Guardian Core; never granted twice.</summary>
        public List<string> acquiredRewards = new List<string>();
        public PlayerProgressionData player = PlayerProgressionData.CreateDefault();
        /// <summary>What the player owns, as ids and counts; never engine references.</summary>
        public ItemCollection inventory = new ItemCollection();
        /// <summary>World pickups already taken, so a unique item is never lying there twice.</summary>
        public List<string> collectedPickups = new List<string>();
        /// <summary>What is worn, as ids. Validated against the inventory on every load.</summary>
        public EquipmentLoadout equipment = new EquipmentLoadout();
        /// <summary>
        /// Gold left where the player last died (the user's choice, 2026-09-28): all of it, waiting
        /// on the last solid ground stood on until it is touched. A second death before that loses
        /// it for good. Written from version eleven on; an older file has none, which is correct.
        /// </summary>
        public int lostCurrency;
        public Vector2 lostCurrencyPosition;
        /// <summary>
        /// The player's own marks on the map, made with the merchant's quill (item 70): a room and
        /// one of five colours. An older file has none, which is correct.
        /// </summary>
        public List<MapMark> mapMarks = new List<MapMark>();
        /// <summary>
        /// The bestiary (item 4): kills by enemy kind, and the kinds its page has already shown.
        /// An older file has none, which is correct.
        /// </summary>
        public List<KillCount> kills = new List<KillCount>();
        public List<string> bestiarySeen = new List<string>();
        /// <summary>
        /// Item 5: deaths over the whole game, for the summary, and whether the King has fallen and
        /// the ending been seen. An older file has neither, which is correct.
        /// </summary>
        public int deaths;
        public bool completed;
        public float totalPlayTimeSeconds;
        public string createdAtUtc = string.Empty;
        public string lastSavedAtUtc = string.Empty;

        public static GameProgressData CreateNew()
        {
            var now = DateTime.UtcNow.ToString("o");
            var data = new GameProgressData
            {
                version = CurrentVersion,
                player = PlayerProgressionData.CreateDefault(),
                createdAtUtc = now,
                lastSavedAtUtc = now
            };

            // A new run starts armed: the starter sword is owned and already in hand, because
            // there is no unarmed moveset to fall back on.
            data.inventory.Add(ItemIds.StarterWeapon, 1, true);
            data.equipment.Set(EquipmentSlot.MainWeapon, ItemIds.StarterWeapon);
            return data;
        }

        public bool IsBossDefeated(string bossId)
        {
            return !string.IsNullOrEmpty(bossId) && defeatedBosses.Contains(bossId);
        }

        public void MarkBossDefeated(string bossId)
        {
            if (!string.IsNullOrEmpty(bossId) && !defeatedBosses.Contains(bossId))
            {
                defeatedBosses.Add(bossId);
            }
        }

        public bool HasReward(string rewardId)
        {
            return !string.IsNullOrEmpty(rewardId) && acquiredRewards.Contains(rewardId);
        }

        public void GrantReward(string rewardId)
        {
            if (!string.IsNullOrEmpty(rewardId) && !acquiredRewards.Contains(rewardId))
            {
                acquiredRewards.Add(rewardId);
            }
        }

        public bool IsPickupCollected(string pickupId)
        {
            return !string.IsNullOrEmpty(pickupId) && collectedPickups.Contains(pickupId);
        }

        public void MarkPickupCollected(string pickupId)
        {
            if (!string.IsNullOrEmpty(pickupId) && !collectedPickups.Contains(pickupId))
            {
                collectedPickups.Add(pickupId);
            }
        }

        public bool IsCheckpointActivated(string checkpointId)
        {
            return !string.IsNullOrEmpty(checkpointId)
                && activatedCheckpoints.Contains(checkpointId);
        }

        public void MarkCheckpointActivated(string checkpointId)
        {
            if (!string.IsNullOrEmpty(checkpointId) && !activatedCheckpoints.Contains(checkpointId))
            {
                activatedCheckpoints.Add(checkpointId);
            }
        }

        public bool IsAreaDiscovered(string areaId)
        {
            return !string.IsNullOrEmpty(areaId) && discoveredAreas.Contains(areaId);
        }

        public void MarkAreaDiscovered(string areaId)
        {
            if (!string.IsNullOrEmpty(areaId) && !discoveredAreas.Contains(areaId))
            {
                discoveredAreas.Add(areaId);
            }
        }

        public bool IsShortcutUnlocked(string shortcutId)
        {
            return !string.IsNullOrEmpty(shortcutId) && unlockedShortcuts.Contains(shortcutId);
        }

        public void UnlockShortcut(string shortcutId)
        {
            if (!string.IsNullOrEmpty(shortcutId) && !unlockedShortcuts.Contains(shortcutId))
            {
                unlockedShortcuts.Add(shortcutId);
            }
        }

        public bool HasSeenZoneIntro(string zoneId)
        {
            return !string.IsNullOrEmpty(zoneId) && seenZoneIntros.Contains(zoneId);
        }

        public void MarkZoneIntroSeen(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && !seenZoneIntros.Contains(zoneId))
            {
                seenZoneIntros.Add(zoneId);
            }
        }

        /// <summary>
        /// Brings a file from any older layout up to the current one and repairs obvious damage.
        /// A field added by a newer build simply defaults; that is not an error.
        /// </summary>
        public void Migrate()
        {
            activatedCheckpoints ??= new List<string>();
            defeatedBosses ??= new List<string>();
            acquiredRewards ??= new List<string>();
            collectedPickups ??= new List<string>();
            // A version nine file has none of the three; the map part is filled in by
            // WorldDiscoveryMigration, which needs the world graph this plain object cannot see.
            discoveredAreas ??= new List<string>();
            unlockedShortcuts ??= new List<string>();
            seenZoneIntros ??= new List<string>();
            mapMarks ??= new List<MapMark>();
            kills ??= new List<KillCount>();
            kills.RemoveAll(k => k == null || string.IsNullOrEmpty(k.id));
            bestiarySeen ??= new List<string>();
            mapMarks.RemoveAll(mark => mark == null || string.IsNullOrEmpty(mark.areaId)
                || mark.color < 0 || mark.color >= MapMark.Colors);
            player ??= PlayerProgressionData.CreateDefault();
            // A version two file has no inventory; an empty one is the right default.
            inventory ??= new ItemCollection();
            // A version three file has no loadout; it is filled in below once the inventory is
            // known, so a save from before equipment existed comes back armed rather than empty.
            equipment ??= new EquipmentLoadout();
            equipment.Normalize();

            PlayerProgressionData.Deduplicate(activatedCheckpoints);
            PlayerProgressionData.Deduplicate(defeatedBosses);
            PlayerProgressionData.Deduplicate(acquiredRewards);
            PlayerProgressionData.Deduplicate(collectedPickups);
            PlayerProgressionData.Deduplicate(discoveredAreas);
            PlayerProgressionData.Deduplicate(unlockedShortcuts);
            PlayerProgressionData.Deduplicate(seenZoneIntros);
            inventory.Normalize();
            player.Validate();
            if (version < 8 && player.stats.Total > 0)
            {
                player.stats.will = 5;
            }

            if (player.unlockedAbilities.Count == 0)
            {
                // Version 1 had no ability list; dash was always available, so keep it that way.
                player.unlockedAbilities.Add(AbilityIds.Dash);
            }

            MigrateStarterWeapon();
            totalPlayTimeSeconds = Mathf.Max(0f, totalPlayTimeSeconds);
            lostCurrency = Mathf.Max(0, lostCurrency);
            deaths = Mathf.Max(0, deaths);
            checkpointId ??= string.Empty;
            // Left empty on purpose for a pre-version-nine file. Guessing "the Tomb" here would be
            // right today and wrong the moment a zone is renamed; the registry knows, this does not.
            zoneId ??= string.Empty;
            version = CurrentVersion;
        }

        /// <summary>
        /// A file written before equipment existed has a main hand but nothing in it. The starter
        /// sword is granted only when the player owns no weapon-shaped item at all, and only once:
        /// the inventory's own uniqueness rule makes a second grant impossible, so a save loaded
        /// twice never ends up with two swords.
        /// </summary>
        private void MigrateStarterWeapon()
        {
            if (!equipment.IsEmpty(EquipmentSlot.MainWeapon))
            {
                return;
            }

            if (inventory.HasItem(ItemIds.StarterWeapon))
            {
                equipment.Set(EquipmentSlot.MainWeapon, ItemIds.StarterWeapon);
                return;
            }

            // Which ids are weapons is a question for the catalogue, which this plain data object
            // cannot see; PlayerEquipment finishes the job on load and prefers an owned weapon.
            // Granting the starter here would duplicate one the player already has under a
            // different id, so the only safe move is to leave the slot for it to resolve.
            if (inventory.DistinctItemCount == 0)
            {
                inventory.Add(ItemIds.StarterWeapon, 1, true);
                equipment.Set(EquipmentSlot.MainWeapon, ItemIds.StarterWeapon);
            }
        }
    }
}
