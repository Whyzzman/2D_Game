using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The world in several scenes (item 3 of the big list, 2026-09-28). <c>TombTrial</c> is the
    /// world scene: the player, the camera, the services, and what follows the camera. Each built
    /// zone's level — ground, backdrops, enemies, altars, people — is a scene of its own in
    /// <c>Scenes/Zones</c>, named by the zone's <c>ContentScene</c> in the world graph;
    /// <see cref="ZoneStreamer"/> loads them around the player.
    ///
    /// Everything is placed by where it stands: the world graph's area under an object's centre
    /// names its zone. A folder whose contents lie in several zones is split, its path rebuilt in
    /// each zone scene. References from one scene to another are not saved by Unity, so they are
    /// cut here on purpose and each such field finds its target at run time.
    ///
    /// Scene tools that change the level (<see cref="SceneNpcs"/>, <see cref="SceneAmbience"/>,
    /// <see cref="SceneForestLook"/>, <see cref="SceneCharacterLight"/>) run inside
    /// <see cref="Edit"/>: the world is put back together, the tool runs as it always did, and the
    /// world is split again.
    /// </summary>
    public static class WorldScenes
    {
        public const string WorldSceneName = "TombTrial";
        public const string WorldScenePath = "Assets/Game/Scenes/TombTrial.unity";
        public const string ZoneFolder = "Assets/Game/Scenes/Zones";
        public const string TombSceneName = "Zone_Tomb";
        public const string ForestSceneName = "Zone_Forest";
        private const string WorldGraphPath = "Assets/Game/Data/WorldGraph.asset";

        /// <summary>What stays in the world scene, by path from a root.</summary>
        private static readonly HashSet<string> WorldPaths = new HashSet<string>
        {
            "Main Camera", "Spawn", "Player", "Systems",
            // The air of the zones follows the camera; the people's services serve every zone.
            "Ambience/ZoneAir", "Npcs/Services",
        };

        public static string ZoneScenePath(string sceneName) => $"{ZoneFolder}/{sceneName}.unity";

        [MenuItem("Forgotten Kingdom/Scene/Open Whole World")]
        public static void OpenWhole()
        {
            var world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            foreach (var zone in StreamedZones(LoadGraph()))
            {
                var path = ZoneScenePath(zone.ContentScene);
                if (File.Exists(path))
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
            }

            SceneManager.SetActiveScene(world);
        }

        /// <summary>Puts the world together and splits it again, replacing the zone scenes.</summary>
        [MenuItem("Forgotten Kingdom/Scene/Split World Into Zones")]
        public static void Split() => Edit(null);

        /// <summary>
        /// For a world scene just built whole (the generator's rebuild): the edit runs on it and
        /// the zone scenes are written from it, whatever they held before.
        /// </summary>
        public static void Rebuild(Action edit)
        {
            var world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            edit?.Invoke();
            Distribute(world);
        }

        /// <summary>A change to the level: the world whole again, the edit, then split and saved.</summary>
        public static void Edit(Action edit)
        {
            var world = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            foreach (var zone in StreamedZones(LoadGraph()))
            {
                var path = ZoneScenePath(zone.ContentScene);
                if (!File.Exists(path))
                {
                    continue;
                }

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                Merge(scene, world);
                EditorSceneManager.CloseScene(scene, true);
            }

            SceneManager.SetActiveScene(world);
            edit?.Invoke();
            Distribute(world);
        }

        // ---------- merging ----------

        private static void Merge(Scene zone, Scene world)
        {
            foreach (var root in zone.GetRootGameObjects())
            {
                var existing = world.GetRootGameObjects().FirstOrDefault(go => go.name == root.name);
                if (existing != null && IsFolder(existing) && IsFolder(root))
                {
                    MergeInto(root.transform, existing.transform);
                    Object.DestroyImmediate(root);
                }
                else
                {
                    SceneManager.MoveGameObjectToScene(root, world);
                }
            }
        }

        private static void MergeInto(Transform from, Transform into)
        {
            foreach (var child in Children(from))
            {
                var existing = Children(into).FirstOrDefault(t => t.name == child.name);
                if (existing != null && IsFolder(existing.gameObject) && IsFolder(child.gameObject))
                {
                    MergeInto(child, existing);
                    Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    child.SetParent(into, true);
                }
            }
        }

        // ---------- splitting ----------

        private static void Distribute(Scene world)
        {
            var graph = LoadGraph();
            var zones = StreamedZones(graph).ToList();
            if (zones.Count == 0)
            {
                Debug.LogError("The world graph has no zone with a scene; nothing to split.");
                return;
            }

            EnsureStreamer(world);
            Physics2D.SyncTransforms();
            var scenes = new Dictionary<string, Scene>();
            EnsureFolder(ZoneFolder);
            foreach (var zone in zones)
            {
                // Saved at once: Unity makes only one untitled scene at a time.
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                EditorSceneManager.SaveScene(scene, ZoneScenePath(zone.ContentScene));
                scenes[zone.ZoneId] = scene;
            }

            var placer = new Placer(graph, scenes);
            foreach (var root in world.GetRootGameObjects())
            {
                placer.Place(root, root.name);
            }

            var severed = Sever(world, scenes.Values);
            EditorSceneManager.MarkSceneDirty(world);
            EditorSceneManager.SaveScene(world);
            foreach (var zone in zones)
            {
                var scene = scenes[zone.ZoneId];
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ZoneScenePath(zone.ContentScene));
            }

            AddToBuild(zones.Select(z => ZoneScenePath(z.ContentScene)));
            SceneManager.SetActiveScene(world);
            Debug.Log($"World split: {string.Join(", ", zones.Select(z => $"{z.ContentScene} {placer.Count(z.ZoneId)} objects"))}; "
                + $"{severed.Count} references across scenes cut:\n{string.Join("\n", severed.Distinct())}");
        }

        /// <summary>The world scene's services load the zones; the streamer sits with them.</summary>
        private static void EnsureStreamer(Scene world)
        {
            var systems = world.GetRootGameObjects().FirstOrDefault(go => go.name == "Systems");
            if (systems == null)
            {
                Debug.LogError("The world scene has no Systems to load the zones from.");
                return;
            }

            var streamer = systems.GetComponent<ZoneStreamer>();
            if (streamer == null)
            {
                streamer = systems.AddComponent<ZoneStreamer>();
            }

            var serialized = new SerializedObject(streamer);
            serialized.FindProperty("_progress").objectReferenceValue = systems.GetComponent<GameProgress>();
            serialized.FindProperty("_player").objectReferenceValue =
                world.GetRootGameObjects().Select(go => go.GetComponent<PlayerCharacter>()).FirstOrDefault(p => p != null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puts each object into the zone its centre stands in.</summary>
        private sealed class Placer
        {
            private readonly WorldGraph _graph;
            private readonly Dictionary<string, Scene> _scenes;
            private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

            public Placer(WorldGraph graph, Dictionary<string, Scene> scenes)
            {
                _graph = graph;
                _scenes = scenes;
            }

            public int Count(string zoneId) => _counts.TryGetValue(zoneId, out var count) ? count : 0;

            public void Place(GameObject go, string path)
            {
                if (WorldPaths.Contains(path))
                {
                    return;
                }

                var zone = ZoneOf(go, path);
                if (zone != null)
                {
                    Move(go, zone);
                    return;
                }

                foreach (var child in Children(go.transform))
                {
                    Place(child.gameObject, path + "/" + child.name);
                }

                if (go.transform.childCount == 0 && IsFolder(go))
                {
                    Object.DestroyImmediate(go);
                }
            }

            /// <summary>The one zone all of it belongs to; null when it is spread over several or stays.</summary>
            private string ZoneOf(GameObject go, string path)
            {
                if (WorldPaths.Contains(path))
                {
                    return null;
                }

                var backdrop = go.GetComponent<ZoneBackdrop>();
                if (backdrop != null)
                {
                    var id = new SerializedObject(backdrop).FindProperty("_zoneId").stringValue;
                    if (_scenes.ContainsKey(id))
                    {
                        return id;
                    }
                }

                if (IsFolder(go) && go.transform.childCount > 0)
                {
                    string shared = null;
                    foreach (var child in Children(go.transform))
                    {
                        var zone = ZoneOf(child.gameObject, path + "/" + child.name);
                        if (zone == null || (shared != null && zone != shared))
                        {
                            return null;
                        }

                        shared = zone;
                    }

                    return shared;
                }

                return ZoneAt(Centre(go));
            }

            private string ZoneAt(Vector2 point)
            {
                WorldAreaDefinition best = null;
                var bestScore = float.MaxValue;
                foreach (var area in _graph.Areas)
                {
                    if (area == null || !_scenes.ContainsKey(area.ZoneId))
                    {
                        continue;
                    }

                    var bounds = area.WorldBounds;
                    // Inside wins, the smallest room first; otherwise the nearest along the way.
                    var score = bounds.Contains(point)
                        ? -1e6f + bounds.width * bounds.height
                        : Mathf.Max(0f, bounds.xMin - point.x, point.x - bounds.xMax) * 1000f
                          + Mathf.Max(0f, bounds.yMin - point.y, point.y - bounds.yMax);
                    if (score < bestScore)
                    {
                        best = area;
                        bestScore = score;
                    }
                }

                return best != null ? best.ZoneId : null;
            }

            private void Move(GameObject go, string zoneId)
            {
                var scene = _scenes[zoneId];
                var parent = go.transform.parent;
                var folder = parent != null ? FolderIn(scene, parent) : null;
                go.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(go, scene);
                if (folder != null)
                {
                    go.transform.SetParent(folder, true);
                }

                _counts[zoneId] = Count(zoneId) + 1;
            }

            /// <summary>The same folder path in the zone scene, made with the same placement.</summary>
            private static Transform FolderIn(Scene scene, Transform worldFolder)
            {
                var parent = worldFolder.parent != null ? FolderIn(scene, worldFolder.parent) : null;
                var siblings = parent != null
                    ? Children(parent)
                    : scene.GetRootGameObjects().Select(go => go.transform);
                var existing = siblings.FirstOrDefault(t => t.name == worldFolder.name);
                if (existing != null)
                {
                    return existing;
                }

                var folder = new GameObject(worldFolder.name).transform;
                SceneManager.MoveGameObjectToScene(folder.gameObject, scene);
                folder.SetParent(parent, false);
                folder.localPosition = worldFolder.localPosition;
                folder.localRotation = worldFolder.localRotation;
                folder.localScale = worldFolder.localScale;
                folder.gameObject.layer = worldFolder.gameObject.layer;
                folder.gameObject.SetActive(worldFolder.gameObject.activeSelf);
                return folder;
            }
        }

        /// <summary>
        /// Where a thing stands: a placed prefab (an enemy, an altar, a find) by its position; level
        /// geometry by the middle of what is drawn or solid. Switched-off renderers and colliders
        /// report empty bounds at the origin, so they are left out.
        /// </summary>
        private static Vector2 Centre(GameObject go)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                return go.transform.position;
            }

            Bounds? total = null;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled && !(renderer is ParticleSystemRenderer))
                {
                    total = Grow(total, renderer.bounds);
                }
            }

            foreach (var collider in go.GetComponentsInChildren<Collider2D>())
            {
                if (collider.enabled)
                {
                    total = Grow(total, collider.bounds);
                }
            }

            return total.HasValue ? (Vector2)total.Value.center : (Vector2)go.transform.position;
        }

        private static Bounds Grow(Bounds? total, Bounds add)
        {
            if (!total.HasValue)
            {
                return add;
            }

            var grown = total.Value;
            grown.Encapsulate(add);
            return grown;
        }

        /// <summary>A plain folder: nothing on it but its transform, and not part of a prefab.</summary>
        private static bool IsFolder(GameObject go)
        {
            return go.GetComponents<Component>().Length == 1
                && !PrefabUtility.IsPartOfPrefabInstance(go);
        }

        private static List<Transform> Children(Transform parent)
        {
            var children = new List<Transform>(parent.childCount);
            foreach (Transform child in parent)
            {
                children.Add(child);
            }

            return children;
        }

        // ---------- references across scenes ----------

        /// <summary>
        /// Clears every reference from one scene to an object of another. Unity would drop them on
        /// save anyway, with a warning each; clearing them here makes it deliberate and listed.
        /// </summary>
        private static List<string> Sever(Scene world, IEnumerable<Scene> zones)
        {
            var cut = new List<string>();
            foreach (var scene in zones.Prepend(world))
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true))
                    {
                        if (component == null || component is Transform)
                        {
                            continue;
                        }

                        var serialized = new SerializedObject(component);
                        var property = serialized.GetIterator();
                        var changed = false;
                        while (property.Next(true))
                        {
                            if (property.propertyType != SerializedPropertyType.ObjectReference)
                            {
                                continue;
                            }

                            var target = property.objectReferenceValue;
                            var targetScene = SceneOf(target);
                            if (targetScene.HasValue && targetScene.Value != scene)
                            {
                                cut.Add($"{component.GetType().Name}.{property.propertyPath} → {targetScene.Value.name}");
                                property.objectReferenceValue = null;
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }
            }

            return cut;
        }

        private static Scene? SceneOf(Object target)
        {
            if (target == null || EditorUtility.IsPersistent(target))
            {
                return null;
            }

            var go = target as GameObject ?? (target as Component)?.gameObject;
            return go != null ? go.scene : (Scene?)null;
        }

        // ---------- the graph and the build ----------

        private static WorldGraph LoadGraph() => AssetDatabase.LoadAssetAtPath<WorldGraph>(WorldGraphPath);

        private static IEnumerable<ZoneDefinition> StreamedZones(WorldGraph graph)
        {
            if (graph == null)
            {
                yield break;
            }

            foreach (var zone in graph.Zones)
            {
                if (zone != null && ZoneStreamingPlan.IsStreamed(graph, zone.ZoneId))
                {
                    yield return zone;
                }
            }
        }

        /// <summary>Zone scenes right after the world scene; the streamer loads them by name.</summary>
        private static void AddToBuild(IEnumerable<string> paths)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var at = scenes.FindIndex(s => s.path == WorldScenePath) + 1;
            foreach (var path in paths)
            {
                scenes.RemoveAll(s => s.path == path);
                at = Mathf.Clamp(at, 0, scenes.Count);
                scenes.Insert(at++, new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
            }
        }
    }
}
