using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Creates the sorting layers the Visual Bible §10 specified but nobody had created yet.
    ///
    /// Until now everything lived in "Default" and was ordered by <c>sortingOrder</c> alone. That
    /// works while there are six things on screen; it stops working the moment there are three
    /// parallax layers behind the level and a foreground in front of it, because order numbers
    /// then have to encode depth that layers exist to express.
    ///
    /// This changes no gameplay. It is the same objects drawn in the same sequence, with the
    /// sequence written down somewhere it can be reasoned about.
    /// </summary>
    public static class SortingLayerSetup
    {
        /// <summary>
        /// Back to front. The order of this array is the order on screen.
        ///
        /// "Default" is in the list on purpose, and early. Unity always keeps it and new layers
        /// would otherwise be appended after it — which would put every background in front of the
        /// characters. Placing it here also means any renderer that was never assigned a layer
        /// lands behind the gameplay plane, where a mistake is visible but harmless, rather than
        /// on top of the player.
        /// </summary>
        public static readonly string[] Layers =
        {
            "BackgroundFar",
            "BackgroundMid",
            "BackgroundNear",
            "EnvironmentBack",
            "Default",
            "GameplayEnvironment",
            "Characters",
            "Weapons",
            "Projectiles",
            "VFXBack",
            "VFXFront",
            "Foreground",
            "WorldUI",
            "ScreenUI"
        };

        [MenuItem("Forgotten Kingdom/Create Sorting Layers")]
        public static void Ensure()
        {
            var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (manager == null || manager.Length == 0)
            {
                Debug.LogError("TagManager.asset could not be opened; sorting layers unchanged.");
                return;
            }

            var serialized = new SerializedObject(manager[0]);
            var sortingLayers = serialized.FindProperty("m_SortingLayers");
            if (sortingLayers == null)
            {
                Debug.LogError("TagManager.asset has no sorting layer list.");
                return;
            }

            if (Matches(sortingLayers))
            {
                return;
            }

            // Rewritten in full rather than appended to, because the order is the whole point and
            // appending cannot put a layer behind Default.
            sortingLayers.arraySize = Layers.Length;
            for (var i = 0; i < Layers.Length; i++)
            {
                var element = sortingLayers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = Layers[i];
                // Unity identifies a sorting layer by a stored id, not by position. Default must
                // keep id 0 or every renderer that uses it silently moves.
                //
                // The field is an unsigned int, and writing it through intValue is ignored — every
                // layer then keeps id 0, collides with Default, and the whole list collapses back
                // to a single layer with no error anywhere. longValue is what actually lands.
                element.FindPropertyRelative("uniqueID").longValue =
                    Layers[i] == "Default" ? 0L : UniqueId(Layers[i]);
                element.FindPropertyRelative("locked").boolValue = false;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"Forgotten Kingdom: sorting layers set ({Layers.Length}).");
        }

        private static bool Matches(SerializedProperty layers)
        {
            if (layers.arraySize != Layers.Length)
            {
                return false;
            }

            for (var i = 0; i < Layers.Length; i++)
            {
                var element = layers.GetArrayElementAtIndex(i);
                var expectedId = Layers[i] == "Default" ? 0L : UniqueId(Layers[i]);
                // Ids are compared as well as names: a layer with the wrong id resolves to
                // Default, which looks identical in the list and is wrong everywhere else.
                if (element.FindPropertyRelative("name").stringValue != Layers[i]
                    || element.FindPropertyRelative("uniqueID").longValue != expectedId)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The same hash Unity uses for a sorting layer id. Deterministic, so a rebuild on another
        /// machine produces identical scene files instead of a diff nobody asked for.
        /// </summary>
        private static long UniqueId(string name)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var c in name)
                {
                    hash = (hash ^ c) * 16777619;
                }

                // Masked to 31 bits: the field is unsigned, but a value above int.MaxValue is
                // clamped on the way in and silently lands as zero — which collides with Default.
                var id = hash & 0x7FFFFFFF;
                return id == 0 ? 1L : id;
            }
        }
    }
}
