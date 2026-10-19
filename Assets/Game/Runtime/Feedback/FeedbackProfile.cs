using System;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// Every combat moment that gets its own presentation. Gameplay never reads this enum — it is
    /// the vocabulary the feedback layer speaks, and nothing else.
    /// </summary>
    public enum FeedbackKind
    {
        LightHit,
        HeavyHit,
        CriticalHit,
        Block,
        Parry,
        GuardBreak,
        StaggerBreak,
        PlayerHurt,
        FireImpact,
        FrostImpact,
        ArcaneBurst,
        ShadowImpact,
        ShadowDashStart,
        ShadowDashEnd,
        BleedProc,
        PoisonProc,
        BurnProc,
        FrostProc,
        Dash,
        AirDash,
        WallContact,
        GroundBreaker,
        DashStrike,
        BossSlam,
        BossCharge,
        BossPhase,
        BossDeath,
        EnemyDeath,
        Pickup,
        /// <summary>A swing down bouncing off an enemy or spikes. Appended: kinds are stored as numbers.</summary>
        Pogo,
        // Movement dust (appended: kinds are stored as numbers).
        JumpDust,
        LandDust,
        RunDust,
        WallSlideDust,
        WallJump,
        AirJump,
        /// <summary>The glint on an enemy's weapon as a swing winds up.</summary>
        EnemyTelegraph,
        /// <summary>A boss about to charge or leap: the red warning over its body.</summary>
        BossDanger,
        /// <summary>A boss arena's barrier sealing, and breaking once the boss is beaten.</summary>
        ArenaSeal,
        ArenaOpen,
        /// <summary>Not a burst: the rune loop and light the sealed barrier carries.</summary>
        ArenaRunes,
        // Magic and altars (item 6 of the 2026-09-27 list).
        CastFizzle,
        AltarKindle,
        /// <summary>The gold left at the last death taken back (item 8 of the 2026-09-27 list).</summary>
        LostGoldRecovered
    }

    /// <summary>
    /// How one combat moment looks and feels: the burst, the freeze, the kick, the flash and the
    /// light. Authored as data so a designer can retune impact without touching combat code, and
    /// so two moments can be compared side by side instead of hunting through call sites.
    ///
    /// It holds presentation only. There is deliberately no damage, no range and no cooldown here:
    /// those live in <see cref="AttackDefinition"/> and <see cref="SpellDefinition"/>, and a
    /// feedback asset must never become a second place where the game is balanced.
    /// </summary>
    [CreateAssetMenu(menuName = "Forgotten Kingdom/Feedback Profile", fileName = "feedback_profile")]
    public sealed class FeedbackProfile : ScriptableObject
    {
        [Header("Burst")]
        [Tooltip("Frames of the impact effect. Empty means this moment has no particle burst.")]
        [SerializeField] private AnimationClipDefinition _burst = new AnimationClipDefinition();
        [SerializeField] private Color _tint = Color.white;
        [SerializeField, Min(0.01f)] private float _scale = 1f;
        [Tooltip("Turns the burst to face the hit direction; off for effects with no direction.")]
        [SerializeField] private bool _facesHitDirection = true;
        [Tooltip("Mirrors the burst left or right instead of turning it, for effects with a floor "
            + "under them: turned, ground dust thrown left would hang upside down.")]
        [SerializeField] private bool _mirrorsToDirection;
        [SerializeField] private string _sortingLayer = "VFXFront";
        [SerializeField] private int _sortingOrder;

        [Header("Feel")]
        [SerializeField, Min(0f)] private float _hitStop = 0.03f;
        [SerializeField] private ShakeProfile _shake = ShakeProfile.Tiny;
        [SerializeField, Min(0f)] private float _shakeScale = 1f;
        [Tooltip("1 shakes evenly; below 1 favours horizontal, above 1 favours vertical.")]
        [SerializeField, Min(0f)] private float _shakeVerticalBias = 1f;
        [Tooltip("Share of the shake spent as one push along the blow before the tremble: 0 trembles "
            + "only, 1 is all push. The camera follows the strike the way the eye does.")]
        [SerializeField, Range(0f, 1f)] private float _shakeKick;

        [Header("Screen and light")]
        [Tooltip("0 for almost everything. Reserved for parry, guard break and boss moments.")]
        [SerializeField, Range(0f, 1f)] private float _screenFlash;
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private Color _lightColor = Color.clear;
        [SerializeField, Min(0f)] private float _lightRadius = 2f;
        [SerializeField, Min(0f)] private float _lightDuration = 0.18f;

        [Header("Routing")]
        [Tooltip("When several moments land together, the highest priority owns the screen.")]
        [SerializeField, Min(0)] private int _priority = 10;
        [Tooltip("Hook for audio, wired in a later phase. Nothing reads it yet.")]
        [SerializeField] private string _soundEvent = string.Empty;
        [Tooltip("Hook for controller vibration where a platform supports it.")]
        [SerializeField, Range(0f, 1f)] private float _haptic;

        public AnimationClipDefinition Burst => _burst;
        public Color Tint => _tint;
        public float Scale => _scale;
        public bool FacesHitDirection => _facesHitDirection;
        public bool MirrorsToDirection => _mirrorsToDirection;
        public string SortingLayer => string.IsNullOrEmpty(_sortingLayer) ? "VFXFront" : _sortingLayer;
        public int SortingOrder => _sortingOrder;
        public ShakeProfile Shake => _shake;
        public float ShakeScale => _shakeScale;
        public float ShakeVerticalBias => _shakeVerticalBias;
        public float ShakeKick => _shakeKick;
        public Color FlashColor => _flashColor;
        public Color LightColor => _lightColor;
        public float LightRadius => _lightRadius;
        public float LightDuration => _lightDuration;
        public int Priority => _priority;
        public string SoundEvent => _soundEvent;
        public float Haptic => _haptic;

        /// <summary>The freeze, already scaled by the player's accessibility setting.</summary>
        public float HitStop => _hitStop * FeedbackSettings.HitStopMultiplier;

        /// <summary>The flash, already scaled. Zero for everything but the rarest moments.</summary>
        public float ScreenFlash => _screenFlash * FeedbackSettings.FlashIntensity;

        public bool HasBurst => _burst != null && _burst.HasFrames;
        public bool HasLight => _lightColor.a > 0.001f && _lightDuration > 0f;
    }

}
