using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Painted level art (the zone rework's pilot, the Tomb, 2026-10-04; approved look —
    /// design/levels/tomb-look). The painter (design/levels/painter, rendered by
    /// tools/level-art/render.sh) draws a zone from its text: the stone of the playing plane in
    /// 16 × 16 unit pieces, and each room's backdrop in two layers — the far one (mid), which slides
    /// with the camera, and the room's own wall (near) with see-through arches. Here the pieces are
    /// imported and put in the world; the blocks keep only their colliders.
    /// </summary>
    public static partial class ContentBuilder
    {
        public const string LevelArtRoot = "Assets/Game/Art/Levels";
        public const string PaintedRootName = "Painted";
        private const float TerrainPixelsPerUnit = 48f;
        private const float RoomPixelsPerUnit = 32f;

        /// <summary>The near wall carries the room's props: as sharp as the stone (tools/level-art/jobs.js NEAR_PPU).</summary>
        private const float NearPixelsPerUnit = 48f;
        private const float TerrainChunk = 16f;
        private const float RoomParallaxMargin = 3f;
        private const float RoomParallaxFactor = 0.15f;
        // The room's foreground (design/levels/painter/front.js): painted 2 units wider on each side,
        // it slides ahead of the camera — nearer than the play — by a fifth of its offset.
        private const float FrontMargin = 2f;
        private const float FrontParallaxFactor = -0.2f;
        private const float BreakablePad = 0.5f;

        /// <summary>The folder of a zone's painted art, e.g. «Tomb».</summary>
        private static string LevelArtFolder(string zone) => $"{LevelArtRoot}/{zone}";

        /// <summary>Whether a zone has painted art; without it the zone keeps its tiles.</summary>
        public static bool HasPaintedArt(string zone) =>
            Directory.Exists(LevelArtFolder(zone) + "/Terrain")
            && Directory.GetFiles(LevelArtFolder(zone) + "/Terrain", "*.png").Length > 0;

        /// <summary>Import settings for every painted piece: a sprite from its bottom-left corner.</summary>
        [MenuItem("Forgotten Kingdom/Levels/Refresh Painted Level Art")]
        public static void RefreshLevelArt()
        {
            if (!Directory.Exists(LevelArtRoot))
            {
                return;
            }

            AssetDatabase.Refresh();
            var count = 0;
            foreach (var file in Directory.GetFiles(LevelArtRoot, "*.png", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    continue;
                }

                var terrain = path.Contains("/Terrain/");
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var nearLayer = path.EndsWith("_near.png");
                importer.spritePixelsPerUnit = terrain ? TerrainPixelsPerUnit : nearLayer ? NearPixelsPerUnit : RoomPixelsPerUnit;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = nearLayer ? 4096 : 2048;
                // BC7 (CompressedHQ), 2026-11-11, KI-012: uncompressed the painted world held 1-2.4 GB of
                // textures and an 8 GB Mac paged, freezing frames for up to 1.3 s. Normal-quality DXT turned the
                // dark gradients into visible tiles and triangles; BC7 keeps them and takes a quarter of the memory.
                // The see-through near and front layers stay uncompressed: on their soft alpha BC7 left dotted blocks.
                var seeThrough = path.EndsWith("_near.png") || path.EndsWith("_front.png");
                importer.textureCompression = seeThrough
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                count++;
            }

            Debug.Log($"Painted level art refreshed: {count} pieces.");
        }

        /// <summary>
        /// Puts a zone's painted stone and room backdrops under <paramref name="root"/>. False when
        /// the zone has none (it keeps its tiles then).
        /// </summary>
        private static bool PlacePaintedArt(string zone, LevelZone layout, Transform root)
        {
            if (!HasPaintedArt(zone))
            {
                return false;
            }

            var painted = Section(root, PaintedRootName);
            var terrain = Section(painted, "Terrain");
            foreach (var file in Directory.GetFiles(LevelArtFolder(zone) + "/Terrain", "t_*.png"))
            {
                var parts = Path.GetFileNameWithoutExtension(file).Split('_');
                if (parts.Length != 3 || !int.TryParse(parts[1], out var ix) || !int.TryParse(parts[2], out var iy))
                {
                    continue;
                }

                var piece = PaintedSprite(terrain, file, new Vector2(ix * TerrainChunk, iy * TerrainChunk),
                    "GameplayEnvironment", -10);
                if (piece != null)
                {
                    piece.name = $"Stone_{ix}_{iy}";
                }
            }

            var rooms = Section(painted, "Rooms");
            for (var index = 0; index < layout.Rooms.Count; index++)
            {
                var room = layout.Rooms[index];
                var bounds = layout.WorldBounds(room);
                var holder = Section(rooms, "Backdrop_" + room.Id);

                // The far layer slides with the camera inside a mask the size of the room; each room
                // masks only its own sorting orders, so a neighbour's far layer never shows through.
                var order = index * 2 + 1;
                var far = PaintedSprite(holder, $"{LevelArtFolder(zone)}/Rooms/{room.Id}_mid.png",
                    bounds.min - Vector2.one * RoomParallaxMargin, "BackgroundMid", order);
                if (far != null)
                {
                    far.name = "Far";
                    far.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    var parallax = far.AddComponent<RoomParallax>();
                    SetFloat(parallax, "_factor", RoomParallaxFactor);
                    SetFloat(parallax, "_margin", RoomParallaxMargin);
                    var centre = new SerializedObject(parallax);
                    centre.FindProperty("_roomCentre").vector2Value = bounds.center;
                    centre.ApplyModifiedPropertiesWithoutUndo();

                    var mask = new GameObject("Mask");
                    mask.transform.SetParent(holder, false);
                    mask.transform.position = bounds.center;
                    var square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                    var spriteMask = mask.AddComponent<SpriteMask>();
                    spriteMask.sprite = square;
                    var size = square != null ? (Vector2)square.bounds.size : Vector2.one;
                    mask.transform.localScale = new Vector3(bounds.width / size.x, bounds.height / size.y, 1f);
                    spriteMask.isCustomRangeActive = true;
                    var layer = SortingLayer.NameToID("BackgroundMid");
                    spriteMask.backSortingLayerID = layer;
                    spriteMask.backSortingOrder = order - 1;
                    spriteMask.frontSortingLayerID = layer;
                    spriteMask.frontSortingOrder = order;
                }

                var near = PaintedSprite(holder, $"{LevelArtFolder(zone)}/Rooms/{room.Id}_near.png", bounds.min,
                    "BackgroundNear", 0);
                if (near != null)
                {
                    near.name = "Wall";
                }

                // Dark growth hanging from the vaults, in front of everything (user's request of
                // 2026-10-27: the mood of Hollow Knight, not its look). Only near the ceilings, so it
                // never lies over a fight.
                var front = PaintedSprite(holder, $"{LevelArtFolder(zone)}/Rooms/{room.Id}_front.png",
                    bounds.min - Vector2.one * FrontMargin, "Foreground", index);
                if (front != null)
                {
                    front.name = "Front";
                    var drift = front.AddComponent<RoomParallax>();
                    SetFloat(drift, "_factor", FrontParallaxFactor);
                    SetFloat(drift, "_margin", FrontMargin);
                    var centre = new SerializedObject(drift);
                    centre.FindProperty("_roomCentre").vector2Value = bounds.center;
                    centre.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return true;
        }

        /// <summary>
        /// A block of a painted zone: the painter draws the ground, so the block is only what the
        /// hero stands on. A secret's wall gets its own painted piece over the opening, so breaking
        /// it leaves the hole the stone around it was painted with.
        /// </summary>
        private static GameObject PaintedSolid(Transform parent, string name, Rect world, string zone,
            LevelZone layout)
        {
            var solid = new GameObject(name);
            solid.transform.SetParent(parent);
            solid.transform.position = world.center;
            solid.layer = GroundLayer;
            solid.AddComponent<BoxCollider2D>().size = world.size;
            if (name == "BreakableWall")
            {
                PlacePaintedBreakable(zone, layout, solid, world);
            }

            return solid;
        }

        /// <summary>The painted stone of a secret's wall, as the wall's own «Visual».</summary>
        private static void PlacePaintedBreakable(string zone, LevelZone layout, GameObject wall, Rect world)
        {
            var cell = layout.CellOf(world.min + Vector2.one * (layout.CellSize * 0.25f));
            var visual = PaintedSprite(wall.transform, $"{LevelArtFolder(zone)}/Breakables/b_{cell.x}_{cell.y}.png",
                world.min - Vector2.one * BreakablePad, "GameplayEnvironment", -9);
            if (visual == null)
            {
                Debug.LogError($"{zone}: no painted wall b_{cell.x}_{cell.y} — run tools/level-art/render.sh.");
                visual = new GameObject("Visual");
                visual.transform.SetParent(wall.transform, false);
                visual.AddComponent<SpriteRenderer>();
            }

            visual.name = "Visual";
        }

        private static GameObject PaintedSprite(Transform parent, string path, Vector2 corner, string layer, int order)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path.Replace('\\', '/'));
            if (sprite == null)
            {
                return null;
            }

            var piece = new GameObject(Path.GetFileNameWithoutExtension(path));
            piece.transform.SetParent(parent, false);
            piece.transform.position = corner;
            var renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = _material != null ? _material : AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            SetSorting(renderer, layer, order);
            return piece;
        }
    }
}
