using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Gives every character frame a normal map (visual item 3, the user's choice of 2026-09-28),
    /// so a candle to one side lights that side of the hero and a spell lights the front of the
    /// enemy it flies at.
    ///
    /// The volume is read from the silhouette: height rises from the outline inward over a few
    /// pixels, so the figure is a rounded shape lit from wherever the light is, and a trace of the
    /// painted values adds folds without letting texture noise take over. Made from whatever is at
    /// the frame's path, so hand-drawn frames dropped in later get their normals the same way.
    ///
    /// Normals live in a parallel tree, <c>Art/CharacterNormals</c>, never next to the frames, so
    /// nothing that lists a state's frames ever counts them. Each is attached to its sprite as the
    /// secondary texture <c>_NormalMap</c>, which the sprite-lit shader and the atlases both read.
    /// </summary>
    public static class CharacterNormals
    {
        public const string FrameRoot = "Assets/Game/Art/Characters";
        public const string NormalRoot = "Assets/Game/Art/CharacterNormals";
        public const string SecondaryName = "_NormalMap";

        /// <summary>Pixels over which the edge rounds off into the body.</summary>
        private const int Bevel = 5;
        private const float Steepness = 2.2f;
        private const float Detail = 0.35f;

        /// <summary>Every frame under the characters' folder.</summary>
        public static void AttachAll()
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { FrameRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".png"))
                {
                    paths.Add(path);
                }
            }

            AttachAll(paths);
        }

        public static void AttachAll(IEnumerable<string> framePaths)
        {
            var normals = new List<(string frame, string normal)>();
            foreach (var frame in framePaths)
            {
                normals.Add((frame, NormalPathFor(frame)));
            }

            // Folders first: creating one while asset editing is paused is not reliable.
            var folders = new HashSet<string>();
            foreach (var (_, normal) in normals)
            {
                folders.Add(Path.GetDirectoryName(normal)?.Replace('\\', '/'));
            }

            foreach (var folder in folders)
            {
                PlaceholderImport.EnsureFolder(folder);
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (frame, normal) in normals)
                {
                    PlaceholderImport.WriteIfChanged(normal, Compute(File.ReadAllBytes(frame)));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (frame, normal) in normals)
                {
                    ConfigureNormal(normal);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (frame, normal) in normals)
                {
                    Attach(frame, AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public static string NormalPathFor(string framePath) =>
            NormalRoot + framePath.Substring(FrameRoot.Length);

        /// <summary>The normal map for one frame's PNG bytes, as PNG bytes.</summary>
        public static byte[] Compute(byte[] framePng)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(framePng);
            var width = source.width;
            var height = source.height;
            var pixels = source.GetPixels32();
            Object.DestroyImmediate(source);

            // Distance inward from the outline, capped at the bevel: a chamfer pass each way.
            var distance = new float[width * height];
            const float far = 1000f;
            for (var i = 0; i < distance.Length; i++)
            {
                distance[i] = pixels[i].a > 40 ? far : 0f;
            }

            Chamfer(distance, width, height);

            var heightField = new float[width * height];
            for (var i = 0; i < heightField.Length; i++)
            {
                if (pixels[i].a <= 40)
                {
                    continue;
                }

                var round = Mathf.SmoothStep(0f, 1f, Mathf.Min(distance[i], Bevel) / Bevel);
                var value = (pixels[i].r * 0.3f + pixels[i].g * 0.59f + pixels[i].b * 0.11f) / 255f;
                heightField[i] = round + value * Detail;
            }

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var output = new Color32[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (pixels[i].a <= 40)
                    {
                        output[i] = new Color32(128, 128, 255, 0);
                        continue;
                    }

                    float H(int px, int py) =>
                        heightField[Mathf.Clamp(py, 0, height - 1) * width + Mathf.Clamp(px, 0, width - 1)];
                    var dx = (H(x + 1, y) - H(x - 1, y)) * 0.5f * Steepness;
                    var dy = (H(x, y + 1) - H(x, y - 1)) * 0.5f * Steepness;
                    var n = new Vector3(-dx, -dy, 1f).normalized;
                    output[i] = new Color32((byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f), pixels[i].a);
                }
            }

            result.SetPixels32(output);
            result.Apply();
            var png = result.EncodeToPNG();
            Object.DestroyImmediate(result);
            return png;
        }

        private static void Chamfer(float[] d, int width, int height)
        {
            const float straight = 1f;
            const float diagonal = 1.4142f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var i = y * width + x;
                    if (d[i] == 0f) { continue; }
                    var best = d[i];
                    if (x > 0) { best = Mathf.Min(best, d[i - 1] + straight); }
                    if (y > 0) { best = Mathf.Min(best, d[i - width] + straight); }
                    if (x > 0 && y > 0) { best = Mathf.Min(best, d[i - width - 1] + diagonal); }
                    if (x < width - 1 && y > 0) { best = Mathf.Min(best, d[i - width + 1] + diagonal); }
                    if (x == 0 || y == 0 || x == width - 1) { best = Mathf.Min(best, 1f); }
                    d[i] = best;
                }
            }

            for (var y = height - 1; y >= 0; y--)
            {
                for (var x = width - 1; x >= 0; x--)
                {
                    var i = y * width + x;
                    if (d[i] == 0f) { continue; }
                    var best = d[i];
                    if (x < width - 1) { best = Mathf.Min(best, d[i + 1] + straight); }
                    if (y < height - 1) { best = Mathf.Min(best, d[i + width] + straight); }
                    if (x < width - 1 && y < height - 1) { best = Mathf.Min(best, d[i + width + 1] + diagonal); }
                    if (x > 0 && y < height - 1) { best = Mathf.Min(best, d[i + width - 1] + diagonal); }
                    if (y == height - 1) { best = Mathf.Min(best, 1f); }
                    d[i] = best;
                }
            }
        }

        private static void ConfigureNormal(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.NormalMap && !importer.mipmapEnabled
                && !importer.convertToNormalmap && importer.filterMode == FilterMode.Bilinear
                && importer.wrapMode == TextureWrapMode.Clamp)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        private static void Attach(string framePath, Texture2D normal)
        {
            var importer = AssetImporter.GetAtPath(framePath) as TextureImporter;
            if (importer == null || normal == null)
            {
                return;
            }

            var current = importer.secondarySpriteTextures;
            if (current != null && current.Length == 1 && current[0].name == SecondaryName
                && current[0].texture == normal)
            {
                return;
            }

            importer.secondarySpriteTextures = new[]
            {
                new SecondarySpriteTexture { name = SecondaryName, texture = normal }
            };
            importer.SaveAndReimport();
        }
    }
}
