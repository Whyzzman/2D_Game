using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The tomb's art as data: that every piece the level needs exists, that it imports under the
    /// same rules as the characters, and that the sorting layers are in the order the whole
    /// composition depends on.
    /// </summary>
    public sealed class TombEnvironmentDataTests
    {
        private const string Root = "Assets/Game/Art/Environment/Tomb";

        /// <summary>The P0 set: without any one of these the level is still a greybox.</summary>
        private static readonly string[] Required =
        {
            "Tiles/env_tomb_floor_01",
            "Tiles/env_tomb_wall_01",
            "Tiles/env_tomb_ceiling_01",
            "Tiles/env_tomb_platform_01",
            "Tiles/env_tomb_platform_broken_01",
            "Tiles/env_tomb_trim_01",
            "Backgrounds/env_tomb_bg_far_01",
            "Backgrounds/env_tomb_bg_mid_01",
            "Backgrounds/env_tomb_bg_near_01",
            "Props/env_tomb_pillar_01",
            "Props/env_tomb_pillar_broken_01",
            "Props/env_tomb_arch_01",
            "Props/env_tomb_statue_knight_01",
            "Props/env_tomb_statue_broken_01",
            "Props/env_tomb_sarcophagus_01",
            "Props/env_tomb_banner_01",
            "Props/env_tomb_chains_01",
            "Props/env_tomb_candles_01",
            "Props/env_tomb_rubble_01",
            "Props/env_tomb_roots_01",
            "Interactive/env_tomb_checkpoint_base",
            "Interactive/env_tomb_checkpoint_flame_cold",
            "Interactive/env_tomb_checkpoint_flame_lit",
            "Interactive/env_tomb_boss_gate",
            "Interactive/env_tomb_boss_barrier",
            "Arena/env_tomb_arena_backdrop",
            "Arena/env_tomb_arena_floor_crest",
            "Decals/env_tomb_pit_depth"
        };

        [Test]
        public void EveryRequiredTombAssetExists()
        {
            foreach (var relative in Required)
            {
                var path = $"{Root}/{relative}.png";
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(path), Is.Not.Null,
                    $"The content build writes {path}.");
            }
        }

        [Test]
        public void TombArtFollowsTheSameImportRulesAsTheCharacters()
        {
            // Mixing import settings between environment and characters is what makes a scene
            // look like two games stitched together, so this is checked rather than trusted.
            foreach (var relative in Required)
            {
                var path = $"{Root}/{relative}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
            }
        }

        [Test]
        public void TilesAndBackgroundsRepeatSoTiledDrawingHasNoSeam()
        {
            foreach (var relative in Required.Where(
                r => r.StartsWith("Tiles/") || r.StartsWith("Backgrounds/")))
            {
                var path = $"{Root}/{relative}.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), path);
            }
        }

        [Test]
        public void TilesRepeatOverSeveralUnitsRatherThanEveryMetre()
        {
            // A one-unit tile shows its seam every metre. Four units is still a repeat, but a
            // repeat the player is far less likely to read as a pattern.
            var floor = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Tiles/env_tomb_floor_01.png");
            Assert.That(floor.bounds.size.x, Is.GreaterThanOrEqualTo(2f));
        }

        // ---------- sorting layers ----------

        [Test]
        public void TheSortingLayersExistInTheOrderTheCompositionNeeds()
        {
            // Spelled out here rather than read from the builder: a test that asks the code under
            // test what it expects cannot catch the code changing its mind.
            var expected = new[]
            {
                "BackgroundFar", "BackgroundMid", "BackgroundNear", "EnvironmentBack", "Default",
                "GameplayEnvironment", "Characters", "Weapons", "Projectiles", "VFXBack",
                "VFXFront", "Foreground", "WorldUI", "ScreenUI"
            };
            var names = SortingLayer.layers.Select(layer => layer.name).ToArray();

            Assert.That(names, Is.EqualTo(expected),
                "Order is the whole point: a background listed after the characters draws on "
                + "top of them.");
        }

        [Test]
        public void DefaultSitsBehindTheGameplayPlane()
        {
            // Anything that was never given a layer lands on Default. Keeping Default behind the
            // gameplay plane makes that mistake visible but harmless, never a sprite over the
            // player's head.
            var names = SortingLayer.layers.Select(layer => layer.name).ToList();

            Assert.That(names.IndexOf("Default"),
                Is.LessThan(names.IndexOf("GameplayEnvironment")));
            Assert.That(names.IndexOf("Default"), Is.GreaterThan(names.IndexOf("BackgroundNear")));
        }

        [Test]
        public void TheLayerOrderRunsBackToFront()
        {
            var names = SortingLayer.layers.Select(layer => layer.name).ToList();

            Assert.That(names.IndexOf("BackgroundFar"), Is.LessThan(names.IndexOf("BackgroundMid")));
            Assert.That(names.IndexOf("BackgroundMid"),
                Is.LessThan(names.IndexOf("BackgroundNear")));
            Assert.That(names.IndexOf("GameplayEnvironment"),
                Is.LessThan(names.IndexOf("Characters")));
            Assert.That(names.IndexOf("Characters"), Is.LessThan(names.IndexOf("Foreground")));
            Assert.That(names.IndexOf("Foreground"), Is.LessThan(names.IndexOf("WorldUI")));
            Assert.That(names.IndexOf("WorldUI"), Is.LessThan(names.IndexOf("ScreenUI")));
        }
    }
}
