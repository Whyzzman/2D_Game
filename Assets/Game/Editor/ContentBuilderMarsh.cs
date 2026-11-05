using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Plague Marsh built from its text (the fifth level, MARSH.md, `Levels/marsh.txt`): east of the
    /// Village, through the old marsh road. The layout gives the ground, the finds, the enemies, the altars
    /// and the graph; this file adds the Plague Stag's lair — sealed behind the hero while he fights — and
    /// the bog's green light. One root, <c>Level/PlagueMarsh</c>, replaced whole on every build.
    /// Decided by Claude under the user's standing approval (2026-11-05: «five levels, finished»).
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string MarshLayoutFile = "marsh.txt";
        public const string MarshRootName = "PlagueMarsh";
        public const string MarshLairRoom = "room_marsh_stag_lair";
        public const string MarshArtFolder = "Marsh";

        private static LevelZone _marshLayout;
        private static bool _marshPainted;

        public static LevelZone MarshLayout => _marshLayout ?? (_marshLayout = LoadLayout(MarshLayoutFile));

        public static Vector2 MarshSpot(string name) => SpotFeet(MarshLayout, name);

        public static BossEncounter BuildMarsh(Transform level)
        {
            _marshLayout = null;
            if (_shapes == null) { _shapes = ProductionSpriteArt.Generate(); }
            if (_sprite == null) { _sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath); }
            if (_material == null) { _material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath); }

            var old = level.Find(MarshRootName);
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var zone = MarshLayout;
            var root = Section(level, MarshRootName);
            var context = LevelContext.FromScene();
            _marshPainted = HasPaintedArt(MarshArtFolder);
            foreach (var problem in BuildLayout(zone, root, MarshLayoutBlock, context, _marshPainted))
            {
                Debug.LogError($"Marsh layout: {problem}");
            }

            var lair = root.Find(MarshLairRoom);
            var encounter = BuildStagLair(zone, lair, context);
            if (_marshPainted)
            {
                PlacePaintedArt(MarshArtFolder, zone, root);
            }

            return encounter;
        }

        private static GameObject MarshLayoutBlock(Transform parent, string name, Rect world, LevelCell kind)
        {
            if (_marshPainted)
            {
                return PaintedSolid(parent, name, world, MarshArtFolder, MarshLayout);
            }

            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.position = world.center;
            block.layer = GroundLayer;
            var collider = block.AddComponent<BoxCollider2D>();
            collider.size = world.size;
            var visual = FindOrCreateChild(block, "Visual");
            visual.transform.localScale = new Vector3(world.width, world.height, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _material;
            renderer.color = kind == LevelCell.Ledge ? new Color(0.2f, 0.17f, 0.12f) : new Color(0.12f, 0.15f, 0.1f);
            SetSorting(renderer, "GameplayEnvironment", 0);
            return block;
        }

        /// <summary>The Stag's lair: the way in shut behind the hero when the fight starts; his fall gives the mask.</summary>
        private static BossEncounter BuildStagLair(LevelZone zone, Transform lair, LevelContext context)
        {
            var section = Section(lair, "Stag");
            var room = zone.WorldBounds(zone.Room(MarshLairRoom));
            var floor = MarshSpot("plague-stag").y;
            var barrier = new GameObject("StagBarrier");
            barrier.transform.SetParent(section);
            barrier.transform.position = new Vector2(room.xMin + 0.5f, floor + 2f);
            barrier.layer = GroundLayer;
            barrier.AddComponent<BoxCollider2D>().size = new Vector2(1f, 4f);
            var roots = FindOrCreateChild(barrier, "Visual");
            roots.transform.localScale = new Vector3(1f, 4f, 1f);
            var renderer = roots.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _material;
            renderer.color = new Color(0.16f, 0.2f, 0.1f);
            SetSorting(renderer, "GameplayEnvironment", 2);
            barrier.SetActive(false);

            BuildCapitalEncounter(section, StagPrefabName, "StagEncounter", barrier, context.Progress, context.Respawner,
                MarshSpot("plague-stag") + new Vector2(0f, 1.4f), new Vector2(room.xMin + 4f, floor + 2f),
                StagRewardItemId, "Чумна маска", 1200, 2200);

            var lights = Section(section, "LairLights");
            foreach (var share in new[] { 0.25f, 0.6f, 0.9f })
            {
                var host = LightChild(lights, "BogLight", new Vector2(Mathf.Lerp(room.xMin, room.xMax, share), floor + 0.5f));
                AddLight(host, Light2D.LightType.Point, new Color(0.6f, 0.95f, 0.35f), 0.9f, 9f, 1.5f, 0.45f);
            }

            return section.GetComponentInChildren<BossEncounter>();
        }

        /// <summary>The marsh's own lights when it is rebuilt in a finished world: altars and the boss.</summary>
        public static void LightMarsh(Transform marsh)
        {
            BuildCheckpointLights(marsh);
            BuildBossLights(marsh);
        }

        /// <summary>The marsh's ambient light and sound: green from the water, frogs and wind.</summary>
        public static void ConfigureMarshAtmosphere()
        {
            var lighting = Object.FindAnyObjectByType<ZoneLighting>(FindObjectsInactive.Include);
            if (lighting != null)
            {
                var serialized = new SerializedObject(lighting);
                var entry = UpsertZone(serialized.FindProperty("_zones"), "_zoneId", MarshZoneId);
                WriteZoneLight(entry, MarshZoneId, new Color(0.66f, 0.82f, 0.6f), 0.78f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var ambience = Object.FindAnyObjectByType<AmbienceController>(FindObjectsInactive.Include);
            if (ambience != null)
            {
                var serialized = new SerializedObject(ambience);
                var palette = UpsertZone(serialized.FindProperty("_palettes"), "_zoneId", MarshZoneId);
                palette.FindPropertyRelative("_zoneId").stringValue = MarshZoneId;
                palette.FindPropertyRelative("_bed").enumValueIndex = (int)AudioId.AmbienceForestBase;
                var details = palette.FindPropertyRelative("_details");
                var sounds = new[] { AudioId.AmbienceDistantBeast, AudioId.AmbienceBranch, AudioId.AmbienceWind };
                details.arraySize = sounds.Length;
                for (var i = 0; i < sounds.Length; i++)
                {
                    details.GetArrayElementAtIndex(i).enumValueIndex = (int)sounds[i];
                }

                palette.FindPropertyRelative("_minimumGap").floatValue = 5f;
                palette.FindPropertyRelative("_maximumGap").floatValue = 15f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
