using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Reading the hero and the enemies (visual item 3, 2026-09-28) as data: every frame carries a
    /// normal map that rounds its silhouette, the hero's lit edge is the brightest and coldest,
    /// enemies' edges are warm and their eyes shine, and the training targets are drawn.
    /// </summary>
    public sealed class CharacterLookDataTests
    {
        private const string Characters = "Assets/Game/Art/Characters";
        private const string Player = Characters + "/Player/Idle/player_idle_00.png";

        private static readonly string[] EnemyIdles =
        {
            "Enemies/ZombieSoldier", "Enemies/ShieldGuard", "Enemies/CorruptedHound",
            "Enemies/ForestArcher", "Enemies/RootShaman", "Enemies/RoyalSpearman"
        };

        [Test]
        public void EveryCharacterFrameCarriesItsNormalMap()
        {
            var frames = AssetDatabase.FindAssets("t:Texture2D", new[] { Characters })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".png"))
                .ToArray();
            Assert.That(frames.Length, Is.GreaterThan(1000));
            foreach (var frame in frames)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(frame);
                var secondary = importer.secondarySpriteTextures;
                Assert.That(secondary != null && secondary.Any(s => s.name == "_NormalMap" && s.texture != null),
                    Is.True, $"{frame} has no normal map, so side light cannot shape it.");
            }
        }

        [Test]
        public void ANormalMapRoundsTheSilhouette()
        {
            var frame = Load(Player);
            var normal = Load("Assets/Game/Art/CharacterNormals/Player/Idle/player_idle_00.png");
            // Every left edge of the figure faces left, every right edge faces right: red, the
            // normal's x, below and above the middle.
            float left = 0f, right = 0f;
            int lefts = 0, rights = 0;
            bool Solid(int x, int y) => frame.GetPixel(x, y).a > 0.16f;
            for (var y = 0; y < frame.height; y++)
            {
                for (var x = 1; x < frame.width - 1; x++)
                {
                    if (Solid(x, y) && !Solid(x - 1, y) && Solid(x + 1, y))
                    {
                        left += normal.GetPixel(x + 1, y).r;
                        lefts++;
                    }

                    if (Solid(x, y) && !Solid(x + 1, y) && Solid(x - 1, y))
                    {
                        right += normal.GetPixel(x - 1, y).r;
                        rights++;
                    }
                }
            }

            Assert.That(left / lefts, Is.LessThan(0.4f), "Left edges turn to the left.");
            Assert.That(right / rights, Is.GreaterThan(0.6f), "Right edges turn to the right.");
        }

        [Test]
        public void TheHerosEdgeIsTheBrightestAndTheColdest()
        {
            var (heroLight, heroWarmth) = Edge(Load(Player));
            foreach (var enemy in EnemyIdles)
            {
                var (light, warmth) = Edge(Load(FirstFrame(enemy)));
                Assert.That(heroLight, Is.GreaterThan(light), $"The hero outshines {enemy} at the edge.");
                Assert.That(warmth, Is.GreaterThan(heroWarmth), $"{enemy}'s edge is warmer than the hero's.");
            }
        }

        [Test]
        public void EveryEnemyHasEyesThatShine()
        {
            foreach (var enemy in EnemyIdles)
            {
                var frame = Load(FirstFrame(enemy));
                var shining = frame.GetPixels32().Any(p =>
                {
                    var max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                    var min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                    // Bright and coloured: an eye, not a steel edge.
                    return p.a > 200 && max >= 225 && max - min >= 60;
                });
                Assert.That(shining, Is.True, $"{enemy} shows no lit eye.");
            }
        }

        [Test]
        public void TheTrainingTargetsAreDrawn()
        {
            foreach (var name in new[] { "training_dummy", "training_attacker" })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Characters}/Training/{name}.png");
                Assert.That(sprite, Is.Not.Null, name);
                Assert.That(sprite.bounds.size.y, Is.InRange(1.8f, 2.3f), $"{name} stands about a man tall.");
                Assert.That(sprite.pivot.y, Is.EqualTo(0f), $"{name} stands on its foot.");
            }
        }

        // ---------- helpers ----------

        private static string FirstFrame(string folder) =>
            Directory.GetFiles($"{Characters}/{folder}", "*.png", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => p.Contains("/Idle/") || p.ToLowerInvariant().Contains("idle"))
                .OrderBy(p => p)
                .First();

        /// <summary>Mean brightness and warmth (red minus blue) of the pixels on the silhouette's edge.</summary>
        private static (float light, float warmth) Edge(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var w = texture.width;
            var h = texture.height;
            // The body is fully opaque; the dark outline ring around it is not, and is not the edge.
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && pixels[y * w + x].a == 255;
            float light = 0f, warmth = 0f;
            var count = 0;
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    if (!Solid(x, y) || (Solid(x - 1, y) && Solid(x, y + 1) && Solid(x - 1, y + 1)))
                    {
                        continue;
                    }

                    var p = pixels[y * w + x];
                    light += (p.r + p.g + p.b) / 3f;
                    warmth += p.r - p.b;
                    count++;
                }
            }

            return (light / count, warmth / count);
        }

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            return texture;
        }
    }
}
