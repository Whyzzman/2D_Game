using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The pictures of the world map (item 2 of the 15-zone list): each region's island and card,
    /// each room's painted interior, the fog and the soft glow. They are drawn by the approved
    /// mockup's own code (design/ui/world-map/*.js, rendered by tools/map-art/render.sh) and gathered
    /// here by the generator; the graph points at this asset, the map asks it by id.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Map Art", fileName = "MapArt")]
    public sealed class MapArt : ScriptableObject
    {
        /// <summary>
        /// An island picture is 460 × 380 map pixels with the island's own point at (230, 250): what
        /// the world map places on the region's position.
        /// </summary>
        public static readonly Vector2 IslandSize = new Vector2(460f, 380f);
        public static readonly Vector2 IslandAnchor = new Vector2(230f, 250f);

        [Serializable]
        public struct Entry
        {
            public string Id;
            public Texture2D Texture;
        }

        [SerializeField] private Entry[] _islands = Array.Empty<Entry>();
        [SerializeField] private Entry[] _cards = Array.Empty<Entry>();
        [SerializeField] private Entry[] _rooms = Array.Empty<Entry>();
        [SerializeField] private Texture2D _fog;
        [SerializeField] private Texture2D _glow;

        public Texture2D Fog => _fog;
        public Texture2D Glow => _glow;
        public Texture2D Island(string art) => Find(_islands, art);
        public Texture2D Card(string art) => Find(_cards, art);
        public Texture2D Room(string areaId) => Find(_rooms, areaId);
        public int RoomCount => _rooms.Length;

        private static Texture2D Find(Entry[] entries, string id)
        {
            foreach (var entry in entries)
            {
                if (entry.Id == id) { return entry.Texture; }
            }

            return null;
        }
    }
}
