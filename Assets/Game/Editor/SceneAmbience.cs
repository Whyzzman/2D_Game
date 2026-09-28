using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The living environment of TombTrial (item 58, the user's choices of 2026-09-27): each
    /// zone's air — dust and grit in the Tomb, leaves and spores in the forest — and sparse dark
    /// shapes in front of the play at the top and bottom of the frame, never in a boss arena.
    ///
    /// Scenes are the source of truth, so this is a scene edit, run once and again whenever the
    /// recipe changes: it replaces its own "Ambience" object and leaves everything else alone.
    /// Run: -executeMethod ForgottenKingdom.EditorTools.SceneAmbience.Apply
    /// </summary>
    public static class SceneAmbience
    {
        public const string RootName = "Ambience";
        private const string ArtFolder = "Assets/Game/Art/Environment/Foreground";
        private const string WorldGraphPath = "Assets/Game/Data/WorldGraph.asset";
        private const string ParticleMaterialPath = "Assets/Game/Art/Shapes/EmberParticles.mat";
        private const string TombZone = "zone_forgotten_tomb";
        private const string ForestZone = "zone_withered_forest";
        private const string CapitalZone = "zone_ashen_capital";
        private const int GroundLayer = 9;

        private static readonly Color32 Shade = new Color32(13, 11, 15, 255);
        private static readonly Color32 ShadeRim = new Color32(34, 30, 38, 255);

        [MenuItem("Forgotten Kingdom/Scene/Apply Zone Ambience")]
        public static void Apply()
        {
            WorldScenes.Edit(() =>
            {
                var old = GameObject.Find(RootName);
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                }

                Build(new GameObject(RootName).transform);
            });
            Debug.Log("Zone ambience applied to the world.");
        }

        /// <summary>The recipe, also for a scene rebuilt from scratch.</summary>
        public static void Build(Transform root)
        {
            var art = GenerateArt();
            BuildAir(root);
            BuildForeground(root, art);
        }

        // ---------- air ----------

        private static void BuildAir(Transform root)
        {
            var host = new GameObject("ZoneAir");
            host.transform.SetParent(root, false);
            var ambience = host.AddComponent<ZoneAmbience>();
            var material = AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);

            var tomb = new[]
            {
                // Dust hanging in the still air: slow, pale, barely there.
                Motes(host, "TombDust", material, rate: 4f, life: (7f, 11f), size: (0.05f, 0.1f),
                    color: new Color(0.86f, 0.8f, 0.7f, 0.42f), speed: 0.08f, gravity: -0.004f, noise: 0.12f),
                // The odd crumb of grit from the vault, falling from above the frame.
                Grit(host, material)
            };
            var forest = new[]
            {
                Leaves(host, material),
                // Spores rising slowly out of the undergrowth.
                Motes(host, "ForestSpores", material, rate: 3f, life: (6f, 9f), size: (0.05f, 0.09f),
                    color: new Color(0.78f, 0.86f, 0.62f, 0.42f), speed: 0.06f, gravity: -0.012f, noise: 0.15f)
            };

            var capital = new[]
            {
                // Ash coming down over the capital, grey and slow, all the time.
                Motes(host, "CapitalAsh", material, rate: 7f, life: (8f, 12f), size: (0.05f, 0.11f),
                    color: new Color(0.7f, 0.66f, 0.64f, 0.5f), speed: 0.1f, gravity: 0.01f, noise: 0.2f),
                // The odd ember still rising from the ruins.
                Motes(host, "CapitalEmbers", material, rate: 1.2f, life: (4f, 6f), size: (0.04f, 0.07f),
                    color: new Color(1f, 0.55f, 0.25f, 0.7f), speed: 0.1f, gravity: -0.02f, noise: 0.25f)
            };

            var serialized = new SerializedObject(ambience);
            var zones = serialized.FindProperty("_zones");
            zones.arraySize = 3;
            Fill(zones.GetArrayElementAtIndex(0), TombZone, tomb);
            Fill(zones.GetArrayElementAtIndex(1), ForestZone, forest);
            Fill(zones.GetArrayElementAtIndex(2), CapitalZone, capital);
            var camera = Camera.main;
            serialized.FindProperty("_camera").objectReferenceValue = camera != null ? camera.transform : null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Fill(SerializedProperty entry, string zone, ParticleSystem[] systems)
        {
            entry.FindPropertyRelative("ZoneId").stringValue = zone;
            var list = entry.FindPropertyRelative("Systems");
            list.arraySize = systems.Length;
            for (var i = 0; i < systems.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = systems[i];
            }
        }

        /// <summary>A particle system filling the view (about 24 by 14 units) with a margin.</summary>
        private static ParticleSystem System(GameObject host, string name, Material material,
            Vector3 boxSize, Vector3 boxOffset, int maxParticles)
        {
            var child = new GameObject(name);
            child.transform.SetParent(host.transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 10f;
            // Already full when the zone begins, rather than filling up in front of the player.
            main.prewarm = true;
            // They ride with the camera and are always in view; never let culling pause them.
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;
            shape.position = boxOffset;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "VFXBack";
            renderer.sortingOrder = -5;
            return system;
        }

        private static ParticleSystem Motes(GameObject host, string name, Material material, float rate,
            (float min, float max) life, (float min, float max) size, Color color, float speed,
            float gravity, float noise)
        {
            var system = System(host, name, material, new Vector3(30f, 18f, 0.1f), Vector3.zero, 60);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.min, life.max);
            main.startSize = new ParticleSystem.MinMaxCurve(size.min, size.max);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, speed);
            main.startColor = color;
            main.gravityModifier = gravity;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var noiseModule = system.noise;
            noiseModule.enabled = true;
            noiseModule.strength = noise;
            noiseModule.frequency = 0.25f;
            noiseModule.scrollSpeed = 0.1f;
            FadeInOut(system, 0.25f, 0.75f);
            return system;
        }

        private static ParticleSystem Grit(GameObject host, Material material)
        {
            var system = System(host, "TombGrit", material, new Vector3(28f, 0.1f, 0.1f),
                new Vector3(0f, 8f, 0f), 12);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.045f);
            main.startSpeed = 0f;
            main.startColor = new Color(0.55f, 0.5f, 0.46f, 0.55f);
            main.gravityModifier = 0.12f;
            var emission = system.emission;
            emission.rateOverTime = 0.6f;
            FadeInOut(system, 0.05f, 0.85f);
            return system;
        }

        /// <summary>Dry leaves: flattened, turning as they fall, blown a little sideways.</summary>
        private static ParticleSystem Leaves(GameObject host, Material material)
        {
            var system = System(host, "ForestLeaves", material, new Vector3(30f, 0.1f, 0.1f),
                new Vector3(0f, 8.5f, 0f), 16);
            var main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(0.14f, 0.2f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(0.06f, 0.09f);
            main.startSizeZ = 1f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startSpeed = 0f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.46f, 0.34f, 0.2f, 0.85f),
                new Color(0.36f, 0.3f, 0.18f, 0.85f));
            main.gravityModifier = 0.035f;
            var emission = system.emission;
            emission.rateOverTime = 1.2f;
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.6f, 1.6f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noiseModule = system.noise;
            noiseModule.enabled = true;
            noiseModule.strength = 0.35f;
            noiseModule.frequency = 0.4f;
            FadeInOut(system, 0.1f, 0.85f);
            return system;
        }

        private static void FadeInOut(ParticleSystem system, float inAt, float outAt)
        {
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, inAt),
                    new GradientAlphaKey(1f, outAt), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
        }

        // ---------- foreground ----------

        private struct Art
        {
            public Sprite[] TombLow;
            public Sprite[] TombHigh;
            public Sprite[] ForestLow;
            public Sprite[] ForestHigh;
        }

        /// <summary>
        /// Shapes along the top and bottom of every room's frame, one every 16–24 units, taking
        /// turns; none in a boss arena, where nothing may stand between the player and the boss.
        /// The frame is the camera's room view, computed from the level as the camera computes it.
        /// </summary>
        private static void BuildForeground(Transform root, Art art)
        {
            var section = new GameObject("Foreground").transform;
            section.SetParent(root, false);
            var graph = AssetDatabase.LoadAssetAtPath<WorldGraph>(WorldGraphPath);
            if (graph == null)
            {
                Debug.LogWarning("No world graph; the foreground is left empty.");
                return;
            }

            Physics2D.SyncTransforms();
            var areas = new List<(string, Rect)>();
            var arenas = new HashSet<string>();
            var zoneOf = new Dictionary<string, string>();
            foreach (var area in graph.Areas)
            {
                if (area == null) { continue; }
                areas.Add((area.AreaId, area.WorldBounds));
                zoneOf[area.AreaId] = area.ZoneId;
                if (area.IsBossArena) { arenas.Add(area.AreaId); }
            }

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(1 << GroundLayer);
            var hits = new List<Collider2D>();
            var rooms = CameraRooms.Build(areas, rect =>
            {
                Physics2D.OverlapArea(rect.min, rect.max, filter, hits);
                var solids = new List<Bounds>(hits.Count);
                foreach (var hit in hits) { solids.Add(hit.bounds); }
                return solids;
            }, ViewWidth(), arenas.Contains);

            // A shape near an arena could slide into it with the parallax: keep a margin.
            var arenaRects = new List<Rect>();
            foreach (var area in graph.Areas)
            {
                if (area != null && area.IsBossArena)
                {
                    var r = area.WorldBounds;
                    arenaRects.Add(Rect.MinMaxRect(r.xMin - 4f, r.yMin - 4f, r.xMax + 4f, r.yMax + 4f));
                }
            }

            var random = new System.Random(58);
            var low = true;
            foreach (var room in rooms.Rooms)
            {
                var id = room.AreaIds[0];
                if (arenas.Contains(id) || !zoneOf.TryGetValue(id, out var zone))
                {
                    continue;
                }

                var forest = zone == ForestZone;
                var view = room.View;
                for (var x = view.xMin + 6f + (float)random.NextDouble() * 4f; x < view.xMax - 5f;
                     x += 16f + (float)random.NextDouble() * 8f)
                {
                    var set = low ? (forest ? art.ForestLow : art.TombLow) : (forest ? art.ForestHigh : art.TombHigh);
                    var sprite = set[random.Next(set.Length)];
                    // Low shapes rise just over the floor at the bottom of the frame; high ones
                    // hang in from above its top.
                    var floor = low ? FloorAt(x, view) : null;
                    if (low && floor == null)
                    {
                        continue;
                    }

                    var y = low ? floor.Value - LowSink : view.yMax + 0.4f;
                    if (arenaRects.Exists(r => r.Contains(new Vector2(x, y))))
                    {
                        continue;
                    }

                    Place(section, sprite, new Vector2(x, y), random.NextDouble() < 0.5);
                    low = !low;
                }
            }
        }

        /// <summary>How deep a low shape is sunk into the floor it stands on; the rest rises over it.</summary>
        private const float LowSink = 0.35f;

        /// <summary>
        /// The lowest floor near x inside the view — the ground at the bottom of the frame, not a
        /// ledge or the top of a pillar — or null when there is none low in the frame. A pillar
        /// hides the floor it stands on from a ray, so a little either side is tried too.
        /// </summary>
        private static float? FloorAt(float x, Rect view)
        {
            var lowest = float.MaxValue;
            foreach (var offset in new[] { 0f, -1.5f, 1.5f })
            {
                foreach (var hit in Physics2D.RaycastAll(new Vector2(x + offset, view.yMax), Vector2.down,
                             view.height, 1 << GroundLayer))
                {
                    if (hit.normal.y > 0.7f) { lowest = Mathf.Min(lowest, hit.point.y); }
                }
            }

            return lowest < view.yMin + 3f ? lowest : (float?)null;
        }

        /// <summary>The camera's view width at 16:9, so rooms join as the camera joins them.</summary>
        private static float ViewWidth()
        {
            var camera = Camera.main;
            return camera != null ? camera.orthographicSize * 2f * 16f / 9f : 21f;
        }

        private static void Place(Transform parent, Sprite sprite, Vector2 position, bool flip)
        {
            var shape = new GameObject(sprite != null ? sprite.name : "fg");
            shape.transform.SetParent(parent, false);
            shape.transform.position = new Vector3(position.x, position.y, 0f);
            var renderer = shape.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.flipX = flip;
            renderer.sortingLayerName = "Foreground";
            shape.AddComponent<ForegroundSilhouette>();
        }

        // ---------- art ----------

        private static Art GenerateArt()
        {
            PlaceholderImport.EnsureFolder(ArtFolder);
            return new Art
            {
                TombLow = new[] { Write("env_fg_tomb_column", Column(), false) , Write("env_fg_tomb_rubble", Rubble(), false) },
                TombHigh = new[] { Write("env_fg_tomb_chains", Chains(), true) },
                ForestLow = new[] { Write("env_fg_forest_roots", Roots(), false) },
                ForestHigh = new[] { Write("env_fg_forest_branch", Branch(), true) }
            };
        }

        private static Sprite Write(string name, Canvas canvas, bool hangs)
        {
            var path = $"{ArtFolder}/{name}.png";
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            // Low shapes stand on their base; high ones hang from their top edge.
            PlaceholderImport.ConfigureImporter(path, new Vector2(0.5f, hangs ? 1f : 0f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A broken column: a shaft snapped off at an angle, with fallen stone at its foot.</summary>
        private static Canvas Column()
        {
            var c = new Canvas(280, 320);
            c.Thick(new Vector2(140f, 0f), new Vector2(140f, 250f), 96f, 84f, Shade);
            c.Triangle(new Vector2(98f, 250f), new Vector2(182f, 250f), new Vector2(170f, 300f), Shade);
            c.Triangle(new Vector2(98f, 250f), new Vector2(130f, 250f), new Vector2(112f, 276f), Shade);
            c.Thick(new Vector2(92f, 250f), new Vector2(92f, 10f), 3f, 3f, ShadeRim);
            c.Ellipse(new Vector2(60f, 18f), 50f, 22f, Shade);
            c.Ellipse(new Vector2(225f, 14f), 44f, 18f, Shade);
            c.Ellipse(new Vector2(140f, 8f), 130f, 14f, Shade);
            return c;
        }

        private static Canvas Rubble()
        {
            var c = new Canvas(340, 150);
            var random = new System.Random(5801);
            for (var i = 0; i < 9; i++)
            {
                var x = 30f + (float)random.NextDouble() * 280f;
                var r = 22f + (float)random.NextDouble() * 34f;
                c.Ellipse(new Vector2(x, r * 0.6f), r, r * 0.75f, Shade);
            }

            c.Ellipse(new Vector2(170f, 10f), 165f, 16f, Shade);
            return c;
        }

        /// <summary>Two chains hanging from the vault, one longer, each ending in a hook.</summary>
        private static Canvas Chains()
        {
            var c = new Canvas(160, 480);
            foreach (var (x, length) in new[] { (55f, 440f), (110f, 330f) })
            {
                for (var y = 480f; y > 480f - length; y -= 22f)
                {
                    var wide = ((int)(y / 22f)) % 2 == 0;
                    c.Ellipse(new Vector2(x, y - 11f), wide ? 9f : 4.5f, 12f, Shade);
                }

                var end = new Vector2(x, 480f - length);
                c.Thick(end, end + new Vector2(0f, -22f), 6f, 6f, Shade);
                c.Thick(end + new Vector2(0f, -22f), end + new Vector2(14f, -12f), 6f, 4f, Shade);
            }

            return c;
        }

        /// <summary>A mound of roots breaking the ground, fingers reaching out along it.</summary>
        private static Canvas Roots()
        {
            var c = new Canvas(400, 230);
            c.Ellipse(new Vector2(200f, 20f), 180f, 60f, Shade);
            var random = new System.Random(5802);
            for (var i = 0; i < 7; i++)
            {
                // Roots crawl out along the ground more than they rise: low arcs, tips outward.
                var side = i % 2 == 0 ? -1f : 1f;
                var start = new Vector2(200f + side * (20f + (float)random.NextDouble() * 60f), 55f);
                var bend = start + new Vector2(side * (50f + (float)random.NextDouble() * 70f),
                    25f + (float)random.NextDouble() * 55f);
                var tip = bend + new Vector2(side * (40f + (float)random.NextDouble() * 50f),
                    -10f - (float)random.NextDouble() * 35f);
                c.Thick(start, bend, 24f, 12f, Shade);
                c.Thick(bend, tip, 12f, 1.5f, Shade);
            }

            return c;
        }

        /// <summary>A dead branch reaching in from the top of the frame, twigs and a few old leaves.</summary>
        private static Canvas Branch()
        {
            var c = new Canvas(480, 330);
            var start = new Vector2(0f, 330f);
            var mid = new Vector2(230f, 210f);
            var tip = new Vector2(460f, 170f);
            c.Thick(start, mid, 44f, 24f, Shade);
            c.Thick(mid, tip, 24f, 3f, Shade);
            var random = new System.Random(5803);
            for (var i = 0; i < 7; i++)
            {
                var along = 0.2f + 0.75f * (float)random.NextDouble();
                var from = along < 0.5f ? Vector2.Lerp(start, mid, along * 2f) : Vector2.Lerp(mid, tip, (along - 0.5f) * 2f);
                // Twigs fork off forward and down at a slant, each ending in a few dry leaves.
                var twig = from + new Vector2(35f + (float)random.NextDouble() * 55f, -25f - (float)random.NextDouble() * 45f);
                var fork = twig + new Vector2(-10f - (float)random.NextDouble() * 20f, -15f - (float)random.NextDouble() * 20f);
                c.Thick(from, twig, 8f, 2f, Shade);
                c.Thick(Vector2.Lerp(from, twig, 0.6f), fork, 4f, 1.5f, Shade);
                foreach (var leaf in new[] { twig, fork, twig + new Vector2(8f, 6f) })
                {
                    c.Ellipse(leaf + new Vector2((float)random.NextDouble() * 6f, -4f), 8f, 4f, Shade);
                }
            }

            return c;
        }
    }
}
