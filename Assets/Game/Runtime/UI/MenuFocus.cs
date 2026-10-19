using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The focus of a menu list as the HTML draws it (design/ui/menus/menus.html, UI_UX_GUIDE.md
    /// §12): a warm wash slides behind the focused item, the crown's flame slides to the left of
    /// its word, and two gold threads draw out to both sides. Light and form change together, so
    /// the focus reads without colour (§4). A dangerous item takes the same in red.
    ///
    /// The pieces are drawn with <see cref="Painter2D"/> curves, so nothing here needs a sprite;
    /// they sit behind the items, ignore the pointer and only follow <see cref="MoveTo"/>.
    /// </summary>
    public sealed class MenuFocus
    {
        private const float FlameGap = 44f;
        private const float ThreadGap = 26f;
        private const float ThreadLength = 70f;
        private static readonly Color Gold = new Color(0.79f, 0.63f, 0.29f);
        private static readonly Color Danger = new Color(0.77f, 0.33f, 0.18f);

        private readonly VisualElement _list;
        private readonly Glow _glow;
        private readonly Flame _flame;
        private readonly VisualElement _left;
        private readonly VisualElement _right;
        private VisualElement _item;
        private bool _placed;

        public MenuFocus(VisualElement list)
        {
            _list = list;
            if (_list == null)
            {
                return;
            }

            _glow = new Glow();
            _flame = new Flame();
            _left = Part("menu-focus__thread menu-focus__thread--left");
            _right = Part("menu-focus__thread menu-focus__thread--right");
            _glow.AddToClassList("menu-focus__glow");
            _flame.AddToClassList("menu-focus__flame");
            foreach (var part in new[] { _right, _left, _flame, (VisualElement)_glow })
            {
                part.pickingMode = PickingMode.Ignore;
                _list.Insert(0, part);
            }

            // The first focus is set before layout; place it again once sizes are known.
            _list.RegisterCallback<GeometryChangedEvent>(_ => Place(false));
        }

        /// <summary>Slides the light, the flame and the threads to <paramref name="item"/>.</summary>
        public void MoveTo(VisualElement item, bool danger = false)
        {
            if (_list == null || item == null)
            {
                return;
            }

            var changed = item != _item;
            _item = item;
            _glow.Tint = danger ? Danger : Gold;
            _flame.Danger = danger;
            _left.EnableInClassList("menu-focus__thread--danger", danger);
            _right.EnableInClassList("menu-focus__thread--danger", danger);
            Place(changed);
        }

        private void Place(bool redrawThreads)
        {
            if (_item == null || float.IsNaN(_item.layout.width) || _item.layout.width <= 0f)
            {
                return;
            }

            if (!_placed)
            {
                // The first placement jumps there: sliding in from the list's corner reads as a glitch.
                _placed = true;
                foreach (var part in new[] { _glow, _flame, _left, _right })
                {
                    part.AddToClassList("menu-focus--instant");
                    part.schedule.Execute(() => part.RemoveFromClassList("menu-focus--instant")).ExecuteLater(50);
                }
            }

            var box = _item.layout;
            var half = TextWidth(_item) * 0.5f;
            var center = box.x + box.width * 0.5f;
            var middle = box.y + box.height * 0.5f;
            _glow.style.translate = new Translate(center - _glow.resolvedStyle.width * 0.5f,
                middle - _glow.resolvedStyle.height * 0.5f);
            _flame.style.translate = new Translate(center - half - FlameGap,
                middle - _flame.resolvedStyle.height * 0.5f);
            _left.style.translate = new Translate(center - half - ThreadGap - ThreadLength, middle);
            _right.style.translate = new Translate(center + half + ThreadGap, middle);
            if (redrawThreads)
            {
                // Drawn out anew on every move: the threads grow from the word, they do not slide.
                foreach (var thread in new[] { _left, _right })
                {
                    thread.RemoveFromClassList("menu-focus__thread--drawn");
                    thread.schedule.Execute(() => thread.AddToClassList("menu-focus__thread--drawn")).ExecuteLater(16);
                }
            }
        }

        private static float TextWidth(VisualElement item)
        {
            var text = item as TextElement;
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                text = item.Q<Label>();
            }

            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return item.layout.width * 0.5f;
            }

            return text.MeasureTextSize(text.text, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x;
        }

        private static VisualElement Part(string classes)
        {
            var part = new VisualElement();
            foreach (var name in classes.Split(' '))
            {
                part.AddToClassList(name);
            }

            return part;
        }

        /// <summary>A soft oval of light: stacked ellipses, faint each, so the edge has no line.</summary>
        private sealed class Glow : VisualElement
        {
            private const int Rings = 12;
            private Color _tint = Gold;

            public Glow()
            {
                generateVisualContent += Draw;
            }

            public Color Tint
            {
                get => _tint;
                set
                {
                    if (_tint == value)
                    {
                        return;
                    }

                    _tint = value;
                    MarkDirtyRepaint();
                }
            }

            private void Draw(MeshGenerationContext context)
            {
                var size = contentRect.size;
                if (size.x <= 0f || size.y <= 0f)
                {
                    return;
                }

                var painter = context.painter2D;
                var center = size * 0.5f;
                for (var i = 0; i < Rings; i++)
                {
                    var share = 1f - i / (float)Rings;
                    painter.fillColor = new Color(_tint.r, _tint.g, _tint.b, 0.022f);
                    Ellipse(painter, center, size.x * 0.5f * share, size.y * 0.5f * Mathf.Lerp(0.35f, 1f, share));
                    painter.Fill();
                }
            }

            private static void Ellipse(Painter2D painter, Vector2 c, float rx, float ry)
            {
                const float k = 0.5523f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(c.x - rx, c.y));
                painter.BezierCurveTo(new Vector2(c.x - rx, c.y - ry * k), new Vector2(c.x - rx * k, c.y - ry), new Vector2(c.x, c.y - ry));
                painter.BezierCurveTo(new Vector2(c.x + rx * k, c.y - ry), new Vector2(c.x + rx, c.y - ry * k), new Vector2(c.x + rx, c.y));
                painter.BezierCurveTo(new Vector2(c.x + rx, c.y + ry * k), new Vector2(c.x + rx * k, c.y + ry), new Vector2(c.x, c.y + ry));
                painter.BezierCurveTo(new Vector2(c.x - rx * k, c.y + ry), new Vector2(c.x - rx, c.y + ry * k), new Vector2(c.x - rx, c.y));
                painter.ClosePath();
            }
        }

        /// <summary>
        /// The crown's flame (VISUAL_BIBLE §7): an almond leaning a little left, with a pale core,
        /// flickering by reshaping rather than by blinking. Still under reduced motion.
        /// </summary>
        private sealed class Flame : VisualElement
        {
            private static readonly Color Outer = new Color(0.94f, 0.71f, 0.25f);
            private static readonly Color Core = new Color(1f, 0.94f, 0.75f);
            private static readonly Color DangerOuter = new Color(0.86f, 0.38f, 0.2f);
            private bool _danger;

            public Flame()
            {
                generateVisualContent += Draw;
                schedule.Execute(MarkDirtyRepaint).Every(40);
            }

            public bool Danger
            {
                set
                {
                    if (_danger == value)
                    {
                        return;
                    }

                    _danger = value;
                    MarkDirtyRepaint();
                }
            }

            private void Draw(MeshGenerationContext context)
            {
                var size = contentRect.size;
                if (size.x <= 0f || size.y <= 0f)
                {
                    return;
                }

                var t = FeedbackSettings.ReducedMotion ? 0f : Time.unscaledTime;
                var sway = Mathf.Sin(t * 3.1f) * 0.08f - 0.06f;
                var height = 1f + Mathf.Sin(t * 9f) * 0.05f + Mathf.Sin(t * 13.7f + 1f) * 0.03f;
                var painter = context.painter2D;
                Tongue(painter, size, 1f, height, sway, _danger ? DangerOuter : Outer);
                Tongue(painter, size, 0.55f, height * 0.9f, sway * 1.4f, Core);
            }

            private static void Tongue(Painter2D painter, Vector2 size, float scale, float height, float sway, Color color)
            {
                var baseY = size.y * 0.92f;
                var mid = size.x * 0.5f;
                var w = size.x * 0.42f * scale;
                var h = size.y * 0.88f * scale * height;
                var tip = new Vector2(mid + sway * size.x, baseY - h);
                painter.fillColor = color;
                painter.BeginPath();
                painter.MoveTo(new Vector2(mid - w, baseY - h * 0.08f));
                painter.BezierCurveTo(new Vector2(mid - w, baseY - h * 0.55f), new Vector2(tip.x - w * 0.3f, baseY - h * 0.8f), tip);
                painter.BezierCurveTo(new Vector2(tip.x + w * 0.4f, baseY - h * 0.7f), new Vector2(mid + w, baseY - h * 0.5f), new Vector2(mid + w, baseY - h * 0.08f));
                painter.QuadraticCurveTo(new Vector2(mid, baseY + h * 0.1f), new Vector2(mid - w, baseY - h * 0.08f));
                painter.ClosePath();
                painter.Fill();
            }
        }
    }
}
