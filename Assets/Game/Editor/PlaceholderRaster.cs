using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// A tiny software rasteriser plus the import rules every placeholder frame shares.
    ///
    /// It exists so the player, the enemies and the boss are drawn by the same code. They are the
    /// same kind of figure at different scales, and having three copies of "draw a limb" would let
    /// them drift apart exactly where the brief asks them to look like one world.
    /// </summary>
    public sealed class Canvas
    {
        public readonly int Width;
        public readonly int Height;
        private readonly Color32[] _pixels;

        public Canvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color32[width * height];
        }

        /// <summary>
        /// Textures are addressed bottom-up, which matches poses being measured from the feet: a
        /// y of zero is the ground line in both.
        /// </summary>
        public void Set(int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            _pixels[y * Width + x] = color;
        }

        public void Ellipse(Vector2 centre, float rx, float ry, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(centre.x - rx));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(centre.x + rx));
            var minY = Mathf.Max(0, Mathf.FloorToInt(centre.y - ry));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(centre.y + ry));
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = (x + 0.5f - centre.x) / Mathf.Max(0.001f, rx);
                    var dy = (y + 0.5f - centre.y) / Mathf.Max(0.001f, ry);
                    if (dx * dx + dy * dy <= 1f)
                    {
                        Set(x, y, color);
                    }
                }
            }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))));
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var ab = Side(p, a, b);
                    var bc = Side(p, b, c);
                    var ca = Side(p, c, a);
                    if ((ab >= 0f && bc >= 0f && ca >= 0f) || (ab <= 0f && bc <= 0f && ca <= 0f))
                    {
                        Set(x, y, color);
                    }
                }
            }
        }

        /// <summary>The alpha already at a pixel; zero outside the canvas.</summary>
        public byte Alpha(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height ? _pixels[y * Width + x].a : (byte)0;

        /// <summary>True where something has already been drawn.</summary>
        public bool IsOpaque(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height && _pixels[y * Width + x].a > 0;

        /// <summary>
        /// Paints only where the canvas is already solid. Detail passes — mortar, speckle, wear —
        /// use this so they stay inside the shape instead of drawing a grid across empty space.
        /// </summary>
        public void RectOver(float x0, float y0, float x1, float y1, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1)));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(x0, x1)) - 1);
            var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1)));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(y0, y1)) - 1);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    if (IsOpaque(x, y))
                    {
                        Set(x, y, color);
                    }
                }
            }
        }

        /// <summary>
        /// Lerps what is already there towards a colour. Light, haze and grime are all this:
        /// they change what the stone looks like without replacing it, and crucially without
        /// punching a transparent hole through the image the way a plain fill would.
        /// </summary>
        public void TintOver(float x0, float y0, float x1, float y1, Color32 color, float strength)
        {
            var amount = Mathf.Clamp01(strength);
            if (amount <= 0f)
            {
                return;
            }

            var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1)));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(x0, x1)) - 1);
            var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1)));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(y0, y1)) - 1);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var index = y * Width + x;
                    var existing = _pixels[index];
                    if (existing.a == 0)
                    {
                        continue;
                    }

                    var mixed = Color32.Lerp(existing, color, amount);
                    // Opacity is the base's; light must never make stone see-through.
                    mixed.a = existing.a;
                    _pixels[index] = mixed;
                }
            }
        }

        /// <summary>An axis-aligned rectangle: stone courses, trim bands, mortar lines.</summary>
        public void Rect(float x0, float y0, float x1, float y1, Color32 color)
        {
            var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1)));
            var maxX = Mathf.Min(Width - 1, Mathf.CeilToInt(Mathf.Max(x0, x1)) - 1);
            var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1)));
            var maxY = Mathf.Min(Height - 1, Mathf.CeilToInt(Mathf.Max(y0, y1)) - 1);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    Set(x, y, color);
                }
            }
        }

        /// <summary>
        /// Wraps horizontally instead of clipping, so a feature that runs off the right edge
        /// reappears on the left. Tiling textures need this or every seam shows.
        /// </summary>
        public void SetWrapped(int x, int y, Color32 color)
        {
            if (y < 0 || y >= Height)
            {
                return;
            }

            Set(((x % Width) + Width) % Width, y, color);
        }

        /// <summary>A quad, as two triangles; cloaks, shields and capes are all quads.</summary>
        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            Triangle(a, b, c, color);
            Triangle(a, c, d, color);
        }

        /// <summary>A line with a start and an end width, which is all limbs and torsos need.</summary>
        public void Thick(Vector2 a, Vector2 b, float widthA, float widthB, Color32 color)
        {
            var steps = Mathf.CeilToInt(Vector2.Distance(a, b)) + 1;
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var p = Vector2.Lerp(a, b, t);
                var r = Mathf.Lerp(widthA, widthB, t) * 0.5f;
                Ellipse(p, r, r, color);
            }
        }

        /// <summary>
        /// A two-segment limb. Angles are degrees with zero hanging straight down, and positive
        /// swinging forward towards +x — the facing direction, since every pose is drawn facing
        /// right and the motor mirrors the whole visual.
        /// </summary>
        public Vector2 Limb(Vector2 origin, float upperAngle, float upperLength, float lowerAngle,
            float lowerLength, float width, Color32 color)
        {
            // Rotate turns clockwise, which reads as forward for the upward torso and head
            // offsets. Limbs hang downward, so their angles are negated here and only here.
            var knee = origin + Rotate(new Vector2(0f, -upperLength), -upperAngle);
            var end = knee + Rotate(new Vector2(0f, -lowerLength), -(upperAngle + lowerAngle));
            Thick(origin, knee, width, width * 0.85f, color);
            Thick(knee, end, width * 0.85f, width * 0.7f, color);
            return end;
        }

        public void Fade(float alpha)
        {
            var scale = Mathf.Clamp01(alpha);
            for (var i = 0; i < _pixels.Length; i++)
            {
                if (_pixels[i].a > 0)
                {
                    _pixels[i].a = (byte)Mathf.RoundToInt(_pixels[i].a * scale);
                }
            }
        }

        /// <summary>
        /// The finishing pass every character gets: a soft dark outline around the silhouette and
        /// a cool rim along the edges that face the light, which falls from behind and above.
        ///
        /// The Tomb is dark and so are the figures in it; without this they sank into the stone
        /// and read as smudges at 144 px. The outline separates the shape from the background, the
        /// rim gives it a lit edge, and neither changes the shape itself — so the silhouette tests
        /// of the Visual Bible still see the same figure.
        /// </summary>
        public void Silhouette(Color32 outline, Color32 rim, float rimStrength, float lift = 0f)
        {
            var source = (Color32[])_pixels.Clone();
            bool Solid(int x, int y) =>
                x >= 0 && y >= 0 && x < Width && y < Height && source[y * Width + x].a > 40;

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var index = y * Width + x;
                    var pixel = source[index];
                    if (pixel.a <= 40)
                    {
                        // Outline: one firm ring and one faint one, so the edge is crisp but not
                        // a cartoon line.
                        var near = Solid(x - 1, y) || Solid(x + 1, y) || Solid(x, y - 1)
                            || Solid(x, y + 1);
                        var far = !near && (Solid(x - 2, y) || Solid(x + 2, y) || Solid(x, y - 2)
                            || Solid(x, y + 2) || Solid(x - 1, y - 1) || Solid(x + 1, y + 1)
                            || Solid(x - 1, y + 1) || Solid(x + 1, y - 1));
                        if (near || far)
                        {
                            var ring = outline;
                            ring.a = (byte)(outline.a * (near ? 1f : 0.35f));
                            _pixels[index] = ring;
                        }

                        continue;
                    }

                    var lit = pixel;
                    if (lift > 0f)
                    {
                        lit = Color32.Lerp(lit, new Color32(255, 255, 255, lit.a), lift);
                        lit.a = pixel.a;
                    }

                    // Rim: the edge that faces back and up — the side away from the facing
                    // direction, since every pose is drawn facing right.
                    var edge = !Solid(x - 1, y) || !Solid(x, y + 1) || !Solid(x - 1, y + 1);
                    var inner = !edge && (!Solid(x - 2, y) || !Solid(x, y + 2));
                    if (edge || inner)
                    {
                        var mixed = Color32.Lerp(lit, rim, rimStrength * (edge ? 1f : 0.45f));
                        mixed.a = pixel.a;
                        lit = mixed;
                    }

                    _pixels[index] = lit;
                }
            }
        }

        public byte[] EncodePng()
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(_pixels);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static float Side(Vector2 p, Vector2 a, Vector2 b) =>
            (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

        /// <summary>Clockwise rotation: positive carries an upward vector forward, towards +x.</summary>
        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            var r = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(r);
            var sin = Mathf.Sin(r);
            return new Vector2(v.x * cos + v.y * sin, -v.x * sin + v.y * cos);
        }
    }

    /// <summary>Writing frames to disk and importing them the way the Visual Bible requires.</summary>
    public static class PlaceholderImport
    {
        public const float PixelsPerUnit = 100f;
        private const string ArtRoot = "Assets/Game/Art/";

        /// <summary>Where hand-drawn replacements are dropped; see gamedesign/ART_ASSET_LIST.md.</summary>
        public const string FinalRoot = ArtRoot + "Source/Final";

        /// <summary>How many generated files were replaced by final art in this run.</summary>
        public static int FinalArtUsed { get; set; }

        /// <summary>The final-art path mirroring a generated one, or null outside Art/.</summary>
        public static string FinalPathFor(string path)
        {
            if (!path.StartsWith(ArtRoot) || path.StartsWith(ArtRoot + "Source/"))
            {
                return null;
            }

            return $"{FinalRoot}/{path.Substring(ArtRoot.Length)}";
        }

        /// <summary>
        /// Writes only when the bytes actually differ, so a rebuild never churns art an artist has
        /// already replaced, and never rewrites a file that has not changed.
        /// </summary>
        public static void WriteIfChanged(string path, byte[] png)
        {
            // Hand-drawn art wins: a file at the mirrored path under Source/Final replaces the
            // procedural bytes, so the sprite keeps its GUID and every animation set, prefab and
            // scene that points at it shows the final art without a single reference changing.
            var final = FinalPathFor(path);
            if (final != null && File.Exists(Path.GetFullPath(final)))
            {
                png = File.ReadAllBytes(Path.GetFullPath(final));
                FinalArtUsed++;
            }

            var full = Path.GetFullPath(path);
            if (File.Exists(full) && BytesEqual(File.ReadAllBytes(full), png))
            {
                return;
            }

            File.WriteAllBytes(full, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Pivot at the centre of the feet, as the Visual Bible requires: it is what lets a pose
        /// change height without the character appearing to sink into the floor. PPU 100,
        /// bilinear, no mipmaps — the same rules for every actor.
        /// </summary>
        public static void ConfigureImporter(string path, Vector2? pivot = null,
            bool repeat = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            // Characters pivot at the feet; tiles and overlays pivot centre so they stretch
            // symmetrically. A tile also wraps, or its tiled draw mode shows a seam at every edge.
            var wanted = pivot ?? new Vector2(0.5f, 0f);
            var wantedWrap = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var needsChange = importer.textureType != TextureImporterType.Sprite
                || importer.spritePixelsPerUnit != PixelsPerUnit
                || importer.filterMode != FilterMode.Bilinear
                || importer.mipmapEnabled
                || importer.wrapMode != wantedWrap
                || settings.spriteAlignment != (int)SpriteAlignment.Custom
                || settings.spritePivot != wanted;
            if (!needsChange)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = wantedWrap;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = wanted;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
