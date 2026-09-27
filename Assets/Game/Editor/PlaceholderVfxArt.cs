using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Draws the combat effects: sparks, slashes, flame, frost, arcane, shadow, dust and the
    /// Guardian's shockwave.
    ///
    /// Not final art. It exists because the questions this phase has to answer are about timing
    /// and readability, not about beauty — does a parry read instantly, can you still see a
    /// wind-up through a burning enemy, is a slam bigger than a sword hit — and those cannot be
    /// judged against nothing. Colours come straight from the Visual Bible's accent palette so
    /// fire, frost, arcane and shadow stay distinguishable from each other and from the tomb.
    ///
    /// Every effect is generated from a fixed seed, so a rebuild rewrites only what changed.
    /// </summary>
    public static class PlaceholderVfxArt
    {
        public const string Root = "Assets/Game/Art/VFX";

        // The accent palette. These are the four elemental languages of the game and they must
        // never drift towards each other, or a player stops being able to tell what hit them.
        private static readonly Color32 FireCore = new Color32(0xFF, 0xD9, 0x8C, 0xFF);
        private static readonly Color32 FireEdge = new Color32(0xFF, 0x8C, 0x33, 0xFF);
        private static readonly Color32 FireSmoke = new Color32(0x7A, 0x24, 0x12, 0xFF);
        private static readonly Color32 FrostCore = new Color32(0xD8, 0xF2, 0xFF, 0xFF);
        private static readonly Color32 FrostEdge = new Color32(0x8F, 0xD6, 0xFF, 0xFF);
        private static readonly Color32 ArcaneCore = new Color32(0xD9, 0xC6, 0xFF, 0xFF);
        private static readonly Color32 ArcaneEdge = new Color32(0xB0, 0x7D, 0xFF, 0xFF);
        private static readonly Color32 ArcaneDeep = new Color32(0x5A, 0x6B, 0xFF, 0xFF);
        private static readonly Color32 ShadowCore = new Color32(0x1A, 0x0F, 0x26, 0xFF);
        private static readonly Color32 ShadowEdge = new Color32(0x9B, 0x4D, 0xFF, 0xFF);
        private static readonly Color32 Steel = new Color32(0xE8, 0xEC, 0xF2, 0xFF);
        private static readonly Color32 SteelWarm = new Color32(0xFF, 0xE8, 0xB8, 0xFF);
        private static readonly Color32 Blood = new Color32(0x8E, 0x1B, 0x28, 0xFF);
        private static readonly Color32 Poison = new Color32(0x7F, 0xA8, 0x3C, 0xFF);
        private static readonly Color32 Dust = new Color32(0x6B, 0x60, 0x52, 0xFF);
        // Footstep dust is lighter than impact dust: on the tomb's dark stone the darker tone vanishes.
        private static readonly Color32 DangerRed = new Color32(0xFF, 0x52, 0x2E, 0xFF);
        // The boss seals: cold, like the arena light they stand in.
        private static readonly Color32 SealBlue = new Color32(0x8F, 0xB8, 0xFF, 0xFF);
        private static readonly Color32 SealPale = new Color32(0xDC, 0xE8, 0xFF, 0xFF);
        private static readonly Color32 DustLight = new Color32(0x9A, 0x8E, 0x7C, 0xFF);
        private static readonly Color32 DustPale = new Color32(0xC4, 0xB8, 0xA4, 0xFF);
        private static readonly Color32 GuardGold = new Color32(0xF0, 0xB4, 0x41, 0xFF);
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>One effect: how many frames and how fast. Combat effects are short by rule.</summary>
        public readonly struct Spec
        {
            public readonly string Name;
            public readonly int Frames;
            public readonly float Fps;

            /// <summary>Height of the pivot: 0.5 bursts from the middle, low values sit on the floor.</summary>
            public readonly float PivotY;

            public Spec(string name, int frames, float fps, float pivotY = 0.5f)
            {
                Name = name;
                Frames = frames;
                Fps = fps;
                PivotY = pivotY;
            }
        }

        /// <summary>
        /// The catalogue, matching `VFX_LIST.md`. Frame counts follow the durations that document
        /// already fixed, so the placeholder and the final set have the same timing.
        /// </summary>
        public static readonly Spec[] Effects =
        {
            new Spec("vfx_hit_light", 4, 26f),
            new Spec("vfx_hit_heavy", 6, 24f),
            new Spec("vfx_hit_critical", 7, 24f),
            new Spec("vfx_block", 5, 25f),
            new Spec("vfx_parry", 8, 24f),
            new Spec("vfx_guard_break", 10, 20f),
            new Spec("vfx_stagger", 8, 20f),
            new Spec("vfx_hurt_flash", 3, 30f),
            new Spec("vfx_fire_impact", 7, 26f),
            new Spec("vfx_frost_impact", 7, 26f),
            new Spec("vfx_arcane_burst", 12, 24f),
            new Spec("vfx_shadow_impact", 7, 24f),
            new Spec("vfx_shadow_dash_start", 4, 26f),
            new Spec("vfx_shadow_dash_end", 4, 26f),
            new Spec("vfx_bleed_proc", 5, 24f),
            new Spec("vfx_poison_proc", 6, 18f),
            new Spec("vfx_burn_proc", 6, 22f),
            new Spec("vfx_frost_proc", 6, 22f),
            new Spec("vfx_dash", 5, 26f),
            new Spec("vfx_air_dash", 5, 26f),
            new Spec("vfx_wall_contact", 4, 26f),
            new Spec("vfx_ground_breaker", 10, 22f),
            new Spec("vfx_dash_strike", 6, 28f),
            new Spec("vfx_guardian_slam", 12, 22f),
            new Spec("vfx_guardian_charge", 8, 22f),
            new Spec("vfx_boss_phase", 16, 18f),
            new Spec("vfx_boss_death", 18, 16f),
            new Spec("vfx_enemy_death", 8, 16f),
            new Spec("vfx_pickup", 6, 20f),
            // Movement dust sits on the floor it rose from, so it pivots at its base.
            new Spec("vfx_dust_jump", 5, 22f, FloorPivot),
            new Spec("vfx_dust_land", 6, 20f, FloorPivot),
            new Spec("vfx_dust_kick", 5, 20f, FloorPivot),
            new Spec("vfx_dust_wall", 4, 16f),
            new Spec("vfx_wall_kick", 5, 22f),
            new Spec("vfx_air_jump", 5, 24f, 0.7f),
            new Spec("vfx_enemy_telegraph", 6, 24f),
            // A loop, not a burst: StunIndicator cycles it while the balance is broken.
            new Spec("vfx_stun_stars", 8, 14f),
            new Spec("vfx_boss_danger", 9, 20f),
            new Spec("vfx_arena_seal", 10, 20f),
            new Spec("vfx_arena_open", 10, 18f),
            // A loop the sealed barrier carries (ArenaSealGlow), not a burst.
            new Spec("vfx_arena_runes", 8, 10f),
            // Magic and altars. The orb is one still frame the CastCharge scales and tints; the
            // fizzle is pale so the spell's own colour can tint it; the kindle is warm already.
            new Spec(CastOrbName, 1, 12f),
            new Spec("vfx_cast_fizzle", 8, 20f),
            new Spec("vfx_altar_kindle", 12, 18f),
            // A loop the lost-gold trace carries while it waits (LostCurrencyMark).
            new Spec(LostGoldName, 8, 8f, FloorPivot)
        };

        public const string LostGoldName = "vfx_lost_gold";

        public const string CastOrbName = "vfx_cast_orb";

        /// <summary>Loops never reach their end: the last frame must lead back into the first.</summary>
        private static bool IsLoop(string name) =>
            name == StunStarsName || name == "vfx_arena_runes" || name == LostGoldName;

        private const float FloorPivot = 0.1f;

        public const string StunStarsName = "vfx_stun_stars";

        /// <summary>Looping overlays that ride on a creature rather than bursting once.</summary>
        public static readonly string[] Overlays =
        {
            "vfx_frost_active", "vfx_burn_active", "vfx_poison_active"
        };

        public static Dictionary<string, Sprite[]> Generate()
        {
            PlaceholderImport.EnsureFolder($"{Root}/Impacts");
            PlaceholderImport.EnsureFolder($"{Root}/Overlays");
            PlaceholderImport.EnsureFolder($"{Root}/Lights");

            var result = new Dictionary<string, Sprite[]>();
            foreach (var spec in Effects)
            {
                var frames = new Sprite[spec.Frames];
                for (var i = 0; i < spec.Frames; i++)
                {
                    // A loop never reaches its end: its last frame must lead back into the first.
                    var t = spec.Frames <= 1 ? 0f
                        : IsLoop(spec.Name) ? i / (float)spec.Frames
                        : i / (float)(spec.Frames - 1);
                    var path = $"{Root}/Impacts/{spec.Name}/{spec.Name}_{i:00}.png";
                    PlaceholderImport.EnsureFolder($"{Root}/Impacts/{spec.Name}");
                    PlaceholderImport.WriteIfChanged(path, Render(spec.Name, t).EncodePng());
                    PlaceholderImport.ConfigureImporter(path, new Vector2(0.5f, spec.PivotY));
                    frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }

                result[spec.Name] = frames;
            }

            foreach (var overlay in Overlays)
            {
                var path = $"{Root}/Overlays/{overlay}.png";
                PlaceholderImport.WriteIfChanged(path, Overlay(overlay).EncodePng());
                PlaceholderImport.ConfigureImporter(path, new Vector2(0.5f, 0.5f));
                result[overlay] = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(path) };
            }

            var lightPath = $"{Root}/Lights/vfx_light_pulse.png";
            PlaceholderImport.WriteIfChanged(lightPath, Glow(220).EncodePng());
            PlaceholderImport.ConfigureImporter(lightPath, new Vector2(0.5f, 0.5f));
            result["vfx_light_pulse"] = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(lightPath) };

            var flashPath = $"{Root}/Lights/vfx_screen_flash.png";
            PlaceholderImport.WriteIfChanged(flashPath, Solid().EncodePng());
            PlaceholderImport.ConfigureImporter(flashPath, new Vector2(0.5f, 0.5f));
            result["vfx_screen_flash"] = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(flashPath) };

            return result;
        }

        // ---------- effects ----------

        private static Canvas Render(string name, float t)
        {
            switch (name)
            {
                case "vfx_hit_light": return Sparks(t, 128, 10, 34f, Steel, 1f);
                case "vfx_hit_heavy": return Sparks(t, 192, 16, 62f, SteelWarm, 1.5f);
                case "vfx_hit_critical": return Critical(t);
                case "vfx_block": return Block(t);
                case "vfx_parry": return Parry(t);
                case "vfx_guard_break": return GuardBreak(t);
                case "vfx_stagger": return Ring(t, 200, Dust, 92f, 5f);
                case "vfx_hurt_flash": return Flash(t, Blood);
                case "vfx_fire_impact": return Elemental(t, FireCore, FireEdge, FireSmoke, 82f);
                case "vfx_frost_impact": return FrostImpact(t);
                case "vfx_arcane_burst": return Arcane(t);
                case "vfx_shadow_impact": return ShadowImpact(t);
                case "vfx_shadow_dash_start": return ShadowPuff(t, true);
                case "vfx_shadow_dash_end": return ShadowPuff(t, false);
                case "vfx_bleed_proc": return Slash(t, 140, Blood, 1f);
                case "vfx_poison_proc": return Motes(t, 150, Poison);
                case "vfx_burn_proc": return Elemental(t, FireCore, FireEdge, FireSmoke, 54f);
                case "vfx_frost_proc": return Crystals(t, 150);
                case "vfx_dash": return Streak(t, 220, Dust, true);
                case "vfx_air_dash": return Streak(t, 220, Steel, false);
                case "vfx_wall_contact": return Sparks(t, 110, 8, 28f, Dust, 0.9f);
                case "vfx_ground_breaker": return GroundImpact(t, 320, Dust, 150f);
                case "vfx_dash_strike": return Slash(t, 220, Steel, 1.4f);
                case "vfx_guardian_slam": return GroundImpact(t, 480, Dust, 230f);
                case "vfx_guardian_charge": return ChargeDust(t);
                case "vfx_boss_phase": return PhasePulse(t);
                case "vfx_boss_death": return BossDeath(t);
                case "vfx_enemy_death": return Motes(t, 170, new Color32(0x2B, 0x1F, 0x3A, 0xFF));
                case "vfx_pickup": return Sparkle(t);
                case "vfx_dust_jump": return DustPuffs(t, 170, 5, 1f, 0.25f, 1f);
                case "vfx_dust_land": return DustPuffs(t, 230, 8, 1f, 0.12f, 1.2f);
                case "vfx_dust_kick": return DustPuffs(t, 150, 5, 0f, 0.2f, 0.9f);
                case "vfx_dust_wall": return WallDust(t, false);
                case "vfx_wall_kick": return WallDust(t, true);
                case "vfx_air_jump": return AirJumpPuff(t);
                case "vfx_enemy_telegraph": return Glint(t);
                case "vfx_stun_stars": return StunStars(t);
                case "vfx_boss_danger": return Danger(t);
                case "vfx_arena_seal": return SealColumn(t, true);
                case "vfx_arena_open": return SealColumn(t, false);
                case "vfx_arena_runes": return RuneColumn(t);
                case CastOrbName: return CastOrb();
                case "vfx_cast_fizzle": return CastFizzle(t);
                case "vfx_altar_kindle": return AltarKindle(t);
                case LostGoldName: return LostGold(t);
                default: return new Canvas(64, 64);
            }
        }

        /// <summary>
        /// The bread-and-butter impact: a bright core that dies fast and sparks thrown along the
        /// hit direction. Everything is drawn pointing right; the burst is rotated to the real hit
        /// direction at runtime.
        /// </summary>
        private static Canvas Sparks(float t, int size, int count, float reach, Color32 tone,
            float coreScale)
        {
            var c = new Canvas(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            var rng = new System.Random(11 + count);
            var fade = Opacity(t);

            // The core: brightest at contact, gone almost immediately.
            var coreRadius = Mathf.Lerp(11f, 2f, t) * coreScale;
            c.Ellipse(centre, coreRadius * 1.9f, coreRadius * 1.5f, Fade(tone, fade * 0.35f));
            c.Ellipse(centre, coreRadius, coreRadius * 0.8f, Fade(tone, fade));

            for (var i = 0; i < count; i++)
            {
                // Biased forward: sparks come off the way the blow was travelling.
                var spread = (float)(rng.NextDouble() - 0.5) * 1.5f;
                var length = reach * Mathf.Lerp(0.35f, 1f, (float)rng.NextDouble());
                var head = centre + new Vector2(Mathf.Cos(spread), Mathf.Sin(spread))
                    * length * Mathf.SmoothStep(0f, 1f, t);
                var tail = Vector2.Lerp(centre, head, 0.55f);
                c.Thick(tail, head, 3.4f * fade, 1f, Fade(tone, fade));
            }

            return c;
        }

        private static Canvas Critical(float t)
        {
            // A crit is sharper, not bigger: a hard star rather than a larger explosion, so it
            // never gets mistaken for a boss-level event.
            var c = Sparks(t, 200, 14, 74f, SteelWarm, 1.2f);
            var centre = new Vector2(100f, 100f);
            var fade = Opacity(t);
            var reach = Mathf.Lerp(18f, 92f, Mathf.SmoothStep(0f, 1f, t));
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI / 2f + Mathf.PI / 4f;
                var arm = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
                c.Thick(centre - arm * 0.15f, centre + arm, 6f * fade, 0.8f,
                    Fade(SteelWarm, fade));
            }

            return c;
        }

        /// <summary>Sparks that scatter off the shield face, spread wide and going nowhere.</summary>
        private static Canvas Block(float t)
        {
            var c = new Canvas(150, 150);
            var centre = new Vector2(75f, 75f);
            var rng = new System.Random(21);
            var fade = Opacity(t);
            c.Ellipse(centre, Mathf.Lerp(13f, 3f, t), Mathf.Lerp(20f, 5f, t), Fade(Steel, fade));
            for (var i = 0; i < 12; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI - Math.PI / 2.0);
                var head = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                    * 56f * Mathf.SmoothStep(0f, 1f, t);
                c.Thick(centre, head, 2.6f * fade, 0.8f, Fade(Steel, fade * 0.9f));
            }

            return c;
        }

        /// <summary>
        /// The most important effect in the game. A hard directional flare plus a ring, so a parry
        /// can never be confused with an ordinary block — it has to be unmistakable at a glance.
        /// </summary>
        private static Canvas Parry(float t)
        {
            var c = new Canvas(256, 256);
            var centre = new Vector2(128f, 128f);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            var ringRadius = Mathf.Lerp(12f, 108f, ease);
            c.Ellipse(centre, ringRadius, ringRadius * 0.86f, Fade(SteelWarm, fade * 0.5f));
            c.Ellipse(centre, ringRadius - 7f, ringRadius * 0.86f - 6f, Clear);

            // Four flares, longest along the direction of the parried blow.
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI / 2f;
                var length = (i == 0 ? 126f : 70f) * Mathf.Lerp(0.5f, 1f, ease);
                var arm = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                c.Thick(centre, centre + arm, 13f * fade, 1f, Fade(SteelWarm, fade));
            }

            c.Ellipse(centre, Mathf.Lerp(24f, 4f, t), Mathf.Lerp(24f, 4f, t),
                Fade(Steel, fade));
            return c;
        }

        private static Canvas GuardBreak(float t)
        {
            var c = new Canvas(256, 256);
            var centre = new Vector2(128f, 128f);
            var rng = new System.Random(31);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            // A shield coming apart: a crack, then fragments thrown outward.
            for (var i = 0; i < 3; i++)
            {
                var angle = -1.1f + i * 1.1f;
                var head = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 74f;
                c.Thick(centre, head, 9f * fade, 2f, Fade(GuardGold, fade * 0.9f));
            }

            for (var i = 0; i < 16; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var distance = Mathf.Lerp(10f, 118f, ease) * (0.5f + (float)rng.NextDouble() * 0.5f);
                var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                var shard = 3f + (float)rng.NextDouble() * 6f;
                c.Ellipse(at, shard * fade, shard * 0.7f * fade, Fade(Steel, fade));
            }

            c.Ellipse(centre, Mathf.Lerp(20f, 60f, ease), Mathf.Lerp(20f, 60f, ease),
                Fade(GuardGold, fade * 0.25f));
            return c;
        }

        private static Canvas Ring(float t, int size, Color32 tone, float maxRadius, float width)
        {
            var c = new Canvas(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            var fade = Opacity(t);
            var radius = Mathf.Lerp(8f, maxRadius, Mathf.SmoothStep(0f, 1f, t));
            c.Ellipse(centre, radius, radius * 0.42f, Fade(tone, fade * 0.8f));
            c.Ellipse(centre, radius - width, radius * 0.42f - width * 0.5f, Clear);
            return c;
        }

        private static Canvas Flash(float t, Color32 tone)
        {
            var c = new Canvas(96, 96);
            c.Ellipse(new Vector2(48f, 48f), 46f, 46f, Fade(tone, (1f - t) * 0.5f));
            return c;
        }

        /// <summary>Fire and burn: a warm core inside a darker plume, never a bright ball.</summary>
        private static Canvas Elemental(float t, Color32 core, Color32 edge, Color32 smoke,
            float reach)
        {
            var size = Mathf.CeilToInt(reach * 2.6f);
            var c = new Canvas(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            var rng = new System.Random(41);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            c.Ellipse(centre, Mathf.Lerp(16f, reach, ease), Mathf.Lerp(16f, reach * 0.8f, ease),
                Fade(smoke, fade * 0.45f));
            c.Ellipse(centre, Mathf.Lerp(12f, reach * 0.62f, ease),
                Mathf.Lerp(12f, reach * 0.5f, ease), Fade(edge, fade * 0.85f));
            c.Ellipse(centre, Mathf.Lerp(8f, reach * 0.24f, ease),
                Mathf.Lerp(8f, reach * 0.2f, ease), Fade(core, fade));

            // Embers rising: what separates fire from every other round burst.
            for (var i = 0; i < 9; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var distance = reach * (0.4f + (float)rng.NextDouble() * 0.7f) * ease;
                var at = centre + new Vector2(Mathf.Cos(angle) * 0.8f, Mathf.Abs(Mathf.Sin(angle)))
                    * distance;
                c.Ellipse(at, 3.4f * fade, 3.4f * fade, Fade(core, fade * 0.9f));
            }

            return c;
        }

        /// <summary>Frost: hard crystalline shards, the opposite shape language to fire.</summary>
        private static Canvas FrostImpact(float t)
        {
            var c = new Canvas(190, 190);
            var centre = new Vector2(95f, 95f);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);
            c.Ellipse(centre, Mathf.Lerp(12f, 52f, ease), Mathf.Lerp(12f, 52f, ease),
                Fade(FrostEdge, fade * 0.35f));
            for (var i = 0; i < 7; i++)
            {
                var angle = i * Mathf.PI * 2f / 7f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var tip = centre + direction * Mathf.Lerp(16f, 84f, ease);
                var side = new Vector2(-direction.y, direction.x) * 7f * fade;
                c.Triangle(centre + side, centre - side, tip, Fade(FrostEdge, fade));
                c.Thick(centre, Vector2.Lerp(centre, tip, 0.65f), 4f * fade, 1f,
                    Fade(FrostCore, fade));
            }

            return c;
        }

        private static Canvas Crystals(float t, int size)
        {
            var c = new Canvas(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            var rng = new System.Random(51);
            var fade = Opacity(t);
            for (var i = 0; i < 10; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                    * (size * 0.3f) * Mathf.SmoothStep(0f, 1f, t);
                var length = 6f + (float)rng.NextDouble() * 12f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                c.Thick(at, at + direction * length, 5f * fade, 1f, Fade(FrostCore, fade));
            }

            return c;
        }

        /// <summary>
        /// Arcane: geometry and runes, expanding as a clean ring. Deliberately nothing like the
        /// Shadow burst, which is organic and smoky — the two must never be confused.
        /// </summary>
        private static Canvas Arcane(float t)
        {
            var c = new Canvas(360, 360);
            var centre = new Vector2(180f, 180f);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            var radius = Mathf.Lerp(20f, 168f, ease);
            c.Ellipse(centre, radius, radius * 0.9f, Fade(ArcaneDeep, fade * 0.3f));
            c.Ellipse(centre, radius - 10f, radius * 0.9f - 9f, Clear);
            c.Ellipse(centre, radius * 0.72f, radius * 0.66f, Fade(ArcaneEdge, fade * 0.45f));
            c.Ellipse(centre, radius * 0.72f - 7f, radius * 0.66f - 6f, Clear);

            // Six spokes: the rune geometry that says "arcane" rather than "explosion".
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f + t * 0.6f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                c.Thick(centre + direction * radius * 0.25f, centre + direction * radius * 0.92f,
                    7f * fade, 2f, Fade(ArcaneEdge, fade * 0.8f));
            }

            c.Ellipse(centre, Mathf.Lerp(26f, 6f, t), Mathf.Lerp(26f, 6f, t),
                Fade(ArcaneCore, fade));
            return c;
        }

        /// <summary>
        /// Shadow: a dark core with a bright violet rim. The rim is what keeps it visible against
        /// the tomb — a pure black effect in a black room is no effect at all.
        /// </summary>
        private static Canvas ShadowImpact(float t)
        {
            var c = new Canvas(200, 200);
            var centre = new Vector2(100f, 100f);
            var rng = new System.Random(61);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            var radius = Mathf.Lerp(14f, 82f, ease);
            c.Ellipse(centre, radius, radius * 0.82f, Fade(ShadowEdge, fade * 0.75f));
            c.Ellipse(centre, radius - 9f, radius * 0.82f - 8f, Fade(ShadowCore, fade * 0.95f));

            for (var i = 0; i < 8; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var at = centre + direction * radius * (0.7f + (float)rng.NextDouble() * 0.5f);
                c.Ellipse(at, 9f * fade, 9f * fade, Fade(ShadowCore, fade * 0.7f));
                c.Ellipse(at, 4f * fade, 4f * fade, Fade(ShadowEdge, fade * 0.6f));
            }

            return c;
        }

        private static Canvas ShadowPuff(float t, bool dissolving)
        {
            var c = new Canvas(150, 150);
            var centre = new Vector2(75f, 75f);
            var rng = new System.Random(dissolving ? 71 : 72);
            // Dissolving gathers inward; re-forming throws outward. Reading one as the other
            // would make a Shadow Dash look like it ended where it started.
            var spread = dissolving ? 1f - t : t;
            var fade = Opacity(t);
            for (var i = 0; i < 12; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 58f * spread;
                c.Ellipse(at, 11f * fade, 11f * fade, Fade(ShadowCore, fade * 0.8f));
                c.Ellipse(at, 5f * fade, 5f * fade, Fade(ShadowEdge, fade * 0.7f));
            }

            return c;
        }

        private static Canvas Slash(float t, int size, Color32 tone, float thickness)
        {
            var c = new Canvas(size, size * 3 / 4);
            var centre = new Vector2(size * 0.5f, size * 0.375f);
            var fade = Opacity(t);
            var length = size * 0.45f * Mathf.Lerp(0.6f, 1f, Mathf.SmoothStep(0f, 1f, t));
            // A curved arc rather than a straight line: a cut, not a laser.
            var steps = 20;
            for (var i = 0; i < steps; i++)
            {
                var a = Mathf.Lerp(-0.7f, 0.7f, i / (float)steps);
                var b = Mathf.Lerp(-0.7f, 0.7f, (i + 1) / (float)steps);
                var from = centre + new Vector2(Mathf.Sin(a) * length, Mathf.Cos(a) * length * 0.4f);
                var to = centre + new Vector2(Mathf.Sin(b) * length, Mathf.Cos(b) * length * 0.4f);
                var taper = Mathf.Sin(i / (float)steps * Mathf.PI);
                c.Thick(from, to, 9f * thickness * taper * fade, 6f * thickness * taper * fade,
                    Fade(tone, fade));
            }

            return c;
        }

        private static Canvas Motes(float t, int size, Color32 tone)
        {
            var c = new Canvas(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.4f);
            var rng = new System.Random(81);
            var fade = Opacity(t);
            for (var i = 0; i < 14; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var rise = t * size * 0.3f * (0.5f + (float)rng.NextDouble());
                var at = centre + new Vector2(Mathf.Cos(angle) * size * 0.22f, rise);
                c.Ellipse(at, 5f * fade, 5f * fade, Fade(tone, fade * 0.8f));
            }

            return c;
        }

        /// <summary>
        /// A ground impact: a low, wide dust ring and debris. Low and wide on purpose — the player
        /// has to be able to tell from the shape that this is something to jump over.
        /// </summary>
        private static Canvas GroundImpact(float t, int size, Color32 tone, float reach)
        {
            var c = new Canvas(size, size / 2);
            var centre = new Vector2(size * 0.5f, size * 0.16f);
            var rng = new System.Random((int)reach);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            var radius = Mathf.Lerp(18f, reach, ease);
            c.Ellipse(centre, radius, radius * 0.3f, Fade(tone, fade * 0.55f));
            c.Ellipse(centre, radius - 12f, radius * 0.3f - 6f, Clear);
            c.Ellipse(centre, radius * 0.5f, radius * 0.18f, Fade(tone, fade * 0.35f));

            for (var i = 0; i < 18; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI);
                var distance = radius * (0.3f + (float)rng.NextDouble() * 0.8f);
                var at = centre + new Vector2(Mathf.Cos(angle) * distance,
                    Mathf.Abs(Mathf.Sin(angle)) * distance * 0.5f);
                var chunk = 3f + (float)rng.NextDouble() * 7f;
                c.Ellipse(at, chunk * fade, chunk * 0.75f * fade, Fade(tone, fade));
            }

            return c;
        }

        private static Canvas ChargeDust(float t)
        {
            var c = new Canvas(300, 150);
            var rng = new System.Random(91);
            var fade = Opacity(t);
            for (var i = 0; i < 20; i++)
            {
                var x = 150f - t * 130f - (float)rng.NextDouble() * 110f;
                var y = 24f + (float)rng.NextDouble() * 46f;
                var puff = 7f + (float)rng.NextDouble() * 13f;
                c.Ellipse(new Vector2(x, y), puff * fade, puff * 0.62f * fade,
                    Fade(Dust, fade * 0.7f));
            }

            return c;
        }

        /// <summary>
        /// Dust rolling out along the floor from the feet. <paramref name="spread"/> 1 throws it
        /// both ways (jump, landing); 0 throws it all behind — drawn to the left, for a run to the
        /// right, and mirrored at runtime. Low and soft: it is a footstep, not an impact.
        /// </summary>
        private static Canvas DustPuffs(float t, int size, int perSide, float spread, float rise,
            float bulk)
        {
            var c = new Canvas(size, size / 2);
            var floor = new Vector2(size * 0.5f, size * 0.5f * FloorPivot);
            var rng = new System.Random(size + perSide);
            var fade = Opacity(t) * 0.8f;
            var ease = 1f - (1f - t) * (1f - t);
            var sides = spread > 0.5f ? new[] { -1f, 1f } : new[] { -1f };
            foreach (var side in sides)
            {
                for (var i = 0; i < perSide; i++)
                {
                    // Spread evenly outward, so the puffs roll out as a line rather than a lump.
                    var reach = size * 0.4f * ((i + 0.3f + 0.4f * (float)rng.NextDouble()) / perSide);
                    var lift = size * rise * (0.3f + (float)rng.NextDouble()) * ease;
                    var at = floor + new Vector2(side * (6f + reach * ease), 7f + lift);
                    var puff = (9f + (float)rng.NextDouble() * 7f) * bulk * Mathf.Lerp(0.6f, 1.35f, t);
                    c.Ellipse(at, puff, puff * 0.7f, Fade(DustLight, fade * 0.7f));
                    c.Ellipse(at + new Vector2(0f, puff * 0.2f), puff * 0.55f, puff * 0.4f,
                        Fade(DustPale, fade * 0.5f));
                }
            }

            return c;
        }

        /// <summary>
        /// Dust off a wall: drawn with the wall on the left and the dust leaving it to the right.
        /// A slide sheds a few flakes that fall; a leap throws a short burst away from the stone.
        /// </summary>
        private static Canvas WallDust(float t, bool leap)
        {
            var c = new Canvas(120, 120);
            var wall = new Vector2(20f, 60f);
            var rng = new System.Random(leap ? 121 : 122);
            var fade = Opacity(t) * 0.8f;
            var count = leap ? 7 : 3;
            for (var i = 0; i < count; i++)
            {
                var angle = Mathf.Lerp(-1.1f, 1.1f, (i + 0.5f) / count) + (float)(rng.NextDouble() - 0.5) * 0.3f;
                var reach = (leap ? 70f : 26f) * (0.5f + 0.5f * (float)rng.NextDouble()) * t;
                var fall = leap ? 0f : t * 30f;
                var at = wall + new Vector2(Mathf.Cos(angle) * reach, Mathf.Sin(angle) * reach * 0.8f - fall);
                var puff = (leap ? 7f : 4.5f) + (float)rng.NextDouble() * 4f;
                c.Ellipse(at, puff, puff * 0.75f, Fade(DustLight, fade * 0.75f));
            }

            return c;
        }

        /// <summary>
        /// The second jump, pushed off nothing: a pale flattened ring under the feet and a few
        /// motes thrown down. Pale steel rather than dust, since there is no floor to raise dust.
        /// </summary>
        private static Canvas AirJumpPuff(float t)
        {
            var c = new Canvas(150, 100);
            var centre = new Vector2(75f, 70f);
            var fade = Opacity(t) * 0.7f;
            var radius = Mathf.Lerp(12f, 58f, 1f - (1f - t) * (1f - t));
            c.Ellipse(centre, radius, radius * 0.25f, Fade(Steel, fade * 0.45f));
            c.Ellipse(centre, radius - 5f, radius * 0.25f - 3f, Clear);
            var rng = new System.Random(131);
            for (var i = 0; i < 6; i++)
            {
                var x = centre.x + ((float)rng.NextDouble() - 0.5f) * radius * 1.4f;
                var y = centre.y - t * (25f + (float)rng.NextDouble() * 30f);
                c.Ellipse(new Vector2(x, y), 3.5f * fade + 1f, 3.5f * fade + 1f, Fade(Steel, fade * 0.8f));
            }

            return c;
        }

        /// <summary>
        /// The glint on an enemy's weapon as its swing winds up: a four-pointed star that flares
        /// and shrinks. Guard gold, the same warning colour as the sigil over the head, so the two
        /// read as one language.
        /// </summary>
        private static Canvas Glint(float t)
        {
            var c = new Canvas(130, 130);
            var centre = new Vector2(65f, 65f);
            // Flares for the first third, then shrinks away.
            var size = t < 0.3f ? Mathf.Lerp(0.3f, 1f, t / 0.3f) : Mathf.Lerp(1f, 0f, (t - 0.3f) / 0.7f);
            var fade = Mathf.Clamp01(size * 1.4f);
            c.Ellipse(centre, 20f * size, 20f * size, Fade(GuardGold, fade * 0.3f));
            foreach (var axis in new[] { Vector2.up, Vector2.right })
            {
                var reach = (axis == Vector2.up ? 58f : 44f) * size;
                c.Thick(centre - axis * reach, centre, 0.5f, 7f * size + 1f, Fade(SteelWarm, fade));
                c.Thick(centre, centre + axis * reach, 7f * size + 1f, 0.5f, Fade(SteelWarm, fade));
            }

            c.Ellipse(centre, 6f * size + 1f, 6f * size + 1f, Fade(Steel, fade));
            return c;
        }

        /// <summary>
        /// Three small stars circling on a flat ellipse over the head; one loop over the frames.
        /// Those behind the head are drawn smaller and dimmer so the circle reads as a ring.
        /// </summary>
        private static Canvas StunStars(float t)
        {
            var c = new Canvas(140, 70);
            var centre = new Vector2(70f, 35f);
            for (var i = 0; i < 3; i++)
            {
                var angle = (t + i / 3f) * Mathf.PI * 2f;
                var depth = (Mathf.Sin(angle) + 1f) * 0.5f;
                var at = centre + new Vector2(Mathf.Cos(angle) * 50f, Mathf.Sin(angle) * 14f);
                var size = Mathf.Lerp(0.6f, 1f, depth);
                var alpha = Mathf.Lerp(0.55f, 1f, depth);
                foreach (var axis in new[] { Vector2.up, Vector2.right })
                {
                    c.Thick(at - axis * 11f * size, at, 0.5f, 4.5f * size, Fade(GuardGold, alpha));
                    c.Thick(at, at + axis * 11f * size, 4.5f * size, 0.5f, Fade(GuardGold, alpha));
                }

                c.Ellipse(at, 3.5f * size, 3.5f * size, Fade(SteelWarm, alpha));
            }

            return c;
        }

        /// <summary>
        /// A boss about to charge or leap: a red ring drawn in toward the body with spikes
        /// pointing at it, so it reads as gathering, not as an impact.
        /// </summary>
        private static Canvas Danger(float t)
        {
            var c = new Canvas(360, 360);
            var centre = new Vector2(180f, 180f);
            var fade = Mathf.Sin(t * Mathf.PI);
            var radius = Mathf.Lerp(170f, 70f, Mathf.SmoothStep(0f, 1f, t));
            c.Ellipse(centre, radius + 10f, radius + 10f, Fade(DangerRed, fade * 0.18f));
            c.Ellipse(centre, radius, radius, Fade(DangerRed, fade * 0.6f));
            c.Ellipse(centre, radius - 12f, radius - 12f, Clear);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f + Mathf.PI / 8f;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                c.Thick(centre + dir * (radius + 24f), centre + dir * (radius - 20f), 11f, 0.5f,
                    Fade(DangerRed, fade * 0.9f));
            }

            return c;
        }

        /// <summary>One rune glyph: a stave with two strokes, the Tomb's warding mark.</summary>
        private static void Rune(Canvas c, Vector2 at, float size, Color32 tone, int variant)
        {
            c.Thick(at + new Vector2(0f, -size), at + new Vector2(0f, size), 3.5f, 3.5f, tone);
            var flip = variant % 2 == 0 ? 1f : -1f;
            c.Thick(at + new Vector2(0f, size * 0.55f), at + new Vector2(size * 0.6f * flip, size * 0.1f),
                3f, 3f, tone);
            c.Thick(at + new Vector2(0f, -size * 0.1f), at + new Vector2(-size * 0.6f * flip, -size * 0.55f),
                3f, 3f, tone);
        }

        private static readonly float[] RuneHeights = { 0.12f, 0.31f, 0.5f, 0.69f, 0.88f };

        /// <summary>
        /// The sealed barrier's runes, breathing in turn from the bottom up; drawn a unit wide and
        /// seven tall, the barrier's own size.
        /// </summary>
        private static Canvas RuneColumn(float t)
        {
            var c = new Canvas(100, 700);
            for (var i = 0; i < RuneHeights.Length; i++)
            {
                var glow = 0.55f + 0.45f * Mathf.Sin((t - i * 0.2f) * Mathf.PI * 2f);
                var at = new Vector2(50f, 700f * RuneHeights[i]);
                c.Ellipse(at, 34f, 40f, Fade(SealBlue, glow * 0.22f));
                Rune(c, at, 26f, Fade(SealPale, 0.5f + glow * 0.5f), i);
            }

            return c;
        }

        /// <summary>
        /// The seal closing: a column of light that flares and settles into the runes — or, when
        /// it breaks, the runes flaring once and scattering upward.
        /// </summary>
        private static Canvas SealColumn(float t, bool closing)
        {
            var c = new Canvas(160, 740);
            var fade = closing ? 1f - t : Opacity(t);
            var width = closing ? Mathf.Lerp(70f, 12f, t) : Mathf.Lerp(20f, 60f, t);
            c.Thick(new Vector2(80f, 20f), new Vector2(80f, 720f), width, width, Fade(SealBlue, fade * 0.35f));
            c.Thick(new Vector2(80f, 20f), new Vector2(80f, 720f), width * 0.3f, width * 0.3f,
                Fade(SealPale, fade * 0.6f));
            var rng = new System.Random(closing ? 141 : 142);
            for (var i = 0; i < RuneHeights.Length; i++)
            {
                var rise = closing ? 0f : t * (60f + (float)rng.NextDouble() * 80f);
                var drift = closing ? 0f : ((float)rng.NextDouble() - 0.5f) * 60f * t;
                var at = new Vector2(80f + drift, 20f + 700f * RuneHeights[i] + rise);
                Rune(c, at, 26f * (closing ? Mathf.Lerp(1.6f, 1f, t) : 1f), Fade(SealPale, fade), i);
            }

            return c;
        }

        private static Canvas Streak(float t, int size, Color32 tone, bool withDust)
        {
            var c = new Canvas(size, size / 2);
            var centre = new Vector2(size * 0.5f, size * 0.25f);
            var fade = Opacity(t);
            var length = size * 0.42f * Mathf.Lerp(0.5f, 1f, t);
            c.Thick(centre - new Vector2(length, 0f), centre + new Vector2(length * 0.2f, 0f),
                2f, 16f * fade, Fade(tone, fade * 0.65f));
            if (!withDust)
            {
                return c;
            }

            var rng = new System.Random(101);
            for (var i = 0; i < 8; i++)
            {
                var at = centre + new Vector2(-(float)rng.NextDouble() * length,
                    -14f + (float)rng.NextDouble() * 10f);
                c.Ellipse(at, 8f * fade, 5f * fade, Fade(Dust, fade * 0.6f));
            }

            return c;
        }

        /// <summary>
        /// The phase change: the armour cracking and dark energy escaping. Large, but a ring and
        /// cracks rather than a wall of smoke — the Guardian has to stay visible through it.
        /// </summary>
        private static Canvas PhasePulse(float t)
        {
            var c = new Canvas(420, 420);
            var centre = new Vector2(210f, 210f);
            var rng = new System.Random(111);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            var radius = Mathf.Lerp(24f, 196f, ease);
            c.Ellipse(centre, radius, radius * 0.92f, Fade(ShadowEdge, fade * 0.35f));
            c.Ellipse(centre, radius - 14f, radius * 0.92f - 13f, Clear);

            for (var i = 0; i < 10; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                c.Thick(centre + direction * 18f, centre + direction * radius * 0.85f,
                    10f * fade, 1.5f, Fade(ShadowCore, fade * 0.8f));
                c.Thick(centre + direction * 18f, centre + direction * radius * 0.6f,
                    4f * fade, 1f, Fade(ShadowEdge, fade * 0.85f));
            }

            return c;
        }

        private static Canvas BossDeath(float t)
        {
            var c = new Canvas(460, 460);
            var centre = new Vector2(230f, 230f);
            var rng = new System.Random(121);
            var fade = Opacity(t);
            var ease = Mathf.SmoothStep(0f, 1f, t);

            for (var ring = 0; ring < 3; ring++)
            {
                var phase = Mathf.Clamp01(ease - ring * 0.2f);
                var radius = Mathf.Lerp(20f, 210f - ring * 30f, phase);
                c.Ellipse(centre, radius, radius * 0.9f,
                    Fade(ring == 1 ? ShadowEdge : ShadowCore, fade * 0.3f));
                c.Ellipse(centre, radius - 9f, radius * 0.9f - 8f, Clear);
            }

            for (var i = 0; i < 20; i++)
            {
                var angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                var distance = Mathf.Lerp(16f, 190f, ease) * (0.4f + (float)rng.NextDouble() * 0.7f);
                var at = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                c.Ellipse(at, 9f * fade, 9f * fade, Fade(ShadowCore, fade * 0.75f));
                c.Ellipse(at, 4f * fade, 4f * fade, Fade(ShadowEdge, fade * 0.7f));
            }

            return c;
        }

        private static Canvas Sparkle(float t)
        {
            var c = new Canvas(110, 110);
            var centre = new Vector2(55f, 55f);
            var fade = Opacity(t);
            var reach = Mathf.Lerp(8f, 38f, Mathf.SmoothStep(0f, 1f, t));
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI / 2f;
                var arm = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * reach;
                c.Thick(centre, centre + arm, 5f * fade, 1f, Fade(GuardGold, fade));
            }

            c.Ellipse(centre, 7f * fade, 7f * fade, Fade(SteelWarm, fade));
            return c;
        }

        // ---------- overlays ----------

        /// <summary>
        /// A looping overlay worn by a creature. Each is a rim or a scatter rather than a fill,
        /// because a status must never repaint a silhouette the player is trying to read.
        /// </summary>
        private static Canvas Overlay(string name)
        {
            var c = new Canvas(160, 200);
            var centre = new Vector2(80f, 100f);
            var rng = new System.Random(name.GetHashCode() & 0x7FFFFFFF);
            switch (name)
            {
                case "vfx_frost_active":
                    // A cold rim following the body outline, never a block of ice.
                    for (var i = 0; i < 26; i++)
                    {
                        var angle = i / 26f * Mathf.PI * 2f;
                        var at = centre + new Vector2(Mathf.Cos(angle) * 46f,
                            Mathf.Sin(angle) * 82f);
                        c.Ellipse(at, 7f, 7f, Fade(FrostEdge, 0.55f));
                        c.Ellipse(at, 3f, 3f, Fade(FrostCore, 0.7f));
                    }

                    break;

                case "vfx_burn_active":
                    for (var i = 0; i < 9; i++)
                    {
                        var x = 34f + (float)rng.NextDouble() * 92f;
                        var y = 40f + (float)rng.NextDouble() * 96f;
                        var height = 16f + (float)rng.NextDouble() * 22f;
                        c.Ellipse(new Vector2(x, y), 9f, height * 0.5f, Fade(FireSmoke, 0.45f));
                        c.Ellipse(new Vector2(x, y + 4f), 5f, height * 0.4f, Fade(FireEdge, 0.7f));
                        c.Ellipse(new Vector2(x, y + 7f), 2.5f, height * 0.22f,
                            Fade(FireCore, 0.8f));
                    }

                    break;

                default:
                    for (var i = 0; i < 14; i++)
                    {
                        var x = 30f + (float)rng.NextDouble() * 100f;
                        var y = 30f + (float)rng.NextDouble() * 140f;
                        c.Ellipse(new Vector2(x, y), 5f, 5f, Fade(Poison, 0.55f));
                    }

                    break;
            }

            return c;
        }

        private static Canvas Glow(byte peak)
        {
            var c = new Canvas(256, 256);
            var centre = new Vector2(128f, 128f);
            for (var y = 0; y < 256; y++)
            {
                for (var x = 0; x < 256; x++)
                {
                    var d = Vector2.Distance(new Vector2(x, y), centre) / 128f;
                    if (d >= 1f)
                    {
                        continue;
                    }

                    var falloff = (1f - d) * (1f - d);
                    c.Set(x, y, new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(falloff * peak)));
                }
            }

            return c;
        }

        private static Canvas Solid()
        {
            var c = new Canvas(8, 8);
            c.Rect(0, 0, 8, 8, new Color32(255, 255, 255, 255));
            return c;
        }

        /// <summary>
        /// How solid an effect is at a moment in its life.
        ///
        /// A linear fade looked translucent for its whole duration, which made every impact feel
        /// weak and washed out. An effect should be solid while it is saying something and then
        /// get out of the way quickly, so this holds full opacity through the first two thirds and
        /// drops off sharply after.
        /// </summary>
        /// <summary>
        /// The gathered spell: a soft halo round a hot core, white so the caster can colour it.
        /// </summary>
        private static Canvas CastOrb()
        {
            var c = new Canvas(64, 64);
            var centre = new Vector2(32f, 32f);
            var white = new Color32(255, 255, 255, 255);
            c.Ellipse(centre, 30f, 30f, Fade(white, 0.18f));
            c.Ellipse(centre, 22f, 22f, Fade(white, 0.35f));
            c.Ellipse(centre, 14f, 14f, Fade(white, 0.75f));
            c.Ellipse(centre, 8f, 8f, white);
            return c;
        }

        /// <summary>
        /// The trace a death leaves: a dark wisp of the hero's shape rising from the floor, the
        /// height of a kneeling figure, with gold motes turning slowly around it. Dark so it is not
        /// mistaken for an altar; the motes and its own light are what catch the eye.
        /// </summary>
        private static Canvas LostGold(float t)
        {
            var c = new Canvas(140, 180);
            var phase = t * Mathf.PI * 2f;
            var dark = new Color32(0x1C, 0x14, 0x24, 0xFF);
            var rim = new Color32(0x5E, 0x4A, 0x6E, 0xFF);
            var gold = new Color32(0xFF, 0xD2, 0x66, 0xFF);

            // The wisp: stacked ellipses narrowing upwards, swaying as one.
            for (var i = 0; i < 9; i++)
            {
                var k = i / 8f;
                var sway = Mathf.Sin(phase + k * 2.4f) * 6f * k;
                var centre = new Vector2(70f + sway, 18f + k * 118f);
                var width = Mathf.Lerp(30f, 8f, k * k);
                c.Ellipse(centre, width + 3f, 15f, Fade(rim, 0.35f * (1f - k * 0.5f)));
                c.Ellipse(centre, width, 13f, Fade(dark, 0.9f - k * 0.45f));
            }

            // A hood's hint at the top, so it reads as the hero's own shade.
            var head = new Vector2(70f + Mathf.Sin(phase + 2.4f) * 6f, 128f);
            c.Ellipse(head, 15f, 17f, Fade(dark, 0.8f));

            // Coins of light circling it: on the near side bright, behind it dim.
            for (var i = 0; i < 7; i++)
            {
                var a = phase + i * Mathf.PI * 2f / 7f;
                var p = new Vector2(70f + Mathf.Cos(a) * 34f, 40f + i * 11f + Mathf.Sin(a) * 6f);
                var near = Mathf.Sin(a) < 0f ? 1f : 0.45f;
                c.Ellipse(p, 5f, 5f, Fade(gold, 0.25f * near));
                c.Ellipse(p, 2.6f, 2.6f, Fade(gold, near));
            }

            c.Ellipse(new Vector2(70f, 10f), 40f, 6f, Fade(gold, 0.22f));
            return c;
        }

        /// <summary>
        /// A broken cast: the shape bursts into a ragged ring and its sparks fall away dimming —
        /// falling, not flying, so it reads as lost rather than as an attack. Pale, to be tinted.
        /// </summary>
        private static Canvas CastFizzle(float t)
        {
            var c = new Canvas(150, 150);
            var centre = new Vector2(75f, 88f);
            var pale = new Color32(235, 235, 240, 255);
            var fade = Mathf.Clamp01((1f - t) * 1.6f);
            var rng = new System.Random(29);
            var ring = Mathf.Lerp(10f, 30f, Mathf.Sqrt(t));
            for (var i = 0; i < 9; i++)
            {
                // Ragged: every other arc is missing, the broken shape of the spell.
                var a = i * Mathf.PI * 2f / 9f + (float)rng.NextDouble() * 0.3f;
                var from = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ring;
                var to = centre + new Vector2(Mathf.Cos(a + 0.35f), Mathf.Sin(a + 0.35f)) * ring;
                c.Thick(from, to, 3f * fade, 2f * fade, Fade(pale, fade * 0.7f));
            }

            for (var i = 0; i < 12; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var speed = Mathf.Lerp(24f, 46f, (float)rng.NextDouble());
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * speed * t
                    + Vector2.down * 60f * t * t;
                var r = Mathf.Lerp(3.2f, 1f, t);
                c.Ellipse(p, r, r, Fade(pale, fade));
            }

            return c;
        }

        /// <summary>
        /// An altar catching: a warm flash, a ring of light rolling out across the stone and
        /// tongues of flame and embers rising. Big and slow next to a hit — it is the one moment
        /// of safety in the level, not a blow.
        /// </summary>
        private static Canvas AltarKindle(float t)
        {
            var c = new Canvas(240, 240);
            var centre = new Vector2(120f, 110f);
            var flash = Mathf.Clamp01(1f - t * 3f);
            c.Ellipse(centre, 46f * (0.6f + flash), 46f * (0.6f + flash), Fade(FireCore, flash * 0.7f));
            var fade = Mathf.Clamp01((1f - t) * 1.4f);
            var ring = Mathf.Lerp(18f, 110f, Mathf.Sqrt(t));
            var width = Mathf.Lerp(9f, 2f, t);
            for (var i = 0; i < 36; i++)
            {
                var a = i * Mathf.PI * 2f / 36f;
                var b = (i + 1) * Mathf.PI * 2f / 36f;
                c.Thick(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.55f) * ring,
                    centre + new Vector2(Mathf.Cos(b), Mathf.Sin(b) * 0.55f) * ring, width, width,
                    Fade(SteelWarm, fade * 0.6f));
            }

            var rng = new System.Random(41);
            for (var i = 0; i < 14; i++)
            {
                var x = centre.x + Mathf.Lerp(-40f, 40f, (float)rng.NextDouble());
                var rise = Mathf.Lerp(40f, 110f, (float)rng.NextDouble()) * t;
                var r = Mathf.Lerp(3.5f, 1f, t) * Mathf.Lerp(0.7f, 1.2f, (float)rng.NextDouble());
                c.Ellipse(new Vector2(x, centre.y + rise), r, r * 1.4f, Fade(FireCore, fade));
            }

            return c;
        }

        private static float Opacity(float t) =>
            Mathf.Clamp01((1f - Mathf.Clamp01(t)) * 2.6f);

        private static Color32 Fade(Color32 tone, float alpha) =>
            new Color32(tone.r, tone.g, tone.b,
                (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * tone.a));
    }
}
