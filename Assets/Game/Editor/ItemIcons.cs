using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Painted item icons (2026-11-02, Midjourney through tools/midjourney/icons.py): a sprite at
    /// <c>Art/Items/Icons/&lt;item id&gt;.png</c> becomes that item's icon. Items without one keep the
    /// vector silhouette <see cref="ItemIcon"/> draws by kind.
    /// </summary>
    public static class ItemIcons
    {
        public const string Folder = "Assets/Game/Art/Items/Icons";
        private const string ItemFolder = "Assets/Game/Data/Items";

        /// <summary>The painted icon for an item id, imported as a sprite; null if there is none.</summary>
        public static Sprite For(string itemId)
        {
            var path = $"{Folder}/{itemId}.png";
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Hands every painted icon to its item. Returns how many items got one.</summary>
        [MenuItem("Forgotten Kingdom/Items/Assign Painted Icons")]
        public static int AssignAll()
        {
            var assigned = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemFolder }))
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var sprite = item != null ? For(item.ItemId) : null;
                if (sprite == null)
                {
                    continue;
                }

                var serialized = new SerializedObject(item);
                serialized.FindProperty("_icon").objectReferenceValue = sprite;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(item);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Painted icons assigned to {assigned} items.");
            return assigned;
        }
    }
}
