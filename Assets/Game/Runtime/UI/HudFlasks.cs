using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The flask row (design/ui/tutorial/tutorial.html, frames 1-2): full flasks lit, spent ones
    /// only dark glass, and the one being drunk draining as the drink goes on. Split out of
    /// <see cref="HudController"/> (2026-10-19, UI_UX_GUIDE.md §2а). Each flask is still an element
    /// with the classes <c>flask</c>, <c>flask--empty</c> and <c>flask--drinking</c>, so the state reads
    /// by shape and class; the bottle itself is drawn with curves (<see cref="FlaskIcon"/>).
    /// </summary>
    internal sealed class HudFlasks
    {
        private readonly VisualElement _flasks;
        private readonly VisualElement _row;
        private readonly List<FlaskIcon> _icons = new List<FlaskIcon>();
        private int _shownCharges = -1;
        private bool _shownDrinking;

        public HudFlasks(VisualElement root)
        {
            _flasks = root.Q("flasks");
            _row = root.Q("flask-row");
        }

        public int Count => _icons.Count;

        public void Update(HealingFlasks flasks)
        {
            if (flasks == null || _row == null)
            {
                return;
            }

            if (_icons.Count != flasks.MaxCharges)
            {
                _row.Clear();
                _icons.Clear();
                for (var i = 0; i < flasks.MaxCharges; i++)
                {
                    var icon = new FlaskIcon { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("flask");
                    _row.Add(icon);
                    _icons.Add(icon);
                }

                _shownCharges = -1;
            }

            var charges = flasks.Charges;
            var drinking = flasks.IsDrinking;
            if (drinking && charges < _icons.Count)
            {
                // The flask being drunk empties as the drink goes on, every frame of it.
                _icons[charges].Level = 1f - flasks.DrinkProgress;
            }

            if (charges == _shownCharges && drinking == _shownDrinking)
            {
                return;
            }

            _shownCharges = charges;
            _shownDrinking = drinking;
            for (var i = 0; i < _icons.Count; i++)
            {
                // The charge is paid as the drink starts, so the one being drunk sits just past
                // the remaining ones.
                var isDrinking = drinking && i == charges;
                var empty = i >= charges && !isDrinking;
                _icons[i].EnableInClassList("flask--drinking", isDrinking);
                _icons[i].EnableInClassList("flask--empty", empty);
                _icons[i].Drinking = isDrinking;
                _icons[i].Level = empty ? 0f : isDrinking ? 1f - flasks.DrinkProgress : 1f;
            }

            _flasks?.EnableInClassList("flasks--empty", charges == 0 && !drinking);
        }

        /// <summary>
        /// One flask, drawn: a round-bellied bottle of dark glass with a cork, amber inside up to
        /// <see cref="Level"/> with a lighter surface line, a highlight down the glass, and — while
        /// drinking — a warm glow around it that breathes. Empty, only the glass and its rim remain.
        /// </summary>
        private sealed class FlaskIcon : VisualElement
        {
            private static readonly Color Glass = new Color(0.13f, 0.11f, 0.09f, 0.95f);
            private static readonly Color Rim = new Color(0.42f, 0.35f, 0.26f, 1f);
            private static readonly Color RimEmpty = new Color(0.23f, 0.18f, 0.15f, 1f);
            private static readonly Color Amber = new Color(0.79f, 0.63f, 0.29f, 1f);
            private static readonly Color AmberDeep = new Color(0.6f, 0.43f, 0.15f, 1f);
            private static readonly Color Surface = new Color(0.91f, 0.8f, 0.54f, 1f);
            private static readonly Color Cork = new Color(0.36f, 0.27f, 0.18f, 1f);
            private static readonly Color Shine = new Color(1f, 0.96f, 0.86f, 0.55f);
            private float _level = 1f;
            private bool _drinking;

            public FlaskIcon()
            {
                generateVisualContent += Draw;
                schedule.Execute(() => { if (_drinking) { MarkDirtyRepaint(); } }).Every(50);
            }

            public float Level
            {
                set
                {
                    value = Mathf.Clamp01(value);
                    if (Mathf.Abs(value - _level) < 0.01f)
                    {
                        return;
                    }

                    _level = value;
                    MarkDirtyRepaint();
                }
            }

            public bool Drinking
            {
                set
                {
                    if (_drinking == value)
                    {
                        return;
                    }

                    _drinking = value;
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

                var p = context.painter2D;
                var w = size.x;
                var h = size.y;
                var belly = new Vector2(w * 0.5f, h * 0.66f);
                var radius = Mathf.Min(w * 0.44f, h * 0.32f);
                var neckW = w * 0.2f;
                var neckTop = h * 0.16f;
                var empty = _level <= 0f;

                if (_drinking)
                {
                    var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                    p.fillColor = new Color(1f, 0.8f, 0.4f, 0.12f + 0.1f * pulse);
                    Circle(p, belly, radius + 4f);
                    p.Fill();
                }

                // Glass: the belly and the neck as one outline.
                Bottle(p, belly, radius, neckW, neckTop);
                p.fillColor = Glass;
                p.Fill();

                if (!empty)
                {
                    // The liquid: the belly cut at the level, darker at the bottom.
                    var top = belly.y + radius - _level * (radius * 2f + (belly.y - radius - neckTop) * 0.4f);
                    top = Mathf.Max(top, neckTop + 2f);
                    Liquid(p, belly, radius - 2f, top, Amber, 1f);
                    Liquid(p, belly, radius - 2f, Mathf.Max(top, belly.y + radius * 0.35f), AmberDeep, 0.7f);
                    p.strokeColor = Surface;
                    p.lineWidth = 1.5f;
                    var half = Mathf.Sqrt(Mathf.Max(0f, (radius - 2f) * (radius - 2f) - (top - belly.y) * (top - belly.y)));
                    if (top > belly.y - radius + 2f)
                    {
                        p.BeginPath();
                        p.MoveTo(new Vector2(belly.x - half + 1f, top + 0.5f));
                        p.LineTo(new Vector2(belly.x + half - 1f, top + 0.5f));
                        p.Stroke();
                    }
                }

                // Rim of the glass and a highlight down its left side.
                Bottle(p, belly, radius, neckW, neckTop);
                p.strokeColor = empty ? RimEmpty : Rim;
                p.lineWidth = 1.4f;
                p.Stroke();
                if (!empty)
                {
                    p.strokeColor = Shine;
                    p.lineWidth = 1.6f;
                    p.BeginPath();
                    p.Arc(belly, radius * 0.7f, Angle.Degrees(200f), Angle.Degrees(250f));
                    p.Stroke();
                }

                // The cork.
                p.fillColor = empty ? RimEmpty : Cork;
                p.BeginPath();
                p.MoveTo(new Vector2(belly.x - neckW * 0.65f, neckTop));
                p.LineTo(new Vector2(belly.x + neckW * 0.65f, neckTop));
                p.LineTo(new Vector2(belly.x + neckW * 0.55f, 1f));
                p.LineTo(new Vector2(belly.x - neckW * 0.55f, 1f));
                p.ClosePath();
                p.Fill();
            }

            private static void Bottle(Painter2D p, Vector2 belly, float radius, float neckW, float neckTop)
            {
                // From the neck's left down into the belly, round it, and back up the right.
                var shoulder = Mathf.Asin(Mathf.Clamp(neckW / radius, -1f, 1f)) * Mathf.Rad2Deg;
                p.BeginPath();
                p.MoveTo(new Vector2(belly.x - neckW, neckTop));
                p.LineTo(new Vector2(belly.x - neckW, belly.y - radius * Mathf.Cos(shoulder * Mathf.Deg2Rad)));
                p.Arc(belly, radius, Angle.Degrees(270f - shoulder), Angle.Degrees(-90f + shoulder), ArcDirection.CounterClockwise);
                p.LineTo(new Vector2(belly.x + neckW, neckTop));
                p.ClosePath();
            }

            private static void Liquid(Painter2D p, Vector2 belly, float radius, float top, Color color, float alpha)
            {
                // The part of the belly's circle below the line y = top (y grows downwards).
                var dy = Mathf.Clamp(top - belly.y, -radius, radius);
                var half = Mathf.Acos(dy / radius) * Mathf.Rad2Deg;
                p.fillColor = new Color(color.r, color.g, color.b, color.a * alpha);
                p.BeginPath();
                p.Arc(belly, radius, Angle.Degrees(90f - half), Angle.Degrees(90f + half));
                p.ClosePath();
                p.Fill();
            }

            private static void Circle(Painter2D p, Vector2 centre, float radius)
            {
                p.BeginPath();
                p.Arc(centre, radius, Angle.Degrees(0f), Angle.Degrees(360f));
                p.ClosePath();
            }
        }
    }
}
