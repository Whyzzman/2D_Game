using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Enemy animation 2.0 as data (visual item 4, 2026-09-28): everyone with something loose has
    /// a hanging point on every frame, and it stays on the body; the guard has none; a windup
    /// flares the eyes before the blow.
    /// </summary>
    public sealed class EnemyLifeDataTests
    {
        private const string Folder = "Assets/Game/Data/Animation";

        [TestCase("ZombieAnimations", 0.7f, 1.8f)]
        [TestCase("HoundAnimations", 0.9f, 1.4f)]
        [TestCase("ArcherAnimations", 0.7f, 2f)]
        [TestCase("ShamanAnimations", 0.7f, 1.8f)]
        [TestCase("SpearmanAnimations", 0.7f, 2f)]
        public void EveryFrameNamesWhereTheLoosePieceHangs(string setName, float reach, float height)
        {
            var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>($"{Folder}/{setName}.asset");
            Assert.That(set, Is.Not.Null, setName);
            foreach (var state in set.RequiredStates())
            {
                var clip = set.Clip(state);
                for (var i = 0; i < clip.FrameCount; i++)
                {
                    Assert.That(clip.TryGetAnchor(i, out var anchor), Is.True, $"{setName} {state} frame {i}");
                    Assert.That(Mathf.Abs(anchor.x), Is.LessThan(reach), $"{setName} {state} frame {i}: off the body.");
                    // A collapsing robe's hem may sink a few pixels into the floor on the last frame.
                    Assert.That(anchor.y, Is.InRange(-0.15f, height), $"{setName} {state} frame {i}");
                }
            }
        }

        [TestCase("GuardianAnimations")]
        [TestCase("WardenAnimations")]
        public void TheBossesCapesHangFromEveryFrame(string setName)
        {
            var set = AssetDatabase.LoadAssetAtPath<BossAnimationSet>($"{Folder}/{setName}.asset");
            foreach (var state in set.RequiredStates())
            {
                var clip = set.Clip(state);
                for (var i = 0; i < clip.FrameCount; i++)
                {
                    Assert.That(clip.TryGetAnchor(i, out var anchor), Is.True, $"{setName} {state} frame {i}");
                    Assert.That(Mathf.Abs(anchor.x), Is.LessThan(1.6f), $"{setName} {state} frame {i}");
                }
            }
        }

        [Test]
        public void TheGuardHasNothingLoose()
        {
            var set = AssetDatabase.LoadAssetAtPath<EnemyAnimationSet>($"{Folder}/ShieldGuardAnimations.asset");
            Assert.That(set.Clip(EnemyAnimationState.Idle).TryGetAnchor(0, out _), Is.False);
        }

        [TestCase("ZombieSoldier/AttackLight/enemy_zombie_attacklight", 18, 0.3f)]
        [TestCase("ShieldGuard/AttackHeavy/enemy_shieldguard_attackheavy", 26, 0.3f)]
        [TestCase("CorruptedHound/AttackLight/enemy_hound_attacklight", 16, 0.34f)]
        public void TheEyesFlareJustBeforeTheBlow(string clip, int frames, float peak)
        {
            var calm = Bright($"Assets/Game/Art/Characters/Enemies/{clip}_00.png");
            var index = Mathf.RoundToInt(peak * (frames - 1));
            var flared = Bright($"Assets/Game/Art/Characters/Enemies/{clip}_{index:00}.png");
            Assert.That(flared, Is.GreaterThan(calm * 2 + 2), "The windup's last beat shows in the eyes.");
        }

        /// <summary>Bright pixels by luminance: the flare's core and glint, whatever the eye's colour.</summary>
        private static int Bright(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            return texture.GetPixels32().Count(p => p.a > 200 && 0.3f * p.r + 0.59f * p.g + 0.11f * p.b >= 200f);
        }
    }
}
