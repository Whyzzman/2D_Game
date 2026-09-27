using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>The picture inside a skill node until the skill has painted art of its own.</summary>
    public enum SkillGlyph
    {
        Crest,
        Blade,
        Shield,
        Dash,
        Heart,
        Drop,
        Flame,
        Frost,
        Strike
    }

    /// <summary>How a node reads at a glance; the shape carries it even without colour.</summary>
    public enum SkillNodeState
    {
        Locked,
        Available,
        Unlocked,
        Max
    }

    /// <summary>
    /// One node of the skill tree (design/ui/skills/skills.html). Its outline is the branch's
    /// shape — Warrior a hexagon, Hunter a diamond, Mage a circle, Shadow a broken stone — and
    /// the glyph inside says what the skill does. Drawn with vectors, as UI Toolkit cannot clip.
    /// </summary>
    public sealed class SkillNodeElement : VisualElement
    {
        private static readonly Color Surface = new Color32(28, 23, 19, 255);
        private static readonly Color Edge = new Color32(58, 47, 37, 255);
        private static readonly Color GoldDim = new Color32(138, 116, 52, 255);
        private static readonly Color Gold = new Color32(201, 161, 74, 255);
        private static readonly Color GoldBright = new Color32(232, 205, 138, 255);
        private static readonly Color Muted = new Color32(142, 131, 117, 255);

        private readonly Label _rank;
        private readonly Label _slot;
        private SkillBranch _branch;
        private SkillGlyph _glyph;
        private SkillNodeState _state;
        private bool _selected;

        public SkillNodeElement()
        {
            AddToClassList("sk-node");
            generateVisualContent += Draw;
            _rank = UiParts.Text(string.Empty, "sk-node__rank");
            Add(_rank);
            _slot = UiParts.Text(string.Empty, "sk-node__slot");
            Add(_slot);
        }

        public string SkillId { get; private set; }
        public SkillNodeState State => _state;

        public void Show(SkillDefinition skill, int rank, SkillNodeState state, bool selected, string slotKey)
        {
            SkillId = skill.SkillId;
            _branch = skill.Branch;
            _glyph = GlyphFor(skill);
            _state = state;
            _selected = selected;
            _rank.text = $"{rank}/{skill.MaxRank}";
            _slot.text = slotKey ?? string.Empty;
            UiParts.Show(_slot, !string.IsNullOrEmpty(slotKey));
            EnableInClassList("sk-node--locked", state == SkillNodeState.Locked);
            MarkDirtyRepaint();
        }

        /// <summary>What the skill does, read from its first effect; actives all strike.</summary>
        public static SkillGlyph GlyphFor(SkillDefinition skill)
        {
            if (skill == null) { return SkillGlyph.Crest; }
            if (skill.IsActive) { return SkillGlyph.Strike; }
            if (!string.IsNullOrEmpty(skill.GrantedSpellId)) { return SkillGlyph.Flame; }
            var effects = skill.Effects;
            if (effects == null || effects.Count == 0 || effects[0] == null) { return SkillGlyph.Crest; }
            switch (effects[0].Type)
            {
                case SkillEffectType.StaggerResistance:
                case SkillEffectType.GuardCost:
                case SkillEffectType.GuardRecovery:
                case SkillEffectType.ParryStagger:
                case SkillEffectType.SpellDamageReduction:
                    return SkillGlyph.Shield;
                case SkillEffectType.DashStaminaCost:
                case SkillEffectType.DashCooldown:
                case SkillEffectType.CuttingShadow:
                case SkillEffectType.ShadowCost:
                case SkillEffectType.WallRunStaminaCost:
                    return SkillGlyph.Dash;
                case SkillEffectType.AdrenalineRegen:
                case SkillEffectType.SecondWindResistance:
                    return SkillGlyph.Heart;
                case SkillEffectType.MaxMana:
                case SkillEffectType.SpellManaCost:
                case SkillEffectType.SpellKillMana:
                case SkillEffectType.SpellCastTime:
                    return SkillGlyph.Drop;
                case SkillEffectType.SpellDamage:
                case SkillEffectType.FireDamage:
                case SkillEffectType.SpellCritChance:
                    return SkillGlyph.Flame;
                case SkillEffectType.FrostDamage:
                    return SkillGlyph.Frost;
                default:
                    return SkillGlyph.Blade;
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < 1f)
            {
                return;
            }

            var painter = context.painter2D;
            var size = Mathf.Min(rect.width, rect.height);
            var outline = Outline(_branch, size, 2f);
            if (_selected)
            {
                // The focus ring: a wider, brighter outline behind the node.
                Path(painter, Outline(_branch, size, -3f));
                painter.strokeColor = new Color(GoldBright.r, GoldBright.g, GoldBright.b, 0.55f);
                painter.lineWidth = 3f;
                painter.Stroke();
            }

            Path(painter, outline);
            painter.fillColor = Surface;
            painter.Fill();
            painter.strokeColor = _state == SkillNodeState.Max ? GoldBright
                : _state == SkillNodeState.Unlocked ? Gold
                : _state == SkillNodeState.Available ? GoldDim
                : Edge;
            painter.lineWidth = 2f;
            painter.Stroke();

            var ink = _state == SkillNodeState.Locked ? Muted : Gold;
            DrawGlyph(painter, size, ink);
        }

        private static List<Vector2> Outline(SkillBranch branch, float size, float inset)
        {
            var points = new List<Vector2>();
            var s = size - inset * 2f;
            void P(float x, float y) => points.Add(new Vector2(inset + x * s, inset + y * s));
            switch (branch)
            {
                case SkillBranch.Warrior:
                    P(0.5f, 0f); P(1f, 0.26f); P(1f, 0.74f); P(0.5f, 1f); P(0f, 0.74f); P(0f, 0.26f);
                    break;
                case SkillBranch.Hunter:
                    P(0.5f, 0f); P(1f, 0.5f); P(0.5f, 1f); P(0f, 0.5f);
                    break;
                case SkillBranch.Shadow:
                    P(0.5f, 0f); P(0.92f, 0.22f); P(1f, 0.7f); P(0.58f, 1f); P(0.08f, 0.82f); P(0f, 0.34f);
                    break;
                default:
                    for (var i = 0; i < 32; i++)
                    {
                        var angle = i / 32f * Mathf.PI * 2f;
                        P(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
                    }

                    break;
            }

            return points;
        }

        private static void Path(Painter2D painter, List<Vector2> points)
        {
            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (var i = 1; i < points.Count; i++)
            {
                painter.LineTo(points[i]);
            }

            painter.ClosePath();
        }

        /// <summary>The glyphs are authored on the mockup's 24-unit view box, 38 px in an 84 px node.</summary>
        private void DrawGlyph(Painter2D painter, float size, Color ink)
        {
            var scale = size * 0.45f / 24f;
            var origin = new Vector2((size - 24f * scale) * 0.5f, (size - 24f * scale) * 0.5f);
            Vector2 At(float x, float y) => origin + new Vector2(x, y) * scale;
            painter.strokeColor = ink;
            painter.fillColor = ink;
            painter.lineWidth = 1.8f * scale;
            painter.lineJoin = LineJoin.Round;
            painter.lineCap = LineCap.Round;
            switch (_glyph)
            {
                case SkillGlyph.Blade:
                    painter.BeginPath();
                    painter.MoveTo(At(15.5f, 2.5f)); painter.LineTo(At(19.5f, 6.5f));
                    painter.LineTo(At(10f, 16f)); painter.LineTo(At(6f, 12f));
                    painter.ClosePath(); painter.Fill();
                    painter.BeginPath();
                    painter.MoveTo(At(5.4f, 12.6f)); painter.LineTo(At(9.4f, 16.6f));
                    painter.LineTo(At(7.4f, 18.6f)); painter.LineTo(At(3.4f, 14.6f));
                    painter.ClosePath(); painter.Fill();
                    break;
                case SkillGlyph.Shield:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 3f)); painter.LineTo(At(19f, 6f)); painter.LineTo(At(19f, 12f));
                    painter.BezierCurveTo(At(19f, 16f), At(16f, 19f), At(12f, 21f));
                    painter.BezierCurveTo(At(8f, 19f), At(5f, 16f), At(5f, 12f));
                    painter.LineTo(At(5f, 6f)); painter.ClosePath(); painter.Stroke();
                    break;
                case SkillGlyph.Dash:
                    painter.BeginPath();
                    painter.MoveTo(At(4f, 12f)); painter.LineTo(At(13f, 12f));
                    painter.MoveTo(At(12f, 7f)); painter.LineTo(At(18f, 12f)); painter.LineTo(At(12f, 17f));
                    painter.Stroke();
                    break;
                case SkillGlyph.Heart:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 20f));
                    painter.BezierCurveTo(At(16f, 17f), At(19f, 14f), At(19f, 10f));
                    painter.BezierCurveTo(At(19f, 6f), At(14f, 5f), At(12f, 7.4f));
                    painter.BezierCurveTo(At(10f, 5f), At(5f, 6f), At(5f, 10f));
                    painter.BezierCurveTo(At(5f, 14f), At(8f, 17f), At(12f, 20f));
                    painter.ClosePath(); painter.Stroke();
                    break;
                case SkillGlyph.Drop:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 3f));
                    painter.BezierCurveTo(At(15f, 8f), At(18f, 11f), At(18f, 15f));
                    painter.BezierCurveTo(At(18f, 18.5f), At(15.3f, 21f), At(12f, 21f));
                    painter.BezierCurveTo(At(8.7f, 21f), At(6f, 18.5f), At(6f, 15f));
                    painter.BezierCurveTo(At(6f, 11f), At(9f, 8f), At(12f, 3f));
                    painter.ClosePath(); painter.Stroke();
                    break;
                case SkillGlyph.Flame:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 2.5f));
                    painter.BezierCurveTo(At(13f, 7f), At(18f, 9f), At(18f, 14.5f));
                    painter.BezierCurveTo(At(18f, 18.5f), At(15.3f, 21f), At(12f, 21f));
                    painter.BezierCurveTo(At(8.7f, 21f), At(6f, 18.5f), At(6f, 14.5f));
                    painter.BezierCurveTo(At(6f, 11f), At(9f, 10f), At(9.5f, 7f));
                    painter.BezierCurveTo(At(11f, 9f), At(11.5f, 6f), At(12f, 2.5f));
                    painter.ClosePath(); painter.Fill();
                    break;
                case SkillGlyph.Frost:
                    painter.BeginPath();
                    for (var i = 0; i < 3; i++)
                    {
                        var angle = i * Mathf.PI / 3f;
                        var dx = Mathf.Sin(angle) * 9f;
                        var dy = Mathf.Cos(angle) * 9f;
                        painter.MoveTo(At(12f - dx, 12f - dy));
                        painter.LineTo(At(12f + dx, 12f + dy));
                    }

                    painter.Stroke();
                    break;
                case SkillGlyph.Strike:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 2f)); painter.LineTo(At(15f, 9f)); painter.LineTo(At(9f, 9f));
                    painter.ClosePath(); painter.Fill();
                    painter.BeginPath();
                    painter.MoveTo(At(4f, 20f)); painter.LineTo(At(12f, 11f)); painter.LineTo(At(20f, 20f));
                    painter.ClosePath(); painter.Stroke();
                    break;
                default:
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 3f)); painter.LineTo(At(19f, 6f)); painter.LineTo(At(19f, 12f));
                    painter.BezierCurveTo(At(19f, 16f), At(16f, 19f), At(12f, 21f));
                    painter.BezierCurveTo(At(8f, 19f), At(5f, 16f), At(5f, 12f));
                    painter.LineTo(At(5f, 6f)); painter.ClosePath(); painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(At(12f, 8f)); painter.LineTo(At(14f, 12f)); painter.LineTo(At(10f, 12f));
                    painter.ClosePath(); painter.Fill();
                    break;
            }
        }
    }

    /// <summary>
    /// The prerequisite lines under the nodes: solid gold where the prerequisite is owned,
    /// dashed where it is not yet (skills.html).
    /// </summary>
    public sealed class SkillLinks : VisualElement
    {
        private static readonly Color Owned = new Color32(201, 161, 74, 255);
        private static readonly Color Missing = new Color32(107, 90, 66, 255);
        private readonly List<(Vector2 from, Vector2 to, bool owned)> _links =
            new List<(Vector2, Vector2, bool)>();

        public SkillLinks()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("sk-links");
            generateVisualContent += Draw;
        }

        public void Set(IEnumerable<(Vector2 from, Vector2 to, bool owned)> links)
        {
            _links.Clear();
            _links.AddRange(links);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            painter.lineWidth = 2f;
            foreach (var (from, to, owned) in _links)
            {
                painter.strokeColor = owned ? Owned : Missing;
                painter.BeginPath();
                if (owned)
                {
                    painter.MoveTo(from);
                    painter.LineTo(to);
                }
                else
                {
                    // Painter2D has no dash pattern: 5 on, 5 off, by hand.
                    var length = Vector2.Distance(from, to);
                    var direction = (to - from) / Mathf.Max(0.001f, length);
                    for (var t = 0f; t < length; t += 10f)
                    {
                        painter.MoveTo(from + direction * t);
                        painter.LineTo(from + direction * Mathf.Min(length, t + 5f));
                    }
                }

                painter.Stroke();
            }
        }
    }
}
