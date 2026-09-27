using System;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenKingdom.Tests
{
    /// <summary>
    /// The animation layer as data: how a clip reads a frame, how a set answers for a state, what
    /// the weapon override does, and whether the shipped assets actually carry frames.
    /// </summary>
    public sealed class AnimationDataTests
    {
        private PlayerAnimationSet _set;

        [SetUp]
        public void SetUp()
        {
            _set = Load<PlayerAnimationSet>("Assets/Game/Data/Animation/PlayerAnimations.asset");
        }

        // ---------- clips ----------

        [Test]
        public void ALoopingClipReturnsToItsFirstFrame()
        {
            var clip = Clip(4, fps: 10f, looping: true);

            Assert.That(clip.FrameAt(0f), Is.SameAs(clip.Frames[0]));
            Assert.That(clip.FrameAt(0.15f), Is.SameAs(clip.Frames[1]));
            Assert.That(clip.FrameAt(0.25f), Is.SameAs(clip.Frames[2]));
            Assert.That(clip.FrameAt(0.4f), Is.SameAs(clip.Frames[0]),
                "One full pass takes 0.4 s, so the loop is back at the start.");
        }

        [Test]
        public void ANonLoopingClipHoldsItsLastFrame()
        {
            var clip = Clip(3, fps: 10f, looping: false);

            // A death or a landing can outlive its clip. Holding the pose is the only answer that
            // is never wrong; vanishing would be a hole in the character.
            Assert.That(clip.FrameAt(10f), Is.SameAs(clip.Frames[2]));
        }

        [Test]
        public void AnEmptyClipIsHarmless()
        {
            var clip = new AnimationClipDefinition("empty", Array.Empty<Sprite>(), 12f, true);

            Assert.That(clip.HasFrames, Is.False);
            Assert.That(clip.Duration, Is.Zero);
            Assert.That(clip.FrameAt(1f), Is.Null, "Missing art must not throw.");
        }

        [Test]
        public void DurationFollowsTheFrameRate()
        {
            Assert.That(Clip(6, 12f, true).Duration, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(Clip(6, 24f, true).Duration, Is.EqualTo(0.25f).Within(0.0001f));
        }

        // ---------- the shipped set ----------

        [Test]
        public void EveryStateHasFrames()
        {
            foreach (PlayerAnimationState state in Enum.GetValues(typeof(PlayerAnimationState)))
            {
                Assert.That(_set.Has(state), Is.True,
                    $"{state} has no clip; the pipeline expects one for every state.");
            }
        }

        [Test]
        public void TheFallbackIsNeverEmpty()
        {
            Assert.That(_set.Fallback.HasFrames, Is.True);
            Assert.That(_set.Validate(), Is.True);
        }

        [Test]
        public void CombatRunsAtTwiceTheFrameRateOfStances()
        {
            // The Visual Bible fixed 12 FPS for stances and 24 for combat; if this drifts the whole
            // frame budget quoted to the artist stops meaning anything.
            Assert.That(_set.Clip(PlayerAnimationState.Attack1, null).FramesPerSecond,
                Is.EqualTo(24f));
            Assert.That(_set.Clip(PlayerAnimationState.Run, null).FramesPerSecond,
                Is.EqualTo(12f));
        }

        [Test]
        public void LocomotionLoopsAndCommittedActionsDoNot()
        {
            Assert.That(_set.Clip(PlayerAnimationState.Idle, null).IsLooping, Is.True);
            Assert.That(_set.Clip(PlayerAnimationState.Run, null).IsLooping, Is.True);
            Assert.That(_set.Clip(PlayerAnimationState.Death, null).IsLooping, Is.False);
            Assert.That(_set.Clip(PlayerAnimationState.Attack1, null).IsLooping, Is.False);
        }

        [Test]
        public void TheSwingsUpAndDownAreTheirOwnPoses()
        {
            var side = _set.Clip(PlayerAnimationState.Attack1, null);
            foreach (var state in new[] { PlayerAnimationState.AttackUp, PlayerAnimationState.AttackDown })
            {
                var clip = _set.Clip(state, null);
                Assert.That(clip, Is.Not.SameAs(side), $"{state} still borrows the side swing.");
                Assert.That(clip.IsLooping, Is.False);
                var greatsword = _set.WeaponSet(WeaponType.Greatsword);
                Assert.That(_set.Clip(state, greatsword), Is.Not.SameAs(clip),
                    $"The greatsword's {state} is its own, as its side swings are.");
            }
        }

        // ---------- weapon overrides ----------

        [Test]
        public void TheGreatswordBringsItsOwnSwings()
        {
            var greatsword = _set.WeaponSet(WeaponType.Greatsword);

            Assert.That(greatsword, Is.Not.Null, "The content build ships a greatsword set.");
            Assert.That(greatsword.HasCompleteAttackSet, Is.True);
            Assert.That(_set.Clip(PlayerAnimationState.Attack1, greatsword),
                Is.Not.SameAs(_set.Clip(PlayerAnimationState.Attack1, null)),
                "A weapon override that returned the shared clip would prove nothing.");
        }

        [Test]
        public void AWeaponOnlyOverridesWhatItAuthors()
        {
            var greatsword = _set.WeaponSet(WeaponType.Greatsword);

            // The greatsword changes how the player swings, not how the player runs.
            Assert.That(_set.Clip(PlayerAnimationState.Run, greatsword),
                Is.SameAs(_set.Clip(PlayerAnimationState.Run, null)));
        }

        [Test]
        public void AWeaponWithNoSetOfItsOwnUsesTheSharedClips()
        {
            Assert.That(_set.WeaponSet(WeaponType.Sword), Is.Null);
            Assert.That(_set.Clip(PlayerAnimationState.Attack1, null).HasFrames, Is.True,
                "The sword falls back to the shared swings rather than to nothing.");
        }

        // ---------- the link from skills to animation ----------

        [Test]
        public void ActiveSkillsNameTheirOwnAnimation()
        {
            var skills = Load<SkillDatabase>("Assets/Game/Data/SkillDatabase.asset");

            Assert.That(skills.GetSkill("warrior_ground_breaker").Active.AnimationState,
                Is.EqualTo(PlayerAnimationState.GroundBreaker));
            Assert.That(skills.GetSkill("hunter_dash_strike").Active.AnimationState,
                Is.EqualTo(PlayerAnimationState.DashStrike));
        }

        [Test]
        public void ASkillThatNamesNoAnimationGetsTheGenericOne()
        {
            // Unity's serializer skips field initializers, so anything unset lands on the enum's
            // zero value. That value is deliberately the generic skill animation, never Idle.
            Assert.That(default(PlayerAnimationState),
                Is.EqualTo(PlayerAnimationState.ActiveSkill));
        }

        private static AnimationClipDefinition Clip(int frames, float fps, bool looping)
        {
            var sprites = new Sprite[frames];
            for (var i = 0; i < frames; i++)
            {
                var texture = new Texture2D(2, 2);
                sprites[i] = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.zero);
            }

            return new AnimationClipDefinition("test", sprites, fps, looping);
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.That(asset, Is.Not.Null, $"The content build writes {path}.");
            return asset;
        }
    }
}
