using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// Every character and the hit effects draw from an atlas of their own, packed by folder, so
    /// regenerated or final frames land in it without anyone touching the atlas.
    /// </summary>
    public sealed class SpriteAtlasTests
    {
        private const string AtlasFolder = "Assets/Game/Art/Atlases";

        private static string[] PackedFolders()
        {
            return new[] { "Assets/Game/Art/Characters/Player", "Assets/Game/Art/VFX/Impacts" }
                .Concat(AssetDatabase.GetSubFolders("Assets/Game/Art/Characters/Enemies"))
                .Concat(AssetDatabase.GetSubFolders("Assets/Game/Art/Characters/Bosses"))
                .ToArray();
        }

        private static string AtlasFor(string folder)
        {
            var name = folder.EndsWith("/Impacts") ? "VfxImpacts" : Path.GetFileName(folder);
            return $"{AtlasFolder}/{name}.spriteatlasv2";
        }

        [Test]
        public void SpritePackingIsOn()
        {
            // An atlas made while packing is disabled looks finished and never packs.
            Assert.That(EditorSettings.spritePackerMode, Is.EqualTo(SpritePackerMode.SpriteAtlasV2));
        }

        [Test]
        public void EveryCharacterAndTheHitEffectsHaveTheirOwnAtlas()
        {
            var folders = PackedFolders();
            Assert.That(folders, Has.Length.EqualTo(25), "Player, seventeen enemies, six bosses, effects.");
            foreach (var folder in folders)
            {
                var path = AtlasFor(folder);
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                Assert.That(atlas, Is.Not.Null, path);

                var packables = atlas.GetPackables();
                Assert.That(packables.Select(p => AssetDatabase.GetAssetPath(p)), Does.Contain(folder),
                    $"{path} packs the folder, so new frames join it on their own.");

                var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.includeInBuild, Is.True, path);
                Assert.That(importer.packingSettings.enableTightPacking, Is.False,
                    "FullRect frames would show neighbours packed into their corners.");

                var sprite = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
                    .First(s => s != null);
                Assert.That(atlas.CanBindTo(sprite), Is.True, $"{sprite.name} is in {path}.");
            }
        }

        [Test]
        public void TheTiledEnvironmentStaysOutOfAtlases()
        {
            // A texture inside an atlas cannot repeat, and the Tomb's walls and floors tile.
            foreach (var guid in AssetDatabase.FindAssets("t:SpriteAtlas", new[] { AtlasFolder }))
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var packable in atlas.GetPackables())
                {
                    Assert.That(AssetDatabase.GetAssetPath(packable), Does.Not.Contain("/Environment"));
                }
            }
        }
    }
}
