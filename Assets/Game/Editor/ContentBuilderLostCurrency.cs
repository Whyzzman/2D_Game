using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The price of dying (item 8 of the 2026-09-27 list): the trace that holds the gold left at the
    /// last death, and the player's side that leaves it there. Built before the player prefab,
    /// which points at the trace.
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string LostCurrencyMarkPath = PrefabFolder + "/LostCurrencyMark.prefab";

        /// <summary>The player's collider is 1.6 tall around its origin: the floor is 0.8 below.</summary>
        private const float PlayerFeetOffset = -0.8f;

        private static void BuildLostCurrencyMarkPrefab()
        {
            var root = new GameObject("LostCurrencyMark");
            var visualObject = FindOrCreateChild(root, "Visual");
            visualObject.transform.localPosition = new Vector3(0f, PlayerFeetOffset, 0f);
            var visual = Ensure<SpriteRenderer>(visualObject);
            var frames = _vfx != null && _vfx.TryGetValue(PlaceholderVfxArt.LostGoldName, out var sprites)
                ? sprites
                : System.Array.Empty<Sprite>();
            visual.sprite = frames.Length > 0 ? frames[0] : _sprite;
            // Unlit, like the effects: it has to be found in the darkest corner.
            visual.sharedMaterial = _material;
            SetSorting(visual, "Characters", -2);

            var trigger = Ensure<CircleCollider2D>(root);
            trigger.isTrigger = true;
            trigger.radius = 0.7f;

            var glow = FindOrCreateChild(root, "Glow");
            glow.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            var light = AddLight(glow, Light2D.LightType.Point,
                LostCurrencyMark.Gold, 0.9f, 2.2f, 0.1f, 0.6f);

            var mark = Ensure<LostCurrencyMark>(root);
            var serialized = new SerializedObject(mark);
            serialized.FindProperty("_renderer").objectReferenceValue = visual;
            serialized.FindProperty("_trigger").objectReferenceValue = trigger;
            serialized.FindProperty("_light").objectReferenceValue = light;
            serialized.FindProperty("_framesPerSecond").floatValue = 8f;
            var list = serialized.FindProperty("_frames");
            list.arraySize = frames.Length;
            for (var i = 0; i < frames.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(root, LostCurrencyMarkPath);
        }

        private static void BuildLostCurrencyDrop(GameObject root)
        {
            var drop = Ensure<LostCurrencyDrop>(root);
            SetReference(drop, "_player", root.GetComponent<PlayerCharacter>());
            SetReference(drop, "_motor", root.GetComponent<PlayerMotor2D>());
            SetReference(drop, "_markPrefab",
                AssetDatabase.LoadAssetAtPath<LostCurrencyMark>(LostCurrencyMarkPath));
        }
    }
}
