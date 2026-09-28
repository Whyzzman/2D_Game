using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The training targets at the start of the Tomb (visual item 3, the user's choice of
    /// 2026-09-28): an old guard's practice dummy — a post, a sack of straw, a rusted breastplate
    /// and a dented helm, arms on a crossbar. The one that hits back holds a wooden sword.
    ///
    /// They used to be coloured rectangles. Drawn facing right with the pivot at the foot of the
    /// post, 100 PPU, under Characters so they get normal maps like everyone else.
    /// </summary>
    public static class PlaceholderDummyArt
    {
        public const string Folder = "Assets/Game/Art/Characters/Training";
        public const string DummyPath = Folder + "/training_dummy.png";
        public const string AttackerPath = Folder + "/training_attacker.png";

        private static readonly Color32 Wood = new Color32(0x4A, 0x3B, 0x2E, 0xFF);
        private static readonly Color32 WoodDark = new Color32(0x33, 0x28, 0x1F, 0xFF);
        private static readonly Color32 Straw = new Color32(0x7C, 0x68, 0x3E, 0xFF);
        private static readonly Color32 StrawDark = new Color32(0x5A, 0x4A, 0x2C, 0xFF);
        private static readonly Color32 Rope = new Color32(0x6A, 0x5A, 0x42, 0xFF);
        private static readonly Color32 Rust = new Color32(0x5E, 0x46, 0x36, 0xFF);
        private static readonly Color32 Steel = new Color32(0x50, 0x54, 0x5A, 0xFF);
        private static readonly Color32 SteelLit = new Color32(0x7A, 0x80, 0x88, 0xFF);
        private static readonly Color32 PracticeWood = new Color32(0x6E, 0x55, 0x38, 0xFF);

        public static (Sprite dummy, Sprite attacker) Generate()
        {
            PlaceholderImport.EnsureFolder(Folder);
            Write(DummyPath, Draw(armed: false));
            Write(AttackerPath, Draw(armed: true));
            return (AssetDatabase.LoadAssetAtPath<Sprite>(DummyPath),
                AssetDatabase.LoadAssetAtPath<Sprite>(AttackerPath));
        }

        private static void Write(string path, Canvas canvas)
        {
            PlaceholderImport.WriteIfChanged(path, canvas.EncodePng());
            PlaceholderImport.ConfigureImporter(path);
        }

        private static Canvas Draw(bool armed)
        {
            var c = new Canvas(180, 214);
            var rng = new System.Random(armed ? 6102 : 6101);
            const float cx = 90f;

            // The foot: two crossed boards, then the post up through the body to the head.
            c.Thick(new Vector2(cx - 34f, 3f), new Vector2(cx + 34f, 3f), 7f, 7f, WoodDark);
            c.Thick(new Vector2(cx - 18f, 8f), new Vector2(cx + 18f, 8f), 6f, 6f, Wood);
            c.Thick(new Vector2(cx, 4f), new Vector2(cx, 176f), 9f, 7f, Wood);

            // The crossbar arms, straw bursting out of the ends.
            c.Thick(new Vector2(cx - 50f, 142f), new Vector2(cx + 50f, 146f), 6f, 6f, Wood);
            foreach (var side in new[] { -1f, 1f })
            {
                var end = new Vector2(cx + side * 52f, 144f);
                for (var i = 0; i < 6; i++)
                {
                    c.Thick(end, end + new Vector2(side * rng.Next(6, 14), rng.Next(-8, 9)), 3f, 1f,
                        i % 2 == 0 ? Straw : StrawDark);
                }
            }

            // The sack body, bound with rope.
            c.Ellipse(new Vector2(cx, 118f), 30f, 40f, StrawDark);
            c.Ellipse(new Vector2(cx - 3f, 121f), 26f, 36f, Straw);
            foreach (var y in new[] { 96f, 138f })
            {
                c.Thick(new Vector2(cx - 29f, y), new Vector2(cx + 29f, y + 2f), 3f, 3f, Rope);
            }

            for (var i = 0; i < 40; i++)
            {
                var x = cx + rng.Next(-24, 24);
                var y = 90f + rng.Next(0, 56);
                c.RectOver(x, y, x + 1f, y + rng.Next(2, 6), StrawDark);
            }

            // A rusted breastplate strapped over the front, dented.
            c.Ellipse(new Vector2(cx + 8f, 122f), 20f, 24f, Rust);
            c.Ellipse(new Vector2(cx + 10f, 126f), 15f, 18f, Steel);
            c.Thick(new Vector2(cx + 2f, 138f), new Vector2(cx + 18f, 108f), 2f, 2f, SteelLit);
            c.Ellipse(new Vector2(cx + 14f, 116f), 3f, 2.5f, Rust);

            // The head: a sack under a dented helm, straw out of the neck.
            c.Ellipse(new Vector2(cx, 184f), 16f, 17f, Straw);
            c.Ellipse(new Vector2(cx + 1f, 192f), 18f, 12f, Steel);
            c.Rect(cx - 18f, 186f, cx + 19f, 190f, Steel);
            c.Thick(new Vector2(cx - 12f, 199f), new Vector2(cx + 6f, 202f), 2f, 2f, SteelLit);
            c.Triangle(new Vector2(cx + 8f, 199f), new Vector2(cx + 16f, 194f), new Vector2(cx + 13f, 202f),
                Rust);
            // Two stitched crosses for eyes: it is a target, not a creature.
            foreach (var ex in new[] { cx + 3f, cx + 11f })
            {
                c.Thick(new Vector2(ex - 2f, 180f), new Vector2(ex + 2f, 184f), 1.5f, 1.5f, WoodDark);
                c.Thick(new Vector2(ex - 2f, 184f), new Vector2(ex + 2f, 180f), 1.5f, 1.5f, WoodDark);
            }

            if (armed)
            {
                // A wooden practice sword lashed to the forward arm, raised.
                var grip = new Vector2(cx + 50f, 146f);
                c.Thick(grip, grip + new Vector2(20f, 52f), 7f, 5f, PracticeWood);
                c.Thick(grip - new Vector2(8f, 3f), grip + new Vector2(8f, 3f), 4f, 4f, WoodDark);
                c.Thick(grip, grip - new Vector2(3f, 10f), 4f, 4f, Rope);
                // A strip of red cloth on the helm: this one strikes back.
                c.Thick(new Vector2(cx - 14f, 196f), new Vector2(cx - 30f, 184f), 4f, 2f,
                    new Color32(0x7A, 0x22, 0x24, 0xFF));
            }

            c.Silhouette(new Color32(6, 5, 5, 220), new Color32(186, 176, 160, 255), 0.35f);
            return c;
        }
    }
}
