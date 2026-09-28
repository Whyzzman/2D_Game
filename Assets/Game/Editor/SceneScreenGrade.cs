using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The post-processing of both scenes (visual item 2, the user's choices of 2026-09-28:
    /// restrained, no film grain, a boss fight a little harsher, one switch in the settings).
    ///
    /// Writes the volume profiles to <c>Data/Grading</c> — a grade per zone, the switchable
    /// effects (bloom and vignette) and the boss grade — and gives each scene's camera a
    /// <see cref="ScreenGrade"/> with its volumes as children. Replaces only its own "Grade"
    /// children of the camera; repeat runs keep the profiles' GUIDs.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneScreenGrade.Apply
    /// </summary>
    public static class SceneScreenGrade
    {
        public const string Folder = "Assets/Game/Data/Grading";
        public const string TombProfile = Folder + "/grade_tomb.asset";
        public const string ForestProfile = Folder + "/grade_forest.asset";
        public const string EffectsProfile = Folder + "/grade_effects.asset";
        public const string BossProfile = Folder + "/grade_boss.asset";
        private const string LevelPath = "Assets/Game/Scenes/TombTrial.unity";
        private const string MenuPath = "Assets/Game/Scenes/MainMenu.unity";
        private const string TombZone = "zone_forgotten_tomb";
        private const string ForestZone = "zone_withered_forest";
        private const string ChildPrefix = "Grade";

        [MenuItem("Forgotten Kingdom/Scene/Apply Screen Grade")]
        public static void Apply()
        {
            WriteProfiles();
            var level = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Single);
            ConfigureLevelCamera(FindCamera());
            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);

            var menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
            ConfigureMenuCamera(FindCamera());
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
            Debug.Log("Screen grade applied to TombTrial and MainMenu.");
        }

        public static Camera FindCamera()
        {
            var camera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (camera == null)
            {
                Debug.LogError("The scene has no camera to grade.");
            }

            return camera;
        }

        /// <summary>Both zones, the boss and the effects, for the level.</summary>
        public static void ConfigureLevelCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            WriteProfiles();
            var grade = Prepare(camera);
            var serialized = new SerializedObject(grade);
            var zones = serialized.FindProperty("_zones");
            zones.arraySize = 2;
            Zone(zones.GetArrayElementAtIndex(0), TombZone, AddVolume(camera, "Tomb", TombProfile, 0));
            Zone(zones.GetArrayElementAtIndex(1), ForestZone, AddVolume(camera, "Forest", ForestProfile, 0));
            serialized.FindProperty("_defaultZoneId").stringValue = TombZone;
            serialized.FindProperty("_boss").objectReferenceValue = AddVolume(camera, "Boss", BossProfile, 1);
            serialized.FindProperty("_effects").objectReferenceValue =
                AddVolume(camera, "Effects", EffectsProfile, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The title screen: the Tomb's grade and the effects, no bosses.</summary>
        public static void ConfigureMenuCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            WriteProfiles();
            var grade = Prepare(camera);
            var serialized = new SerializedObject(grade);
            var zones = serialized.FindProperty("_zones");
            zones.arraySize = 1;
            Zone(zones.GetArrayElementAtIndex(0), TombZone, AddVolume(camera, "Tomb", TombProfile, 0));
            serialized.FindProperty("_defaultZoneId").stringValue = TombZone;
            serialized.FindProperty("_boss").objectReferenceValue = null;
            serialized.FindProperty("_effects").objectReferenceValue =
                AddVolume(camera, "Effects", EffectsProfile, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ScreenGrade Prepare(Camera camera)
        {
            for (var i = camera.transform.childCount - 1; i >= 0; i--)
            {
                if (camera.transform.GetChild(i).name.StartsWith(ChildPrefix))
                {
                    Object.DestroyImmediate(camera.transform.GetChild(i).gameObject);
                }
            }

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            EditorUtility.SetDirty(data);
            var grade = camera.GetComponent<ScreenGrade>();
            return grade != null ? grade : camera.gameObject.AddComponent<ScreenGrade>();
        }

        private static void Zone(SerializedProperty entry, string zoneId, Volume volume)
        {
            entry.FindPropertyRelative("_zoneId").stringValue = zoneId;
            entry.FindPropertyRelative("_volume").objectReferenceValue = volume;
        }

        private static Volume AddVolume(Camera camera, string name, string profilePath, float priority)
        {
            var host = new GameObject(ChildPrefix + name);
            host.transform.SetParent(camera.transform, false);
            var volume = host.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = priority;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            volume.weight = name == "Boss" ? 0f : 1f;
            return volume;
        }

        // ---------- profiles ----------

        /// <summary>
        /// The numbers. Restrained on purpose: the fight must stay the clearest thing on screen, so
        /// nothing here crushes the dark or pushes the colour further than the Visual Bible's zone
        /// temperatures — cold blue-grey with warm altars in the Tomb, cold green and moon silver
        /// in the Forest.
        /// </summary>
        public static void WriteProfiles()
        {
            PlaceholderImport.EnsureFolder(Folder);

            var tomb = Profile(TombProfile);
            Adjustments(tomb, contrast: 8f, saturation: -8f, filter: new Color(0.96f, 0.98f, 1.03f));
            Toning(tomb, shadows: new Color(0.46f, 0.49f, 0.56f), highlights: new Color(0.57f, 0.52f, 0.45f));

            var forest = Profile(ForestProfile);
            Adjustments(forest, contrast: 6f, saturation: -4f, filter: new Color(0.95f, 1f, 0.99f));
            Toning(forest, shadows: new Color(0.45f, 0.52f, 0.5f), highlights: new Color(0.52f, 0.54f, 0.56f));

            var effects = Profile(EffectsProfile);
            var bloom = Ensure<Bloom>(effects);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(Color.white);
            var vignette = Ensure<Vignette>(effects);
            vignette.color.Override(Color.black);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
            vignette.rounded.Override(false);

            var boss = Profile(BossProfile);
            Adjustments(boss, contrast: 18f, saturation: -14f, filter: null);

            foreach (var profile in new[] { tomb, forest, effects, boss })
            {
                EditorUtility.SetDirty(profile);
            }

            AssetDatabase.SaveAssets();
        }

        private static VolumeProfile Profile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static T Ensure<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        private static void Adjustments(VolumeProfile profile, float contrast, float saturation, Color? filter)
        {
            var adjustments = Ensure<ColorAdjustments>(profile);
            adjustments.contrast.Override(contrast);
            adjustments.saturation.Override(saturation);
            if (filter.HasValue)
            {
                adjustments.colorFilter.Override(filter.Value);
            }
            else
            {
                // The boss leaves each zone's tint alone and only hardens it.
                adjustments.colorFilter.overrideState = false;
            }
        }

        private static void Toning(VolumeProfile profile, Color shadows, Color highlights)
        {
            var toning = Ensure<SplitToning>(profile);
            toning.shadows.Override(shadows);
            toning.highlights.Override(highlights);
            toning.balance.Override(0f);
        }
    }
}
