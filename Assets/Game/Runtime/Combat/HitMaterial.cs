using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>What a struck body is made of: it colours the spark and tunes the sound of a landed blow.</summary>
    public enum HitMaterial
    {
        Flesh,
        Bone,
        Armor,
        Wood,
        Stone
    }

    /// <summary>
    /// Which material each enemy and boss is (GAME_FEEL_IDEAS.md, item 7): the player hears and sees
    /// whether the blade met a shield, a skull or a body. One table, by the enemy definition's asset
    /// name or the boss id; anything not named here is flesh. Presentation only — no damage depends on it.
    /// </summary>
    public static class HitMaterials
    {
        private static readonly Dictionary<string, HitMaterial> ByName = new Dictionary<string, HitMaterial>
        {
            { "ShieldGuard", HitMaterial.Armor },
            { "BoneArcher", HitMaterial.Bone },
            { "RootShaman", HitMaterial.Wood },
            { "RoyalSpearman", HitMaterial.Armor },
            { "Scarecrow", HitMaterial.Wood },
            { "RoyalCrossbowman", HitMaterial.Armor },
            { "StoneGargoyle", HitMaterial.Stone },
            { "guardian-of-the-tomb", HitMaterial.Armor },
            { "hollow-warden", HitMaterial.Wood },
            { "ash-commander", HitMaterial.Armor },
            { "ashen-king", HitMaterial.Armor },
            { "plague-stag", HitMaterial.Bone }
        };

        public static HitMaterial Of(CombatActor victim)
        {
            if (victim == null)
            {
                return HitMaterial.Flesh;
            }

            var enemy = victim.GetComponent<EnemyController>();
            if (enemy != null && enemy.Definition != null
                && ByName.TryGetValue(enemy.Definition.name, out var byDefinition))
            {
                return byDefinition;
            }

            var boss = victim.GetComponent<BossController>();
            if (boss != null && ByName.TryGetValue(boss.BossId, out var byBoss))
            {
                return byBoss;
            }

            return HitMaterial.Flesh;
        }

        /// <summary>The spark's colour: dark red splashes, pale chips, yellow sparks, brown splinters, grey grit.</summary>
        public static Color Tint(HitMaterial material)
        {
            switch (material)
            {
                case HitMaterial.Bone: return new Color(0.96f, 0.95f, 0.86f);
                case HitMaterial.Armor: return new Color(1f, 0.86f, 0.42f);
                case HitMaterial.Wood: return new Color(0.7f, 0.5f, 0.3f);
                case HitMaterial.Stone: return new Color(0.72f, 0.74f, 0.8f);
                default: return new Color(0.82f, 0.32f, 0.3f);
            }
        }

        /// <summary>The blow's pitch: armour rings high, wood and flesh thud low.</summary>
        public static float Pitch(HitMaterial material)
        {
            switch (material)
            {
                case HitMaterial.Bone: return 1.12f;
                case HitMaterial.Armor: return 1.3f;
                case HitMaterial.Wood: return 0.82f;
                case HitMaterial.Stone: return 1.04f;
                default: return 0.92f;
            }
        }
    }
}
