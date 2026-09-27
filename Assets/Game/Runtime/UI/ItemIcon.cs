using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The silhouette an item is drawn with until it has painted art of its own.</summary>
    public enum ItemIconKind
    {
        Sword,
        Greatsword,
        Head,
        Chest,
        Gloves,
        Boots,
        Ring,
        Amulet,
        Herb,
        Shard,
        Key,
        Relic
    }

    /// <summary>
    /// An item's picture in the inventory. An item with an <see cref="ItemDefinition.Icon"/> shows
    /// it; the rest are drawn as vector silhouettes by kind, the same shapes as the approved
    /// design/ui/inventory and equipment mockups, so painted icons can arrive one at a time
    /// without the screen ever showing a blank cell.
    /// </summary>
    public sealed class ItemIcon : VisualElement
    {
        private static readonly Color Blade = new Color32(185, 192, 201, 255);
        private static readonly Color DarkBlade = new Color32(154, 162, 173, 255);
        private static readonly Color GoldDim = new Color32(138, 116, 52, 255);
        private static readonly Color Gold = new Color32(201, 161, 74, 255);
        private static readonly Color GoldBright = new Color32(232, 205, 138, 255);
        private static readonly Color Leather = new Color32(74, 59, 46, 255);
        private static readonly Color LeatherDark = new Color32(42, 35, 28, 255);
        private static readonly Color Iron = new Color32(62, 66, 71, 255);
        private static readonly Color Sapphire = new Color32(78, 127, 176, 255);
        private static readonly Color Leaf = new Color32(111, 143, 90, 255);
        private static readonly Color LeafLight = new Color32(127, 168, 92, 255);
        private static readonly Color Stone = new Color32(107, 90, 66, 255);
        private static readonly Color StoneLight = new Color32(142, 131, 117, 255);
        private static readonly Color Umber = new Color32(58, 47, 37, 255);

        private ItemIconKind _kind;
        private bool _hasSprite;

        public ItemIcon()
        {
            AddToClassList("item-icon");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public ItemIconKind Kind => _kind;

        public void Show(ItemDefinition item)
        {
            var sprite = item != null ? item.Icon : null;
            _hasSprite = sprite != null;
            style.backgroundImage = _hasSprite
                ? new StyleBackground(sprite)
                : new StyleBackground(StyleKeyword.None);
            _kind = KindFor(item);
            MarkDirtyRepaint();
        }

        /// <summary>Which silhouette an item gets: its weapon type, armour slot or category.</summary>
        public static ItemIconKind KindFor(ItemDefinition item)
        {
            if (item == null)
            {
                return ItemIconKind.Shard;
            }

            if (item.Weapon != null)
            {
                return item.Weapon.WeaponType == WeaponType.Greatsword
                    ? ItemIconKind.Greatsword
                    : ItemIconKind.Sword;
            }

            if (item.Equipment is ArmorDefinition armor)
            {
                switch (armor.Slot)
                {
                    case EquipmentSlot.Head: return ItemIconKind.Head;
                    case EquipmentSlot.Gloves: return ItemIconKind.Gloves;
                    case EquipmentSlot.Boots: return ItemIconKind.Boots;
                    default: return ItemIconKind.Chest;
                }
            }

            if (item.Equipment is AccessoryDefinition)
            {
                // Rings and signets go on a finger; charms, sigils and talismans hang on a chain.
                var id = item.ItemId ?? string.Empty;
                return id.Contains("ring") || id.Contains("signet")
                    ? ItemIconKind.Ring
                    : ItemIconKind.Amulet;
            }

            switch (item.Category)
            {
                case ItemCategory.Consumable: return ItemIconKind.Herb;
                case ItemCategory.KeyItem:
                case ItemCategory.QuestItem: return ItemIconKind.Key;
                case ItemCategory.Progression: return ItemIconKind.Relic;
                default: return ItemIconKind.Shard;
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (_hasSprite || rect.width < 1f || rect.height < 1f)
            {
                return;
            }

            // The shapes are authored on the mockups' 24-unit view box.
            var scale = Mathf.Min(rect.width, rect.height) / 24f;
            var origin = new Vector2((rect.width - 24f * scale) * 0.5f, (rect.height - 24f * scale) * 0.5f);
            var p = new Pen(context.painter2D, origin, scale);
            switch (_kind)
            {
                case ItemIconKind.Sword:
                    p.Poly(Blade, 14.5f, 3.5f, 17.5f, 6.5f, 9.5f, 14.5f, 6.5f, 11.5f);
                    p.Poly(GoldDim, 6.2f, 11.8f, 9.2f, 14.8f, 7.6f, 16.4f, 4.6f, 13.4f);
                    p.Poly(Leather, 4f, 18f, 6.4f, 15.6f, 8f, 17.2f, 5.6f, 19.6f);
                    break;
                case ItemIconKind.Greatsword:
                    p.Poly(DarkBlade, 15.5f, 2.5f, 19.5f, 6.5f, 10f, 16f, 6f, 12f);
                    p.Poly(GoldDim, 5.4f, 12.6f, 9.4f, 16.6f, 7.4f, 18.6f, 3.4f, 14.6f);
                    p.Poly(Leather, 2.6f, 18.4f, 5f, 16f, 7f, 18f, 4.6f, 20.4f);
                    break;
                case ItemIconKind.Head:
                    p.Hood();
                    break;
                case ItemIconKind.Chest:
                    p.Poly(Iron, 9f, 3f, 12f, 5f, 15f, 3f, 19f, 6f, 17.5f, 10f, 17.5f, 19f, 7.5f, 19f,
                        7.5f, 10f, 6f, 6f);
                    p.Line(GoldDim, 1.4f, 12f, 7f, 12f, 17f);
                    break;
                case ItemIconKind.Gloves:
                    p.Poly(Leather, 8f, 20f, 8f, 11f, 7f, 7f, 9f, 6.5f, 10f, 10f, 10.2f, 5f, 12f, 4.5f,
                        12.5f, 10f, 13.2f, 5f, 15f, 5.2f, 14.8f, 10.5f, 16.5f, 8f, 18f, 9f, 16f, 14f,
                        16f, 20f);
                    p.Poly(GoldDim, 8f, 17f, 16f, 17f, 16f, 20f, 8f, 20f);
                    break;
                case ItemIconKind.Boots:
                    p.Poly(Leather, 8f, 4f, 13f, 4f, 13f, 14.5f, 19f, 16.5f, 19f, 19.5f, 8f, 19.5f);
                    p.Poly(LeatherDark, 8f, 19f, 19f, 19f, 19f, 21f, 8f, 21f);
                    p.Poly(GoldDim, 8f, 6f, 13f, 6f, 13f, 7.2f, 8f, 7.2f);
                    break;
                case ItemIconKind.Ring:
                    p.Circle(12f, 14f, 6f, null, Gold, 2f);
                    p.Poly(Sapphire, 12f, 3f, 14f, 6.6f, 12f, 8.2f, 10f, 6.6f);
                    break;
                case ItemIconKind.Amulet:
                    p.Chain(GoldDim);
                    p.Poly(Gold, 12f, 10f, 16f, 14.5f, 12f, 20.5f, 8f, 14.5f);
                    p.Poly(Sapphire, 12f, 12.5f, 14f, 14.5f, 12f, 17.5f, 10f, 14.5f);
                    break;
                case ItemIconKind.Herb:
                    p.Line(Leaf, 1.4f, 12f, 21f, 12f, 9f);
                    p.Leaf(Leaf, 12f, 14f, -1f);
                    p.Leaf(LeafLight, 12f, 11f, 1f);
                    break;
                case ItemIconKind.Key:
                    p.Circle(8f, 8f, 3.8f, null, Gold, 2f);
                    p.Line(Gold, 2f, 10.8f, 10.8f, 19.5f, 19.5f);
                    p.Line(Gold, 2f, 16.5f, 16.5f, 18.5f, 14.5f);
                    p.Line(Gold, 2f, 18.8f, 18.8f, 20.8f, 16.8f);
                    break;
                case ItemIconKind.Relic:
                    p.Circle(12f, 12f, 7f, Umber, Gold, 1.2f);
                    p.Circle(12f, 12f, 3.5f, GoldBright, null, 0f);
                    break;
                default:
                    p.Poly(Stone, 12f, 3f, 18f, 9f, 15f, 20f, 8f, 19f, 6f, 10f);
                    p.Poly(StoneLight, 12f, 3f, 18f, 9f, 12.5f, 11.5f);
                    break;
            }
        }

        /// <summary>Paths in view-box units, placed and scaled into the element.</summary>
        private readonly struct Pen
        {
            private readonly Painter2D _painter;
            private readonly Vector2 _origin;
            private readonly float _scale;

            public Pen(Painter2D painter, Vector2 origin, float scale)
            {
                _painter = painter;
                _origin = origin;
                _scale = scale;
            }

            private Vector2 At(float x, float y) => _origin + new Vector2(x, y) * _scale;

            public void Poly(Color color, params float[] xy)
            {
                _painter.fillColor = color;
                _painter.BeginPath();
                _painter.MoveTo(At(xy[0], xy[1]));
                for (var i = 2; i + 1 < xy.Length; i += 2)
                {
                    _painter.LineTo(At(xy[i], xy[i + 1]));
                }

                _painter.ClosePath();
                _painter.Fill();
            }

            public void Line(Color color, float width, float x1, float y1, float x2, float y2)
            {
                _painter.strokeColor = color;
                _painter.lineWidth = width * _scale;
                _painter.lineCap = LineCap.Round;
                _painter.BeginPath();
                _painter.MoveTo(At(x1, y1));
                _painter.LineTo(At(x2, y2));
                _painter.Stroke();
            }

            public void Circle(float x, float y, float radius, Color? fill, Color? stroke, float width)
            {
                _painter.BeginPath();
                _painter.Arc(At(x, y), radius * _scale, Angle.Degrees(0f), Angle.Degrees(360f));
                _painter.ClosePath();
                if (fill.HasValue)
                {
                    _painter.fillColor = fill.Value;
                    _painter.Fill();
                }

                if (stroke.HasValue)
                {
                    _painter.strokeColor = stroke.Value;
                    _painter.lineWidth = width * _scale;
                    _painter.Stroke();
                }
            }

            /// <summary>The hood from equipment.html: a rounded cowl with a dark opening.</summary>
            public void Hood()
            {
                _painter.fillColor = Leather;
                _painter.BeginPath();
                _painter.MoveTo(At(12f, 3f));
                _painter.BezierCurveTo(At(16f, 3f), At(18f, 5f), At(18f, 8f));
                _painter.LineTo(At(18f, 11f));
                _painter.BezierCurveTo(At(18f, 15f), At(15f, 18f), At(12f, 19f));
                _painter.BezierCurveTo(At(9f, 18f), At(6f, 15f), At(6f, 11f));
                _painter.LineTo(At(6f, 8f));
                _painter.BezierCurveTo(At(6f, 5f), At(8f, 3f), At(12f, 3f));
                _painter.ClosePath();
                _painter.Fill();

                _painter.fillColor = LeatherDark;
                _painter.BeginPath();
                _painter.MoveTo(At(8f, 7f));
                _painter.BezierCurveTo(At(9.4f, 6f), At(14.6f, 6f), At(16f, 7f));
                _painter.LineTo(At(16f, 9f));
                _painter.BezierCurveTo(At(14.4f, 8f), At(9.6f, 8f), At(8f, 9f));
                _painter.ClosePath();
                _painter.Fill();
            }

            public void Chain(Color color)
            {
                _painter.strokeColor = color;
                _painter.lineWidth = 1.2f * _scale;
                _painter.BeginPath();
                _painter.MoveTo(At(7f, 3f));
                _painter.BezierCurveTo(At(8f, 9f), At(16f, 9f), At(17f, 3f));
                _painter.Stroke();
            }

            /// <summary>A leaf growing from the stem at (x, y), to the left (-1) or right (1).</summary>
            public void Leaf(Color color, float x, float y, float side)
            {
                _painter.fillColor = color;
                _painter.BeginPath();
                _painter.MoveTo(At(x, y));
                _painter.BezierCurveTo(At(x + 5f * side, y - 1f), At(x + 6f * side, y - 5f),
                    At(x + 5f * side, y - 8f));
                _painter.BezierCurveTo(At(x + 2f * side, y - 7f), At(x, y - 4f), At(x, y));
                _painter.ClosePath();
                _painter.Fill();
            }
        }
    }
}
