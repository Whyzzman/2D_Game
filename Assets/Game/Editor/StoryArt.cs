using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The painted steles the world's words are cut into (2026-11-04, Midjourney through
    /// tools/midjourney/sprites.py): 200 px a unit, the pivot at the foot, so a stele stands on the
    /// floor where its text puts it.
    /// </summary>
    public static class StoryArt
    {
        public const float PixelsPerUnit = 200f;

        /// <summary>The stele at <paramref name="path"/>, imported as a standing sprite; null if it is not painted.</summary>
        public static Sprite Stele(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return null;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType != TextureImporterType.Sprite || settings.spriteAlignment != (int)SpriteAlignment.BottomCenter
                || !Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
