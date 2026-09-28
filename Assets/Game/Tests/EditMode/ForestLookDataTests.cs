using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The forest's new look as data (visual item 1, 2026-09-28): a sky that reaches past every
    /// climb and ends in the camera's colour, layers in front that let it through, no seam where a
    /// layer repeats, an abbey that melts into the forest at its sides and fog without stripes.
    /// </summary>
    public sealed class ForestLookDataTests
    {
        private const string Root = "Assets/Game/Art/Environment/WitheredForest";
        private const string Far = Root + "/Backgrounds/env_forest_bg_far_01.png";
        private const string Mid = Root + "/Backgrounds/env_forest_bg_mid_01.png";
        private const string Near = Root + "/Backgrounds/env_forest_bg_near_01.png";
        private const string Abbey = Root + "/Arena/env_forest_arena_backdrop.png";
        private const string Fog = "Assets/Game/Art/Shapes/shape_fog_band.png";

        [Test]
        public void EachLayerReachesFromUnderTheFloorToPastTheClimbs()
        {
            foreach (var path in new[] { Far, Mid, Near })
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.That(sprite, Is.Not.Null, path);
                Assert.That(sprite.bounds.size.x, Is.EqualTo(20.48f).Within(0.01f), path);
                Assert.That(sprite.bounds.size.y, Is.EqualTo(20f).Within(0.01f),
                    $"{path}: four units under the floor and sixteen over it.");
            }
        }

        [Test]
        public void TheSkyIsSolidAndEndsInTheCamerasColour()
        {
            var far = Load(Far);
            var pixels = far.GetPixels32();
            foreach (var p in pixels)
            {
                Assert.That(p.a, Is.EqualTo(255), "The far layer is the sky: nothing shows through it.");
            }

            // Past the top of the layer the camera's clear colour shows; the sky must end in it.
            var clear = new Color32(10, 11, 17, 255);
            for (var x = 0; x < far.width; x += 97)
            {
                var top = far.GetPixel(x, far.height - 1);
                Assert.That(Mathf.Abs(top.r * 255f - clear.r), Is.LessThan(3f));
                Assert.That(Mathf.Abs(top.g * 255f - clear.g), Is.LessThan(3f));
                Assert.That(Mathf.Abs(top.b * 255f - clear.b), Is.LessThan(3f));
            }
        }

        [TestCase(Mid, 0.5f)]
        [TestCase(Near, 0.7f)]
        public void TheLayersInFrontLetTheSkyThrough(string path, float share)
        {
            // It used to be painted edge to edge, and the near layer hid the moon entirely.
            var texture = Load(path);
            var open = 0;
            var total = 0;
            for (var y = 600; y < texture.height; y += 7)
            {
                for (var x = 0; x < texture.width; x += 7)
                {
                    total++;
                    if (texture.GetPixel(x, y).a < 0.1f)
                    {
                        open++;
                    }
                }
            }

            Assert.That(open / (float)total, Is.GreaterThan(share),
                $"{path} covers too much of the sky above the floor.");
        }

        [Test]
        public void TheGreatTrunksFadeOutBeforeTheTopOfTheirLayer()
        {
            var near = Load(Near);
            for (var x = 0; x < near.width; x++)
            {
                Assert.That(near.GetPixel(x, near.height - 1).a, Is.LessThan(0.02f),
                    "A climb must never see where a trunk is cut off.");
            }
        }

        [TestCase(Far)]
        [TestCase(Mid)]
        [TestCase(Near)]
        [TestCase(Fog)]
        public void ALayerRepeatsWithoutASeam(string path)
        {
            var texture = Load(path);
            var last = texture.width - 1;
            var seam = 0f;
            var inside = 0f;
            for (var y = 0; y < texture.height; y++)
            {
                seam += Difference(texture.GetPixel(last, y), texture.GetPixel(0, y));
                inside += Difference(texture.GetPixel(last - 1, y), texture.GetPixel(last, y));
            }

            // The step across the wrap is no bigger than a step inside the picture.
            Assert.That(seam / texture.height, Is.LessThan(inside / texture.height + 0.02f), path);
        }

        [Test]
        public void TheAbbeyMeltsIntoTheForestAtItsSides()
        {
            var abbey = Load(Abbey);
            for (var y = 0; y < abbey.height; y += 25)
            {
                Assert.That(abbey.GetPixel(0, y).a, Is.LessThan(0.02f), "No hard edge on the left.");
                Assert.That(abbey.GetPixel(abbey.width - 1, y).a, Is.LessThan(0.02f), "Nor on the right.");
                Assert.That(abbey.GetPixel(abbey.width / 2, y).a, Is.GreaterThan(0.98f),
                    "The middle, behind the fight, is solid.");
            }
        }

        [Test]
        public void TheFogHasNoStripes()
        {
            // The old band was two sines sheared by height, which drew diagonals across the zone.
            // Diagonal bands make the gradients lean one way: x and y change together. Measured
            // on the fog divided by each row's mean, so the band's own soft top and bottom do not
            // count. The old texture scored 0.40. Billows lean nowhere, but a band holds only a
            // score of them and chance overlaps lean a little: this one scores 0.27, other
            // placements anywhere from 0 to 0.25. The bound sits between, and the generator is
            // deterministic, so it cannot flicker.
            var fog = Load(Fog);
            var mean = new float[fog.height];
            for (var y = 0; y < fog.height; y++)
            {
                for (var x = 0; x < fog.width; x++)
                {
                    mean[y] += fog.GetPixel(x, y).a;
                }

                mean[y] = mean[y] / fog.width + 0.0001f;
            }

            float N(int x, int y) => fog.GetPixel((x + fog.width) % fog.width, y).a / mean[y];
            double xx = 0, yy = 0, xy = 0;
            for (var y = 30; y < fog.height - 30; y++)
            {
                for (var x = 0; x < fog.width; x++)
                {
                    var gx = N(x + 1, y) - N(x - 1, y);
                    var gy = N(x, y + 1) - N(x, y - 1);
                    xx += gx * gx;
                    yy += gy * gy;
                    xy += gx * gy;
                }
            }

            Assert.That(System.Math.Abs(xy) / System.Math.Sqrt(xx * yy), Is.LessThan(0.33),
                "Fog drifts in billows, not in diagonal bands.");
        }

        [Test]
        public void EdgeArtTilesAlongALedge()
        {
            foreach (var name in new[] { "env_forest_edge_top_01", "env_forest_edge_roots_01" })
            {
                var path = $"{Root}/Tiles/{name}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), path);
            }

            Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Props/env_forest_edge_cap_01.png"),
                Is.Not.Null);
        }

        // ---------- helpers ----------

        private static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            return texture;
        }

        private static float Difference(Color a, Color b) =>
            (Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a)) * 0.25f;
    }
}
