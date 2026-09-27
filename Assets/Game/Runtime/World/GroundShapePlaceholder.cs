using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Fills a <see cref="PolygonCollider2D"/>'s shape with flat stone so a slope in the sandbox
    /// can be seen; a stand-in until the level's own art covers such shapes.
    /// </summary>
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class GroundShapePlaceholder : MonoBehaviour
    {
        private const int PixelsPerUnit = 50;
        [SerializeField] private Color _color = new Color32(74, 64, 54, 255);

        private void Awake()
        {
            if (GetComponentInChildren<SpriteRenderer>() != null)
            {
                return;
            }

            var points = GetComponent<PolygonCollider2D>().points;
            if (points.Length < 3)
            {
                return;
            }

            var min = points[0];
            var max = points[0];
            foreach (var p in points)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            var width = Mathf.Max(2, Mathf.CeilToInt((max.x - min.x) * PixelsPerUnit));
            var height = Mathf.Max(2, Mathf.CeilToInt((max.y - min.y) * PixelsPerUnit));
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var clear = new Color(0f, 0f, 0f, 0f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var point = min + new Vector2((x + 0.5f) / PixelsPerUnit, (y + 0.5f) / PixelsPerUnit);
                    texture.SetPixel(x, y, Inside(points, point) ? _color : clear);
                }
            }

            texture.Apply();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = (min + max) * 0.5f;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static bool Inside(Vector2[] polygon, Vector2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > point.y) != (polygon[j].y > point.y)
                    && point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y)
                        / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
