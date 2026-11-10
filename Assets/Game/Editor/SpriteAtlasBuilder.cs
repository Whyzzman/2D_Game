// [UNITY-SKILL:SPRITEATLAS]
// [TYPE:PREBUILD]
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// One sprite atlas per character and one for the hit effects, so each character draws from
    /// a single texture instead of a texture per frame (1 365 of them before this).
    ///
    /// Every atlas packs a whole folder rather than a list of sprites, so the placeholder
    /// generator can rewrite frames, and drawn art can replace them in place
    /// (<c>Art/Painted.txt</c>), without the atlases going stale. The environment is left out on
    /// purpose: its tiled walls and floors repeat their textures, and a texture inside an atlas
    /// cannot repeat.
    ///
    /// Built into the player (no Addressables). Runs before every build, and on demand through
    /// <c>-executeMethod ForgottenKingdom.EditorTools.SpriteAtlasBuilder.Generate</c>.
    /// </summary>
    public sealed class SpriteAtlasBuilder : IPreprocessBuildWithReport
    {
        public const string AtlasFolder = "Assets/Game/Art/Atlases";
        private const string CharacterFolder = "Assets/Game/Art/Characters";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Generate();

        /// <summary>Which folder each atlas packs, keyed by the atlas path.</summary>
        public static IReadOnlyDictionary<string, string> Plan()
        {
            var plan = new SortedDictionary<string, string>
            {
                [$"{AtlasFolder}/Player.spriteatlasv2"] = $"{CharacterFolder}/Player",
                [$"{AtlasFolder}/VfxImpacts.spriteatlasv2"] = "Assets/Game/Art/VFX/Impacts"
            };

            foreach (var group in new[] { "Enemies", "Bosses" })
            {
                var root = $"{CharacterFolder}/{group}";
                if (!AssetDatabase.IsValidFolder(root))
                {
                    continue;
                }

                foreach (var folder in AssetDatabase.GetSubFolders(root))
                {
                    plan[$"{AtlasFolder}/{Path.GetFileName(folder)}.spriteatlasv2"] = folder;
                }
            }

            return plan;
        }

        public static void Generate()
        {
            // An atlas made while packing is disabled imports and looks finished, but never packs.
            if (EditorSettings.spritePackerMode != SpritePackerMode.SpriteAtlasV2)
            {
                EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
            }

            Debug.Log($"[SpriteAtlas] Sprite packer mode: {EditorSettings.spritePackerMode}");
            if (!AssetDatabase.IsValidFolder(AtlasFolder))
            {
                AssetDatabase.CreateFolder("Assets/Game/Art", "Atlases");
            }

            foreach (var entry in Plan())
            {
                GenerateForFolder(entry.Value, entry.Key);
            }

            AssetDatabase.SaveAssets();
        }

        private static void GenerateForFolder(string folder, string atlasPath)
        {
            var packable = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder);
            if (packable == null || !folder.StartsWith("Assets/"))
            {
                Debug.LogWarning($"[SpriteAtlas] No folder at {folder}; {atlasPath} skipped.");
                return;
            }

            SpriteAtlasAsset atlas;
            if (File.Exists(atlasPath))
            {
                atlas = SpriteAtlasAsset.Load(atlasPath);
                var runtime = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
                if (runtime != null)
                {
                    atlas.Remove(runtime.GetPackables());
                }
            }
            else
            {
                atlas = new SpriteAtlasAsset();
            }

            atlas.Add(new Object[] { packable });
            SpriteAtlasAsset.Save(atlas, atlasPath);
            AssetDatabase.ImportAsset(atlasPath);
            Configure(atlasPath);
        }

        private static void Configure(string atlasPath)
        {
            if (!(AssetImporter.GetAtPath(atlasPath) is SpriteAtlasImporter importer))
            {
                Debug.LogWarning($"[SpriteAtlas] {atlasPath} has no atlas importer.");
                return;
            }

            var texture = importer.textureSettings;
            texture.filterMode = FilterMode.Bilinear;
            texture.generateMipMaps = false;
            texture.readable = false;
            importer.textureSettings = texture;

            // Rotation stays off: flipped and rotated frames would fight the sprites' pivots.
            // Tight packing stays off too: the frames use FullRect meshes (PlaceholderRaster),
            // which draw their whole rectangle, so a neighbour packed into a transparent corner
            // would show through.
            var packing = importer.packingSettings;
            packing.padding = 4;
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.enableAlphaDilation = true;
            importer.packingSettings = packing;

            var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 4096;
            importer.SetPlatformSettings(platform);

            importer.includeInBuild = true;
            importer.SaveAndReimport();
        }
    }
}
