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
        private const string BossFolder = CharacterFolder + "/Bosses";

        /// <summary>
        /// Bosses ship as a variant of their atlas at this scale (KI-012, 2026-11-12, decided by Claude under
        /// the standing consent). Their frames are drawn at 100 px a unit, but a boss is seen through the
        /// boss zoom (6.5 × 1.18 units half-height): about 70 screen px a unit at 1080p. At 0.75 a boss atlas
        /// goes from 3 pages of 4096² (and as many of normals) to 2 — the heaviest textures left in memory.
        /// </summary>
        public const float BossVariantScale = 0.75f;

        /// <summary>The variant atlas that ships for a boss's master atlas.</summary>
        public static string VariantPath(string masterPath) =>
            masterPath.Replace(".spriteatlasv2", "Variant.spriteatlasv2");

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
                if (entry.Value.StartsWith(BossFolder + "/"))
                {
                    GenerateVariant(entry.Key);
                }
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

        /// <summary>
        /// The boss's master atlas stays the source of its frames but leaves the build; its variant, a
        /// scaled copy of the master's pages (normals included), is what the player loads and binds to.
        /// </summary>
        private static void GenerateVariant(string masterPath)
        {
            var master = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(masterPath);
            var variantPath = VariantPath(masterPath);
            var variant = File.Exists(variantPath) ? SpriteAtlasAsset.Load(variantPath) : new SpriteAtlasAsset();
            variant.SetIsVariant(true);
            variant.SetMasterAtlas(master);
            SpriteAtlasAsset.Save(variant, variantPath);
            AssetDatabase.ImportAsset(variantPath);

            if (AssetImporter.GetAtPath(variantPath) is SpriteAtlasImporter importer)
            {
                var texture = importer.textureSettings;
                texture.filterMode = FilterMode.Bilinear;
                texture.generateMipMaps = false;
                texture.readable = false;
                importer.textureSettings = texture;
                var platform = importer.GetPlatformSettings("DefaultTexturePlatform");
                platform.maxTextureSize = 4096;
                importer.SetPlatformSettings(platform);
                importer.variantScale = BossVariantScale;
                importer.includeInBuild = true;
                importer.SaveAndReimport();
            }

            if (AssetImporter.GetAtPath(masterPath) is SpriteAtlasImporter masterImporter)
            {
                masterImporter.includeInBuild = false;
                masterImporter.SaveAndReimport();
            }
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
