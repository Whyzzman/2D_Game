namespace ForgottenKingdom
{
    /// <summary>
    /// The mixing groups. Five is the minimum that lets a player turn down what bothers them
    /// without silencing what they need: music, combat, menus and the room are different things.
    /// </summary>
    public enum AudioBus
    {
        /// <summary>Scales everything else. Never used directly by an event.</summary>
        Master,
        Music,
        /// <summary>Combat, movement, magic — everything the world does.</summary>
        Sfx,
        /// <summary>Menus. Kept audible while paused, when gameplay is not.</summary>
        Ui,
        /// <summary>The room itself. Quiet, constant, and never carries information.</summary>
        Ambience
    }

    /// <summary>
    /// Every sound the game can ask for. Gameplay names a moment, never a file — which is what
    /// keeps clip choice, variation and mixing out of combat code entirely.
    ///
    /// Zero is <see cref="None"/> on purpose: Unity's serializer skips field initializers, so a
    /// forgotten reference asks for silence rather than firing a random sound.
    /// </summary>
    public enum AudioId
    {
        None,

        // ---- player movement ----
        FootstepStone,
        Land,
        Jump,
        DashGround,
        DashAir,
        WallSlide,
        WallContact,

        // ---- weapons ----
        SwordSwingLight,
        SwordSwingHeavy,
        GreatswordSwingLight,
        GreatswordSwingHeavy,
        WeaponSwitch,

        // ---- impacts ----
        HitFlesh,
        HitArmor,
        HitHeavy,
        HitCritical,

        // ---- defence ----
        Block,
        BlockShield,
        Parry,
        GuardBreak,
        StaggerBreak,

        // ---- the player being hurt ----
        PlayerHurt,
        PlayerHurtHeavy,
        PlayerDeath,

        // ---- skills ----
        GroundBreaker,
        DashStrike,
        PerfectDodge,

        // ---- magic ----
        FireCast,
        FireImpact,
        FrostCast,
        FrostImpact,
        ArcaneCast,
        ArcaneBurst,
        ManaShieldUp,
        ManaShieldHit,
        CastFailed,

        // ---- shadow ----
        ShadowDashStart,
        ShadowDashEnd,
        ShadowStrike,
        ShadowImpact,

        // ---- statuses ----
        BleedProc,
        PoisonProc,
        BurnProc,
        FrostProc,

        // ---- zombie soldier ----
        ZombieStep,
        ZombieAlert,
        ZombieSwing,
        ZombieHurt,
        ZombieDeath,

        // ---- shield guard ----
        GuardStep,
        GuardRaise,
        GuardBash,
        GuardHeavy,
        GuardHurt,
        GuardDeath,

        // ---- guardian of the tomb ----
        BossAwaken,
        BossStep,
        BossSlash,
        BossBash,
        BossChargeStart,
        BossCharge,
        BossCombo,
        BossPhase,
        BossJump,
        BossSlam,
        BossStunned,
        BossDeath,

        // ---- world ----
        CheckpointActivate,
        CheckpointRest,
        Respawn,
        PickupCommon,
        PickupRare,
        PickupCurrency,
        LevelUp,
        SkillUnlock,

        // ---- interface ----
        UiNavigate,
        UiConfirm,
        UiCancel,
        UiOpen,
        UiClose,
        UiError,
        UiSaveFailed,

        // ---- ambience ----
        AmbienceTombBase,
        AmbienceArenaBase,
        AmbienceStoneSettle,
        AmbienceChain,
        AmbienceDrip,
        AmbienceWind,

        // ---- corrupted hound ----
        HoundStep,
        HoundGrowl,
        HoundBite,
        HoundLeap,
        HoundHurt,
        HoundDeath,

        // ---- forest archer ----
        ArcherStep,
        ArcherDraw,
        ArcherRelease,
        ArrowImpact,
        ArcherHurt,
        ArcherDeath,

        // ---- root shaman ----
        ShamanStep,
        ShamanCast,
        ShamanOrb,
        ShamanZone,
        ShamanHurt,
        ShamanDeath,

        // ---- hollow warden ----
        WardenAwaken,
        WardenStep,
        WardenGlaive,
        WardenThrust,
        WardenSweep,
        WardenRootShot,
        WardenRootSlam,
        WardenPoisonField,
        WardenLeap,
        WardenPhase,
        WardenStunned,
        WardenDeath,

        // ---- forest world ----
        HazardTelegraph,
        HazardBurst,
        GateOpen,
        DoubleJump,
        AbilityUnlocked,

        // ---- forest ambience ----
        AmbienceForestBase,
        AmbienceDeepForestBase,
        AmbienceLeaves,
        AmbienceBranch,
        AmbienceInsects,
        AmbienceDistantBeast,
        AmbienceCorruptionHum,

        // ---- healing (appended: ids are stored as numbers) ----
        FlaskDrink,
        HerbEat,

        // ---- pogo ----
        Pogo,

        // ---- a spell lost to a hit (appended) ----
        CastFizzle,

        // ---- a dead body crumbling to ash (appended) ----
        CorpseCrumble,

        // ---- the merchant and his chests (item 70, appended) ----
        ShopBuy,
        ChestOpen,

        // ---- the Ashen Capital (item 2 of the big list, appended) ----
        AmbienceCapitalBase,
        AmbienceBellToll,
        AmbienceEmbers,

        // ---- the capital's enemies (item 2, stage C, appended) ----
        RavenCaw,
        GargoyleWake,
        PriestHeal,
        PriestBlink,

        // ---- the Oath-Bound King (item 2, stage D, appended) ----
        KingAwaken,
        KingStep,
        KingRage,
        KingStunned,
        KingDeath,

        // ---- exploration (item 4, stage B, appended) ----
        WallCrack,
        WallCrumble,
        TrialGong,

        // ---- the ground under the feet (GAME_FEEL_IDEAS item 4, appended) ----
        FootstepWood,
        FootstepEarth,
        FootstepWater,
        FootstepAsh,
        // ---- footwork (2026-10-19, appended): the skid of a start or a turn, the kick off a wall ----
        FootstepScuff,
        WallJumpKick,
        // ---- the world answering the hero (GAME_FEEL_IDEAS items 13-14, appended) ----
        PropBreak,
        // ---- the hero's death and return (GAME_FEEL_IDEAS item 22, appended) ----
        HeroFall,
        AltarCall
        // ---- ringing ears when the hero is hit (GAME_FEEL_IDEAS item 10, appended) ----
        HurtRing
        // ---- the heart heard at low health (GAME_FEEL_IDEAS item 17, appended) ----
        Heartbeat
    }
}
