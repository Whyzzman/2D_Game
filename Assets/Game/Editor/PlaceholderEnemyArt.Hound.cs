using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The Corrupted Hound, drawn on its own four-legged rig.
    ///
    /// Everything else in the game is a soldier, so everything else shares one body. This one is
    /// not, and bending the humanoid rig into a quadruped produces something that reads as broken
    /// rather than as an animal. The rig here is deliberately small: a spine, four two-segment
    /// legs, a head that leads the body and a tail that trails it. That is enough for the frames
    /// the fight actually needs — a low prowl, a run, a long crouch and a fully extended leap.
    ///
    /// The crouch is the important one. It is more than a third of the leap clip, because the leap
    /// has a 0.52 s windup and the player is expected to beat it on reaction; if the crouch does
    /// not read, the attack is unfair rather than hard.
    /// </summary>
    public static partial class PlaceholderEnemyArt
    {
        /// <summary>A four-legged frame. Angles are degrees, zero straight down.</summary>
        private struct HoundPose
        {
            public float SpineTilt;
            public float BodyY;
            public float BodyX;
            public float FrontUpper;
            public float FrontLower;
            public float RearUpper;
            public float RearLower;
            public float OffFrontUpper;
            public float OffRearUpper;
            public float Neck;
            public float HeadTilt;
            public float Jaw;
            public float Tail;
            public float Squash;
            public float Alpha;
            public float Glow;
            public float EyeFlare;

            public static HoundPose Stand => new HoundPose
            {
                SpineTilt = -4f, BodyY = 0f, BodyX = 0f,
                FrontUpper = 6f, FrontLower = -12f, RearUpper = -10f, RearLower = 16f,
                OffFrontUpper = -6f, OffRearUpper = 4f,
                Neck = 24f, HeadTilt = -8f, Jaw = 0f, Tail = -20f,
                Squash = 1f, Alpha = 1f, Glow = 0f
            };
        }

        private static byte[] RenderHound(EnemyAnimationState state, float t)
        {
            var build = ForestBuildFor(Actor.Hound);
            var canvas = new Canvas(build.Width, build.Height);
            DrawHound(canvas, build, HoundTelegraphed(state, t, HoundPoseFor(state, t)));
            canvas.Silhouette(new Color32(6, 5, 5, 225), EnemyRim, 0.42f);
            return canvas.EncodePng();
        }

        private static HoundPose HoundPoseFor(EnemyAnimationState state, float t)
        {
            var pose = HoundPose.Stand;
            switch (state)
            {
                case EnemyAnimationState.Idle:
                    // Breathing, head low and swinging slightly. Not asleep — waiting.
                    pose.BodyY = Wave(t) * 1.2f;
                    pose.Neck = 26f + Wave(t) * 3f;
                    pose.HeadTilt = -6f + Wave(t * 0.5f) * 4f;
                    pose.Tail = -18f + Wave(t) * 8f;
                    break;

                case EnemyAnimationState.Patrol:
                    // A loose trot: diagonal pairs, low amplitude.
                    pose.BodyY = Mathf.Abs(Wave(t)) * 2f;
                    pose.FrontUpper = Wave(t) * 26f;
                    pose.OffFrontUpper = -Wave(t) * 26f;
                    pose.RearUpper = -Wave(t) * 24f;
                    pose.OffRearUpper = Wave(t) * 24f;
                    pose.FrontLower = -14f - Mathf.Max(0f, Wave(t)) * 16f;
                    pose.RearLower = 14f + Mathf.Max(0f, -Wave(t)) * 16f;
                    pose.Tail = -22f + Wave(t) * 10f;
                    break;

                case EnemyAnimationState.Alert:
                    // Head snaps up and round, shoulders drop. One beat, and it must be legible.
                    var notice = Ease(t);
                    pose.Neck = Mathf.Lerp(26f, -6f, notice);
                    pose.HeadTilt = Mathf.Lerp(-6f, -26f, notice);
                    pose.SpineTilt = Mathf.Lerp(-4f, 4f, notice);
                    pose.BodyY = Mathf.Lerp(0f, -3f, notice);
                    pose.Tail = Mathf.Lerp(-20f, 34f, notice);
                    pose.Jaw = Mathf.Lerp(0f, 12f, notice);
                    pose.Glow = notice * 0.6f;
                    break;

                case EnemyAnimationState.Chase:
                    // A bound, not a trot: the whole spine flexes and both pairs leave the ground.
                    var bound = Wave(t);
                    pose.BodyY = 2.5f + bound * 4.5f;
                    pose.SpineTilt = -6f + bound * 8f;
                    pose.FrontUpper = bound * 44f;
                    pose.OffFrontUpper = bound * 34f;
                    pose.RearUpper = -bound * 46f;
                    pose.OffRearUpper = -bound * 36f;
                    pose.FrontLower = -18f - Mathf.Max(0f, bound) * 30f;
                    pose.RearLower = 18f + Mathf.Max(0f, -bound) * 30f;
                    pose.Neck = 12f;
                    pose.HeadTilt = -14f;
                    pose.Tail = -34f;
                    pose.Jaw = 8f;
                    pose.Glow = 0.4f;
                    break;

                case EnemyAnimationState.AttackLight:
                    // The bite: a short snap forward from the shoulders, jaw wide at the strike.
                    var snap = Ease(Mathf.Clamp01((t - 0.34f) / 0.26f));
                    var recover = Ease(Mathf.Clamp01((t - 0.62f) / 0.38f));
                    pose.BodyX = Mathf.Lerp(-3f, 5f, snap) - recover * 3f;
                    pose.SpineTilt = Mathf.Lerp(-10f, 6f, snap);
                    pose.Neck = Mathf.Lerp(30f, 2f, snap) + recover * 16f;
                    pose.HeadTilt = Mathf.Lerp(4f, -18f, snap);
                    pose.Jaw = Mathf.Lerp(4f, 34f, snap) * (1f - recover);
                    pose.FrontUpper = Mathf.Lerp(-12f, 20f, snap);
                    pose.RearUpper = Mathf.Lerp(6f, -18f, snap);
                    pose.Tail = Mathf.Lerp(-10f, -38f, snap);
                    pose.Glow = 0.5f;
                    break;

                case EnemyAnimationState.AttackHeavy:
                    return HoundLeap(t);

                case EnemyAnimationState.Hurt:
                    pose.SpineTilt = -16f;
                    pose.BodyX = -4f * (1f - Ease(t));
                    pose.BodyY = -3f;
                    pose.Neck = 36f;
                    pose.HeadTilt = 16f;
                    pose.Tail = 6f;
                    pose.FrontUpper = -16f;
                    pose.RearUpper = 14f;
                    break;

                case EnemyAnimationState.Stunned:
                    pose.SpineTilt = -2f + Wave(t) * 4f;
                    pose.BodyY = -6f;
                    pose.Neck = 42f + Wave(t) * 5f;
                    pose.HeadTilt = 24f;
                    pose.Jaw = 14f;
                    pose.Tail = -4f;
                    pose.FrontUpper = -20f;
                    pose.FrontLower = -30f;
                    pose.RearUpper = 18f;
                    pose.RearLower = 30f;
                    break;

                default:
                    // Death: the legs give out, then it settles and the corruption goes dark.
                    var fall = Ease(t);
                    pose.SpineTilt = Mathf.Lerp(-4f, 14f, fall);
                    pose.BodyY = Mathf.Lerp(0f, -22f, fall);
                    pose.Neck = Mathf.Lerp(26f, 58f, fall);
                    pose.HeadTilt = Mathf.Lerp(-6f, 30f, fall);
                    pose.FrontUpper = Mathf.Lerp(6f, -52f, fall);
                    pose.FrontLower = Mathf.Lerp(-12f, -64f, fall);
                    pose.RearUpper = Mathf.Lerp(-10f, 54f, fall);
                    pose.RearLower = Mathf.Lerp(16f, 62f, fall);
                    pose.Tail = Mathf.Lerp(-20f, 4f, fall);
                    pose.Jaw = Mathf.Lerp(0f, 18f, fall);
                    pose.Squash = Mathf.Lerp(1f, 0.82f, fall);
                    pose.Glow = Mathf.Max(0f, 1f - fall * 2f);
                    pose.Alpha = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01((t - 0.72f) / 0.28f));
                    break;
            }

            return pose;
        }

        /// <summary>
        /// The leap. Thirty-eight percent of the clip is the crouch, because that is roughly the
        /// share of the 0.52 s windup in the whole move — the pose the dash is timed against gets
        /// the frames, and the flight, which nothing can be done about, gets few.
        /// </summary>
        private static HoundPose HoundLeap(float t)
        {
            var pose = HoundPose.Stand;
            var crouch = Ease(Mathf.Clamp01(t / 0.38f));
            var launch = Ease(Mathf.Clamp01((t - 0.40f) / 0.16f));
            var land = Ease(Mathf.Clamp01((t - 0.74f) / 0.26f));

            pose.BodyY = Mathf.Lerp(0f, -9f, crouch) + launch * 22f - land * 20f;
            pose.BodyX = launch * 10f;
            pose.SpineTilt = Mathf.Lerp(-4f, -22f, crouch) + launch * 30f + land * 10f;
            // Coiled: everything folds under the body, then extends all at once.
            pose.FrontUpper = Mathf.Lerp(6f, -46f, crouch) + launch * 96f - land * 40f;
            pose.FrontLower = Mathf.Lerp(-12f, -58f, crouch) + launch * 54f;
            pose.OffFrontUpper = Mathf.Lerp(-6f, -40f, crouch) + launch * 84f - land * 36f;
            pose.RearUpper = Mathf.Lerp(-10f, 44f, crouch) - launch * 92f + land * 30f;
            pose.RearLower = Mathf.Lerp(16f, 62f, crouch) - launch * 46f;
            pose.OffRearUpper = Mathf.Lerp(4f, 38f, crouch) - launch * 80f + land * 26f;
            pose.Neck = Mathf.Lerp(24f, 8f, crouch) - launch * 14f + land * 18f;
            pose.HeadTilt = Mathf.Lerp(-8f, -22f, crouch);
            // The jaw opens only once it is committed, so the crouch is the tell and not this.
            pose.Jaw = launch * 36f * (1f - land * 0.6f);
            pose.Tail = Mathf.Lerp(-20f, 26f, crouch) - launch * 70f;
            pose.Squash = Mathf.Lerp(1f, 0.92f, crouch) * Mathf.Lerp(1f, 1.06f, launch);
            pose.Glow = Mathf.Max(crouch, 1f - land);
            return pose;
        }

        /// <summary>The hound's windup made clearer: it sinks lower, pulls its head back, eyes flare.</summary>
        private static HoundPose HoundTelegraphed(EnemyAnimationState state, float t, HoundPose pose)
        {
            var peak = state == EnemyAnimationState.AttackLight ? 0.34f
                : state == EnemyAnimationState.AttackHeavy ? 0.4f : -1f;
            if (peak < 0f)
            {
                return pose;
            }

            var w = Windup(t, peak);
            pose.BodyY -= 4f * w;
            pose.SpineTilt -= 4f * w;
            pose.Neck += 6f * w;
            pose.EyeFlare = w;
            return pose;
        }

        private static void DrawHound(Canvas canvas, Build build, HoundPose pose)
        {
            // The spine runs back to front across the canvas, with the head towards +x, which is
            // the facing direction every other actor is drawn in.
            var centre = new Vector2(canvas.Width * 0.5f + pose.BodyX,
                (build.HipY + pose.BodyY) * pose.Squash);
            var halfSpine = 34f;
            var rear = centre + Canvas.Rotate(new Vector2(-halfSpine, 0f), pose.SpineTilt);
            var front = centre + Canvas.Rotate(new Vector2(halfSpine, 0f), pose.SpineTilt);

            // Far-side legs first: depth is draw order.
            canvas.Limb(front, pose.OffFrontUpper, 20f, pose.FrontLower, 18f, 5f, build.Metal);
            canvas.Limb(rear, pose.OffRearUpper, 22f, pose.RearLower, 20f, 5.5f, build.Metal);

            // Tail: only its root is drawn; the rest is the live tail hung from the stub's end
            // (visual item 4), which carries on the angle the pose gives it.
            var tailAngle = pose.SpineTilt + pose.Tail;
            var tailStub = rear + Canvas.Rotate(new Vector2(-9f, 0f), tailAngle);
            canvas.Thick(rear, tailStub, 5.5f, 4.5f, build.Armor);
            Anchor(canvas, tailStub, HangAngle(Canvas.Rotate(new Vector2(-1f, 0f), tailAngle)));

            // Body: haunch heavier than chest, so it reads as built for springing.
            canvas.Thick(rear, front, 22f, 17f, build.Armor);
            canvas.Ellipse(rear + Canvas.Rotate(new Vector2(-4f, 1f), pose.SpineTilt), 15f, 13f,
                build.Armor);

            // Exposed ribs, over the body only so they can never break the silhouette.
            for (var i = 0; i < 4; i++)
            {
                var at = Vector2.Lerp(centre, front, 0.15f + i * 0.2f);
                canvas.RectOver(at.x - 1.2f, at.y - 8f, at.x + 1.2f, at.y + 7f, build.Metal);
            }

            // Near-side legs.
            canvas.Limb(front, pose.FrontUpper, 21f, pose.FrontLower, 19f, 5.5f, build.Armor);
            canvas.Limb(rear, pose.RearUpper, 23f, pose.RearLower, 21f, 6f, build.Armor);

            // Neck and head: the head leads the body rather than sitting above it.
            var head = front + Canvas.Rotate(new Vector2(20f, 0f), pose.SpineTilt - pose.Neck + 90f);
            canvas.Thick(front, head, 11f, 9f, build.Armor);
            var tilt = pose.SpineTilt + pose.HeadTilt;
            canvas.Ellipse(head, 10f, 8.5f, build.Armor);

            // The snout, and a jaw that hinges off it. Wide open is the bite's strike frame.
            var snoutBase = head + Canvas.Rotate(new Vector2(6f, 1f), tilt);
            var snoutTip = head + Canvas.Rotate(new Vector2(19f, -1f), tilt);
            canvas.Thick(snoutBase, snoutTip, 7f, 4f, build.Armor);
            var jawTip = head + Canvas.Rotate(new Vector2(17f, -2f), tilt - pose.Jaw);
            canvas.Thick(snoutBase + Canvas.Rotate(new Vector2(0f, -3f), tilt), jawTip, 5f, 2.5f,
                build.Metal);
            if (pose.Jaw > 6f)
            {
                // Teeth only when the jaw is open; a closed mouth with visible fangs reads as noise.
                canvas.Triangle(snoutBase + Canvas.Rotate(new Vector2(7f, -2f), tilt),
                    snoutBase + Canvas.Rotate(new Vector2(11f, -2f), tilt),
                    snoutBase + Canvas.Rotate(new Vector2(9f, -7f), tilt), build.Trim);
                canvas.Triangle(jawTip + Canvas.Rotate(new Vector2(-6f, 1f), tilt),
                    jawTip + Canvas.Rotate(new Vector2(-2f, 1f), tilt),
                    jawTip + Canvas.Rotate(new Vector2(-4f, 5f), tilt), build.Trim);
            }

            // Ears, back along the skull.
            for (var i = -1; i <= 1; i += 2)
            {
                var earBase = head + Canvas.Rotate(new Vector2(-3f, 6f + i * 1.5f), tilt);
                canvas.Triangle(earBase,
                    earBase + Canvas.Rotate(new Vector2(-6f, 2f), tilt),
                    earBase + Canvas.Rotate(new Vector2(-1f, 11f + i * 2f), tilt), build.Metal);
            }

            // The corruption: violet, over the body only, brightest along the spine. The eye is a
            // dot of the same colour, which is what makes it findable in a dark clearing.
            var glow = Mathf.Clamp01(0.35f + pose.Glow * 0.65f);
            var mark = new Color32(build.Cloth.r, build.Cloth.g, build.Cloth.b,
                (byte)Mathf.RoundToInt(255f * glow));
            for (var i = 0; i < 5; i++)
            {
                var at = Vector2.Lerp(rear, front, i / 4f);
                canvas.RectOver(at.x - 2f, at.y + 6f, at.x + 2f, at.y + 10f, mark);
            }

            Eye(canvas, head + Canvas.Rotate(new Vector2(3f, 3f), tilt), 1.9f, VioletEye, pose.EyeFlare);

            if (pose.Alpha < 0.999f)
            {
                canvas.Fade(pose.Alpha);
            }
        }
    }
}
