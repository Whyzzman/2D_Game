using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Hero animation 2.0 (2026-09-28) as data: a swing's fastest frame is a smear, a long drop
    /// lands on one knee and stands up again, the stance breathes slowly and has a second flourish.
    /// </summary>
    public sealed class HeroMotionDataTests
    {
        private const string Player = "Assets/Game/Art/Characters/Player";
        private PlayerAnimationSet _set;

        [SetUp]
        public void SetUp()
        {
            _set = AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>("Assets/Game/Data/Animation/PlayerAnimations.asset");
            Assert.That(_set, Is.Not.Null);
        }

        [TestCase("Attack1/player_attack1")]
        [TestCase("Attack3/player_attack3")]
        [TestCase("AttackDown/player_attackdown")]
        [TestCase("Greatsword/Attack1/player_attack1")]
        public void TheFastestFrameOfASwingIsASmear(string frames)
        {
            var counts = Directory.GetFiles(Path.GetDirectoryName($"{Player}/{frames}"), "*.png")
                .OrderBy(p => p)
                .Select(p => Bright(Load(p)))
                .ToArray();
            // The blade alone is a thin line; smeared over its arc it covers several times as much.
            Assert.That(counts.Max(), Is.GreaterThan(counts[0] * 2.2f),
                $"No frame of {frames} draws the arc: {string.Join(", ", counts)}.");
        }

        [Test]
        public void ALongDropKneelsAndEndsStanding()
        {
            var clip = _set.Clip(PlayerAnimationState.HardLand, null);
            Assert.That(clip, Is.Not.SameAs(_set.Clip(PlayerAnimationState.Land, null)));
            Assert.That(clip.IsLooping, Is.False);
            Assert.That(clip.Duration, Is.InRange(0.3f, 0.45f), "Long enough to read, short enough not to drag.");

            var standing = Height(Frame("Idle/player_idle_00"));
            Assert.That(Height(Frame("HardLand/player_hardland_00")), Is.LessThan(standing * 0.8f),
                "The first frame is down on one knee.");
            Assert.That(Height(Frame("HardLand/player_hardland_04")), Is.EqualTo(standing).Within(standing * 0.05f),
                "The last frame is the stance, so Idle follows without a jump.");
        }

        [Test]
        public void TheStanceBreathesSlowly()
        {
            var idle = _set.Clip(PlayerAnimationState.Idle, null);
            Assert.That(idle.IsLooping, Is.True);
            Assert.That(idle.Duration, Is.GreaterThanOrEqualTo(1.6f), "A breath, not a pant.");
            var heights = Enumerable.Range(0, idle.FrameCount)
                .Select(i => Height(Frame($"Idle/player_idle_{i:00}")))
                .ToArray();
            Assert.That(heights.Max() - heights.Min(), Is.GreaterThanOrEqualTo(2),
                "The chest rises: the figure's top moves.");
        }

        [Test]
        public void TheGlanceIsASecondFlourish()
        {
            var glance = _set.Clip(PlayerAnimationState.IdleGlance, null);
            Assert.That(glance, Is.Not.SameAs(_set.Clip(PlayerAnimationState.Idle, null)));
            Assert.That(glance, Is.Not.SameAs(_set.Clip(PlayerAnimationState.IdleVariant, null)));
            Assert.That(glance.IsLooping, Is.False);
            Assert.That(glance.FrameCount, Is.EqualTo(10));
        }

        // ---------- helpers ----------

        private static Texture2D Frame(string name) => Load($"{Player}/{name}.png");

        /// <summary>Pale, opaque pixels: the blade, its edge and the smear.</summary>
        private static int Bright(Texture2D texture) =>
            texture.GetPixels32().Count(p => p.a == 255 && Mathf.Min(p.r, Mathf.Min(p.g, p.b)) >= 140);

        /// <summary>The highest opaque row: how tall the figure stands above its feet.</summary>
        private static int Height(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            for (var y = texture.height - 1; y >= 0; y--)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    if (pixels[y * texture.width + x].a > 200)
                    {
                        return y;
                    }
                }
            }

            return 0;
        }

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            return texture;
        }
    }
}
